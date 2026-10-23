using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // One owner for each world object; the central raycast prevents actions through walls.
    public class WorldInteractable : MonoBehaviour
    {
        public string Prompt = "สำรวจ";
        public float Range = 4.5f;
        public bool Available = true;
        public string ReadingTitle;
        [TextArea] public string ReadingBody;
        public Action OnInteract;

        public void Interact()
        {
            if (Available && isActiveAndEnabled) OnInteract?.Invoke();
        }
    }
}
