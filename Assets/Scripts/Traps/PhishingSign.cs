using System.Collections;
using UnityEngine;

namespace ScammerNoScamming
{
    // A poster or phone screen with a too-good offer and a misspelled link. Pressing E "opens the link":
    // the player is hacked and the gun locks for hackDuration seconds. Damage defaults to 0.
    public class PhishingSign : Trap
    {
        public float interactRange = 2.5f;
        public float hackDuration = 6f;

        public override TrapKind Kind => TrapKind.PhishingLink;
        public bool IsHacking { get; private set; }

        protected override float DefaultDamage => 0f;

        void Update()
        {
            var player = FindPlayer();
            if (HasSprung || player == null) return;
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > interactRange * interactRange) return;
            var controls = player.GetComponent<PlayerControls>();
            if (controls != null && controls.Interact != null && controls.Interact.WasPressedThisFrame())
                Spring(player.gameObject);
        }

        protected override void OnSprung(GameObject target)
        {
            var weapon = target != null ? target.GetComponentInChildren<PlayerWeaponController>() : null;
            if (weapon != null) StartCoroutine(LockWeapon(weapon));
        }

        IEnumerator LockWeapon(PlayerWeaponController weapon)
        {
            IsHacking = true;
            weapon.enabled = false;
            yield return new WaitForSeconds(hackDuration);
            if (weapon != null) weapon.enabled = true;
            IsHacking = false;
        }
    }
}
