using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;
using YallaCatch.Models;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages achievements and player progression
    /// Syncs with backend /api/v1/gamification/achievements
    /// Tracks progress and unlocks
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float checkInterval = 5f; // Check achievements every 5 seconds

        private List<Achievement> allAchievements = new List<Achievement>();
        private List<Achievement> unlockedAchievements = new List<Achievement>();
        private Dictionary<string, float> progressTracker = new Dictionary<string, float>();

        public event System.Action<Achievement> OnAchievementUnlocked;
        public event System.Action OnAchievementsUpdated;

        #region Unity Lifecycle

        private void Awake()
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

        private void Start()
        {
            StartCoroutine(InitializeAchievementsWhenReady());
            StartCoroutine(CheckAchievementsCoroutine());
        }

        private IEnumerator InitializeAchievementsWhenReady()
        {
            float timeout = 5f;
            float elapsed = 0f;

            while ((APIClient.Instance == null || GamificationAPI.Instance == null) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (APIClient.Instance == null || GamificationAPI.Instance == null)
            {
                Debug.LogWarning("[AchievementManager] API dependencies are not ready. Achievement loading will be skipped for this session.");
                yield break;
            }

            if (!APIClient.Instance.IsAuthenticated)
            {
                Debug.Log("[AchievementManager] Skipping achievement load until player is authenticated.");
                yield break;
            }

            yield return StartCoroutine(LoadAchievements());
        }

        #endregion

        #region Load Achievements

        private IEnumerator LoadAchievements()
        {
            if (GamificationAPI.Instance == null)
            {
                Debug.LogWarning("[AchievementManager] GamificationAPI is not available.");
                yield break;
            }

            yield return StartCoroutine(GamificationAPI.Instance.GetAchievements((achievements) =>
            {
                if (achievements != null)
                {
                    allAchievements = new List<Achievement>(achievements);
                    
                    // Separate unlocked achievements
                    unlockedAchievements.Clear();
                    foreach (var achievement in allAchievements)
                    {
                        if (achievement.isUnlocked)
                        {
                            unlockedAchievements.Add(achievement);
                        }
                    }

                    OnAchievementsUpdated?.Invoke();

                    Debug.Log($"Loaded {allAchievements.Count} achievements ({unlockedAchievements.Count} unlocked)");
                }
            }));
        }

        #endregion

        #region Track Progress

        public void TrackProgress(string achievementType, float amount)
        {
            if (!progressTracker.ContainsKey(achievementType))
            {
                progressTracker[achievementType] = 0f;
            }

            progressTracker[achievementType] += amount;
            
            // Check if any achievement is completed
            CheckAchievements();
        }

        public void IncrementProgress(string achievementType)
        {
            TrackProgress(achievementType, 1f);
        }

        private IEnumerator CheckAchievementsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);
                CheckAchievements();
            }
        }

        private void CheckAchievements()
        {
            foreach (var achievement in allAchievements)
            {
                if (achievement.isUnlocked)
                    continue;

                if (IsAchievementCompleted(achievement))
                {
                    StartCoroutine(UnlockAchievement(achievement));
                }
            }
        }

        private bool IsAchievementCompleted(Achievement achievement)
        {
            if (!progressTracker.ContainsKey(achievement.type))
                return false;

            float progress = progressTracker[achievement.type];
            return progress >= achievement.targetValue;
        }

        #endregion

        #region Unlock Achievement

        private IEnumerator UnlockAchievement(Achievement achievement)
        {
            Debug.Log($"Unlocking achievement: {achievement.name}");

            if (GamificationAPI.Instance == null)
            {
                Debug.LogWarning("[AchievementManager] Cannot unlock achievement because GamificationAPI is missing.");
                yield break;
            }

            yield return StartCoroutine(GamificationAPI.Instance.UnlockAchievement(achievement._id, (success, points) =>
            {
                if (success)
                {
                    achievement.isUnlocked = true;
                    unlockedAchievements.Add(achievement);

                    // Add points to player
                    GameManager.Instance.AddPoints(points);

                    // Trigger event
                    OnAchievementUnlocked?.Invoke(achievement);

                    // Show notification
                    NotificationManager.Instance?.NotifyAchievementUnlocked(achievement.name);

                    // Show UI
                    ShowAchievementUnlockedUI(achievement);

                    Debug.Log($"Achievement unlocked: {achievement.name} (+{points} points)");
                }
            }));
        }

        private void ShowAchievementUnlockedUI(Achievement achievement)
        {
            UI.UIManager.Instance?.ShowMessage($"Achievement Unlocked!\n{achievement.name}\n+{achievement.rewardPoints} points");
        }

        #endregion

        #region Predefined Achievement Tracking

        public void OnPrizeCaptured(string prizeType)
        {
            IncrementProgress("capture");
            IncrementProgress($"capture_{prizeType}");
        }

        public void OnRewardClaimed()
        {
            IncrementProgress("claim");
        }

        public void OnDailyLogin()
        {
            IncrementProgress("daily_login");
        }

        public void OnDistanceWalked(float meters)
        {
            TrackProgress("distance", meters);
        }

        public void OnFriendAdded()
        {
            IncrementProgress("friends");
        }

        public void OnAdWatched()
        {
            IncrementProgress("ads_watched");
        }

        #endregion

        #region Get Achievement Data

        public List<Achievement> GetAllAchievements()
        {
            return new List<Achievement>(allAchievements);
        }

        public List<Achievement> GetUnlockedAchievements()
        {
            return new List<Achievement>(unlockedAchievements);
        }

        public float GetProgress(string achievementType)
        {
            if (progressTracker.ContainsKey(achievementType))
            {
                return progressTracker[achievementType];
            }
            return 0f;
        }

        public float GetAchievementProgress(Achievement achievement)
        {
            if (achievement.isUnlocked)
                return 1f;

            float current = GetProgress(achievement.type);
            return Mathf.Clamp01(current / achievement.targetValue);
        }

        #endregion

        #region Refresh

        public void RefreshAchievements()
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                Debug.Log("[AchievementManager] RefreshAchievements skipped (not authenticated).");
                return;
            }

            StartCoroutine(LoadAchievements());
        }

        #endregion
    }
}
