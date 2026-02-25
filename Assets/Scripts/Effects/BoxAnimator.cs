using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using YallaCatch.Models;

namespace YallaCatch.Effects
{
    public enum BoxAnimationState
    {
        Idle,
        Dropping,
        Landed,
        Opening,
        Opened
    }

    /// <summary>
    /// Handles the 3D high-end animation sequence for loot boxes/mystery boxes,
    /// synchronized with the backend's PrizeDisplayType.
    /// Attach this to the 3D Box Prefab.
    /// </summary>
    public class BoxAnimator : MonoBehaviour
    {
        [Header("Animation References")]
        public Animator boxAnimatorComponent; // Requires Unity Animator with "Drop", "Open" triggers
        public ParticleSystem dustParticles;
        public ParticleSystem rarityBurstParticles;
        public ParticleSystem godRays; // For golden_box
        
        [Header("Juice")]
        public Transform lidTransform;
        public Vector3 shakeAmount = new Vector3(0.1f, 0.1f, 0.1f);
        public float shakeDuration = 0.5f;

        [Header("Events")]
        public UnityEvent onBoxLanded;
        public UnityEvent onBoxOpened;

        private BoxAnimationState currentState = BoxAnimationState.Idle;
        private Prize currentPrize;
        private Color rarityColor;

        public void Initialize(Prize prize, Color color)
        {
            currentPrize = prize;
            rarityColor = color;
            
            // Apply rarity color to particle systems
            if (rarityBurstParticles != null)
            {
                var main = rarityBurstParticles.main;
                main.startColor = rarityColor;
            }

            // Apply special styling based on PrizeDisplayType
            ApplyDisplayTypeStyling(prize.displayType);
        }

        private void ApplyDisplayTypeStyling(string displayType)
        {
            switch (displayType?.ToLower())
            {
                case "golden_box":
                    if (godRays != null) godRays.gameObject.SetActive(true);
                    // Could also swap main material to gold here
                    break;
                case "mystery_box":
                    // Setup neon/glitch styling
                    if (godRays != null) godRays.gameObject.SetActive(false);
                    break;
                default:
                    // standard
                    if (godRays != null) godRays.gameObject.SetActive(false);
                    break;
            }
        }

        public void PlayDropSequence()
        {
            if (currentState != BoxAnimationState.Idle) return;
            currentState = BoxAnimationState.Dropping;
            StartCoroutine(DropRoutine());
        }

        private IEnumerator DropRoutine()
        {
            // Trigger drop animation (e.g. falling from sky)
            if (boxAnimatorComponent != null) boxAnimatorComponent.SetTrigger("Drop");
            else yield return new WaitForSeconds(0.5f); // Fallback if no Animator attached

            // Wait for landing
            yield return new WaitForSeconds(0.8f); // Adjust timing based on actual animation clip

            // Landing Impact
            currentState = BoxAnimationState.Landed;
            if (dustParticles != null) dustParticles.Play();
            
            // Screen Shake equivalent on the box itself or trigger main camera shake
            StartCoroutine(ShakeRoutine(shakeDuration, shakeAmount));

            onBoxLanded?.Invoke();
        }

        public void OpenBox()
        {
            if (currentState != BoxAnimationState.Landed) return;
            currentState = BoxAnimationState.Opening;
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            // Pre-open rumble
            StartCoroutine(ShakeRoutine(0.4f, shakeAmount * 1.5f));
            yield return new WaitForSeconds(0.4f);

            // Pop Lid
            if (boxAnimatorComponent != null) boxAnimatorComponent.SetTrigger("Open");
            
            // Burst
            if (rarityBurstParticles != null) rarityBurstParticles.Play();

            // Wait for reveal
            yield return new WaitForSeconds(1.0f);
            
            currentState = BoxAnimationState.Opened;
            onBoxOpened?.Invoke();
        }

        private IEnumerator ShakeRoutine(float duration, Vector3 magnitude)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude.x;
                float y = Random.Range(-1f, 1f) * magnitude.y;
                float z = Random.Range(-1f, 1f) * magnitude.z;

                transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z + z);
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
        }
    }
}
