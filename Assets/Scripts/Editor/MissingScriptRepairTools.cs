using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YallaCatch.Editor
{
    internal static class MissingScriptRepairTools
    {
        [MenuItem("YallaCatch / Scan Missing Scripts / Active Scene")]
        private static void ScanActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Scan Blocked", "Exit Play Mode before scanning missing scripts.", "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[MissingScriptRepairTools] No active loaded scene to scan.");
                return;
            }

            int total = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                ScanRecursive(root.transform, GetGameObjectPath(root.transform), ref total);
            }

            Debug.Log($"[MissingScriptRepairTools] Scan complete for '{scene.name}'. Missing script components found: {total}");
            EditorUtility.DisplayDialog("Missing Script Scan", $"Scene: {scene.name}\nMissing script components: {total}\nCheck Console for details.", "OK");
        }

        [MenuItem("YallaCatch / Repair Missing Scripts / Active Scene")]
        private static void RepairActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Repair Blocked", "Exit Play Mode before removing missing scripts.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[MissingScriptRepairTools] No active loaded scene to repair.");
                return;
            }

            int removed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                removed += RemoveRecursive(root.transform);
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[MissingScriptRepairTools] Repair complete for '{scene.name}'. Removed missing script components: {removed}");
            EditorUtility.DisplayDialog("Missing Script Repair", $"Scene: {scene.name}\nRemoved missing script components: {removed}", "OK");
        }

        [MenuItem("YallaCatch / Repair Missing Scripts / Generated Scenes")]
        private static void RepairGeneratedScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Repair Blocked", "Exit Play Mode before removing missing scripts.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string originalScenePath = SceneManager.GetActiveScene().path;
            int totalRemoved = 0;

            IReadOnlyList<SceneGenerationOrchestrator.GeneratedSceneSpec> manifest = SceneGenerationOrchestrator.BuildDefaultManifest();
            foreach (var spec in manifest)
            {
                string scenePath = $"Assets/Scenes/{spec.SceneName}.unity";
                if (!spec.EnabledInBuild && !System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
                {
                    continue;
                }

                if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int removed = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    removed += RemoveRecursive(root.transform);
                }

                if (removed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                totalRemoved += removed;
                Debug.Log($"[MissingScriptRepairTools] {spec.SceneName}: removed {removed} missing script component(s).");
            }

            if (!string.IsNullOrEmpty(originalScenePath) && System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", originalScenePath)))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[MissingScriptRepairTools] Generated-scene repair complete. Total removed: {totalRemoved}");
            EditorUtility.DisplayDialog("Generated Scene Repair", $"Removed missing script components across generated scenes: {totalRemoved}", "OK");
        }

        private static void ScanRecursive(Transform node, string path, ref int total)
        {
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
            if (missingCount > 0)
            {
                total += missingCount;
                Debug.LogWarning($"[MissingScriptRepairTools] Missing scripts on '{path}': {missingCount}", node.gameObject);
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                ScanRecursive(child, $"{path}/{child.name}", ref total);
            }
        }

        private static int RemoveRecursive(Transform node)
        {
            int removed = 0;
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);

            for (int i = 0; i < node.childCount; i++)
            {
                removed += RemoveRecursive(node.GetChild(i));
            }

            return removed;
        }

        private static string GetGameObjectPath(Transform node)
        {
            var names = new Stack<string>();
            Transform current = node;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }
    }
}
