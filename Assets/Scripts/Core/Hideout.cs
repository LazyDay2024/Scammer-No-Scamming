using UnityEngine;

namespace ScammerNoScamming
{
    // A scammer den in the action scene. Cases point at one by index; CaseStage fills it with people.
    public class Hideout : MonoBehaviour
    {
        public int index;
        public string label = "Hideout";
        // Where scammers and workers stand.
        public Transform[] spawnPoints;
        // Where victims hide, away from the door.
        public Transform[] hidingCorners;
    }
}
