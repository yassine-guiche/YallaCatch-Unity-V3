using System;
using System.Collections;
using UnityEngine;
using YallaCatch.Models;

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

            StartCoroutine(APIClient.Instance.Post<LoginResponse>(APIEndpoints.AUTH_LOGIN, body, response =>
            {
                if (response.success && response.data != null)
                {
                    APIClient.Instance.SetTokens(
                        response.data.tokens.accessToken,
                        response.data.tokens.refreshToken,
                        response.data.user._id
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

            StartCoroutine(APIClient.Instance.Post<LoginResponse>(APIEndpoints.AUTH_GUEST, body, response =>
            {
                if (response.success && response.data != null)
                {
                    APIClient.Instance.SetTokens(
                        response.data.tokens.accessToken,
                        response.data.tokens.refreshToken,
                        response.data.user._id
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

            StartCoroutine(APIClient.Instance.Post<LoginResponse>(APIEndpoints.AUTH_REGISTER, body, response =>
            {
                if (response.success && response.data != null)
                {
                    APIClient.Instance.SetTokens(
                        response.data.tokens.accessToken,
                        response.data.tokens.refreshToken,
                        response.data.user._id
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
            var body = new { deviceId = APIClient.Instance.GetDeviceId() };

            StartCoroutine(APIClient.Instance.Post<object>(APIEndpoints.AUTH_LOGOUT, body, response =>
            {
                APIClient.Instance.ClearTokens();
                callback?.Invoke(response);
            }));
        }

        #endregion

        #region Profile Methods

        /// <summary>
        /// Get current user profile
        /// </summary>
        public void GetProfile(Action<ApiResponse<User>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<User>(APIEndpoints.AUTH_ME, callback));
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        public void UpdateProfile(UpdateProfileRequest request, Action<ApiResponse<User>> callback)
        {
            StartCoroutine(APIClient.Instance.Put<User>(APIEndpoints.AUTH_ME, request, callback));
        }

        #endregion
    }

    #region Auth Response Models

    [Serializable]
    public class LoginResponse
    {
        public User user;
        public AuthTokens tokens;
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
