using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Direct meta-scene panel builder.
    /// Creates non-gameplay panel content with the exact object names expected by UIManager/UIManagerExtensions wiring.
    /// </summary>
    internal static class GeneratedMetaUIBuilder
    {
        internal static void BuildAll(Transform canvasT)
        {
            BuildCapturePanel(canvasT);
            BuildSettingsPanel(canvasT);
            BuildMainMenuPanel(canvasT);
            BuildRewardsPanel(canvasT);
            BuildMarketplacePanel(canvasT);
            BuildSocialPanel(canvasT);
            BuildChallengesPanel(canvasT);
            BuildClaimsPanel(canvasT);
            BuildProfilePanel(canvasT);
        }

        private static void BuildCapturePanel(Transform canvasT)
        {
            GameObject capturePanel = GetOrCreateRootPanel(canvasT, "CapturePanel");
            ClearChildren(capturePanel.transform);
            capturePanel.SetActive(false);
        }

        private static void BuildSettingsPanel(Transform canvasT)
        {
            GameObject settingsPanel = GetOrCreateRootPanel(canvasT, "SettingsPanel");
            ClearChildren(settingsPanel.transform);
            SetPanelColor(settingsPanel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(settingsPanel.transform, "SettingsTitle", "SYSTEM PREFERENCES", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(1000f, 90f));

            GameObject settingsCard = GeneratedSceneUIFactory.CreatePanel(settingsPanel.transform, "SettingsCard", new Color(0.08f, 0.08f, 0.12f, 0.5f));
            RectTransform cardRect = settingsCard.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, 150f);
            cardRect.sizeDelta = new Vector2(900f, 1100f);

            GeneratedSceneUIFactory.CreateText(settingsCard.transform, "AudioHeader", "AUDIO & HAPTICS", new Vector2(-220f, 450f), new Color(0.5f, 0.7f, 1f), 24, new Vector2(420f, 50f));
            GeneratedSceneUIFactory.CreateButton(settingsCard.transform, "SoundToggle", "MASTER AUDIO [ON]", new Vector2(0f, 350f), new Color(0.15f, 0.15f, 0.2f), new Vector2(800f, 120f), 30);
            GeneratedSceneUIFactory.CreateButton(settingsCard.transform, "HapticsToggle", "HAPTIC FEEDBACK [ON]", new Vector2(0f, 200f), new Color(0.15f, 0.15f, 0.2f), new Vector2(800f, 120f), 30);

            GeneratedSceneUIFactory.CreateText(settingsCard.transform, "PrivacyHeader", "PRIVACY & LOCATION", new Vector2(-210f, 50f), new Color(0.5f, 0.7f, 1f), 24, new Vector2(420f, 50f));
            GeneratedSceneUIFactory.CreateButton(settingsCard.transform, "NotifToggle", "PUSH NOTIFICATIONS [ON]", new Vector2(0f, -50f), new Color(0.15f, 0.15f, 0.2f), new Vector2(800f, 120f), 30);
            GeneratedSceneUIFactory.CreateButton(settingsCard.transform, "LocationToggle", "GPS TRACKING [ON]", new Vector2(0f, -200f), new Color(0.15f, 0.15f, 0.2f), new Vector2(800f, 120f), 30);
            GeneratedSceneUIFactory.CreateButton(settingsCard.transform, "LogoutButton", "DISCONNECT ACCOUNT", new Vector2(0f, -400f), new Color(0.8f, 0.2f, 0.2f), new Vector2(800f, 120f), 30);

            GeneratedSceneUIFactory.CreateButton(settingsPanel.transform, "CloseSettingsButton", "<- NAV MENU", new Vector2(0f, -700f), new Color(0.2f, 0.2f, 0.3f), new Vector2(400f, 100f), 30);
            settingsPanel.SetActive(false);
        }

        private static void BuildMainMenuPanel(Transform canvasT)
        {
            GameObject mainMenu = GetOrCreateRootPanel(canvasT, "MainMenuPanel");
            ClearChildren(mainMenu.transform);
            SetPanelColor(mainMenu, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateImage(mainMenu.transform, "GameLogo", new Vector2(0f, 800f), new Vector2(600f, 150f), "UI/GameLogo");
            GeneratedSceneUIFactory.CreateText(mainMenu.transform, "SubtitleText", "COMMERCIAL GAMING ECOSYSTEM", new Vector2(0f, 740f), new Color(0.5f, 0.5f, 0.6f), 24, new Vector2(1000f, 50f));
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "SettingsButton", "SET", new Vector2(430f, 800f), new Color(0.15f, 0.15f, 0.2f), new Vector2(100f, 80f), 22);

            GameObject playerCard = GeneratedSceneUIFactory.CreatePanel(mainMenu.transform, "PlayerCard", new Color(0.08f, 0.08f, 0.12f, 0.6f));
            RectTransform cardRect = playerCard.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, 480f);
            cardRect.sizeDelta = new Vector2(850f, 260f);

            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(playerCard.transform, false);
            RectTransform avRect = avatar.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0.5f, 0.5f);
            avRect.anchorMax = new Vector2(0.5f, 0.5f);
            avRect.anchoredPosition = new Vector2(-280f, 10f);
            avRect.sizeDelta = new Vector2(160f, 160f);
            avatar.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);

            GeneratedSceneUIFactory.CreateText(playerCard.transform, "UsernameText", "Premium Player", new Vector2(50f, 70f), Color.white, 40, new Vector2(520f, 70f));
            GeneratedSceneUIFactory.CreateText(playerCard.transform, "LevelText", "Level 1", new Vector2(50f, 20f), new Color(0.4f, 0.8f, 0.5f), 28, new Vector2(520f, 60f));
            GeneratedSceneUIFactory.CreateText(playerCard.transform, "PointsText", "0", new Vector2(50f, -30f), new Color(1f, 0.8f, 0.2f), 34, new Vector2(520f, 60f));
            CreateSlider(playerCard.transform, "ExperienceSlider", new Vector2(0f, -90f), new Vector2(750f, 16f));

            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "PlayButton", "ENTER WORLD", new Vector2(0f, 140f), new Color(0.1f, 0.75f, 0.35f), new Vector2(850f, 140f), 45);

            float gridY = -120f;
            float spacingX = 290f;
            float spacingY = 160f;
            Vector2 btnSize = new Vector2(270f, 140f);

            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "RewardsButton", "Arsenal", new Vector2(-spacingX, gridY), new Color(0.8f, 0.5f, 0.1f), btnSize, 26);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "MarketplaceButton", "Market", new Vector2(0f, gridY), new Color(0.2f, 0.4f, 0.8f), btnSize, 26);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "ChallengesButton", "Quests", new Vector2(spacingX, gridY), new Color(0.8f, 0.3f, 0.3f), btnSize, 26);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "SocialButton", "Network", new Vector2(-spacingX, gridY - spacingY), new Color(0.5f, 0.3f, 0.8f), btnSize, 26);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "ClaimsButton", "Wallet", new Vector2(0f, gridY - spacingY), new Color(0.5f, 0.4f, 0.3f), btnSize, 26);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "ProfileButton", "Profile", new Vector2(spacingX, gridY - spacingY), new Color(0.3f, 0.3f, 0.4f), btnSize, 26);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "AchievementsButton", "ACHIEVEMENTS", new Vector2(-145f, gridY - (spacingY * 2f)), new Color(0.28f, 0.52f, 0.24f), new Vector2(415f, 120f), 28);
            GeneratedSceneUIFactory.CreateButton(mainMenu.transform, "NotificationsButton", "NOTIFICATIONS", new Vector2(145f, gridY - (spacingY * 2f)), new Color(0.22f, 0.35f, 0.66f), new Vector2(415f, 120f), 28);
        }

        private static void BuildRewardsPanel(Transform canvasT)
        {
            GameObject panel = GetOrCreateRootPanel(canvasT, "RewardsPanel");
            ClearChildren(panel.transform);
            SetPanelColor(panel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "RewardsTitle", "ARSENAL", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(900f, 80f));
            GeneratedSceneUIFactory.CreateTMPDropdown(panel.transform, "CategoryDropdown", new Vector2(-220f, 680f), new Vector2(400f, 80f), new System.Collections.Generic.List<string> { "All" });
            GeneratedSceneUIFactory.CreateButton(panel.transform, "FavoritesButton", "FAVORITES", new Vector2(220f, 680f), new Color(0.8f, 0.6f, 0.1f), new Vector2(400f, 80f), 28);
            GeneratedSceneUIFactory.CreateScrollContainer(panel.transform, "RewardsContainer", new Vector2(0f, 100f), new Vector2(950f, 1000f));
            GeneratedSceneUIFactory.CreateButton(panel.transform, "CloseRewardsButton", "<- NAV MENU", new Vector2(0f, -780f), new Color(0.2f, 0.2f, 0.3f), new Vector2(500f, 100f), 32);
            panel.SetActive(false);
        }

        private static void BuildMarketplacePanel(Transform canvasT)
        {
            GameObject panel = GetOrCreateRootPanel(canvasT, "MarketplacePanel");
            ClearChildren(panel.transform);
            SetPanelColor(panel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "MarketplaceTitle", "GLOBAL MARKET", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(1000f, 80f));
            GeneratedSceneUIFactory.CreateButton(panel.transform, "InventoryButton", "INVENTORY", new Vector2(0f, 680f), new Color(0.2f, 0.35f, 0.7f), new Vector2(850f, 80f), 28);
            GeneratedSceneUIFactory.CreateScrollContainer(panel.transform, "MarketplaceContainer", new Vector2(0f, 80f), new Vector2(950f, 1060f));
            GeneratedSceneUIFactory.CreateButton(panel.transform, "CloseMarketButton", "<- NAV MENU", new Vector2(0f, -780f), new Color(0.2f, 0.2f, 0.3f), new Vector2(500f, 100f), 32);
            panel.SetActive(false);
        }

        private static void BuildSocialPanel(Transform canvasT)
        {
            GameObject panel = GetOrCreateRootPanel(canvasT, "SocialPanel");
            ClearChildren(panel.transform);
            SetPanelColor(panel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "SocialTitle", "NETWORK", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(900f, 80f));
            GeneratedSceneUIFactory.CreateButton(panel.transform, "LeaderboardButton", "LEADERBOARD", new Vector2(0f, 700f), new Color(0.25f, 0.45f, 0.75f), new Vector2(850f, 80f), 28);

            GeneratedSceneUIFactory.CreateText(panel.transform, "FriendsHeader", "FRIENDS", new Vector2(0f, 590f), new Color(0.5f, 0.7f, 1f), 24, new Vector2(900f, 50f));
            GeneratedSceneUIFactory.CreateScrollContainer(panel.transform, "FriendsContainer", new Vector2(0f, 240f), new Vector2(950f, 620f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "RequestsHeader", "PENDING REQUESTS", new Vector2(0f, -110f), new Color(0.5f, 0.7f, 1f), 24, new Vector2(900f, 50f));
            GeneratedSceneUIFactory.CreateScrollContainer(panel.transform, "RequestsContainer", new Vector2(0f, -380f), new Vector2(950f, 360f));

            GeneratedSceneUIFactory.CreateButton(panel.transform, "CloseSocialButton", "<- NAV MENU", new Vector2(0f, -860f), new Color(0.2f, 0.2f, 0.3f), new Vector2(500f, 100f), 32);
            panel.SetActive(false);
        }

        private static void BuildChallengesPanel(Transform canvasT)
        {
            GameObject panel = GetOrCreateRootPanel(canvasT, "ChallengesPanel");
            ClearChildren(panel.transform);
            SetPanelColor(panel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "ChallengesTitle", "QUESTS", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(900f, 80f));
            GeneratedSceneUIFactory.CreateText(panel.transform, "ChallengesProgressText", "Daily Progress: 0/0", new Vector2(0f, 690f), new Color(0.5f, 0.7f, 1f), 26, new Vector2(900f, 60f));
            GeneratedSceneUIFactory.CreateScrollContainer(panel.transform, "ChallengesContainer", new Vector2(0f, 80f), new Vector2(950f, 1060f));
            GeneratedSceneUIFactory.CreateButton(panel.transform, "CloseChallengesButton", "<- NAV MENU", new Vector2(0f, -780f), new Color(0.2f, 0.2f, 0.3f), new Vector2(500f, 100f), 32);
            panel.SetActive(false);
        }

        private static void BuildClaimsPanel(Transform canvasT)
        {
            GameObject panel = GetOrCreateRootPanel(canvasT, "ClaimsPanel");
            ClearChildren(panel.transform);
            SetPanelColor(panel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "ClaimsTitle", "WALLET", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(900f, 80f));
            GeneratedSceneUIFactory.CreateText(panel.transform, "ClaimsSummaryText", "Your purchased claims and redeem codes", new Vector2(0f, 700f), new Color(0.6f, 0.7f, 0.8f), 22, new Vector2(900f, 60f));
            GeneratedSceneUIFactory.CreateScrollContainer(panel.transform, "ClaimsContainer", new Vector2(0f, 100f), new Vector2(950f, 1000f));
            GeneratedSceneUIFactory.CreateButton(panel.transform, "CloseClaimsButton", "<- NAV MENU", new Vector2(0f, -780f), new Color(0.2f, 0.2f, 0.3f), new Vector2(500f, 100f), 32);
            panel.SetActive(false);
        }

        private static void BuildProfilePanel(Transform canvasT)
        {
            GameObject panel = GetOrCreateRootPanel(canvasT, "ProfilePanel");
            ClearChildren(panel.transform);
            SetPanelColor(panel, new Color(0.04f, 0.04f, 0.06f, 0.7f));

            GeneratedSceneUIFactory.CreateText(panel.transform, "ProfileTitle", "AGENT PROFILE", new Vector2(0f, 800f), new Color(0.9f, 0.9f, 0.95f), 55, new Vector2(900f, 80f));

            GameObject card = GeneratedSceneUIFactory.CreatePanel(panel.transform, "PlayerIDCard", new Color(0.08f, 0.08f, 0.12f, 0.6f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, 140f);
            cardRect.sizeDelta = new Vector2(900f, 1050f);

            GeneratedSceneUIFactory.CreateText(card.transform, "ProfileNameText", "Agent", new Vector2(0f, 420f), Color.white, 40, new Vector2(700f, 80f));
            GeneratedSceneUIFactory.CreateText(card.transform, "LastIpText", "IP NODE // 0.0.0.0", new Vector2(-220f, -70f), new Color(0.6f, 0.6f, 0.7f), 24, new Vector2(380f, 70f));
            GeneratedSceneUIFactory.CreateText(card.transform, "DeviceText", "UPLINK // Device", new Vector2(220f, -70f), new Color(0.6f, 0.6f, 0.7f), 24, new Vector2(380f, 70f));
            GeneratedSceneUIFactory.CreateText(card.transform, "LastActiveText", "LAST SIGNAL: --", new Vector2(0f, -120f), new Color(0.8f, 0.4f, 0.4f), 24, new Vector2(700f, 70f));
            GeneratedSceneUIFactory.CreateText(card.transform, "ClaimsText", "TARGETS\n0", new Vector2(-200f, -300f), Color.white, 36, new Vector2(250f, 120f));
            GeneratedSceneUIFactory.CreateText(card.transform, "StreakText", "STREAK\n0 DAYS", new Vector2(200f, -300f), Color.white, 36, new Vector2(250f, 120f));

            GeneratedSceneUIFactory.CreateButton(panel.transform, "LogoutButton", "DISCONNECT", new Vector2(0f, -550f), new Color(0.8f, 0.2f, 0.2f), new Vector2(400f, 100f), 30);
            GeneratedSceneUIFactory.CreateButton(panel.transform, "CloseProfileButton", "<- NAV MENU", new Vector2(0f, -700f), new Color(0.2f, 0.2f, 0.3f), new Vector2(400f, 100f), 30);
            panel.SetActive(false);
        }

        private static GameObject GetOrCreateRootPanel(Transform canvasT, string name)
        {
            Transform existing = canvasT.Find(name);
            GameObject panel;
            if (existing != null)
            {
                EnsurePanelComponents(existing.gameObject);
                panel = existing.gameObject;
            }
            else
            {
                panel = GeneratedSceneUIFactory.CreatePanel(canvasT, name);
            }
            GeneratedSceneUIFactory.AttachUIAnimator(panel, YallaCatch.UI.UIAnimationType.ScaleBounce, 0.4f);
            return panel;
        }

        private static void EnsurePanelComponents(GameObject obj)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            if (obj.GetComponent<Image>() == null) obj.AddComponent<Image>();
            if (obj.GetComponent<CanvasGroup>() == null) obj.AddComponent<CanvasGroup>();
        }

        private static void SetPanelColor(GameObject panel, Color color)
        {
            if (panel == null) return;
            Image img = panel.GetComponent<Image>();
            if (img == null) img = panel.AddComponent<Image>();
            img.color = color;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.value = 0.35f;

            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            GeneratedSceneUIFactory.StretchToParent(bgRect);
            background.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRect = fillArea.AddComponent<RectTransform>();
            GeneratedSceneUIFactory.StretchToParent(faRect);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            GeneratedSceneUIFactory.StretchToParent(fillRect);
            fill.AddComponent<Image>().color = new Color(0.1f, 0.8f, 0.4f);

            slider.fillRect = fillRect;
            slider.targetGraphic = background.GetComponent<Image>();
        }
    }
}
