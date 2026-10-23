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
        public bool Active { get; private set; }
        public bool EvidenceCollected { get; private set; }
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
        WorldInteractable evidenceInteraction;

        void Awake()
        {
            HideTemplates();
            if (evidencePoint != null)
            {
                evidenceInteraction = evidencePoint.GetComponent<WorldInteractable>();
                if (evidenceInteraction == null) evidenceInteraction = evidencePoint.gameObject.AddComponent<WorldInteractable>();
                evidenceInteraction.Prompt = "เก็บหลักฐาน";
                evidenceInteraction.Available = false;
                evidenceInteraction.OnInteract = () => TryCollectEvidence();
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
            EvidenceCollected = false;
            PoliceSpawned = false;
            enemies.Clear();
            RemoveInstance(ref liveRaid);
            RemoveInstance(ref livePolice);
            HideTemplates();
            if (evidenceInteraction != null) evidenceInteraction.Available = false;
            if (evidencePoint != null) evidencePoint.gameObject.SetActive(true);
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

        public bool TryCollectEvidence()
        {
            if (!Cleared || EvidenceCollected) return false;
            EvidenceCollected = true;
            if (evidencePoint != null) evidencePoint.gameObject.SetActive(false);
            return true;
        }

        void Update()
        {
            if (evidenceInteraction != null) evidenceInteraction.Available = Cleared && !EvidenceCollected;
        }

        void OnDisable() => ResetRaid();
    }
}
