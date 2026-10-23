using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class TroubleTests : InputTestFixture
    {
        Mouse mouse;
        GameObject player;
        readonly List<GameObject> spawned = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            TroubleMeter.ResetAll(); // the meter is static and other tests add to it
        }

        public override void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                Object.Destroy(enemy.gameObject);
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
            if (player != null) Object.Destroy(player);
            player = null;
            TroubleMeter.ResetAll();
            base.TearDown();
        }

        PlayerWeaponController MakePlayer()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.AddComponent<RuntimeNavMesh>();
            spawned.Add(ground);
            player = PlayerSandbox.CreatePlayer();
            var gun = player.GetComponent<PlayerWeaponController>();
            gun.requireLockedCursor = false;
            return gun;
        }

        [Test]
        public void Analysis_and_gene_trouble_survive_a_replay_but_action_trouble_does_not()
        {
            TroubleMeter.Add(TroubleSource.WrongAnswer, 10f);
            TroubleMeter.Add(TroubleSource.ExtraGene, 5f);
            TroubleMeter.Add(TroubleSource.Gunshot, 3f);
            TroubleMeter.Add(TroubleSource.Trap, 7f);
            Assert.AreEqual(25f, TroubleMeter.Current, 0.001f);

            GameEvents.RaiseActionReplaying();

            Assert.AreEqual(15f, TroubleMeter.Carried, 0.001f);
            Assert.AreEqual(0f, TroubleMeter.Action, 0.001f);
            Assert.AreEqual(15f, TroubleMeter.Current, 0.001f);
        }

        [Test]
        public void Full_is_announced_once_per_fill_and_the_meter_caps_at_max()
        {
            int full = 0;
            System.Action listener = () => full++;
            GameEvents.TroubleFull += listener;
            try
            {
                TroubleMeter.Add(TroubleSource.Gunshot, TroubleMeter.Max - 1f);
                Assert.AreEqual(0, full);
                TroubleMeter.Add(TroubleSource.Gunshot, 5f);
                TroubleMeter.Add(TroubleSource.Gunshot, 5f);
                Assert.AreEqual(1, full, "crossing the top raises it once");
                Assert.AreEqual(TroubleMeter.Max, TroubleMeter.Current, 0.001f);

                TroubleMeter.ResetAction();
                TroubleMeter.Add(TroubleSource.Trap, TroubleMeter.Max);
                Assert.AreEqual(2, full, "filling it again after a reset raises it again");
            }
            finally
            {
                GameEvents.TroubleFull -= listener;
            }
        }

        [UnityTest]
        public IEnumerator Each_shot_the_player_fires_adds_gunshot_trouble()
        {
            var gun = MakePlayer();
            int shots = 0;
            gun.Weapon.Fired += _ => shots++;
            yield return null;

            // The input test harness sometimes drops a click, so allow a few.
            for (int attempt = 0; attempt < 4 && shots == 0; attempt++)
            {
                Press(mouse.leftButton, queueEventOnly: true);
                yield return null;
                Release(mouse.leftButton, queueEventOnly: true);
                yield return new WaitForSeconds(0.25f);
            }

            Assert.AreEqual(1, shots);
            Assert.AreEqual(TroubleMeter.GunshotCost, TroubleMeter.Action, 0.001f);
        }

        [UnityTest]
        public IEnumerator A_sprung_trap_adds_its_trouble_cost()
        {
            MakePlayer();
            var spot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spot.transform.position = new Vector3(0f, 0.5f, 3f);
            spawned.Add(spot);
            var trap = spot.AddComponent<ProximityTrap>();
            trap.triggers = TriggerKind.Enter;
            trap.troubleCost = 7f;
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(trap.IsSprung);
            Assert.AreEqual(7f, TroubleMeter.Action, 0.001f);
        }

        [UnityTest]
        public IEnumerator Full_trouble_sends_corrupt_police_riders_after_the_player()
        {
            MakePlayer();
            var raidObject = new GameObject("Raid");
            spawned.Add(raidObject);
            var group = raidObject.AddComponent<AmbushGroup>();
            group.kind = AmbushKind.Riders;
            group.count = 2;
            group.minRadius = 25f;
            group.maxRadius = 30f;
            var raid = raidObject.AddComponent<CorruptPoliceRaid>();
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<RiderScammer>(FindObjectsSortMode.None).Length);

            TroubleMeter.Add(TroubleSource.WrongAnswer, TroubleMeter.Max);
            yield return null;

            Assert.IsTrue(raid.HasRaided);
            var riders = Object.FindObjectsByType<RiderScammer>(FindObjectsSortMode.None);
            Assert.AreEqual(2, riders.Length);
            foreach (var rider in riders)
            {
                Assert.AreEqual(Faction.CorruptPolice, rider.Faction);
                Assert.IsTrue(rider.IsEngaged);
            }
            Assert.IsTrue(player.GetComponent<PlayerCharacter>().Health.Current > 0f, "a raid is danger, not an instant loss");
        }
    }
}
