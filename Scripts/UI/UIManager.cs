using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YallaCatch.Managers;
using YallaCatch.API;

namespace YallaCatch.UI
{
    /// <summary>
    /// Manages all UI panels and interactions
    /// Central hub for UI updates and user feedback
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject capturePanel;
        [SerializeField] private GameObject rewardsPanel;
        [SerializeField] private GameObject profilePanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Slider experienceSlider;

        [Header("Capture Dialog")]
        [SerializeField] private GameObject captureDialog;
        [SerializeField] private TextMeshProUGUI prizeName Text;
        [SerializeField] private TextMeshProUGUI prizeDescriptionText;
        [SerializeField] private TextMeshProUGUI prizePointsText;
        [SerializeField] private Image prizeImage;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button cancelButton;

        [Header("Message Dialog")]
        [SerializeField] private GameObject messageDialog;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button messageOkButton;

        [Header("Loading")]
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TextMeshProUGUI loadingText;

        private Prize currentPrize;

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
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPointsChanged += UpdatePointsDisplay;
                GameManager.Instance.OnGameReady += OnGameReady;
            }

            // Setup button listeners
            SetupButtonListeners();

            // Show login panel if not authenticated
            if (!APIManager.Instance.IsAuthenticated())
            {
                ShowPanel(loginPanel);
            }
            else
            {
                ShowPanel(mainMenuPanel);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPointsChanged -= UpdatePointsDisplay;
                GameManager.Instance.OnGameReady -= OnGameReady;
            }
        }

        #endregion

        #region Panel Management

        private void ShowPanel(GameObject panel)
        {
            // Hide all panels
            loginPanel?.SetActive(false);
            mainMenuPanel?.SetActive(false);
            gamePanel?.SetActive(false);
            capturePanel?.SetActive(false);
            rewardsPanel?.SetActive(false);
            profilePanel?.SetActive(false);
            settingsPanel?.SetActive(false);

            // Show requested panel
            panel?.SetActive(true);
        }

        public void ShowLoginPanel()
        {
            ShowPanel(loginPanel);
        }

        public void ShowMainMenu()
        {
            ShowPanel(mainMenuPanel);
        }

        public void ShowGamePanel()
        {
            ShowPanel(gamePanel);
        }

        public void ShowRewardsPanel()
        {
            ShowPanel(rewardsPanel);
            LoadRewards();
        }

        public void ShowProfilePanel()
        {
            ShowPanel(profilePanel);
            LoadProfile();
        }

        public void ShowSettingsPanel()
        {
            ShowPanel(settingsPanel);
        }

        #endregion

        #region HUD Updates

        private void UpdatePointsDisplay(int points)
        {
            if (pointsText != null)
            {
                pointsText.text = $"{points:N0}";
            }
        }

        public void UpdatePlayerInfo(string username, int level)
        {
            if (usernameText != null)
            {
                usernameText.text = username;
            }

            if (levelText != null)
            {
                levelText.text = $"Level {level}";
            }
        }

        public void UpdateExperience(float progress)
        {
            if (experienceSlider != null)
            {
                experienceSlider.value = progress;
            }
        }

        #endregion

        #region Capture Dialog

        public void ShowCaptureDialog(Prize prize)
        {
            currentPrize = prize;

            if (captureDialog != null)
            {
                captureDialog.SetActive(true);

                if (prizeNameText != null)
                    prizeNameText.text = prize.name;

                if (prizeDescriptionText != null)
                    prizeDescriptionText.text = prize.description;

                if (prizePointsText != null)
                    prizePointsText.text = $"+{prize.pointValue} points";

                // Load prize image
                if (prizeImage != null && !string.IsNullOrEmpty(prize.imageUrl))
                {
                    StartCoroutine(LoadPrizeImage(prize.imageUrl));
                }
            }
        }

        public void OnCaptureButtonClicked()
        {
            if (currentPrize != null)
            {
                ShowLoading("Capturing prize...");
                
                StartCoroutine(GameManager.Instance.CapturePrize(currentPrize, (success, message, points) =>
                {
                    HideLoading();
                    captureDialog.SetActive(false);

                    if (success)
                    {
                        ShowCaptureSuccess(currentPrize.name, points);
                    }
                    else
                    {
                        ShowMessage(message);
                    }
                }));
            }
        }

        public void OnCancelButtonClicked()
        {
            if (captureDialog != null)
            {
                captureDialog.SetActive(false);
            }
        }

        private void ShowCaptureSuccess(string prizeName, int points)
        {
            // Show celebration animation/effects
            ShowMessage($"🎉 Captured {prizeName}!\n+{points} points");
            
            // Play success sound
            // AudioManager.Instance?.PlaySound("capture_success");
            
            // Play particle effects
            // ParticleManager.Instance?.PlayEffect("capture_particles");
        }

        private IEnumerator LoadPrizeImage(string url)
        {
            using (WWW www = new WWW(url))
            {
                yield return www;

                if (string.IsNullOrEmpty(www.error) && prizeImage != null)
                {
                    Texture2D texture = www.texture;
                    prizeImage.sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }
        }

        #endregion

        #region Message Dialog

        public void ShowMessage(string message)
        {
            if (messageDialog != null)
            {
                messageDialog.SetActive(true);
                
                if (messageText != null)
                {
                    messageText.text = message;
                }
            }
        }

        public void HideMessage()
        {
            if (messageDialog != null)
            {
                messageDialog.SetActive(false);
            }
        }

        #endregion

        #region Loading

        public void ShowLoading(string message = "Loading...")
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
                
                if (loadingText != null)
                {
                    loadingText.text = message;
                }
            }
        }

        public void HideLoading()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        #endregion

        #region Rewards

        private void LoadRewards()
        {
            ShowLoading("Loading rewards...");

            StartCoroutine(GameManager.Instance.GetAvailableRewards((rewards) =>
            {
                HideLoading();
                DisplayRewards(rewards);
            }));
        }

        private void DisplayRewards(Reward[] rewards)
        {
            // TODO: Populate rewards list UI
            Debug.Log($"Displaying {rewards.Length} rewards");
        }

        public void OnClaimRewardClicked(Reward reward)
        {
            ShowLoading("Claiming reward...");

            StartCoroutine(GameManager.Instance.ClaimReward(reward, (success, message) =>
            {
                HideLoading();
                ShowMessage(message);

                if (success)
                {
                    LoadRewards(); // Refresh rewards list
                }
            }));
        }

        #endregion

        #region Profile

        private void LoadProfile()
        {
            ShowLoading("Loading profile...");

            StartCoroutine(APIManager.Instance.GetUserProfile((profile) =>
            {
                HideLoading();

                if (profile != null)
                {
                    UpdatePlayerInfo(profile.username, profile.level);
                    UpdatePointsDisplay(profile.points);
                    
                    // Update stats display
                    // TODO: Display profile stats
                }
            }));
        }

        #endregion

        #region Button Listeners

        private void SetupButtonListeners()
        {
            if (captureButton != null)
                captureButton.onClick.AddListener(OnCaptureButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);

            if (messageOkButton != null)
                messageOkButton.onClick.AddListener(HideMessage);
        }

        #endregion

        #region Game Events

        private void OnGameReady()
        {
            UpdatePlayerInfo(
                GameManager.Instance.PlayerUsername,
                GameManager.Instance.PlayerLevel
            );
            UpdatePointsDisplay(GameManager.Instance.PlayerPoints);
        }

        #endregion
    }
}
