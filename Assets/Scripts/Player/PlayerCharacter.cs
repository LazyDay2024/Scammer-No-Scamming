using System.Collections;
using UnityEngine;

namespace ScammerNoScamming
{
    // Gives the player health so enemies can hurt them. On death the player drops off the horse,
    // loses control and the view falls to the ground; ActionReplay decides what happens next.
    public class PlayerCharacter : Character
    {
        public const float StartingHealth = 100f;

        CapsuleCollider riderHitbox;
        Coroutine fall;
        const float FallSeconds = 1.1f;

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

            // The view collapses to the floor and rolls onto its side (see FallRoutine).
            var view = GetComponentInChildren<Camera>();
            if (view != null)
            {
                if (fall != null) StopCoroutine(fall);
                fall = StartCoroutine(FallRoutine(view.transform));
            }
        }

        // Knees give way: the eye drops with growing speed while the view pitches down and rolls over. Unscaled time, so
        // it also plays if something pauses the game at the moment of death.
        IEnumerator FallRoutine(Transform view)
        {
            var pivot = view.parent != transform ? view.parent : null;
            Vector3 from = pivot != null ? pivot.localPosition : Vector3.zero;
            Vector3 to = new Vector3(from.x, 0.4f, from.z);
            Quaternion roll = Quaternion.Euler(18f, 0f, 70f);
            for (float t = 0f; t < FallSeconds; t += Time.unscaledDeltaTime)
            {
                float u = Mathf.Clamp01(t / FallSeconds);
                float drop = u * u;                          // accelerates like a body falling
                float tip = 1f - (1f - u) * (1f - u);        // settles gently on its side
                if (pivot != null) pivot.localPosition = Vector3.Lerp(from, to, drop);
                view.localRotation = Quaternion.Slerp(Quaternion.identity, roll, tip);
                yield return null;
            }
            if (pivot != null) pivot.localPosition = to;
            view.localRotation = roll;
            fall = null;
        }

        // Undo OnDied after the health has been restored: control back on, camera upright.
        public void Revive()
        {
            if (fall != null) { StopCoroutine(fall); fall = null; }
            var mount = GetComponent<PlayerMount>();
            if (mount != null) mount.enabled = true;
            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = true;
            var gun = GetComponent<PlayerWeaponController>();
            if (gun != null) gun.enabled = true;
            var view = GetComponentInChildren<Camera>();
            if (view != null) view.transform.localRotation = Quaternion.identity; // the pivot height is reset by PlayerController
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
