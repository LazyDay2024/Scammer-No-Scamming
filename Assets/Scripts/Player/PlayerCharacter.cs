using UnityEngine;

namespace ScammerNoScamming
{
    // Gives the player health so enemies can hurt them. On death the player drops off the horse,
    // loses control and the view falls to the ground; ActionReplay decides what happens next.
    public class PlayerCharacter : Character
    {
        public const float StartingHealth = 100f;

        CapsuleCollider riderHitbox;

        public bool RiderHitboxActive => riderHitbox != null && riderHitbox.enabled;

        protected override void Awake()
        {
            base.Awake();
            Faction = Faction.Player;
            Health.SetMax(StartingHealth);
            CreateRiderHitbox();
        }

        // The walking CharacterController is off while riding, so shots need another collider to find the rider.
        // A shot aimed low still stops at the horse; only shots over its back reach the rider.
        public void SetRiding(Collider horseBody)
        {
            if (horseBody != null) Physics.IgnoreCollision(riderHitbox, horseBody);
            riderHitbox.enabled = horseBody != null;
        }

        protected override void OnDied(Health health)
        {
            GameEvents.RaisePlayerDied(this);
            var mount = GetComponent<PlayerMount>();
            if (mount != null)
            {
                mount.Dismount();
                mount.enabled = false;
            }

            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
            var gun = GetComponent<PlayerWeaponController>();
            if (gun != null) gun.enabled = false;

            // Drop the view to the floor and tip it over so the death reads at a glance.
            var view = GetComponentInChildren<Camera>();
            if (view != null)
            {
                if (view.transform.parent != transform) view.transform.parent.localPosition = new Vector3(0f, 0.4f, 0f);
                view.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
            }
        }

        void CreateRiderHitbox()
        {
            var hitbox = new GameObject("RiderHitbox");
            hitbox.transform.SetParent(transform, false);
            riderHitbox = hitbox.AddComponent<CapsuleCollider>();
            riderHitbox.radius = 0.3f;
            riderHitbox.height = 1.2f;
            riderHitbox.center = new Vector3(0f, 0.6f, 0f);
            riderHitbox.enabled = false;

            var walking = GetComponent<CharacterController>();
            if (walking != null) Physics.IgnoreCollision(riderHitbox, walking);
        }
    }
}
