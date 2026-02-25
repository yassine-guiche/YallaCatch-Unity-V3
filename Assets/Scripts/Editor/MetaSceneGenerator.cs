using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YallaCatch;
using YallaCatch.Core;
using YallaCatch.API;
using YallaCatch.Managers;
using YallaCatch.UI;

namespace YallaCatch.Editor
{
    /// <summary>
    /// MetaScene generator (direct builder).
    /// Builds a non-gameplay app shell scene using shared UI builders and FullSceneGenerator wiring helpers.
    /// </summary>
    internal static class MetaSceneGenerator
    {
        internal const string ScenePath = "Assets/Scenes/MetaScene.unity";

        internal static string GenerateMetaSceneCompat(bool promptToSaveModifiedScenes = true)
        {
            if (promptToSaveModifiedScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[MetaSceneGenerator] Generation cancelled by user.");
                return null;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Base auth/ui scaffold (Canvas + Managers root + AuthManager/UIManager/APIClient + root panels)
            FullSceneGenerator.GenerateUICoreScaffoldOnly();

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MetaSceneGenerator] Canvas not found after UI core scaffold generation.");
                return null;
            }

            Transform canvasT = canvas.transform;
            GameObject managers = GameObject.Find("Managers") ?? new GameObject("Managers");

            // Add meta-scene managers (exclude AR and gameplay-core scene objects).
            AddMetaManagers(managers);

            // Build direct meta UI content and shared overlays.
            GeneratedMetaUIBuilder.BuildAll(canvasT);
            FullSceneGenerator.EnsureExtendedFeaturePanelsAndListPrefabs(canvasT);
            GeneratedOverlayUIBuilder.BuildAll(canvasT);

            // MetaScene should not include embedded gameplay view panel in the target architecture.
            DestroyCanvasChildIfExists("GamePanel");
            DestroyRootIfExists("Core");
            DestroyRootIfExists("Effects");
            DestroyRootIfExists("ARScaffold");

            // Add scene navigation service and route Play button into multi-scene gameplay flow.
            SceneNavigationService navigation = CompatSceneGenerationUtilities.EnsureSceneNavigationService();
            CompatSceneGenerationUtilities.ConfigureSceneNavigationService(
                navigation,
                loadOverlayForGameplay: false,
                loadOverlayForMeta: false,
                defaultGameplayTarget: SceneNavigationService.GameplayTarget.AR);

            // Serialize UIManager/UIManagerExtensions refs and default runtime listeners.
            FullSceneGenerator.WireUIManagerCanvasReferences(canvasT, managers);

            // Override legacy Play->ShowGamePanel listener with scene navigation.
            CompatSceneGenerationUtilities.WireButtonToAction(canvasT, "MainMenuPanel/PlayButton", navigation.LoadPreferredGameplay);

            SaveCurrentScene(ScenePath);
            Debug.Log("<color=green>[MetaSceneGenerator] MetaScene generated (direct builder).</color>");
            return ScenePath;
        }

        private static void AddMetaManagers(GameObject managersObj)
        {
            // Core service managers safe for meta scene (no Core/GPS dependency loops).
            GetOrAdd<ConfigManager>(managersObj);
            GetOrAdd<SoundManager>(managersObj);
            GetOrAdd<NotificationManager>(managersObj);
            GetOrAdd<OfflineManager>(managersObj);
            GetOrAdd<OfflineQueueManager>(managersObj);
            GetOrAdd<AdManager>(managersObj);
            GetOrAdd<AdMobManager>(managersObj);

            // Feature managers exposed in Meta UI.
            GetOrAdd<RewardsManager>(managersObj);
            GetOrAdd<MarketplaceManager>(managersObj);
            GetOrAdd<SocialManager>(managersObj);
            GetOrAdd<PowerUpManager>(managersObj);
            GetOrAdd<ChallengesManager>(managersObj);
            GetOrAdd<ClaimsManager>(managersObj);
            GetOrAdd<AchievementManager>(managersObj);

            // API bridge/services
            GetOrAdd<APIManager>(managersObj);
            GetOrAdd<APIInitializer>(managersObj);

            // Ensure scaffold-required components exist (GenerateUICoreScaffoldOnly usually adds these).
            GetOrAdd<ThemeManager>(managersObj);
            GetOrAdd<AuthManager>(managersObj);
            GetOrAdd<UIManager>(managersObj);
            GetOrAdd<APIClient>(managersObj);
        }

        private static void DestroyRootIfExists(string rootName)
        {
            GameObject obj = GameObject.Find(rootName);
            if (obj != null)
                Object.DestroyImmediate(obj);
        }

        private static void DestroyCanvasChildIfExists(string childName)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform t = canvas.transform.Find(childName);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        private static void SaveCurrentScene(string scenePath)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
                System.IO.Path.Combine(Application.dataPath, "../", scenePath)));

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }
    }
}
