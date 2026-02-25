using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private readonly HashSet<string> unauthSkippedRequestKeys = new HashSet<string>();

        // Device info
        private string deviceId;
        private string platform;

        // Events
        public event Action OnTokenExpired;
        public event Action OnAccessDenied;
        public event Action OnLoggedOut;
        public event Action OnTokensUpdated;
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

            // Avoid startup request storms with stale JWTs cached in PlayerPrefs.
            if (!string.IsNullOrEmpty(accessToken) && IsTokenExpired(accessToken))
            {
                Debug.LogWarning("[APIClient] Stored access token is expired. Clearing local session.");
                ClearTokens();
            }

            // Generate device info
            deviceId = SystemInfo.deviceUniqueIdentifier;
            platform = GetPlatformString();

            Debug.Log($"[APIClient] Initialized - Has token: {!string.IsNullOrEmpty(accessToken)}");
        }

        #endregion

        #region Public Properties

        public bool IsAuthenticated => !string.IsNullOrEmpty(accessToken) && !IsTokenExpired(accessToken);
        public string UserId => userId;
        public string BaseUrl => baseUrl;

        public string GetFullImageUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.StartsWith("http") || path.StartsWith("data:")) return path;

            string normalizedPath = path.StartsWith("/") ? path : "/" + path;
            return baseUrl + normalizedPath;
        }

        public void SetBaseUrl(string url)
        {
            baseUrl = url.TrimEnd('/');
            Debug.Log($"[APIClient] Base URL updated to: {baseUrl}");
        }

        #endregion

        #region Token Management

        public void SetTokens(string access, string refresh, string uid)
        {
            accessToken = access ?? "";
            refreshToken = refresh ?? "";
            userId = uid ?? "";

            PlayerPrefs.SetString(ACCESS_TOKEN_KEY, accessToken);
            PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refreshToken);
            PlayerPrefs.SetString(USER_ID_KEY, userId);
            PlayerPrefs.Save();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                Debug.LogWarning($"[APIClient] Tokens saved but one or more token values are empty. accessLen={accessToken.Length}, refreshLen={refreshToken.Length}, userId='{userId}'");
            }
            else
            {
                Debug.Log($"[APIClient] Tokens saved (accessLen={accessToken.Length}, refreshLen={refreshToken.Length}, userId='{userId}')");
            }

            OnTokensUpdated?.Invoke();
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

        /// <summary>
        /// DELETE request with JSON body
        /// </summary>
        public IEnumerator Delete<T>(string endpoint, object body, Action<ApiResponse<T>> callback) where T : class
        {
            yield return SendRequest<T>(endpoint, "DELETE", body, callback);
        }

        #endregion

        #region Core Request Handler

        private IEnumerator SendRequest<T>(string endpoint, string method, object body, Action<ApiResponse<T>> callback, bool isRetry = false) where T : class
        {
            if (RequiresAuthentication(endpoint) && !IsAuthenticated)
            {
                WarnSkippedUnauthenticatedRequest(method, endpoint);
                callback?.Invoke(new ApiResponse<T>
                {
                    success = false,
                    error = "AUTH_REQUIRED",
                    message = "Authentication required. Please login first."
                });
                yield break;
            }

            string url = baseUrl + endpoint;
            UnityWebRequest request;

            // Create request based on method
            switch (method.ToUpper())
            {
                case "GET":
                    request = UnityWebRequest.Get(url);
                    break;
                case "DELETE":
                case "POST":
                case "PUT":
                case "PATCH":
                    string jsonBody = body != null ? JsonConvert.SerializeObject(body) : "{}";
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request = new UnityWebRequest(url, method);
                    if (body != null || method != "GET")
                    {
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        request.SetRequestHeader("Content-Type", "application/json");
                    }
                    request.downloadHandler = new DownloadHandlerBuffer();
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
                    var response = DeserializeApiResponse<T>(responseText);
                    callback?.Invoke(response);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIClient] Parse error: {ex.Message}");
                    callback?.Invoke(new ApiResponse<T> { success = false, error = "PARSE_ERROR", message = ex.Message });
                }
            }
            else if (request.responseCode == 401)
            {
                if (!isRetry && !string.IsNullOrEmpty(refreshToken))
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
                    bool hadCredentials = !string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(refreshToken);
                    if (hadCredentials)
                    {
                        Debug.LogWarning($"[APIClient] Unauthorized ({method} {endpoint}). Clearing local session.");
                        ClearTokens();
                    }

                    OnTokenExpired?.Invoke();
                    callback?.Invoke(new ApiResponse<T>
                    {
                        success = false,
                        error = "UNAUTHORIZED",
                        message = "Authentication required. Please login again."
                    });
                }
            }
            else if (request.responseCode == 403)
            {
                // Access Denied / Banned
                Debug.LogWarning("[APIClient] Access denied (403). Possible ban.");
                OnAccessDenied?.Invoke();
                callback?.Invoke(new ApiResponse<T> { success = false, error = "ACCESS_DENIED", message = "Your account has been restricted or banned." });
            }
            else
            {
                string errorMessage = $"HTTP {request.responseCode}: {request.error}";
                Debug.LogError($"[APIClient] Request failed ({method} {endpoint}): {errorMessage}");
                if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                {
                    Debug.LogWarning($"[APIClient] Error response body ({method} {endpoint}): {request.downloadHandler.text}");
                }
                
                // Try to parse error response
                try
                {
                    if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                    {
                        var errorResponse = DeserializeApiResponse<T>(request.downloadHandler.text);
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

                OnRequestError?.Invoke($"{method} {endpoint} -> {errorMessage}");
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
            if (isRefreshing)
            {
                while (isRefreshing)
                    yield return null;
                yield break;
            }
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
                        var response = DeserializeApiResponse<AuthRefreshResponse>(request.downloadHandler.text);
                        if (response.success && response.data?.tokens != null)
                        {
                            SetTokens(response.data.tokens.GetAccessToken(), response.data.tokens.GetRefreshToken(), userId);
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
            return "Web";
            #endif
        }

        public string GetDeviceId() => deviceId;
        public string GetPlatform() => platform;
        public string GetAccessToken() => accessToken;
        public string GetRefreshToken() => refreshToken;

        private bool RequiresAuthentication(string endpoint)
        {
            string normalized = NormalizeEndpointPathForAuthCheck(endpoint);
            if (string.IsNullOrEmpty(normalized))
                return false;

            if (normalized.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                return false;

            if (normalized.StartsWith("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("/api/v1/auth/register", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("/api/v1/auth/guest", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private string NormalizeEndpointPathForAuthCheck(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return string.Empty;

            string value = endpoint.Trim();
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri absoluteUri))
            {
                return absoluteUri.AbsolutePath;
            }

            return value.StartsWith("/") ? value : "/" + value;
        }

        private void WarnSkippedUnauthenticatedRequest(string method, string endpoint)
        {
            string key = $"{method.ToUpperInvariant()} {NormalizeEndpointPathForAuthCheck(endpoint)}";
            if (unauthSkippedRequestKeys.Add(key))
            {
                Debug.LogWarning($"[APIClient] Skipping unauthenticated request: {key}");
            }
        }

        private bool IsTokenExpired(string token, int skewSeconds = 15)
        {
            if (string.IsNullOrWhiteSpace(token))
                return true;

            string[] parts = token.Split('.');
            if (parts.Length < 2)
            {
                // Non-JWT token format: cannot inspect expiry locally, assume usable.
                return false;
            }

            try
            {
                string payloadJson = Base64UrlDecode(parts[1]);
                JObject payload = JObject.Parse(payloadJson);
                JToken expToken = payload["exp"];
                if (expToken == null)
                    return false;

                long exp = expToken.Value<long>();
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return exp <= (now + skewSeconds);
            }
            catch
            {
                // If parsing fails, avoid falsely locking out login flows.
                return false;
            }
        }

        private ApiResponse<T> DeserializeApiResponse<T>(string responseText) where T : class
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return new ApiResponse<T> { success = false, error = "EMPTY_RESPONSE", message = "Empty response body" };
            }

            // Normalize Mongo/JSON ID shapes before deserializing typed models.
            JToken root = JToken.Parse(responseText);
            root = NormalizeJsonIds(root);
            return root.ToObject<ApiResponse<T>>();
        }

        private JToken NormalizeJsonIds(JToken token)
        {
            if (token == null)
            {
                return token;
            }

            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;

                // Mongo extended JSON ObjectId => string
                if (obj.Count == 1 && obj.TryGetValue("$oid", out JToken oidToken) && oidToken.Type == JTokenType.String)
                {
                    return new JValue(oidToken.Value<string>());
                }

                var properties = new List<JProperty>(obj.Properties());
                foreach (JProperty property in properties)
                {
                    property.Value = NormalizeJsonIds(property.Value);
                }

                // Add `id` alias for `_id` to align Unity/admin/partner contracts
                if (obj.Property("id") == null && obj.TryGetValue("_id", out JToken idToken))
                {
                    if (idToken.Type == JTokenType.String || idToken.Type == JTokenType.Integer)
                    {
                        obj["id"] = idToken.Type == JTokenType.String
                            ? new JValue(idToken.Value<string>())
                            : new JValue(idToken.ToString());
                    }
                }

                return obj;
            }

            if (token.Type == JTokenType.Array)
            {
                JArray array = (JArray)token;
                for (int i = 0; i < array.Count; i++)
                {
                    array[i] = NormalizeJsonIds(array[i]);
                }
                return array;
            }

            return token;
        }

        private string Base64UrlDecode(string input)
        {
            string padded = input.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(padded);
            return Encoding.UTF8.GetString(bytes);
        }

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
        [JsonProperty("accessToken")]
        public string accessToken;
        [JsonProperty("refreshToken")]
        public string refreshToken;

        // Defensive aliases for alternate payloads used by some environments.
        [JsonProperty("access")]
        public string access;
        [JsonProperty("refresh")]
        public string refresh;

        public string GetAccessToken() => !string.IsNullOrEmpty(accessToken) ? accessToken : access;
        public string GetRefreshToken() => !string.IsNullOrEmpty(refreshToken) ? refreshToken : refresh;
    }

    [Serializable]
    public class AuthRefreshResponse
    {
        public AuthTokens tokens;
    }

    #endregion
}

