using UnityEngine;

namespace ScammerNoScamming
{
    // Finds the PlayerCharacter (health, faction) once and caches it; PlayerLocator does the same for
    // the PlayerController.
    public static class Interaction
    {
        static PlayerCharacter player;
        static float nextSearch;

        public static PlayerCharacter Player
        {
            get
            {
                if (player == null && Time.time >= nextSearch)
                {
                    player = Object.FindFirstObjectByType<PlayerCharacter>();
                    // Only back off when there is no player to find, so a respawned player is picked up at once.
                    if (player == null) nextSearch = Time.time + 0.5f;
                }
                return player;
            }
        }
    }
}
