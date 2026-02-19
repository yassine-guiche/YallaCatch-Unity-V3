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
            StartCoroutine(APIClient.Instance.Post<AdViewResponse>(APIEndpoints.ADMOB_REWARD, request, callback));
        }

        /// <summary>
        /// Claim ad reward
        /// </summary>
        public void ClaimReward(AdRewardRequest request, Action<ApiResponse<AdRewardResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<AdRewardResponse>(APIEndpoints.ADMOB_REWARD, request, callback));
        }

        #endregion

        #region Stats Methods

        /// <summary>
        /// Get user ad stats
        /// </summary>
        public void GetStats(Action<ApiResponse<AdStatsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<AdStatsResponse>(APIEndpoints.ADMOB_STATS, callback));
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
        public int pointsAwarded;
        public int totalDailyRewards;
        public int remainingDailyRewards;
        public string message;
    }

    [Serializable]
    public class AdStatsResponse
    {
        public int totalViews;
        public int totalRewardsClaimed;
        public int totalPointsEarned;
        public int dailyViewsToday;
        public int dailyRewardsToday;
        public int remainingDailyRewards;
    }

    #endregion
}
