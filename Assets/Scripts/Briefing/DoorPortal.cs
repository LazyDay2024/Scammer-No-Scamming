using UnityEngine;

namespace ScammerNoScamming
{
    // Walking through this doorway on foot moves the player to `destination`, standing in for the scene change
    // into the hideout. A short fade hides the jump.
    public class DoorPortal : MonoBehaviour
    {
        public Transform destination;
        [Tooltip("If set, the doorway only works once this hideout has been revealed by picking it on the map.")]
        public Hideout requiresReveal;
        public float radius = 1.3f;
        public float height = 3f;

        static float blackUntil;
        static float lastUse = -10f;

        void Update()
        {
            var player = PlayerLocator.Player;
            if (player == null || destination == null || Time.unscaledTime - lastUse < 1f || GameScreen.AnyOpen) return;
            if (requiresReveal != null && !requiresReveal.IsRevealed) return; // only the raid target's door opens
            var mount = player.GetComponent<PlayerMount>();
            if (mount != null && mount.IsMounted) return;
            Vector3 offset = player.transform.position - transform.position;
            if (offset.y < -.5f || offset.y > height || new Vector2(offset.x, offset.z).sqrMagnitude > radius * radius) return;

            lastUse = Time.unscaledTime;
            blackUntil = Time.unscaledTime + .45f;
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.SetPositionAndRotation(destination.position, destination.rotation);
            player.ResetLook();
            controller.enabled = true;
        }

        void OnGUI()
        {
            float left = blackUntil - Time.unscaledTime;
            if (left <= 0f || this != FirstPortal) return;
            GUI.depth = -100;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(left / .45f));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), ThaiGui.Solid(Color.black));
            GUI.color = Color.white;
        }

        // One portal draws the fade so overlapping ones do not darken it twice.
        static DoorPortal first;
        static DoorPortal FirstPortal => first != null ? first : first = FindFirstObjectByType<DoorPortal>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { first = null; blackUntil = 0f; lastUse = -10f; }
    }
}
