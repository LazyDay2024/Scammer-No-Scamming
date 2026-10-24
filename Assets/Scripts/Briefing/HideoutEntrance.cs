using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // The street side of a hideout: its door, its guards, and the name of the interior scene behind it.
    // CaseBriefing loads that scene when the case starts and binds it here; it finds the entrance of the
    // current case by hideout id.
    public class HideoutEntrance : MonoBehaviour
    {
        public static readonly List<HideoutEntrance> All = new List<HideoutEntrance>();

        public string hideoutId;
        public string sceneName;
        public RaidEncounter guards;
        public DoorPortal enterPortal;
        [Tooltip("Where the player stands on the street after coming out of the hideout.")]
        public Transform exitPoint;
        [Tooltip("Anything within interiorRadius of this point counts as inside the hideout (set when the scene is bound).")]
        public Transform interiorAnchor;
        public float interiorRadius = 60f;

        public static HideoutEntrance Find(string id) => All.Find(e => e.hideoutId == id);

        // Connects this door to the loaded interior: portals both ways, the beacon over the street door.
        public void Bind(HideoutInterior interior)
        {
            interiorAnchor = interior.transform;
            enterPortal.destination = interior.entry;
            enterPortal.requiresReveal = interior.hideout;
            if (interior.exitPortal != null) interior.exitPortal.destination = exitPoint;
            interior.hideout.beaconOffset = transform.position - interior.hideout.transform.position + Vector3.up * 7f;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => All.Clear();
    }
}
