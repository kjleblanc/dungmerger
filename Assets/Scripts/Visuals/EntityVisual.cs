using UnityEngine;

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

        protected virtual void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (uiImage == null) uiImage = GetComponent<UnityEngine.UI.Image>();
            ApplyOverride();
        }

        public void ApplyOverride()
        {
            if (animator != null && overrideController != null)
            {
                animator.runtimeAnimatorController = overrideController;
                animator.enabled = true;
                // Don't disable rendering components - the animator controls what sprites are shown
            }
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


