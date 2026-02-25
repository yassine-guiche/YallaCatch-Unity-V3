using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using YallaCatch;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.Managers;
using YallaCatch.UI;

namespace YallaCatch.Editor
{
    public static class GeneratedSceneValidator
    {
        [MenuItem("YallaCatch / Validate Generated Scene")]
        public static void ValidateGeneratedScene()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (IsMultiSceneGeneratedRole(scene.name))
            {
                string msg = $"Active scene '{scene.name}' uses the new multi-scene architecture.\n\n" +
                             "The legacy 'Validate Generated Scene' check expects the old single full-scene layout and will report false errors.\n\n" +
                             "Use 'YallaCatch > Validate Project Readiness' (or 'Validate Generated Scenes') for role-aware validation.";
                Debug.LogWarning($"[GeneratedSceneValidator] {msg}");
                EditorUtility.DisplayDialog("Legacy Validator Skipped", msg, "OK");
                return;
            }

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            infos.Add($"Scene: {scene.name}");

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) AddError(errors, "Canvas", "Missing Canvas in active scene.");

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                AddError(errors, "EventSystem", "Missing EventSystem.");
            }

            GameObject managersObj = GameObject.Find("Managers");
            GameObject coreObj = GameObject.Find("Core");
            GameObject effectsObj = GameObject.Find("Effects");

            if (managersObj == null) AddError(errors, "Managers", "Missing root object 'Managers'.");
            if (coreObj == null) AddError(errors, "Core", "Missing root object 'Core'.");
            if (effectsObj == null) AddWarning(warnings, "Effects", "Missing root object 'Effects'. Capture effects will be incomplete.");

            AuthManager authManager = ValidateRequiredComponent<AuthManager>(managersObj, "Managers", errors);
            UIManager uiManager = ValidateRequiredComponent<UIManager>(managersObj, "Managers", errors);
            ValidateRequiredComponent<APIClient>(managersObj, "Managers", errors);
            ValidateRequiredComponent<GameManager>(managersObj, "Managers", errors);
            ValidateRequiredComponent<RewardsManager>(managersObj, "Managers", errors);
            ValidateRequiredComponent<MarketplaceManager>(managersObj, "Managers", errors);
            ValidateRequiredComponent<SocialManager>(managersObj, "Managers", errors);
            ValidateRequiredComponent<ChallengesManager>(managersObj, "Managers", errors);
            ValidateRequiredComponent<ClaimsManager>(managersObj, "Managers", errors);
            ARManager arManager = ValidateRequiredComponent<ARManager>(managersObj, "Managers", errors);
            ValidateOptionalComponent<AchievementManager>(managersObj, "Managers", warnings);
            ValidateOptionalComponent<NotificationManager>(managersObj, "Managers", warnings);
            ValidateOptionalComponent<AdMobManager>(managersObj, "Managers", warnings);

            MapController mapController = ValidateRequiredComponent<MapController>(coreObj, "Core", errors);
            CameraLiveManager cameraLiveManager = ValidateRequiredComponent<CameraLiveManager>(coreObj, "Core", errors);
            GameModeManager gameModeManager = ValidateRequiredComponent<GameModeManager>(coreObj, "Core", errors);
            CaptureController captureController = ValidateRequiredComponent<CaptureController>(coreObj, "Core", errors);
            ValidateOptionalComponent<GPSManager>(coreObj, "Core", warnings);
            CaptureAnimationController captureAnimationController = ValidateRequiredComponent<CaptureAnimationController>(effectsObj, "Effects", errors);

            if (authManager != null)
            {
                ValidateSerializedRefs(authManager, errors, warnings,
                    requiredRefs: new[]
                    {
                        "loginEmailInput","loginPasswordInput","loginButton","guestLoginButton","showRegisterButton",
                        "registerPanel","registerUsernameInput","registerEmailInput","registerPasswordInput",
                        "registerButton","showLoginButton","errorText","loadingIndicator"
                    });
            }

            if (uiManager != null)
            {
                ValidateSerializedRefs(uiManager, errors, warnings,
                    requiredRefs: new[]
                    {
                        "loginPanel","mainMenuPanel","gamePanel","rewardsPanel","profilePanel",
                        "settingsPanel","captureDialog","messageDialog","loadingOverlay","capturePanel",
                        "marketplacePanel","socialPanel","challengesPanel","claimsPanel",
                        "inventoryPanel","leaderboardPanel","achievementsPanel","notificationsPanel",
                        "rewardsContainer","categoryDropdown","favoritesButton",
                        "marketplaceContainer","inventoryButton",
                        "friendsContainer","requestsContainer","leaderboardButton",
                        "challengesContainer","challengesProgressText",
                        "claimsContainer","qrCodePanel","qrCodeImage",
                        "inventoryContainer","inventorySummaryText",
                        "leaderboardContainer","leaderboardSummaryText",
                        "achievementsContainer","achievementsSummaryText","refreshAchievementsButton",
                        "notificationsContainer","notificationsSummaryText","markAllNotificationsReadButton","refreshNotificationsButton",
                        "reportPanel","reportTypeDropdown","reportDescriptionInput","submitReportButton"
                    },
                    optionalRefs: new[]
                    {
                        "rewardItemPrefab","marketplaceItemPrefab","friendItemPrefab","friendRequestPrefab","challengeItemPrefab","claimItemPrefab",
                        "rewardedAdButton","powerUpIndicator","powerUpTimerText","powerUpIcon","confirmDialog"
                    });
            }

            if (mapController != null)
            {
                ValidateSerializedRefs(mapController, errors, warnings,
                    requiredRefs: new[]
                    {
                        "mapImage","fullScreenMapContainer","centerOnPlayerButton","zoomInButton","zoomOutButton",
                        "playerMarkerPrefab","prizeMarkerPrefab","markersContainer","heatmapOverlay"
                    });
            }

            if (cameraLiveManager != null)
            {
                ValidateSerializedRefs(cameraLiveManager, errors, warnings,
                    requiredRefs: new[]
                    {
                        "cameraFeed","miniMapImage","miniMapContainer","miniMapBorder","playerDot",
                        "prizeMarkersContainer","prizeMarkerPrefab","catchButtonObject","catchButton","catchButtonText",
                        "catchButtonGlow","prizeOverlayContainer"
                    },
                    optionalRefs: new[] { "mainCamera","prizeOverlayPrefab" });
            }

            if (arManager != null)
            {
                ValidateSerializedRefs(arManager, errors, warnings,
                    requiredRefs: new[] { "arSession", "arSessionOrigin", "arRaycastManager", "arPlaneManager" });
            }

            if (gameModeManager != null)
            {
                ValidateSerializedRefs(gameModeManager, errors, warnings,
                    requiredRefs: new[]
                    {
                        "cameraViewContainer","mapViewContainer","switchModeButton","switchModeText","cameraManager","mapController"
                    },
                    optionalRefs: new[] { "switchModeIcon","cameraIcon","mapIcon" });
            }

            if (captureController != null)
            {
                ValidateSerializedRefs(captureController, errors, warnings,
                    requiredRefs: new[] { "captureButton","arInstructions","reportIssueButton" },
                    optionalRefs: new[] { "arSession","arSessionOrigin","arPlaneManager","arRaycastManager","prizePrefab","captureParticles" });
            }

            if (captureAnimationController != null)
            {
                ValidateSerializedRefs(captureAnimationController, errors, warnings,
                    requiredRefs: new[]
                    {
                        "prizeBoxPrefab","boxSpawnPoint",
                        "captureParticles","starBurstParticles","confettiParticles","glowParticles",
                        "rewardPopupPanel","rewardIcon","rewardNameText","rewardPointsText","rewardMessageText","rewardCloseButton",
                        "screenFlashImage"
                    },
                    optionalRefs: new[] { "boxOpenSound","rewardSound","particlesSound" });
            }

            LogSummary(infos, errors, warnings);

            string title = errors.Count == 0 ? "Validation Passed" : "Validation Failed";
            string message = $"Errors: {errors.Count}\nWarnings: {warnings.Count}\nCheck Console for details.";
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static T ValidateRequiredComponent<T>(GameObject root, string rootName, List<string> errors) where T : Component
        {
            if (root == null)
            {
                AddError(errors, rootName, $"Cannot validate {typeof(T).Name} because root '{rootName}' is missing.");
                return null;
            }

            T component = root.GetComponent<T>();
            if (component == null)
            {
                AddError(errors, typeof(T).Name, $"Missing component on '{rootName}'.");
            }
            return component;
        }

        private static T ValidateOptionalComponent<T>(GameObject root, string rootName, List<string> warnings) where T : Component
        {
            if (root == null)
            {
                AddWarning(warnings, rootName, $"Cannot validate {typeof(T).Name}; root '{rootName}' is missing.");
                return null;
            }

            T component = root.GetComponent<T>();
            if (component == null)
            {
                AddWarning(warnings, typeof(T).Name, $"Optional component missing on '{rootName}'.");
            }
            return component;
        }

        private static void ValidateSerializedRefs(Object target, List<string> errors, List<string> warnings, string[] requiredRefs, string[] optionalRefs = null)
        {
            if (target == null) return;

            SerializedObject so = new SerializedObject(target);

            foreach (string field in requiredRefs)
            {
                SerializedProperty prop = so.FindProperty(field);
                if (prop == null)
                {
                    AddError(errors, target.GetType().Name, $"Serialized field '{field}' not found (generator/code drift).");
                    continue;
                }

                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    AddWarning(warnings, target.GetType().Name, $"Field '{field}' is not an object reference (validator skipped null check).");
                    continue;
                }

                if (prop.objectReferenceValue == null)
                {
                    AddError(errors, target.GetType().Name, $"Missing required ref '{field}'.");
                }
            }

            if (optionalRefs == null) return;

            foreach (string field in optionalRefs)
            {
                SerializedProperty prop = so.FindProperty(field);
                if (prop == null)
                {
                    AddWarning(warnings, target.GetType().Name, $"Optional serialized field '{field}' not found.");
                    continue;
                }

                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (prop.objectReferenceValue == null)
                {
                    AddWarning(warnings, target.GetType().Name, $"Optional ref '{field}' is not assigned.");
                }
            }
        }

        private static void LogSummary(List<string> infos, List<string> errors, List<string> warnings)
        {
            Debug.Log("[GeneratedSceneValidator] Validation started");

            foreach (string info in infos)
            {
                Debug.Log($"[GeneratedSceneValidator] {info}");
            }

            foreach (string warning in warnings)
            {
                Debug.LogWarning($"[GeneratedSceneValidator] {warning}");
            }

            foreach (string error in errors)
            {
                Debug.LogError($"[GeneratedSceneValidator] {error}");
            }

            string color = errors.Count == 0 ? "green" : "red";
            Debug.Log($"<color={color}>[GeneratedSceneValidator] Completed. Errors: {errors.Count}, Warnings: {warnings.Count}</color>");
        }

        private static void AddError(List<string> errors, string scope, string message)
        {
            errors.Add($"[{scope}] {message}");
        }

        private static void AddWarning(List<string> warnings, string scope, string message)
        {
            warnings.Add($"[{scope}] {message}");
        }

        private static bool IsMultiSceneGeneratedRole(string sceneName)
        {
            switch (sceneName)
            {
                case "BootScene":
                case "MetaScene":
                case "GameplayMapScene":
                case "GameplayARScene":
                case "OverlayScene":
                case "TestHarnessScene":
                    return true;
                default:
                    return false;
            }
        }
    }
}
