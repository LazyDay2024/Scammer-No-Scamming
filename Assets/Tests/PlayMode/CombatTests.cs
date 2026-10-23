using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class CombatTests : InputTestFixture
    {
        Mouse mouse;
        GameObject player;
        readonly List<GameObject> spawned = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            if (player != null) Object.Destroy(player);
            base.TearDown();
        }

        GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        // A health root with one box collider that carries a HitZone, standing in front of the player.
        Health MakeTarget(Vector3 position, HitZoneType type, float multiplier, float hp)
        {
            var root = NewObject("Target");
            root.transform.position = position;
            var health = root.AddComponent<Health>();
            health.SetMax(hp);

            var zoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zoneObject.transform.SetParent(root.transform, false);
            zoneObject.transform.localScale = new Vector3(1f, 1f, 1f);
            zoneObject.AddComponent<HitZone>().Configure(type, multiplier, health);
            Physics.SyncTransforms(); // the collider was created at the origin and then moved with its parent
            return health;
        }

        PlayerWeaponController MakePlayer()
        {
            // Without a floor the player falls while a test waits, and the camera drops below the target.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            spawned.Add(ground);

            player = PlayerSandbox.CreatePlayer();
            var weapon = player.GetComponent<PlayerWeaponController>();
            weapon.requireLockedCursor = false;
            return weapon;
        }

        // Fires the weapon straight down the camera axis, bypassing the mouse.
        bool FireDirect(PlayerWeaponController controller)
        {
            Transform cam = player.GetComponentInChildren<Camera>().transform;
            return controller.Weapon.TryFire(cam.position, cam.forward, Faction.Player, out _);
        }

        // Queue-only keeps the click on the normal frame path, so Fire.WasPressedThisFrame sees it in the next Update.
        IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }

        [Test]
        public void Health_TakeDamage_reduces_current_and_raises_events_once()
        {
            var health = NewObject("Dummy").AddComponent<Health>();
            int damaged = 0;
            int died = 0;
            health.Damaged += (h, info) => damaged++;
            health.Died += h => died++;

            health.TakeDamage(new DamageInfo(2f, HitZoneType.Body, Faction.Player, Vector3.zero));
            Assert.AreEqual(1f, health.Current, 0.001f);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(1, damaged);

            health.TakeDamage(new DamageInfo(2f, HitZoneType.Body, Faction.Player, Vector3.zero));
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, died);

            health.TakeDamage(new DamageInfo(2f, HitZoneType.Body, Faction.Player, Vector3.zero));
            Assert.AreEqual(2, damaged, "no damage events after death");
            Assert.AreEqual(1, died);
        }

        [Test]
        public void HitZone_scales_damage_and_reports_its_zone()
        {
            var health = NewObject("Dummy").AddComponent<Health>();
            health.SetMax(10f);
            var zone = NewObject("Head").AddComponent<HitZone>();
            zone.Configure(HitZoneType.Head, 2f, health);
            DamageInfo seen = default;
            health.Damaged += (h, info) => seen = info;

            zone.TakeDamage(new DamageInfo(1f, HitZoneType.Body, Faction.Player, Vector3.zero));

            Assert.AreEqual(8f, health.Current, 0.001f);
            Assert.AreEqual(HitZoneType.Head, seen.Zone);
            Assert.AreEqual(2f, seen.Amount, 0.001f);
        }

        [UnityTest]
        public IEnumerator Shot_uses_the_zone_multiplier_of_the_collider_that_was_hit()
        {
            var controller = MakePlayer();
            var head = MakeTarget(new Vector3(0f, 1.6f, 10f), HitZoneType.Head, 2f, 10f);
            var feet = MakeTarget(new Vector3(0f, 1.6f, 14f), HitZoneType.Feet, 0.5f, 10f);
            yield return null;

            Assert.IsTrue(FireDirect(controller));
            Assert.AreEqual(8f, head.Current, 0.001f, "head zone doubles the 1 point shot");

            head.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(FireDirect(controller), "0.25s is longer than the 0.2s fire interval");
            Assert.AreEqual(9.5f, feet.Current, 0.001f, "feet zone halves the 1 point shot");
        }

        [UnityTest]
        public IEnumerator Weapon_fire_interval_blocks_a_second_shot_inside_the_cooldown()
        {
            var controller = MakePlayer();
            var target = MakeTarget(new Vector3(0f, 1.6f, 10f), HitZoneType.Body, 1f, 10f);
            yield return null;

            Assert.IsTrue(FireDirect(controller));
            Assert.IsFalse(FireDirect(controller), "second shot in the same instant is inside the 0.2s interval");
            Assert.AreEqual(9f, target.Current, 0.001f);

            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(FireDirect(controller), "the weapon fires again once the interval has passed");
            Assert.AreEqual(8f, target.Current, 0.001f);
        }

        [UnityTest]
        public IEnumerator Click_reaches_the_weapon_and_reports_noise_position()
        {
            var controller = MakePlayer();
            var target = MakeTarget(new Vector3(0f, 1.6f, 10f), HitZoneType.Body, 1f, 10f);
            Vector3 heardAt = Vector3.one * 999f;
            int heard = 0;
            controller.Fired += position => { heard++; heardAt = position; };
            yield return null;

            // The input test harness sometimes drops a click, so allow a few clicks before failing.
            for (int attempt = 0; attempt < 4 && heard == 0; attempt++)
            {
                yield return Tap(mouse.leftButton);
                yield return new WaitForSeconds(0.25f);
            }

            Assert.AreEqual(1, heard, "a click should fire exactly one shot");
            Assert.AreEqual(player.transform.position.x, heardAt.x, 0.01f);
            Assert.AreEqual(player.transform.position.z, heardAt.z, 0.01f);
            Assert.AreEqual(9f, target.Current, 0.001f, "the shot should hit the target in front");
        }
    }
}
