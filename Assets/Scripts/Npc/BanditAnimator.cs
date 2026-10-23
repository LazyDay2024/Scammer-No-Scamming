using UnityEngine;

namespace ScammerNoScamming
{
    // Drives Nithis's realistic bandit (Resources/Scammer/ScammerModel: ScammerAnimator controller with Speed,
    // Shooting, Dead, Mounted and an upper-body aim layer) from our NPC AI through INpcAnimation.
    public class BanditAnimator : MonoBehaviour, INpcAnimation
    {
        public Animator animator;
        // Above this ground speed the bandit runs/walks instead of holding the firing stance.
        public float standingSpeed = .5f;
        public bool Mounted;
        float speed;
        bool aiming, dead;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }

        void Update()
        {
            if (animator == null || dead) return;
            animator.SetFloat("Speed", speed);
            animator.SetBool("Shooting", aiming && (Mounted || speed < standingSpeed));
            animator.SetBool("Mounted", Mounted);
            if (animator.layerCount > 1) animator.SetLayerWeight(1, Mounted && aiming ? 1f : 0f);
        }

        public void ResetForAction()
        {
            dead = aiming = false;
            speed = 0f;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (animator.isActiveAndEnabled)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            animator.SetBool("Dead", false);
            animator.SetBool("Mounted", Mounted);
            animator.SetBool("Shooting", false);
            if (animator.layerCount > 1) animator.SetLayerWeight(1, 0f);
        }

        public void SetSpeed(float metersPerSecond) => speed = Mathf.Max(0f, metersPerSecond);
        public void Aim(bool aim) => aiming = aim;
        public void Shoot() { aiming = true; }
        public void Hit() { }
        public void Die()
        {
            dead = true;
            aiming = false;
            if (animator == null) return;
            if (animator.layerCount > 1) animator.SetLayerWeight(1, 0f);
            animator.SetBool("Shooting", false);
            animator.SetFloat("Speed", 0f);
            animator.SetBool("Dead", true);
        }
        public void HandsUp(bool up) { }
        public void Cower(bool cower) { }
    }
}
