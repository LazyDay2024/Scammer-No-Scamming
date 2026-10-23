using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class PlayerSandboxTests : InputTestFixture
    {
        Keyboard keyboard;
        Mouse mouse;
        PlayerSandbox.Result world;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (world != null)
            {
                Object.Destroy(world.Player);
                Object.Destroy(world.Horse);
                foreach (var target in world.Targets)
                    if (target != null) Object.Destroy(target.gameObject);
                Object.Destroy(world.Spawner.gameObject);
                Object.Destroy(world.Replay.gameObject);
            }
            base.TearDown();
        }

        IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Walk_W_moves_player_forward()
        {
            world = PlayerSandbox.Create();
            yield return null;
            float startZ = world.Player.transform.position.z;

            Press(keyboard.wKey);
            yield return new WaitForSeconds(1f);
            Release(keyboard.wKey);

            Assert.Greater(world.Player.transform.position.z - startZ, 3f);
        }

        [UnityTest]
        public IEnumerator Interact_near_horse_mounts_and_rides_then_dismounts()
        {
            world = PlayerSandbox.Create();
            var player = world.Player.GetComponent<PlayerController>();
            var mount = world.Player.GetComponent<PlayerMount>();
            var horse = world.Horse.GetComponent<HorseController>();
            yield return null;

            yield return Tap(keyboard.eKey);

            Assert.IsTrue(player.IsMounted, "player should be mounted after E near the horse");
            Assert.AreSame(horse.Seat, world.Player.transform.parent, "player should sit on the horse seat");
            Assert.IsTrue(horse.HasRider);
            Assert.IsFalse(world.Player.GetComponent<CharacterController>().enabled, "walking collider is off while riding");
            Assert.IsTrue(world.Player.GetComponent<PlayerCharacter>().RiderHitboxActive, "the rider can still be hit");

            float startZ = world.Horse.transform.position.z;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(1f);
            Release(keyboard.wKey);
            Assert.Greater(world.Horse.transform.position.z - startZ, 2f, "horse should move forward on W");

            yield return Tap(keyboard.eKey);

            Assert.IsFalse(player.IsMounted, "player should be dismounted after second E");
            Assert.IsNull(world.Player.transform.parent);
            Assert.IsFalse(horse.HasRider);
            Assert.IsTrue(world.Player.GetComponent<CharacterController>().enabled);
            Assert.IsFalse(world.Player.GetComponent<PlayerCharacter>().RiderHitboxActive);
            Assert.IsNull(mount.CurrentHorse);
        }

        [UnityTest]
        public IEnumerator Player_is_thrown_when_the_horse_goes_down_and_cannot_remount_it()
        {
            world = PlayerSandbox.Create();
            var player = world.Player.GetComponent<PlayerController>();
            var horse = world.Horse.GetComponent<HorseController>();
            yield return null;
            yield return Tap(keyboard.eKey);
            Assert.IsTrue(player.IsMounted);

            world.Horse.GetComponent<Health>().TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Scammer, Vector3.zero));
            yield return null;
            yield return null;

            Assert.IsTrue(horse.IsDown);
            Assert.IsFalse(player.IsMounted, "a downed horse throws its rider");
            Assert.IsFalse(horse.HasRider);

            yield return Tap(keyboard.eKey);
            Assert.IsFalse(player.IsMounted, "a downed horse cannot be mounted");
        }

        [UnityTest]
        public IEnumerator Mounted_player_loses_health_to_an_enemy_shooting_over_the_horse()
        {
            world = PlayerSandbox.Create();
            var character = world.Player.GetComponent<PlayerCharacter>();
            yield return null;
            yield return Tap(keyboard.eKey);
            Assert.IsTrue(world.Player.GetComponent<PlayerController>().IsMounted);

            var enemy = PlayerSandbox.CreateEnemy(new Vector3(2f, 0f, 16f));
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            enemy.patrolRadius = 0f;
            enemy.spreadDegrees = 0f;
            enemy.reactionTime = 0.1f;
            try
            {
                float deadline = Time.time + 4f;
                while (character.Health.Current >= 100f && Time.time < deadline) yield return null;

                Assert.Less(character.Health.Current, 100f, "a rider must be hittable");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Interact_far_from_horse_does_nothing()
        {
            world = PlayerSandbox.Create();
            world.Horse.transform.position = new Vector3(30f, 0.05f, 30f);
            Physics.SyncTransforms(); // a CharacterController snaps back to its old position otherwise
            var player = world.Player.GetComponent<PlayerController>();
            yield return null;

            yield return Tap(keyboard.eKey);

            Assert.IsFalse(player.IsMounted);
        }

        [UnityTest]
        public IEnumerator Fire_three_times_at_target_kills_it()
        {
            world = PlayerSandbox.Create();
            var weapon = world.Player.GetComponent<PlayerWeaponController>();
            weapon.requireLockedCursor = false;
            var shots = new System.Collections.Generic.List<string>();
            weapon.Weapon.Fired += shot => shots.Add($"t={Time.time:F2} hit={shot.HitDamageable} end={shot.EndPoint}");
            var target = world.Targets[0];
            target.transform.position = new Vector3(0f, 1.6f, 10f);
            Physics.SyncTransforms(); // otherwise the ray can still see the collider at its old spot
            yield return null;

            // The input test harness sometimes drops a click, so each of the three hits may take a few clicks.
            for (int hit = 0; hit < 3 && !target.IsDead; hit++)
            {
                float before = target.Current;
                for (int attempt = 0; attempt < 4 && target.Current >= before; attempt++)
                {
                    yield return Tap(mouse.leftButton);
                    yield return new WaitForSeconds(0.25f);
                }
            }

            var cam = world.Player.GetComponentInChildren<Camera>();
            Assert.IsTrue(target.IsDead, $"three hitscan hits should kill a 3 HP target, health left {target.Current}, shots: [{string.Join(" | ", shots)}] camForward={cam.transform.forward} targetPos={target.transform.position}");
        }
    }
}
