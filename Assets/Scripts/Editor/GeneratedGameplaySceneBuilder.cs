using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YallaCatch;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.Managers;
using YallaCatch.UI;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Direct gameplay scene builder (shared by GameplayMapSceneGenerator / GameplayARSceneGenerator).
    /// Uses UI core scaffold + extracted FullSceneGenerator gameplay builders/wiring (no full-scene compat pruning).
    /// </summary>
    internal static class GeneratedGameplaySceneBuilder
    {
        internal enum Variant
        {
            Map,
            AR
        }

        internal static string Generate(Variant variant, string scenePath, bool promptToSaveModifiedScenes = true)
        {
            if (promptToSaveModifiedScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning($"[GeneratedGameplaySceneBuilder] {variant} generation cancelled by user.");
                return null;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Base auth/ui scaffold (Canvas + Managers root + AuthManager/UIManager/APIClient + root panels).
            FullSceneGenerator.GenerateUICoreScaffoldOnly();

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"[GeneratedGameplaySceneBuilder] Canvas not found after UI core scaffold generation ({variant}).");
                return null;
            }

            Transform canvasT = canvas.transform;
            GameObject managersObj = GameObject.Find("Managers") ?? new GameObject("Managers");
            GameObject coreObj = GameObject.Find("Core") ?? new GameObject("Core");
            GameObject effectsObj = GameObject.Find("Effects") ?? new GameObject("Effects");

            AddGameplayManagers(managersObj, includeARManager: variant == Variant.AR);
            AddGameplayCore(coreObj);
            AddGameplayEffects(effectsObj);

            // Build gameplay shell + overlays directly.
            FullSceneGenerator.EnsureGameplayPanelAndRuntimeScaffold(canvasT, coreObj);
            GeneratedOverlayUIBuilder.BuildAll(canvasT);
            FullSceneGenerator.EnsureAREffectsScaffoldForGameplay(canvasT, coreObj, effectsObj);

            // Keep gameplay scene canvas lean: game panel + overlays only.
            CompatSceneGenerationUtilities.PruneCanvasToGameplayAndOverlay();
            CompatSceneGenerationUtilities.SetGamePanelActive(true);

            if (variant == Variant.Map)
            {
                // Map scene should not depend on AR scaffold/runtime.
                CompatSceneGenerationUtilities.DestroyRootIfExists("ARScaffold");
                RemoveManagerComponent<ARManager>(managersObj);
            }

            // Wire UI/gameplay refs after final scene topology is in place.
            FullSceneGenerator.WireUIManagerCanvasReferences(canvasT, managersObj);
            FullSceneGenerator.WireGameplaySceneReferences(canvasT, managersObj, coreObj, effectsObj);

            // Scene-specific routing and default mode behavior.
            SceneNavigationService navigation = CompatSceneGenerationUtilities.EnsureSceneNavigationService();
            CompatSceneGenerationUtilities.ConfigureSceneNavigationService(
                navigation,
                loadOverlayForGameplay: false, // overlays still embedded in direct gameplay scenes during transition
                loadOverlayForMeta: false,
                defaultGameplayTarget: variant == Variant.Map
                    ? SceneNavigationService.GameplayTarget.Map
                    : SceneNavigationService.GameplayTarget.AR);

            CompatSceneGenerationUtilities.WireButtonToAction(canvasT, "GamePanel/TopHUDBar/BackToMenuButton", navigation.LoadMetaScene);

            if (variant == Variant.Map)
            {
                CompatSceneGenerationUtilities.SetGameModeDefault(GameModeManager.GameMode.Map);
                CompatSceneGenerationUtilities.SetGameModeContainers(cameraActive: false, mapActive: true);
            }
            else
            {
                CompatSceneGenerationUtilities.SetGameModeDefault(GameModeManager.GameMode.Camera);
                CompatSceneGenerationUtilities.SetGameModeContainers(cameraActive: true, mapActive: false);
            }

            CompatSceneGenerationUtilities.SaveCurrentScene(scenePath);
            Debug.Log($"<color=green>[GeneratedGameplaySceneBuilder] {variant} gameplay scene generated (direct builder): {scenePath}</color>");
            return scenePath;
        }

        private static void AddGameplayManagers(GameObject managersObj, bool includeARManager)
        {
            // Core managers
            GetOrAdd<GameManager>(managersObj);
            GetOrAdd<ConfigManager>(managersObj);
            GetOrAdd<SoundManager>(managersObj);
            GetOrAdd<NotificationManager>(managersObj);
            GetOrAdd<OfflineManager>(managersObj);
            GetOrAdd<OfflineQueueManager>(managersObj);
            GetOrAdd<AdManager>(managersObj);
            GetOrAdd<AdMobManager>(managersObj);

            // Feature managers
            GetOrAdd<RewardsManager>(managersObj);
            GetOrAdd<MarketplaceManager>(managersObj);
            GetOrAdd<SocialManager>(managersObj);
            GetOrAdd<PowerUpManager>(managersObj);
            GetOrAdd<ChallengesManager>(managersObj);
            GetOrAdd<ClaimsManager>(managersObj);
            GetOrAdd<AchievementManager>(managersObj);
            if (includeARManager)
            {
                GetOrAdd<ARManager>(managersObj);
            }

            // API layer + scaffold dependencies
            GetOrAdd<APIManager>(managersObj);
            GetOrAdd<APIInitializer>(managersObj);
            GetOrAdd<AuthManager>(managersObj);
            GetOrAdd<UIManager>(managersObj);
            GetOrAdd<APIClient>(managersObj);
        }

        private static void AddGameplayCore(GameObject coreObj)
        {
            GetOrAdd<GPSManager>(coreObj);
            GetOrAdd<MapController>(coreObj);
            GetOrAdd<CaptureController>(coreObj);
            GetOrAdd<GameModeManager>(coreObj);
            GetOrAdd<CameraLiveManager>(coreObj);
        }

        private static void AddGameplayEffects(GameObject effectsObj)
        {
            GetOrAdd<CaptureAnimationController>(effectsObj);
        }

        private static void RemoveManagerComponent<T>(GameObject managersObj) where T : Component
        {
            if (managersObj == null) return;

            T comp = managersObj.GetComponent<T>();
            if (comp != null)
            {
                Object.DestroyImmediate(comp, true);
            }
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            if (obj == null) return null;

            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }
    }
}
