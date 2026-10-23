using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class PlayerDeathTests : InputTestFixture
    {
        // Counts replays instead of reloading the scene, which would tear down the test.
        public class CountingReplay : ActionReplay
        {
            public int Replays;
            protected override void Replay() { Replays++; }
        }

        Keyboard keyboard;
        GameObject player;
        PlayerSandbox.Result world;
        readonly List<GameObject> spawned = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                Object.Destroy(enemy.gameObject);
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            if (player != null) Object.Destroy(player);
            if (world != null)
            {
                Object.Destroy(world.Player);
                Object.Destroy(world.Horse);
                foreach (var target in world.Targets)
                    if (target != null) Object.Destroy(target.gameObject);
                Object.Destroy(world.Spawner.gameObject);
                Object.Destroy(world.Replay.gameObject);
            }
            // NUnit reuses this fixture instance, so clear what the next test must not see.
            world = null;
            player = null;
            spawned.Clear();
            base.TearDown();
        }

        PlayerCharacter MakePlayer()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.AddComponent<RuntimeNavMesh>();
            spawned.Add(ground);
            player = PlayerSandbox.CreatePlayer();
            return player.GetComponent<PlayerCharacter>();
        }

        static void Kill(PlayerCharacter character)
        {
            character.Health.TakeDamage(new DamageInfo(PlayerCharacter.StartingHealth, HitZoneType.Body, Faction.Scammer, Vector3.zero));
        }

        IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Death_takes_away_control_and_drops_the_view()
        {
            var character = MakePlayer();
            yield return null;

            Kill(character);
            yield return null;

            Assert.IsTrue(character.Health.IsDead);
            Assert.IsTrue(player.activeSelf, "the player stays in the world while dead");
            Assert.IsFalse(player.GetComponent<PlayerController>().enabled);
            Assert.IsFalse(player.GetComponent<PlayerWeaponController>().enabled);
            Assert.IsFalse(player.GetComponent<PlayerMount>().enabled);
            var view = player.GetComponentInChildren<Camera>().transform;
            Assert.Less(view.parent.localPosition.y, 1f, "the view drops to the floor");
        }

        [UnityTest]
        public IEnumerator Dying_on_horseback_throws_the_player_off()
        {
            world = PlayerSandbox.Create();
            world.Replay.delay = 999f; // keep the real replay from reloading the test scene
            var character = world.Player.GetComponent<PlayerCharacter>();
            var horse = world.Horse.GetComponent<HorseController>();
            yield return null;
            yield return Tap(keyboard.eKey);
            Assert.IsTrue(horse.HasRider);

            Kill(character);
            yield return null;

            Assert.IsFalse(world.Player.GetComponent<PlayerController>().IsMounted);
            Assert.IsFalse(horse.HasRider);
            Assert.IsFalse(character.RiderHitboxActive);
            Assert.IsNull(world.Player.transform.parent, "the body is no longer on the seat");
        }

        [UnityTest]
        public IEnumerator Replay_starts_once_after_the_delay()
        {
            var character = MakePlayer();
            var replay = new GameObject("Replay").AddComponent<CountingReplay>();
            replay.delay = 0.5f;
            spawned.Add(replay.gameObject);
            yield return null;
            Assert.AreEqual(-1f, replay.SecondsLeft, "nothing is scheduled while the player lives");
            TroubleMeter.ResetAll();
            TroubleMeter.Add(TroubleSource.WrongAnswer, 10f);
            TroubleMeter.Add(TroubleSource.Gunshot, 4f);

            Kill(character);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(0, replay.Replays, "not before the delay");
            Assert.Greater(replay.SecondsLeft, 0f);

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, replay.Replays);
            Assert.AreEqual(4f, TroubleMeter.Action, 0.001f, "action-phase Trouble is kept through a death");
            Assert.AreEqual(10f, TroubleMeter.Carried, 0.001f, "analysis Trouble is kept");
            TroubleMeter.ResetAll();

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1, replay.Replays, "one death, one replay");
        }

        [UnityTest]
        public IEnumerator Enemies_stop_shooting_once_the_player_is_dead()
        {
            var character = MakePlayer();
            var enemy = PlayerSandbox.CreateEnemy(new Vector3(0f, 0f, 12f));
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            enemy.patrolRadius = 0f;
            enemy.reactionTime = 0.1f;
            Physics.SyncTransforms();

            float deadline = Time.time + 2f;
            while (!enemy.IsEngaged && Time.time < deadline) yield return null;
            Assert.IsTrue(enemy.IsEngaged);

            Kill(character);
            int hitsAfterDeath = 0;
            character.Health.Damaged += (h, info) => hitsAfterDeath++;
            yield return new WaitForSeconds(1.5f);

            Assert.IsFalse(enemy.IsEngaged);
            Assert.AreEqual(0, hitsAfterDeath);
        }
    }
}
