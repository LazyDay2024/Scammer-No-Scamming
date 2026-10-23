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
        public IEnumerator Reload_swaps_in_a_fresh_magazine_after_the_delay_and_blocks_firing_meanwhile()
        {
            world = PlayerSandbox.Create();
            var gun = world.Player.GetComponent<PlayerWeaponController>();
            gun.requireLockedCursor = false;
            gun.reloadTime = 0.3f;
            yield return null;
            gun.SetRounds(2, 20);

            yield return Tap(keyboard.rKey);
            Assert.IsTrue(gun.Reloading);

            yield return Tap(mouse.leftButton);
            Assert.AreEqual(2, gun.Loaded, "a gun that is reloading cannot fire");

            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(gun.Reloading);
            Assert.AreEqual(gun.magazineSize, gun.Loaded);
            Assert.AreEqual(12, gun.Reserve, "only the missing rounds leave the reserve");
        }

        [UnityTest]
        public IEnumerator Reload_does_nothing_with_a_full_magazine_or_an_empty_reserve()
        {
            world = PlayerSandbox.Create();
            var gun = world.Player.GetComponent<PlayerWeaponController>();
            yield return null;

            yield return Tap(keyboard.rKey);
            Assert.IsFalse(gun.Reloading, "magazine is already full");

            gun.SetRounds(0, 0);
            yield return Tap(keyboard.rKey);
            Assert.IsFalse(gun.Reloading, "nothing left to load");
        }

        [UnityTest]
        public IEnumerator Player_can_reload_while_mounted()
        {
            world = PlayerSandbox.Create();
            var gun = world.Player.GetComponent<PlayerWeaponController>();
            gun.reloadTime = 0.3f;
            yield return null;
            yield return Tap(keyboard.eKey);
            Assert.IsTrue(world.Player.GetComponent<PlayerController>().IsMounted);
            gun.SetRounds(1, 20);

            yield return Tap(keyboard.rKey);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(gun.magazineSize, gun.Loaded);
            Assert.IsTrue(world.Player.GetComponent<PlayerController>().IsMounted, "reloading must not drop the rider");
        }

        [UnityTest]
        public IEnumerator Hurt_and_kill_keys_work_for_a_mounted_player()
        {
            world = PlayerSandbox.Create();
            var character = world.Player.GetComponent<PlayerCharacter>();
            yield return null;
            yield return Tap(keyboard.eKey);

            yield return Tap(keyboard.hKey);
            Assert.AreEqual(80f, character.Health.Current, 0.01f);
            Assert.IsTrue(world.Player.GetComponent<PlayerController>().IsMounted);

            yield return Tap(keyboard.kKey);
            Assert.IsTrue(character.Health.IsDead);
            Assert.IsFalse(world.Player.GetComponent<PlayerController>().IsMounted, "a dead rider falls off the horse");
        }

        [UnityTest]
        public IEnumerator Sandbox_squad_patrols_out_of_reach_until_the_player_engages()
        {
            world = PlayerSandbox.Create();
            var squad = PlayerSandbox.AddEnemies();
            var character = world.Player.GetComponent<PlayerCharacter>();
            try
            {
                yield return new WaitForSeconds(2f);

                Assert.AreEqual(4, squad.Count);
                foreach (var enemy in squad)
                    Assert.IsFalse(enemy.IsEngaged, enemy.name + " should not notice the player at the start");
                Assert.AreEqual(PlayerCharacter.StartingHealth, character.Health.Current);
            }
            finally
            {
                foreach (var enemy in squad)
                    if (enemy != null) Object.Destroy(enemy.gameObject);
                foreach (var wall in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (wall.name == "Cover") Object.Destroy(wall.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Trap_course_has_all_five_trap_kinds_and_T_puts_everything_back()
        {
            world = PlayerSandbox.Create();
            TroubleMeter.ResetAll();
            var character = world.Player.GetComponent<PlayerCharacter>();
            var gun = world.Player.GetComponent<PlayerWeaponController>();
            var course = new GameObject("Course").AddComponent<TrapCourse>();
            try
            {
                yield return null;

                var kinds = new System.Collections.Generic.HashSet<TrapKind>();
                foreach (var trap in course.Traps) kinds.Add(trap.Kind);
                Assert.AreEqual(5, kinds.Count, "one of every trap kind");

                // Fall for a fake crate, wake the ambush, and shoot the villager dead.
                SupplyCrate fake = null;
                AmbushBait bait = null;
                DisguisedScammer villager = null;
                foreach (var trap in course.Traps)
                {
                    if (trap is SupplyCrate crate && crate.isFake) fake = crate;
                    if (trap is AmbushBait b) bait = b;
                    if (trap is DisguisedScammer d) villager = d;
                }
                fake.Use(world.Player);
                bait.Spring(world.Player);
                villager.GetComponent<Health>().TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
                yield return null;

                Assert.Less(character.Health.Current, 100f);
                Assert.AreEqual(0, gun.Ammo, "the fake crate robbed the ammo");
                Assert.Greater(TroubleMeter.Current, 0f);
                Assert.IsTrue(villager.IsDefused);
                foreach (var hidden in bait.activateOnSpring)
                {
                    Assert.IsTrue(hidden.activeSelf, "the ambushers woke");
                    Assert.IsNotNull(hidden.GetComponent<Enemy>());
                }

                yield return Tap(keyboard.tKey);

                Assert.AreEqual(100f, character.Health.Current, 0.01f);
                Assert.AreEqual(gun.maxAmmo, gun.Ammo);
                Assert.AreEqual(0f, TroubleMeter.Current);
                Assert.AreEqual(0, TrapLog.SprungKinds.Count);
                Assert.AreEqual(6, course.Traps.Count);
                foreach (var trap in course.Traps)
                {
                    Assert.IsFalse(trap.HasSprung, trap.name + " is armed again");
                    Assert.IsFalse(trap.IsDefused, trap.name + " is back to life");
                    foreach (var hidden in trap.activateOnSpring)
                        Assert.IsFalse(hidden.activeSelf, "ambushers hide until the trap springs");
                }
            }
            finally
            {
                if (course != null) Object.Destroy(course.gameObject);
                TroubleMeter.ResetAll();
                TrapLog.Reset();
            }
        }

        void StandBeside(Vector3 point)
        {
            var walking = world.Player.GetComponent<CharacterController>();
            walking.enabled = false; // a CharacterController snaps back to its old spot otherwise
            world.Player.transform.position = new Vector3(point.x, 0.05f, point.z + 1.5f);
            walking.enabled = true;
            Physics.SyncTransforms();
        }

        IEnumerator HoldE(float seconds)
        {
            Press(keyboard.eKey);
            yield return new WaitForSeconds(seconds);
            Release(keyboard.eKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Mission_ends_only_after_every_item_of_evidence_is_collected_and_Sealed()
        {
            world = PlayerSandbox.Create();
            var evidence = PlayerSandbox.AddEvidence();
            evidence.collectSeconds = 0.2f;
            evidence.sealSeconds = 0.2f;
            bool complete = false;
            System.Action onComplete = () => complete = true;
            GameEvents.MissionComplete += onComplete;
            try
            {
                yield return null;
                Assert.AreEqual(4, evidence.Total, "a case has several items of evidence");

                StandBeside(evidence.sealTable.position);
                yield return HoldE(0.5f);
                Assert.AreEqual(0, evidence.Sealed, "nothing to Seal with empty hands");

                for (int i = 0; i < 3; i++)
                {
                    StandBeside(evidence.items[i].transform.position);
                    yield return HoldE(0.45f);
                    Assert.AreEqual(i + 1, evidence.Carried);
                    Assert.IsTrue(evidence.items[i].Collected);
                    Assert.IsFalse(evidence.items[i].gameObject.activeSelf, "a collected item leaves the floor");
                }

                StandBeside(evidence.sealTable.position);
                yield return HoldE(1f); // one long hold Seals the carried items one after another
                Assert.AreEqual(3, evidence.Sealed);
                Assert.AreEqual(0, evidence.Carried);
                Assert.IsFalse(complete, "one item is still out there");
                Assert.IsFalse(evidence.IsComplete);

                StandBeside(evidence.items[3].transform.position);
                yield return HoldE(0.45f);
                StandBeside(evidence.sealTable.position);
                yield return HoldE(0.5f);

                Assert.AreEqual(4, evidence.Sealed);
                Assert.IsTrue(evidence.IsComplete);
                Assert.IsTrue(complete, "the mission-complete event fires once everything is Sealed");
            }
            finally
            {
                GameEvents.MissionComplete -= onComplete;
                Object.Destroy(evidence.gameObject);
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
