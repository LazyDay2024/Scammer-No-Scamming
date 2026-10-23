using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class MountedScammerTests
    {
        [TestCase(15f)]
        [TestCase(22f)]
        [TestCase(30f)]
        public void FlankDistanceIncludesBothBoundaries(float distance)
        {
            Vector3 rider = new Vector3(6f, 0f, -Mathf.Sqrt(distance * distance - 36f));
            Vector3 destination = MountedScammer.CalculateFlankDestination(
                rider, Vector3.zero, Vector3.forward, 1f, 15f, 30f);
            Assert.That(destination, Is.EqualTo(rider));
        }

        [TestCase(2f)]
        [TestCase(50f)]
        [TestCase(0f)]
        public void OutsideDistanceBandReturnsToSelectedFlank(float distance)
        {
            Vector3 destination = MountedScammer.CalculateFlankDestination(
                Vector3.right * distance, Vector3.zero, Vector3.forward, 1f, 15f, 30f);
            Assert.That(destination.x, Is.EqualTo(6f).Within(.001f));
            Assert.That(destination.z, Is.LessThan(0f));
            Assert.That(destination.magnitude, Is.InRange(15f, 30f));
        }

        [Test]
        public void WrongFlankChangesSidesAndIgnoresHeight()
        {
            Vector3 destination = MountedScammer.CalculateFlankDestination(
                new Vector3(22f, 100f, 0f), new Vector3(0f, 7f, 0f), Vector3.forward, -1f, 15f, 30f);
            Assert.That(destination.x, Is.EqualTo(-6f));
            Assert.That(destination.y, Is.EqualTo(7f));
            Assert.That((destination - new Vector3(0f, 7f, 0f)).magnitude, Is.EqualTo(22.5f).Within(.001f));
        }

        [Test]
        public void OverlapAndZeroPlayerForwardHaveStableFlank()
        {
            Vector3 destination = MountedScammer.CalculateFlankDestination(
                Vector3.zero, Vector3.zero, Vector3.zero, -1f, 15f, 30f);
            Assert.That(destination.x, Is.EqualTo(-6f));
            Assert.That(destination.magnitude, Is.EqualTo(22.5f).Within(.001f));
        }

        [Test]
        public void PlayerMovementTranslatesDesiredFlank()
        {
            Vector3 destination = MountedScammer.CalculateFlankDestination(
                new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 60f), Vector3.forward, 1f, 15f, 30f);
            Assert.That(destination.x, Is.EqualTo(6f));
            Assert.That(destination.z, Is.EqualTo(60f - Mathf.Sqrt(22.5f * 22.5f - 36f)).Within(.001f));
        }

        [Test]
        public void JoinNearStationaryRoadThreePlayerIsRejected()
        {
            Assert.That(MountedScammer.IsWaypointOutsideMinimumDistance(
                new Vector3(6f, 0f, -18f), new Vector3(6f, 0f, 5f), Vector3.zero, 15f), Is.False);
        }

        [Test]
        public void OutsideEndpointsCannotCutAcrossPlayerExclusionRadius()
        {
            Assert.That(MountedScammer.IsWaypointOutsideMinimumDistance(
                new Vector3(6f, 0f, -20f), new Vector3(6f, 0f, 20f), Vector3.zero, 15f), Is.False);
        }

        [Test]
        public void RockEgressOutsideRadiusRemainsUsable()
        {
            Assert.That(MountedScammer.IsWaypointOutsideMinimumDistance(
                new Vector3(9f, 0f, -18f), new Vector3(6f, 0f, -15f), Vector3.zero, 15f), Is.True);
        }

        [Test]
        public void RiderAlreadyTooCloseCannotSelectEvenFarWaypoint()
        {
            Assert.That(MountedScammer.IsWaypointOutsideMinimumDistance(
                new Vector3(6f, 0f, -5f), new Vector3(6f, 0f, -30f), Vector3.zero, 15f), Is.False);
        }

        [Test]
        public void HorizontalTangentAtMinimumRadiusIsAllowed()
        {
            Assert.That(MountedScammer.IsWaypointOutsideMinimumDistance(
                new Vector3(15f, 100f, -20f), new Vector3(15f, -100f, 20f), Vector3.zero, 15f), Is.True);
        }

        [TestCase(-20f, -17f, true)]
        [TestCase(-16f, -14f, false)]
        [TestCase(-10f, -11f, true)]
        [TestCase(-10f, -9f, false)]
        public void MountedStepCannotCrossMinimumDistanceAndOnlyEscapesWhenInside(float from, float to, bool expected)
        {
            Assert.That(MountedScammer.CanTakeMountedStep(new Vector3(0f, 0f, from),
                new Vector3(0f, 0f, to), Vector3.zero, 15f), Is.EqualTo(expected));
        }

        [Test]
        public void MountedStepWithOutsideEndpointsCannotCrossThroughPlayer()
        {
            Assert.That(MountedScammer.CanTakeMountedStep(new Vector3(0f, 0f, -20f),
                new Vector3(0f, 0f, 20f), Vector3.zero, 15f), Is.False);
        }

        [TestCase(true, true, false, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(true, true, true, false)]
        public void OnlyDeadHorseWithLivingMountedRiderDismounts(bool mounted, bool horseDead, bool riderDead, bool expected)
        {
            Assert.That(MountedScammer.ShouldDismount(mounted, horseDead, riderDead), Is.EqualTo(expected));
        }

        [Test]
        public void InactiveTemplateCanDismountAndResetBeforeAwake()
        {
            var template = new GameObject("Encounter template");
            template.SetActive(false);
            try
            {
                var actor = new GameObject("Mounted scammer");
                actor.transform.SetParent(template.transform, false);
                var horse = new GameObject("Horse");
                horse.transform.SetParent(actor.transform, false);
                var horseHealth = horse.AddComponent<Health>();
                var horseCollider = horse.AddComponent<BoxCollider>();
                var rider = new GameObject("Rider");
                rider.transform.SetParent(actor.transform, false);
                rider.transform.localPosition = new Vector3(0f, .72f, .08f);
                var body = new GameObject("Body");
                body.transform.SetParent(rider.transform, false);
                body.transform.localPosition = new Vector3(0f, 1.22f, 0f);
                var bodyCollider = body.AddComponent<BoxCollider>();
                bodyCollider.size = new Vector3(.6f, .65f, .45f);
                var mounted = actor.AddComponent<MountedScammer>();
                mounted.horseRoot = horse.transform;
                mounted.horseHealth = horseHealth;
                mounted.riderRoot = rider.transform;
                mounted.bodyCollider = bodyCollider;

                mounted.ResetForAction();
                horseHealth.TakeDamage(new DamageInfo(100f, HitZoneType.Body, Faction.Player, Vector3.zero));
                mounted.Tick(0f);
                Assert.That(mounted.IsMounted, Is.False);
                Assert.That(horse.transform.parent, Is.EqualTo(template.transform));
                Assert.That(horseCollider.enabled, Is.False);
                Assert.That(rider.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(bodyCollider.size.y, Is.EqualTo(1.15f));
                Assert.That(body.transform.localPosition.y, Is.EqualTo(.99f));

                mounted.ResetForAction();
                Assert.That(mounted.IsMounted, Is.True);
                Assert.That(horseHealth.IsDead, Is.False);
                Assert.That(horse.transform.parent, Is.EqualTo(actor.transform));
                Assert.That(horse.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(horse.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(horseCollider.enabled, Is.True);
                Assert.That(rider.transform.localPosition, Is.EqualTo(new Vector3(0f, .72f, .08f)));
                Assert.That(bodyCollider.size.y, Is.EqualTo(.65f));
                Assert.That(body.transform.localPosition.y, Is.EqualTo(1.22f));
            }
            finally { Object.DestroyImmediate(template); }
        }
    }
}
