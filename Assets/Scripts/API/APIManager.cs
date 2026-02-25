using UnityEngine;
using System;
using System.Collections;
using YallaCatch.Models;
using YallaCatch.Managers;
using Newtonsoft.Json.Linq;

namespace YallaCatch.API
{
    /// <summary>
    /// Legacy API Wrapper (Strangler Fig Pattern)
    /// Bridges old Managers (MarketplaceManager, ClaimsManager, etc.) to the new APIClient HTTP core.
    /// Prevents 1000+ broken compilation errors while safely allowing future modular refactoring.
    /// </summary>
    public class APIManager : MonoBehaviour
    {
        public static APIManager Instance { get; private set; }

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

        public string GetBaseURL()
        {
            if (APIClient.Instance != null)
            {
                string url = APIClient.Instance.BaseUrl ?? "http://localhost:3000";
                return url.EndsWith("/api/v1") ? url.Substring(0, url.Length - "/api/v1".Length) : url;
            }
            return "http://localhost:3000";
        }

        public bool IsAuthenticated()
        {
            return APIClient.Instance != null && APIClient.Instance.IsAuthenticated;
        }

        #region Core Generic HTTP Bridge

        public IEnumerator Get(string endpoint, Action<ApiResponse<object>> callback)
        {
            yield return APIClient.Instance.Get<object>(NormalizeEndpoint(endpoint), callback);
        }

        public IEnumerator Post(string endpoint, object body, Action<ApiResponse<object>> callback)
        {
            yield return APIClient.Instance.Post<object>(NormalizeEndpoint(endpoint), body, callback);
        }

        public IEnumerator Put(string endpoint, object body, Action<ApiResponse<object>> callback)
        {
            yield return APIClient.Instance.Put<object>(NormalizeEndpoint(endpoint), body, callback);
        }

        public IEnumerator Delete(string endpoint, Action<ApiResponse<object>> callback)
        {
            yield return APIClient.Instance.Delete<object>(NormalizeEndpoint(endpoint), callback);
        }

        #endregion

        #region Specialized Legacy Endpoints

        public IEnumerator SyncOfflineQueue(string jsonData, Action<bool, int, YallaCatch.Managers.OfflineAction[]> callback)
        {
            object payload = jsonData;
            try
            {
                payload = JObject.Parse(jsonData);
            }
            catch
            {
                // Keep legacy behavior if payload is not valid JSON
            }

            yield return APIClient.Instance.Post<object>(APIEndpoints.OFFLINE_SYNC, payload, (response) =>
            {
                if (!response.success || response.data == null)
                {
                    callback?.Invoke(false, 0, System.Array.Empty<YallaCatch.Managers.OfflineAction>());
                    return;
                }

                try
                {
                    var data = response.data as JObject ?? JObject.FromObject(response.data);
                    int syncedCount = (data["synced"] as JArray)?.Count ?? 0;
                    callback?.Invoke(true, syncedCount, System.Array.Empty<YallaCatch.Managers.OfflineAction>());
                }
                catch
                {
                    callback?.Invoke(true, 0, System.Array.Empty<YallaCatch.Managers.OfflineAction>());
                }
            });
        }

        public IEnumerator CheckAdAvailability(string adType, Action<bool, int> callback)
        {
            yield return APIClient.Instance.Get<object>(APIEndpoints.ADMOB_CONFIG, (response) =>
            {
                if (!response.success || response.data == null)
                {
                    callback?.Invoke(false, 0);
                    return;
                }

                try
                {
                    var data = response.data as JObject ?? JObject.FromObject(response.data);
                    var availability = data[adType];
                    bool available = availability?["available"]?.Value<bool>() ?? false;
                    int remaining = availability?["remaining"]?.Value<int>() ?? 0;
                    callback?.Invoke(available, remaining);
                }
                catch
                {
                    callback?.Invoke(false, 0);
                }
            });
        }

        public IEnumerator RewardAdWatched(string adType, string adUnitId, Action<bool, int> callback)
        {
            var body = new { adType = adType, adUnitId = adUnitId, completed = true };
            yield return APIClient.Instance.Post<object>(APIEndpoints.ADMOB_REWARD, body, (response) =>
            {
                if (!response.success || response.data == null)
                {
                    callback?.Invoke(false, 0);
                    return;
                }

                try
                {
                    var data = response.data as JObject ?? JObject.FromObject(response.data);
                    int points = data["rewardAmount"]?.Value<int>() ?? 0;
                    callback?.Invoke(true, points);
                }
                catch
                {
                    callback?.Invoke(false, 0);
                }
            });
        }

        public IEnumerator GetGameConfig(Action<GameConfig> callback)
        {
            yield return APIClient.Instance.Get<GameConfig>(APIEndpoints.GAME_CONFIG, (response) =>
            {
                callback?.Invoke(response.success ? response.data : null);
            });
        }

        public IEnumerator GetMyProfile(Action<object> callback)
        {
            yield return APIClient.Instance.Get<object>(APIEndpoints.USER_PROFILE, (response) =>
            {
                callback?.Invoke(response.success ? response.data : null);
            });
        }

        private string NormalizeEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return endpoint;

            endpoint = endpoint.Trim();

            if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }

            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }

            if (endpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }

            return "/api/v1" + endpoint;
        }

        #endregion
    }
}
