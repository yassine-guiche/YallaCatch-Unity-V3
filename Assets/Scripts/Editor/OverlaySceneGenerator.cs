using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Overlay scene generator.
    /// Direct builder for UI-only dialogs/overlays used by the multi-scene architecture.
    /// </summary>
    internal static class OverlaySceneGenerator
    {
        internal const string ScenePath = "Assets/Scenes/OverlayScene.unity";

        internal static string GenerateOverlaySceneCompat(bool promptToSaveModifiedScenes = true)
        {
            if (promptToSaveModifiedScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[OverlaySceneGenerator] Generation cancelled by user.");
                return null;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Canvas canvas = GeneratedSceneUIFactory.CreateCanvasSceneRoot(createEventSystem: false);
            GeneratedOverlayUIBuilder.BuildAll(canvas.transform);

            CompatSceneGenerationUtilities.ClearAllButtonListenersUnderCanvas();
            CompatSceneGenerationUtilities.SaveCurrentScene(ScenePath);
            Debug.Log("<color=green>[OverlaySceneGenerator] OverlayScene generated (direct builder).</color>");
            return ScenePath;
        }
    }
}
