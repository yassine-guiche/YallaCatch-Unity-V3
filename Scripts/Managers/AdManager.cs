using UnityEngine;
using YallaCatch.API;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages AdMob integration and ad rewards
    /// Uses AdMobAPI
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("Ad Configuration")]
        public AdMobConfigResponse Config { get; private set; }
        public AdStatsResponse Stats { get; private set; }

        // Events
        public event System.Action<int> OnAdRewardClaimed;
        public event System.Action OnConfigLoaded;

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
            LoadConfig();
        }

        #region Configuration

        public void LoadConfig(System.Action<bool> callback = null)
        {
            AdMobAPI.Instance.GetConfig(response =>
            {
                if (response.success && response.data != null)
                {
                    Config = response.data;
                    OnConfigLoaded?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        #endregion

        #region Ad Views

        public void RecordAdView(string adType, string adUnitId, bool completed, System.Action<bool> callback = null)
        {
            var request = new AdViewRequest
            {
                adType = adType,
                adUnitId = adUnitId,
                completed = completed,
                deviceInfo = new AdDeviceInfo
                {
                    platform = APIClient.Instance.GetPlatform(),
                    version = Application.version,
                    model = SystemInfo.deviceModel
                }
            };

            AdMobAPI.Instance.RecordView(request, response =>
            {
                callback?.Invoke(response.success);
            });
        }

        public void ClaimAdReward(string adType, string adUnitId, string viewId, System.Action<bool, int> callback)
        {
            var request = new AdRewardRequest
            {
                adType = adType,
                adUnitId = adUnitId,
                completed = true,
                viewId = viewId
            };

            AdMobAPI.Instance.ClaimReward(request, response =>
            {
                if (response.success && response.data != null)
                {
                    int points = response.data.pointsAwarded;
                    GameManager.Instance.AddPoints(points);
                    OnAdRewardClaimed?.Invoke(points);
                    callback?.Invoke(true, points);
                }
                else
                {
                    callback?.Invoke(false, 0);
                }
            });
        }

        #endregion

        #region Stats

        public void LoadStats(System.Action<bool> callback = null)
        {
            AdMobAPI.Instance.GetStats(response =>
            {
                if (response.success && response.data != null)
                {
                    Stats = response.data;
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public int GetRemainingDailyRewards()
        {
            return Stats?.remainingDailyRewards ?? Config?.dailyRewardLimit ?? 5;
        }

        public bool CanWatchRewardedAd()
        {
            return GetRemainingDailyRewards() > 0;
        }

        #endregion

        #region Convenience Methods

        public void WatchRewardedAd(System.Action<bool, int> callback)
        {
            if (!CanWatchRewardedAd())
            {
                callback?.Invoke(false, 0);
                return;
            }

            string adUnitId = Config?.rewardedAd?.unitId ?? "default_rewarded";

            // Step 1: Record view start
            RecordAdView("rewarded", adUnitId, false, viewSuccess =>
            {
                if (!viewSuccess)
                {
                    callback?.Invoke(false, 0);
                    return;
                }

                // Step 2: Show actual ad (integrate with AdMob SDK here)
                // For now, simulating ad completion
                SimulateAdWatch(adUnitId, callback);
            });
        }

        private void SimulateAdWatch(string adUnitId, System.Action<bool, int> callback)
        {
            // In real implementation, this would be called by AdMob callback
            // after user watches the full ad

            // Record completion and claim reward
            RecordAdView("rewarded", adUnitId, true, _ =>
            {
                ClaimAdReward("rewarded", adUnitId, null, callback);
            });
        }

        #endregion
    }
}
