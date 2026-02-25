using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace YallaCatch.UI
{
    public enum UIAnimationType
    {
        FadeOnly,
        ScaleBounce,
        SlideUp,
        SlideRight,
        DropIn
    }

    /// <summary>
    /// A dependency-free UI animation system for "Premium" panel reveals.
    /// Attach to any panel you want to animate in/out.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public UIAnimationType animationType = UIAnimationType.ScaleBounce;
        public float duration = 0.4f;
        public bool playOnEnable = false;

        [Header("Offsets (For Slide)")]
        public float slideDistance = 300f;

        [Header("Events")]
        public UnityEvent onShowComplete;
        public UnityEvent onHideComplete;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        
        // Original states
        private Vector3 originalScale;
        private Vector2 originalPosition;

        private Coroutine activeAnimation;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            
            originalScale = rectTransform.localScale;
            originalPosition = rectTransform.anchoredPosition;

            // Ensure we start hidden if we aren't meant to be visible yet
            if (!playOnEnable && canvasGroup.alpha == 1f)
            {
                // Optionally hide immediately depending on architecture.
                // Normally UIManager handles initial state.
            }
        }

        private void OnEnable()
        {
            if (playOnEnable) Show();
        }

        public void Show(System.Action callback = null)
        {
            gameObject.SetActive(true);
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(AnimateIn(callback));
        }

        public void Hide(System.Action callback = null, bool disableOnComplete = true)
        {
            if (!gameObject.activeInHierarchy) return;
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(AnimateOut(() => {
                if (disableOnComplete) gameObject.SetActive(false);
                callback?.Invoke();
            }));
        }

        private IEnumerator AnimateIn(System.Action callback)
        {
            float time = 0;
            canvasGroup.alpha = 0f;
            
            // Setup initial state based on type
            switch (animationType)
            {
                case UIAnimationType.ScaleBounce:
                    rectTransform.localScale = originalScale * 0.5f;
                    break;
                case UIAnimationType.DropIn:
                    rectTransform.localScale = originalScale * 1.5f;
                    break;
                case UIAnimationType.SlideUp:
                    rectTransform.anchoredPosition = originalPosition + new Vector2(0, -slideDistance);
                    break;
                case UIAnimationType.SlideRight:
                    rectTransform.anchoredPosition = originalPosition + new Vector2(-slideDistance, 0);
                    break;
            }

            // Animation Loop
            while (time < duration)
            {
                time += Time.unscaledDeltaTime; // Unscaled so it works even if Time.timeScale is 0
                float t = Mathf.Clamp01(time / duration);

                // Fade is always applied
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                // Transform changes based on type
                switch (animationType)
                {
                    case UIAnimationType.ScaleBounce:
                        // Custom EaseOutBack calculation for the bounce
                        float easeOutBack = EaseOutBack(t);
                        rectTransform.localScale = Vector3.LerpUnclamped(originalScale * 0.5f, originalScale, easeOutBack);
                        break;
                    case UIAnimationType.DropIn:
                        float easeOutCubicDrop = EaseOutCubic(t);
                        rectTransform.localScale = Vector3.LerpUnclamped(originalScale * 1.5f, originalScale, easeOutCubicDrop);
                        break;
                    case UIAnimationType.SlideUp:
                        float easeOutCubicUp = EaseOutCubic(t);
                        rectTransform.anchoredPosition = Vector2.Lerp(originalPosition + new Vector2(0, -slideDistance), originalPosition, easeOutCubicUp);
                        break;
                    case UIAnimationType.SlideRight:
                        float easeOutCubicRight = EaseOutCubic(t);
                        rectTransform.anchoredPosition = Vector2.Lerp(originalPosition + new Vector2(-slideDistance, 0), originalPosition, easeOutCubicRight);
                        break;
                }

                yield return null;
            }

            // Ensure exact final state
            canvasGroup.alpha = 1f;
            rectTransform.localScale = originalScale;
            rectTransform.anchoredPosition = originalPosition;

            onShowComplete?.Invoke();
            callback?.Invoke();
            activeAnimation = null;
        }

        private IEnumerator AnimateOut(System.Action callback)
        {
            float time = 0;
            // Shorten hide duration to make it snappier
            float hideDuration = duration * 0.7f; 

            Vector3 startScale = rectTransform.localScale;
            Vector2 startPos = rectTransform.anchoredPosition;

            while (time < hideDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / hideDuration);
                float easeInCubic = EaseInCubic(t);

                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                switch (animationType)
                {
                    case UIAnimationType.ScaleBounce:
                    case UIAnimationType.DropIn:
                        rectTransform.localScale = Vector3.Lerp(startScale, originalScale * 0.8f, easeInCubic);
                        break;
                    case UIAnimationType.SlideUp:
                        rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition + new Vector2(0, -slideDistance), easeInCubic);
                        break;
                    case UIAnimationType.SlideRight:
                        rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition + new Vector2(slideDistance, 0), easeInCubic);
                        break;
                }

                yield return null;
            }

            canvasGroup.alpha = 0f;
            onHideComplete?.Invoke();
            callback?.Invoke();
            activeAnimation = null;
        }

        // --- Easing Math ---
        private float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }

        private float EaseInCubic(float x)
        {
            return x * x * x;
        }
    }
}
