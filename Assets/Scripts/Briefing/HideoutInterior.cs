using UnityEngine;

namespace ScammerNoScamming
{
    // The root of a hideout interior scene (Assets/Scenes/HideOut). The scene is loaded additively while its case
    // is running; HideoutEntrance.Bind connects it to the door on the street.
    public class HideoutInterior : MonoBehaviour
    {
        public string hideoutId;
        public Hideout hideout;
        [Tooltip("Where the player stands after coming through the street door.")]
        public Transform entry;
        [Tooltip("The inside doorway; its destination is set to the street when the scene is bound.")]
        public DoorPortal exitPortal;

        public static HideoutInterior Find(string id)
        {
            foreach (var interior in FindObjectsByType<HideoutInterior>(FindObjectsSortMode.None))
                if (interior.hideoutId == id) return interior;
            return null;
        }
    }
}
