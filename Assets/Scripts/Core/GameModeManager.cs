using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using YallaCatch.Core;

namespace YallaCatch
{
    /// <summary>
    /// Gère les deux modes de jeu:
    /// 1. Mode Caméra (préféré): Vue caméra live avec mini-map et AR overlay
    /// 2. Mode Map (alternatif): Vue map plein écran sans caméra
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public enum GameMode
        {
            Camera,  // Mode caméra live (préféré)
            Map      // Mode map plein écran (alternatif)
        }

        [Header("Mode Settings")]
        [SerializeField] private GameMode defaultMode = GameMode.Camera;
        [SerializeField] private GameMode currentMode;
        
        [Header("UI References")]
        [SerializeField] private GameObject cameraViewContainer;
        [SerializeField] private GameObject mapViewContainer;
        [SerializeField] private Button switchModeButton;
        [SerializeField] private Image switchModeIcon;
        [SerializeField] private Sprite cameraIcon;
        [SerializeField] private Sprite mapIcon;
        [SerializeField] private TMP_Text switchModeText;
        
        [Header("Managers")]
        [SerializeField] private CameraLiveManager cameraManager;
        [SerializeField] private MapController mapController;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private bool isTransitioning = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            SetupUI();
            
            // Démarrer avec le mode par défaut
            SetMode(defaultMode, false);
        }

        /// <summary>
        /// Configure l'UI du bouton de changement de mode
        /// </summary>
        private void SetupUI()
        {
            if (switchModeButton != null)
            {
                switchModeButton.onClick.AddListener(ToggleMode);
            }
            
            UpdateModeButtonUI();
        }

        /// <summary>
        /// Bascule entre les deux modes
        /// </summary>
        public void ToggleMode()
        {
            if (isTransitioning) return;
            
            GameMode newMode = currentMode == GameMode.Camera ? GameMode.Map : GameMode.Camera;
            SetMode(newMode, true);
        }

        /// <summary>
        /// Définit le mode de jeu
        /// </summary>
        public void SetMode(GameMode mode, bool animated = true)
        {
            if (currentMode == mode && !isTransitioning) return;
            
            GameMode previousMode = currentMode;
            currentMode = mode;
            
            Debug.Log($"[GameModeManager] Switching from {previousMode} to {currentMode}");
            
            if (animated)
            {
                StartCoroutine(TransitionToMode(previousMode, currentMode));
            }
            else
            {
                ApplyMode(currentMode);
            }
            
            UpdateModeButtonUI();
            
            // Sauvegarder la préférence
            PlayerPrefs.SetInt("PreferredGameMode", (int)currentMode);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Applique le mode immédiatement
        /// </summary>
        private void ApplyMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Camera:
                    ActivateCameraMode();
                    break;
                    
                case GameMode.Map:
                    ActivateMapMode();
                    break;
            }
        }

        /// <summary>
        /// Transition animée entre les modes
        /// </summary>
        private IEnumerator TransitionToMode(GameMode from, GameMode to)
        {
            isTransitioning = true;
            
            // Désactiver les interactions pendant la transition
            if (switchModeButton != null)
            {
                switchModeButton.interactable = false;
            }
            
            // Fade out du mode actuel
            yield return StartCoroutine(FadeOutMode(from));
            
            // Changer de mode
            ApplyMode(to);
            
            // Fade in du nouveau mode
            yield return StartCoroutine(FadeInMode(to));
            
            // Réactiver les interactions
            if (switchModeButton != null)
            {
                switchModeButton.interactable = true;
            }
            
            isTransitioning = false;
        }

        /// <summary>
        /// Fade out d'un mode
        /// </summary>
        private IEnumerator FadeOutMode(GameMode mode)
        {
            GameObject container = mode == GameMode.Camera ? cameraViewContainer : mapViewContainer;
            if (container == null) yield break;
            
            CanvasGroup canvasGroup = container.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = container.AddComponent<CanvasGroup>();
            }
            
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                canvasGroup.alpha = 1f - transitionCurve.Evaluate(t);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            container.SetActive(false);
        }

        /// <summary>
        /// Fade in d'un mode
        /// </summary>
        private IEnumerator FadeInMode(GameMode mode)
        {
            GameObject container = mode == GameMode.Camera ? cameraViewContainer : mapViewContainer;
            if (container == null) yield break;
            
            container.SetActive(true);
            
            CanvasGroup canvasGroup = container.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = container.AddComponent<CanvasGroup>();
            }
            
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                canvasGroup.alpha = transitionCurve.Evaluate(t);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Active le mode caméra
        /// </summary>
        private void ActivateCameraMode()
        {
            // Activer la vue caméra
            if (cameraViewContainer != null)
            {
                cameraViewContainer.SetActive(true);
            }
            
            // Désactiver la vue map
            if (mapViewContainer != null)
            {
                mapViewContainer.SetActive(false);
            }
            
            // Activer le CameraLiveManager
            if (cameraManager != null)
            {
                cameraManager.ActivateCameraMode();
            }
            
            // Désactiver le MapController
            if (mapController != null)
            {
                mapController.DeactivateFullScreenMode();
            }
            
            Debug.Log("[GameModeManager] Camera mode activated");
        }

        /// <summary>
        /// Active le mode map
        /// </summary>
        private void ActivateMapMode()
        {
            // Désactiver la vue caméra
            if (cameraViewContainer != null)
            {
                cameraViewContainer.SetActive(false);
            }
            
            // Activer la vue map
            if (mapViewContainer != null)
            {
                mapViewContainer.SetActive(true);
            }
            
            // Désactiver le CameraLiveManager
            if (cameraManager != null)
            {
                cameraManager.DeactivateCameraMode();
            }
            
            // Activer le MapController en mode plein écran
            if (mapController != null)
            {
                mapController.ActivateFullScreenMode();
            }
            
            Debug.Log("[GameModeManager] Map mode activated");
        }

        /// <summary>
        /// Met à jour l'UI du bouton de changement de mode
        /// </summary>
        private void UpdateModeButtonUI()
        {
            if (switchModeIcon != null)
            {
                // Afficher l'icône du mode vers lequel on peut basculer
                switchModeIcon.sprite = currentMode == GameMode.Camera ? mapIcon : cameraIcon;
            }
            
            if (switchModeText != null)
            {
                // Texte du mode vers lequel on peut basculer
                switchModeText.text = currentMode == GameMode.Camera ? "Map View" : "Camera View";
            }
        }

        /// <summary>
        /// Retourne le mode actuel
        /// </summary>
        public GameMode GetCurrentMode()
        {
            return currentMode;
        }

        /// <summary>
        /// Vérifie si on est en mode caméra
        /// </summary>
        public bool IsCameraMode()
        {
            return currentMode == GameMode.Camera;
        }

        /// <summary>
        /// Vérifie si on est en mode map
        /// </summary>
        public bool IsMapMode()
        {
            return currentMode == GameMode.Map;
        }

        /// <summary>
        /// Force le mode caméra (utilisé au démarrage ou après tutoriel)
        /// </summary>
        public void ForceCameraMode()
        {
            SetMode(GameMode.Camera, false);
        }

        /// <summary>
        /// Force le mode map (utilisé pour certaines fonctionnalités)
        /// </summary>
        public void ForceMapMode()
        {
            SetMode(GameMode.Map, false);
        }

        /// <summary>
        /// Charge la préférence de mode sauvegardée
        /// </summary>
        public void LoadPreferredMode()
        {
            if (PlayerPrefs.HasKey("PreferredGameMode"))
            {
                int savedMode = PlayerPrefs.GetInt("PreferredGameMode");
                defaultMode = (GameMode)savedMode;
                SetMode(defaultMode, false);
            }
        }
    }
}
