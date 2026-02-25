using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YallaCatch;
using YallaCatch.Core;
using YallaCatch.Managers;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Transitional utilities for generating scene-specific outputs by pruning the hardened FullSceneGenerator result.
    /// This keeps feature/UI parity high while the generators are being split into dedicated builders.
    /// </summary>
    internal static class CompatSceneGenerationUtilities
    {
        private static readonly string[] GameplayAndOverlayCanvasRoots =
        {
            "GamePanel",
            "CaptureDialog",
            "MessageDialog",
            "ConfirmDialog",
            "LoadingOverlay",
            "ReportPanel",
            "QRCodePanel",
            "CaptureRewardPopupPanel",
            "CaptureScreenFlash"
        };

        private static readonly string[] OverlayOnlyCanvasRoots =
        {
            "CaptureDialog",
            "MessageDialog",
            "ConfirmDialog",
            "LoadingOverlay",
            "ReportPanel",
            "QRCodePanel",
            "CaptureRewardPopupPanel",
            "CaptureScreenFlash"
        };

        internal static void PruneCanvasToGameplayAndOverlay()
        {
            DestroyCanvasChildrenExcept(GameplayAndOverlayCanvasRoots);
        }

        internal static void PruneCanvasToOverlayOnly()
        {
            DestroyCanvasChildrenExcept(OverlayOnlyCanvasRoots);
        }

        internal static void DestroyCanvasChildrenExcept(IEnumerable<string> namesToKeep)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[CompatSceneGenerationUtilities] Canvas not found while pruning.");
                return;
            }

            var keep = new HashSet<string>(namesToKeep);
            Transform canvasT = canvas.transform;
            for (int i = canvasT.childCount - 1; i >= 0; i--)
            {
                Transform child = canvasT.GetChild(i);
                if (!keep.Contains(child.name))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        internal static void DestroyRootIfExists(string rootName)
        {
            GameObject obj = GameObject.Find(rootName);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        internal static void RemoveManagerComponent<T>() where T : Component
        {
            GameObject managers = GameObject.Find("Managers");
            if (managers == null) return;

            T component = managers.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component, true);
            }
        }

        internal static void SetGamePanelActive(bool active)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform gamePanel = canvas.transform.Find("GamePanel");
            if (gamePanel != null)
            {
                gamePanel.gameObject.SetActive(active);
            }
        }

        internal static void SetGameModeDefault(GameModeManager.GameMode mode)
        {
            GameModeManager gameModeManager = Object.FindObjectOfType<GameModeManager>();
            if (gameModeManager == null) return;

            SerializedObject so = new SerializedObject(gameModeManager);
            SerializedProperty defaultMode = so.FindProperty("defaultMode");
            SerializedProperty currentMode = so.FindProperty("currentMode");

            if (defaultMode != null) defaultMode.enumValueIndex = (int)mode;
            if (currentMode != null) currentMode.enumValueIndex = (int)mode;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetGameModeContainers(bool cameraActive, bool mapActive)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform gamePanel = canvas.transform.Find("GamePanel");
            if (gamePanel == null) return;

            Transform camera = gamePanel.Find("CameraViewContainer");
            Transform map = gamePanel.Find("MapViewContainer");
            if (camera != null) camera.gameObject.SetActive(cameraActive);
            if (map != null) map.gameObject.SetActive(mapActive);
        }

        internal static void ClearAllButtonListenersUnderCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.onClick = new Button.ButtonClickedEvent();
            }
        }

        internal static SceneNavigationService EnsureSceneNavigationService()
        {
            GameObject root = GameObject.Find("SceneNavigation") ?? new GameObject("SceneNavigation");
            SceneNavigationService service = root.GetComponent<SceneNavigationService>();
            if (service == null)
            {
                service = root.AddComponent<SceneNavigationService>();
            }

            SerializedObject so = new SerializedObject(service);
            SetString(so, "metaSceneName", "MetaScene");
            SetString(so, "gameplayMapSceneName", "GameplayMapScene");
            SetString(so, "gameplayARSceneName", "GameplayARScene");
            SetString(so, "overlaySceneName", "OverlayScene");
            SetBool(so, "persistAcrossScenes", true);
            so.ApplyModifiedPropertiesWithoutUndo();

            return service;
        }

        internal static void ConfigureSceneNavigationService(
            SceneNavigationService service,
            bool? loadOverlayForGameplay = null,
            bool? loadOverlayForMeta = null,
            SceneNavigationService.GameplayTarget? defaultGameplayTarget = null)
        {
            if (service == null) return;

            SerializedObject so = new SerializedObject(service);
            if (loadOverlayForGameplay.HasValue)
            {
                SetBool(so, "loadOverlayForGameplay", loadOverlayForGameplay.Value);
            }

            if (loadOverlayForMeta.HasValue)
            {
                SetBool(so, "loadOverlayForMeta", loadOverlayForMeta.Value);
            }

            if (defaultGameplayTarget.HasValue)
            {
                SerializedProperty prop = so.FindProperty("defaultGameplayTarget");
                if (prop != null) prop.enumValueIndex = (int)defaultGameplayTarget.Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void WireButtonToAction(Transform root, string path, UnityEngine.Events.UnityAction action)
        {
            if (root == null || action == null) return;

            Transform t = root.Find(path);
            if (t == null) return;

            Button button = t.GetComponent<Button>();
            if (button == null) return;

            ResetButtonListeners(button);
            UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        }

        internal static void ResetButtonListeners(Button button)
        {
            if (button == null) return;

            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            button.onClick.RemoveAllListeners();
        }

        internal static void SetInactiveIfExists(string canvasChildName)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform t = canvas.transform.Find(canvasChildName);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        internal static void SaveCurrentScene(string scenePath)
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, "../", scenePath);
            string directory = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.String)
            {
                prop.stringValue = value;
            }
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Boolean)
            {
                prop.boolValue = value;
            }
        }
    }
}
