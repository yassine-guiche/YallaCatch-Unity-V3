using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YallaCatch.API;
using YallaCatch.Models;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages user authentication (login, register, logout)
    /// Uses AuthAPI for all backend calls
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [Header("Login UI")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button guestLoginButton;
        [SerializeField] private Button showRegisterButton;

        [Header("Register UI")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button showLoginButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private GameObject loadingIndicator;

        // Current user data
        public User CurrentUser { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupButtonListeners();
            
            // Try auto-login if token exists
            if (APIClient.Instance != null && APIClient.Instance.IsAuthenticated)
            {
                OnAutoLogin();
            }
        }

        #endregion

        #region Auto Login

        private void OnAutoLogin()
        {
            ShowLoading(true);
            AuthAPI.Instance.GetProfile(response =>
            {
                ShowLoading(false);
                if (response.success && response.data != null)
                {
                    OnLoginSuccess(response.data);
                }
                else
                {
                    // Token invalid, clear and show login
                    APIClient.Instance.ClearTokens();
                }
            });
        }

        #endregion

        #region Email Login

        public void OnLoginButtonClicked()
        {
            string email = loginEmailInput?.text.Trim() ?? "";
            string password = loginPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter email and password");
                return;
            }

            Login(email, password);
        }

        private void Login(string email, string password)
        {
            ShowError("");
            ShowLoading(true);
            SetButtonsInteractable(false);

            string deviceId = APIClient.Instance.GetDeviceId();

            AuthAPI.Instance.Login(email, password, deviceId, response =>
            {
                ShowLoading(false);
                SetButtonsInteractable(true);

                if (response.success && response.data != null)
                {
                    OnLoginSuccess(response.data.user);
                }
                else
                {
                    ShowError(response.message ?? "Login failed");
                }
            });
        }

        #endregion

        #region Guest Login

        public void OnGuestLoginButtonClicked()
        {
            ShowError("");
            ShowLoading(true);
            SetButtonsInteractable(false);

            string deviceId = APIClient.Instance.GetDeviceId();

            AuthAPI.Instance.GuestLogin(deviceId, response =>
            {
                ShowLoading(false);
                SetButtonsInteractable(true);

                if (response.success && response.data != null)
                {
                    OnLoginSuccess(response.data.user);
                }
                else
                {
                    ShowError(response.message ?? "Guest login failed");
                }
            });
        }

        #endregion

        #region Register

        public void OnRegisterButtonClicked()
        {
            string displayName = registerUsernameInput?.text.Trim() ?? "";
            string email = registerEmailInput?.text.Trim() ?? "";
            string password = registerPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please fill all fields");
                return;
            }

            if (password.Length < 8)
            {
                ShowError("Password must be at least 8 characters");
                return;
            }

            Register(email, password, displayName);
        }

        private void Register(string email, string password, string displayName)
        {
            ShowError("");
            ShowLoading(true);
            SetButtonsInteractable(false);

            string deviceId = APIClient.Instance.GetDeviceId();

            AuthAPI.Instance.Register(email, password, displayName, deviceId, response =>
            {
                ShowLoading(false);
                SetButtonsInteractable(true);

                if (response.success && response.data != null)
                {
                    OnLoginSuccess(response.data.user);
                }
                else
                {
                    ShowError(response.message ?? "Registration failed");
                }
            });
        }

        #endregion

        #region Login Success

        private void OnLoginSuccess(User user)
        {
            CurrentUser = user;
            Debug.Log($"[AuthManager] Login successful: {user.displayName}");

            // Update GameManager with user data
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerData(user.displayName, user.AvailablePoints, int.Parse(user.level ?? "1"));
            }

            // Navigate to main menu
            UI.UIManager.Instance?.ShowMainMenu();
        }

        #endregion

        #region Logout

        public void Logout()
        {
            ShowLoading(true);

            AuthAPI.Instance.Logout(response =>
            {
                ShowLoading(false);
                CurrentUser = null;
                UI.UIManager.Instance?.ShowLoginPanel();
            });
        }

        #endregion

        #region Profile

        public void UpdateProfile(string displayName, string avatar, System.Action<bool> callback)
        {
            var request = new UpdateProfileRequest
            {
                displayName = displayName,
                avatar = avatar
            };

            AuthAPI.Instance.UpdateProfile(request, response =>
            {
                if (response.success && response.data != null)
                {
                    CurrentUser = response.data;
                }
                callback?.Invoke(response.success);
            });
        }

        #endregion

        #region UI Helpers

        public void ShowLoginPanel()
        {
            if (registerPanel != null)
                registerPanel.SetActive(false);
        }

        public void ShowRegisterPanel()
        {
            if (registerPanel != null)
                registerPanel.SetActive(true);
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (loginButton != null) loginButton.interactable = interactable;
            if (registerButton != null) registerButton.interactable = interactable;
            if (guestLoginButton != null) guestLoginButton.interactable = interactable;
        }

        private void SetupButtonListeners()
        {
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);

            if (guestLoginButton != null)
                guestLoginButton.onClick.AddListener(OnGuestLoginButtonClicked);

            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClicked);

            if (showRegisterButton != null)
                showRegisterButton.onClick.AddListener(ShowRegisterPanel);

            if (showLoginButton != null)
                showLoginButton.onClick.AddListener(ShowLoginPanel);
        }

        #endregion
    }
}
