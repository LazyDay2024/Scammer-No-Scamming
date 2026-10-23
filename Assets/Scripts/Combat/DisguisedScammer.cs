using UnityEngine;

namespace ScammerNoScamming
{
    // A scammer dressed as a passer-by. It wanders and ignores the player until the player stares at it
    // from close range, then it drops the act, rushes in and fires for several times the normal damage.
    public class DisguisedScammer : ShooterScammer
    {
        [Header("Disguise (placeholder numbers, not decided yet)")]
        public float noticeRange = 8f;
        public float noticeAngle = 15f;
        public float eyeContactTime = 0.5f;
        public float strongDamageMultiplier = 3f;
        public float closeRange = 4f;
        public float chaseSpeed = 4.5f;

        float contact;

        public bool IsRevealed { get; private set; }

        protected override void Tick()
        {
            if (!IsRevealed) WatchForEyeContact();
            base.Tick();
        }

        // Eye contact: the player's view points at its head, it is near, and nothing is in the way.
        void WatchForEyeContact()
        {
            if (Target == null || TargetEye == null) return;

            Vector3 toMe = EyePosition - TargetEye.position;
            bool looking = toMe.magnitude <= noticeRange
                && Vector3.Angle(TargetEye.forward, toMe) <= noticeAngle
                && HasLineOfSight(TargetEye.position, EyePosition);

            contact = looking ? contact + Time.deltaTime : Mathf.Max(0f, contact - Time.deltaTime);
            if (contact >= eyeContactTime) Reveal();
        }

        void Reveal()
        {
            IsRevealed = true;
            Weapon.Damage = shotDamage * strongDamageMultiplier;
            Alert(Target.transform.position);
        }

        protected override void Attack(PlayerCharacter target)
        {
            if (IsRevealed) base.Attack(target);
        }

        protected override void Pursue()
        {
            if (IsRevealed) Chase(closeRange, chaseSpeed);
            else Patrol();
        }
    }
}
