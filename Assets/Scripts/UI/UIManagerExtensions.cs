using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using YallaCatch.Managers;
using YallaCatch.Models;
using YallaCatch.API;
using YallaCatch.Core;

namespace YallaCatch.UI
{
    /// <summary>
    /// Extensions du UIManager pour les nouveaux managers
    /// Contient toutes les méthodes UI pour Rewards, Marketplace, Social, PowerUps, Challenges, Claims
    /// </summary>
    public partial class UIManager
    {
        [Header("Rewards UI - List")]
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private Button favoritesButton;
        
        [Header("Marketplace UI - List")]
        [SerializeField] private GameObject marketplacePanel;
        [SerializeField] private GameObject marketplaceItemPrefab;
        [SerializeField] private Transform marketplaceContainer;
        [SerializeField] private Button inventoryButton;
        
        [Header("Social UI")]
        [SerializeField] private GameObject friendItemPrefab;
        [SerializeField] private Transform friendsContainer;
        [SerializeField] private GameObject friendRequestPrefab;
        [SerializeField] private Transform requestsContainer;
        [SerializeField] private Button leaderboardButton;
        
        [Header("PowerUps UI")]
        [SerializeField] private GameObject powerUpIndicator;
        [SerializeField] private TMP_Text powerUpTimerText;
        [SerializeField] private Image powerUpIcon;
        
        [Header("Challenges UI - List")]
        [SerializeField] private GameObject challengeItemPrefab;
        [SerializeField] private Transform challengesContainer;
        [SerializeField] private TMP_Text challengesProgressText;
        
        [Header("Claims UI")]
        [SerializeField] private GameObject claimsPanel;
        [SerializeField] private GameObject claimItemPrefab;
        [SerializeField] private Transform claimsContainer;
        [SerializeField] private GameObject qrCodePanel;
        [SerializeField] private RawImage qrCodeImage;

        [Header("Inventory UI")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform inventoryContainer;
        [SerializeField] private TMP_Text inventorySummaryText;

        [Header("Leaderboard UI")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private TMP_Text leaderboardSummaryText;

        [Header("Achievements UI")]
        [SerializeField] private GameObject achievementsPanel;
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private TMP_Text achievementsSummaryText;
        [SerializeField] private Button refreshAchievementsButton;

        [Header("Notifications UI")]
        [SerializeField] private GameObject notificationsPanel;
        [SerializeField] private Transform notificationsContainer;
        [SerializeField] private TMP_Text notificationsSummaryText;
        [SerializeField] private Button markAllNotificationsReadButton;
        [SerializeField] private Button refreshNotificationsButton;
        
        [Header("Report UI")]
        [SerializeField] private GameObject reportPanel;
        [SerializeField] private TMP_Dropdown reportTypeDropdown;
        [SerializeField] private TMP_InputField reportDescriptionInput;
        [SerializeField] private Button submitReportButton;

        [Header("Profile Drawer Extra UI")]
        [SerializeField] private TMP_Text profileLastIpText;
        [SerializeField] private TMP_Text profileDeviceText;
        [SerializeField] private TMP_Text profileLastActiveText;
        [SerializeField] private TMP_Text profileClaimsText;
        [SerializeField] private TMP_Text profileStreakText;
        
        [Header("Confirm Dialog Extra UI")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private TMP_Text confirmTitleText;
        [SerializeField] private TMP_Text confirmMessageText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private bool extensionControlsInitialized;
        private bool rewardsFavoritesOnly;
        private string activeRewardsCategoryFilter;
        private bool hapticsEnabled = true;
        private bool notificationsEnabled = true;
        private bool locationTrackingEnabled = true;
        private bool achievementEventsSubscribed;
        private bool notificationEventsSubscribed;

        private void SetupExtensionButtonListeners()
        {
            if (extensionControlsInitialized)
            {
                RefreshRewardsCategoryOptions();
                UpdateSettingsToggleLabels();
                UpdateFeatureControlAvailability();
                return;
            }

            extensionControlsInitialized = true;

            if (favoritesButton != null)
            {
                favoritesButton.onClick.RemoveAllListeners();
                favoritesButton.onClick.AddListener(OnFavoritesButtonClicked);
            }

            if (categoryDropdown != null)
            {
                categoryDropdown.onValueChanged.RemoveAllListeners();
                categoryDropdown.onValueChanged.AddListener(OnRewardsCategoryChanged);
            }

            if (inventoryButton != null)
            {
                if (inventoryButton.onClick.GetPersistentEventCount() == 0)
                {
                    inventoryButton.onClick.RemoveAllListeners();
                    inventoryButton.onClick.AddListener(ShowInventoryPanel);
                }
            }

            if (leaderboardButton != null)
            {
                if (leaderboardButton.onClick.GetPersistentEventCount() == 0)
                {
                    leaderboardButton.onClick.RemoveAllListeners();
                    leaderboardButton.onClick.AddListener(ShowLeaderboardPanel);
                }
            }

            if (refreshAchievementsButton != null)
            {
                refreshAchievementsButton.onClick.RemoveAllListeners();
                refreshAchievementsButton.onClick.AddListener(RefreshAchievementsPanel);
            }

            if (markAllNotificationsReadButton != null)
            {
                markAllNotificationsReadButton.onClick.RemoveAllListeners();
                markAllNotificationsReadButton.onClick.AddListener(MarkAllNotificationsAsReadFromUI);
            }

            if (refreshNotificationsButton != null)
            {
                refreshNotificationsButton.onClick.RemoveAllListeners();
                refreshNotificationsButton.onClick.AddListener(RefreshNotificationsPanel);
            }

            SyncSettingsStateFromManagers();
            SubscribeToFeatureManagerEvents();
            RefreshRewardsCategoryOptions();
            UpdateFavoritesButtonLabel();
            UpdateSettingsToggleLabels();
            UpdateFeatureControlAvailability();
        }

        private void SubscribeToFeatureManagerEvents()
        {
            if (!achievementEventsSubscribed && AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementsUpdated -= HandleAchievementsUpdated;
                AchievementManager.Instance.OnAchievementsUpdated += HandleAchievementsUpdated;
                achievementEventsSubscribed = true;
            }

            if (!notificationEventsSubscribed && NotificationManager.Instance != null)
            {
                NotificationManager.Instance.OnNotificationsUpdated -= HandleNotificationsUpdated;
                NotificationManager.Instance.OnNotificationsUpdated += HandleNotificationsUpdated;
                notificationEventsSubscribed = true;
            }
        }

        private void SyncSettingsStateFromManagers()
        {
            if (SoundManager.Instance != null)
            {
                bool isAudioEnabled = SoundManager.Instance.IsMusicEnabled() || SoundManager.Instance.IsSFXEnabled();
                // Keep one master switch in generated UI and apply it to both channels.
                if (!isAudioEnabled)
                {
                    // no-op; label refresh will pick it up from SoundManager directly
                }
            }

            if (NotificationManager.Instance?.Settings != null)
            {
                notificationsEnabled = NotificationManager.Instance.Settings.push;
            }

            if (GPSManager.Instance != null)
            {
                locationTrackingEnabled = GPSManager.Instance.IsLocationServiceEnabled;
            }
        }

        private void RefreshRewardsCategoryOptions()
        {
            if (categoryDropdown == null)
            {
                return;
            }

            void ApplyOptions(List<string> categories)
            {
                var optionLabels = new List<string> { "All" };
                if (categories != null)
                {
                    optionLabels.AddRange(categories.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct());
                }

                categoryDropdown.ClearOptions();
                categoryDropdown.AddOptions(optionLabels);

                int selectedIndex = 0;
                if (!string.IsNullOrWhiteSpace(activeRewardsCategoryFilter))
                {
                    selectedIndex = optionLabels.FindIndex(c => string.Equals(c, activeRewardsCategoryFilter, StringComparison.OrdinalIgnoreCase));
                    if (selectedIndex < 0) selectedIndex = 0;
                }

                categoryDropdown.SetValueWithoutNotify(selectedIndex);
            }

            if (RewardsManager.Instance == null)
            {
                ApplyOptions(null);
                return;
            }

            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                ApplyOptions(null);
                return;
            }

            if (RewardsManager.Instance.Categories != null && RewardsManager.Instance.Categories.Count > 0)
            {
                ApplyOptions(RewardsManager.Instance.Categories);
                return;
            }

            ApplyOptions(null);
            RewardsManager.Instance.GetCategories(categories => ApplyOptions(categories));
        }

        private void OnRewardsCategoryChanged(int index)
        {
            if (categoryDropdown == null || index < 0 || index >= categoryDropdown.options.Count)
            {
                return;
            }

            string selected = categoryDropdown.options[index].text;
            activeRewardsCategoryFilter = string.Equals(selected, "All", StringComparison.OrdinalIgnoreCase) ? null : selected;
            RefreshRewardsList(activeRewardsCategoryFilter);
        }

        private void OnFavoritesButtonClicked()
        {
            rewardsFavoritesOnly = !rewardsFavoritesOnly;
            UpdateFavoritesButtonLabel();

            if (rewardsFavoritesOnly && RewardsManager.Instance != null)
            {
                RewardsManager.Instance.LoadFavorites(success =>
                {
                    if (!success)
                    {
                        ShowMessage("Failed to load favorites");
                        ClearListItems(rewardsContainer);
                        RenderListErrorPlaceholder(rewardsContainer, "Failed to load favorite rewards.");
                        return;
                    }

                    RefreshRewardsList(activeRewardsCategoryFilter);
                });
                return;
            }

            RefreshRewardsList(activeRewardsCategoryFilter);
        }

        private void UpdateFavoritesButtonLabel()
        {
            if (favoritesButton == null)
            {
                return;
            }

            TMP_Text buttonText = favoritesButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = rewardsFavoritesOnly ? "FAVORITES [ON]" : "FAVORITES";
            }
        }

        private void UpdateSettingsToggleLabels()
        {
            SetGeneratedButtonLabel("SettingsCard/SoundToggle", GetAudioToggleLabel());
            SetGeneratedButtonLabel("SettingsCard/HapticsToggle", $"HAPTIC FEEDBACK [ {(hapticsEnabled ? "ON" : "OFF")} ]");
            SetGeneratedButtonLabel("SettingsCard/NotifToggle", $"PUSH NOTIFICATIONS [ {(notificationsEnabled ? "ON" : "OFF")} ]");
            SetGeneratedButtonLabel("SettingsCard/LocationToggle", $"GPS TRACKING [ {(locationTrackingEnabled ? "ON" : "OFF")} ]");
        }

        private string GetAudioToggleLabel()
        {
            bool isOn = true;
            if (SoundManager.Instance != null)
            {
                isOn = SoundManager.Instance.IsMusicEnabled() || SoundManager.Instance.IsSFXEnabled();
            }
            return $"MASTER AUDIO [ {(isOn ? "ON" : "OFF")} ]";
        }

        private void SetGeneratedButtonLabel(string relativePath, string label)
        {
            if (settingsPanel == null || string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            TMP_Text text = settingsPanel.transform.Find(relativePath + "/Text")?.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private void UpdateFeatureControlAvailability()
        {
            SetButtonAvailability(favoritesButton, RewardsManager.Instance != null, "Rewards unavailable");
            if (categoryDropdown != null)
            {
                categoryDropdown.interactable = RewardsManager.Instance != null;
            }

            SetButtonAvailability(inventoryButton, MarketplaceManager.Instance != null, "Marketplace unavailable");
            SetButtonAvailability(leaderboardButton, GameManager.Instance != null, "Leaderboard unavailable");
            SetButtonAvailability(refreshAchievementsButton, AchievementManager.Instance != null, "Achievements unavailable");
            SetButtonAvailability(refreshNotificationsButton, NotificationManager.Instance != null, "Notifications unavailable");
            SetButtonAvailability(markAllNotificationsReadButton, NotificationManager.Instance != null, "Notifications unavailable");
        }

        private void SetButtonAvailability(Button button, bool available, string unavailableTooltip = null)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = available;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                return;
            }

            Color baseColor = available ? Color.white : new Color(0.75f, 0.75f, 0.78f, 0.9f);
            label.color = baseColor;

            string suffix = " (UNAVAILABLE)";
            if (available)
            {
                if (label.text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    label.text = label.text.Substring(0, label.text.Length - suffix.Length);
                }
                return;
            }

            if (!string.IsNullOrWhiteSpace(unavailableTooltip))
            {
                if (!label.text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    label.text = $"{label.text}{suffix}";
                }
            }
        }

        private bool IsUserAuthenticatedForUi()
        {
            return APIClient.Instance != null && APIClient.Instance.IsAuthenticated;
        }

        private bool RequireAuthenticatedUiAccess(string featureLabel, bool redirectToLogin = true)
        {
            if (IsUserAuthenticatedForUi())
            {
                return true;
            }

            ShowMessage($"Please sign in to access {featureLabel}.");
            if (redirectToLogin)
            {
                ShowLoginPanel();
            }

            return false;
        }

        public void CloseReportPanel()
        {
            if (reportPanel != null)
            {
                reportPanel.SetActive(false);
            }
        }

        public void CloseQRCodePanel()
        {
            if (qrCodePanel != null)
            {
                qrCodePanel.SetActive(false);
            }
        }

        public void LogoutFromGeneratedUI()
        {
            if (!IsUserAuthenticatedForUi())
            {
                ShowMessage("You are already signed out.");
                ShowLoginPanel();
                return;
            }

            AuthManager.Instance?.Logout();
        }

        public void ToggleMasterAudioSetting()
        {
            if (SoundManager.Instance != null)
            {
                bool next = !(SoundManager.Instance.IsMusicEnabled() || SoundManager.Instance.IsSFXEnabled());
                SoundManager.Instance.SetMusicEnabled(next);
                SoundManager.Instance.SetSFXEnabled(next);
            }

            UpdateSettingsToggleLabels();
        }

        public void ToggleHapticsSetting()
        {
            hapticsEnabled = !hapticsEnabled;
            UpdateSettingsToggleLabels();
        }

        public void ToggleNotificationsSetting()
        {
            if (!RequireAuthenticatedUiAccess("notification settings", redirectToLogin: false))
            {
                UpdateSettingsToggleLabels();
                return;
            }

            notificationsEnabled = !notificationsEnabled;

            if (NotificationManager.Instance != null)
            {
                var current = NotificationManager.Instance.Settings ?? new NotificationSettings();
                current.push = notificationsEnabled;
                NotificationManager.Instance.UpdateSettings(current, success =>
                {
                    if (!success)
                    {
                        ShowMessage("Failed to update notification settings");
                        notificationsEnabled = !notificationsEnabled;
                    }
                    UpdateSettingsToggleLabels();
                });
                return;
            }

            UpdateSettingsToggleLabels();
        }

        public void ToggleLocationSetting()
        {
            locationTrackingEnabled = !locationTrackingEnabled;

            if (!locationTrackingEnabled && GPSManager.Instance != null)
            {
                GPSManager.Instance.StopLocationService();
            }

            if (locationTrackingEnabled && GPSManager.Instance == null)
            {
                ShowMessage("GPS manager not available");
            }

            UpdateSettingsToggleLabels();
        }

        private void ShowInventorySummary()
        {
            if (MarketplaceManager.Instance == null)
            {
                ShowMessage("Inventory unavailable");
                return;
            }

            var inventory = MarketplaceManager.Instance.GetInventory();
            int uniqueItems = inventory?.Count ?? 0;
            int totalQty = inventory?.Values.Sum() ?? 0;
            ShowMessage($"Inventory panel is unavailable in this scene.\nCurrent inventory: {uniqueItems} item types / {totalQty} total.");
        }

        private void ShowLeaderboardSummary()
        {
            if (GameManager.Instance == null)
            {
                ShowMessage("Leaderboard unavailable");
                return;
            }

            ShowLoading("Loading leaderboard...");
            GameManager.Instance.GetLeaderboard("global", (entries, userRank) =>
            {
                HideLoading();
                if (entries == null || entries.Count == 0)
                {
                    ShowMessage("Leaderboard panel is unavailable in this scene.\nNo leaderboard data available.");
                    return;
                }

                var top = entries.Take(3)
                    .Select(e => $"#{e.rank} {e.displayName} ({e.points} pts)");
                string rankLine = userRank != null ? $"\nYour rank: #{userRank.rank}" : string.Empty;
                ShowMessage("Leaderboard panel is unavailable in this scene.\n" + string.Join("\n", top) + rankLine);
            });
        }

        private void EnsureReportTypeOptions()
        {
            if (reportTypeDropdown == null)
            {
                return;
            }

            if (reportTypeDropdown.options == null || reportTypeDropdown.options.Count == 0)
            {
                reportTypeDropdown.ClearOptions();
                reportTypeDropdown.AddOptions(new List<string>
                {
                    "Incorrect Prize Location",
                    "Prize Not Visible",
                    "Duplicate Prize",
                    "Other"
                });
                reportTypeDropdown.SetValueWithoutNotify(0);
            }
        }

        private void RenderListPlaceholder(Transform container, string message)
        {
            if (container == null) return;

            GameObject placeholder = new GameObject("ListPlaceholder");
            placeholder.transform.SetParent(container, false);

            RectTransform rect = placeholder.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 120f);

            LayoutElement layout = placeholder.AddComponent<LayoutElement>();
            layout.preferredHeight = 120f;
            layout.flexibleWidth = 1f;

            TextMeshProUGUI tmp = placeholder.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.7f, 0.72f, 0.78f, 0.95f);
            tmp.fontSize = 24f;
        }

        private void RenderListErrorPlaceholder(Transform container, string message)
        {
            RenderListPlaceholder(container, message);
            if (container == null || container.childCount == 0) return;

            TMP_Text tmp = container.GetChild(container.childCount - 1).GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.color = new Color(1f, 0.55f, 0.45f, 0.95f);
            }
        }

        private void ClearListItems(Transform container)
        {
            if (container == null) return;
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void RenderListDataCard(Transform container, string title, string subtitle = null, string meta = null, Color? accent = null)
        {
            if (container == null) return;

            GameObject row = new GameObject("RowItem");
            row.transform.SetParent(container, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 120f);

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 120f;
            layout.flexibleWidth = 1f;

            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            if (accent.HasValue)
            {
                GameObject accentBar = new GameObject("Accent");
                accentBar.transform.SetParent(row.transform, false);
                RectTransform accentRect = accentBar.AddComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.sizeDelta = new Vector2(8f, 0f);
                Image accentImage = accentBar.AddComponent<Image>();
                accentImage.color = accent.Value;
            }

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(24f, -50f);
            titleRect.offsetMax = new Vector2(-24f, -10f);
            TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title ?? string.Empty;
            titleText.color = Color.white;
            titleText.fontSize = 24f;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.enableWordWrapping = false;

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                GameObject subtitleObj = new GameObject("SubtitleText");
                subtitleObj.transform.SetParent(row.transform, false);
                RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
                subtitleRect.anchorMin = new Vector2(0f, 0f);
                subtitleRect.anchorMax = new Vector2(1f, 1f);
                subtitleRect.offsetMin = new Vector2(24f, 8f);
                subtitleRect.offsetMax = new Vector2(-24f, -50f);
                TMP_Text subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
                subtitleText.text = subtitle;
                subtitleText.color = new Color(0.72f, 0.74f, 0.8f, 0.95f);
                subtitleText.fontSize = 18f;
                subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
                subtitleText.enableWordWrapping = true;
            }

            if (!string.IsNullOrWhiteSpace(meta))
            {
                GameObject metaObj = new GameObject("MetaText");
                metaObj.transform.SetParent(row.transform, false);
                RectTransform metaRect = metaObj.AddComponent<RectTransform>();
                metaRect.anchorMin = new Vector2(1f, 0.5f);
                metaRect.anchorMax = new Vector2(1f, 0.5f);
                metaRect.pivot = new Vector2(1f, 0.5f);
                metaRect.anchoredPosition = new Vector2(-18f, 0f);
                metaRect.sizeDelta = new Vector2(260f, 60f);
                TMP_Text metaText = metaObj.AddComponent<TextMeshProUGUI>();
                metaText.text = meta;
                metaText.color = accent ?? new Color(0.6f, 0.8f, 1f);
                metaText.fontSize = 18f;
                metaText.alignment = TextAlignmentOptions.MidlineRight;
                metaText.enableWordWrapping = false;
            }
        }

        private string FormatRelativeTime(DateTime timestamp)
        {
            if (timestamp == default)
            {
                return "Unknown";
            }

            TimeSpan delta = DateTime.UtcNow - timestamp.ToUniversalTime();
            if (delta.TotalMinutes < 1) return "Just now";
            if (delta.TotalHours < 1) return $"{Mathf.Max(1, (int)delta.TotalMinutes)}m ago";
            if (delta.TotalDays < 1) return $"{Mathf.Max(1, (int)delta.TotalHours)}h ago";
            return $"{Mathf.Max(1, (int)delta.TotalDays)}d ago";
        }

        private void HandleAchievementsUpdated()
        {
            if (achievementsPanel != null && achievementsPanel.activeSelf)
            {
                PopulateAchievementsPanelFromCache();
                HideLoading();
            }
        }

        private void HandleNotificationsUpdated()
        {
            if (notificationsPanel != null && notificationsPanel.activeSelf)
            {
                PopulateNotificationsPanelFromCache();
            }
        }

        #region Rewards UI

        /// <summary>
        /// Affiche le panneau des récompenses
        /// </summary>
        public void ShowRewardsPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("rewards"))
            {
                return;
            }
            ShowPanel(rewardsPanel);
            ShowLoading("Loading rewards...");

            if (RewardsManager.Instance != null)
            {
                if (rewardsFavoritesOnly)
                {
                    RewardsManager.Instance.LoadFavorites(success =>
                    {
                        HideLoading();
                        if (!success)
                        {
                            ShowMessage("Failed to load favorites");
                            ClearListItems(rewardsContainer);
                            RenderListErrorPlaceholder(rewardsContainer, "Failed to load favorite rewards.");
                            return;
                        }

                        RefreshRewardsList(activeRewardsCategoryFilter);
                    });
                    return;
                }

                RewardsManager.Instance.LoadRewards(activeRewardsCategoryFilter, 1, 50, success =>
                {
                    HideLoading();
                    if (!success)
                    {
                        ShowMessage("Failed to load rewards");
                        ClearListItems(rewardsContainer);
                        RenderListErrorPlaceholder(rewardsContainer, "Failed to load rewards.");
                        return;
                    }

                    RefreshRewardsCategoryOptions();
                    RefreshRewardsList(activeRewardsCategoryFilter);
                });
            }
            else
            {
                HideLoading();
                RefreshRewardsList(activeRewardsCategoryFilter);
            }
        }

        /// <summary>
        /// Rafraîchit la liste des récompenses
        /// </summary>
        public void RefreshRewardsList(string categoryFilter = null)
        {
            if (rewardsContainer == null || rewardItemPrefab == null || RewardsManager.Instance == null)
                return;

            if (categoryFilter != null)
            {
                activeRewardsCategoryFilter = string.IsNullOrWhiteSpace(categoryFilter) ? null : categoryFilter;
            }
            
            // Clear existing items
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get rewards
            IEnumerable<Reward> rewards = rewardsFavoritesOnly
                ? (RewardsManager.Instance.FavoriteRewards ?? new List<Reward>())
                : RewardsManager.Instance.GetAllRewards();

            if (!string.IsNullOrWhiteSpace(activeRewardsCategoryFilter))
            {
                rewards = rewards.Where(r => string.Equals(r.category, activeRewardsCategoryFilter, StringComparison.OrdinalIgnoreCase));
            }
            
            // Create UI items
            foreach (var reward in rewards)
            {
                GameObject item = Instantiate(rewardItemPrefab, rewardsContainer);
                SetupRewardItem(item, reward);
            }

            if (!rewards.Any())
            {
                string emptyMessage = rewardsFavoritesOnly
                    ? "No favorite rewards yet."
                    : "No rewards available for the selected filter.";
                RenderListPlaceholder(rewardsContainer, emptyMessage);
            }
        }

        /// <summary>
        /// Configure un item de récompense
        /// </summary>
        private void SetupRewardItem(GameObject item, Reward reward)
        {
            // Set name
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = reward.name;
            
            // Set points cost
            TMP_Text pointsText = item.transform.Find("PointsText")?.GetComponent<TMP_Text>();
            if (pointsText != null)
                pointsText.text = $"{reward.pointsCost} pts";
            
            // Set image
            Image rewardImage = item.transform.Find("RewardImage")?.GetComponent<Image>();
            if (rewardImage != null && !string.IsNullOrEmpty(reward.PrimaryImage))
            {
                StartCoroutine(LoadImageFromURL(APIClient.Instance.GetFullImageUrl(reward.PrimaryImage), rewardImage));
            }
            
            // Redeem button
            Button redeemButton = item.transform.Find("RedeemButton")?.GetComponent<Button>();
            if (redeemButton != null)
            {
                bool canAfford = RewardsManager.Instance.CanAffordReward(reward);
                redeemButton.interactable = canAfford && reward.stock > 0;
                redeemButton.onClick.AddListener(() => OnRedeemRewardClicked(reward));
            }
            
            // Favorite button
            Button favoriteButton = item.transform.Find("FavoriteButton")?.GetComponent<Button>();
            if (favoriteButton != null)
            {
                bool isFavorite = RewardsManager.Instance.IsFavorite(reward.Id);
                TMP_Text favText = favoriteButton.GetComponentInChildren<TMP_Text>();
                if (favText != null)
                {
                    favText.text = isFavorite ? "FAV" : "ADD";
                }
                favoriteButton.onClick.AddListener(() => ToggleFavorite(reward.Id));
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton d'échange de récompense
        /// </summary>
        private void OnRedeemRewardClicked(Reward reward)
        {
            ShowConfirmDialog(
                $"Redeem {reward.name}?",
                $"This will cost {reward.pointsCost} points.",
                () => RewardsManager.Instance.RedeemReward(reward.Id, (success, redemption, message) =>
                {
                    ShowMessage(message ?? (success ? "Reward redeemed successfully!" : "Redemption failed"));
                    if (success)
                    {
                        RefreshRewardsList(activeRewardsCategoryFilter);
                    }
                })
            );
        }

        /// <summary>
        /// Toggle favorite
        /// </summary>
        private void ToggleFavorite(string rewardId)
        {
            if (RewardsManager.Instance.IsFavorite(rewardId))
            {
                RewardsManager.Instance.RemoveFromFavorites(rewardId, success =>
                {
                    if (!success) ShowMessage("Failed to remove favorite");
                    RefreshRewardsList(activeRewardsCategoryFilter);
                });
            }
            else
            {
                RewardsManager.Instance.AddToFavorites(rewardId, success =>
                {
                    if (!success) ShowMessage("Failed to add favorite");
                    RefreshRewardsList(activeRewardsCategoryFilter);
                });
            }
        }

        #endregion

        #region Marketplace UI

        /// <summary>
        /// Affiche le panneau marketplace
        /// </summary>
        public void ShowMarketplacePanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("marketplace"))
            {
                return;
            }
            ShowPanel(marketplacePanel);

            if (MarketplaceManager.Instance == null)
            {
                RefreshMarketplaceList();
                return;
            }

            if (MarketplaceManager.Instance.GetAllItems() == null || MarketplaceManager.Instance.GetAllItems().Count == 0)
            {
                ShowLoading("Loading marketplace...");
                StartCoroutine(MarketplaceManager.Instance.LoadAllItems(success =>
                {
                    HideLoading();
                    if (!success)
                    {
                        ShowMessage("Failed to load marketplace");
                        ClearListItems(marketplaceContainer);
                        RenderListErrorPlaceholder(marketplaceContainer, "Failed to load marketplace items.");
                        return;
                    }
                    RefreshMarketplaceList();
                }));
                return;
            }

            RefreshMarketplaceList();
        }

        /// <summary>
        /// Rafraîchit la liste du marketplace
        /// </summary>
        public void RefreshMarketplaceList()
        {
            if (marketplaceContainer == null || marketplaceItemPrefab == null || MarketplaceManager.Instance == null)
                return;
            
            // Clear existing items
            foreach (Transform child in marketplaceContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get items
            var items = MarketplaceManager.Instance.GetAllItems();
            
            // Create UI items
            foreach (var item in items)
            {
                GameObject uiItem = Instantiate(marketplaceItemPrefab, marketplaceContainer);
                SetupMarketplaceItem(uiItem, item);
            }

            if (items == null || items.Count == 0)
            {
                RenderListPlaceholder(marketplaceContainer, "No marketplace items available.");
            }
        }

        /// <summary>
        /// Configure un item du marketplace
        /// </summary>
        private void SetupMarketplaceItem(GameObject uiItem, MarketplaceItem item)
        {
            // Similar to SetupRewardItem but for marketplace items
            TMP_Text nameText = uiItem.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = item.name ?? item.title;
            
            TMP_Text priceText = uiItem.transform.Find("PriceText")?.GetComponent<TMP_Text>();
            if (priceText != null)
            {
                int cost = item.pointsCost > 0 ? item.pointsCost : item.price;
                priceText.text = $"{cost} pts";
            }
            
            Button buyButton = uiItem.transform.Find("BuyButton")?.GetComponent<Button>();
            if (buyButton != null)
            {
                bool canAfford = MarketplaceManager.Instance.CanAffordItem(item);
                buyButton.interactable = canAfford;
                buyButton.onClick.AddListener(() => OnBuyItemClicked(item));
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton d'achat
        /// </summary>
        private void OnBuyItemClicked(MarketplaceItem item)
        {
            int cost = item.pointsCost > 0 ? item.pointsCost : item.price;
            ShowConfirmDialog(
                $"Buy {item.name ?? item.title}?",
                $"This will cost {cost} points.",
                () => StartCoroutine(MarketplaceManager.Instance.PurchaseItem(item.id))
            );
        }

        #endregion

        #region Social UI

        /// <summary>
        /// Affiche le panneau social
        /// </summary>
        public void ShowSocialPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("social"))
            {
                return;
            }
            ShowPanel(socialPanel);
            RefreshFriendsList();
            RefreshFriendRequests();

            if (SocialManager.Instance != null)
            {
                ShowLoading("Loading social...");
                int pendingLoads = 2;
                System.Action finishLoad = () =>
                {
                    pendingLoads--;
                    if (pendingLoads <= 0)
                    {
                        HideLoading();
                    }
                };

                SocialManager.Instance.LoadFriends(success =>
                {
                    if (!success)
                    {
                        ShowMessage("Failed to load friends");
                        var cachedFriends = SocialManager.Instance.GetFriends();
                        if (cachedFriends == null || cachedFriends.Count == 0)
                        {
                            ClearListItems(friendsContainer);
                            RenderListErrorPlaceholder(friendsContainer, "Failed to load friends.");
                            finishLoad();
                            return;
                        }
                    }
                    RefreshFriendsList();
                    finishLoad();
                });

                SocialManager.Instance.LoadPendingRequests(success =>
                {
                    if (!success)
                    {
                        ShowMessage("Failed to load friend requests");
                        var cachedRequests = SocialManager.Instance.GetPendingRequests();
                        if (cachedRequests == null || cachedRequests.Count == 0)
                        {
                            ClearListItems(requestsContainer);
                            RenderListErrorPlaceholder(requestsContainer, "Failed to load friend requests.");
                            finishLoad();
                            return;
                        }
                    }
                    RefreshFriendRequests();
                    finishLoad();
                });
            }
        }

        /// <summary>
        /// Rafraîchit la liste d'amis
        /// </summary>
        public void RefreshFriendsList()
        {
            if (friendsContainer == null || friendItemPrefab == null || SocialManager.Instance == null)
                return;
            
            foreach (Transform child in friendsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var friends = SocialManager.Instance.GetFriends();
            
            foreach (var friend in friends)
            {
                GameObject item = Instantiate(friendItemPrefab, friendsContainer);
                SetupFriendItem(item, friend);
            }

            if (friends == null || friends.Count == 0)
            {
                RenderListPlaceholder(friendsContainer, "No friends yet.");
            }
        }

        /// <summary>
        /// Configure un item d'ami
        /// </summary>
        private void SetupFriendItem(GameObject item, Friend friend)
        {
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = friend.displayName;
            
            TMP_Text statusText = item.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusText != null)
                statusText.text = friend.isOnline ? "Online" : "Offline";
            
            Button profileButton = item.transform.Find("ProfileButton")?.GetComponent<Button>();
            if (profileButton != null)
            {
                profileButton.onClick.AddListener(() => ShowPlayerProfile(friend.friendId));
            }
        }

        /// <summary>
        /// Rafraîchit les demandes d'amis
        /// </summary>
        public void RefreshFriendRequests()
        {
            if (requestsContainer == null || friendRequestPrefab == null || SocialManager.Instance == null)
                return;
            
            foreach (Transform child in requestsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var requests = SocialManager.Instance.GetPendingRequests();
            
            foreach (var request in requests)
            {
                GameObject item = Instantiate(friendRequestPrefab, requestsContainer);
                SetupFriendRequestItem(item, request);
            }

            if (requests == null || requests.Count == 0)
            {
                RenderListPlaceholder(requestsContainer, "No pending friend requests.");
            }
        }

        /// <summary>
        /// Configure un item de demande d'ami
        /// </summary>
        private void SetupFriendRequestItem(GameObject item, FriendRequest request)
        {
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = request.displayName;
            
            Button acceptButton = item.transform.Find("AcceptButton")?.GetComponent<Button>();
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(() =>
                {
                    SocialManager.Instance.AcceptFriendRequest(request.requestId, success =>
                    {
                        if (success)
                        {
                            Destroy(item);
                            RefreshFriendsList();
                        }
                        else
                        {
                            ShowMessage("Failed to accept friend request");
                        }
                    });
                });
            }
            
            Button rejectButton = item.transform.Find("RejectButton")?.GetComponent<Button>();
            if (rejectButton != null)
            {
                rejectButton.onClick.AddListener(() =>
                {
                    SocialManager.Instance.RejectFriendRequest(request.requestId, success =>
                    {
                        if (success)
                        {
                            Destroy(item);
                        }
                        else
                        {
                            ShowMessage("Failed to reject friend request");
                        }
                    });
                });
            }
        }

        /// <summary>
        /// Affiche le profil d'un joueur
        /// </summary>
        public void ShowPlayerProfile(string userId)
        {
            if (SocialManager.Instance == null)
            {
                ShowMessage("Social system unavailable");
                return;
            }

            ShowLoading("Loading profile...");
            SocialManager.Instance.GetPlayerProfile(userId, (user, status) =>
            {
                HideLoading();
                if (user != null)
                {
                    if (profilePanel != null)
                    {
                        profilePanel.SetActive(true);
                        
                        // Populate Basic Info
                        TMP_Text nameText = profilePanel.transform.Find("PlayerIDCard/NameText")?.GetComponent<TMP_Text>();
                        if (nameText != null) nameText.text = user.displayName;
                        
                        TMP_Text levelText = profilePanel.transform.Find("PlayerIDCard/LevelText")?.GetComponent<TMP_Text>();
                        if (levelText != null) levelText.text = $"Level {user.level}";
                        
                        // Avatar
                        Image avatarImage = profilePanel.transform.Find("PlayerIDCard/AvatarFrame/AvatarImage")?.GetComponent<Image>();
                        if (avatarImage != null && !string.IsNullOrEmpty(user.avatar))
                        {
                            StartCoroutine(LoadImageFromURL(APIClient.Instance.GetFullImageUrl(user.avatar), avatarImage));
                        }
                        
                        // Technical Specifications (from GEMINI.md)
                        if (profileLastIpText != null) profileLastIpText.text = $"IP: {user.lastIp ?? "Unknown"}";
                        if (profileDeviceText != null) profileDeviceText.text = $"Device: {user.lastUserAgent ?? "Unknown"}";
                        if (profileLastActiveText != null) profileLastActiveText.text = $"Last Signal: {user.lastActive.ToLocalTime()}";
                        
                        // Game Stats
                        if (profileClaimsText != null) profileClaimsText.text = $"Claims: {user.stats?.prizesFound ?? 0}";
                        if (profileStreakText != null) profileStreakText.text = $"Streak: {user.stats?.currentStreak ?? 0} days";
                    }
                }
            });
        }

        #endregion

        #region PowerUps UI

        /// <summary>
        /// Affiche l'indicateur de power-up actif
        /// </summary>
        public void ShowPowerUpIndicator(string powerUpType, float duration)
        {
            if (powerUpIndicator != null)
            {
                powerUpIndicator.SetActive(true);
                StartCoroutine(UpdatePowerUpTimer(powerUpType, duration));
            }
        }

        /// <summary>
        /// Met à jour le timer du power-up
        /// </summary>
        private IEnumerator UpdatePowerUpTimer(string powerUpType, float duration)
        {
            float remaining = duration;
            
            while (remaining > 0)
            {
                if (powerUpTimerText != null)
                {
                    powerUpTimerText.text = $"{powerUpType}\n{remaining:F1}s";
                }
                
                remaining -= Time.deltaTime;
                yield return null;
            }
            
            if (powerUpIndicator != null)
            {
                powerUpIndicator.SetActive(false);
            }
        }

        #endregion

        #region Challenges UI

        /// <summary>
        /// Affiche le panneau des challenges
        /// </summary>
        public void ShowChallengesPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("challenges"))
            {
                return;
            }
            ShowPanel(challengesPanel);

            if (ChallengesManager.Instance == null)
            {
                RefreshChallengesList();
                return;
            }

            ShowLoading("Loading challenges...");
            StartCoroutine(ChallengesManager.Instance.LoadDailyChallenges(challenges =>
            {
                HideLoading();
                RefreshChallengesList();
            }));
        }

        /// <summary>
        /// Rafraîchit la liste des challenges
        /// </summary>
        public void RefreshChallengesList()
        {
            if (challengesContainer == null || challengeItemPrefab == null || ChallengesManager.Instance == null)
                return;
            
            foreach (Transform child in challengesContainer)
            {
                Destroy(child.gameObject);
            }
            
            var challenges = ChallengesManager.Instance.GetDailyChallenges();
            
            foreach (var challenge in challenges)
            {
                GameObject item = Instantiate(challengeItemPrefab, challengesContainer);
                SetupChallengeItem(item, challenge);
            }
            
            // Update progress text
            if (challengesProgressText != null)
            {
                int completed = ChallengesManager.Instance.GetCompletedChallengesCount();
                int total = ChallengesManager.Instance.GetTotalChallengesCount();
                challengesProgressText.text = $"{completed}/{total} Completed";
            }

            if (challenges == null || challenges.Count == 0)
            {
                RenderListPlaceholder(challengesContainer, "No challenges available.");
            }
        }

        /// <summary>
        /// Configure un item de challenge
        /// </summary>
        private void SetupChallengeItem(GameObject item, DailyChallenge challenge)
        {
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = challenge.title;
            
            TMP_Text descText = item.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = challenge.description;
            
            TMP_Text progressText = item.transform.Find("ProgressText")?.GetComponent<TMP_Text>();
            if (progressText != null)
                progressText.text = $"{challenge.progress}/{challenge.target}";
            
            Slider progressBar = item.transform.Find("ProgressBar")?.GetComponent<Slider>();
            if (progressBar != null)
            {
                progressBar.maxValue = challenge.target;
                progressBar.value = challenge.progress;
            }
            
            TMP_Text rewardText = item.transform.Find("RewardText")?.GetComponent<TMP_Text>();
            if (rewardText != null)
                rewardText.text = $"+{challenge.reward} pts";
            
            // Checkmark si complété
            GameObject checkmark = item.transform.Find("Checkmark")?.gameObject;
            if (checkmark != null)
                checkmark.SetActive(challenge.completed);
        }

        /// <summary>
        /// Affiche le popup de challenge complété
        /// </summary>
        public void ShowChallengeCompletePopup(DailyChallenge challenge, ChallengeCompletionResult result)
        {
            string message = $"Challenge Complete!\n\n{challenge.title}\n\n+{result.reward} points";
            
            if (result.xpReward > 0)
            {
                message += $"\n+{result.xpReward} XP";
            }
            
            if (result.bonusPoints > 0)
            {
                message += $"\n\nBonus: +{result.bonusPoints} points!";
            }
            
            ShowMessage(message);
        }

        #endregion

        #region Claims UI

        /// <summary>
        /// Affiche le panneau des réclamations
        /// </summary>
        public void ShowClaimsPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("claims"))
            {
                return;
            }
            ShowPanel(claimsPanel);

            if (ClaimsManager.Instance == null)
            {
                RefreshClaimsList();
                return;
            }

            ShowLoading("Loading claims...");
            StartCoroutine(ClaimsManager.Instance.LoadMyClaims(claims =>
            {
                HideLoading();
                RefreshClaimsList();
            }));
        }

        /// <summary>
        /// Rafraîchit la liste des réclamations
        /// </summary>
        public void RefreshClaimsList()
        {
            if (claimsContainer == null || claimItemPrefab == null || ClaimsManager.Instance == null)
                return;
            
            foreach (Transform child in claimsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var claims = ClaimsManager.Instance.GetMyClaims();
            
            foreach (var claim in claims)
            {
                GameObject item = Instantiate(claimItemPrefab, claimsContainer);
                SetupClaimItem(item, claim);
            }

            if (claims == null || claims.Count == 0)
            {
                RenderListPlaceholder(claimsContainer, "No claims found.");
            }
        }

        /// <summary>
        /// Configure un item de réclamation
        /// </summary>
        private void SetupClaimItem(GameObject item, YallaCatch.Claim claim)
        {
            TMP_Text rewardText = item.transform.Find("RewardText")?.GetComponent<TMP_Text>();
            if (rewardText != null)
                rewardText.text = claim.rewardName;
            
            TMP_Text statusText = item.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusText != null)
                statusText.text = claim.status.ToUpper();
            
            TMP_Text codeText = item.transform.Find("CodeText")?.GetComponent<TMP_Text>();
            if (codeText != null)
                codeText.text = $"Code: {claim.claimCode}";
            
            Button viewQRButton = item.transform.Find("ViewQRButton")?.GetComponent<Button>();
            if (viewQRButton != null)
            {
                viewQRButton.onClick.AddListener(() => ShowClaimQRCode(claim));
            }
        }

        /// <summary>
        /// Affiche le QR code d'une réclamation
        /// </summary>
        public void ShowClaimQRCode(YallaCatch.Claim claim)
        {
            if (qrCodePanel != null)
            {
                qrCodePanel.SetActive(true);
                
                if (qrCodeImage != null && !string.IsNullOrEmpty(claim.qrCodeUrl))
                {
                    StartCoroutine(LoadQRCodeImage(claim.qrCodeUrl));
                }
            }
        }

        /// <summary>
        /// Charge l'image du QR code
        /// </summary>
        private IEnumerator LoadQRCodeImage(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    if (qrCodeImage != null)
                    {
                        qrCodeImage.texture = texture;
                    }
                }
            }
        }

        #endregion

        #region Extended Feature Panels

        public void ShowInventoryPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("inventory"))
            {
                return;
            }

            if (inventoryPanel == null || inventoryContainer == null)
            {
                ShowInventorySummary();
                return;
            }

            ShowPanel(inventoryPanel);
            RefreshInventoryPanel();
        }

        public void RefreshInventoryPanel()
        {
            if (inventoryContainer == null)
            {
                return;
            }

            ClearListItems(inventoryContainer);

            if (MarketplaceManager.Instance == null)
            {
                inventorySummaryText?.SetText("Marketplace unavailable");
                RenderListPlaceholder(inventoryContainer, "Marketplace manager not available.");
                return;
            }

            var inventory = MarketplaceManager.Instance.GetInventory() ?? new Dictionary<string, int>();
            var allItems = MarketplaceManager.Instance.GetAllItems() ?? new List<MarketplaceItem>();
            var itemsById = allItems.Where(i => !string.IsNullOrEmpty(i.id)).GroupBy(i => i.id).ToDictionary(g => g.Key, g => g.First());

            int totalQty = inventory.Values.Sum();
            if (inventorySummaryText != null)
            {
                inventorySummaryText.text = $"Inventory: {inventory.Count} item types • {totalQty} total items";
            }

            foreach (var kvp in inventory.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                itemsById.TryGetValue(kvp.Key, out MarketplaceItem item);
                string title = item?.name ?? item?.title ?? kvp.Key;
                string subtitle = item != null
                    ? $"{(string.IsNullOrEmpty(item.categoryName) ? (item.category ?? "unknown") : item.categoryName)} • {(item.consumable ? "Consumable" : "Persistent")}"
                    : "Unknown item metadata";
                string meta = $"x{kvp.Value}";
                RenderListDataCard(inventoryContainer, title, subtitle, meta, new Color(0.2f, 0.7f, 1f));
            }

            if (inventory.Count == 0)
            {
                RenderListPlaceholder(inventoryContainer, "Your inventory is empty.");
            }
        }

        public void ShowLeaderboardPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("leaderboard"))
            {
                return;
            }

            if (leaderboardPanel == null || leaderboardContainer == null)
            {
                ShowLeaderboardSummary();
                return;
            }

            ShowPanel(leaderboardPanel);
            RefreshLeaderboardPanel();
        }

        public void RefreshLeaderboardPanel()
        {
            if (leaderboardContainer == null)
            {
                return;
            }

            ClearListItems(leaderboardContainer);

            if (GameManager.Instance == null)
            {
                if (leaderboardSummaryText != null) leaderboardSummaryText.text = "Leaderboard unavailable";
                RenderListPlaceholder(leaderboardContainer, "Game manager not available.");
                return;
            }

            if (leaderboardSummaryText != null)
            {
                leaderboardSummaryText.text = "Loading global leaderboard...";
            }
            RenderListPlaceholder(leaderboardContainer, "Loading leaderboard...");

            ShowLoading("Loading leaderboard...");
            GameManager.Instance.GetLeaderboard("global", (entries, userRank) =>
            {
                HideLoading();
                ClearListItems(leaderboardContainer);

                if (entries == null || entries.Count == 0)
                {
                    if (leaderboardSummaryText != null)
                    {
                        leaderboardSummaryText.text = "No leaderboard data available";
                    }
                    RenderListPlaceholder(leaderboardContainer, "No leaderboard entries found.");
                    return;
                }

                if (leaderboardSummaryText != null)
                {
                    leaderboardSummaryText.text = userRank != null
                        ? $"Global leaderboard • Your rank: #{userRank.rank}"
                        : $"Global leaderboard • Top {entries.Count}";
                }

                foreach (var entry in entries.Take(20))
                {
                    string title = $"#{entry.rank}  {entry.displayName}";
                    string subtitle = $"Level {entry.level} • Prizes {entry.prizesFound}";
                    string meta = $"{entry.points} pts";
                    var accent = entry.rank <= 3 ? new Color(1f, 0.8f, 0.2f) : new Color(0.3f, 0.7f, 1f);
                    RenderListDataCard(leaderboardContainer, title, subtitle, meta, accent);
                }
            });
        }

        public void ShowAchievementsPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("achievements"))
            {
                return;
            }

            if (achievementsPanel == null || achievementsContainer == null)
            {
                ShowMessage("Achievements panel is unavailable in this scene.");
                return;
            }

            ShowPanel(achievementsPanel);
            RefreshAchievementsPanel();
        }

        public void RefreshAchievementsPanel()
        {
            if (achievementsContainer == null)
            {
                return;
            }

            if (AchievementManager.Instance == null)
            {
                ClearListItems(achievementsContainer);
                if (achievementsSummaryText != null) achievementsSummaryText.text = "Achievements unavailable";
                RenderListPlaceholder(achievementsContainer, "Achievement manager not available.");
                return;
            }

            if (achievementsSummaryText != null)
            {
                achievementsSummaryText.text = "Refreshing achievements...";
            }

            PopulateAchievementsPanelFromCache();
            ShowLoading("Loading achievements...");
            AchievementManager.Instance.RefreshAchievements();
        }

        private void PopulateAchievementsPanelFromCache()
        {
            if (achievementsContainer == null || AchievementManager.Instance == null)
            {
                return;
            }

            ClearListItems(achievementsContainer);
            List<Achievement> achievements = AchievementManager.Instance.GetAllAchievements() ?? new List<Achievement>();
            int unlockedCount = achievements.Count(a => a.isUnlocked);

            if (achievementsSummaryText != null)
            {
                achievementsSummaryText.text = $"Achievements: {unlockedCount}/{achievements.Count} unlocked";
            }

            foreach (var achievement in achievements.OrderBy(a => a.isUnlocked ? 1 : 0).ThenBy(a => a.name))
            {
                if (achievement.isHidden && !achievement.isUnlocked)
                {
                    continue;
                }

                float progress = AchievementManager.Instance.GetAchievementProgress(achievement);
                string title = achievement.name;
                string subtitle = $"{achievement.description}\nProgress: {(progress * 100f):F0}%";
                string meta = achievement.isUnlocked ? "Unlocked" : $"+{achievement.rewardPoints} pts";
                Color accent = achievement.isUnlocked ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.8f, 0.6f, 0.2f);
                RenderListDataCard(achievementsContainer, title, subtitle, meta, accent);
            }

            if (achievements.Count == 0)
            {
                RenderListPlaceholder(achievementsContainer, "No achievements loaded yet.");
            }
        }

        public void ShowNotificationsPanel()
        {
            SetupExtensionButtonListeners();
            if (!RequireAuthenticatedUiAccess("notifications"))
            {
                return;
            }

            if (notificationsPanel == null || notificationsContainer == null)
            {
                ShowMessage("Notifications panel is unavailable in this scene.");
                return;
            }

            ShowPanel(notificationsPanel);
            RefreshNotificationsPanel();
        }

        public void RefreshNotificationsPanel()
        {
            if (notificationsContainer == null)
            {
                return;
            }

            ClearListItems(notificationsContainer);

            if (NotificationManager.Instance == null)
            {
                if (notificationsSummaryText != null) notificationsSummaryText.text = "Notifications unavailable";
                RenderListPlaceholder(notificationsContainer, "Notification manager not available.");
                return;
            }

            if (notificationsSummaryText != null)
            {
                notificationsSummaryText.text = "Loading notifications...";
            }
            RenderListPlaceholder(notificationsContainer, "Loading notifications...");
            ShowLoading("Loading notifications...");

            int pending = 2;
            bool notificationsLoadSucceeded = true;
            Action finish = () =>
            {
                pending--;
                if (pending <= 0)
                {
                    HideLoading();
                    if (notificationsLoadSucceeded || ((NotificationManager.Instance.Notifications?.Count ?? 0) > 0))
                    {
                        PopulateNotificationsPanelFromCache();
                    }
                }
            };

            NotificationManager.Instance.LoadSettings(_ => finish());
            NotificationManager.Instance.LoadNotifications(1, false, success =>
            {
                if (!success)
                {
                    notificationsLoadSucceeded = false;
                    ShowMessage("Failed to load notifications");
                    ClearListItems(notificationsContainer);
                    if (notificationsSummaryText != null) notificationsSummaryText.text = "Failed to load notifications";
                    RenderListErrorPlaceholder(notificationsContainer, "Failed to load notifications.");
                }
                finish();
            });
        }

        private void PopulateNotificationsPanelFromCache()
        {
            if (notificationsContainer == null || NotificationManager.Instance == null)
            {
                return;
            }

            ClearListItems(notificationsContainer);

            var notifications = NotificationManager.Instance.Notifications ?? new List<Notification>();
            var settings = NotificationManager.Instance.Settings;

            if (notificationsSummaryText != null)
            {
                string pushState = settings != null ? (settings.push ? "Push ON" : "Push OFF") : "Push ?";
                string inAppState = settings != null ? (settings.inApp ? "In-app ON" : "In-app OFF") : "In-app ?";
                notificationsSummaryText.text = $"Unread: {NotificationManager.Instance.UnreadCount} • {pushState} • {inAppState}";
            }

            foreach (var notification in notifications.Take(30))
            {
                string title = string.IsNullOrWhiteSpace(notification.title) ? "Notification" : notification.title;
                string subtitle = $"{notification.message}\n{FormatRelativeTime(notification.createdAt)}";
                string meta = notification.read ? "READ" : "NEW";
                Color accent = notification.read ? new Color(0.45f, 0.45f, 0.5f) : new Color(1f, 0.8f, 0.2f);
                RenderListDataCard(notificationsContainer, title, subtitle, meta, accent);
            }

            if (notifications.Count == 0)
            {
                RenderListPlaceholder(notificationsContainer, "No notifications yet.");
            }
        }

        public void MarkAllNotificationsAsReadFromUI()
        {
            if (!RequireAuthenticatedUiAccess("notifications", redirectToLogin: false))
            {
                return;
            }

            if (NotificationManager.Instance == null)
            {
                ShowMessage("Notification system unavailable");
                return;
            }

            ShowLoading("Marking notifications as read...");
            NotificationManager.Instance.MarkAllAsRead(success =>
            {
                HideLoading();
                if (!success)
                {
                    ShowMessage("Failed to mark notifications as read");
                    return;
                }

                PopulateNotificationsPanelFromCache();
            });
        }

        #endregion

        #region Report UI

        /// <summary>
        /// Affiche le dialogue de signalement
        /// </summary>
        public void ShowReportDialog(string captureId, Prize prize)
        {
            if (reportPanel != null)
            {
                EnsureReportTypeOptions();
                reportPanel.SetActive(true);
                
                if (submitReportButton != null)
                {
                    submitReportButton.onClick.RemoveAllListeners();
                    submitReportButton.onClick.AddListener(() => SubmitReport(captureId));
                }
            }
        }

        /// <summary>
        /// Soumet un rapport
        /// </summary>
        private void SubmitReport(string captureId)
        {
            if (reportTypeDropdown == null || reportDescriptionInput == null)
                return;

            EnsureReportTypeOptions();
            if (reportTypeDropdown.options == null || reportTypeDropdown.options.Count == 0)
            {
                ShowMessage("Report types are unavailable");
                return;
            }
            
            string issueType = reportTypeDropdown.options[reportTypeDropdown.value].text;
            string description = reportDescriptionInput.text;
            
            StartCoroutine(CaptureController.Instance.ReportCaptureIssue(captureId, issueType, description, (success) =>
            {
                if (success && reportPanel != null)
                {
                    reportPanel.SetActive(false);
                }
            }));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Charge une image depuis une URL
        /// </summary>
        private IEnumerator LoadImageFromURL(string url, Image targetImage)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        /// <summary>
        /// Affiche un dialogue de confirmation
        /// </summary>
        public void ShowConfirmDialog(string title, string message, System.Action onConfirm)
        {
            if (confirmDialog != null)
            {
                confirmDialog.SetActive(true);
                if (confirmTitleText != null) confirmTitleText.text = title;
                if (confirmMessageText != null) confirmMessageText.text = message;
                
                confirmYesButton.onClick.RemoveAllListeners();
                confirmYesButton.onClick.AddListener(() => {
                    confirmDialog.SetActive(false);
                    onConfirm?.Invoke();
                });
                
                confirmNoButton.onClick.RemoveAllListeners();
                confirmNoButton.onClick.AddListener(() => confirmDialog.SetActive(false));
            }
            else
            {
                // Fallback if dialog not set
                onConfirm?.Invoke();
            }
        }

        #endregion
    }
}

