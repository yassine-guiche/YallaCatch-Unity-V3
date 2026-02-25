using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YallaCatch.Managers;
using YallaCatch.API;
using YallaCatch.Models;
using YallaCatch.Networking;

namespace YallaCatch.UI
{
    /// <summary>
    /// Manages all UI panels and interactions
    /// Central hub for UI updates and user feedback
    /// </summary>
    public partial class UIManager : MonoBehaviour
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
        
        [Header("Live Signal")]
        [SerializeField] private GameObject liveSignalIndicator;
        [SerializeField] private Color pulseColor = Color.green;
        [SerializeField] private Button rewardedAdButton;

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Slider experienceSlider;

        [Header("Capture Dialog")]
        [SerializeField] private GameObject captureDialog;
        [SerializeField] private TextMeshProUGUI prizeNameText;
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

            // Global API error bridging
            if (APIClient.Instance != null)
            {
                APIClient.Instance.OnRequestError += (err) => ShowMessage($"NETWORK ALERT\n{err}");
            }

            // Global realtime error bridging
            if (SocketIOClient.Instance != null)
            {
                SocketIOClient.Instance.OnError += (err) => ShowMessage(err);
            }

            // Global Access Denied (Ban) bridging
            if (APIClient.Instance != null)
            {
                APIClient.Instance.OnAccessDenied += () => {
                    ShowMessage("<color=red>ACCOUNT RESTRICTED</color>\nYour access has been suspended by an administrator.\nPlease contact support for more information.");
                };
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

        [Header("Extensions")]
        [SerializeField] private GameObject socialPanel;
        [SerializeField] private GameObject challengesPanel;

        private void ShowPanel(GameObject panel)
        {
            if (panel == null) return;

            // Hide all panels with a quick fade or animator
            GameObject[] allPanels = { 
                loginPanel, mainMenuPanel, gamePanel, capturePanel, 
                rewardsPanel, profilePanel, settingsPanel, socialPanel, 
                challengesPanel, marketplacePanel, claimsPanel,
                inventoryPanel, leaderboardPanel, achievementsPanel, notificationsPanel
            };

            foreach (var p in allPanels)
            {
                if (p != null && p.activeSelf && p != panel)
                {
                    UIAnimator animator = p.GetComponent<UIAnimator>();
                    if (animator != null)
                    {
                        animator.Hide();
                    }
                    else
                    {
                        CanvasGroup cg = p.GetComponent<CanvasGroup>();
                        if (cg != null) StartCoroutine(FadePanel(cg, 0, 0.2f, () => p.SetActive(false)));
                        else p.SetActive(false);
                    }
                }
            }

            // Show requested panel
            UIAnimator targetAnimator = panel.GetComponent<UIAnimator>();
            if (targetAnimator != null)
            {
                targetAnimator.Show();
            }
            else
            {
                panel.SetActive(true);
                CanvasGroup targetCg = panel.GetComponent<CanvasGroup>();
                if (targetCg != null)
                {
                    targetCg.alpha = 0;
                    StartCoroutine(FadePanel(targetCg, 1, 0.3f));
                }
            }
        }

        private IEnumerator FadePanel(CanvasGroup cg, float targetAlpha, float duration, System.Action onComplete = null)
        {
            float startAlpha = cg.alpha;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                yield return null;
            }
            cg.alpha = targetAlpha;
            onComplete?.Invoke();
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

        public void ShowProfilePanel()
        {
            if (!IsUserAuthenticatedForUi())
            {
                ShowMessage("Please sign in to access profile.");
                ShowLoginPanel();
                return;
            }

            ShowPanel(profilePanel);
            // Profile data is now handled by ProfileManager or AuthAPI and populated on event
        }

        public void ShowSettingsPanel()
        {
            if (!IsUserAuthenticatedForUi())
            {
                ShowMessage("Please sign in to access settings.");
                ShowLoginPanel();
                return;
            }

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

        [Header("3D Effects Integration")]
        [SerializeField] private GameObject boxAnimatorPrefab;
        private Effects.BoxAnimator currentBoxAnimator;

        public void ShowCaptureDialog(Prize prize)
        {
            currentPrize = prize;

            // 1. Hide HUD
            ShowPanel(null); // Clear panels

            // 2. Instantiate 3D Box in AR/World Space
            if (boxAnimatorPrefab != null)
            {
                GameObject boxObj = Instantiate(boxAnimatorPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, Quaternion.identity);
                currentBoxAnimator = boxObj.GetComponent<Effects.BoxAnimator>();
                
                if (currentBoxAnimator != null)
                {
                    Color rarityColor = ThemeManager.Instance != null ? ThemeManager.Instance.GetRarityColor(prize.rarity) : Color.white;
                    currentBoxAnimator.Initialize(prize, rarityColor);
                    
                    currentBoxAnimator.onBoxOpened.AddListener(() => ShowCaptureUI(prize));
                    currentBoxAnimator.PlayDropSequence();

                    StartCoroutine(WaitForTapToOpenBox());
                }
                else
                {
                    ShowCaptureUI(prize);
                }
            }
            else
            {
                ShowCaptureUI(prize);
            }
        }

        private IEnumerator WaitForTapToOpenBox()
        {
            yield return new WaitForSeconds(1f); // wait for drop
            bool tapped = false;
            while (!tapped)
            {
                if (Input.GetMouseButtonDown(0)) tapped = true;
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) tapped = true;
                yield return null;
            }

            if (currentBoxAnimator != null)
            {
                currentBoxAnimator.OpenBox();
            }
        }

        private void ShowCaptureUI(Prize prize)
        {
            if (captureDialog != null)
            {
                UIAnimator anim = captureDialog.GetComponent<UIAnimator>();
                if (anim != null) anim.Show();
                else captureDialog.SetActive(true);

                if (prizeNameText != null)
                    prizeNameText.text = prize.name;

                if (prizeDescriptionText != null)
                    prizeDescriptionText.text = prize.description;

                if (prizePointsText != null)
                    prizePointsText.text = $"+{prize.pointsValue} points";

                if (ThemeManager.Instance != null && prizeNameText != null)
                {
                    prizeNameText.color = ThemeManager.Instance.GetRarityColor(prize.rarity);
                }

                if (prizeImage != null && !string.IsNullOrEmpty(prize.imageUrl))
                {
                    StartCoroutine(LoadPrizeImage(APIClient.Instance.GetFullImageUrl(prize.imageUrl)));
                }
            }
        }

        public void OnCaptureButtonClicked()
        {
            if (currentPrize != null)
            {
                ShowLoading("Capturing prize...");

                if (GameManager.Instance == null)
                {
                    HideLoading();
                    ShowMessage("Game system is not ready.");
                    return;
                }

                GameManager.Instance.CapturePrize(currentPrize, (success, message, points) =>
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
                });
            }
        }

        public void OnCancelButtonClicked()
        {
            if (captureDialog != null)
            {
                UIAnimator anim = captureDialog.GetComponent<UIAnimator>();
                if (anim != null) anim.Hide();
                else captureDialog.SetActive(false);
            }

            if (currentBoxAnimator != null)
            {
                Destroy(currentBoxAnimator.gameObject);
            }
        }

        private void ShowCaptureSuccess(string prizeName, int points)
        {
            // Show celebration animation/effects
            ShowMessage($"Captured {prizeName}!\n+{points} points");
            
            // Play success sound
            // AudioManager.Instance?.PlaySound("capture_success");
            
            // Play particle effects
            // ParticleManager.Instance?.PlayEffect("capture_particles");
        }

        private IEnumerator LoadPrizeImage(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success && prizeImage != null)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
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
                UIAnimator anim = messageDialog.GetComponent<UIAnimator>();
                if (anim != null) anim.Show();
                else messageDialog.SetActive(true);
                
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
                UIAnimator anim = messageDialog.GetComponent<UIAnimator>();
                if (anim != null) anim.Hide();
                else messageDialog.SetActive(false);
                
                // If the message was about a ban or access denial, force logout
                if (messageText != null && (messageText.text.Contains("ACCOUNT RESTRICTED") || messageText.text.Contains("suspendu") || messageText.text.Contains("banned")))
                {
                    AuthManager.Instance?.Logout();
                }
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
        
        #region Live Signal Pulse
        
        public void PulseLiveSignal()
        {
            if (liveSignalIndicator != null)
            {
                StopCoroutine("PulseEffect");
                StartCoroutine("PulseEffect");
            }
        }
        
        private IEnumerator PulseEffect()
        {
            Image img = liveSignalIndicator.GetComponent<Image>();
            if (img == null) yield break;
            
            Color original = img.color;
            float duration = 0.5f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                img.color = Color.Lerp(pulseColor, original, t);
                liveSignalIndicator.transform.localScale = Vector3.one * (1f + 0.2f * (1f - t));
                yield return null;
            }
            
            img.color = original;
            liveSignalIndicator.transform.localScale = Vector3.one;
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

            if (rewardedAdButton != null)
                rewardedAdButton.onClick.AddListener(OnAdButtonClicked);

            SetupExtensionButtonListeners();
        }

        private void OnAdButtonClicked()
        {
            if (AdMobManager.Instance != null)
            {
                ShowLoading("Loading ad...");
                AdMobManager.Instance.ShowRewardedAd((success, points) =>
                {
                    HideLoading();
                    if (success)
                    {
                        ShowMessage($"REWARD GRANTED!\n+{points} Points Added");
                    }
                    else
                    {
                        ShowMessage("AD UNAVAILABLE\nTry again later");
                    }
                });
            }
        }

        public void ShowGPSAlert()
        {
            ShowMessage("<color=red>SIGNAL COMPROMISED</color>\nGPS Spoofing / Weak Signal detected.\nRewards disabled.");
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
