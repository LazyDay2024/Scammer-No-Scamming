using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class RaidCombatTests
    {
        GameObject root;
        [TearDown] public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            PlayerLocator.Set(null);
            Time.timeScale = 1f;
            var listener = Object.FindAnyObjectByType<NpcGunfire>();
            if (listener != null) Object.DestroyImmediate(listener.gameObject);
        }

        [Test] public void RegenerationUsesOnlyTimeAfterDelayAndNeverRevives()
        {
            Assert.AreEqual(2.5f, PlayerVitals.RegenerationAmount(40f, 100f, 10f, 16.5f, 1f, 6f, 5f));
            Assert.AreEqual(0f, PlayerVitals.RegenerationAmount(0f, 100f, 10f, 20f, 1f, 6f, 5f));
            Assert.AreEqual(1f, PlayerVitals.RegenerationAmount(99f, 100f, 10f, 20f, 1f, 6f, 5f));
        }

        [Test] public void ReducedRawLethalDamageSurvivesAndRetryClearsDeath()
        {
            root = new GameObject("Player vitals");
            var vitals = root.AddComponent<PlayerVitals>();
            vitals.damageMultiplier = 0.5f;
            vitals.Health.TakeDamage(new DamageInfo(150f, HitZoneType.Body, Faction.Scammer, Vector3.zero));
            Assert.IsFalse(vitals.Health.IsDead);
            Assert.AreEqual(25f, vitals.Health.Current);
            vitals.Health.TakeDamage(new DamageInfo(100f, HitZoneType.Body, Faction.Scammer, Vector3.zero));
            Assert.IsTrue(vitals.Health.IsDead);
            vitals.ResetForAction(RunModifiers.None);
            Assert.IsFalse(vitals.Health.IsDead);
            Assert.AreEqual(100f, vitals.Health.Current);
        }

        [UnityTest] public IEnumerator EvidenceRequiresEveryEnemyAndRetryRestoresVanishedCorpses()
        {
            root = new GameObject("Encounter");
            root.SetActive(false);
            var templates = new GameObject("Templates");
            templates.transform.SetParent(root.transform);
            for (int i = 0; i < 2; i++)
            {
                var enemy = new GameObject("Enemy " + i);
                enemy.transform.SetParent(templates.transform);
                enemy.AddComponent<ArmedEnemy>();
            }
            var raid = root.AddComponent<RaidEncounter>();
            raid.enemyRoot = templates.transform;
            root.SetActive(true);
            raid.BeginRaid();
            Assert.AreEqual(2, raid.RemainingEnemies);
            Assert.IsFalse(raid.TryCollectEvidence());
            var enemies = root.GetComponentsInChildren<ArmedEnemy>().Where(e => e.gameObject.activeInHierarchy).ToArray();
            enemies[0].Health.TakeDamage(new DamageInfo(1000f, HitZoneType.Head, Faction.Player, Vector3.zero));
            Assert.IsFalse(raid.TryCollectEvidence());
            enemies[1].Health.TakeDamage(new DamageInfo(1000f, HitZoneType.Head, Faction.Player, Vector3.zero));
            Assert.IsTrue(raid.Cleared);
            foreach (var enemy in enemies) enemy.gameObject.SetActive(false);
            Assert.IsTrue(raid.TryCollectEvidence());
            Assert.IsFalse(raid.TryCollectEvidence());
            raid.ResetRaid();
            raid.BeginRaid();
            Assert.AreEqual(2, raid.RemainingEnemies);
            Assert.IsFalse(raid.Cleared);
            Assert.IsFalse(raid.EvidenceCollected);
            yield return null;
        }

        [Test] public void BlankRoomDoesNotCountAsCleared()
        {
            root = new GameObject("Empty");
            Assert.IsFalse(root.AddComponent<RaidRoom>().CheckCleared());
        }

        [UnityTest] public IEnumerator KilledSiblingAmbusherReturnsAliveOnRetryWithoutOldCorpseTimer()
        {
            root = new GameObject("Trap and sibling ambusher");
            var trapObject = new GameObject("Supply trap");
            trapObject.transform.SetParent(root.transform);
            var trap = trapObject.AddComponent<SupplyCrate>();
            var enemyObject = new GameObject("Hidden ambusher");
            enemyObject.transform.SetParent(root.transform);
            enemyObject.transform.localPosition = new Vector3(3f, 0f, 4f);
            var body = enemyObject.AddComponent<CapsuleCollider>();
            var enemy = enemyObject.AddComponent<ArmedEnemy>();
            enemy.corpseDelay = 0.02f;
            enemy.sinkDuration = 0.02f;
            Vector3 spawn = enemyObject.transform.localPosition;
            trap.activateOnSpring = new[] { enemyObject };
            trap.ResetForAction();
            trap.Spring(null);
            enemy.Health.TakeDamage(new DamageInfo(10000f, HitZoneType.Head, Faction.Player, Vector3.zero));
            Assert.IsTrue(enemy.IsDead);
            Assert.IsFalse(body.enabled);
            enemyObject.transform.position += Vector3.forward * 20f;
            trap.ResetForAction();
            Assert.IsFalse(enemyObject.activeSelf);
            trap.Spring(null);
            Assert.IsTrue(enemyObject.activeInHierarchy);
            Assert.IsFalse(enemy.IsDead);
            Assert.IsTrue(body.enabled);
            Assert.AreEqual(spawn, enemyObject.transform.localPosition);
            Assert.AreEqual(ArmedEnemyState.Idle, enemy.State);
            yield return new WaitForSeconds(0.12f);
            Assert.IsTrue(enemyObject.activeInHierarchy, "Prior corpse timer must be cancelled");
            Assert.AreEqual(spawn, enemyObject.transform.localPosition);
            enemy.Health.TakeDamage(new DamageInfo(10000f, HitZoneType.Head, Faction.Player, Vector3.zero));
            Assert.AreEqual(ArmedEnemyState.Dead, enemy.State, "Death handler must work on the second attempt");
            Assert.IsFalse(body.enabled);
        }

        [Test] public void RetryClearsDisguisedScammerStare()
        {
            root = new GameObject("Disguised scammer");
            var disguised = root.AddComponent<DisguisedScammer>();
            disguised.UpdateStare(true, 2f);
            disguised.ResetForAction();
            Assert.AreEqual(0f, disguised.Stare);
        }

        [Test] public void WallBlocksEnemyFireAndClearSightCanDamagePlayer()
        {
            root = new GameObject("Combat test");
            var player = PlayerSandbox.CreatePlayer();
            player.transform.SetParent(root.transform);
            player.transform.position = Vector3.forward * 10f;
            var vitals = player.AddComponent<PlayerVitals>();
            PlayerLocator.Set(player.GetComponent<PlayerController>());
            var enemyObject = new GameObject("Enemy");
            enemyObject.transform.SetParent(root.transform);
            var enemy = enemyObject.AddComponent<ArmedEnemy>();
            enemy.initialAimError = enemy.finalAimError = 0f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(0f, 1.5f, 5f);
            wall.transform.localScale = new Vector3(4f, 3f, 1f);
            Physics.SyncTransforms();
            Assert.IsFalse(enemy.CanSeePlayer(player.GetComponent<PlayerController>()));
            enemy.Tick(1f);
            Assert.AreEqual(100f, vitals.Health.Current);
            wall.SetActive(false);
            Physics.SyncTransforms();
            Assert.IsTrue(enemy.CanSeePlayer(player.GetComponent<PlayerController>()));
            enemy.Tick(1f);
            Assert.Less(vitals.Health.Current, 100f);
        }

        [UnityTest] public IEnumerator RetryKeepsAnalysisAndGenesButResetsActionTroubleTrapsAndInnocents()
        {
            root = new GameObject("Retry test");
            var player = PlayerSandbox.CreatePlayer();
            player.transform.SetParent(root.transform);
            PlayerLocator.Set(player.GetComponent<PlayerController>());
            var trapObject = new GameObject("Trap");
            trapObject.transform.SetParent(root.transform);
            var trap = trapObject.AddComponent<SupplyCrate>();
            var flowObject = new GameObject("Flow");
            flowObject.transform.SetParent(root.transform);
            var flow = flowObject.AddComponent<CaseFlowDemo>();
            yield return null;
            Assert.IsFalse(trapObject.activeSelf, "No road damage while investigating at the station");
            flowObject.GetComponent<MainMenuScreen>().Close();
            var database = CaseDatabase.LoadDefault();
            var catalog = GeneCatalog.CreateDefault();
            try
            {
                flow.DebugStartAction(database.Pool[0]);
                var result = new AnalysisResult { TroublePenalty = 10f, WrongCount = 1 };
                flow.OnAnalysed(result);
                flowObject.GetComponent<GeneSelectionScreen>().Close();
                var chosen = new GeneLoadout();
                chosen.TryAdd(catalog.Find("armored"));
                flow.OnGenes(chosen);
                float baseline = flow.Trouble;
                trap.Defuse();
                flow.AddActionTrouble(30f);
                var workerObject = new GameObject("Innocent");
                workerObject.transform.SetParent(root.transform);
                var worker = workerObject.AddComponent<Bystander>();
                worker.Health.TakeDamage(new DamageInfo(1f, HitZoneType.Body, Faction.Player, Vector3.zero));
                Assert.AreEqual(1, flow.InnocentsHit);
                player.GetComponent<PlayerVitals>().Health.TakeDamage(new DamageInfo(10000f, HitZoneType.Body, Faction.Scammer, Vector3.zero));
                flow.RetryAction();
                Assert.AreSame(chosen, flow.Loadout);
                Assert.AreSame(result, flow.CurrentAnalysis);
                Assert.AreEqual(baseline, flow.Trouble);
                Assert.AreEqual(0, flow.InnocentsHit);
                Assert.IsFalse(trap.IsDefused);
                Assert.IsFalse(trap.HasSprung);
                Assert.IsTrue(flow.ActionActive);
                Assert.IsFalse(player.GetComponent<PlayerVitals>().Health.IsDead);
            }
            finally
            {
                foreach (var definition in database.Pool) Object.DestroyImmediate(definition);
                Object.DestroyImmediate(database);
                foreach (var gene in catalog.Genes) Object.DestroyImmediate(gene);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test] public void PlayerShotsOnForcedWorkerAreRecordedAsInnocentDamage()
        {
            root = new GameObject("Worker");
            root.SetActive(false);
            var worker = root.AddComponent<Bystander>();
            worker.Faction = Faction.ForcedWorker;
            root.SetActive(true);
            int hits = 0;
            System.Action<Npc, DamageInfo> callback = (npc, info) =>
            {
                if (npc.Faction == Faction.ForcedWorker || npc.Faction == Faction.Victim) hits++;
            };
            Npc.Shot += callback;
            try
            {
                worker.Health.TakeDamage(new DamageInfo(1f, HitZoneType.Body, Faction.Player, Vector3.zero));
                Assert.AreEqual(1, hits);
            }
            finally { Npc.Shot -= callback; }
        }
    }
}
