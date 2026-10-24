using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Scene children are immutable templates. Attempts use fresh clones so dead/freed NPCs reset too.
    public class RaidEncounter : MonoBehaviour
    {
        public Transform enemyRoot;
        public Transform corruptPoliceRoot;
        public Transform evidencePoint;
        [Tooltip("More evidence besides evidencePoint (for example the boss's laptop); all must be taken.")]
        public Transform[] extraEvidence = new Transform[0];
        public bool Active { get; private set; }
        public bool EvidenceCollected => evidenceItems.Count > 0 ? taken.Count >= evidenceItems.Count : collectedWhole;
        public int EvidenceTotal => evidenceItems.Count;
        public int EvidenceTaken => taken.Count;
        public bool PoliceSpawned { get; private set; }
        public int RemainingEnemies
        {
            get
            {
                int count = 0;
                foreach (var enemy in enemies) if (enemy != null && !enemy.IsDead) count++;
                return count;
            }
        }
        public bool Cleared => Active && enemies.Count > 0 && RemainingEnemies == 0;
        public IReadOnlyList<ArmedEnemy> Enemies => enemies;

        readonly List<ArmedEnemy> enemies = new List<ArmedEnemy>();
        GameObject liveRaid, livePolice;
        readonly List<Transform> evidenceItems = new List<Transform>();
        readonly HashSet<Transform> taken = new HashSet<Transform>();
        readonly Dictionary<Transform, WorldInteractable> interactions = new Dictionary<Transform, WorldInteractable>();
        bool collectedWhole;

        void Awake()
        {
            HideTemplates();
            if (evidencePoint != null) evidenceItems.Add(evidencePoint);
            foreach (var extra in extraEvidence) if (extra != null) evidenceItems.Add(extra);
            foreach (var item in evidenceItems)
            {
                var interaction = item.GetComponent<WorldInteractable>();
                if (interaction == null) interaction = item.gameObject.AddComponent<WorldInteractable>();
                var label = item.GetComponent<EvidenceItem>();
                interaction.Prompt = label != null ? "เก็บหลักฐาน: " + label.label : "เก็บหลักฐาน";
                interaction.Available = false;
                var captured = item;
                interaction.OnInteract = () => TryCollectEvidence(captured);
                interactions[item] = interaction;
            }
        }

        void HideTemplates()
        {
            if (enemyRoot != null) enemyRoot.gameObject.SetActive(false);
            if (corruptPoliceRoot != null) corruptPoliceRoot.gameObject.SetActive(false);
        }

        public void BeginRaid()
        {
            ResetRaid();
            Active = true;
            HideTemplates();
            if (enemyRoot == null) return;
            liveRaid = Instantiate(enemyRoot.gameObject, transform);
            liveRaid.name = "Active Raid";
            enemies.AddRange(liveRaid.GetComponentsInChildren<ArmedEnemy>(true));
            liveRaid.SetActive(true);
            NpcGunfire.EnsureExists();
        }

        public void ResetRaid()
        {
            Active = false;
            collectedWhole = false;
            taken.Clear();
            PoliceSpawned = false;
            enemies.Clear();
            RemoveInstance(ref liveRaid);
            RemoveInstance(ref livePolice);
            HideTemplates();
            foreach (var pair in interactions) if (pair.Value != null) pair.Value.Available = false;
            foreach (var item in evidenceItems) if (item != null) item.gameObject.SetActive(true);
        }

        void RemoveInstance(ref GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
            instance = null;
        }

        public void SpawnCorruptPolice()
        {
            if (!Active || PoliceSpawned) return;
            PoliceSpawned = true;
            if (corruptPoliceRoot == null) return;
            livePolice = Instantiate(corruptPoliceRoot.gameObject, transform);
            livePolice.name = "Corrupt Police Reinforcements";
            livePolice.SetActive(true);
            // Police are a consequence, not extra evidence guards after the compound was cleared.
            var player = PlayerLocator.Player;
            foreach (var police in livePolice.GetComponentsInChildren<ArmedEnemy>())
            {
                police.Faction = Faction.CorruptPolice;
                if (player != null) police.Alert(player.transform.position);
            }
            NpcGunfire.EnsureExists();
        }

        // Takes one piece (the main one when none is named); true once something was taken.
        public bool TryCollectEvidence(Transform item = null)
        {
            if (!Cleared || EvidenceCollected) return false;
            if (evidenceItems.Count == 0) { collectedWhole = true; return true; }
            if (item == null) item = evidenceItems.Find(e => !taken.Contains(e));
            if (item == null || !taken.Add(item)) return false;
            item.gameObject.SetActive(false);
            return true;
        }

        void Update()
        {
            foreach (var pair in interactions)
                if (pair.Value != null) pair.Value.Available = Cleared && !taken.Contains(pair.Key);
        }

        void OnDisable() => ResetRaid();
    }
}
