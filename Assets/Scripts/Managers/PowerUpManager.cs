using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.Managers;
using YallaCatch.UI;

namespace YallaCatch
{
    /// <summary>
    /// Gère tous les power-ups du jeu
    /// Intégration avec le module game backend (power-ups endpoints)
    /// Types: Radar, Multiplicateur de points, Aimant, Speed Boost, XP Boost
    /// Contrôlé par l'admin panel
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        [Header("Power-Up Settings")]
        [SerializeField] private float radarRange = 500f; // Rayon du radar en mètres
        [SerializeField] private Color radarColor = Color.yellow;
        [SerializeField] private Color magnetColor = Color.cyan;
        
        // État des power-ups actifs
        private Dictionary<string, ActivePowerUp> activePowerUps = new Dictionary<string, ActivePowerUp>();
        
        // Effets actifs
        private bool radarActive = false;
        private float pointsMultiplier = 1f;
        private float magnetRange = 0f;
        private float speedMultiplier = 1f;
        private float xpMultiplier = 1f;

        // Events
        public delegate void OnPowerUpActivated(string powerUpType);
        public event OnPowerUpActivated OnPowerUpActivatedEvent;
        
        public delegate void OnPowerUpExpired(string powerUpType);
        public event OnPowerUpExpired OnPowerUpExpiredEvent;

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

        void Update()
        {
            // Mettre à jour les power-ups actifs
            UpdateActivePowerUps();
        }

        #region Power-Up Activation

        /// <summary>
        /// Active le radar (révèle les prizes cachés)
        /// </summary>
        public void ActivateRadar(float duration)
        {
            string powerUpId = "radar";
            
            if (activePowerUps.ContainsKey(powerUpId))
            {
                // Prolonger la durée
                activePowerUps[powerUpId].remainingTime += duration;
            }
            else
            {
                // Activer le power-up
                activePowerUps[powerUpId] = new ActivePowerUp
                {
                    type = powerUpId,
                    remainingTime = duration,
                    startTime = Time.time
                };
                
                radarActive = true;
                
                // Révéler les prizes cachés
                RevealHiddenPrizes();
                
                OnPowerUpActivatedEvent?.Invoke(powerUpId);
                UIManager.Instance.ShowMessage($"Radar activated for {duration}s!");
                
                Debug.Log($"[PowerUpManager] Radar activated for {duration}s");
            }
            
            // Enregistrer l'utilisation côté backend
            StartCoroutine(RecordPowerUpUsage(powerUpId));
        }

        /// <summary>
        /// Active le multiplicateur de points
        /// </summary>
        public void ActivatePointsMultiplier(float multiplier, float duration)
        {
            string powerUpId = "multiplier";
            
            if (activePowerUps.ContainsKey(powerUpId))
            {
                // Prolonger la durée et augmenter le multiplicateur
                activePowerUps[powerUpId].remainingTime += duration;
                pointsMultiplier = Mathf.Max(pointsMultiplier, multiplier);
            }
            else
            {
                activePowerUps[powerUpId] = new ActivePowerUp
                {
                    type = powerUpId,
                    remainingTime = duration,
                    startTime = Time.time,
                    value = multiplier
                };
                
                pointsMultiplier = multiplier;
                
                OnPowerUpActivatedEvent?.Invoke(powerUpId);
                UIManager.Instance.ShowMessage($"{multiplier}x Points for {duration}s!");
                
                Debug.Log($"[PowerUpManager] Points multiplier {multiplier}x activated for {duration}s");
            }
            
            StartCoroutine(RecordPowerUpUsage(powerUpId));
        }

        /// <summary>
        /// Active l'aimant (augmente le rayon de capture)
        /// </summary>
        public void ActivateMagnet(float range, float duration)
        {
            string powerUpId = "magnet";
            
            if (activePowerUps.ContainsKey(powerUpId))
            {
                activePowerUps[powerUpId].remainingTime += duration;
                magnetRange = Mathf.Max(magnetRange, range);
            }
            else
            {
                activePowerUps[powerUpId] = new ActivePowerUp
                {
                    type = powerUpId,
                    remainingTime = duration,
                    startTime = Time.time,
                    value = range
                };
                
                magnetRange = range;
                
                OnPowerUpActivatedEvent?.Invoke(powerUpId);
                UIManager.Instance.ShowMessage($"Magnet activated! +{range}m range for {duration}s!");
                
                Debug.Log($"[PowerUpManager] Magnet activated: +{range}m for {duration}s");
            }
            
            StartCoroutine(RecordPowerUpUsage(powerUpId));
        }

        /// <summary>
        /// Active le boost de vitesse
        /// </summary>
        public void ActivateSpeedBoost(float multiplier, float duration)
        {
            string powerUpId = "speed";
            
            if (activePowerUps.ContainsKey(powerUpId))
            {
                activePowerUps[powerUpId].remainingTime += duration;
                speedMultiplier = Mathf.Max(speedMultiplier, multiplier);
            }
            else
            {
                activePowerUps[powerUpId] = new ActivePowerUp
                {
                    type = powerUpId,
                    remainingTime = duration,
                    startTime = Time.time,
                    value = multiplier
                };
                
                speedMultiplier = multiplier;
                
                OnPowerUpActivatedEvent?.Invoke(powerUpId);
                UIManager.Instance.ShowMessage($"Speed Boost {multiplier}x for {duration}s!");
                
                Debug.Log($"[PowerUpManager] Speed boost {multiplier}x activated for {duration}s");
            }
            
            StartCoroutine(RecordPowerUpUsage(powerUpId));
        }

        /// <summary>
        /// Active le boost d'XP
        /// </summary>
        public void ActivateXPBoost(float multiplier, float duration)
        {
            string powerUpId = "xp_boost";
            
            if (activePowerUps.ContainsKey(powerUpId))
            {
                activePowerUps[powerUpId].remainingTime += duration;
                xpMultiplier = Mathf.Max(xpMultiplier, multiplier);
            }
            else
            {
                activePowerUps[powerUpId] = new ActivePowerUp
                {
                    type = powerUpId,
                    remainingTime = duration,
                    startTime = Time.time,
                    value = multiplier
                };
                
                xpMultiplier = multiplier;
                
                OnPowerUpActivatedEvent?.Invoke(powerUpId);
                UIManager.Instance.ShowMessage($"XP Boost {multiplier}x for {duration}s!");
                
                Debug.Log($"[PowerUpManager] XP boost {multiplier}x activated for {duration}s");
            }
            
            StartCoroutine(RecordPowerUpUsage(powerUpId));
        }

        #endregion

        #region Power-Up Management

        /// <summary>
        /// Met à jour les power-ups actifs
        /// </summary>
        private void UpdateActivePowerUps()
        {
            List<string> expiredPowerUps = new List<string>();
            
            foreach (var kvp in activePowerUps)
            {
                string powerUpId = kvp.Key;
                ActivePowerUp powerUp = kvp.Value;
                
                powerUp.remainingTime -= Time.deltaTime;
                
                if (powerUp.remainingTime <= 0)
                {
                    expiredPowerUps.Add(powerUpId);
                }
            }
            
            // Retirer les power-ups expirés
            foreach (string powerUpId in expiredPowerUps)
            {
                DeactivatePowerUp(powerUpId);
            }
        }

        /// <summary>
        /// Désactive un power-up
        /// </summary>
        private void DeactivatePowerUp(string powerUpId)
        {
            if (!activePowerUps.ContainsKey(powerUpId))
                return;
            
            activePowerUps.Remove(powerUpId);
            
            // Réinitialiser les effets
            switch (powerUpId)
            {
                case "radar":
                    radarActive = false;
                    break;
                    
                case "multiplier":
                    pointsMultiplier = 1f;
                    break;
                    
                case "magnet":
                    magnetRange = 0f;
                    break;
                    
                case "speed":
                    speedMultiplier = 1f;
                    break;
                    
                case "xp_boost":
                    xpMultiplier = 1f;
                    break;
            }
            
            OnPowerUpExpiredEvent?.Invoke(powerUpId);
            UIManager.Instance.ShowMessage($"{powerUpId} expired");
            
            Debug.Log($"[PowerUpManager] {powerUpId} expired");
        }

        #endregion

        #region API Calls

        /// <summary>
        /// POST /game/power-ups/use - Enregistre l'utilisation d'un power-up
        /// </summary>
        private IEnumerator RecordPowerUpUsage(string powerUpType)
        {
            var data = new
            {
                powerUpType = powerUpType,
                timestamp = System.DateTime.UtcNow.ToString("o"),
                location = new
                {
                    lat = GPSManager.Instance.CurrentLatitude,
                    lng = GPSManager.Instance.CurrentLongitude
                }
            };
            
            string endpoint = "/game/powerup/use";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    Debug.Log($"[PowerUpManager] Power-up usage recorded: {powerUpType}");
                }
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Vérifie si un power-up est actif
        /// </summary>
        public bool IsPowerUpActive(string powerUpType)
        {
            return activePowerUps.ContainsKey(powerUpType);
        }

        /// <summary>
        /// Obtient le temps restant d'un power-up
        /// </summary>
        public float GetRemainingTime(string powerUpType)
        {
            if (activePowerUps.ContainsKey(powerUpType))
            {
                return activePowerUps[powerUpType].remainingTime;
            }
            return 0f;
        }

        /// <summary>
        /// Obtient tous les power-ups actifs
        /// </summary>
        public Dictionary<string, ActivePowerUp> GetActivePowerUps()
        {
            return activePowerUps;
        }

        /// <summary>
        /// Obtient le multiplicateur de points actuel
        /// </summary>
        public float GetPointsMultiplier()
        {
            return pointsMultiplier;
        }

        /// <summary>
        /// Obtient le rayon de l'aimant actuel
        /// </summary>
        public float GetMagnetRange()
        {
            return magnetRange;
        }

        /// <summary>
        /// Obtient le multiplicateur de vitesse actuel
        /// </summary>
        public float GetSpeedMultiplier()
        {
            return speedMultiplier;
        }

        /// <summary>
        /// Obtient le multiplicateur d'XP actuel
        /// </summary>
        public float GetXPMultiplier()
        {
            return xpMultiplier;
        }

        /// <summary>
        /// Vérifie si le radar est actif
        /// </summary>
        public bool IsRadarActive()
        {
            return radarActive;
        }

        /// <summary>
        /// Applique le multiplicateur de points à un montant
        /// </summary>
        public int ApplyPointsMultiplier(int basePoints)
        {
            return Mathf.RoundToInt(basePoints * pointsMultiplier);
        }

        /// <summary>
        /// Applique le multiplicateur d'XP à un montant
        /// </summary>
        public int ApplyXPMultiplier(int baseXP)
        {
            return Mathf.RoundToInt(baseXP * xpMultiplier);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Révèle les prizes cachés (effet du radar)
        /// </summary>
        private void RevealHiddenPrizes()
        {
            // Demander au GameManager de charger plus de prizes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNearbyPrizes(radarRange);
            }
            
            // Effet visuel sur la mini-map
            if (CameraLiveManager.Instance != null)
            {
                CameraLiveManager.Instance.RefreshPrizes();
            }
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class ActivePowerUp
    {
        public string type;
        public float remainingTime;
        public float startTime;
        public float value;
    }

    #endregion
}
