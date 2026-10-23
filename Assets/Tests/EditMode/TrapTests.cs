using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class TrapTests
    {
        class FakeDamageable : MonoBehaviour, IDamageable
        {
            public float Taken;
            public void TakeDamage(DamageInfo info) => Taken += info.Amount;
        }

        readonly List<GameObject> spawned = new List<GameObject>();
        int sprungEvents;

        GameObject New(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        void CountSprung(Trap _) => sprungEvents++;

        [SetUp]
        public void SetUp()
        {
            TrapLog.Reset();
            sprungEvents = 0;
            Trap.Sprung += CountSprung;
        }

        [TearDown]
        public void TearDown()
        {
            Trap.Sprung -= CountSprung;
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        [Test]
        public void FakeCrateDamagesWakesHiddenScammersAndLogsOnce()
        {
            var player = New("Player").AddComponent<FakeDamageable>();
            var hidden = New("HiddenScammer");
            hidden.SetActive(false);
            var crate = New("Crate").AddComponent<SupplyCrate>();
            crate.isFake = true;
            crate.activateOnSpring = new[] { hidden };

            crate.Use(player.gameObject);
            crate.Use(player.gameObject);

            Assert.AreEqual(25f, player.Taken);
            Assert.IsTrue(hidden.activeSelf);
            Assert.AreEqual(1, sprungEvents);
            CollectionAssert.AreEqual(new[] { TrapKind.FakeSupply }, TrapLog.SprungKinds);
        }

        [Test]
        public void RealCrateIsNotATrap()
        {
            var player = New("Player").AddComponent<FakeDamageable>();
            var crate = New("Crate").AddComponent<SupplyCrate>();
            crate.isFake = false;
            bool collected = false;
            System.Action<SupplyCrate, GameObject> onCollected = (c, user) => collected = true;
            SupplyCrate.Collected += onCollected;
            try
            {
                crate.Use(player.gameObject);
            }
            finally
            {
                SupplyCrate.Collected -= onCollected;
            }

            Assert.IsTrue(collected);
            Assert.AreEqual(0f, player.Taken);
            Assert.AreEqual(0, sprungEvents);
        }

        [Test]
        public void DefaultDamageDependsOnTrapAndCanBeOverridden()
        {
            Assert.AreEqual(0f, New("Sign").AddComponent<PhishingSign>().Damage);
            Assert.AreEqual(0f, New("Bait").AddComponent<AmbushBait>().Damage);
            var disguised = New("Villager").AddComponent<DisguisedScammer>();
            Assert.AreEqual(40f, disguised.Damage);
            disguised.damage = 10f;
            Assert.AreEqual(10f, disguised.Damage);
        }

        [Test]
        public void DefusedTrapCannotSpring()
        {
            var player = New("Player").AddComponent<FakeDamageable>();
            var disguised = New("Villager").AddComponent<DisguisedScammer>();
            disguised.Defuse();
            disguised.Spring(player.gameObject);

            Assert.AreEqual(0f, player.Taken);
            CollectionAssert.AreEqual(new[] { TrapKind.Disguised }, TrapLog.DefusedKinds);
            Assert.IsEmpty(TrapLog.SprungKinds);
        }

        [Test]
        public void DisguisedScammerNeedsHeldEyeContact()
        {
            var disguised = New("Villager").AddComponent<DisguisedScammer>();
            disguised.stareTime = 1.5f;

            Assert.IsFalse(disguised.UpdateStare(true, 1f));
            Assert.IsFalse(disguised.UpdateStare(false, 0.25f));
            Assert.AreEqual(0.5f, disguised.Stare, 1e-4f);
            Assert.IsFalse(disguised.UpdateStare(true, 0.9f));
            Assert.IsTrue(disguised.UpdateStare(true, 0.2f));
        }

        [Test]
        public void EyeContactNeedsRangeAndAngle()
        {
            var disguised = New("Villager").AddComponent<DisguisedScammer>();
            disguised.attackRange = 8f;
            disguised.lookAngle = 12f;
            Vector3 head = Vector3.up * 1.6f;

            Vector3 eye = new Vector3(0f, 1.6f, -5f);
            Assert.IsTrue(disguised.IsEyeContact(eye, (head - eye).normalized));
            Assert.IsFalse(disguised.IsEyeContact(eye, Vector3.right));
            Vector3 far = new Vector3(0f, 1.6f, -20f);
            Assert.IsFalse(disguised.IsEyeContact(far, (head - far).normalized));
        }

        [Test]
        public void AmbushBaitRadiusIgnoresHeight()
        {
            var bait = New("Bait").AddComponent<AmbushBait>();
            bait.triggerRadius = 3f;
            Assert.IsTrue(bait.IsInside(new Vector3(2f, 5f, 0f)));
            Assert.IsFalse(bait.IsInside(new Vector3(4f, 0f, 0f)));
        }

        [Test]
        public void HideoutRevealRaisesBeaconAndHideLowersIt()
        {
            var hideout = New("Hideout").AddComponent<Hideout>();
            Assert.IsFalse(hideout.IsRevealed);
            hideout.Reveal();
            Assert.IsTrue(hideout.IsRevealed);
            Assert.IsNotNull(hideout.beacon);
            Assert.IsTrue(hideout.beacon.activeSelf);
            Assert.IsNull(hideout.beacon.GetComponent<Collider>());
            hideout.Hide();
            Assert.IsFalse(hideout.beacon.activeSelf);
        }
    }
}
