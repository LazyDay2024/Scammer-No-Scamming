using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    // How the teammate's traps reach the player systems: crates and ammo, the phishing sign calling riders.
    public class TrapBridgeTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        GameObject player;
        PlayerWeaponController gun;

        [SetUp]
        public void SetUp()
        {
            TroubleMeter.ResetAll();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.AddComponent<RuntimeNavMesh>();
            spawned.Add(ground);
            player = PlayerSandbox.CreatePlayer();
            spawned.Add(player);
            gun = player.GetComponent<PlayerWeaponController>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                Object.Destroy(enemy.gameObject);
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
            TroubleMeter.ResetAll();
        }

        SupplyCrate Crate(bool fake)
        {
            var crate = new GameObject(fake ? "Fake crate" : "Crate").AddComponent<SupplyCrate>();
            crate.isFake = fake;
            crate.damage = 0f;
            spawned.Add(crate.gameObject);
            return crate;
        }

        [UnityTest]
        public IEnumerator A_real_crate_refills_ammo()
        {
            yield return null;
            gun.Drain();

            Crate(fake: false).Use(player);

            Assert.AreEqual(RevolverAmmo.SupplyReserve, gun.Ammo.Reserve);
            Assert.AreEqual(0, gun.Ammo.Loaded, "supplies refill reserve; the player still reloads");
        }

        [UnityTest]
        public IEnumerator A_fake_crate_robs_every_round()
        {
            yield return null;
            Assert.AreEqual(RevolverAmmo.Capacity, gun.Ammo.Loaded);
            Assert.AreEqual(RevolverAmmo.SupplyReserve, gun.Ammo.Reserve);

            Crate(fake: true).Use(player);

            Assert.AreEqual(0, gun.Ammo.Loaded);
            Assert.AreEqual(0, gun.Ammo.Reserve);
        }

        [UnityTest]
        public IEnumerator Opening_a_phishing_link_calls_riders_in_around_the_player()
        {
            var sign = new GameObject("Sign").AddComponent<PhishingSign>();
            spawned.Add(sign.gameObject);
            var ambush = sign.gameObject.AddComponent<AmbushGroup>();
            ambush.kind = AmbushKind.Riders;
            ambush.count = 3;
            ambush.minRadius = 30f;
            ambush.maxRadius = 40f;
            ambush.trigger = sign;
            yield return null;

            sign.Spring(player);
            yield return null;

            var riders = Object.FindObjectsByType<RiderScammer>(FindObjectsSortMode.None);
            Assert.AreEqual(3, riders.Length);
            foreach (var rider in riders)
            {
                Assert.That(Vector3.Distance(rider.transform.position, player.transform.position), Is.InRange(25f, 45f));
                Assert.IsTrue(rider.IsEngaged);
            }
        }
    }
}
