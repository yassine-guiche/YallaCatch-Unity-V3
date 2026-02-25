using System;
using System.Collections;
using UnityEngine;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif
using YallaCatch.API;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages AdMob integration for rewarded videos and interstitial ads
    /// Syncs with backend for reward validation and anti-cheat
    /// </summary>
#if GOOGLE_MOBILE_ADS
    public class AdMobManager : MonoBehaviour
    {
        public static AdMobManager Instance { get; private set; }

        [Header("AdMob Configuration")]
        [SerializeField] private string androidAppId = "ca-app-pub-3940256099942544~3347511713"; // Test ID
        [SerializeField] private string iosAppId = "ca-app-pub-3940256099942544~1458002511"; // Test ID

        [Header("Rewarded Video")]
        [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Test ID
        [SerializeField] private string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313"; // Test ID

        [Header("Interstitial")]
        [SerializeField] private string androidInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // Test ID
        [SerializeField] private string iosInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910"; // Test ID

        private RewardedAd rewardedAd;
        private InterstitialAd interstitialAd;
        
        private bool isRewardedAdLoaded = false;
        private bool isInterstitialAdLoaded = false;

        public event Action<int> OnRewardedAdCompleted;
        public event Action OnInterstitialAdClosed;

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
            InitializeAdMob();
        }

        #endregion

        #region Initialization

        private void InitializeAdMob()
        {
            // Initialize the Google Mobile Ads SDK
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("AdMob initialized");
                LoadRewardedAd();
                LoadInterstitialAd();
            });
        }

        #endregion

        #region Rewarded Video

        private void LoadRewardedAd()
        {
            // Clean up old ad
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            string adUnitId = GetRewardedAdUnitId();

            var adRequest = new AdRequest.Builder().Build();

            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"Rewarded ad failed to load: {error}");
                    isRewardedAdLoaded = false;
                    return;
                }

                Debug.Log("Rewarded ad loaded");
                rewardedAd = ad;
                isRewardedAdLoaded = true;

                RegisterRewardedAdEvents(ad);
            });
        }

        private void RegisterRewardedAdEvents(RewardedAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"Rewarded ad paid {adValue.Value} {adValue.CurrencyCode}");
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Rewarded ad recorded an impression");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("Rewarded ad was clicked");
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Rewarded ad full screen content opened");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Rewarded ad full screen content closed");
                LoadRewardedAd(); // Preload next ad
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"Rewarded ad failed to open: {error}");
                LoadRewardedAd(); // Try to load again
            };
        }

        public void ShowRewardedAd(Action<bool, int> callback)
        {
            if (!isRewardedAdLoaded || rewardedAd == null)
            {
                Debug.LogWarning("Rewarded ad is not ready yet");
                callback?.Invoke(false, 0);
                return;
            }

            // First check with backend if ad is available
            StartCoroutine(APIManager.Instance.CheckAdAvailability("rewarded", (available, remaining) =>
            {
                if (!available)
                {
                    Debug.LogWarning($"Ad limit reached. Remaining today: {remaining}");
                    callback?.Invoke(false, 0);
                    return;
                }

                // Show the ad
                rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log($"Rewarded ad granted reward: {reward.Amount}");
                    
                    // Validate with backend and get points
                    string adUnitId = GetRewardedAdUnitId();
                    StartCoroutine(ValidateAdReward("rewarded", adUnitId, callback));
                });
            }));
        }

        private IEnumerator ValidateAdReward(string adType, string adUnitId, Action<bool, int> callback)
        {
            yield return StartCoroutine(APIManager.Instance.RewardAdWatched(adType, adUnitId, (success, points) =>
            {
                if (success)
                {
                    Debug.Log($"Ad reward validated: +{points} points");
                    OnRewardedAdCompleted?.Invoke(points);
                }
                else
                {
                    Debug.LogError("Failed to validate ad reward");
                }

                callback?.Invoke(success, points);
            }));
        }

        private string GetRewardedAdUnitId()
        {
            #if UNITY_ANDROID
                return androidRewardedAdUnitId;
            #elif UNITY_IOS
                return iosRewardedAdUnitId;
            #else
                return "unexpected_platform";
            #endif
        }

        #endregion

        #region Interstitial

        private void LoadInterstitialAd()
        {
            // Clean up old ad
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            string adUnitId = GetInterstitialAdUnitId();

            var adRequest = new AdRequest.Builder().Build();

            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"Interstitial ad failed to load: {error}");
                    isInterstitialAdLoaded = false;
                    return;
                }

                Debug.Log("Interstitial ad loaded");
                interstitialAd = ad;
                isInterstitialAdLoaded = true;

                RegisterInterstitialAdEvents(ad);
            });
        }

        private void RegisterInterstitialAdEvents(InterstitialAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"Interstitial ad paid {adValue.Value} {adValue.CurrencyCode}");
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Interstitial ad recorded an impression");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("Interstitial ad was clicked");
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Interstitial ad full screen content opened");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial ad full screen content closed");
                OnInterstitialAdClosed?.Invoke();
                LoadInterstitialAd(); // Preload next ad
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"Interstitial ad failed to open: {error}");
                LoadInterstitialAd(); // Try to load again
            };
        }

        public void ShowInterstitialAd(Action<bool> callback = null)
        {
            if (!isInterstitialAdLoaded || interstitialAd == null)
            {
                Debug.LogWarning("Interstitial ad is not ready yet");
                callback?.Invoke(false);
                return;
            }

            // Check with backend if ad is available
            StartCoroutine(APIManager.Instance.CheckAdAvailability("interstitial", (available, remaining) =>
            {
                if (!available)
                {
                    Debug.LogWarning($"Interstitial ad limit reached. Remaining today: {remaining}");
                    callback?.Invoke(false);
                    return;
                }

                // Show the ad
                interstitialAd.Show();
                
                // Validate with backend (smaller reward for interstitial)
                string adUnitId = GetInterstitialAdUnitId();
                StartCoroutine(ValidateInterstitialReward(adUnitId, callback));
            }));
        }

        private IEnumerator ValidateInterstitialReward(string adUnitId, Action<bool> callback)
        {
            yield return StartCoroutine(APIManager.Instance.RewardAdWatched("interstitial", adUnitId, (success, points) =>
            {
                if (success)
                {
                    Debug.Log($"Interstitial reward validated: +{points} points");
                }

                callback?.Invoke(success);
            }));
        }

        private string GetInterstitialAdUnitId()
        {
            #if UNITY_ANDROID
                return androidInterstitialAdUnitId;
            #elif UNITY_IOS
                return iosInterstitialAdUnitId;
            #else
                return "unexpected_platform";
            #endif
        }

        #endregion

        #region Public Methods

        public bool IsRewardedAdReady()
        {
            return isRewardedAdLoaded && rewardedAd != null;
        }

        public bool IsInterstitialAdReady()
        {
            return isInterstitialAdLoaded && interstitialAd != null;
        }

        #endregion
    }
#else
    /// <summary>
    /// Compile-safe AdMob fallback used when Google Mobile Ads plugin is not imported yet.
    /// </summary>
    public class AdMobManager : MonoBehaviour
    {
        public static AdMobManager Instance { get; private set; }

        public event Action<int> OnRewardedAdCompleted { add { } remove { } } // Suppress unused warning in fallback
        public event Action OnInterstitialAdClosed;

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
            Debug.LogWarning("[AdMobManager] Google Mobile Ads plugin not detected. AdMob is disabled until the plugin is imported.");
        }

        public void ShowRewardedAd(Action<bool, int> callback)
        {
            callback?.Invoke(false, 0);
        }

        public void ShowInterstitialAd(Action<bool> callback = null)
        {
            callback?.Invoke(false);
            OnInterstitialAdClosed?.Invoke();
        }

        public bool IsRewardedAdReady()
        {
            return false;
        }

        public bool IsInterstitialAdReady()
        {
            return false;
        }
    }
#endif
}
