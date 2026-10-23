using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class EnemyTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        GameObject player;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            if (player != null) Object.Destroy(player);
        }

        PlayerCharacter MakePlayer()
        {
            // The floor keeps the player from falling while a test waits.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.AddComponent<RuntimeNavMesh>(); // bakes now, so anything built before this call carves the NavMesh
            spawned.Add(ground);

            player = PlayerSandbox.CreatePlayer();
            return player.GetComponent<PlayerCharacter>();
        }

        // A scammer 15 m down the +z axis, facing the player, standing still with no aiming error.
        ShooterScammer MakeEnemy(float yaw = 180f)
        {
            var enemy = PlayerSandbox.CreateEnemy(new Vector3(0f, 0f, 15f));
            enemy.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            enemy.patrolRadius = 0f;
            enemy.spreadDegrees = 0f;
            enemy.reactionTime = 0.1f;
            spawned.Add(enemy.gameObject);
            Physics.SyncTransforms();
            return enemy;
        }

        [UnityTest]
        public IEnumerator Player_has_100_health_and_the_player_faction()
        {
            var character = MakePlayer();
            yield return null;

            Assert.AreEqual(100f, character.Health.Current, 0.001f);
            Assert.AreEqual(Faction.Player, character.Faction);
        }

        [UnityTest]
        public IEnumerator Enemy_in_sight_shoots_the_player_and_costs_health()
        {
            var character = MakePlayer();
            MakeEnemy();

            float deadline = Time.time + 3f;
            while (character.Health.Current >= 100f && Time.time < deadline) yield return null;

            Assert.Less(character.Health.Current, 100f, "an enemy that sees the player should land a shot within 3 s");
        }

        [UnityTest]
        public IEnumerator Enemy_behind_a_wall_does_not_see_or_shoot()
        {
            var character = MakePlayer();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 2f, 8f);
            wall.transform.localScale = new Vector3(10f, 4f, 0.5f);
            spawned.Add(wall);
            var enemy = MakeEnemy();

            yield return new WaitForSeconds(1.5f);

            Assert.IsFalse(enemy.IsEngaged);
            Assert.AreEqual(100f, character.Health.Current, 0.001f);
        }

        [UnityTest]
        public IEnumerator Enemy_facing_away_notices_a_shot_then_turns_and_fires()
        {
            var character = MakePlayer();
            var enemy = MakeEnemy(yaw: 0f); // looking away from the player
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(enemy.IsEngaged, "it cannot see a player standing behind it");
            Assert.AreEqual(100f, character.Health.Current, 0.001f);

            // A shot that hits nothing still makes noise at the player's position.
            var gun = player.GetComponent<HitscanWeapon>();
            Assert.IsTrue(gun.TryFire(player.transform.position + Vector3.up, Vector3.up, Faction.Player, out _));

            float deadline = Time.time + 3f;
            while (character.Health.Current >= 100f && Time.time < deadline) yield return null;

            Assert.IsTrue(enemy.IsEngaged);
            Assert.Less(character.Health.Current, 100f, "after hearing the shot it should turn, see the player and shoot");
        }

        [UnityTest]
        public IEnumerator Dead_enemy_falls_stops_blocking_shots_and_is_removed()
        {
            MakePlayer();
            var enemy = MakeEnemy();
            enemy.fallDuration = 0.1f;
            enemy.lingerTime = 0.1f;
            enemy.sinkDuration = 0.1f;
            yield return null;

            enemy.Health.TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
            yield return null;

            Assert.IsTrue(enemy.Health.IsDead);
            Assert.IsTrue(enemy.gameObject.activeSelf, "an NPC keeps its body while it plays the death");
            foreach (var body in enemy.GetComponentsInChildren<Collider>())
                Assert.IsFalse(body.enabled, "a body must not soak up shots");

            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(enemy == null, "the body is destroyed after the fall, linger and sink");
        }

        [UnityTest]
        public IEnumerator Enemy_patrols_inside_its_radius()
        {
            MakePlayer();
            var enemy = MakeEnemy();
            enemy.patrolRadius = 3f;
            enemy.patrolPause = 0.1f;
            enemy.walkSpeed = 6f;
            enemy.sightRange = 0f; // keep it from noticing the player so it only patrols
            Vector3 home = enemy.transform.position;

            float farthest = 0f;
            bool moved = false;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                float distance = Vector3.Distance(enemy.transform.position, home);
                farthest = Mathf.Max(farthest, distance);
                moved |= distance > 0.5f;
                yield return null;
            }

            Assert.IsTrue(moved, "it should wander away from its post");
            // The agent coasts a little past its target while braking at 6 m/s, so allow a metre of slack.
            Assert.LessOrEqual(farthest, 4f, "it should stay near its patrol radius");
        }

        [UnityTest]
        public IEnumerator Enemy_patrol_walks_around_a_wall_instead_of_through_it()
        {
            // Built before MakePlayer so the NavMesh bake sees it.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1f, 17f);
            wall.transform.localScale = new Vector3(6f, 2f, 0.5f);
            spawned.Add(wall);
            MakePlayer();
            var enemy = MakeEnemy();
            enemy.patrolRadius = 5f;
            enemy.patrolPause = 0.1f;
            enemy.walkSpeed = 6f;
            enemy.sightRange = 0f;
            var wallBounds = wall.GetComponent<Collider>().bounds;
            Random.InitState(7); // patrol points are random; a fixed seed keeps this test about the wall, not luck

            bool inside = false;
            bool wentBehind = false;
            for (float t = 0f; t < 15f; t += Time.deltaTime)
            {
                Vector3 position = enemy.transform.position;
                inside |= wallBounds.Contains(new Vector3(position.x, 1f, position.z));
                wentBehind |= position.z > 18f;
                yield return null;
            }

            Assert.IsFalse(inside, "it must never stand inside the wall");
            Assert.IsTrue(wentBehind, "it should still reach the far side by walking around");
        }

        [UnityTest]
        public IEnumerator Spawner_drops_a_scammer_ahead_of_the_player_facing_them()
        {
            MakePlayer();
            var spawner = new GameObject("Spawner").AddComponent<EnemySpawner>();
            spawned.Add(spawner.gameObject);
            yield return null;

            var enemy = spawner.Spawn();
            spawned.Add(enemy.gameObject);

            Vector3 toEnemy = enemy.transform.position - player.transform.position;
            Assert.AreEqual(spawner.distance, new Vector2(toEnemy.x, toEnemy.z).magnitude, 0.5f);
            Assert.Greater(Vector3.Dot(enemy.transform.forward, -toEnemy.normalized), 0.95f, "it should face the player");
        }

        [Test]
        public void Damaged_event_reports_each_hit_with_its_amount_and_zone()
        {
            var health = new GameObject("Dummy").AddComponent<Health>();
            spawned.Add(health.gameObject);
            DamageInfo seen = default;
            int count = 0;
            System.Action<Health, DamageInfo> listener = (h, info) => { count++; seen = info; };
            GameEvents.Damaged += listener;
            try
            {
                health.TakeDamage(new DamageInfo(1.5f, HitZoneType.Head, Faction.Scammer, Vector3.zero));
            }
            finally
            {
                GameEvents.Damaged -= listener;
            }

            Assert.AreEqual(1, count);
            Assert.AreEqual(1.5f, seen.Amount, 0.001f);
            Assert.AreEqual(HitZoneType.Head, seen.Zone);
        }

        // A mounted scammer 15 m ahead that stays put: it can neither see nor hear, so shots only test the hit zones.
        RiderScammer MakeStillRider()
        {
            var rider = PlayerSandbox.CreateRider(new Vector3(0f, 0f, 15f));
            rider.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            rider.patrolRadius = 0f;
            rider.sightRange = 0f;
            rider.hearRange = 0f;
            spawned.Add(rider.gameObject);
            Physics.SyncTransforms();
            return rider;
        }

        bool ShootAt(Vector3 point)
        {
            Vector3 origin = player.GetComponentInChildren<Camera>().transform.position;
            return player.GetComponent<HitscanWeapon>().TryFire(origin, (point - origin).normalized, Faction.Player, out _);
        }

        [UnityTest]
        public IEnumerator Shot_at_the_horse_hurts_the_horse_and_not_the_rider()
        {
            MakePlayer();
            var rider = MakeStillRider();
            yield return null;

            Assert.IsTrue(ShootAt(rider.transform.position + new Vector3(0f, 1f, 0f)));

            Assert.AreEqual(rider.horseMaxHealth - 1f * rider.horseDamageMultiplier, rider.HorseHealth.Current, 0.001f);
            Assert.AreEqual(3f, rider.Health.Current, 0.001f, "the rider is untouched");
        }

        [UnityTest]
        public IEnumerator Shot_at_the_rider_hurts_the_rider_and_not_the_horse()
        {
            MakePlayer();
            var rider = MakeStillRider();
            yield return null;

            Assert.IsTrue(ShootAt(rider.transform.position + new Vector3(0f, 2.6f, 0f))); // above the horse's head, which faces the player

            Assert.AreEqual(2f, rider.Health.Current, 0.001f);
            Assert.AreEqual(rider.horseMaxHealth, rider.HorseHealth.Current, 0.001f, "the horse is untouched");
        }

        [UnityTest]
        public IEnumerator Killing_the_horse_throws_the_rider_who_keeps_fighting_on_foot()
        {
            MakePlayer();
            var rider = MakeStillRider();
            rider.fallDuration = 0.1f;
            rider.lingerTime = 0.1f;
            rider.sinkDuration = 0.1f;
            yield return null;
            Transform horse = rider.transform.Find(RiderScammer.HorseName);
            Transform seatedRider = rider.transform.Find(RiderScammer.RiderName);
            Assert.Greater(seatedRider.localPosition.y, 1f, "starts seated above the horse");

            rider.HorseHealth.TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
            yield return null;

            Assert.IsFalse(rider.IsMounted);
            Assert.IsFalse(rider.Health.IsDead, "killing the horse does not kill the rider");
            Assert.AreEqual(0f, seatedRider.localPosition.y, 0.001f, "the rider is on the ground");
            Assert.IsNull(horse.parent, "the horse is left behind");

            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(horse == null, "the dead horse is removed");
        }

        [UnityTest]
        public IEnumerator Rider_gallops_toward_the_player_and_shoots_on_the_way()
        {
            var character = MakePlayer();
            var rider = PlayerSandbox.CreateRider(new Vector3(0f, 0f, 24f));
            rider.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            rider.patrolRadius = 0f;
            rider.spreadDegrees = 0f;
            rider.reactionTime = 0.1f;
            spawned.Add(rider.gameObject);
            Physics.SyncTransforms();

            float startDistance = 24f;
            yield return new WaitForSeconds(3f);

            float distance = Vector3.Distance(rider.transform.position, player.transform.position);
            Assert.Less(distance, startDistance - 5f, "it should have closed in");
            Assert.Less(character.Health.Current, 100f, "and landed a shot");
        }
    }
}
