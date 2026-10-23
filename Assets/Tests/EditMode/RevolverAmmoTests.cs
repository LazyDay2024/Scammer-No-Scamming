using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class RevolverAmmoTests
    {
        GameObject owner;
        RevolverAmmo ammo;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Revolver ammo test");
            ammo = owner.AddComponent<RevolverAmmo>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(owner);

        [Test]
        public void StartsWithSixRoundsAndThirtySixInReserve()
        {
            Assert.That(ammo.Loaded, Is.EqualTo(6));
            Assert.That(ammo.Reserve, Is.EqualTo(36));
            Assert.That(ammo.Reloading, Is.False);
            Assert.That(ammo.TryReload(), Is.False, "a full cylinder cannot reload");
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(6)]
        public void ReloadOnlyTransfersMissingRoundsAfterOnePointSixSeconds(int fired)
        {
            FireRounds(fired);
            Assert.That(ammo.TryReload(), Is.True);
            ammo.AdvanceReload(0.8f);
            Assert.That(ammo.Loaded, Is.EqualTo(6 - fired));
            Assert.That(ammo.Reserve, Is.EqualTo(36));
            Assert.That(ammo.ReloadProgress, Is.EqualTo(0.5f).Within(0.001f));

            ammo.AdvanceReload(0.8f);
            Assert.That(ammo.Loaded, Is.EqualTo(6));
            Assert.That(ammo.Reserve, Is.EqualTo(36 - fired));
            Assert.That(ammo.Reloading, Is.False);
        }

        [Test]
        public void ReloadWithLimitedReserveCannotCreateRounds()
        {
            SpendReserve(34);
            FireRounds(6);
            Assert.That(ammo.TryReload(), Is.True);
            ammo.AdvanceReload(1.6f);
            Assert.That(ammo.Loaded, Is.EqualTo(2));
            Assert.That(ammo.Reserve, Is.Zero);
        }

        [Test]
        public void EmptyReserveCannotStartManualOrAutomaticReload()
        {
            SpendReserve(36);
            FireRounds(6);
            int starts = 0;
            int emptyPulls = 0;
            ammo.ReloadStarted += () => starts++;
            ammo.EmptyTriggered += () => emptyPulls++;
            Assert.That(ammo.TryReload(), Is.False);
            Assert.That(ammo.TryConsumeRound(), Is.False);
            Assert.That(ammo.Reloading, Is.False);
            Assert.That(starts, Is.Zero);
            Assert.That(emptyPulls, Is.EqualTo(1));
        }

        [Test]
        public void EmptyTriggerStartsReloadWithoutFiring()
        {
            FireRounds(6);
            int emptyPulls = 0;
            int starts = 0;
            ammo.EmptyTriggered += () => emptyPulls++;
            ammo.ReloadStarted += () => starts++;
            Assert.That(ammo.Reloading, Is.False, "the sixth shot itself does not reload");
            Assert.That(ammo.TryConsumeRound(), Is.False);
            Assert.That(ammo.Reloading, Is.True);
            Assert.That(emptyPulls, Is.EqualTo(1));
            Assert.That(starts, Is.EqualTo(1));
        }

        [Test]
        public void FireAndRepeatedReloadCannotInterruptOrRestartReload()
        {
            FireRounds(2);
            ammo.TryReload();
            ammo.AdvanceReload(0.8f);
            Assert.That(ammo.TryConsumeRound(), Is.False);
            Assert.That(ammo.TryReload(), Is.False);
            Assert.That(ammo.ReloadProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(ammo.Loaded, Is.EqualTo(4));
            ammo.AdvanceReload(0.8f);
            Assert.That(ammo.Loaded, Is.EqualTo(6));
            Assert.That(ammo.Reserve, Is.EqualTo(34));
        }

        [Test]
        public void InterruptPreservesLoadedAndReserveAndCannotCompleteLater()
        {
            FireRounds(3);
            ammo.TryReload();
            ammo.AdvanceReload(1.2f);
            int interruptions = 0;
            int completions = 0;
            ammo.ReloadInterrupted += () => interruptions++;
            ammo.ReloadCompleted += () => completions++;
            ammo.InterruptReload();
            ammo.InterruptReload();
            ammo.AdvanceReload(10f);
            Assert.That(ammo.Reloading, Is.False);
            Assert.That(ammo.ReloadProgress, Is.Zero);
            Assert.That(ammo.Loaded, Is.EqualTo(3));
            Assert.That(ammo.Reserve, Is.EqualTo(36));
            Assert.That(interruptions, Is.EqualTo(1));
            Assert.That(completions, Is.Zero);
        }

        [Test]
        public void MountTransitionBlocksReloadAndFireForItsFrame()
        {
            FireRounds(1);
            ammo.TryReload();
            ammo.NotifyMountTransition();
            Assert.That(ammo.Reloading, Is.False);
            Assert.That(ammo.TryReload(), Is.False);
            Assert.That(ammo.TryConsumeRound(), Is.False);
            Assert.That(ammo.Loaded, Is.EqualTo(5));
            Assert.That(ammo.Reserve, Is.EqualTo(36));
        }

        [Test]
        public void ChangeAndCompletionEventsOnlyReportRealTransitions()
        {
            int changes = 0;
            int starts = 0;
            int completions = 0;
            ammo.Changed += () => changes++;
            ammo.ReloadStarted += () => starts++;
            ammo.ReloadCompleted += () => completions++;
            ammo.TryReload();
            ammo.TryConsumeRound();
            ammo.TryReload();
            ammo.TryReload();
            ammo.AdvanceReload(0.4f);
            ammo.AdvanceReload(2f);
            ammo.AdvanceReload(2f);
            Assert.That(changes, Is.EqualTo(3), "shot, reload start, reload completion");
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void ZeroNegativeAndInvalidTimeDoNotAdvanceReload()
        {
            FireRounds(1);
            ammo.TryReload();
            ammo.AdvanceReload(0f);
            ammo.AdvanceReload(-1f);
            ammo.AdvanceReload(float.NaN);
            ammo.AdvanceReload(float.PositiveInfinity);
            Assert.That(ammo.ReloadProgress, Is.Zero);
            Assert.That(ammo.Loaded, Is.EqualTo(5));
            Assert.That(ammo.Reserve, Is.EqualTo(36));
        }

        [Test]
        public void SupplyRefillsReserveWithoutFinishingOrRestartingReload()
        {
            SpendReserve(12);
            FireRounds(2);
            ammo.TryReload();
            ammo.AdvanceReload(0.8f);
            ammo.RefillReserve();
            Assert.That(ammo.Loaded, Is.EqualTo(4));
            Assert.That(ammo.Reserve, Is.EqualTo(36));
            Assert.That(ammo.ReloadProgress, Is.EqualTo(0.5f).Within(0.001f));
            ammo.AdvanceReload(0.8f);
            Assert.That(ammo.Reserve, Is.EqualTo(34));
        }

        [Test]
        public void WeaponFindsAmmoAddedLaterAndCooldownDoesNotSpendExtraRound()
        {
            Object.DestroyImmediate(ammo);
            var weapon = owner.AddComponent<HitscanWeapon>();
            ammo = owner.AddComponent<RevolverAmmo>();
            weapon.FireInterval = 100f;
            Assert.That(weapon.TryFire(Vector3.zero, Vector3.forward, Faction.Player, out _), Is.True);
            Assert.That(weapon.TryFire(Vector3.zero, Vector3.forward, Faction.Player, out _), Is.False);
            Assert.That(ammo.Loaded, Is.EqualTo(5));
        }

        [Test]
        public void WeaponsWithoutAmmoKeepExistingUnlimitedFireBehavior()
        {
            Object.DestroyImmediate(ammo);
            var weapon = owner.AddComponent<HitscanWeapon>();
            weapon.FireInterval = 0f;
            for (int i = 0; i < 12; i++)
                Assert.That(weapon.TryFire(Vector3.zero, Vector3.forward, Faction.Player, out _), Is.True);
        }

        void FireRounds(int count)
        {
            for (int i = 0; i < count; i++)
                Assert.That(ammo.TryConsumeRound(), Is.True);
        }

        void SpendReserve(int count)
        {
            while (count > 0)
            {
                int rounds = Mathf.Min(RevolverAmmo.Capacity, count);
                FireRounds(rounds);
                Assert.That(ammo.TryReload(), Is.True);
                ammo.AdvanceReload(1.6f);
                count -= rounds;
            }
        }
    }
}
