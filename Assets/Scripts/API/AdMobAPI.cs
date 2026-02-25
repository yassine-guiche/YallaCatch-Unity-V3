using System;
using System.Collections;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// AdMob API - Ad rewards and configuration
    /// </summary>
    public class AdMobAPI : MonoBehaviour
    {
        public static AdMobAPI Instance { get; private set; }

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

        #region Config Methods

        /// <summary>
        /// Get ad configuration
        /// </summary>
        public void GetConfig(Action<ApiResponse<AdMobConfigResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<AdMobConfigResponse>(APIEndpoints.ADMOB_CONFIG, callback));
        }

        #endregion

        #region Ad View Methods

        /// <summary>
        /// Record ad view and claim reward
        /// Backend has no separate /view endpoint - ad views are tracked via /reward
        /// The completed flag in the request body controls whether points are awarded
        /// </summary>
        public void RecordView(AdViewRequest request, Action<ApiResponse<AdViewResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<AdViewResponse>(APIEndpoints.ADMOB_REWARD, request, response =>
            {
                NormalizeAdViewResponse(response?.data);
                callback?.Invoke(response);
            }));
        }

        /// <summary>
        /// Claim ad reward
        /// </summary>
        public void ClaimReward(AdRewardRequest request, Action<ApiResponse<AdRewardResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<AdRewardResponse>(APIEndpoints.ADMOB_REWARD, request, response =>
            {
                NormalizeAdRewardResponse(response?.data);
                callback?.Invoke(response);
            }));
        }

        #endregion

        #region Stats Methods

        /// <summary>
        /// Get user ad stats
        /// </summary>
        public void GetStats(Action<ApiResponse<AdStatsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<AdStatsResponse>(APIEndpoints.ADMOB_STATS, response =>
            {
                NormalizeAdStatsResponse(response?.data);
                callback?.Invoke(response);
            }));
        }

        #endregion

        #region Normalizers

        private void NormalizeAdViewResponse(AdViewResponse data)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.viewId) && !string.IsNullOrEmpty(data.adViewId))
                data.viewId = data.adViewId;
        }

        private void NormalizeAdRewardResponse(AdRewardResponse data)
        {
            if (data == null) return;
            if (data.pointsAwarded <= 0 && data.rewardAmount > 0)
                data.pointsAwarded = data.rewardAmount;
        }

        private void NormalizeAdStatsResponse(AdStatsResponse data)
        {
            if (data == null) return;

            if (data.allTime != null)
            {
                if (data.totalViews <= 0) data.totalViews = data.allTime.totalViews;
                if (data.totalRewardsClaimed <= 0) data.totalRewardsClaimed = data.allTime.totalCompleted;
                if (data.totalPointsEarned <= 0) data.totalPointsEarned = data.allTime.totalRewards;
            }

            if (data.today != null)
            {
                int totalTodayViews = 0;
                int totalTodayRewards = 0;
                foreach (var row in data.today)
                {
                    if (row == null) continue;
                    totalTodayViews += row.count;
                    totalTodayRewards += row.totalReward;
                }

                if (data.dailyViewsToday <= 0) data.dailyViewsToday = totalTodayViews;
                if (data.dailyRewardsToday <= 0) data.dailyRewardsToday = totalTodayRewards;
            }
        }

        #endregion
    }

    #region AdMob Request/Response Models

    [Serializable]
    public class AdMobConfigResponse
    {
        public AdUnitConfig rewardedAd;
        public AdUnitConfig interstitialAd;
        public AdUnitConfig bannerAd;
        public int dailyRewardLimit;
        public int rewardPerAd;
    }

    [Serializable]
    public class AdUnitConfig
    {
        public string unitId;
        public bool enabled;
        public int cooldownSeconds;
    }

    [Serializable]
    public class AdViewRequest
    {
        public string adType; // rewarded, interstitial, banner
        public string adUnitId;
        public bool completed;
        public AdDeviceInfo deviceInfo;
        public AdLocation location;
        public AdMetadata metadata;
    }

    [Serializable]
    public class AdDeviceInfo
    {
        public string platform;
        public string version;
        public string model;
    }

    [Serializable]
    public class AdLocation
    {
        public string city;
        public string country;
    }

    [Serializable]
    public class AdMetadata
    {
        public string sessionId;
        public string placementId;
    }

    [Serializable]
    public class AdViewResponse
    {
        public bool success;
        public string viewId;
        public string adViewId; // Backend field from /admob/reward
        public int rewardAmount;
        public string rewardType;
        public int cooldownSeconds;
        public string message;
    }

    [Serializable]
    public class AdRewardRequest
    {
        public string adType;
        public string adUnitId;
        public bool completed;
        public string viewId;
    }

    [Serializable]
    public class AdRewardResponse
    {
        public bool success;
        public int rewardAmount; // Backend field
        public string rewardType;
        public object newBalance;
        public string adViewId;
        public int cooldownSeconds;
        public int pointsAwarded;
        public int totalDailyRewards;
        public int remainingDailyRewards;
        public string message;

        public int GetAwardedPoints()
        {
            if (pointsAwarded > 0) return pointsAwarded;
            return rewardAmount;
        }
    }

    [Serializable]
    public class AdStatsResponse
    {
        public AdStatsTodayEntry[] today;   // Backend nested shape
        public AdStatsAllTime allTime;      // Backend nested shape
        public AdStatsLast7Day[] last7Days; // Backend nested shape
        public int totalViews;
        public int totalRewardsClaimed;
        public int totalPointsEarned;
        public int dailyViewsToday;
        public int dailyRewardsToday;
        public int remainingDailyRewards;

        public int GetRemainingDailyRewards(int dailyLimit)
        {
            if (remainingDailyRewards > 0) return remainingDailyRewards;

            int rewardedCompletedToday = 0;
            if (today != null)
            {
                foreach (var row in today)
                {
                    if (row == null) continue;
                    if (string.Equals(row._id, "rewarded", StringComparison.OrdinalIgnoreCase))
                    {
                        rewardedCompletedToday = row.completed > 0 ? row.completed : row.count;
                        break;
                    }
                }
            }

            return Mathf.Max(0, dailyLimit - rewardedCompletedToday);
        }
    }

    [Serializable]
    public class AdStatsTodayEntry
    {
        public string _id;
        public int count;
        public int completed;
        public int totalReward;
    }

    [Serializable]
    public class AdStatsAllTime
    {
        public int totalViews;
        public int totalCompleted;
        public int totalRewards;
    }

    [Serializable]
    public class AdStatsLast7Day
    {
        public string _id;
        public int count;
        public int rewards;
    }

    #endregion
}
