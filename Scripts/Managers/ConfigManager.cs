using System.Collections;
using UnityEngine;
using YallaCatch.API;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages dynamic game configuration from backend admin panel
    /// Allows real-time updates without app redeployment
    /// Syncs with backend /api/v1/admin/settings
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float refreshInterval = 300f; // Refresh every 5 minutes

        // Game Configuration (loaded from backend)
        public GameConfig Config { get; private set; }

        private bool isInitialized = false;

        public event System.Action OnConfigUpdated;

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
            StartCoroutine(LoadConfig());
            StartCoroutine(RefreshConfigCoroutine());
        }

        #endregion

        #region Load Configuration

        private IEnumerator LoadConfig()
        {
            Debug.Log("Loading game configuration from backend...");

            yield return StartCoroutine(APIManager.Instance.GetGameConfig((config) =>
            {
                if (config != null)
                {
                    Config = config;
                    isInitialized = true;
                    OnConfigUpdated?.Invoke();
                    
                    Debug.Log("Game configuration loaded successfully");
                    LogConfig();
                }
                else
                {
                    // Use default config if backend fails
                    Config = GetDefaultConfig();
                    isInitialized = true;
                    Debug.LogWarning("Using default configuration");
                }
            }));
        }

        private IEnumerator RefreshConfigCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshInterval);
                yield return StartCoroutine(LoadConfig());
            }
        }

        #endregion

        #region Default Configuration

        private GameConfig GetDefaultConfig()
        {
            return new GameConfig
            {
                // Capture Settings
                captureRadius = 50f,
                scanRadius = 500f,
                captureCooldown = 60,
                
                // Points & Rewards
                basePointsPerCapture = 10,
                pointsMultiplier = 1.0f,
                dailyBonusPoints = 50,
                
                // AdMob Settings
                rewardedAdPoints = 100,
                interstitialAdPoints = 20,
                maxRewardedAdsPerDay = 10,
                maxInterstitialAdsPerDay = 20,
                
                // Features
                arEnabled = true,
                offlineMode = true,
                notificationsEnabled = true,
                
                // Limits
                maxDailyClaims = 5,
                maxInventorySize = 100,
                
                // Maintenance
                maintenanceMode = false,
                maintenanceMessage = ""
            };
        }

        #endregion

        #region Configuration Access

        public float GetCaptureRadius()
        {
            return Config?.captureRadius ?? 50f;
        }

        public float GetScanRadius()
        {
            return Config?.scanRadius ?? 500f;
        }

        public int GetCaptureCooldown()
        {
            return Config?.captureCooldown ?? 60;
        }

        public int GetBasePointsPerCapture()
        {
            return Config?.basePointsPerCapture ?? 10;
        }

        public float GetPointsMultiplier()
        {
            return Config?.pointsMultiplier ?? 1.0f;
        }

        public int GetRewardedAdPoints()
        {
            return Config?.rewardedAdPoints ?? 100;
        }

        public int GetInterstitialAdPoints()
        {
            return Config?.interstitialAdPoints ?? 20;
        }

        public int GetMaxRewardedAdsPerDay()
        {
            return Config?.maxRewardedAdsPerDay ?? 10;
        }

        public int GetMaxInterstitialAdsPerDay()
        {
            return Config?.maxInterstitialAdsPerDay ?? 20;
        }

        public bool IsAREnabled()
        {
            return Config?.arEnabled ?? true;
        }

        public bool IsOfflineModeEnabled()
        {
            return Config?.offlineMode ?? true;
        }

        public bool AreNotificationsEnabled()
        {
            return Config?.notificationsEnabled ?? true;
        }

        public bool IsMaintenanceMode()
        {
            return Config?.maintenanceMode ?? false;
        }

        public string GetMaintenanceMessage()
        {
            return Config?.maintenanceMessage ?? "Game is under maintenance. Please try again later.";
        }

        #endregion

        #region Public Methods

        public void ForceRefresh()
        {
            StartCoroutine(LoadConfig());
        }

        public bool IsConfigLoaded()
        {
            return isInitialized && Config != null;
        }

        #endregion

        #region Debug

        private void LogConfig()
        {
            if (Config == null)
                return;

            Debug.Log("=== Game Configuration ===");
            Debug.Log($"Capture Radius: {Config.captureRadius}m");
            Debug.Log($"Scan Radius: {Config.scanRadius}m");
            Debug.Log($"Base Points: {Config.basePointsPerCapture}");
            Debug.Log($"Points Multiplier: {Config.pointsMultiplier}x");
            Debug.Log($"Rewarded Ad Points: {Config.rewardedAdPoints}");
            Debug.Log($"Max Rewarded Ads/Day: {Config.maxRewardedAdsPerDay}");
            Debug.Log($"AR Enabled: {Config.arEnabled}");
            Debug.Log($"Offline Mode: {Config.offlineMode}");
            Debug.Log($"Maintenance Mode: {Config.maintenanceMode}");
            Debug.Log("========================");
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class GameConfig
    {
        // Capture Settings
        public float captureRadius;
        public float scanRadius;
        public int captureCooldown;
        
        // Points & Rewards
        public int basePointsPerCapture;
        public float pointsMultiplier;
        public int dailyBonusPoints;
        
        // AdMob Settings
        public int rewardedAdPoints;
        public int interstitialAdPoints;
        public int maxRewardedAdsPerDay;
        public int maxInterstitialAdsPerDay;
        
        // Features
        public bool arEnabled;
        public bool offlineMode;
        public bool notificationsEnabled;
        
        // Limits
        public int maxDailyClaims;
        public int maxInventorySize;
        
        // Maintenance
        public bool maintenanceMode;
        public string maintenanceMessage;
    }

    #endregion
}
