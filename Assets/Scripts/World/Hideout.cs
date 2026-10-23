using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // One scam hideout in the city. The city has several; a case names the one it is about by id.
    // The analysis phase reveals it (decided: analysis unlocks the hideout location), which raises a tall
    // beacon over it that can be seen from the station, so revealing it needs no map UI.
    public class Hideout : MonoBehaviour
    {
        public static readonly List<Hideout> All = new List<Hideout>();

        public string hideoutId = "hideout";
        [Tooltip("Shown to the player when the location is unknown, e.g. \"โซนตึกแถว ทิศตะวันออก\".")]
        public string zoneHint;
        [Tooltip("Optional art for the beacon. If empty a plain pillar is created when revealed.")]
        public GameObject beacon;
        public Color beaconColor = new Color(0.9f, 0.73f, 0.43f);
        public float beaconHeight = 120f;
        [Tooltip("Serialized URP material, so its shader is included in Windows builds.")]
        public Material beaconMaterialAsset;

        public bool IsRevealed { get; private set; }

        Material beaconMaterial;

        void OnDestroy()
        {
            if (beaconMaterial == null) return;
            if (Application.isPlaying) Destroy(beaconMaterial);
            else DestroyImmediate(beaconMaterial);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => All.Clear();

        // Hideouts register while enabled, so keep the hideout object active and hide its contents instead.

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        public static Hideout Find(string id) => All.Find(h => h.hideoutId == id);

        public static void HideAll()
        {
            foreach (var hideout in All) hideout.Hide();
        }

        public void Reveal()
        {
            IsRevealed = true;
            if (beacon == null) beacon = CreatePillar();
            else if (!beacon.scene.IsValid()) beacon = Instantiate(beacon, transform);
            beacon.SetActive(true);
        }

        public void Hide()
        {
            IsRevealed = false;
            if (beacon != null) beacon.SetActive(false);
        }

        GameObject CreatePillar()
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Beacon";
            var collider = pillar.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
            pillar.transform.SetParent(transform, false);
            pillar.transform.localPosition = Vector3.up * beaconHeight * 0.5f;
            pillar.transform.localScale = new Vector3(1.2f, beaconHeight * 0.5f, 1.2f);
            var renderer = pillar.GetComponent<Renderer>();
            if (beaconMaterialAsset != null) renderer.sharedMaterial = beaconMaterialAsset;
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) { renderer.enabled = false; return pillar; }
                beaconMaterial = new Material(shader) { color = beaconColor };
                renderer.sharedMaterial = beaconMaterial;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return pillar;
        }
    }
}
