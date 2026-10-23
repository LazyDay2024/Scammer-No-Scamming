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

        [Test]
        public void UnsafeRoadSegmentHoldsPositionUntilPlayerMovesAway()
        {
            using (var scene = new MovementScene(true, new Vector3(12f, 0f, -18f)))
            {
                var waypoint = new GameObject("Unsafe road waypoint").transform;
                waypoint.SetParent(scene.Root.transform);
                waypoint.position = scene.Origin + new Vector3(6f, 0f, 5f);
                scene.Actor.waypoints = new[] { waypoint };
                Vector3 start = scene.Actor.transform.position;
                scene.Actor.Alert(scene.Player.transform.position);
                scene.Actor.Tick(.1f);
                Assert.That(scene.Actor.transform.position, Is.EqualTo(start), "An unsafe route must not fall back to a direct flank.");

                scene.Player.transform.position = scene.Origin + Vector3.forward * 40f;
                Physics.SyncTransforms();
                scene.Actor.Tick(.1f);
                Vector3 movement = scene.Actor.transform.position - start;
                Assert.That(movement.magnitude, Is.GreaterThan(.5f));
                Assert.That(Vector3.Dot(movement.normalized, (waypoint.position - start).normalized), Is.GreaterThan(.99f),
                    "The retained waypoint should resume when its full segment is safe.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerEnteringSpacingAllowsRetreatAfterRouteEnds(bool completedWaypoint)
        {
            using (var scene = new MovementScene(true, new Vector3(6f, 0f, -20f)))
            {
                if (completedWaypoint)
                {
                    var waypoint = new GameObject("Reached waypoint").transform;
                    waypoint.SetParent(scene.Root.transform);
                    waypoint.position = scene.Actor.transform.position;
                    scene.Actor.waypoints = new[] { waypoint };
                }
                scene.Actor.Alert(scene.Player.transform.position);
                scene.Actor.Tick(.1f);
                Vector3 start = scene.Actor.transform.position;
                scene.Player.transform.position = scene.Origin + Vector3.back * 10f;
                Physics.SyncTransforms();
                float before = Vector3.Distance(start, scene.Player.transform.position);

                scene.Actor.Tick(.1f);

                Vector3 end = scene.Actor.transform.position;
                Assert.That(Vector3.Distance(start, end), Is.GreaterThan(.5f), "An exhausted route must allow a safe retreat.");
                Assert.That(Vector3.Distance(end, scene.Player.transform.position), Is.GreaterThan(before));
                Assert.That(Vector3.Dot(start - scene.Player.transform.position, end - start), Is.GreaterThan(0f),
                    "The retreat must immediately increase clearance, without first moving toward the player.");
            }
        }

        [Test]
        public void PlayerEnteringSpacingCannotPullRiderCloserAlongUnsafeActiveRoute()
        {
            using (var scene = new MovementScene(true, new Vector3(6f, 0f, -20f)))
            {
                var waypoint = new GameObject("Unsafe active waypoint").transform;
                waypoint.SetParent(scene.Root.transform);
                waypoint.position = scene.Origin + new Vector3(6f, 0f, -10f);
                scene.Actor.waypoints = new[] { waypoint };
                scene.Actor.Alert(scene.Player.transform.position);
                scene.Actor.Tick(.1f);
                Vector3 start = scene.Actor.transform.position;
                scene.Player.transform.position = scene.Origin + Vector3.back * 10f;
                Physics.SyncTransforms();

                scene.Actor.Tick(.1f);

                Assert.That(scene.Actor.transform.position, Is.EqualTo(start),
                    "Keep the active road route and hold instead of approaching the player or cutting off-road.");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TickCannotMoveHorseOrDismountedBodyThroughVerticalWall(bool mounted)
        {
            using (var scene = new MovementScene(mounted, Vector3.back * 40f))
            {
                var wall = new GameObject("Thin vertical wall");
                wall.transform.SetParent(scene.Root.transform);
                wall.transform.position = scene.Actor.transform.position + new Vector3(0f, 2f, 2f);
                wall.AddComponent<BoxCollider>().size = new Vector3(12f, 4f, .1f);
                Physics.SyncTransforms();
                Vector3 start = scene.Actor.transform.position;
                scene.Actor.Alert(start + Vector3.forward * 20f);
                scene.Actor.Tick(1f);
                Assert.That(scene.Actor.transform.position, Is.EqualTo(start), "A long tick must sweep the body rather than tunnel past the wall.");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TickMovesOnClearGroundIgnoringOwnCollidersAndTriggers(bool mounted)
        {
            using (var scene = new MovementScene(mounted, Vector3.back * 40f))
            {
                var trigger = new GameObject("Nonblocking trigger");
                trigger.transform.SetParent(scene.Root.transform);
                trigger.transform.position = scene.Actor.transform.position + new Vector3(0f, 2f, 2f);
                var collider = trigger.AddComponent<BoxCollider>();
                collider.size = new Vector3(12f, 4f, .1f);
                collider.isTrigger = true;
                Physics.SyncTransforms();
                Vector3 start = scene.Actor.transform.position;
                scene.Actor.Alert(start + Vector3.forward * 20f);
                scene.Actor.Tick(1f);
                Assert.That(Vector3.Distance(start, scene.Actor.transform.position), Is.GreaterThan(3f));
                Assert.That(scene.Actor.transform.position.y, Is.EqualTo(0f).Within(.001f));
            }
        }

        sealed class MovementScene : System.IDisposable
        {
            public readonly Vector3 Origin = new Vector3(10000f, 0f, 10000f);
            public readonly GameObject Root;
            public readonly MountedScammer Actor;
            public readonly PlayerController Player;
            readonly PlayerController previousPlayer;

            public MovementScene(bool mounted, Vector3 position)
            {
                previousPlayer = PlayerLocator.Player;
                Root = new GameObject("Mounted movement regression scene");
                var ground = new GameObject("Ground");
                ground.transform.SetParent(Root.transform);
                ground.transform.position = Origin + Vector3.down * .5f;
                ground.AddComponent<BoxCollider>().size = new Vector3(200f, 1f, 200f);
                var playerObject = new GameObject("Inactive perception target");
                playerObject.SetActive(false);
                playerObject.transform.SetParent(Root.transform);
                playerObject.transform.position = Origin;
                Player = playerObject.AddComponent<PlayerController>();
                PlayerLocator.Set(Player);

                var actor = new GameObject("Movement actor");
                actor.SetActive(false);
                actor.transform.SetParent(Root.transform);
                actor.transform.position = Origin + position;
                var body = new GameObject("Body");
                body.transform.SetParent(actor.transform, false);
                body.transform.localPosition = Vector3.up * (mounted ? 2f : .99f);
                var bodyCollider = body.AddComponent<BoxCollider>();
                bodyCollider.size = new Vector3(.6f, 1.15f, .45f);
                Actor = actor.AddComponent<MountedScammer>();
                Actor.bodyCollider = bodyCollider;
                Actor.turnSpeed = 0f;
                if (mounted)
                {
                    var horse = new GameObject("Horse");
                    horse.transform.SetParent(actor.transform, false);
                    var horseCollider = horse.AddComponent<BoxCollider>();
                    horseCollider.center = Vector3.up;
                    horseCollider.size = new Vector3(.85f, 1.3f, 2.3f);
                    Actor.horseRoot = horse.transform;
                    Actor.horseHealth = horse.AddComponent<Health>();
                }
                actor.SetActive(true);
                Actor.enabled = false;
                Actor.ResetForAction();
                Physics.SyncTransforms();
            }

            public void Dispose()
            {
                PlayerLocator.Set(previousPlayer);
                Object.DestroyImmediate(Root);
                Physics.SyncTransforms();
            }
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
