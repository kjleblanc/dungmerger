using UnityEngine;
using System.Collections;

namespace MergeDungeon.Core
{
    public class EntityVisual : MonoBehaviour
    {
        [Header("Animator")]
        public Animator animator;
        [Tooltip("Optional overrides for this entity kind.")]
        public AnimatorOverrideController overrideController;

        [Header("Static Sprite Fallback")]
        public SpriteRenderer spriteRenderer;
        public UnityEngine.UI.Image uiImage;

        [Header("State Names / Triggers")]
        public string idleState = "Idle";
        public string useTrigger = "Use";
        public string hitTrigger = "Hit";
        public string deathTrigger = "Death";

        private Coroutine _applyOverrideRoutine;

        protected virtual void OnEnable()
        {
            if (overrideController == null) return;
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) return;

            if (CanApplyAnimatorOverride())
            {
                ApplyAnimatorOverride();
            }
            else
            {
                ScheduleApplyOverride();
            }
        }

        protected virtual void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (uiImage == null) uiImage = GetComponent<UnityEngine.UI.Image>();
            ApplyOverride();
        }

        public void ApplyOverride()
        {
            if (overrideController == null) return;
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) return;

            if (!CanApplyAnimatorOverride())
            {
                ScheduleApplyOverride();
                return;
            }

            ApplyAnimatorOverride();
        }

        private bool CanApplyAnimatorOverride()
        {
            if (animator == null) return false;
            if (!animator.gameObject.activeInHierarchy) return false;
            if (!animator.isActiveAndEnabled) return false;
            if (!animator.isInitialized) return false;
            return true;
        }

        private void ApplyAnimatorOverride()
        {
            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;
        }

        private void ScheduleApplyOverride()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_applyOverrideRoutine != null) return;
            _applyOverrideRoutine = StartCoroutine(ApplyOverrideWhenReady());
        }

        private IEnumerator ApplyOverrideWhenReady()
        {
            while (animator != null && overrideController != null)
            {
                if (CanApplyAnimatorOverride())
                {
                    ApplyAnimatorOverride();
                    break;
                }
                yield return null;
            }
            _applyOverrideRoutine = null;
        }

        public void SetStaticSprite(Sprite sprite)
        {
            if (sprite == null) return;
            
            // Disable animator when using static sprite
            if (animator != null) animator.enabled = false;
            
            // Set the sprite on the appropriate component and ensure it's enabled
            if (uiImage != null)
            {
                uiImage.sprite = sprite;
                uiImage.enabled = true;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.enabled = true;
            }
        }

        public virtual void PlayIdle()
        {
            if (animator == null || !animator.enabled) return;
            if (!string.IsNullOrEmpty(idleState))
            {
                animator.Play(idleState, 0, 0f);
            }
        }

        public virtual void PlayUse()
        {
            if (animator == null || !animator.enabled) return;
            if (!string.IsNullOrEmpty(useTrigger)) animator.SetTrigger(useTrigger);
        }

        public virtual void PlayHit()
        {
            if (animator == null || !animator.enabled) return;
            if (!string.IsNullOrEmpty(hitTrigger)) animator.SetTrigger(hitTrigger);
        }

        public virtual void PlayDeath()
        {
            if (animator == null || !animator.enabled) return;
            if (!string.IsNullOrEmpty(deathTrigger)) animator.SetTrigger(deathTrigger);
        }
    }
}


