using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YallaCatch.Core
{
    /// <summary>
    /// Transitional boot loader for the generated multi-scene architecture.
    /// Phase 2/3 bootstrap: loads MetaScene from BootScene.
    /// </summary>
    public class AppSceneBootstrap : MonoBehaviour
    {
        [Header("Boot Routing")]
        [SerializeField] private string initialSceneName = "MetaScene";
        [SerializeField] private bool loadAdditive = false;
        [SerializeField] private bool autoLoadOnStart = true;

        private bool hasStartedLoad;

        private void Start()
        {
            if (!autoLoadOnStart || hasStartedLoad)
                return;

            StartCoroutine(LoadInitialScene());
        }

        public void LoadNow()
        {
            if (hasStartedLoad)
                return;

            StartCoroutine(LoadInitialScene());
        }

        private IEnumerator LoadInitialScene()
        {
            hasStartedLoad = true;

            if (string.IsNullOrWhiteSpace(initialSceneName))
            {
                Debug.LogError("[AppSceneBootstrap] Initial scene name is empty.");
                yield break;
            }

            Scene existingScene = SceneManager.GetSceneByName(initialSceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                Debug.Log($"[AppSceneBootstrap] Scene '{initialSceneName}' already loaded.");
                yield break;
            }

            LoadSceneMode mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            AsyncOperation op = SceneManager.LoadSceneAsync(initialSceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[AppSceneBootstrap] Failed to start loading scene '{initialSceneName}'.");
                yield break;
            }

            while (!op.isDone)
                yield return null;

            Debug.Log($"[AppSceneBootstrap] Loaded scene '{initialSceneName}' ({mode}).");
        }
    }
}
