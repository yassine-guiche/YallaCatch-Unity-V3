using System;
using System.Collections;
using UnityEngine;
using YallaCatch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YallaCatch.API
{
    /// <summary>
    /// Authentication API - Login, Register, Profile management
    /// </summary>
    public class AuthAPI : MonoBehaviour
    {
        public static AuthAPI Instance { get; private set; }

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

        #region Login Methods

        /// <summary>
        /// Email login
        /// </summary>
        public void Login(string email, string password, string deviceId, Action<ApiResponse<LoginResponse>> callback)
        {
            var body = new
            {
                email,
                password,
                deviceId,
                platform = APIClient.Instance.GetPlatform()
            };

            StartCoroutine(APIClient.Instance.Post<JObject>(APIEndpoints.AUTH_LOGIN, body, rawResponse =>
            {
                var response = NormalizeLoginResponse(rawResponse, "login");
                if (response.success && response.data != null)
                {
                    string accessToken = response.data.tokens?.GetAccessToken();
                    string refreshToken = response.data.tokens?.GetRefreshToken();
                    APIClient.Instance.SetTokens(
                        accessToken,
                        refreshToken,
                        response.data.user?.Id
                    );
                }
                callback?.Invoke(response);
            }));
        }

        /// <summary>
        /// Guest login (anonymous)
        /// </summary>
        public void GuestLogin(string deviceId, Action<ApiResponse<LoginResponse>> callback)
        {
            var body = new
            {
                deviceId,
                platform = APIClient.Instance.GetPlatform()
            };

            StartCoroutine(APIClient.Instance.Post<JObject>(APIEndpoints.AUTH_GUEST, body, rawResponse =>
            {
                var response = NormalizeLoginResponse(rawResponse, "guest");
                if (response.success && response.data != null)
                {
                    string accessToken = response.data.tokens?.GetAccessToken();
                    string refreshToken = response.data.tokens?.GetRefreshToken();
                    APIClient.Instance.SetTokens(
                        accessToken,
                        refreshToken,
                        response.data.user?.Id
                    );
                }
                callback?.Invoke(response);
            }));
        }

        /// <summary>
        /// Email registration
        /// </summary>
        public void Register(string email, string password, string displayName, string deviceId, Action<ApiResponse<LoginResponse>> callback)
        {
            var body = new
            {
                email,
                password,
                displayName,
                deviceId,
                platform = APIClient.Instance.GetPlatform()
            };

            StartCoroutine(APIClient.Instance.Post<JObject>(APIEndpoints.AUTH_REGISTER, body, rawResponse =>
            {
                var response = NormalizeLoginResponse(rawResponse, "register");
                if (response.success && response.data != null)
                {
                    string accessToken = response.data.tokens?.GetAccessToken();
                    string refreshToken = response.data.tokens?.GetRefreshToken();
                    APIClient.Instance.SetTokens(
                        accessToken,
                        refreshToken,
                        response.data.user?.Id
                    );
                }
                callback?.Invoke(response);
            }));
        }

        /// <summary>
        /// Logout
        /// </summary>
        public void Logout(Action<ApiResponse<object>> callback = null)
        {
            var body = new
            {
                deviceId = APIClient.Instance.GetDeviceId(),
                refreshToken = APIClient.Instance.GetRefreshToken()
            };

            StartCoroutine(APIClient.Instance.Post<object>(APIEndpoints.AUTH_LOGOUT, body, response =>
            {
                APIClient.Instance.ClearTokens();
                callback?.Invoke(response);
            }));
        }

        #endregion

        #region Response Normalization

        private ApiResponse<LoginResponse> NormalizeLoginResponse(ApiResponse<JObject> rawResponse, string operation)
        {
            if (rawResponse == null)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    error = "NULL_RESPONSE",
                    message = "No response from server"
                };
            }

            var normalized = new ApiResponse<LoginResponse>
            {
                success = rawResponse.success,
                error = rawResponse.error,
                message = rawResponse.message,
                timestamp = rawResponse.timestamp
            };

            if (!rawResponse.success || rawResponse.data == null)
            {
                return normalized;
            }

            JObject payload = rawResponse.data;
            if (payload["data"] is JObject nestedData && payload["user"] == null && payload["tokens"] == null)
            {
                payload = nestedData;
            }

            var result = new LoginResponse
            {
                sessionId = payload["sessionId"]?.ToString()
            };

            if (payload["tokens"] is JObject tokensObj)
            {
                result.tokens = new AuthTokens
                {
                    accessToken = tokensObj["accessToken"]?.ToString(),
                    refreshToken = tokensObj["refreshToken"]?.ToString(),
                    access = tokensObj["access"]?.ToString(),
                    refresh = tokensObj["refresh"]?.ToString()
                };
            }

            if (payload["user"] is JObject userObj)
            {
                // Start with normal deserialization, then patch critical fields explicitly.
                result.user = userObj.ToObject<User>() ?? new User();
                result.user._id = userObj["_id"]?.ToString() ?? result.user._id;
                result.user.authId = userObj["id"]?.ToString() ?? result.user.authId;
                result.user.displayName = userObj["displayName"]?.ToString() ?? userObj["username"]?.ToString() ?? userObj["name"]?.ToString() ?? result.user.displayName;
                result.user.role = userObj["role"]?.ToString() ?? result.user.role;
                result.user.level = userObj["level"]?.ToString() ?? result.user.level;

                if (userObj["points"] is JObject pointsObj)
                {
                    result.user.points ??= new UserPoints();
                    result.user.points.available = pointsObj["available"]?.Value<int?>() ?? result.user.points.available;
                    result.user.points.total = pointsObj["total"]?.Value<int?>() ?? result.user.points.total;
                    result.user.points.spent = pointsObj["spent"]?.Value<int?>() ?? result.user.points.spent;
                }
            }

            normalized.data = result;

            if (normalized.success)
            {
                bool missingTokens = string.IsNullOrEmpty(result.tokens?.GetAccessToken()) || string.IsNullOrEmpty(result.tokens?.GetRefreshToken());
                if (missingTokens)
                {
                    Debug.LogWarning($"[AuthAPI] {operation} response parsed with missing tokens. Raw payload: {payload}");
                }
            }

            return normalized;
        }

        #endregion

        #region Profile Methods

        /// <summary>
        /// Get current user profile (uses canonical /users/profile endpoint)
        /// </summary>
        public void GetProfile(Action<ApiResponse<User>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<User>(APIEndpoints.USER_PROFILE, callback));
        }

        /// <summary>
        /// Update user profile (uses PATCH /users/profile)
        /// </summary>
        public void UpdateProfile(UpdateProfileRequest request, Action<ApiResponse<User>> callback)
        {
            StartCoroutine(APIClient.Instance.Patch<User>(APIEndpoints.USER_PROFILE, request, callback));
        }

        #endregion
    }

    #region Auth Response Models

    [Serializable]
    public class LoginResponse
    {
        [JsonProperty("user")]
        public User user;
        [JsonProperty("tokens")]
        public AuthTokens tokens;
        [JsonProperty("sessionId")]
        public string sessionId;
    }

    [Serializable]
    public class UpdateProfileRequest
    {
        public string displayName;
        public string avatar;
        public UserSettings settings;
    }

    #endregion
}
