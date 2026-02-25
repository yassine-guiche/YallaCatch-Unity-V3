using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Phase 2/3 bootstrap: orchestrator scaffold for future multi-scene generation.
    /// Keeps current FullSceneGenerator flow intact while introducing a canonical scene manifest.
    /// </summary>
    internal static class SceneGenerationOrchestrator
    {
        internal enum GeneratedSceneRole
        {
            Boot,
            Meta,
            GameplayMap,
            GameplayAR,
            Overlay,
            TestHarness,
            LegacyMainSceneCompat
        }

        internal sealed class GeneratedSceneSpec
        {
            public GeneratedSceneRole Role;
            public string SceneName;
            public bool EnabledInBuild = true;
            public bool Additive = false;

            public GeneratedSceneSpec(GeneratedSceneRole role, string sceneName, bool enabledInBuild = true, bool additive = false)
            {
                Role = role;
                SceneName = sceneName;
                EnabledInBuild = enabledInBuild;
                Additive = additive;
            }
        }

        internal static IReadOnlyList<GeneratedSceneSpec> BuildDefaultManifest()
        {
            return new[]
            {
                new GeneratedSceneSpec(GeneratedSceneRole.Boot, "BootScene", enabledInBuild: true, additive: false),
                new GeneratedSceneSpec(GeneratedSceneRole.Meta, "MetaScene", enabledInBuild: true, additive: false),
                new GeneratedSceneSpec(GeneratedSceneRole.GameplayMap, "GameplayMapScene", enabledInBuild: true, additive: true),
                new GeneratedSceneSpec(GeneratedSceneRole.GameplayAR, "GameplayARScene", enabledInBuild: true, additive: true),
                new GeneratedSceneSpec(GeneratedSceneRole.Overlay, "OverlayScene", enabledInBuild: true, additive: true),
                new GeneratedSceneSpec(GeneratedSceneRole.TestHarness, "TestHarnessScene", enabledInBuild: false, additive: false),
            };
        }

        /// <summary>
        /// Temporary compatibility path while the per-scene generators are introduced.
        /// </summary>
        [MenuItem("YallaCatch / Generate All Scenes")]
        internal static void GenerateAllScenes()
        {
            GenerateAllScenesOrchestrated(promptToSaveModifiedScenes: true);
        }

        [MenuItem("YallaCatch / Generate Scene / Boot")]
        internal static void GenerateBootSceneOnly()
        {
            if (!ValidateEditorContext()) return;
            BootSceneGenerator.GenerateBootScene(promptToSaveModifiedScenes: true);
        }

        [MenuItem("YallaCatch / Generate Scene / Meta")]
        internal static void GenerateMetaSceneOnly()
        {
            if (!ValidateEditorContext()) return;
            MetaSceneGenerator.GenerateMetaSceneCompat(promptToSaveModifiedScenes: true);
        }

        [MenuItem("YallaCatch / Generate Scene / Gameplay Map")]
        internal static void GenerateGameplayMapSceneOnly()
        {
            if (!ValidateEditorContext()) return;
            GameplayMapSceneGenerator.GenerateGameplayMapSceneCompat(promptToSaveModifiedScenes: true);
        }

        [MenuItem("YallaCatch / Generate Scene / Gameplay AR")]
        internal static void GenerateGameplayARSceneOnly()
        {
            if (!ValidateEditorContext()) return;
            GameplayARSceneGenerator.GenerateGameplayARSceneCompat(promptToSaveModifiedScenes: true);
        }

        [MenuItem("YallaCatch / Generate Scene / Overlay")]
        internal static void GenerateOverlaySceneOnly()
        {
            if (!ValidateEditorContext()) return;
            OverlaySceneGenerator.GenerateOverlaySceneCompat(promptToSaveModifiedScenes: true);
        }

        /// <summary>
        /// Temporary compatibility path while the per-scene generators are introduced.
        /// </summary>
        internal static void GenerateLegacyMainSceneCompat(bool promptToSaveModifiedScenes = true)
        {
            Debug.Log("[SceneGenerationOrchestrator] Using legacy compatibility path -> FullSceneGenerator.GenerateFullSceneLegacyCompat()");
            FullSceneGenerator.GenerateFullSceneLegacyCompat(promptToSaveModifiedScenes);
        }

        internal static void LogPlannedManifest()
        {
            IReadOnlyList<GeneratedSceneSpec> manifest = BuildDefaultManifest();
            foreach (GeneratedSceneSpec spec in manifest)
            {
                Debug.Log($"[SceneGenerationOrchestrator] Planned scene: {spec.SceneName} ({spec.Role}) build={spec.EnabledInBuild} additive={spec.Additive}");
            }
        }

        internal static bool ValidateEditorContext()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SceneGenerationOrchestrator] Cannot generate scenes while entering/exiting Play Mode.");
                return false;
            }

            return true;
        }

        internal static void GenerateAllScenesOrchestrated(bool promptToSaveModifiedScenes)
        {
            if (!ValidateEditorContext()) return;

            if (promptToSaveModifiedScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[SceneGenerationOrchestrator] Generation cancelled by user.");
                return;
            }

            LogPlannedManifest();

            var generated = new List<EditorBuildSettingsScene>();

            // Transitional multi-scene generation order (compat mode builders backed by FullSceneGenerator).
            string bootScene = BootSceneGenerator.GenerateBootScene(promptToSaveModifiedScenes: false);
            AddIfExists(generated, bootScene, enabled: true);

            string metaScene = MetaSceneGenerator.GenerateMetaSceneCompat(promptToSaveModifiedScenes: false);
            AddIfExists(generated, metaScene, enabled: true);

            string gameplayMapScene = GameplayMapSceneGenerator.GenerateGameplayMapSceneCompat(promptToSaveModifiedScenes: false);
            AddIfExists(generated, gameplayMapScene, enabled: true);

            string gameplayARScene = GameplayARSceneGenerator.GenerateGameplayARSceneCompat(promptToSaveModifiedScenes: false);
            AddIfExists(generated, gameplayARScene, enabled: true);

            string overlayScene = OverlaySceneGenerator.GenerateOverlaySceneCompat(promptToSaveModifiedScenes: false);
            AddIfExists(generated, overlayScene, enabled: true);

            // Keep legacy MainScene generation available via explicit orchestrator compatibility entrypoint, but no longer include it by default.
            Debug.Log("[SceneGenerationOrchestrator] Using generated Boot/Meta/GameplayMap/GameplayAR/Overlay scenes. Legacy MainScene is excluded from default build settings.");

            if (generated.Count > 0)
            {
                EditorBuildSettings.scenes = generated.ToArray();
                Debug.Log($"[SceneGenerationOrchestrator] Build settings updated with {generated.Count} generated scenes.");
            }
        }

        private static void AddIfExists(List<EditorBuildSettingsScene> buildScenes, string scenePath, bool enabled)
        {
            if (string.IsNullOrEmpty(scenePath)) return;
            if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath))) return;

            buildScenes.Add(new EditorBuildSettingsScene(scenePath, enabled));
        }
    }
}
