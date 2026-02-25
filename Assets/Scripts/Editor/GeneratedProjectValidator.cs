using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using YallaCatch;
using YallaCatch.Core;
using YallaCatch.Managers;
using YallaCatch.UI;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Role-aware validator for the generated multi-scene architecture.
    /// Complements GeneratedSceneValidator (which remains focused on the legacy/full gameplay scene).
    /// </summary>
    internal static class GeneratedProjectValidator
    {
        [MenuItem("YallaCatch / Validate Generated Scenes")]
        internal static void ValidateGeneratedScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Validation Blocked", "Exit Play Mode before validating generated scenes.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[GeneratedProjectValidator] Validation cancelled by user.");
                return;
            }

            string originalScenePath = SceneManager.GetActiveScene().path;
            var errors = new List<string>();
            var warnings = new List<string>();

            IReadOnlyList<SceneGenerationOrchestrator.GeneratedSceneSpec> manifest = SceneGenerationOrchestrator.BuildDefaultManifest();
            foreach (SceneGenerationOrchestrator.GeneratedSceneSpec spec in manifest)
            {
                string scenePath = $"Assets/Scenes/{spec.SceneName}.unity";
                if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
                {
                    if (spec.EnabledInBuild)
                        errors.Add($"[{spec.SceneName}] Missing generated scene file: {scenePath}");
                    continue;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ValidateSceneByRole(spec.Role, spec.SceneName, errors, warnings);
            }

            ValidateBuildSettings(manifest, errors, warnings);

            if (!string.IsNullOrEmpty(originalScenePath) && System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", originalScenePath)))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            foreach (string warning in warnings)
                Debug.LogWarning($"[GeneratedProjectValidator] {warning}");
            foreach (string error in errors)
                Debug.LogError($"[GeneratedProjectValidator] {error}");

            Debug.Log($"[GeneratedProjectValidator] Completed. Errors={errors.Count} Warnings={warnings.Count}");
            EditorUtility.DisplayDialog(
                errors.Count == 0 ? "Generated Scenes Valid" : "Generated Scenes Validation Failed",
                $"Errors: {errors.Count}\nWarnings: {warnings.Count}\nCheck Console for details.",
                "OK");
        }

        [MenuItem("YallaCatch / Validate Project Readiness")]
        internal static void ValidateProjectReadiness()
        {
            ValidateGeneratedScenes();
        }

        private static void ValidateSceneByRole(SceneGenerationOrchestrator.GeneratedSceneRole role, string sceneName, List<string> errors, List<string> warnings)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                errors.Add($"[{sceneName}] Failed to open scene.");
                return;
            }

            switch (role)
            {
                case SceneGenerationOrchestrator.GeneratedSceneRole.Boot:
                    RequireComponentInScene<AppSceneBootstrap>(sceneName, errors);
                    RequireRoot(sceneName, "Boot", errors);
                    ValidateBootstrapConfig(sceneName, warnings);
                    WarnIfObjectExists(sceneName, "Core", warnings);
                    break;

                case SceneGenerationOrchestrator.GeneratedSceneRole.Meta:
                    RequireCanvas(sceneName, errors);
                    RequireEventSystem(sceneName, errors);
                    RequireRoot(sceneName, "Managers", errors);
                    RequireComponentOnRoot<UIManager>(sceneName, "Managers", errors);
                    RequireRoot(sceneName, "SceneNavigation", errors);
                    RequireComponentOnRoot<SceneNavigationService>(sceneName, "SceneNavigation", errors);
                    ValidateSceneNavigationConfig(sceneName, warnings);
                    WarnIfCanvasChildExists(sceneName, "GamePanel", warnings);
                    WarnIfObjectExists(sceneName, "Core", warnings);
                    WarnIfObjectExists(sceneName, "ARScaffold", warnings);
                    break;

                case SceneGenerationOrchestrator.GeneratedSceneRole.GameplayMap:
                    RequireCanvas(sceneName, errors);
                    RequireEventSystem(sceneName, errors);
                    RequireRoot(sceneName, "Managers", errors);
                    RequireRoot(sceneName, "Core", errors);
                    RequireRoot(sceneName, "SceneNavigation", errors);
                    RequireComponentOnRoot<SceneNavigationService>(sceneName, "SceneNavigation", errors);
                    ValidateSceneNavigationConfig(sceneName, warnings);
                    RequireComponentOnRoot<MapController>(sceneName, "Core", errors);
                    RequireComponentOnRoot<GameModeManager>(sceneName, "Core", errors);
                    RequireCanvasChild(sceneName, "GamePanel", errors);
                    WarnIfCanvasChildExists(sceneName, "MainMenuPanel", warnings);
                    WarnIfObjectExists(sceneName, "ARScaffold", warnings);
                    break;

                case SceneGenerationOrchestrator.GeneratedSceneRole.GameplayAR:
                    RequireCanvas(sceneName, errors);
                    RequireEventSystem(sceneName, errors);
                    RequireRoot(sceneName, "Managers", errors);
                    RequireRoot(sceneName, "Core", errors);
                    RequireRoot(sceneName, "ARScaffold", errors);
                    RequireRoot(sceneName, "SceneNavigation", errors);
                    RequireComponentOnRoot<SceneNavigationService>(sceneName, "SceneNavigation", errors);
                    ValidateSceneNavigationConfig(sceneName, warnings);
                    RequireComponentOnRoot<ARManager>(sceneName, "Managers", errors);
                    RequireComponentOnRoot<CaptureController>(sceneName, "Core", errors);
                    RequireCanvasChild(sceneName, "GamePanel", errors);
                    WarnIfCanvasChildExists(sceneName, "MainMenuPanel", warnings);
                    break;

                case SceneGenerationOrchestrator.GeneratedSceneRole.Overlay:
                    RequireCanvas(sceneName, errors);
                    RequireCanvasChild(sceneName, "CaptureDialog", errors);
                    RequireCanvasChild(sceneName, "MessageDialog", errors);
                    RequireCanvasChild(sceneName, "ConfirmDialog", errors);
                    RequireCanvasChild(sceneName, "LoadingOverlay", errors);
                    RequireCanvasChild(sceneName, "ReportPanel", errors);
                    RequireCanvasChild(sceneName, "QRCodePanel", errors);
                    RequireCanvasChild(sceneName, "CaptureRewardPopupPanel", errors);
                    RequireCanvasChild(sceneName, "CaptureScreenFlash", errors);
                    WarnIfCanvasChildExists(sceneName, "GamePanel", warnings);
                    WarnIfCanvasChildExists(sceneName, "MainMenuPanel", warnings);
                    WarnIfObjectExists(sceneName, "Managers", warnings);
                    WarnIfObjectExists(sceneName, "Core", warnings);
                    WarnIfObjectExists(sceneName, "Effects", warnings);
                    WarnIfObjectExists(sceneName, "SceneNavigation", warnings);
                    if (GameObject.Find("EventSystem") != null)
                        warnings.Add($"[{sceneName}] EventSystem exists; additive overlay scenes usually should not own a duplicate EventSystem.");
                    break;

                case SceneGenerationOrchestrator.GeneratedSceneRole.TestHarness:
                    warnings.Add($"[{sceneName}] TestHarness scene validation not implemented yet.");
                    break;

                case SceneGenerationOrchestrator.GeneratedSceneRole.LegacyMainSceneCompat:
                    warnings.Add($"[{sceneName}] Legacy scene role not part of default manifest validation.");
                    break;
            }
        }

        private static void ValidateBuildSettings(IReadOnlyList<SceneGenerationOrchestrator.GeneratedSceneSpec> manifest, List<string> errors, List<string> warnings)
        {
            var expectedEnabled = new HashSet<string>();
            var expectedEnabledOrdered = new List<string>();
            foreach (SceneGenerationOrchestrator.GeneratedSceneSpec spec in manifest)
            {
                if (spec.EnabledInBuild)
                {
                    expectedEnabled.Add($"Assets/Scenes/{spec.SceneName}.unity");
                    expectedEnabledOrdered.Add($"Assets/Scenes/{spec.SceneName}.unity");
                }
            }

            var actualEnabled = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    actualEnabled.Add(scene.path);
            }

            foreach (string path in expectedEnabled)
            {
                if (!actualEnabled.Contains(path))
                    errors.Add($"[BuildSettings] Missing enabled generated scene: {path}");
            }

            foreach (string path in actualEnabled)
            {
                if (!expectedEnabled.Contains(path))
                    warnings.Add($"[BuildSettings] Extra enabled scene present (not in default manifest): {path}");
            }

            int orderCheckCount = 0;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                if (orderCheckCount >= expectedEnabledOrdered.Count) break;

                if (scene.path != expectedEnabledOrdered[orderCheckCount])
                {
                    warnings.Add($"[BuildSettings] Scene order mismatch at enabled index {orderCheckCount}: expected '{expectedEnabledOrdered[orderCheckCount]}', found '{scene.path}'.");
                    break;
                }

                orderCheckCount++;
            }
        }

        private static void RequireCanvas(string sceneName, List<string> errors)
        {
            if (Object.FindObjectOfType<Canvas>() == null)
                errors.Add($"[{sceneName}] Missing Canvas.");
        }

        private static void ValidateBootstrapConfig(string sceneName, List<string> warnings)
        {
            AppSceneBootstrap bootstrap = Object.FindObjectOfType<AppSceneBootstrap>();
            if (bootstrap == null) return;

            SerializedObject so = new SerializedObject(bootstrap);
            SerializedProperty initialScene = so.FindProperty("initialSceneName");
            SerializedProperty autoLoad = so.FindProperty("autoLoadOnStart");

            if (initialScene == null || string.IsNullOrWhiteSpace(initialScene.stringValue))
            {
                warnings.Add($"[{sceneName}] AppSceneBootstrap.initialSceneName is missing.");
            }
            else if (!string.Equals(initialScene.stringValue, "MetaScene"))
            {
                warnings.Add($"[{sceneName}] AppSceneBootstrap.initialSceneName is '{initialScene.stringValue}' (expected 'MetaScene' for default flow).");
            }

            if (autoLoad != null && !autoLoad.boolValue)
            {
                warnings.Add($"[{sceneName}] AppSceneBootstrap.autoLoadOnStart is disabled.");
            }
        }

        private static void ValidateSceneNavigationConfig(string sceneName, List<string> warnings)
        {
            GameObject root = GameObject.Find("SceneNavigation");
            if (root == null) return;

            SceneNavigationService service = root.GetComponent<SceneNavigationService>();
            if (service == null) return;

            SerializedObject so = new SerializedObject(service);
            WarnIfMissingOrEmptyString(sceneName, warnings, so, "metaSceneName");
            WarnIfMissingOrEmptyString(sceneName, warnings, so, "gameplayMapSceneName");
            WarnIfMissingOrEmptyString(sceneName, warnings, so, "gameplayARSceneName");
            WarnIfMissingOrEmptyString(sceneName, warnings, so, "overlaySceneName");

            // Transitional state: MetaScene and gameplay scenes still embed overlays, so additive overlay loading
            // should stay disabled until overlay ownership is fully moved to OverlayScene.
            SerializedProperty loadOverlayForGameplay = so.FindProperty("loadOverlayForGameplay");
            if (loadOverlayForGameplay != null && loadOverlayForGameplay.propertyType == SerializedPropertyType.Boolean && loadOverlayForGameplay.boolValue)
            {
                warnings.Add($"[{sceneName}] SceneNavigationService.loadOverlayForGameplay is enabled while scenes still embed overlays; this can duplicate overlay UI.");
            }
        }

        private static void RequireCanvasChild(string sceneName, string childName, List<string> errors)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                errors.Add($"[{sceneName}] Cannot validate Canvas child '{childName}' because Canvas is missing.");
                return;
            }

            if (canvas.transform.Find(childName) == null)
                errors.Add($"[{sceneName}] Missing Canvas child '{childName}'.");
        }

        private static void RequireEventSystem(string sceneName, List<string> errors)
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                errors.Add($"[{sceneName}] Missing EventSystem.");
        }

        private static void RequireRoot(string sceneName, string rootName, List<string> errors)
        {
            if (GameObject.Find(rootName) == null)
                errors.Add($"[{sceneName}] Missing root object '{rootName}'.");
        }

        private static void WarnIfObjectExists(string sceneName, string objectName, List<string> warnings)
        {
            if (GameObject.Find(objectName) != null)
                warnings.Add($"[{sceneName}] Unexpected object '{objectName}' exists for this role.");
        }

        private static void WarnIfCanvasChildExists(string sceneName, string childName, List<string> warnings)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            if (canvas.transform.Find(childName) != null)
                warnings.Add($"[{sceneName}] Unexpected Canvas child '{childName}' exists for this role.");
        }

        private static void WarnIfMissingOrEmptyString(string sceneName, List<string> warnings, SerializedObject so, string propertyName)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                warnings.Add($"[{sceneName}] SceneNavigationService field '{propertyName}' not found.");
                return;
            }

            if (prop.propertyType != SerializedPropertyType.String || string.IsNullOrWhiteSpace(prop.stringValue))
            {
                warnings.Add($"[{sceneName}] SceneNavigationService '{propertyName}' is empty.");
            }
        }

        private static void RequireComponentInScene<T>(string sceneName, List<string> errors) where T : Component
        {
            if (Object.FindObjectOfType<T>() == null)
                errors.Add($"[{sceneName}] Missing required component {typeof(T).Name} in scene.");
        }

        private static void RequireComponentOnRoot<T>(string sceneName, string rootName, List<string> errors) where T : Component
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                errors.Add($"[{sceneName}] Cannot validate {typeof(T).Name}; root '{rootName}' missing.");
                return;
            }

            if (root.GetComponent<T>() == null)
                errors.Add($"[{sceneName}] Missing {typeof(T).Name} on root '{rootName}'.");
        }
    }
}
