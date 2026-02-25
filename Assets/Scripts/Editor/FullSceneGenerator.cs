using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using TMPro;
using YallaCatch;
using YallaCatch.Managers;
using YallaCatch.UI;
using YallaCatch.API;
using YallaCatch.Core;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Extended Scaffold Generator: Creates a fully playable scene
    /// Menu: YallaCatch > Generate Full Scene
    /// </summary>
    public class FullSceneGenerator : EditorWindow
    {
        [MenuItem("YallaCatch / Generate Full Scene")]
        public static void GenerateFullScene()
        {
            GenerateFullSceneLegacyCompat(true);
        }

        internal static void GenerateUICoreScaffoldOnly()
        {
            GenerateUICoreInline();
        }

        internal static void EnsureExtendedFeaturePanelsAndListPrefabs(Transform canvasT)
        {
            EnsureExtendedFeaturePanels(canvasT);

            CreateListItemPrefab("RewardItem", new[] { "RewardImage", "NameText", "PointsText", "RedeemButton", "FavoriteButton" });
            CreateListItemPrefab("MarketplaceItem", new[] { "ItemImage", "NameText", "PriceText", "BuyButton" });
            CreateListItemPrefab("FriendItem", new[] { "AvatarImage", "NameText", "StatusText", "ProfileButton" });
            CreateListItemPrefab("FriendRequestItem", new[] { "NameText", "AcceptButton", "RejectButton" });
            CreateListItemPrefab("ChallengeItem", new[] { "NameText", "DescriptionText", "ProgressText", "ProgressBar", "RewardText", "Checkmark" });
            CreateListItemPrefab("ClaimItem", new[] { "RewardText", "StatusText", "CodeText", "ViewQRButton" });
            CreateListItemPrefab("PrizeMarker", new[] { "MarkerImage" });
        }

        internal static void WireUIManagerCanvasReferences(Transform canvasT, GameObject managersObj)
        {
            WireUIManagerExtras(canvasT, managersObj);
        }

        internal static void EnsureGameplayPanelAndRuntimeScaffold(Transform canvasT, GameObject coreObj)
        {
            EnsureGameplayPanelShell(canvasT, coreObj);
        }

        internal static void EnsureAREffectsScaffoldForGameplay(Transform canvasT, GameObject coreObj, GameObject effectsObj)
        {
            EnsureAREffectsScaffold(canvasT, coreObj, effectsObj);
        }

        internal static void WireGameplaySceneReferences(Transform canvasT, GameObject managersObj, GameObject coreObj, GameObject effectsObj)
        {
            WireGameplayCoreReferences(canvasT, managersObj, coreObj, effectsObj);
        }

        internal static void GenerateFullSceneLegacyCompat(
            bool promptToSaveModifiedScenes = true,
            bool saveMainScene = true,
            bool overwriteBuildSettings = true)
        {
            // ── Step 1: Run the base UI Core generator ──────────────
            if (promptToSaveModifiedScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[FullSceneGenerator] Generation cancelled by user.");
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GenerateUICoreInline();

            // ── Step 2: Create ALL Managers ─────────────────────────
            GameObject managersObj = GameObject.Find("Managers") ?? new GameObject("Managers");

            // Core Managers
            GetOrAdd<GameManager>(managersObj);
            GetOrAdd<ConfigManager>(managersObj);
            GetOrAdd<SoundManager>(managersObj);
            GetOrAdd<NotificationManager>(managersObj);
            GetOrAdd<OfflineManager>(managersObj);
            GetOrAdd<OfflineQueueManager>(managersObj);
            GetOrAdd<AdManager>(managersObj);
            GetOrAdd<AdMobManager>(managersObj);

            // Game Feature Managers
            GetOrAdd<RewardsManager>(managersObj);
            GetOrAdd<MarketplaceManager>(managersObj);
            GetOrAdd<SocialManager>(managersObj);
            GetOrAdd<PowerUpManager>(managersObj);
            GetOrAdd<ChallengesManager>(managersObj);
            GetOrAdd<ClaimsManager>(managersObj);
            GetOrAdd<AchievementManager>(managersObj);
            GetOrAdd<ARManager>(managersObj);

            // API layer
            GetOrAdd<APIManager>(managersObj);
            GetOrAdd<APIInitializer>(managersObj);

            // ── Step 3: Create Core Game Objects ────────────────────
            GameObject coreObj = GameObject.Find("Core") ?? new GameObject("Core");

            var gps = GetOrAdd<GPSManager>(coreObj);
            var mapCtrl = GetOrAdd<MapController>(coreObj);
            var captureCtrl = GetOrAdd<CaptureController>(coreObj);
            var gameModeManager = GetOrAdd<GameModeManager>(coreObj);
            var cameraLive = GetOrAdd<CameraLiveManager>(coreObj);

            // ── Step 4: Create Effects Object ───────────────────────
            GameObject effectsObj = GameObject.Find("Effects") ?? new GameObject("Effects");
            GetOrAdd<CaptureAnimationController>(effectsObj);

            // ── Step 5: Find the Canvas ─────────────────────────────
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found. Run 'Auto Generate UI Core' first.");
                return;
            }

            Transform canvasT = canvas.transform;

            // ── Step 5b: Create panels that UIScaffoldGenerator missed ──
            // capturePanel (referenced in UIManager.cs L23)
            GameObject capturePanel = canvasT.Find("CapturePanel")?.gameObject;
            if (capturePanel == null)
            {
                capturePanel = CreatePanel(canvasT, "CapturePanel");
                capturePanel.SetActive(false);
            }
            // settingsPanel (referenced in UIManager.cs L26)
            GameObject settingsPanel = canvasT.Find("SettingsPanel")?.gameObject;
            if (settingsPanel == null)
            {
                settingsPanel = CreatePanel(canvasT, "SettingsPanel");
            }
            // Clear old contents to apply premium design if running over existing canvas
            for (int i = settingsPanel.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(settingsPanel.transform.GetChild(i).gameObject);
            }

            // Background
            var sBgImage = settingsPanel.GetComponent<Image>();
            if (sBgImage != null) sBgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f); 

            CreateText(settingsPanel.transform, "SettingsTitle", "SYSTEM PREFERENCES", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);

            // Main Card
            GameObject settingsCard = CreatePanel(settingsPanel.transform, "SettingsCard");
            var cardRect = settingsCard.GetComponent<RectTransform>();
            cardRect.anchoredPosition = new Vector2(0, 150);
            cardRect.sizeDelta = new Vector2(900, 1100);
            settingsCard.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            
            CreateText(settingsCard.transform, "AudioHeader", "AUDIO & HAPTICS", new Vector2(-230, 450), new Color(0.5f, 0.7f, 1f), 24);
            CreateButton(settingsCard.transform, "SoundToggle", "MASTER AUDIO [ ON ]", new Vector2(0, 350), new Color(0.15f, 0.15f, 0.2f), new Vector2(800, 120), 30);
            CreateButton(settingsCard.transform, "HapticsToggle", "HAPTIC FEEDBACK [ ON ]", new Vector2(0, 200), new Color(0.15f, 0.15f, 0.2f), new Vector2(800, 120), 30);

            CreateText(settingsCard.transform, "PrivacyHeader", "PRIVACY & LOCATION", new Vector2(-210, 50), new Color(0.5f, 0.7f, 1f), 24);
            CreateButton(settingsCard.transform, "NotifToggle", "PUSH NOTIFICATIONS [ ON ]", new Vector2(0, -50), new Color(0.15f, 0.15f, 0.2f), new Vector2(800, 120), 30);
            CreateButton(settingsCard.transform, "LocationToggle", "GPS TRACKING [ ON ]", new Vector2(0, -200), new Color(0.15f, 0.15f, 0.2f), new Vector2(800, 120), 30);

            CreateButton(settingsCard.transform, "LogoutButton", "DISCONNECT ACCOUNT", new Vector2(0, -400), new Color(0.8f, 0.2f, 0.2f), new Vector2(800, 120), 30);

            CreateButton(settingsPanel.transform, "CloseSettingsButton", "← NAV MENU", new Vector2(0, -700), new Color(0.2f, 0.2f, 0.3f), new Vector2(400, 100), 30);
            settingsPanel.SetActive(false);

            // ── Step 6: Populate Main Menu Panel + Wire Navigation ──
            GameObject mainMenuPanel = canvasT.Find("MainMenuPanel")?.gameObject;
            if (mainMenuPanel != null)
            {
                // Background Aesthetic (Glassmorphism Dark)
                var bgImage = mainMenuPanel.GetComponent<Image>();
                if (bgImage != null) bgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f);

                // --- 1. HEADER ---
                GeneratedSceneUIFactory.CreateImage(mainMenuPanel.transform, "GameLogo", new Vector2(0, 800), new Vector2(600, 150), "UI/GameLogo");
                CreateText(mainMenuPanel.transform, "SubtitleText", "COMMERCIAL GAMING ECOSYSTEM", new Vector2(0, 740), new Color(0.5f, 0.5f, 0.6f), 24);

                // Settings dock (Top Right)
                CreateButton(mainMenuPanel.transform, "SettingsButton", "⚙️", new Vector2(400, 800), new Color(0.15f, 0.15f, 0.2f), new Vector2(80, 80));

                // --- 2. PREMIUM PLAYER CARD ---
                GameObject playerCard = CreatePanel(mainMenuPanel.transform, "PlayerCard");
                var pcRect = playerCard.GetComponent<RectTransform>();
                pcRect.anchoredPosition = new Vector2(0, 480);
                pcRect.sizeDelta = new Vector2(850, 260);
                playerCard.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.6f); // Panel color

                // Avatar Placeholder
                GameObject avatar = new GameObject("Avatar");
                avatar.transform.SetParent(playerCard.transform, false);
                var avRect = avatar.AddComponent<RectTransform>();
                avRect.anchoredPosition = new Vector2(-280, 10);
                avRect.sizeDelta = new Vector2(160, 160);
                var avImg = avatar.AddComponent<Image>();
                avImg.color = new Color(0.2f, 0.2f, 0.3f); 
                
                // Info Texts
                CreateText(playerCard.transform, "UsernameText", "Premium Player", new Vector2(50, 70), Color.white, 40);
                CreateText(playerCard.transform, "LevelText", "Level 1 (Bronze)", new Vector2(50, 20), new Color(0.4f, 0.8f, 0.5f), 28);
                CreateText(playerCard.transform, "PointsText", "1,250 Pts", new Vector2(50, -30), new Color(1f, 0.8f, 0.2f), 34);

                // Premium XP Slider
                GameObject xpObj = new GameObject("ExperienceSlider");
                xpObj.transform.SetParent(playerCard.transform, false);
                var xpRect = xpObj.AddComponent<RectTransform>();
                xpRect.anchoredPosition = new Vector2(0, -90);
                xpRect.sizeDelta = new Vector2(750, 16);
                var xpSlider = xpObj.AddComponent<Slider>();
                xpSlider.value = 0.45f;
                // Slider Background
                var xpBgObj = new GameObject("Background");
                xpBgObj.transform.SetParent(xpObj.transform, false);
                var xpBgRect = xpBgObj.AddComponent<RectTransform>();
                xpBgRect.anchorMin = Vector2.zero; xpBgRect.anchorMax = Vector2.one; xpBgRect.sizeDelta = Vector2.zero;
                xpBgObj.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f);
                // Slider Fill
                var fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(xpObj.transform, false);
                var faRect = fillArea.AddComponent<RectTransform>();
                faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one; faRect.sizeDelta = Vector2.zero;
                var fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(fillArea.transform, false);
                var fillRect = fillObj.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.sizeDelta = Vector2.zero;
                fillObj.AddComponent<Image>().color = new Color(0.1f, 0.8f, 0.4f); // Emerald
                xpSlider.fillRect = fillRect;

                // --- 3. ACTION CENTER ---
                CreateButton(mainMenuPanel.transform, "PlayButton", "ENTER WORLD", new Vector2(0, 140), new Color(0.1f, 0.75f, 0.35f), new Vector2(850, 140), 45);

                // --- 4. NAVIGATION GRID ---
                float gridY = -120f;
                float spacingX = 290f;
                float spacingY = 160f;
                Vector2 btnSize = new Vector2(270, 140);
                
                // Row 1
                CreateButton(mainMenuPanel.transform, "RewardsButton", "Rewards", new Vector2(-spacingX, gridY), new Color(0.8f, 0.5f, 0.1f), btnSize);
                CreateButton(mainMenuPanel.transform, "MarketplaceButton", "Marketplace", new Vector2(0, gridY), new Color(0.2f, 0.4f, 0.8f), btnSize);
                CreateButton(mainMenuPanel.transform, "ChallengesButton", "⚔️ Quests", new Vector2(spacingX, gridY), new Color(0.8f, 0.3f, 0.3f), btnSize);
                
                // Row 2
                CreateButton(mainMenuPanel.transform, "SocialButton", "Social", new Vector2(-spacingX, gridY - spacingY), new Color(0.5f, 0.3f, 0.8f), btnSize);
                CreateButton(mainMenuPanel.transform, "ClaimsButton", "Claims", new Vector2(0, gridY - spacingY), new Color(0.5f, 0.4f, 0.3f), btnSize);
                CreateButton(mainMenuPanel.transform, "ProfileButton", "Profile", new Vector2(spacingX, gridY - spacingY), new Color(0.3f, 0.3f, 0.4f), btnSize);

                // Row 3 (extended feature access)
                CreateButton(mainMenuPanel.transform, "AchievementsButton", "ACHIEVEMENTS", new Vector2(-145f, gridY - (spacingY * 2f)), new Color(0.28f, 0.52f, 0.24f), new Vector2(415, 120), 30);
                CreateButton(mainMenuPanel.transform, "NotificationsButton", "NOTIFICATIONS", new Vector2(145f, gridY - (spacingY * 2f)), new Color(0.22f, 0.35f, 0.66f), new Vector2(415, 120), 30);
            }

            // ── Step 6b: Populate Rewards Panel with scroll container ──
            GameObject rewardsPanel = canvasT.Find("RewardsPanel")?.gameObject;
            if (rewardsPanel != null)
            {
                // Background
                var rBgImage = rewardsPanel.GetComponent<Image>();
                if (rBgImage != null) rBgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f); 

                CreateText(rewardsPanel.transform, "RewardsTitle", "ARSENAL", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);

                // Top Actions Row
                CreateTMPDropdown(
                    rewardsPanel.transform,
                    "CategoryDropdown",
                    new Vector2(-220, 680),
                    new Vector2(400, 80),
                    new List<string> { "All" }
                );

                CreateButton(rewardsPanel.transform, "FavoritesButton", "FAVORITES", new Vector2(220, 680), new Color(0.8f, 0.6f, 0.1f), new Vector2(400, 80), 30);

                // Main Scroll Container (Wider, taller)
                CreateScrollContainer(rewardsPanel.transform, "RewardsContainer", new Vector2(0, 100), new Vector2(950, 1000));

                CreateButton(rewardsPanel.transform, "CloseRewardsButton", "<- NAV MENU", new Vector2(0, -780), new Color(0.2f, 0.2f, 0.3f), new Vector2(500, 100), 32);
                rewardsPanel.SetActive(false);
            }

            // ── Step 6c: Populate Marketplace Panel ──────────────────
            GameObject marketplacePanel = canvasT.Find("MarketplacePanel")?.gameObject;
            if (marketplacePanel != null)
            {
                var mBgImage = marketplacePanel.GetComponent<Image>();
                if (mBgImage != null) mBgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f); 

                CreateText(marketplacePanel.transform, "MarketplaceTitle", "GLOBAL MARKET", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);

                // Inventory Swap Button
                CreateButton(marketplacePanel.transform, "InventoryButton", "MY INVENTORY", new Vector2(0, 680), new Color(0.2f, 0.4f, 0.8f), new Vector2(850, 80), 30);

                // Scroll Container
                CreateScrollContainer(marketplacePanel.transform, "MarketplaceContainer", new Vector2(0, 100), new Vector2(950, 1000));

                CreateButton(marketplacePanel.transform, "CloseMarketButton", "← NAV MENU", new Vector2(0, -780), new Color(0.2f, 0.2f, 0.3f), new Vector2(500, 100), 32);
                marketplacePanel.SetActive(false);
            }

            // ── Step 6d: Populate Social Panel ───────────────────────
            GameObject socialPanelObj = canvasT.Find("SocialPanel")?.gameObject;
            if (socialPanelObj != null)
            {
                var socialBgImage = socialPanelObj.GetComponent<Image>();
                if (socialBgImage != null) socialBgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f); 

                CreateText(socialPanelObj.transform, "SocialTitle", "NETWORK", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);

                // Top Actions
                CreateButton(socialPanelObj.transform, "LeaderboardButton", "GLOBAL LEADERBOARD", new Vector2(0, 680), new Color(0.8f, 0.6f, 0.1f), new Vector2(850, 80), 30);

                // Friends List
                CreateText(socialPanelObj.transform, "FriendsLabel", "ACTIVE AGENTS", new Vector2(-250, 580), new Color(0.4f, 0.8f, 0.5f), 24);
                CreateScrollContainer(socialPanelObj.transform, "FriendsContainer", new Vector2(0, 310), new Vector2(950, 480));

                // Requests List
                CreateText(socialPanelObj.transform, "RequestsLabel", "PENDING UPLINKS", new Vector2(-220, 10), new Color(1f, 0.8f, 0.2f), 24);
                CreateScrollContainer(socialPanelObj.transform, "RequestsContainer", new Vector2(0, -260), new Vector2(950, 480));

                CreateButton(socialPanelObj.transform, "CloseSocialButton", "← NAV MENU", new Vector2(0, -780), new Color(0.2f, 0.2f, 0.3f), new Vector2(500, 100), 32);
                socialPanelObj.SetActive(false);
            }

            // ── Step 6e: Populate Challenges Panel ───────────────────
            GameObject challengesPanelObj = canvasT.Find("ChallengesPanel")?.gameObject;
            if (challengesPanelObj != null)
            {
                var cBgImage = challengesPanelObj.GetComponent<Image>();
                if (cBgImage != null) cBgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f); 

                CreateText(challengesPanelObj.transform, "ChallengesTitle", "FIELD MANSIONS", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);
                CreateText(challengesPanelObj.transform, "ChallengesProgressText", "OBJECTIVES CLEARED: 0/3", new Vector2(0, 720), new Color(1f, 0.8f, 0.2f), 28);

                CreateScrollContainer(challengesPanelObj.transform, "ChallengesContainer", new Vector2(0, 50), new Vector2(950, 1100));

                CreateButton(challengesPanelObj.transform, "CloseChallengesButton", "← NAV MENU", new Vector2(0, -780), new Color(0.2f, 0.2f, 0.3f), new Vector2(500, 100), 32);
                challengesPanelObj.SetActive(false);
            }

            // ── Step 6f: Populate Claims Panel ───────────────────────
            GameObject claimsPanelObj = canvasT.Find("ClaimsPanel")?.gameObject;
            if (claimsPanelObj == null)
            {
                claimsPanelObj = CreatePanel(canvasT, "ClaimsPanel");
            }
            if (claimsPanelObj.transform.childCount == 0 || claimsPanelObj.transform.Find("ClaimsTitle")?.GetComponent<TextMeshProUGUI>()?.text == "MY CLAIMS")
            {
                // Clear out stale basic UI
                for (int i = claimsPanelObj.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(claimsPanelObj.transform.GetChild(i).gameObject);
                }

                var cBgImage = claimsPanelObj.GetComponent<Image>();
                if (cBgImage != null) cBgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f); 

                CreateText(claimsPanelObj.transform, "ClaimsTitle", "SECURE WALLET", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);

                CreateScrollContainer(claimsPanelObj.transform, "ClaimsContainer", new Vector2(0, 50), new Vector2(950, 1100));

                CreateButton(claimsPanelObj.transform, "CloseClaimsButton", "← NAV MENU", new Vector2(0, -780), new Color(0.2f, 0.2f, 0.3f), new Vector2(500, 100), 32);
                claimsPanelObj.SetActive(false);
            }

            // ── Step 7: Populate Game Panel ──────────────────────────
            GameObject gamePanel = canvasT.Find("GamePanel")?.gameObject;
            if (gamePanel != null)
            {
                // Top HUD Bar (Glassmorphism)
                GameObject topBar = CreatePanel(gamePanel.transform, "TopHUDBar");
                var tbRect = topBar.GetComponent<RectTransform>();
                tbRect.anchorMin = new Vector2(0, 1); tbRect.anchorMax = new Vector2(1, 1);
                tbRect.pivot = new Vector2(0.5f, 1);
                tbRect.anchoredPosition = Vector2.zero;
                tbRect.sizeDelta = new Vector2(0, 200); // Fills top
                topBar.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

                // Back to Menu Button (Top Left)
                CreateButton(topBar.transform, "BackToMenuButton", "☰ Menu", new Vector2(-360, -100), new Color(0.15f, 0.15f, 0.2f), new Vector2(240, 100), 36);

                // Stats (Center)
                CreateText(topBar.transform, "GameLevelText", "Level 1 (Bronze)", new Vector2(0, -60), new Color(0.4f, 0.8f, 0.5f), 28);
                CreateText(topBar.transform, "GamePointsText", "1,250 Pts", new Vector2(0, -130), new Color(1f, 0.8f, 0.2f), 42);

                // Live Signal Indicator (Top Right)
                GameObject liveSignalBg = new GameObject("LiveSignalBg");
                liveSignalBg.transform.SetParent(topBar.transform, false);
                var lsBgRect = liveSignalBg.AddComponent<RectTransform>();
                lsBgRect.anchorMin = new Vector2(0.5f, 0.5f); lsBgRect.anchorMax = new Vector2(0.5f, 0.5f);
                lsBgRect.anchoredPosition = new Vector2(400, -100);
                lsBgRect.sizeDelta = new Vector2(100, 100);
                var lsBgImg = liveSignalBg.AddComponent<Image>();
                lsBgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f); 

                // Ad Button (Next to Signal)
                CreateButton(topBar.transform, "AdButton", "AD", new Vector2(280, -100), new Color(0.1f, 0.6f, 1f), new Vector2(170, 80), 28);

                GameObject liveSignal = new GameObject("LiveSignalIndicator");
                liveSignal.transform.SetParent(liveSignalBg.transform, false);
                var lsRect = liveSignal.AddComponent<RectTransform>();
                lsRect.anchoredPosition = Vector2.zero;
                lsRect.sizeDelta = new Vector2(40, 40);
                var lsImg = liveSignal.AddComponent<Image>();
                lsImg.color = new Color(0.1f, 0.9f, 0.4f); // Emerald pulsing signal

                // Power-Up Indicator (Directly below Top Bar)
                GameObject powerUpInd = CreatePanel(gamePanel.transform, "PowerUpIndicator");
                var puRect = powerUpInd.GetComponent<RectTransform>();
                puRect.anchorMin = new Vector2(0.5f, 1); puRect.anchorMax = new Vector2(0.5f, 1);
                puRect.pivot = new Vector2(0.5f, 1);
                puRect.anchoredPosition = new Vector2(0, -220);
                puRect.sizeDelta = new Vector2(450, 80);
                powerUpInd.GetComponent<Image>().color = new Color(0.6f, 0.3f, 0.9f, 0.95f); // Amethyst

                GameObject powerUpIconObj = new GameObject("PowerUpIcon");
                powerUpIconObj.transform.SetParent(powerUpInd.transform, false);
                RectTransform powerUpIconRect = powerUpIconObj.AddComponent<RectTransform>();
                powerUpIconRect.anchorMin = new Vector2(0f, 0.5f);
                powerUpIconRect.anchorMax = new Vector2(0f, 0.5f);
                powerUpIconRect.pivot = new Vector2(0f, 0.5f);
                powerUpIconRect.anchoredPosition = new Vector2(16f, 0f);
                powerUpIconRect.sizeDelta = new Vector2(48f, 48f);
                Image powerUpIconImage = powerUpIconObj.AddComponent<Image>();
                powerUpIconImage.color = new Color(1f, 0.9f, 0.35f, 1f);

                TextMeshProUGUI powerUpTimer = CreateText(powerUpInd.transform, "PowerUpTimerText", "RADAR 00:59", Vector2.zero, Color.white, 28);
                RectTransform powerUpTimerRect = powerUpTimer.GetComponent<RectTransform>();
                if (powerUpTimerRect != null)
                {
                    powerUpTimerRect.anchoredPosition = new Vector2(28f, 0f);
                }
                powerUpInd.SetActive(false); 

                // Mode toggle button (Camera / Map)
                CreateButton(gamePanel.transform, "ModeToggleButton", "CAM / MAP", new Vector2(380, -850), new Color(0.2f, 0.2f, 0.25f), new Vector2(250, 120), 40);

                // Main Scan Button (Bottom Center)
                CreateButton(gamePanel.transform, "MockCaptureBtn", "SCAN TARGET", new Vector2(0, -800), new Color(0.1f, 0.8f, 0.4f), new Vector2(400, 150), 40);
                EnsureGameplayRuntimeScaffold(gamePanel.transform, coreObj);
            }

            // ── Step 8: Populate Profile Panel ──────────────────────
            GameObject profilePanel = canvasT.Find("ProfilePanel")?.gameObject;
            if (profilePanel != null)
            {
                // Background
                var bgImage = profilePanel.GetComponent<Image>();
                if (bgImage != null) bgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f);
                
                CreateText(profilePanel.transform, "ProfileTitle", "COMMAND CENTER", new Vector2(0, 800), new Color(0.9f, 0.9f, 0.95f), 55);

                // Central ID Card Panel
                GameObject idCard = CreatePanel(profilePanel.transform, "PlayerIDCard");
                var idRect = idCard.GetComponent<RectTransform>();
                idRect.anchoredPosition = new Vector2(0, 150);
                idRect.sizeDelta = new Vector2(900, 1100);
                idCard.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
                
                // Avatar Frame (Golden/Amber)
                GameObject avatarFrame = new GameObject("AvatarFrame");
                avatarFrame.transform.SetParent(idCard.transform, false);
                var frameRect = avatarFrame.AddComponent<RectTransform>();
                frameRect.anchoredPosition = new Vector2(0, 350);
                frameRect.sizeDelta = new Vector2(260, 260);
                avatarFrame.AddComponent<Image>().color = new Color(0.8f, 0.6f, 0.1f);
                
                // Avatar placeholder
                GameObject avatarObj = new GameObject("AvatarImage");
                avatarObj.transform.SetParent(avatarFrame.transform, false);
                var avRect = avatarObj.AddComponent<RectTransform>();
                avRect.anchoredPosition = Vector2.zero;
                avRect.sizeDelta = new Vector2(240, 240);
                var avImg = avatarObj.AddComponent<Image>();
                avImg.color = new Color(0.15f, 0.15f, 0.2f);

                CreateText(idCard.transform, "NameText", "AGENT ALPHA", new Vector2(0, 150), Color.white, 48);
                CreateText(idCard.transform, "LevelText", "CLASSIFICATION: BRONZE", new Vector2(0, 90), new Color(0.4f, 0.8f, 0.5f), 28);

                // Divider
                GameObject div1 = new GameObject("Divider1");
                div1.transform.SetParent(idCard.transform, false);
                div1.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
                div1.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 2);
                div1.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f, 0.5f);

                // Technical specs section
                CreateText(idCard.transform, "TechSpecsTitle", "TERMINAL DIAGNOSTICS", new Vector2(0, -10), new Color(0.5f, 0.7f, 1f), 22);
                CreateText(idCard.transform, "LastIpText", "IP NODE // 192.168.x.x", new Vector2(-220, -70), new Color(0.6f, 0.6f, 0.7f), 24);
                CreateText(idCard.transform, "DeviceText", "UPLINK // iOS Device", new Vector2(220, -70), new Color(0.6f, 0.6f, 0.7f), 24);
                CreateText(idCard.transform, "LastActiveText", "LAST SIGNAL: 00:00:00", new Vector2(0, -120), new Color(0.8f, 0.4f, 0.4f), 24);

                // Divider 2
                GameObject div2 = new GameObject("Divider2");
                div2.transform.SetParent(idCard.transform, false);
                div2.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, -170);
                div2.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 2);
                div2.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f, 0.5f);

                // Game stats section
                CreateText(idCard.transform, "GameStatsTitle", "FIELD OPERATIONS", new Vector2(0, -220), new Color(0.5f, 0.7f, 1f), 22);
                CreateText(idCard.transform, "ClaimsText", "TARGETS\n0", new Vector2(-200, -300), Color.white, 36);
                CreateText(idCard.transform, "StreakText", "STREAK\n0 DAYS", new Vector2(200, -300), Color.white, 36);

                // Buttons
                CreateButton(profilePanel.transform, "LogoutButton", "DISCONNECT", new Vector2(0, -550), new Color(0.8f, 0.2f, 0.2f), new Vector2(400, 100), 30);
                CreateButton(profilePanel.transform, "CloseProfileButton", "← NAV MENU", new Vector2(0, -700), new Color(0.2f, 0.2f, 0.3f), new Vector2(400, 100), 30);
            }

            // ── Step 9: Create List Item Prefabs ────────────────────
            EnsureExtendedFeaturePanels(canvasT);

            CreateListItemPrefab("RewardItem", new string[] { "RewardImage", "NameText", "PointsText", "RedeemButton", "FavoriteButton" });
            CreateListItemPrefab("MarketplaceItem", new string[] { "ItemImage", "NameText", "PriceText", "BuyButton" });
            CreateListItemPrefab("FriendItem", new string[] { "AvatarImage", "NameText", "StatusText", "ProfileButton" });
            CreateListItemPrefab("FriendRequestItem", new string[] { "NameText", "AcceptButton", "RejectButton" });
            CreateListItemPrefab("ChallengeItem", new string[] { "NameText", "DescriptionText", "ProgressText", "ProgressBar", "RewardText", "Checkmark" });
            CreateListItemPrefab("ClaimItem", new string[] { "RewardText", "StatusText", "CodeText", "ViewQRButton" });
            CreateListItemPrefab("PrizeMarker", new string[] { "MarkerImage" });

            // ── Step 10: Setup Capture Dialog ───────────────────────
            GameObject captureDialog = canvasT.Find("CaptureDialog")?.gameObject;
            if (captureDialog == null)
            {
                captureDialog = CreatePanel(canvasT, "CaptureDialog");
                captureDialog.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.06f, 0.98f); // Intense dark

                CreateText(captureDialog.transform, "CaptureHeader", "TARGET ACQUIRED", new Vector2(0, 700), new Color(0.1f, 0.9f, 0.4f), 55);
                CreateText(captureDialog.transform, "PrizeNameText", "Prize Name", new Vector2(0, 500), Color.white, 48);
                CreateText(captureDialog.transform, "PrizeDescriptionText", "Description...", new Vector2(0, 380), new Color(0.7f, 0.7f, 0.75f), 28);
                CreateText(captureDialog.transform, "PrizePointsText", "+100 pts", new Vector2(0, -350), new Color(1f, 0.8f, 0.2f), 42);

                // Prize image frame
                GameObject piObj = new GameObject("PrizeImage");
                piObj.transform.SetParent(captureDialog.transform, false);
                var piRect = piObj.AddComponent<RectTransform>();
                piRect.anchoredPosition = new Vector2(0, 30);
                piRect.sizeDelta = new Vector2(350, 350);
                piObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

                CreateButton(captureDialog.transform, "CaptureButton", "INITIATE CAPTURE SEQUENCE", new Vector2(0, -550), new Color(0.1f, 0.7f, 0.3f), new Vector2(850, 140), 40);
                CreateButton(captureDialog.transform, "CancelButton", "ABORT", new Vector2(0, -750), new Color(0.6f, 0.2f, 0.2f), new Vector2(400, 100), 32);

                captureDialog.SetActive(false);
            }

            // ── Step 11: Message Dialog ──────────────────────────────
            GameObject messageDialog = canvasT.Find("MessageDialog")?.gameObject;
            if (messageDialog == null)
            {
                messageDialog = CreatePanel(canvasT, "MessageDialog");
                messageDialog.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.7f);

                CreateText(messageDialog.transform, "MessageHeader", "SYSTEM MESSAGE", new Vector2(0, 300), new Color(0.5f, 0.7f, 1f), 36);
                CreateText(messageDialog.transform, "MessageText", "Message Content...", new Vector2(0, 100), Color.white, 32);
                CreateButton(messageDialog.transform, "MessageOkButton", "ACKNOWLEDGE", new Vector2(0, -200), new Color(0.2f, 0.4f, 0.8f), new Vector2(500, 120), 36);
                
                messageDialog.SetActive(false);
            }

            // ── Step 12: Confirm Dialog ─────────────────────────────
            GameObject confirmDialog = canvasT.Find("ConfirmDialog")?.gameObject;
            if (confirmDialog == null)
            {
                confirmDialog = CreatePanel(canvasT, "ConfirmDialog");
                confirmDialog.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.7f);

                CreateText(confirmDialog.transform, "ConfirmTitleText", "AUTHORIZATION REQUIRED", new Vector2(0, 300), new Color(1f, 0.6f, 0.1f), 36);
                CreateText(confirmDialog.transform, "ConfirmMessageText", "Proceed?", new Vector2(0, 100), Color.white, 32);
                
                CreateButton(confirmDialog.transform, "ConfirmYesButton", "AUTHORIZE", new Vector2(-250, -200), new Color(0.1f, 0.7f, 0.3f), new Vector2(400, 120), 36);
                CreateButton(confirmDialog.transform, "ConfirmNoButton", "CANCEL", new Vector2(250, -200), new Color(0.7f, 0.2f, 0.2f), new Vector2(400, 120), 36);
                
                confirmDialog.SetActive(false);
            }

            // ── Step 13: Loading Overlay ─────────────────────────────
            GameObject loadingOverlay = canvasT.Find("LoadingOverlay")?.gameObject;
            if (loadingOverlay == null)
            {
                loadingOverlay = CreatePanel(canvasT, "LoadingOverlay");
                loadingOverlay.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.95f); // Deep dark

                CreateText(loadingOverlay.transform, "LoadingText", "SYNCING WITH UPLINK...", Vector2.zero, new Color(0.4f, 0.8f, 0.5f), 32);
                loadingOverlay.SetActive(false);
            }

            // ── Step 14: Report Dialog ───────────────────────────────
            GameObject reportDialog = canvasT.Find("ReportPanel")?.gameObject;
            if (reportDialog == null)
            {
                reportDialog = CreatePanel(canvasT, "ReportPanel");
                reportDialog.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.7f);

                CreateText(reportDialog.transform, "ReportTitle", "SUBMIT FIELD REPORT", new Vector2(0, 400), new Color(0.9f, 0.9f, 0.95f), 48);

                // Dropdown for report type
                CreateTMPDropdown(
                    reportDialog.transform,
                    "ReportTypeDropdown",
                    new Vector2(0, 200),
                    new Vector2(600, 80),
                    new List<string> { "Incorrect Prize Location", "Prize Not Visible", "Duplicate Prize", "Other" }
                );

                // Input for description
                var descInput = CreateInputField(reportDialog.transform, "ReportDescriptionInput", new Vector2(0, -50));

                CreateButton(reportDialog.transform, "SubmitReportButton", "TRANSMIT REPORT", new Vector2(0, -300), new Color(0.8f, 0.3f, 0.2f), new Vector2(600, 100), 30);
                CreateButton(reportDialog.transform, "CloseReportButton", "CANCEL", new Vector2(0, -450), new Color(0.3f, 0.3f, 0.4f), new Vector2(400, 80), 28);
                reportDialog.SetActive(false);
            }

            // ── Step 15: QR Code Panel ───────────────────────────────
            GameObject qrPanel = canvasT.Find("QRCodePanel")?.gameObject;
            if (qrPanel == null)
            {
                qrPanel = CreatePanel(canvasT, "QRCodePanel");
                qrPanel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.7f);

                CreateText(qrPanel.transform, "QRTitle", "SECURITY CLEARANCE CODE", new Vector2(0, 350), new Color(0.9f, 0.9f, 0.95f), 42);

                GameObject qrImg = new GameObject("QRCodeImage");
                qrImg.transform.SetParent(qrPanel.transform, false);
                var qrRect = qrImg.AddComponent<RectTransform>();
                qrRect.anchoredPosition = new Vector2(0, 50);
                qrRect.sizeDelta = new Vector2(400, 400);
                qrImg.AddComponent<RawImage>();

                CreateButton(qrPanel.transform, "CloseQRButton", "CLOSE TERMINAL", new Vector2(0, -300), new Color(0.3f, 0.3f, 0.4f), new Vector2(500, 100), 32);
                qrPanel.SetActive(false);
            }

            // ── Step 16: Wire all SeralizedObject references ────────
            EnsureAREffectsScaffold(canvasT, coreObj, effectsObj);
            WireUIManagerExtras(canvasT, managersObj);
            WireGameplayCoreReferences(canvasT, managersObj, coreObj, effectsObj);

            // ── Step 17: Save Scene ─────────────────────────────────
            string scenePath = "Assets/Scenes/MainScene.unity";
            if (saveMainScene)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
                    System.IO.Path.Combine(Application.dataPath, "../", scenePath)));
                EditorSceneManager.SaveScene(
                    EditorSceneManager.GetActiveScene(),
                    scenePath
                );
            }

            if (saveMainScene && overwriteBuildSettings)
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(scenePath, true)
                };
            }

            if (saveMainScene)
            {
                Debug.Log("<color=green>YALLACATCH FULL SCENE GENERATED: MainScene.unity saved with all managers, panels, and prefabs.</color>");
            }
            else
            {
                Debug.Log("<color=green>YALLACATCH FULL SCENE GENERATED (compat transient mode): current scene populated without saving MainScene/build settings.</color>");
            }
        }

        /// <summary>
        /// Inlined version of the former UI core scaffold so FullSceneGenerator is self-sufficient.
        /// Kept separate for readability while still using a single menu command.
        /// </summary>
        private static void GenerateUICoreInline()
        {
            // 1. EventSystem
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // 2. Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2b. Add Premium Background
            if (canvas.transform.Find("PremiumBackground") == null)
            {
                GeneratedSceneUIFactory.CreateBackground(canvas.transform, "PremiumBackground", "UI/PremiumBackground");
            }

            // 3. Managers root + core components needed by auth/base UI
            GameObject managersObj = GameObject.Find("Managers") ?? new GameObject("Managers");
            GetOrAdd<ThemeManager>(managersObj);
            AuthManager authManager = GetOrAdd<AuthManager>(managersObj);
            UIManager uiManager = GetOrAdd<UIManager>(managersObj);
            GetOrAdd<APIClient>(managersObj);

            // 4. Root panels (base shell)
            GameObject loginPanel = GetOrCreateRootPanel(canvas.transform, "LoginPanel");
            GameObject registerPanel = GetOrCreateRootPanel(canvas.transform, "RegisterPanel");
            GameObject mainMenuPanel = GetOrCreateRootPanel(canvas.transform, "MainMenuPanel");
            GameObject rewardsPanel = GetOrCreateRootPanel(canvas.transform, "RewardsPanel");
            GameObject marketplacePanel = GetOrCreateRootPanel(canvas.transform, "MarketplacePanel");
            GameObject socialPanel = GetOrCreateRootPanel(canvas.transform, "SocialPanel");
            GameObject challengesPanel = GetOrCreateRootPanel(canvas.transform, "ChallengesPanel");
            GameObject profilePanel = GetOrCreateRootPanel(canvas.transform, "ProfilePanel");
            GameObject gamePanel = GetOrCreateRootPanel(canvas.transform, "GamePanel");
            GameObject inventoryPanel = GetOrCreateRootPanel(canvas.transform, "InventoryPanel");
            GameObject leaderboardPanel = GetOrCreateRootPanel(canvas.transform, "LeaderboardPanel");
            GameObject achievementsPanel = GetOrCreateRootPanel(canvas.transform, "AchievementsPanel");
            GameObject notificationsPanel = GetOrCreateRootPanel(canvas.transform, "NotificationsPanel");

            // 5. Login/Register panel contents
            ClearChildren(loginPanel.transform);
            ClearChildren(registerPanel.transform);

            TMP_InputField emailInput = CreateAuthInputField(loginPanel.transform, "EmailInput", new Vector2(0, 100));
            TMP_InputField passwordInput = CreateAuthInputField(loginPanel.transform, "PasswordInput", new Vector2(0, 0));
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            Button loginBtn = CreateButton(loginPanel.transform, "LoginButton", "LOGIN", new Vector2(0, -100), new Color(0.2f, 0.6f, 0.8f), new Vector2(600, 80), 36);
            Button guestBtn = CreateButton(loginPanel.transform, "GuestLoginButton", "GUEST", new Vector2(0, -200), new Color(0.2f, 0.6f, 0.8f), new Vector2(600, 80), 36);
            Button showRegBtn = CreateButton(loginPanel.transform, "ShowRegisterButton", "CREATE ACCOUNT", new Vector2(0, -300), new Color(0.2f, 0.6f, 0.8f), new Vector2(600, 80), 36);
            TextMeshProUGUI errorText = CreateText(loginPanel.transform, "ErrorText", "", new Vector2(0, 200), Color.red, 28);
            GameObject loadingIndicator = CreatePanel(loginPanel.transform, "LoadingIndicator");
            Image loadingIndicatorImage = loadingIndicator.GetComponent<Image>();
            if (loadingIndicatorImage != null)
            {
                loadingIndicatorImage.color = new Color(0f, 0f, 0f, 0.5f);
            }
            loadingIndicator.SetActive(false);

            TMP_InputField regUsernameInput = CreateAuthInputField(registerPanel.transform, "UsernameInput", new Vector2(0, 150));
            TMP_InputField regEmailInput = CreateAuthInputField(registerPanel.transform, "EmailInput", new Vector2(0, 50));
            TMP_InputField regPasswordInput = CreateAuthInputField(registerPanel.transform, "PasswordInput", new Vector2(0, -50));
            regPasswordInput.contentType = TMP_InputField.ContentType.Password;
            Button regBtn = CreateButton(registerPanel.transform, "RegisterButton", "REGISTER", new Vector2(0, -150), new Color(0.2f, 0.6f, 0.8f), new Vector2(600, 80), 36);
            Button showLogBtn = CreateButton(registerPanel.transform, "ShowLoginButton", "BACK", new Vector2(0, -250), new Color(0.2f, 0.6f, 0.8f), new Vector2(600, 80), 36);
            registerPanel.SetActive(false);

            // 6. Wire AuthManager
            SerializedObject authSO = new SerializedObject(authManager);
            SetRef(authSO, "loginEmailInput", emailInput);
            SetRef(authSO, "loginPasswordInput", passwordInput);
            SetRef(authSO, "loginButton", loginBtn);
            SetRef(authSO, "guestLoginButton", guestBtn);
            SetRef(authSO, "showRegisterButton", showRegBtn);
            SetRef(authSO, "registerPanel", registerPanel);
            SetRef(authSO, "registerUsernameInput", regUsernameInput);
            SetRef(authSO, "registerEmailInput", regEmailInput);
            SetRef(authSO, "registerPasswordInput", regPasswordInput);
            SetRef(authSO, "registerButton", regBtn);
            SetRef(authSO, "showLoginButton", showLogBtn);
            SetRef(authSO, "errorText", errorText);
            SetRef(authSO, "loadingIndicator", loadingIndicator);
            authSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(authManager);

            // 7. Wire base UIManager panel refs
            SerializedObject uiSO = new SerializedObject(uiManager);
            SetRef(uiSO, "loginPanel", loginPanel);
            SetRef(uiSO, "mainMenuPanel", mainMenuPanel);
            SetRef(uiSO, "gamePanel", gamePanel);
            SetRef(uiSO, "rewardsPanel", rewardsPanel);
            SetRef(uiSO, "profilePanel", profilePanel);
            SetRef(uiSO, "marketplacePanel", marketplacePanel);
            SetRef(uiSO, "socialPanel", socialPanel);
            SetRef(uiSO, "challengesPanel", challengesPanel);
            uiSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiManager);
        }

        /// <summary>
        /// Wire extended UIManager references via SerializedObject
        /// </summary>
        private static void WireUIManagerExtras(Transform canvasT, GameObject managersObj)
        {
            UIManager uiManager = managersObj.GetComponent<UIManager>();
            if (uiManager == null) return;

            SerializedObject so = new SerializedObject(uiManager);

            // ── UIManager.cs base fields ──────────────────────────────
            // Panels (missing from UIScaffoldGenerator)
            SetRef(so, "capturePanel", canvasT.Find("CapturePanel")?.gameObject);
            SetRef(so, "settingsPanel", canvasT.Find("SettingsPanel")?.gameObject);

            // Capture Dialog
            SetRef(so, "captureDialog", canvasT.Find("CaptureDialog")?.gameObject);
            SetRef(so, "prizeNameText", FindTMP(canvasT, "CaptureDialog/PrizeNameText"));
            SetRef(so, "prizeDescriptionText", FindTMP(canvasT, "CaptureDialog/PrizeDescriptionText"));
            SetRef(so, "prizePointsText", FindTMP(canvasT, "CaptureDialog/PrizePointsText"));
            SetRef(so, "prizeImage", canvasT.Find("CaptureDialog/PrizeImage")?.GetComponent<Image>());
            SetRef(so, "captureButton", canvasT.Find("CaptureDialog/CaptureButton")?.GetComponent<Button>());
            SetRef(so, "cancelButton", canvasT.Find("CaptureDialog/CancelButton")?.GetComponent<Button>());

            // Message Dialog
            SetRef(so, "messageDialog", canvasT.Find("MessageDialog")?.gameObject);
            SetRef(so, "messageText", FindTMP(canvasT, "MessageDialog/MessageText"));
            SetRef(so, "messageOkButton", canvasT.Find("MessageDialog/MessageOkButton")?.GetComponent<Button>());

            // Loading
            SetRef(so, "loadingOverlay", canvasT.Find("LoadingOverlay")?.gameObject);
            SetRef(so, "loadingText", FindTMP(canvasT, "LoadingOverlay/LoadingText"));

            // Live Signal
            SetRef(so, "liveSignalIndicator", canvasT.Find("GamePanel/TopHUDBar/LiveSignalBg/LiveSignalIndicator")?.gameObject);
            SetRef(so, "rewardedAdButton", canvasT.Find("GamePanel/TopHUDBar/AdButton")?.GetComponent<Button>());

            // HUD
            SetRef(so, "pointsText", FindTMP(canvasT, "MainMenuPanel/PointsText"));
            SetRef(so, "levelText", FindTMP(canvasT, "MainMenuPanel/LevelText"));
            SetRef(so, "usernameText", FindTMP(canvasT, "MainMenuPanel/UsernameText"));
            SetRef(so, "experienceSlider", canvasT.Find("MainMenuPanel/ExperienceSlider")?.GetComponent<Slider>());

            // ── UIManagerExtensions.cs fields ─────────────────────────
            // Rewards UI
            SetRef(so, "rewardsContainer", canvasT.Find("RewardsPanel/RewardsContainer/Viewport/Content"));
            SetRef(so, "categoryDropdown", canvasT.Find("RewardsPanel/CategoryDropdown")?.GetComponent<TMP_Dropdown>());
            SetRef(so, "favoritesButton", canvasT.Find("RewardsPanel/FavoritesButton")?.GetComponent<Button>());

            // Marketplace UI
            SetRef(so, "marketplacePanel", canvasT.Find("MarketplacePanel")?.gameObject);
            SetRef(so, "marketplaceContainer", canvasT.Find("MarketplacePanel/MarketplaceContainer/Viewport/Content"));
            SetRef(so, "inventoryButton", canvasT.Find("MarketplacePanel/InventoryButton")?.GetComponent<Button>());

            // Inventory UI
            SetRef(so, "inventoryPanel", canvasT.Find("InventoryPanel")?.gameObject);
            SetRef(so, "inventoryContainer", canvasT.Find("InventoryPanel/InventoryContainer/Viewport/Content"));
            SetRef(so, "inventorySummaryText", FindTMP(canvasT, "InventoryPanel/InventorySummaryText"));

            // Social UI
            SetRef(so, "socialPanel", canvasT.Find("SocialPanel")?.gameObject);
            SetRef(so, "friendsContainer", canvasT.Find("SocialPanel/FriendsContainer/Viewport/Content"));
            SetRef(so, "requestsContainer", canvasT.Find("SocialPanel/RequestsContainer/Viewport/Content"));
            SetRef(so, "leaderboardButton", canvasT.Find("SocialPanel/LeaderboardButton")?.GetComponent<Button>());

            // Leaderboard UI
            SetRef(so, "leaderboardPanel", canvasT.Find("LeaderboardPanel")?.gameObject);
            SetRef(so, "leaderboardContainer", canvasT.Find("LeaderboardPanel/LeaderboardContainer/Viewport/Content"));
            SetRef(so, "leaderboardSummaryText", FindTMP(canvasT, "LeaderboardPanel/LeaderboardSummaryText"));

            // PowerUp UI
            SetRef(so, "powerUpIndicator", canvasT.Find("GamePanel/PowerUpIndicator")?.gameObject);
            SetRef(so, "powerUpTimerText", FindTMP(canvasT, "GamePanel/PowerUpIndicator/PowerUpTimerText"));
            SetRef(so, "powerUpIcon", canvasT.Find("GamePanel/PowerUpIndicator/PowerUpIcon")?.GetComponent<Image>());

            // Challenges UI
            SetRef(so, "challengesPanel", canvasT.Find("ChallengesPanel")?.gameObject);
            SetRef(so, "challengesContainer", canvasT.Find("ChallengesPanel/ChallengesContainer/Viewport/Content"));
            SetRef(so, "challengesProgressText", FindTMP(canvasT, "ChallengesPanel/ChallengesProgressText"));

            // Claims UI
            SetRef(so, "claimsPanel", canvasT.Find("ClaimsPanel")?.gameObject);
            SetRef(so, "claimsContainer", canvasT.Find("ClaimsPanel/ClaimsContainer/Viewport/Content"));
            SetRef(so, "qrCodePanel", canvasT.Find("QRCodePanel")?.gameObject);
            SetRef(so, "qrCodeImage", canvasT.Find("QRCodePanel/QRCodeImage")?.GetComponent<RawImage>());

            // Achievements UI
            SetRef(so, "achievementsPanel", canvasT.Find("AchievementsPanel")?.gameObject);
            SetRef(so, "achievementsContainer", canvasT.Find("AchievementsPanel/AchievementsContainer/Viewport/Content"));
            SetRef(so, "achievementsSummaryText", FindTMP(canvasT, "AchievementsPanel/AchievementsSummaryText"));
            SetRef(so, "refreshAchievementsButton", canvasT.Find("AchievementsPanel/RefreshAchievementsButton")?.GetComponent<Button>());

            // Notifications UI
            SetRef(so, "notificationsPanel", canvasT.Find("NotificationsPanel")?.gameObject);
            SetRef(so, "notificationsContainer", canvasT.Find("NotificationsPanel/NotificationsContainer/Viewport/Content"));
            SetRef(so, "notificationsSummaryText", FindTMP(canvasT, "NotificationsPanel/NotificationsSummaryText"));
            SetRef(so, "markAllNotificationsReadButton", canvasT.Find("NotificationsPanel/MarkAllNotificationsReadButton")?.GetComponent<Button>());
            SetRef(so, "refreshNotificationsButton", canvasT.Find("NotificationsPanel/RefreshNotificationsButton")?.GetComponent<Button>());

            // Report UI
            SetRef(so, "reportPanel", canvasT.Find("ReportPanel")?.gameObject);
            SetRef(so, "reportTypeDropdown", canvasT.Find("ReportPanel/ReportTypeDropdown")?.GetComponent<TMP_Dropdown>());
            SetRef(so, "reportDescriptionInput", canvasT.Find("ReportPanel/ReportDescriptionInput")?.GetComponent<TMP_InputField>());
            SetRef(so, "submitReportButton", canvasT.Find("ReportPanel/SubmitReportButton")?.GetComponent<Button>());

            // Profile Drawer extras
            SetRef(so, "profileLastIpText", FindTMP(canvasT, "ProfilePanel/PlayerIDCard/LastIpText"));
            SetRef(so, "profileDeviceText", FindTMP(canvasT, "ProfilePanel/PlayerIDCard/DeviceText"));
            SetRef(so, "profileLastActiveText", FindTMP(canvasT, "ProfilePanel/PlayerIDCard/LastActiveText"));
            SetRef(so, "profileClaimsText", FindTMP(canvasT, "ProfilePanel/PlayerIDCard/ClaimsText"));
            SetRef(so, "profileStreakText", FindTMP(canvasT, "ProfilePanel/PlayerIDCard/StreakText"));

            // Confirm Dialog
            SetRef(so, "confirmDialog", canvasT.Find("ConfirmDialog")?.gameObject);
            SetRef(so, "confirmTitleText", FindTMP(canvasT, "ConfirmDialog/ConfirmTitleText"));
            SetRef(so, "confirmMessageText", FindTMP(canvasT, "ConfirmDialog/ConfirmMessageText"));
            SetRef(so, "confirmYesButton", canvasT.Find("ConfirmDialog/ConfirmYesButton")?.GetComponent<Button>());
            SetRef(so, "confirmNoButton", canvasT.Find("ConfirmDialog/ConfirmNoButton")?.GetComponent<Button>());

            // Prefab wiring — load from Assets/Prefabs/
            SetRef(so, "rewardItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RewardItem.prefab"));
            SetRef(so, "marketplaceItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MarketplaceItem.prefab"));
            SetRef(so, "friendItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FriendItem.prefab"));
            SetRef(so, "friendRequestPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FriendRequestItem.prefab"));
            SetRef(so, "challengeItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ChallengeItem.prefab"));
            SetRef(so, "claimItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ClaimItem.prefab"));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiManager);

            // ── Wire navigation buttons (runtime listeners) ──────────
            WireNavigationButtons(canvasT, uiManager);
        }

        /// <summary>
        /// Builds the GamePanel HUD shell and runtime gameplay UI scaffold (camera/map containers, minimap, controls).
        /// Extracted for direct gameplay scene generators.
        /// </summary>
        private static void EnsureGameplayPanelShell(Transform canvasT, GameObject coreObj)
        {
            if (canvasT == null) return;

            GameObject gamePanel = canvasT.Find("GamePanel")?.gameObject;
            if (gamePanel == null) return;

            ClearChildren(gamePanel.transform);
            gamePanel.SetActive(false);

            Image gameBg = gamePanel.GetComponent<Image>();
            if (gameBg != null) gameBg.color = new Color(0.02f, 0.02f, 0.03f, 1f);

            // Top HUD Bar (Glassmorphism)
            GameObject topBar = CreatePanel(gamePanel.transform, "TopHUDBar");
            var tbRect = topBar.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0, 1); tbRect.anchorMax = new Vector2(1, 1);
            tbRect.pivot = new Vector2(0.5f, 1);
            tbRect.anchoredPosition = Vector2.zero;
            tbRect.sizeDelta = new Vector2(0, 200);
            topBar.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

            // Back/Menu
            CreateButton(topBar.transform, "BackToMenuButton", "< Menu", new Vector2(-360, -100), new Color(0.15f, 0.15f, 0.2f), new Vector2(240, 100), 36);

            // Stats
            CreateText(topBar.transform, "GameLevelText", "Level 1", new Vector2(0, -60), new Color(0.4f, 0.8f, 0.5f), 28);
            CreateText(topBar.transform, "GamePointsText", "0 Pts", new Vector2(0, -130), new Color(1f, 0.8f, 0.2f), 42);

            // Live signal / ad
            GameObject liveSignalBg = new GameObject("LiveSignalBg");
            liveSignalBg.transform.SetParent(topBar.transform, false);
            var lsBgRect = liveSignalBg.AddComponent<RectTransform>();
            lsBgRect.anchorMin = new Vector2(0.5f, 0.5f); lsBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            lsBgRect.anchoredPosition = new Vector2(400, -100);
            lsBgRect.sizeDelta = new Vector2(100, 100);
            var lsBgImg = liveSignalBg.AddComponent<Image>();
            lsBgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            CreateButton(topBar.transform, "AdButton", "AD", new Vector2(280, -100), new Color(0.1f, 0.6f, 1f), new Vector2(170, 80), 28);

            GameObject liveSignal = new GameObject("LiveSignalIndicator");
            liveSignal.transform.SetParent(liveSignalBg.transform, false);
            var lsRect = liveSignal.AddComponent<RectTransform>();
            lsRect.anchoredPosition = Vector2.zero;
            lsRect.sizeDelta = new Vector2(40, 40);
            var lsImg = liveSignal.AddComponent<Image>();
            lsImg.color = new Color(0.1f, 0.9f, 0.4f);

            // Power-up indicator
            GameObject powerUpInd = CreatePanel(gamePanel.transform, "PowerUpIndicator");
            var puRect = powerUpInd.GetComponent<RectTransform>();
            puRect.anchorMin = new Vector2(0.5f, 1); puRect.anchorMax = new Vector2(0.5f, 1);
            puRect.pivot = new Vector2(0.5f, 1);
            puRect.anchoredPosition = new Vector2(0, -220);
            puRect.sizeDelta = new Vector2(450, 80);
            powerUpInd.GetComponent<Image>().color = new Color(0.6f, 0.3f, 0.9f, 0.95f);

            GameObject powerUpIconObj = new GameObject("PowerUpIcon");
            powerUpIconObj.transform.SetParent(powerUpInd.transform, false);
            RectTransform powerUpIconRect = powerUpIconObj.AddComponent<RectTransform>();
            powerUpIconRect.anchorMin = new Vector2(0f, 0.5f);
            powerUpIconRect.anchorMax = new Vector2(0f, 0.5f);
            powerUpIconRect.pivot = new Vector2(0f, 0.5f);
            powerUpIconRect.anchoredPosition = new Vector2(16f, 0f);
            powerUpIconRect.sizeDelta = new Vector2(48f, 48f);
            Image powerUpIconImage = powerUpIconObj.AddComponent<Image>();
            powerUpIconImage.color = new Color(1f, 0.9f, 0.35f, 1f);

            TextMeshProUGUI powerUpTimer = CreateText(powerUpInd.transform, "PowerUpTimerText", "RADAR 00:59", Vector2.zero, Color.white, 28);
            RectTransform powerUpTimerRect = powerUpTimer.GetComponent<RectTransform>();
            if (powerUpTimerRect != null)
            {
                powerUpTimerRect.anchoredPosition = new Vector2(28f, 0f);
            }
            powerUpInd.SetActive(false);

            // Core gameplay buttons
            CreateButton(gamePanel.transform, "ModeToggleButton", "CAM / MAP", new Vector2(380, -850), new Color(0.2f, 0.2f, 0.25f), new Vector2(250, 120), 30);
            CreateButton(gamePanel.transform, "MockCaptureBtn", "SCAN TARGET", new Vector2(0, -800), new Color(0.1f, 0.8f, 0.4f), new Vector2(400, 150), 40);

            EnsureGameplayRuntimeScaffold(gamePanel.transform, coreObj);
        }

        /// <summary>
        /// Wire menu navigation buttons to UIManager panel show methods
        /// </summary>
        private static void WireNavigationButtons(Transform canvasT, UIManager uiManager)
        {
            // MainMenu navigation
            WireButton(canvasT, "MainMenuPanel/PlayButton", uiManager.ShowGamePanel);
            WireButton(canvasT, "MainMenuPanel/RewardsButton", uiManager.ShowRewardsPanel);
            WireButton(canvasT, "MainMenuPanel/MarketplaceButton", uiManager.ShowMarketplacePanel);
            WireButton(canvasT, "MainMenuPanel/SocialButton", uiManager.ShowSocialPanel);
            WireButton(canvasT, "MainMenuPanel/ChallengesButton", uiManager.ShowChallengesPanel);
            WireButton(canvasT, "MainMenuPanel/ProfileButton", uiManager.ShowProfilePanel);
            WireButton(canvasT, "MainMenuPanel/SettingsButton", uiManager.ShowSettingsPanel);
            WireButton(canvasT, "MainMenuPanel/ClaimsButton", uiManager.ShowClaimsPanel);
            WireButton(canvasT, "MainMenuPanel/AchievementsButton", uiManager.ShowAchievementsPanel);
            WireButton(canvasT, "MainMenuPanel/NotificationsButton", uiManager.ShowNotificationsPanel);

            // In-panel feature entry points
            WireButton(canvasT, "MarketplacePanel/InventoryButton", uiManager.ShowInventoryPanel);
            WireButton(canvasT, "SocialPanel/LeaderboardButton", uiManager.ShowLeaderboardPanel);

            // Back buttons → MainMenu
            WireButton(canvasT, "GamePanel/TopHUDBar/BackToMenuButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "ProfilePanel/CloseProfileButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "RewardsPanel/CloseRewardsButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "MarketplacePanel/CloseMarketButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "SocialPanel/CloseSocialButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "ChallengesPanel/CloseChallengesButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "ClaimsPanel/CloseClaimsButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "SettingsPanel/CloseSettingsButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "InventoryPanel/CloseInventoryButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "LeaderboardPanel/CloseLeaderboardButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "AchievementsPanel/CloseAchievementsButton", uiManager.ShowMainMenu);
            WireButton(canvasT, "NotificationsPanel/CloseNotificationsButton", uiManager.ShowMainMenu);

            // Dialog/overlay close actions
            WireButton(canvasT, "ReportPanel/CloseReportButton", uiManager.CloseReportPanel);
            WireButton(canvasT, "QRCodePanel/CloseQRButton", uiManager.CloseQRCodePanel);

            // Settings/Profile actions (generated controls)
            WireButton(canvasT, "SettingsPanel/SettingsCard/SoundToggle", uiManager.ToggleMasterAudioSetting);
            WireButton(canvasT, "SettingsPanel/SettingsCard/HapticsToggle", uiManager.ToggleHapticsSetting);
            WireButton(canvasT, "SettingsPanel/SettingsCard/NotifToggle", uiManager.ToggleNotificationsSetting);
            WireButton(canvasT, "SettingsPanel/SettingsCard/LocationToggle", uiManager.ToggleLocationSetting);
            WireButton(canvasT, "SettingsPanel/SettingsCard/LogoutButton", uiManager.LogoutFromGeneratedUI);
            WireButton(canvasT, "ProfilePanel/LogoutButton", uiManager.LogoutFromGeneratedUI);
        }

        private static void WireButton(Transform root, string path, UnityEngine.Events.UnityAction action)
        {
            Transform t = root.Find(path);
            if (t == null) return;
            Button btn = t.GetComponent<Button>();
            if (btn == null) return;
            ResetButtonListeners(btn);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
        }

        private static void ResetButtonListeners(Button btn)
        {
            if (btn == null) return;

            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, i);
            }

            btn.onClick.RemoveAllListeners();
        }

        private static void EnsureExtendedFeaturePanels(Transform canvasT)
        {
            BuildFeatureListPanel(
                canvasT,
                "InventoryPanel",
                "INVENTORY",
                "InventorySummaryText",
                "InventoryContainer",
                "CloseInventoryButton",
                "<- NAV MENU");

            BuildFeatureListPanel(
                canvasT,
                "LeaderboardPanel",
                "GLOBAL LEADERBOARD",
                "LeaderboardSummaryText",
                "LeaderboardContainer",
                "CloseLeaderboardButton",
                "<- NAV MENU");

            BuildFeatureListPanel(
                canvasT,
                "AchievementsPanel",
                "ACHIEVEMENTS",
                "AchievementsSummaryText",
                "AchievementsContainer",
                "CloseAchievementsButton",
                "<- NAV MENU",
                extraButtons: new[]
                {
                    new FeaturePanelButtonConfig("RefreshAchievementsButton", "REFRESH", new Vector2(0f, 680f), new Color(0.28f, 0.52f, 0.24f), new Vector2(850f, 80f), 28)
                });

            BuildFeatureListPanel(
                canvasT,
                "NotificationsPanel",
                "NOTIFICATIONS",
                "NotificationsSummaryText",
                "NotificationsContainer",
                "CloseNotificationsButton",
                "<- NAV MENU",
                extraButtons: new[]
                {
                    new FeaturePanelButtonConfig("MarkAllNotificationsReadButton", "MARK ALL READ", new Vector2(-220f, 680f), new Color(0.2f, 0.45f, 0.78f), new Vector2(400f, 80f), 24),
                    new FeaturePanelButtonConfig("RefreshNotificationsButton", "REFRESH", new Vector2(220f, 680f), new Color(0.28f, 0.52f, 0.24f), new Vector2(400f, 80f), 24)
                });
        }

        private static void BuildFeatureListPanel(
            Transform canvasT,
            string panelName,
            string title,
            string summaryTextName,
            string containerName,
            string closeButtonName,
            string closeButtonLabel,
            FeaturePanelButtonConfig[] extraButtons = null)
        {
            GameObject panelObj = canvasT.Find(panelName)?.gameObject;
            if (panelObj == null)
            {
                panelObj = CreatePanel(canvasT, panelName);
            }

            ClearChildren(panelObj.transform);

            Image bgImage = panelObj.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.7f);
            }

            CreateText(panelObj.transform, panelName + "Title", title, new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55);
            CreateText(panelObj.transform, summaryTextName, "Loading...", new Vector2(0f, 720f), new Color(0.7f, 0.8f, 0.95f), 24);

            if (extraButtons != null)
            {
                foreach (FeaturePanelButtonConfig cfg in extraButtons)
                {
                    CreateButton(panelObj.transform, cfg.Name, cfg.Label, cfg.Position, cfg.Color, cfg.Size, cfg.FontSize);
                }
            }

            float containerY = extraButtons != null && extraButtons.Length > 0 ? 40f : 80f;
            float containerHeight = extraButtons != null && extraButtons.Length > 0 ? 1040f : 1100f;
            CreateScrollContainer(panelObj.transform, containerName, new Vector2(0f, containerY), new Vector2(950f, containerHeight));
            CreateButton(panelObj.transform, closeButtonName, closeButtonLabel, new Vector2(0f, -780f), new Color(0.2f, 0.2f, 0.3f), new Vector2(500f, 100f), 32);
            panelObj.SetActive(false);
        }

        private struct FeaturePanelButtonConfig
        {
            public readonly string Name;
            public readonly string Label;
            public readonly Vector2 Position;
            public readonly Color Color;
            public readonly Vector2 Size;
            public readonly int FontSize;

            public FeaturePanelButtonConfig(string name, string label, Vector2 position, Color color, Vector2 size, int fontSize)
            {
                Name = name;
                Label = label;
                Position = position;
                Color = color;
                Size = size;
                FontSize = fontSize;
            }
        }

        private static void EnsureAREffectsScaffold(Transform canvasT, GameObject coreObj, GameObject effectsObj)
        {
            EnsureARFoundationScaffold(coreObj);
            EnsureCaptureAnimationScaffold(canvasT, effectsObj);
        }

        private static void EnsureARFoundationScaffold(GameObject coreObj)
        {
            if (coreObj == null) return;

            Transform existing = coreObj.transform.Find("ARScaffold");
            GameObject arScaffold = existing != null ? existing.gameObject : new GameObject("ARScaffold");
            arScaffold.transform.SetParent(coreObj.transform, false);

            GameObject arSessionObj = arScaffold.transform.Find("ARSession")?.gameObject;
            if (arSessionObj == null)
            {
                arSessionObj = new GameObject("ARSession");
                arSessionObj.transform.SetParent(arScaffold.transform, false);
            }
            ARSession arSession = GetOrAdd<ARSession>(arSessionObj);
            arSession.enabled = false;

            GameObject arOriginObj = arScaffold.transform.Find("ARSessionOrigin")?.gameObject;
            if (arOriginObj == null)
            {
                arOriginObj = new GameObject("ARSessionOrigin");
                arOriginObj.transform.SetParent(arScaffold.transform, false);
            }

            XROrigin arSessionOrigin = GetOrAdd<XROrigin>(arOriginObj);
            ARPlaneManager arPlaneManager = GetOrAdd<ARPlaneManager>(arOriginObj);
            ARRaycastManager arRaycastManager = GetOrAdd<ARRaycastManager>(arOriginObj);
            arPlaneManager.enabled = false;
            arRaycastManager.enabled = false;

            // XR Origin expects a floor-offset object; providing one suppresses the fallback warning.
            GameObject cameraFloorOffsetObj = arOriginObj.transform.Find("CameraFloorOffset")?.gameObject;
            if (cameraFloorOffsetObj == null)
            {
                cameraFloorOffsetObj = new GameObject("CameraFloorOffset");
                cameraFloorOffsetObj.transform.SetParent(arOriginObj.transform, false);
                cameraFloorOffsetObj.transform.localPosition = Vector3.zero;
                cameraFloorOffsetObj.transform.localRotation = Quaternion.identity;
                cameraFloorOffsetObj.transform.localScale = Vector3.one;
            }

            // Add a dedicated AR camera child placeholder without replacing the main gameplay camera.
            GameObject arCameraObj = arOriginObj.transform.Find("ARCamera")?.gameObject;
            if (arCameraObj == null)
            {
                arCameraObj = new GameObject("ARCamera");
                arCameraObj.transform.SetParent(cameraFloorOffsetObj.transform, false);
                Camera arCamera = arCameraObj.AddComponent<Camera>();
                arCamera.enabled = false;
                arCamera.clearFlags = CameraClearFlags.SolidColor;
                arCamera.backgroundColor = Color.black;
                arCamera.nearClipPlane = 0.01f;
                arCamera.farClipPlane = 30f;
            }
            else if (arCameraObj.transform.parent != cameraFloorOffsetObj.transform)
            {
                arCameraObj.transform.SetParent(cameraFloorOffsetObj.transform, false);
            }

            TryAddTrackedPoseDriverIfAvailable(arCameraObj);

            // Best-effort assignment of session origin camera reference (API/version-safe via SerializedObject fallback).
            SerializedObject originSO = new SerializedObject(arSessionOrigin);
            SerializedProperty cameraProp = originSO.FindProperty("m_Camera");
            if (cameraProp != null)
            {
                cameraProp.objectReferenceValue = arCameraObj.GetComponent<Camera>();
            }

            // XR Core Utils serialized field used for floor-offset object in 2022.3+ package versions.
            SerializedProperty floorOffsetProp = originSO.FindProperty("m_CameraFloorOffsetObject");
            if (floorOffsetProp != null)
            {
                floorOffsetProp.objectReferenceValue = cameraFloorOffsetObj;
            }

            originSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arSessionOrigin);
        }

        private static void EnsureCaptureAnimationScaffold(Transform canvasT, GameObject effectsObj)
        {
            if (canvasT == null || effectsObj == null) return;

            GameObject runtime = effectsObj.transform.Find("CaptureAnimationRuntime")?.gameObject;
            if (runtime == null)
            {
                runtime = new GameObject("CaptureAnimationRuntime");
                runtime.transform.SetParent(effectsObj.transform, false);
            }

            GameObject boxSpawnPoint = runtime.transform.Find("BoxSpawnPoint")?.gameObject;
            if (boxSpawnPoint == null)
            {
                boxSpawnPoint = new GameObject("BoxSpawnPoint");
                boxSpawnPoint.transform.SetParent(runtime.transform, false);
                Camera mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
                if (mainCamera != null)
                {
                    boxSpawnPoint.transform.position = mainCamera.transform.position + (mainCamera.transform.forward * 2f) + (mainCamera.transform.up * -0.1f);
                    boxSpawnPoint.transform.rotation = mainCamera.transform.rotation;
                }
                else
                {
                    boxSpawnPoint.transform.localPosition = new Vector3(0f, 1.5f, 2f);
                }
            }

            GameObject prizeBoxTemplate = runtime.transform.Find("PrizeBoxTemplate")?.gameObject;
            if (prizeBoxTemplate == null)
            {
                prizeBoxTemplate = new GameObject("PrizeBoxTemplate");
                prizeBoxTemplate.transform.SetParent(runtime.transform, false);

                GameObject boxBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxBase.name = "BoxBase";
                boxBase.transform.SetParent(prizeBoxTemplate.transform, false);
                boxBase.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
                Renderer baseRenderer = boxBase.GetComponent<Renderer>();
                if (baseRenderer != null) baseRenderer.sharedMaterial.color = new Color(0.2f, 0.16f, 0.1f, 1f);

                GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lid.name = "Lid";
                lid.transform.SetParent(prizeBoxTemplate.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.23f, 0f);
                lid.transform.localScale = new Vector3(0.52f, 0.08f, 0.52f);
                Renderer lidRenderer = lid.GetComponent<Renderer>();
                if (lidRenderer != null) lidRenderer.sharedMaterial.color = new Color(0.75f, 0.58f, 0.15f, 1f);
            }
            prizeBoxTemplate.SetActive(false);

            EnsureNamedParticle(runtime.transform, "CaptureParticles", new Color(0.2f, 0.9f, 1f, 1f), 24f, new Vector3(0f, 0f, 0f));
            EnsureNamedParticle(runtime.transform, "StarBurstParticles", new Color(1f, 0.85f, 0.2f, 1f), 36f, new Vector3(0f, 0f, 0f));
            EnsureNamedParticle(runtime.transform, "ConfettiParticles", new Color(0.2f, 1f, 0.4f, 1f), 42f, new Vector3(0f, 0f, 0f));
            EnsureNamedParticle(runtime.transform, "GlowParticles", new Color(1f, 0.6f, 0.1f, 1f), 18f, new Vector3(0f, 0f, 0f));

            GameObject rewardPopup = canvasT.Find("CaptureRewardPopupPanel")?.gameObject;
            if (rewardPopup == null)
            {
                rewardPopup = CreatePanel(canvasT, "CaptureRewardPopupPanel");
            }
            ClearChildren(rewardPopup.transform);
            Image rewardPopupImage = rewardPopup.GetComponent<Image>();
            if (rewardPopupImage == null) rewardPopupImage = rewardPopup.AddComponent<Image>();
            rewardPopupImage.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject rewardCard = CreatePanel(rewardPopup.transform, "RewardCard");
            RectTransform cardRect = rewardCard.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(820f, 760f);
            Image rewardCardImage = rewardCard.GetComponent<Image>();
            if (rewardCardImage == null) rewardCardImage = rewardCard.AddComponent<Image>();
            rewardCardImage.color = new Color(0.06f, 0.07f, 0.1f, 0.97f);
            if (rewardCard.GetComponent<CanvasGroup>() == null)
            {
                rewardCard.AddComponent<CanvasGroup>();
            }

            GameObject rewardIconObj = new GameObject("RewardIcon");
            rewardIconObj.transform.SetParent(rewardCard.transform, false);
            RectTransform iconRect = rewardIconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -60f);
            iconRect.sizeDelta = new Vector2(180f, 180f);
            Image rewardIcon = rewardIconObj.AddComponent<Image>();
            rewardIcon.color = new Color(0.8f, 0.8f, 0.85f, 1f);

            CreateText(rewardCard.transform, "RewardNameText", "Reward Name", new Vector2(0f, 150f), Color.white, 40);
            CreateText(rewardCard.transform, "RewardPointsText", "+0 Points!", new Vector2(0f, 60f), new Color(1f, 0.82f, 0.2f), 34);
            CreateText(rewardCard.transform, "RewardMessageText", "Prize captured!", new Vector2(0f, -20f), new Color(0.75f, 0.8f, 0.95f), 24);
            CreateButton(rewardCard.transform, "RewardCloseButton", "CONTINUE", new Vector2(0f, -250f), new Color(0.22f, 0.58f, 0.25f), new Vector2(420f, 90f), 28);
            rewardPopup.SetActive(false);

            GameObject screenFlash = canvasT.Find("CaptureScreenFlash")?.gameObject;
            if (screenFlash == null)
            {
                screenFlash = new GameObject("CaptureScreenFlash");
                screenFlash.transform.SetParent(canvasT, false);
                RectTransform flashRect = screenFlash.AddComponent<RectTransform>();
                StretchToParent(flashRect);
                Image flashImage = screenFlash.AddComponent<Image>();
                flashImage.color = new Color(1f, 1f, 1f, 0.5f);
            }
            else
            {
                RectTransform flashRect = screenFlash.GetComponent<RectTransform>();
                if (flashRect == null) flashRect = screenFlash.AddComponent<RectTransform>();
                StretchToParent(flashRect);
                Image flashImage = screenFlash.GetComponent<Image>();
                if (flashImage == null) flashImage = screenFlash.AddComponent<Image>();
                flashImage.color = new Color(1f, 1f, 1f, 0.5f);
            }
            screenFlash.SetActive(false);
        }

        private static ParticleSystem EnsureNamedParticle(Transform parent, string name, Color color, float speed, Vector3 localPosition)
        {
            GameObject obj = parent.Find(name)?.gameObject;
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
            }

            obj.transform.localPosition = localPosition;
            ParticleSystem ps = GetOrAdd<ParticleSystem>(obj);
            ConfigureParticlePlaceholder(ps, color, speed);
            return ps;
        }

        private static void ConfigureParticlePlaceholder(ParticleSystem ps, Color color, float speed)
        {
            if (ps == null) return;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.8f;
            main.startLifetime = 0.6f;
            main.startSpeed = speed;
            main.startSize = 0.08f;
            main.startColor = color;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;

            var noise = ps.noise;
            noise.enabled = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        private static void EnsureGameplayRuntimeScaffold(Transform gamePanel, GameObject coreObj)
        {
            if (gamePanel == null || coreObj == null) return;

            // Camera mode container (full panel background + camera feed target)
            GameObject cameraView = CreatePanel(gamePanel, "CameraViewContainer");
            cameraView.transform.SetAsFirstSibling();
            Image cameraViewBg = cameraView.GetComponent<Image>();
            if (cameraViewBg != null) cameraViewBg.color = new Color(0.02f, 0.02f, 0.03f, 1f);

            GameObject cameraFeedObj = new GameObject("CameraFeed");
            cameraFeedObj.transform.SetParent(cameraView.transform, false);
            RectTransform cameraFeedRect = cameraFeedObj.AddComponent<RectTransform>();
            StretchToParent(cameraFeedRect);
            RawImage cameraFeed = cameraFeedObj.AddComponent<RawImage>();
            cameraFeed.color = new Color(0.08f, 0.1f, 0.12f, 1f);

            // Map mode container (hidden by default, activated by GameModeManager)
            GameObject mapView = CreatePanel(gamePanel, "MapViewContainer");
            mapView.transform.SetAsFirstSibling();
            Image mapViewBg = mapView.GetComponent<Image>();
            if (mapViewBg != null) mapViewBg.color = new Color(0.05f, 0.06f, 0.08f, 1f);
            mapView.SetActive(false);

            GameObject mapImageObj = new GameObject("MapImage");
            mapImageObj.transform.SetParent(mapView.transform, false);
            RectTransform mapImageRect = mapImageObj.AddComponent<RectTransform>();
            StretchToParent(mapImageRect);
            RawImage mapImage = mapImageObj.AddComponent<RawImage>();
            mapImage.color = new Color(0.12f, 0.16f, 0.18f, 1f);

            GameObject heatmapOverlay = new GameObject("HeatmapOverlay");
            heatmapOverlay.transform.SetParent(mapView.transform, false);
            RectTransform heatmapRect = heatmapOverlay.AddComponent<RectTransform>();
            StretchToParent(heatmapRect);
            RawImage heatmapRaw = heatmapOverlay.AddComponent<RawImage>();
            heatmapRaw.color = new Color(1f, 0.4f, 0.1f, 0f);
            heatmapOverlay.SetActive(false);

            GameObject mapMarkers = new GameObject("MarkersContainer");
            mapMarkers.transform.SetParent(mapView.transform, false);
            RectTransform mapMarkersRect = mapMarkers.AddComponent<RectTransform>();
            StretchToParent(mapMarkersRect);

            Button centerOnPlayerButton = CreateButton(mapView.transform, "CenterOnPlayerButton", "CENTER", new Vector2(-320, -850), new Color(0.15f, 0.2f, 0.28f), new Vector2(220, 90), 26);
            if (centerOnPlayerButton != null && centerOnPlayerButton.TryGetComponent<RectTransform>(out var centerBtnRect))
            {
                centerBtnRect.anchorMin = new Vector2(0f, 0f);
                centerBtnRect.anchorMax = new Vector2(0f, 0f);
                centerBtnRect.pivot = new Vector2(0f, 0f);
                centerBtnRect.anchoredPosition = new Vector2(24f, 24f);
            }

            Button zoomInButton = CreateButton(mapView.transform, "ZoomInButton", "+", new Vector2(380, -760), new Color(0.15f, 0.2f, 0.28f), new Vector2(100, 90), 36);
            if (zoomInButton != null && zoomInButton.TryGetComponent<RectTransform>(out var zoomInRect))
            {
                zoomInRect.anchorMin = new Vector2(1f, 0f);
                zoomInRect.anchorMax = new Vector2(1f, 0f);
                zoomInRect.pivot = new Vector2(1f, 0f);
                zoomInRect.anchoredPosition = new Vector2(-24f, 126f);
            }

            Button zoomOutButton = CreateButton(mapView.transform, "ZoomOutButton", "-", new Vector2(380, -860), new Color(0.15f, 0.2f, 0.28f), new Vector2(100, 90), 36);
            if (zoomOutButton != null && zoomOutButton.TryGetComponent<RectTransform>(out var zoomOutRect))
            {
                zoomOutRect.anchorMin = new Vector2(1f, 0f);
                zoomOutRect.anchorMax = new Vector2(1f, 0f);
                zoomOutRect.pivot = new Vector2(1f, 0f);
                zoomOutRect.anchoredPosition = new Vector2(-24f, 24f);
            }

            // Mini-map UI for camera mode
            GameObject miniMapContainerObj = new GameObject("MiniMapContainer");
            miniMapContainerObj.transform.SetParent(gamePanel, false);
            RectTransform miniMapRect = miniMapContainerObj.AddComponent<RectTransform>();
            miniMapRect.anchorMin = new Vector2(1f, 1f);
            miniMapRect.anchorMax = new Vector2(1f, 1f);
            miniMapRect.pivot = new Vector2(1f, 1f);
            miniMapRect.anchoredPosition = new Vector2(-20f, -240f);
            miniMapRect.sizeDelta = new Vector2(220f, 220f);
            Image miniMapMaskImage = miniMapContainerObj.AddComponent<Image>();
            miniMapMaskImage.color = new Color(0f, 0f, 0f, 0.1f);

            GameObject miniMapImageObj = new GameObject("MiniMapImage");
            miniMapImageObj.transform.SetParent(miniMapContainerObj.transform, false);
            RectTransform miniMapImageRect = miniMapImageObj.AddComponent<RectTransform>();
            StretchToParent(miniMapImageRect);
            RawImage miniMapImage = miniMapImageObj.AddComponent<RawImage>();
            miniMapImage.color = new Color(0.1f, 0.14f, 0.18f, 0.95f);

            GameObject miniMapBorderObj = new GameObject("MiniMapBorder");
            miniMapBorderObj.transform.SetParent(miniMapContainerObj.transform, false);
            RectTransform miniMapBorderRect = miniMapBorderObj.AddComponent<RectTransform>();
            StretchToParent(miniMapBorderRect);
            Image miniMapBorder = miniMapBorderObj.AddComponent<Image>();
            miniMapBorder.color = new Color(1f, 1f, 1f, 0.4f);

            GameObject miniMapMarkersObj = new GameObject("PrizeMarkersContainer");
            miniMapMarkersObj.transform.SetParent(miniMapContainerObj.transform, false);
            RectTransform miniMapMarkersRect = miniMapMarkersObj.AddComponent<RectTransform>();
            StretchToParent(miniMapMarkersRect);

            GameObject playerDotObj = new GameObject("PlayerDot");
            playerDotObj.transform.SetParent(miniMapContainerObj.transform, false);
            RectTransform playerDotRect = playerDotObj.AddComponent<RectTransform>();
            playerDotRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerDotRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerDotRect.pivot = new Vector2(0.5f, 0.5f);
            playerDotRect.anchoredPosition = Vector2.zero;
            playerDotRect.sizeDelta = new Vector2(18f, 18f);
            Image playerDot = playerDotObj.AddComponent<Image>();
            playerDot.color = new Color(0.2f, 0.5f, 1f, 1f);

            // Catch button extras + report issue affordance
            Transform mockCatchBtn = gamePanel.Find("MockCaptureBtn");
            if (mockCatchBtn != null)
            {
                GameObject glowObj = new GameObject("CatchButtonGlow");
                glowObj.transform.SetParent(mockCatchBtn, false);
                RectTransform glowRect = glowObj.AddComponent<RectTransform>();
                StretchToParent(glowRect);
                Image glow = glowObj.AddComponent<Image>();
                glow.color = new Color(1f, 0.95f, 0.2f, 0.2f);
                glow.enabled = false;
                glowObj.transform.SetAsFirstSibling();
            }

            Button reportIssueBtn = CreateButton(gamePanel, "ReportIssueButton", "REPORT ISSUE", new Vector2(-300, -760), new Color(0.5f, 0.2f, 0.2f), new Vector2(260, 90), 22);
            if (reportIssueBtn != null && reportIssueBtn.TryGetComponent<RectTransform>(out var reportIssueRect))
            {
                reportIssueRect.anchorMin = new Vector2(0f, 0f);
                reportIssueRect.anchorMax = new Vector2(0f, 0f);
                reportIssueRect.pivot = new Vector2(0f, 0f);
                reportIssueRect.anchoredPosition = new Vector2(24f, 126f);
            }
            reportIssueBtn.gameObject.SetActive(false);

            GameObject arInstructionsPanel = CreatePanel(gamePanel, "ARInstructionsPanel");
            Image arInstructionsBg = arInstructionsPanel.GetComponent<Image>();
            if (arInstructionsBg != null) arInstructionsBg.color = new Color(0f, 0f, 0f, 0.65f);
            CreateText(arInstructionsPanel.transform, "ARInstructionsText", "Move device to detect a surface, then tap to place the prize.", new Vector2(0, 0), Color.white, 24);
            arInstructionsPanel.SetActive(false);

            // Runtime templates for marker prefabs / overlay parents
            GameObject runtimeTemplates = new GameObject("RuntimeTemplates");
            runtimeTemplates.transform.SetParent(coreObj.transform, false);
            runtimeTemplates.SetActive(false);

            GameObject playerMarkerTemplate = new GameObject("PlayerMarkerTemplate");
            playerMarkerTemplate.transform.SetParent(runtimeTemplates.transform, false);
            RectTransform pmtRect = playerMarkerTemplate.AddComponent<RectTransform>();
            pmtRect.sizeDelta = new Vector2(22f, 22f);
            Image pmtImage = playerMarkerTemplate.AddComponent<Image>();
            pmtImage.color = new Color(0.2f, 0.5f, 1f, 1f);

            GameObject prizeMarkerTemplate = new GameObject("PrizeMarkerTemplate");
            prizeMarkerTemplate.transform.SetParent(runtimeTemplates.transform, false);
            RectTransform prtRect = prizeMarkerTemplate.AddComponent<RectTransform>();
            prtRect.sizeDelta = new Vector2(28f, 28f);
            Image prtImage = prizeMarkerTemplate.AddComponent<Image>();
            prtImage.color = new Color(1f, 0.85f, 0.2f, 1f);
            prizeMarkerTemplate.AddComponent<Button>();

            GameObject arPrizeTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            arPrizeTemplate.name = "ARPrizeTemplate";
            arPrizeTemplate.transform.SetParent(runtimeTemplates.transform, false);
            arPrizeTemplate.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Renderer arPrizeRenderer = arPrizeTemplate.GetComponent<Renderer>();
            if (arPrizeRenderer != null)
            {
                arPrizeRenderer.sharedMaterial.color = new Color(1f, 0.78f, 0.2f, 1f);
            }
            arPrizeTemplate.SetActive(false);

            GameObject prizeOverlayTemplate = new GameObject("PrizeOverlayTemplate");
            prizeOverlayTemplate.transform.SetParent(runtimeTemplates.transform, false);
            prizeOverlayTemplate.transform.localScale = Vector3.one;

            GameObject overlayVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlayVisual.name = "Visual";
            overlayVisual.transform.SetParent(prizeOverlayTemplate.transform, false);
            overlayVisual.transform.localScale = new Vector3(0.6f, 0.3f, 0.08f);
            Renderer overlayRenderer = overlayVisual.GetComponent<Renderer>();
            if (overlayRenderer != null)
            {
                overlayRenderer.sharedMaterial.color = new Color(0.2f, 0.7f, 1f, 1f);
            }

            GameObject overlayLabel = new GameObject("OverlayLabel");
            overlayLabel.transform.SetParent(prizeOverlayTemplate.transform, false);
            overlayLabel.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            TextMeshPro overlayLabelText = overlayLabel.AddComponent<TextMeshPro>();
            overlayLabelText.text = "Prize";
            overlayLabelText.fontSize = 3f;
            overlayLabelText.alignment = TextAlignmentOptions.Center;
            overlayLabelText.color = Color.white;
            overlayLabel.transform.localScale = Vector3.one * 0.1f;
            prizeOverlayTemplate.SetActive(false);

            GameObject overlayContainer = new GameObject("PrizeOverlayContainer");
            overlayContainer.transform.SetParent(coreObj.transform, false);

            GameObject captureParticlesObj = new GameObject("CaptureParticlesPlaceholder");
            captureParticlesObj.transform.SetParent(coreObj.transform, false);
            ParticleSystem captureParticles = captureParticlesObj.AddComponent<ParticleSystem>();
            ConfigureParticlePlaceholder(captureParticles, new Color(1f, 0.85f, 0.25f, 1f), 20f);
        }

        private static void WireGameplayCoreReferences(Transform canvasT, GameObject managersObj, GameObject coreObj, GameObject effectsObj)
        {
            if (canvasT == null || coreObj == null) return;

            Transform gamePanel = canvasT.Find("GamePanel");
            if (gamePanel == null) return;

            Camera mainCam = Camera.main ?? Object.FindObjectOfType<Camera>();
            Transform runtimeTemplates = coreObj.transform.Find("RuntimeTemplates");
            GameObject playerMarkerTemplate = runtimeTemplates?.Find("PlayerMarkerTemplate")?.gameObject;
            GameObject prizeMarkerTemplate = runtimeTemplates?.Find("PrizeMarkerTemplate")?.gameObject;
            GameObject arPrizeTemplate = runtimeTemplates?.Find("ARPrizeTemplate")?.gameObject;
            GameObject prizeOverlayTemplate = runtimeTemplates?.Find("PrizeOverlayTemplate")?.gameObject;
            Transform prizeOverlayContainer = coreObj.transform.Find("PrizeOverlayContainer");
            Transform arScaffold = coreObj.transform.Find("ARScaffold");
            ARSession arSession = arScaffold?.Find("ARSession")?.GetComponent<ARSession>();
            XROrigin arSessionOrigin = arScaffold?.Find("ARSessionOrigin")?.GetComponent<XROrigin>();
            ARPlaneManager arPlaneManager = arScaffold?.Find("ARSessionOrigin")?.GetComponent<ARPlaneManager>();
            ARRaycastManager arRaycastManager = arScaffold?.Find("ARSessionOrigin")?.GetComponent<ARRaycastManager>();
            ParticleSystem captureParticles = coreObj.transform.Find("CaptureParticlesPlaceholder")?.GetComponent<ParticleSystem>();

            MapController mapController = coreObj.GetComponent<MapController>();
            if (mapController != null)
            {
                SerializedObject so = new SerializedObject(mapController);
                SetRef(so, "mapImage", gamePanel.Find("MapViewContainer/MapImage")?.GetComponent<RawImage>());
                SetRef(so, "fullScreenMapContainer", gamePanel.Find("MapViewContainer")?.gameObject);
                SetRef(so, "centerOnPlayerButton", gamePanel.Find("MapViewContainer/CenterOnPlayerButton")?.GetComponent<Button>());
                SetRef(so, "zoomInButton", gamePanel.Find("MapViewContainer/ZoomInButton")?.GetComponent<Button>());
                SetRef(so, "zoomOutButton", gamePanel.Find("MapViewContainer/ZoomOutButton")?.GetComponent<Button>());
                SetRef(so, "playerMarkerPrefab", playerMarkerTemplate);
                SetRef(so, "prizeMarkerPrefab", prizeMarkerTemplate);
                SetRef(so, "markersContainer", gamePanel.Find("MapViewContainer/MarkersContainer"));
                SetRef(so, "heatmapOverlay", gamePanel.Find("MapViewContainer/HeatmapOverlay")?.GetComponent<RawImage>());
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(mapController);
            }

            CameraLiveManager cameraLive = coreObj.GetComponent<CameraLiveManager>();
            if (cameraLive != null)
            {
                SerializedObject so = new SerializedObject(cameraLive);
                SetRef(so, "mainCamera", mainCam);
                SetRef(so, "cameraFeed", gamePanel.Find("CameraViewContainer/CameraFeed")?.GetComponent<RawImage>());
                SetRef(so, "miniMapImage", gamePanel.Find("MiniMapContainer/MiniMapImage")?.GetComponent<RawImage>());
                SetRef(so, "miniMapContainer", gamePanel.Find("MiniMapContainer")?.GetComponent<RectTransform>());
                SetRef(so, "miniMapBorder", gamePanel.Find("MiniMapContainer/MiniMapBorder")?.GetComponent<Image>());
                SetRef(so, "playerDot", gamePanel.Find("MiniMapContainer/PlayerDot")?.GetComponent<Image>());
                SetRef(so, "prizeMarkersContainer", gamePanel.Find("MiniMapContainer/PrizeMarkersContainer"));
                SetRef(so, "prizeMarkerPrefab", prizeMarkerTemplate);
                SetRef(so, "catchButtonObject", gamePanel.Find("MockCaptureBtn")?.gameObject);
                SetRef(so, "catchButton", gamePanel.Find("MockCaptureBtn")?.GetComponent<Button>());
                SetRef(so, "catchButtonText", FindTMP(gamePanel, "MockCaptureBtn/Text"));
                SetRef(so, "catchButtonGlow", gamePanel.Find("MockCaptureBtn/CatchButtonGlow")?.GetComponent<Image>());
                SetRef(so, "prizeOverlayPrefab", prizeOverlayTemplate);
                SetRef(so, "prizeOverlayContainer", prizeOverlayContainer);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(cameraLive);
            }

            GameModeManager gameModeManager = coreObj.GetComponent<GameModeManager>();
            if (gameModeManager != null)
            {
                SerializedObject so = new SerializedObject(gameModeManager);
                SetRef(so, "cameraViewContainer", gamePanel.Find("CameraViewContainer")?.gameObject);
                SetRef(so, "mapViewContainer", gamePanel.Find("MapViewContainer")?.gameObject);
                SetRef(so, "switchModeButton", gamePanel.Find("ModeToggleButton")?.GetComponent<Button>());
                SetRef(so, "switchModeText", FindTMP(gamePanel, "ModeToggleButton/Text"));
                SetRef(so, "cameraManager", cameraLive);
                SetRef(so, "mapController", mapController);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameModeManager);
            }

            CaptureController captureController = coreObj.GetComponent<CaptureController>();
            if (captureController != null)
            {
                SerializedObject so = new SerializedObject(captureController);
                SetRef(so, "arSession", arSession);
                SetRef(so, "arSessionOrigin", arSessionOrigin);
                SetRef(so, "arPlaneManager", arPlaneManager);
                SetRef(so, "arRaycastManager", arRaycastManager);
                SetRef(so, "prizePrefab", arPrizeTemplate);
                SetRef(so, "captureParticles", captureParticles);
                SetRef(so, "captureButton", gamePanel.Find("MockCaptureBtn")?.gameObject);
                SetRef(so, "arInstructions", gamePanel.Find("ARInstructionsPanel")?.gameObject);
                SetRef(so, "reportIssueButton", gamePanel.Find("ReportIssueButton")?.GetComponent<Button>());
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(captureController);
            }

            ARManager arManager = managersObj != null ? managersObj.GetComponent<ARManager>() : null;
            if (arManager != null)
            {
                SerializedObject so = new SerializedObject(arManager);
                SetRef(so, "arSession", arSession);
                SetRef(so, "arSessionOrigin", arSessionOrigin);
                SetRef(so, "arRaycastManager", arRaycastManager);
                SetRef(so, "arPlaneManager", arPlaneManager);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(arManager);
            }

            CaptureAnimationController captureFx = effectsObj != null ? effectsObj.GetComponent<CaptureAnimationController>() : null;
            if (captureFx != null && canvasT != null)
            {
                SerializedObject so = new SerializedObject(captureFx);
                SetRef(so, "prizeBoxPrefab", effectsObj.transform.Find("CaptureAnimationRuntime/PrizeBoxTemplate")?.gameObject);
                SetRef(so, "boxSpawnPoint", effectsObj.transform.Find("CaptureAnimationRuntime/BoxSpawnPoint"));
                SetRef(so, "captureParticles", effectsObj.transform.Find("CaptureAnimationRuntime/CaptureParticles")?.GetComponent<ParticleSystem>());
                SetRef(so, "starBurstParticles", effectsObj.transform.Find("CaptureAnimationRuntime/StarBurstParticles")?.GetComponent<ParticleSystem>());
                SetRef(so, "confettiParticles", effectsObj.transform.Find("CaptureAnimationRuntime/ConfettiParticles")?.GetComponent<ParticleSystem>());
                SetRef(so, "glowParticles", effectsObj.transform.Find("CaptureAnimationRuntime/GlowParticles")?.GetComponent<ParticleSystem>());
                SetRef(so, "rewardPopupPanel", canvasT.Find("CaptureRewardPopupPanel")?.gameObject);
                SetRef(so, "rewardIcon", canvasT.Find("CaptureRewardPopupPanel/RewardCard/RewardIcon")?.GetComponent<Image>());
                SetRef(so, "rewardNameText", FindTMP(canvasT, "CaptureRewardPopupPanel/RewardCard/RewardNameText"));
                SetRef(so, "rewardPointsText", FindTMP(canvasT, "CaptureRewardPopupPanel/RewardCard/RewardPointsText"));
                SetRef(so, "rewardMessageText", FindTMP(canvasT, "CaptureRewardPopupPanel/RewardCard/RewardMessageText"));
                SetRef(so, "rewardCloseButton", canvasT.Find("CaptureRewardPopupPanel/RewardCard/RewardCloseButton")?.GetComponent<Button>());
                SetRef(so, "screenFlashImage", canvasT.Find("CaptureScreenFlash")?.GetComponent<Image>());
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(captureFx);
            }
        }

        #region Helpers

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }

        private static void TryAddTrackedPoseDriverIfAvailable(GameObject cameraObj)
        {
            if (cameraObj == null) return;

            // Prefer the Input System tracked pose driver (matches the XROrigin warning text).
            string[] candidateTypes =
            {
                "UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem",
                "UnityEngine.SpatialTracking.TrackedPoseDriver, Unity.XR.LegacyInputHelpers"
            };

            for (int i = 0; i < candidateTypes.Length; i++)
            {
                System.Type t = System.Type.GetType(candidateTypes[i]);
                if (t == null) continue;

                if (cameraObj.GetComponent(t) == null)
                {
                    cameraObj.AddComponent(t);
                }

                return;
            }
        }

        private static GameObject GetOrCreateRootPanel(Transform parent, string name)
        {
            GameObject obj;
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                obj = existing.gameObject;
                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect == null) rect = obj.AddComponent<RectTransform>();
                StretchToParent(rect);

                Image img = obj.GetComponent<Image>();
                if (img == null) img = obj.AddComponent<Image>();
                img.color = new Color(0.08f, 0.08f, 0.12f, 0.6f);

                if (obj.GetComponent<CanvasGroup>() == null)
                {
                    obj.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                obj = CreatePanel(parent, name);
            }

            GeneratedSceneUIFactory.AttachUIAnimator(obj, YallaCatch.UI.UIAnimationType.ScaleBounce, 0.4f);
            return obj;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static TMP_InputField CreateAuthInputField(Transform parent, string name, Vector2 pos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(600, 80);

            Image img = obj.AddComponent<Image>();
            img.color = Color.white;

            TMP_InputField input = obj.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-20, -20);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.black;
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Left;

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(obj.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = new Vector2(-20, -20);
            TextMeshProUGUI phtmp = phObj.AddComponent<TextMeshProUGUI>();
            phtmp.text = "Enter " + name.Replace("Input", "");
            phtmp.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            phtmp.fontSize = 32;
            phtmp.alignment = TextAlignmentOptions.Left;

            input.textComponent = tmp;
            input.placeholder = phtmp;

            return input;
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            if (value == null) return;
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"[FullSceneGenerator] Property '{propName}' not found on {so.targetObject.GetType().Name}");
            }
        }

        private static TextMeshProUGUI FindTMP(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t?.GetComponent<TextMeshProUGUI>();
        }

        private static GameObject CreateScrollContainer(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            // ScrollView root
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = pos;
            scrollRect.sizeDelta = size;

            var scrollView = scrollObj.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Elastic;
            scrollObj.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.5f);
            scrollObj.AddComponent<UnityEngine.UI.Mask>();

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            scrollView.viewport = vpRect;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = cRect;

            return scrollObj;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.6f);
            
            // Phase 4: Add CanvasGroup for smooth transitions
            obj.AddComponent<CanvasGroup>();
            
            return obj;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Color color, Vector2? size = null, int fontSize = 30)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(500, 80);

            var img = obj.AddComponent<Image>();
            img.color = color;

            var btn = obj.AddComponent<Button>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontSize = fontSize;

            return btn;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 pos, Color color, int size = 36)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(600, 60);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontSize = size;

            return tmp;
        }

        private static TMP_InputField CreateInputField(Transform parent, string name, Vector2 pos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(500, 120);

            var img = obj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);

            var input = obj.AddComponent<TMP_InputField>();

            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-20, -10);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            tmp.fontSize = 20;

            // Placeholder
            var phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(obj.transform, false);
            var phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = new Vector2(-20, -10);
            var phtmp = phObj.AddComponent<TextMeshProUGUI>();
            phtmp.text = "Describe the issue...";
            phtmp.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            phtmp.fontSize = 20;

            input.textComponent = tmp;
            input.placeholder = phtmp;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;

            return input;
        }

        private static TMP_Dropdown CreateTMPDropdown(Transform parent, string name, Vector2 pos, Vector2 size, List<string> options = null)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = pos;
            rootRect.sizeDelta = size;

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bg;

            // Caption text
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 8f);
            labelRect.offsetMax = new Vector2(-70f, -8f);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.color = Color.white;
            label.fontSize = 26;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = options != null && options.Count > 0 ? options[0] : "Select";
            dropdown.captionText = label;

            // Arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(root.transform, false);
            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-18f, 0f);
            arrowRect.sizeDelta = new Vector2(36f, 36f);
            TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.color = new Color(0.85f, 0.85f, 0.9f);
            arrowText.fontSize = 22;
            arrowText.alignment = TextAlignmentOptions.Center;

            // Template (dropdown popup)
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(root.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -6f);
            templateRect.sizeDelta = new Vector2(0f, 260f);
            Image templateBg = templateObj.AddComponent<Image>();
            templateBg.color = new Color(0.09f, 0.09f, 0.13f, 0.98f);
            ScrollRect templateScroll = templateObj.AddComponent<ScrollRect>();
            templateScroll.horizontal = false;
            templateScroll.vertical = true;
            templateScroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            templateScroll.viewport = viewportRect;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 40f);
            templateScroll.content = contentRect;

            // Item template
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 48f);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();

            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.14f, 0.14f, 0.2f, 1f);

            GameObject itemCheckObj = new GameObject("Item Checkmark");
            itemCheckObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemCheckRect = itemCheckObj.AddComponent<RectTransform>();
            itemCheckRect.anchorMin = new Vector2(0f, 0.5f);
            itemCheckRect.anchorMax = new Vector2(0f, 0.5f);
            itemCheckRect.pivot = new Vector2(0f, 0.5f);
            itemCheckRect.anchoredPosition = new Vector2(12f, 0f);
            itemCheckRect.sizeDelta = new Vector2(18f, 18f);
            Image itemCheckImage = itemCheckObj.AddComponent<Image>();
            itemCheckImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);

            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(40f, 4f);
            itemLabelRect.offsetMax = new Vector2(-12f, -4f);
            TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.color = Color.white;
            itemLabel.fontSize = 22;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.text = "Option";

            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = itemCheckImage;
            itemToggle.isOn = true;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            dropdown.ClearOptions();
            dropdown.AddOptions(options ?? new List<string> { "Select" });
            dropdown.RefreshShownValue();

            templateObj.SetActive(false);
            return dropdown;
        }

        /// <summary>
        /// Creates a list item prefab in Assets/Prefabs/
        /// </summary>
        private static void CreateListItemPrefab(string name, string[] childNames)
        {
            string prefabPath = $"Assets/Prefabs/{name}.prefab";

            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Build the prefab structure
            GameObject root = new GameObject(name);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(900, 160); // Taller premium card

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.5f); // Glassmorphism dark

            // Add a horizontal layout
            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 15, 15); // Better padding
            hlg.spacing = 25; // More spacing between elements
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            foreach (string childName in childNames)
            {
                if (childName.Contains("Image"))
                {
                    // Image child
                    GameObject img = new GameObject(childName);
                    img.transform.SetParent(root.transform, false);
                    var r = img.AddComponent<RectTransform>();
                    r.sizeDelta = new Vector2(80, 80);
                    var i = img.AddComponent<Image>();
                    i.color = new Color(0.25f, 0.25f, 0.35f);

                    var le = img.AddComponent<LayoutElement>();
                    le.preferredWidth = 80;
                    le.preferredHeight = 80;
                }
                else if (childName.Contains("Button"))
                {
                    // Button child
                    GameObject btnObj = new GameObject(childName);
                    btnObj.transform.SetParent(root.transform, false);
                    var r = btnObj.AddComponent<RectTransform>();
                    r.sizeDelta = new Vector2(180, 80);
                    var i = btnObj.AddComponent<Image>();
                    i.color = new Color(0.1f, 0.7f, 0.4f); // Emerald active button
                    btnObj.AddComponent<Button>();

                    var le = btnObj.AddComponent<LayoutElement>();
                    le.preferredWidth = 180;
                    le.preferredHeight = 80;

                    // Button text
                    GameObject txt = new GameObject("Text");
                    txt.transform.SetParent(btnObj.transform, false);
                    var tR = txt.AddComponent<RectTransform>();
                    tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one; tR.sizeDelta = Vector2.zero;
                    var t = txt.AddComponent<TextMeshProUGUI>();
                    t.text = childName.Replace("Button", "");
                    t.alignment = TextAlignmentOptions.Center;
                    t.fontSize = 24;
                    t.color = Color.white;
                }
                else if (childName.Contains("Bar") || childName.Contains("Slider"))
                {
                    // Slider/progress bar
                    GameObject sObj = new GameObject(childName);
                    sObj.transform.SetParent(root.transform, false);
                    var r = sObj.AddComponent<RectTransform>();
                    r.sizeDelta = new Vector2(200, 20);
                    sObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
                    var slider = sObj.AddComponent<Slider>();

                    var le = sObj.AddComponent<LayoutElement>();
                    le.preferredWidth = 200;
                    le.preferredHeight = 20;

                    // Fill
                    GameObject fillArea = new GameObject("Fill Area");
                    fillArea.transform.SetParent(sObj.transform, false);
                    var faR = fillArea.AddComponent<RectTransform>();
                    faR.anchorMin = Vector2.zero; faR.anchorMax = Vector2.one; faR.sizeDelta = Vector2.zero;
                    GameObject fill = new GameObject("Fill");
                    fill.transform.SetParent(fillArea.transform, false);
                    var fR = fill.AddComponent<RectTransform>();
                    fR.anchorMin = Vector2.zero; fR.anchorMax = Vector2.one; fR.sizeDelta = Vector2.zero;
                    fill.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.4f);
                    slider.fillRect = fR;
                }
                else if (childName == "Checkmark")
                {
                    // Checkmark indicator
                    GameObject chk = new GameObject(childName);
                    chk.transform.SetParent(root.transform, false);
                    var r = chk.AddComponent<RectTransform>();
                    r.sizeDelta = new Vector2(40, 40);
                    var t = chk.AddComponent<TextMeshProUGUI>();
                    t.text = "✓";
                    t.color = Color.green;
                    t.fontSize = 30;
                    t.alignment = TextAlignmentOptions.Center;

                    var le = chk.AddComponent<LayoutElement>();
                    le.preferredWidth = 40;
                    le.preferredHeight = 40;

                    chk.SetActive(false);
                }
                else
                {
                    // Text child
                    GameObject txt = new GameObject(childName);
                    txt.transform.SetParent(root.transform, false);
                    var r = txt.AddComponent<RectTransform>();
                    r.sizeDelta = new Vector2(200, 40);
                    var t = txt.AddComponent<TextMeshProUGUI>();
                    t.text = childName.Replace("Text", "");
                    t.color = Color.white;
                    t.fontSize = 20;

                    var le = txt.AddComponent<LayoutElement>();
                    le.preferredWidth = 200;
                    le.preferredHeight = 40;
                    le.flexibleWidth = 1;
                }
            }

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"<color=cyan>Prefab created: {prefabPath}</color>");
        }

        #endregion
    }
}

