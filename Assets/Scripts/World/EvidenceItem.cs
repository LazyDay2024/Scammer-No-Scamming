using UnityEngine;

namespace ScammerNoScamming
{
    // One piece of evidence in a hideout (a server, a drive, a money bag...). EvidenceCase does the collecting.
    public class EvidenceItem : MonoBehaviour
    {
        public string label = "หลักฐาน";

        public bool Collected { get; private set; }

        public void Collect()
        {
            Collected = true;
            gameObject.SetActive(false);
        }
    }
}
