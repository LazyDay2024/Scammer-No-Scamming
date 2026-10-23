using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class TrapTests : InputTestFixture
    {
        Keyboard keyboard;
        Mouse mouse;
        GameObject player;
        PlayerWeaponController gun;
        readonly List<GameObject> spawned = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                Object.Destroy(enemy.gameObject);
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            if (player != null) Object.Destroy(player);
            base.TearDown();
        }

        void MakePlayer()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.AddComponent<RuntimeNavMesh>();
            spawned.Add(ground);

            player = PlayerSandbox.CreatePlayer();
            gun = player.GetComponent<PlayerWeaponController>();
            gun.requireLockedCursor = false;
        }

        GameObject Box(string name, Vector3 position)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            spawned.Add(box);
            return box;
        }

        AmbushGroup AddAmbush(GameObject owner, AmbushKind kind, int count, float min, float max)
        {
            var group = owner.AddComponent<AmbushGroup>();
            group.kind = kind;
            group.count = count;
            group.minRadius = min;
            group.maxRadius = max;
            return group;
        }

        IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Real_supply_box_refills_ammo()
        {
            MakePlayer();
            Box("Supplies", new Vector3(0f, 0.5f, 1.5f)).AddComponent<SupplyBox>();
            gun.Drain();
            yield return null;

            yield return Tap(keyboard.eKey);

            Assert.AreEqual(gun.maxAmmo, gun.Ammo);
        }

        [UnityTest]
        public IEnumerator Fake_supply_box_empties_the_gun_and_sends_scammers_once()
        {
            MakePlayer();
            var box = Box("Free supplies", new Vector3(0f, 0.5f, 1.5f));
            AddAmbush(box, AmbushKind.OnFoot, 2, 8f, 12f);
            var trap = box.AddComponent<FakeSupplyBox>();
            yield return null;
            Assert.AreEqual(gun.maxAmmo, gun.Ammo);

            yield return Tap(keyboard.eKey);
            yield return null;

            Assert.IsTrue(trap.IsSprung);
            Assert.AreEqual(0, gun.Ammo, "the box takes every round");
            var scammers = Object.FindObjectsByType<ShooterScammer>(FindObjectsSortMode.None);
            Assert.AreEqual(2, scammers.Length);
            foreach (var scammer in scammers)
                Assert.IsTrue(scammer.IsEngaged, "they come straight for the player");

            yield return Tap(keyboard.eKey);
            Assert.AreEqual(2, Object.FindObjectsByType<ShooterScammer>(FindObjectsSortMode.None).Length, "a trap springs only once");
        }

        [UnityTest]
        public IEnumerator Link_trap_calls_riders_in_from_a_ring_around_the_player()
        {
            MakePlayer();
            var terminal = Box("Terminal", new Vector3(0f, 0.5f, 1.5f));
            AddAmbush(terminal, AmbushKind.Riders, 3, 30f, 40f);
            terminal.AddComponent<LinkTrap>();
            yield return null;

            yield return Tap(keyboard.eKey);
            yield return null;

            var riders = Object.FindObjectsByType<RiderScammer>(FindObjectsSortMode.None);
            Assert.AreEqual(3, riders.Length);
            foreach (var rider in riders)
            {
                float distance = Vector3.Distance(rider.transform.position, player.transform.position);
                Assert.That(distance, Is.InRange(25f, 45f));
                Assert.IsTrue(rider.IsEngaged);
            }
        }

        [UnityTest]
        public IEnumerator Ambush_stays_hidden_until_the_player_comes_within_its_radius()
        {
            MakePlayer();
            var spot = Box("Ambush", new Vector3(0f, 0.5f, 30f));
            var hiding = new Transform[2];
            for (int i = 0; i < hiding.Length; i++)
            {
                hiding[i] = new GameObject("Hiding spot " + i).transform;
                hiding[i].SetParent(spot.transform, false);
                hiding[i].localPosition = new Vector3(i * 4f - 2f, -0.5f, 0f);
                spawned.Add(hiding[i].gameObject);
            }
            AddAmbush(spot, AmbushKind.OnFoot, 2, 0f, 0f).points = hiding;
            var trap = spot.AddComponent<ProximityTrap>();
            trap.triggers = TriggerKind.Enter;
            trap.enterRadius = 10f;
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(trap.IsSprung);
            Assert.AreEqual(0, Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length);

            spot.transform.position = new Vector3(0f, 0.5f, 5f); // the player is now inside the radius
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(trap.IsSprung);
            var enemies = Object.FindObjectsByType<ShooterScammer>(FindObjectsSortMode.None);
            Assert.AreEqual(2, enemies.Length);
            foreach (var enemy in enemies)
            {
                float nearest = Mathf.Min(Vector3.Distance(enemy.transform.position, hiding[0].position), Vector3.Distance(enemy.transform.position, hiding[1].position));
                Assert.Less(nearest, 3f, "they appear where they were hiding");
            }
        }

        [UnityTest]
        public IEnumerator Staring_at_a_look_trap_springs_it_but_looking_away_does_not()
        {
            MakePlayer();
            var behind = Box("Behind", new Vector3(0f, 0.5f, -10f)); // the player faces +z
            var ahead = Box("Ahead", new Vector3(0f, 0.5f, 10f));
            var quiet = behind.AddComponent<ProximityTrap>();
            var loud = ahead.AddComponent<ProximityTrap>();
            quiet.triggers = loud.triggers = TriggerKind.Look;
            quiet.lookRange = loud.lookRange = 15f;
            yield return new WaitForSeconds(0.4f);

            Assert.IsFalse(quiet.IsSprung);
            Assert.IsTrue(loud.IsSprung);
        }

        [UnityTest]
        public IEnumerator Call_center_decoys_vanish_and_armed_scammers_appear_where_they_stood()
        {
            MakePlayer();
            var building = Box("Call center", new Vector3(0f, 0.5f, 14f));
            var decoys = new Transform[2];
            for (int i = 0; i < decoys.Length; i++)
                decoys[i] = Box("Decoy " + i, new Vector3(i * 6f - 3f, 0.9f, 12f)).transform;
            AddAmbush(building, AmbushKind.OnFoot, 2, 0f, 0f).points = decoys;
            var trap = building.AddComponent<CallCenterTrap>();
            trap.triggers = TriggerKind.Enter;
            trap.enterRadius = 15f; // the player starts inside it
            trap.decoys = decoys;
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(trap.IsSprung);
            foreach (var decoy in decoys)
                Assert.IsFalse(decoy.gameObject.activeSelf, "the act is over");
            var enemies = Object.FindObjectsByType<ShooterScammer>(FindObjectsSortMode.None);
            Assert.AreEqual(2, enemies.Length);
        }

        DisguisedScammer MakeDisguised(Vector3 position)
        {
            var scammer = PlayerSandbox.CreateDisguised(position);
            scammer.patrolRadius = 0f;
            scammer.spreadDegrees = 0f;
            scammer.reactionTime = 0.1f;
            Physics.SyncTransforms();
            return scammer;
        }

        [UnityTest]
        public IEnumerator Eye_contact_turns_a_passer_by_hostile_and_the_first_hit_is_three_times_normal()
        {
            MakePlayer();
            var scammer = MakeDisguised(new Vector3(0f, 0f, 6f)); // straight ahead, within noticing range
            var character = player.GetComponent<PlayerCharacter>();
            float firstHit = 0f;
            character.Health.Damaged += (h, info) => { if (firstHit == 0f) firstHit = info.Amount; };

            float deadline = Time.time + 4f;
            while (firstHit == 0f && Time.time < deadline) yield return null;

            Assert.IsTrue(scammer.IsRevealed);
            Assert.AreEqual(scammer.shotDamage * scammer.strongDamageMultiplier, firstHit, 0.001f);
        }

        [UnityTest]
        public IEnumerator A_passer_by_behind_the_player_never_drops_the_disguise()
        {
            MakePlayer();
            var scammer = MakeDisguised(new Vector3(0f, 0f, -6f));
            var character = player.GetComponent<PlayerCharacter>();

            yield return new WaitForSeconds(1.5f);

            Assert.IsFalse(scammer.IsRevealed);
            Assert.AreEqual(PlayerCharacter.StartingHealth, character.Health.Current, 0.001f);
        }

        [UnityTest]
        public IEnumerator Click_spends_one_round_and_an_empty_gun_does_not_fire()
        {
            MakePlayer();
            int heard = 0;
            gun.Weapon.Fired += shot => heard++;
            yield return null;

            // The input test harness sometimes drops a click, so allow a few clicks.
            for (int attempt = 0; attempt < 4 && heard == 0; attempt++)
            {
                yield return Tap(mouse.leftButton);
                yield return new WaitForSeconds(0.25f);
            }
            Assert.AreEqual(1, heard);
            Assert.AreEqual(gun.maxAmmo - 1, gun.Ammo);

            gun.Drain();
            for (int attempt = 0; attempt < 3; attempt++)
            {
                yield return Tap(mouse.leftButton);
                yield return new WaitForSeconds(0.25f);
            }
            Assert.AreEqual(1, heard, "no rounds, no shot");
            Assert.AreEqual(0, gun.Ammo);
        }
    }
}
