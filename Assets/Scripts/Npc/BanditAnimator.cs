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
            animator.SetBool("Shooting", aiming && speed < standingSpeed);
            animator.SetBool("Mounted", false);
        }

        public void ResetForAction()
        {
            dead = aiming = false;
            speed = 0f;
            if (animator == null) return;
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool("Dead", false);
        }

        public void SetSpeed(float metersPerSecond) => speed = Mathf.Max(0f, metersPerSecond);
        public void Aim(bool aim) => aiming = aim;
        public void Shoot() { aiming = true; }
        public void Hit() { }
        public void Die()
        {
            dead = true;
            if (animator == null) return;
            animator.SetBool("Shooting", false);
            animator.SetFloat("Speed", 0f);
            animator.SetBool("Dead", true);
        }
        public void HandsUp(bool up) { }
        public void Cower(bool cower) { }
    }
}
