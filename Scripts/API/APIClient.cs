using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace YallaCatch.API
{
    /// <summary>
    /// Base HTTP client for all API calls.
    /// Handles JWT authentication, token refresh, and request serialization.
    /// </summary>
    public class APIClient : MonoBehaviour
    {
        public static APIClient Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private string baseUrl = "http://localhost:3000";
        [SerializeField] private float timeout = 30f;

        // Token storage keys
        private const string ACCESS_TOKEN_KEY = "yallacatch_access_token";
        private const string REFRESH_TOKEN_KEY = "yallacatch_refresh_token";
        private const string USER_ID_KEY = "yallacatch_user_id";

        // Current tokens
        private string accessToken;
        private string refreshToken;
        private string userId;
        private bool isRefreshing = false;

        // Device info
        private string deviceId;
        private string platform;

        // Events
        public event Action OnTokenExpired;
        public event Action OnLoggedOut;
        public event Action<string> OnRequestError;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Load stored tokens
            accessToken = PlayerPrefs.GetString(ACCESS_TOKEN_KEY, "");
            refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
            userId = PlayerPrefs.GetString(USER_ID_KEY, "");

            // Generate device info
            deviceId = SystemInfo.deviceUniqueIdentifier;
            platform = GetPlatformString();

            Debug.Log($"[APIClient] Initialized - Has token: {!string.IsNullOrEmpty(accessToken)}");
        }

        #endregion

        #region Public Properties

        public bool IsAuthenticated => !string.IsNullOrEmpty(accessToken);
        public string UserId => userId;
        public string BaseUrl => baseUrl;

        #endregion

        #region Token Management

        public void SetTokens(string access, string refresh, string uid)
        {
            accessToken = access;
            refreshToken = refresh;
            userId = uid;

            PlayerPrefs.SetString(ACCESS_TOKEN_KEY, access);
            PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refresh);
            PlayerPrefs.SetString(USER_ID_KEY, uid);
            PlayerPrefs.Save();

            Debug.Log("[APIClient] Tokens saved");
        }

        public void ClearTokens()
        {
            accessToken = "";
            refreshToken = "";
            userId = "";

            PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
            PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
            PlayerPrefs.DeleteKey(USER_ID_KEY);
            PlayerPrefs.Save();

            OnLoggedOut?.Invoke();
            Debug.Log("[APIClient] Tokens cleared");
        }

        #endregion

        #region HTTP Methods

        /// <summary>
        /// GET request
        /// </summary>
        public IEnumerator Get<T>(string endpoint, Action<ApiResponse<T>> callback) where T : class
        {
            yield return SendRequest<T>(endpoint, "GET", null, callback);
        }

        /// <summary>
        /// POST request with JSON body
        /// </summary>
        public IEnumerator Post<T>(string endpoint, object body, Action<ApiResponse<T>> callback) where T : class
        {
            yield return SendRequest<T>(endpoint, "POST", body, callback);
        }

        /// <summary>
        /// PUT request with JSON body
        /// </summary>
        public IEnumerator Put<T>(string endpoint, object body, Action<ApiResponse<T>> callback) where T : class
        {
            yield return SendRequest<T>(endpoint, "PUT", body, callback);
        }

        /// <summary>
        /// PATCH request with JSON body
        /// </summary>
        public IEnumerator Patch<T>(string endpoint, object body, Action<ApiResponse<T>> callback) where T : class
        {
            yield return SendRequest<T>(endpoint, "PATCH", body, callback);
        }

        /// <summary>
        /// DELETE request
        /// </summary>
        public IEnumerator Delete<T>(string endpoint, Action<ApiResponse<T>> callback) where T : class
        {
            yield return SendRequest<T>(endpoint, "DELETE", null, callback);
        }

        #endregion

        #region Core Request Handler

        private IEnumerator SendRequest<T>(string endpoint, string method, object body, Action<ApiResponse<T>> callback, bool isRetry = false) where T : class
        {
            string url = baseUrl + endpoint;
            UnityWebRequest request;

            // Create request based on method
            switch (method.ToUpper())
            {
                case "GET":
                    request = UnityWebRequest.Get(url);
                    break;
                case "DELETE":
                    request = UnityWebRequest.Delete(url);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case "POST":
                case "PUT":
                case "PATCH":
                    string jsonBody = body != null ? JsonConvert.SerializeObject(body) : "{}";
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request = new UnityWebRequest(url, method);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    break;
                default:
                    callback?.Invoke(new ApiResponse<T> { success = false, error = "INVALID_METHOD" });
                    yield break;
            }

            // Set headers
            SetRequestHeaders(request);
            request.timeout = (int)timeout;

            // Send request
            yield return request.SendWebRequest();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonConvert.DeserializeObject<ApiResponse<T>>(responseText);
                    callback?.Invoke(response);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIClient] Parse error: {ex.Message}");
                    callback?.Invoke(new ApiResponse<T> { success = false, error = "PARSE_ERROR", message = ex.Message });
                }
            }
            else if (request.responseCode == 401 && !isRetry && !string.IsNullOrEmpty(refreshToken))
            {
                // Token expired - try to refresh
                Debug.Log("[APIClient] Token expired, attempting refresh...");
                yield return TryRefreshToken();

                if (IsAuthenticated)
                {
                    // Retry the original request
                    yield return SendRequest<T>(endpoint, method, body, callback, true);
                }
                else
                {
                    OnTokenExpired?.Invoke();
                    callback?.Invoke(new ApiResponse<T> { success = false, error = "TOKEN_EXPIRED", message = "Please login again" });
                }
            }
            else
            {
                string errorMessage = $"HTTP {request.responseCode}: {request.error}";
                Debug.LogError($"[APIClient] Request failed: {errorMessage}");
                
                // Try to parse error response
                try
                {
                    if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(request.downloadHandler.text);
                        callback?.Invoke(errorResponse);
                    }
                    else
                    {
                        callback?.Invoke(new ApiResponse<T> { success = false, error = "NETWORK_ERROR", message = errorMessage });
                    }
                }
                catch
                {
                    callback?.Invoke(new ApiResponse<T> { success = false, error = "NETWORK_ERROR", message = errorMessage });
                }

                OnRequestError?.Invoke(errorMessage);
            }

            request.Dispose();
        }

        private void SetRequestHeaders(UnityWebRequest request)
        {
            // Authorization
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            }

            // Device info
            request.SetRequestHeader("X-Device-Id", deviceId);
            request.SetRequestHeader("X-Platform", platform);

            // Common headers
            request.SetRequestHeader("Accept", "application/json");
        }

        private IEnumerator TryRefreshToken()
        {
            if (isRefreshing) yield break;
            isRefreshing = true;

            string url = baseUrl + APIEndpoints.AUTH_REFRESH;
            var body = new { refreshToken = this.refreshToken };
            string jsonBody = JsonConvert.SerializeObject(body);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonConvert.DeserializeObject<ApiResponse<AuthTokens>>(request.downloadHandler.text);
                        if (response.success && response.data != null)
                        {
                            SetTokens(response.data.accessToken, response.data.refreshToken, userId);
                            Debug.Log("[APIClient] Token refreshed successfully");
                        }
                        else
                        {
                            ClearTokens();
                        }
                    }
                    catch
                    {
                        ClearTokens();
                    }
                }
                else
                {
                    ClearTokens();
                }
            }

            isRefreshing = false;
        }

        #endregion

        #region Utility

        private string GetPlatformString()
        {
            #if UNITY_IOS
            return "iOS";
            #elif UNITY_ANDROID
            return "Android";
            #else
            return "Unity";
            #endif
        }

        public string GetDeviceId() => deviceId;
        public string GetPlatform() => platform;

        #endregion
    }

    #region Response Models

    /// <summary>
    /// Standard API response wrapper matching backend format
    /// </summary>
    [Serializable]
    public class ApiResponse<T> where T : class
    {
        public bool success;
        public T data;
        public string error;
        public string message;
        public string timestamp;
    }

    /// <summary>
    /// Auth tokens from login/refresh
    /// </summary>
    [Serializable]
    public class AuthTokens
    {
        public string accessToken;
        public string refreshToken;
    }

    #endregion
}
