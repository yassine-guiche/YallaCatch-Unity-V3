using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YallaCatch.Core
{
    /// <summary>
    /// Transitional runtime scene router for the generated multi-scene architecture.
    /// Keeps scene transitions centralized while generators are being split by feature role.
    /// </summary>
    public class SceneNavigationService : MonoBehaviour
    {
        public static SceneNavigationService Instance { get; private set; }

        public enum GameplayTarget
        {
            AR,
            Map
        }

        [Header("Scene Names")]
        [SerializeField] private string metaSceneName = "MetaScene";
        [SerializeField] private string gameplayMapSceneName = "GameplayMapScene";
        [SerializeField] private string gameplayARSceneName = "GameplayARScene";
        [SerializeField] private string overlaySceneName = "OverlayScene";

        [Header("Routing")]
        [SerializeField] private GameplayTarget defaultGameplayTarget = GameplayTarget.AR;
        [SerializeField] private bool loadOverlayForGameplay = false;
        [SerializeField] private bool loadOverlayForMeta = false;
        [SerializeField] private bool persistAcrossScenes = true;

        private bool isTransitioning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                // Replace the previous router with the scene-local router so serialized button listeners remain valid
                // after scene transitions (MetaScene -> Gameplay scenes -> MetaScene).
                if (Instance != null)
                {
                    Destroy(Instance.gameObject);
                }

                Instance = this;
            }

            if (Instance == this && persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public void LoadPreferredGameplay()
        {
            if (defaultGameplayTarget == GameplayTarget.Map)
            {
                LoadGameplayMap();
            }
            else
            {
                LoadGameplayAR();
            }
        }

        public void LoadGameplayMap()
        {
            StartSceneTransition(gameplayMapSceneName, includeOverlay: loadOverlayForGameplay);
        }

        public void LoadGameplayAR()
        {
            StartSceneTransition(gameplayARSceneName, includeOverlay: loadOverlayForGameplay);
        }

        public void LoadMetaScene()
        {
            StartSceneTransition(metaSceneName, includeOverlay: loadOverlayForMeta);
        }

        public void LoadOverlayOnly()
        {
            if (isTransitioning) return;
            if (string.IsNullOrWhiteSpace(overlaySceneName))
            {
                Debug.LogWarning("[SceneNavigationService] Overlay scene name is empty.");
                return;
            }

            StartCoroutine(LoadOverlayIfNeeded());
        }

        public void UnloadOverlay()
        {
            if (isTransitioning) return;
            if (string.IsNullOrWhiteSpace(overlaySceneName)) return;

            Scene overlayScene = SceneManager.GetSceneByName(overlaySceneName);
            if (!overlayScene.IsValid() || !overlayScene.isLoaded) return;

            StartCoroutine(UnloadSceneAsync(overlaySceneName));
        }

        private void StartSceneTransition(string primarySceneName, bool includeOverlay)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[SceneNavigationService] Transition already in progress.");
                return;
            }

            if (string.IsNullOrWhiteSpace(primarySceneName))
            {
                Debug.LogError("[SceneNavigationService] Primary scene name is empty.");
                return;
            }

            StartCoroutine(LoadSceneFlow(primarySceneName, includeOverlay));
        }

        private IEnumerator LoadSceneFlow(string primarySceneName, bool includeOverlay)
        {
            isTransitioning = true;

            AsyncOperation loadPrimary = SceneManager.LoadSceneAsync(primarySceneName, LoadSceneMode.Single);
            if (loadPrimary == null)
            {
                Debug.LogError($"[SceneNavigationService] Failed to start loading scene '{primarySceneName}'.");
                isTransitioning = false;
                yield break;
            }

            while (!loadPrimary.isDone)
            {
                yield return null;
            }

            if (includeOverlay)
            {
                yield return LoadOverlayIfNeeded();
            }
            else
            {
                yield return UnloadOverlayIfLoaded();
            }

            Debug.Log($"[SceneNavigationService] Loaded primary scene '{primarySceneName}' (overlay={(includeOverlay ? "on" : "off")}).");
            isTransitioning = false;
        }

        private IEnumerator LoadOverlayIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(overlaySceneName))
                yield break;

            Scene overlayScene = SceneManager.GetSceneByName(overlaySceneName);
            if (overlayScene.IsValid() && overlayScene.isLoaded)
                yield break;

            AsyncOperation loadOverlay = SceneManager.LoadSceneAsync(overlaySceneName, LoadSceneMode.Additive);
            if (loadOverlay == null)
            {
                Debug.LogWarning($"[SceneNavigationService] Failed to start additive load for overlay scene '{overlaySceneName}'.");
                yield break;
            }

            while (!loadOverlay.isDone)
            {
                yield return null;
            }
        }

        private IEnumerator UnloadOverlayIfLoaded()
        {
            if (string.IsNullOrWhiteSpace(overlaySceneName))
                yield break;

            Scene overlayScene = SceneManager.GetSceneByName(overlaySceneName);
            if (!overlayScene.IsValid() || !overlayScene.isLoaded)
                yield break;

            yield return UnloadSceneAsync(overlaySceneName);
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(sceneName);
            if (unload == null) yield break;

            while (!unload.isDone)
            {
                yield return null;
            }
        }
    }
}
