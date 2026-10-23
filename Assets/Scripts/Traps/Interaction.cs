using UnityEngine;

namespace ScammerNoScamming
{
    // The "[E] do something" check shared by everything the player can use on foot.
    // Whoever offers a prompt each frame has it shown by the HUD.
    public static class Interaction
    {
        static PlayerCharacter player;
        static float nextSearch;
        static string prompt;
        static int promptFrame = -10;

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

        public static string CurrentPrompt => promptFrame >= Time.frameCount - 1 ? prompt : null;

        // True on the frame the player presses the use key while on foot within range of the spot.
        public static bool Pressed(PlayerCharacter user, Vector3 at, float range, string text)
        {
            var mount = user.GetComponent<PlayerMount>();
            if (mount != null && mount.IsMounted) return false;

            Vector3 offset = at - user.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > range * range) return false;

            prompt = text;
            promptFrame = Time.frameCount;
            return user.GetComponent<PlayerControls>().Interact.WasPressedThisFrame();
        }
    }
}
