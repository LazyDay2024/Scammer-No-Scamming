using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class TrapPlayTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            TrapLog.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ShootingTheDisguisedScammerDownDefusesIt()
        {
            root = new GameObject("Villager");
            var disguised = root.AddComponent<DisguisedScammer>();
            var health = root.GetComponent<Health>();
            Assert.IsNotNull(health, "DisguisedScammer must bring its own Health");
            yield return null;

            health.TakeDamage(new DamageInfo(health.max + 1f, HitZoneType.Body, Faction.Player, root.transform.position));
            yield return null;

            Assert.IsTrue(disguised.IsDefused);
            CollectionAssert.AreEqual(new[] { TrapKind.Disguised }, TrapLog.DefusedKinds);
        }

        [UnityTest]
        public IEnumerator WeaponLockEndsAndOnlyReenablesWhatItDisabled()
        {
            root = new GameObject("Player");
            var weapon = root.AddComponent<PlayerWeaponController>();
            var weaponLock = root.AddComponent<WeaponLock>();
            yield return null;

            weaponLock.Lock(0.1f);
            Assert.IsFalse(weapon.enabled);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(weapon.enabled);

            weapon.enabled = false;
            weaponLock.Lock(0.1f);
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(weapon.enabled, "a weapon disabled by something else must stay disabled");
        }
    }
}
