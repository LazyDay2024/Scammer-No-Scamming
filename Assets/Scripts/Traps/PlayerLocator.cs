using UnityEngine;

namespace ScammerNoScamming
{
    // Finds the single player once and caches it, so traps and markers do not each search the scene.
    public static class PlayerLocator
    {
        static PlayerController cached;

        public static PlayerController Player
        {
            get
            {
                if (cached == null) cached = Object.FindAnyObjectByType<PlayerController>();
                return cached;
            }
        }

        public static void Set(PlayerController player) => cached = player;
    }
}
