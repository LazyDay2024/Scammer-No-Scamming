using UnityEngine;

namespace ScammerNoScamming
{
    // Added to the player's weapon by the phishing trap. It lives on the player, so the lock always ends
    // even if the sign is destroyed, and it only re-enables a weapon that it disabled itself.
    public class WeaponLock : MonoBehaviour
    {
        PlayerWeaponController weapon;
        float unlockTime;

        public bool IsLocked { get; private set; }

        public void Lock(float seconds)
        {
            if (weapon == null) weapon = GetComponent<PlayerWeaponController>();
            if (weapon == null) return;
            if (!IsLocked)
            {
                if (!weapon.enabled) return;
                weapon.enabled = false;
                IsLocked = true;
            }
            unlockTime = Mathf.Max(unlockTime, Time.time + seconds);
        }

        void Update()
        {
            if (IsLocked && Time.time >= unlockTime) Release();
        }

        void OnDisable()
        {
            if (IsLocked) Release();
        }

        void Release()
        {
            IsLocked = false;
            if (weapon != null) weapon.enabled = true;
        }
    }
}
