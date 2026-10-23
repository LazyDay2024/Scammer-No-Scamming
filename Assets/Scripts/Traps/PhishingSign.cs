using UnityEngine;

namespace ScammerNoScamming
{
    // A poster or phone screen with a too-good offer and a misspelled link. Pressing E "opens the link":
    // the player is hacked and the gun locks for hackDuration seconds (a proposal; the hack effect is not decided). Damage defaults to 0.
    public class PhishingSign : Trap
    {
        public float interactRange = 2.5f;
        public float hackDuration = 6f;

        public override TrapKind Kind => TrapKind.PhishingLink;

        protected override float DefaultDamage => 0f;

        void LateUpdate()
        {
            var player = FindPlayer();
            if (HasSprung || player == null) return;
            bool pressed = InteractPressedOnFoot(player);
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            if (pressed && offset.sqrMagnitude <= interactRange * interactRange)
                Spring(player.gameObject);
        }

        void OnGUI()
        {
            if (HasSprung || GameScreen.AnyOpen || !PlayerWithin(interactRange)) return;
            DrawPrompt("กด E สแกนลิงก์");
        }

        protected override void OnSprung(GameObject target)
        {
            var weapon = target != null ? target.GetComponentInChildren<PlayerWeaponController>() : null;
            if (weapon == null) return;
            var weaponLock = weapon.GetComponent<WeaponLock>();
            if (weaponLock == null) weaponLock = weapon.gameObject.AddComponent<WeaponLock>();
            weaponLock.Lock(hackDuration);
        }
    }
}
