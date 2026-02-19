using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace YallaCatch
{
    /// <summary>
    /// Gère les défis quotidiens et hebdomadaires
    /// Intégration avec le module game backend (challenges endpoints)
    /// Augmente l'engagement quotidien avec des objectifs et récompenses
    /// Contrôlé par l'admin panel
    /// </summary>
    public class ChallengesManager : MonoBehaviour
    {
        public static ChallengesManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float refreshInterval = 60f; // Rafraîchir chaque minute
        
        // État
        private List<DailyChallenge> dailyChallenges = new List<DailyChallenge>();
        private List<WeeklyChallenge> weeklyChallenges = new List<WeeklyChallenge>();
        private Dictionary<string, ChallengeProgress> challengeProgress = new Dictionary<string, ChallengeProgress>();
        
        private float lastRefresh = 0f;
        private bool isInitialized = false;

        // Events
        public delegate void OnChallengesUpdated();
        public event OnChallengesUpdated OnChallengesListUpdated;
        
        public delegate void OnChallengeCompleted(DailyChallenge challenge);
        public event OnChallengeCompleted OnChallengeCompletedEvent;
        
        public delegate void OnChallengeProgressUpdated(string challengeId, float progress);
        public event OnChallengeProgressUpdated OnProgressUpdated;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            StartCoroutine(Initialize());
        }

        void Update()
        {
            if (!isInitialized) return;
            
            // Rafraîchir périodiquement
            if (Time.time - lastRefresh > refreshInterval)
            {
                StartCoroutine(LoadDailyChallenges());
                lastRefresh = Time.time;
            }
        }

        /// <summary>
        /// Initialise le système de challenges
        /// </summary>
        private IEnumerator Initialize()
        {
            // Charger les défis quotidiens
            yield return StartCoroutine(LoadDailyChallenges());
            
            isInitialized = true;
            Debug.Log("[ChallengesManager] Initialized successfully");
        }

        #region API Calls

        /// <summary>
        /// GET /game/challenges/daily - Charge les défis quotidiens
        /// </summary>
        public IEnumerator LoadDailyChallenges(System.Action<List<DailyChallenge>> callback = null)
        {
            string endpoint = "/game/challenges/daily";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    dailyChallenges = JsonConvert.DeserializeObject<List<DailyChallenge>>(response.data.ToString());
                    
                    // Mettre à jour la progression locale
                    foreach (var challenge in dailyChallenges)
                    {
                        if (!challengeProgress.ContainsKey(challenge.id))
                        {
                            challengeProgress[challenge.id] = new ChallengeProgress
                            {
                                challengeId = challenge.id,
                                currentValue = challenge.currentProgress,
                                targetValue = challenge.targetValue,
                                isCompleted = challenge.isCompleted
                            };
                        }
                    }
                    
                    OnChallengesListUpdated?.Invoke();
                    callback?.Invoke(dailyChallenges);
                    
                    Debug.Log($"[ChallengesManager] Loaded {dailyChallenges.Count} daily challenges");
                }
                else
                {
                    callback?.Invoke(new List<DailyChallenge>());
                }
            });
        }

        /// <summary>
        /// POST /game/challenges/complete - Marque un défi comme complété
        /// </summary>
        public IEnumerator CompleteChallenge(string challengeId, System.Action<ChallengeCompletionResult> callback = null)
        {
            var data = new
            {
                challengeId = challengeId,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            string endpoint = "/game/challenges/complete";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    var result = JsonConvert.DeserializeObject<ChallengeCompletionResult>(response.data.ToString());
                    
                    // Mettre à jour la progression locale
                    if (challengeProgress.ContainsKey(challengeId))
                    {
                        challengeProgress[challengeId].isCompleted = true;
                    }
                    
                    // Trouver le challenge
                    DailyChallenge challenge = dailyChallenges.Find(c => c.id == challengeId);
                    if (challenge != null)
                    {
                        challenge.isCompleted = true;
                        
                        // Ajouter les récompenses
                        if (result.pointsReward > 0)
                        {
                            GameManager.Instance.AddPoints(result.pointsReward);
                        }
                        
                        if (result.xpReward > 0)
                        {
                            GameManager.Instance.AddXP(result.xpReward);
                        }
                        
                        OnChallengeCompletedEvent?.Invoke(challenge);
                        
                        // Afficher la notification
                        UIManager.Instance.ShowChallengeCompletePopup(challenge, result);
                    }
                    
                    callback?.Invoke(result);
                    
                    Debug.Log($"[ChallengesManager] Challenge completed: {challengeId}");
                }
                else
                {
                    callback?.Invoke(null);
                }
            });
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Met à jour la progression d'un défi
        /// </summary>
        public void UpdateChallengeProgress(string challengeType, int value)
        {
            foreach (var challenge in dailyChallenges)
            {
                if (challenge.type == challengeType && !challenge.isCompleted)
                {
                    if (!challengeProgress.ContainsKey(challenge.id))
                    {
                        challengeProgress[challenge.id] = new ChallengeProgress
                        {
                            challengeId = challenge.id,
                            currentValue = 0,
                            targetValue = challenge.targetValue,
                            isCompleted = false
                        };
                    }
                    
                    challengeProgress[challenge.id].currentValue += value;
                    challenge.currentProgress = challengeProgress[challenge.id].currentValue;
                    
                    // Calculer le pourcentage
                    float progress = (float)challenge.currentProgress / challenge.targetValue;
                    OnProgressUpdated?.Invoke(challenge.id, progress);
                    
                    // Vérifier si le défi est complété
                    if (challenge.currentProgress >= challenge.targetValue)
                    {
                        StartCoroutine(CompleteChallenge(challenge.id));
                    }
                    
                    Debug.Log($"[ChallengesManager] Progress updated: {challenge.name} - {challenge.currentProgress}/{challenge.targetValue}");
                }
            }
        }

        /// <summary>
        /// Appelé quand un prize est capturé
        /// </summary>
        public void OnPrizeCaptured(string prizeType, int points)
        {
            UpdateChallengeProgress("capture_prizes", 1);
            UpdateChallengeProgress("earn_points", points);
            
            // Challenges spécifiques par type de prize
            if (prizeType == "rare")
            {
                UpdateChallengeProgress("capture_rare_prizes", 1);
            }
        }

        /// <summary>
        /// Appelé quand le joueur marche
        /// </summary>
        public void OnDistanceWalked(float meters)
        {
            UpdateChallengeProgress("walk_distance", Mathf.RoundToInt(meters));
        }

        /// <summary>
        /// Appelé quand un ami est ajouté
        /// </summary>
        public void OnFriendAdded()
        {
            UpdateChallengeProgress("add_friends", 1);
        }

        /// <summary>
        /// Appelé quand une vidéo AdMob est regardée
        /// </summary>
        public void OnAdWatched()
        {
            UpdateChallengeProgress("watch_ads", 1);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Obtient tous les défis quotidiens
        /// </summary>
        public List<DailyChallenge> GetDailyChallenges()
        {
            return dailyChallenges;
        }

        /// <summary>
        /// Obtient les défis actifs (non complétés)
        /// </summary>
        public List<DailyChallenge> GetActiveChallenges()
        {
            return dailyChallenges.Where(c => !c.isCompleted).ToList();
        }

        /// <summary>
        /// Obtient les défis complétés
        /// </summary>
        public List<DailyChallenge> GetCompletedChallenges()
        {
            return dailyChallenges.Where(c => c.isCompleted).ToList();
        }

        /// <summary>
        /// Obtient la progression d'un défi
        /// </summary>
        public ChallengeProgress GetChallengeProgress(string challengeId)
        {
            if (challengeProgress.ContainsKey(challengeId))
            {
                return challengeProgress[challengeId];
            }
            return null;
        }

        /// <summary>
        /// Obtient le pourcentage de progression d'un défi
        /// </summary>
        public float GetChallengeProgressPercentage(string challengeId)
        {
            if (challengeProgress.ContainsKey(challengeId))
            {
                var progress = challengeProgress[challengeId];
                return (float)progress.currentValue / progress.targetValue;
            }
            return 0f;
        }

        /// <summary>
        /// Obtient le nombre de défis complétés aujourd'hui
        /// </summary>
        public int GetCompletedChallengesCount()
        {
            return dailyChallenges.Count(c => c.isCompleted);
        }

        /// <summary>
        /// Obtient le nombre total de défis
        /// </summary>
        public int GetTotalChallengesCount()
        {
            return dailyChallenges.Count;
        }

        /// <summary>
        /// Vérifie si tous les défis sont complétés
        /// </summary>
        public bool AreAllChallengesCompleted()
        {
            return dailyChallenges.All(c => c.isCompleted);
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class DailyChallenge
    {
        public string id;
        public string name;
        public string description;
        public string type; // capture_prizes, earn_points, walk_distance, add_friends, watch_ads, capture_rare_prizes
        public int targetValue;
        public int currentProgress;
        public bool isCompleted;
        public int pointsReward;
        public int xpReward;
        public string icon;
        public string expiresAt;
    }

    [System.Serializable]
    public class WeeklyChallenge
    {
        public string id;
        public string name;
        public string description;
        public string type;
        public int targetValue;
        public int currentProgress;
        public bool isCompleted;
        public int pointsReward;
        public int xpReward;
        public string specialReward; // Item spécial pour les challenges hebdomadaires
        public string icon;
        public string expiresAt;
    }

    [System.Serializable]
    public class ChallengeProgress
    {
        public string challengeId;
        public int currentValue;
        public int targetValue;
        public bool isCompleted;
    }

    [System.Serializable]
    public class ChallengeCompletionResult
    {
        public bool success;
        public string message;
        public int pointsReward;
        public int xpReward;
        public string specialReward;
        public int totalChallengesCompleted;
        public bool allDailyChallengesCompleted;
        public int bonusPoints; // Bonus si tous les défis sont complétés
    }

    #endregion
}
