using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace YallaCatch
{
    /// <summary>
    /// Contrôle toutes les animations de capture de prizes
    /// Inclut: animation de la boîte, particules, popup de récompense, etc.
    /// </summary>
    public class CaptureAnimationController : MonoBehaviour
    {
        public static CaptureAnimationController Instance { get; private set; }

        [Header("Prize Box Animation")]
        [SerializeField] private GameObject prizeBoxPrefab;
        [SerializeField] private Transform boxSpawnPoint;
        [SerializeField] private float boxFlyDuration = 1.2f;
        [SerializeField] private AnimationCurve boxFlyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem captureParticles;
        [SerializeField] private ParticleSystem starBurstParticles;
        [SerializeField] private ParticleSystem confettiParticles;
        [SerializeField] private ParticleSystem glowParticles;
        
        [Header("Reward Popup")]
        [SerializeField] private GameObject rewardPopupPanel;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TextMeshProUGUI rewardNameText;
        [SerializeField] private TextMeshProUGUI rewardPointsText;
        [SerializeField] private TextMeshProUGUI rewardMessageText;
        [SerializeField] private Button rewardCloseButton;
        [SerializeField] private float popupDisplayDuration = 3f;
        
        [Header("Screen Effects")]
        [SerializeField] private Image screenFlashImage;
        [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private float flashDuration = 0.3f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip boxOpenSound;
        [SerializeField] private AudioClip rewardSound;
        [SerializeField] private AudioClip particlesSound;
        
        private AudioSource audioSource;
        private bool isAnimating = false;

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
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void Start()
        {
            // Cacher le popup au démarrage
            if (rewardPopupPanel != null)
            {
                rewardPopupPanel.SetActive(false);
            }
            
            // Cacher le flash screen
            if (screenFlashImage != null)
            {
                screenFlashImage.gameObject.SetActive(false);
            }
            
            // Setup close button
            if (rewardCloseButton != null)
            {
                rewardCloseButton.onClick.AddListener(CloseRewardPopup);
            }
        }

        /// <summary>
        /// Lance l'animation complète de capture
        /// </summary>
        public void PlayCaptureAnimation(Prize prize, int pointsEarned, System.Action onComplete = null)
        {
            if (isAnimating)
            {
                Debug.LogWarning("[CaptureAnimation] Animation already playing!");
                return;
            }
            
            StartCoroutine(CaptureAnimationSequence(prize, pointsEarned, onComplete));
        }

        /// <summary>
        /// Séquence complète d'animation de capture
        /// </summary>
        private IEnumerator CaptureAnimationSequence(Prize prize, int pointsEarned, System.Action onComplete)
        {
            isAnimating = true;
            
            // 1. Animation de la boîte qui vole vers le joueur
            yield return StartCoroutine(AnimatePrizeBox(prize));
            
            // 2. Flash screen
            StartCoroutine(ScreenFlash());
            
            // 3. Explosion de particules
            PlayParticleEffects();
            
            // 4. Son de récompense
            PlayRewardSound();
            
            // 5. Vibration
            Handheld.Vibrate();
            
            // 6. Attendre un peu
            yield return new WaitForSeconds(0.5f);
            
            // 7. Afficher le popup de récompense
            ShowRewardPopup(prize, pointsEarned);
            
            // 8. Attendre la fermeture du popup
            yield return new WaitForSeconds(popupDisplayDuration);
            
            isAnimating = false;
            
            // Callback
            onComplete?.Invoke();
        }

        /// <summary>
        /// Anime la boîte du prize qui vole vers le joueur
        /// </summary>
        private IEnumerator AnimatePrizeBox(Prize prize)
        {
            if (prizeBoxPrefab == null) yield break;
            
            // Créer la boîte
            GameObject box = Instantiate(prizeBoxPrefab, boxSpawnPoint.position, Quaternion.identity);
            
            // Charger l'icône du prize sur la boîte
            Image boxIcon = box.GetComponentInChildren<Image>();
            if (boxIcon != null && !string.IsNullOrEmpty(prize.icon))
            {
                // TODO: Charger l'icône depuis les resources ou API
                // boxIcon.sprite = Resources.Load<Sprite>(prize.icon);
            }
            
            // Position de départ et d'arrivée
            Vector3 startPos = boxSpawnPoint.position;
            Vector3 endPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            
            // Animation
            float elapsed = 0f;
            while (elapsed < boxFlyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / boxFlyDuration;
                float curveT = boxFlyCurve.Evaluate(t);
                
                // Position
                box.transform.position = Vector3.Lerp(startPos, endPos, curveT);
                
                // Rotation (spin)
                box.transform.Rotate(Vector3.up, Time.deltaTime * 360f);
                box.transform.Rotate(Vector3.right, Time.deltaTime * 180f);
                
                // Scale (grossit en approchant)
                float scale = Mathf.Lerp(0.5f, 1.5f, curveT);
                box.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            // Son d'ouverture de boîte
            if (boxOpenSound != null)
            {
                audioSource.PlayOneShot(boxOpenSound);
            }
            
            // Animation d'ouverture de la boîte
            yield return StartCoroutine(AnimateBoxOpening(box));
            
            // Détruire la boîte
            Destroy(box);
        }

        /// <summary>
        /// Anime l'ouverture de la boîte
        /// </summary>
        private IEnumerator AnimateBoxOpening(GameObject box)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            
            // Trouver le couvercle de la boîte (si existe)
            Transform lid = box.transform.Find("Lid");
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ouvrir le couvercle
                if (lid != null)
                {
                    lid.localRotation = Quaternion.Euler(-90f * t, 0, 0);
                    lid.localPosition = Vector3.up * t * 0.5f;
                }
                
                // Scale pulsation
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.1f;
                box.transform.localScale = Vector3.one * pulse * 1.5f;
                
                yield return null;
            }
        }

        /// <summary>
        /// Joue les effets de particules
        /// </summary>
        private void PlayParticleEffects()
        {
            Vector3 playerPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            
            // Particules de capture (cercle qui se resserre)
            if (captureParticles != null)
            {
                captureParticles.transform.position = playerPos;
                captureParticles.Play();
            }
            
            // Star burst (étoiles qui explosent)
            if (starBurstParticles != null)
            {
                starBurstParticles.transform.position = playerPos;
                starBurstParticles.Play();
            }
            
            // Confettis
            if (confettiParticles != null)
            {
                confettiParticles.transform.position = playerPos;
                confettiParticles.Play();
            }
            
            // Glow (lueur dorée)
            if (glowParticles != null)
            {
                glowParticles.transform.position = playerPos;
                glowParticles.Play();
            }
            
            // Son de particules
            if (particlesSound != null)
            {
                audioSource.PlayOneShot(particlesSound);
            }
        }

        /// <summary>
        /// Flash blanc sur tout l'écran
        /// </summary>
        private IEnumerator ScreenFlash()
        {
            if (screenFlashImage == null) yield break;
            
            screenFlashImage.gameObject.SetActive(true);
            screenFlashImage.color = flashColor;
            
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                
                Color color = flashColor;
                color.a = flashColor.a * (1f - t);
                screenFlashImage.color = color;
                
                yield return null;
            }
            
            screenFlashImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Affiche le popup de récompense
        /// </summary>
        private void ShowRewardPopup(Prize prize, int pointsEarned)
        {
            if (rewardPopupPanel == null) return;
            
            rewardPopupPanel.SetActive(true);
            
            // Icône du prize
            if (rewardIcon != null && !string.IsNullOrEmpty(prize.icon))
            {
                // TODO: Charger l'icône
                // rewardIcon.sprite = Resources.Load<Sprite>(prize.icon);
            }
            
            // Nom du prize
            if (rewardNameText != null)
            {
                rewardNameText.text = prize.name;
            }
            
            // Points gagnés
            if (rewardPointsText != null)
            {
                rewardPointsText.text = $"+{pointsEarned} Points!";
                
                // Animation de compteur
                StartCoroutine(AnimatePointsCounter(0, pointsEarned, 1f));
            }
            
            // Message
            if (rewardMessageText != null)
            {
                string message = GetRewardMessage(prize.rarity);
                rewardMessageText.text = message;
            }
            
            // Animation d'apparition du popup
            StartCoroutine(AnimatePopupAppearance());
        }

        /// <summary>
        /// Anime l'apparition du popup
        /// </summary>
        private IEnumerator AnimatePopupAppearance()
        {
            if (rewardPopupPanel == null) yield break;
            
            RectTransform popupRect = rewardPopupPanel.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = rewardPopupPanel.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = rewardPopupPanel.AddComponent<CanvasGroup>();
            }
            
            // Démarrer petit et transparent
            popupRect.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease out elastic
                float scale = ElasticEaseOut(t);
                popupRect.localScale = Vector3.one * scale;
                
                // Fade in
                canvasGroup.alpha = t;
                
                yield return null;
            }
            
            popupRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Anime le compteur de points
        /// </summary>
        private IEnumerator AnimatePointsCounter(int from, int to, float duration)
        {
            if (rewardPointsText == null) yield break;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                int currentPoints = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                rewardPointsText.text = $"+{currentPoints} Points!";
                
                yield return null;
            }
            
            rewardPointsText.text = $"+{to} Points!";
        }

        /// <summary>
        /// Ferme le popup de récompense
        /// </summary>
        public void CloseRewardPopup()
        {
            if (rewardPopupPanel != null)
            {
                StartCoroutine(AnimatePopupDisappearance());
            }
        }

        /// <summary>
        /// Anime la disparition du popup
        /// </summary>
        private IEnumerator AnimatePopupDisappearance()
        {
            if (rewardPopupPanel == null) yield break;
            
            RectTransform popupRect = rewardPopupPanel.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = rewardPopupPanel.GetComponent<CanvasGroup>();
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Scale down
                popupRect.localScale = Vector3.one * (1f - t);
                
                // Fade out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - t;
                }
                
                yield return null;
            }
            
            rewardPopupPanel.SetActive(false);
        }

        /// <summary>
        /// Joue le son de récompense
        /// </summary>
        private void PlayRewardSound()
        {
            if (rewardSound != null)
            {
                audioSource.PlayOneShot(rewardSound);
            }
        }

        /// <summary>
        /// Retourne un message selon la rareté du prize
        /// </summary>
        private string GetRewardMessage(string rarity)
        {
            switch (rarity?.ToLower())
            {
                case "common":
                    return "Nice catch!";
                case "uncommon":
                    return "Good find!";
                case "rare":
                    return "Rare prize! Well done!";
                case "epic":
                    return "Epic catch! Amazing!";
                case "legendary":
                    return "LEGENDARY! Incredible!";
                default:
                    return "Prize captured!";
            }
        }

        /// <summary>
        /// Fonction d'easing elastic
        /// </summary>
        private float ElasticEaseOut(float t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            
            float p = 0.3f;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
        }

        /// <summary>
        /// Animation rapide pour les captures multiples
        /// </summary>
        public void PlayQuickCaptureEffect(Vector3 position)
        {
            // Juste des particules et un son, pas de popup
            if (starBurstParticles != null)
            {
                starBurstParticles.transform.position = position;
                starBurstParticles.Play();
            }
            
            if (rewardSound != null)
            {
                audioSource.PlayOneShot(rewardSound, 0.5f);
            }
        }
    }
}
