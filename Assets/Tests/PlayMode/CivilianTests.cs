using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class CivilianTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            TroubleMeter.ResetAll(); // the meter is static and other tests add to it
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
            TroubleMeter.ResetAll();
        }

        T Track<T>(T component) where T : Component
        {
            spawned.Add(component.gameObject);
            return component;
        }

        static DamageInfo Shot(Faction from)
        {
            return new DamageInfo(1f, HitZoneType.Body, from, Vector3.zero);
        }

        [Test]
        public void Victims_and_forced_workers_are_on_their_own_sides()
        {
            Assert.AreEqual(Faction.Victim, Track(PlayerSandbox.CreateVictim(Vector3.zero)).Faction);
            Assert.AreEqual(Faction.ForcedWorker, Track(PlayerSandbox.CreateForcedWorker(new Vector3(3f, 0f, 0f))).Faction);
        }

        [Test]
        public void Player_hurting_a_civilian_adds_wrong_target_trouble_once_per_person()
        {
            var victim = Track(PlayerSandbox.CreateVictim(Vector3.zero));
            var worker = Track(PlayerSandbox.CreateForcedWorker(new Vector3(3f, 0f, 0f)));

            victim.Health.TakeDamage(Shot(Faction.Player));
            victim.Health.TakeDamage(Shot(Faction.Player));
            Assert.AreEqual(TroubleMeter.WrongTargetCost, TroubleMeter.Action, 0.001f, "a second hit on the same person adds nothing");

            worker.Health.TakeDamage(Shot(Faction.Player));
            Assert.AreEqual(TroubleMeter.WrongTargetCost * 2f, TroubleMeter.Action, 0.001f, "a different person counts again");
        }

        [Test]
        public void Damage_from_enemies_is_not_the_players_fault()
        {
            var victim = Track(PlayerSandbox.CreateVictim(Vector3.zero));

            victim.Health.TakeDamage(Shot(Faction.Scammer));

            Assert.AreEqual(0f, TroubleMeter.Current, 0.001f);
        }

        [Test]
        public void Victims_start_cowering_and_workers_only_cower_at_nearby_gunfire()
        {
            var victim = Track(PlayerSandbox.CreateVictim(Vector3.zero));
            var worker = Track(PlayerSandbox.CreateForcedWorker(new Vector3(3f, 0f, 0f)));
            Assert.IsTrue(victim.IsCowering, "victims are already hiding in fear");
            Assert.IsFalse(worker.IsCowering);
            float standingHeight = worker.transform.localScale.y;

            GameEvents.RaisePlayerFired(new Vector3(3f, 0f, 100f)); // far beyond hearing
            Assert.IsFalse(worker.IsCowering);

            GameEvents.RaisePlayerFired(new Vector3(3f, 0f, 10f));
            Assert.IsTrue(worker.IsCowering);
            Assert.Less(worker.transform.localScale.y, standingHeight, "a cowering worker is a smaller target");
        }

        [UnityTest]
        public IEnumerator A_real_player_shot_on_a_victim_hurts_them_and_costs_trouble()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            spawned.Add(ground);
            var player = PlayerSandbox.CreatePlayer();
            spawned.Add(player);
            var worker = Track(PlayerSandbox.CreateForcedWorker(new Vector3(0f, 0f, 8f)));
            Physics.SyncTransforms();
            yield return null;

            Vector3 origin = player.GetComponentInChildren<Camera>().transform.position;
            Vector3 chest = worker.transform.position + Vector3.up * 0.8f;
            Assert.IsTrue(player.GetComponent<HitscanWeapon>().TryFire(origin, (chest - origin).normalized, Faction.Player, out ShotResult shot));

            Assert.IsTrue(shot.HitDamageable);
            Assert.Less(worker.Health.Current, worker.Health.max);
            Assert.AreEqual(TroubleMeter.WrongTargetCost, TroubleMeter.Action, 0.001f);
        }
    }
}
