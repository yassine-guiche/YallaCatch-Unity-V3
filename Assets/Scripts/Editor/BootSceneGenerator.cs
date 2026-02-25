using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YallaCatch.Core;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Phase 2/3 bootstrap generator: creates a minimal BootScene that routes into MetaScene.
    /// </summary>
    internal static class BootSceneGenerator
    {
        internal const string ScenePath = "Assets/Scenes/BootScene.unity";

        internal static string GenerateBootScene(bool promptToSaveModifiedScenes = true)
        {
            if (promptToSaveModifiedScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[BootSceneGenerator] Generation cancelled by user.");
                return null;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera mainCamera = Object.FindObjectOfType<Camera>();
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.black;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }

            GameObject bootRoot = GameObject.Find("Boot") ?? new GameObject("Boot");
            AppSceneBootstrap bootstrap = bootRoot.GetComponent<AppSceneBootstrap>();
            if (bootstrap == null) bootstrap = bootRoot.AddComponent<AppSceneBootstrap>();

            SerializedObject so = new SerializedObject(bootstrap);
            SetValue(so, "initialSceneName", "MetaScene");
            SetValue(so, "loadAdditive", false);
            SetValue(so, "autoLoadOnStart", true);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveCurrentScene(ScenePath);
            Debug.Log("<color=green>[BootSceneGenerator] BootScene generated.</color>");
            return ScenePath;
        }

        private static void SaveCurrentScene(string scenePath)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
                System.IO.Path.Combine(Application.dataPath, "../", scenePath)));

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        }

        private static void SetValue<T>(SerializedObject so, string propertyName, T value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null) return;

            if (typeof(T) == typeof(string))
                prop.stringValue = (string)(object)value;
            else if (typeof(T) == typeof(bool))
                prop.boolValue = (bool)(object)value;
        }
    }
}
