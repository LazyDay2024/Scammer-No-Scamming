using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class ScammerAnimationTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        GameObject player;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
            if (player != null) Object.Destroy(player);
            player = null;
        }

        void MakePlayer()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.AddComponent<RuntimeNavMesh>();
            spawned.Add(ground);
            player = PlayerSandbox.CreatePlayer();
        }

        ShooterScammer MakeScammer(float z)
        {
            var scammer = PlayerSandbox.CreateEnemy(new Vector3(0f, 0f, z));
            scammer.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // facing the player
            scammer.patrolRadius = 0f;
            scammer.shotDamage = 0f; // the player must outlast the whole test
            scammer.reactionTime = 0.1f;
            spawned.Add(scammer.gameObject);
            return scammer;
        }

        static string StateName(Animator animator)
        {
            foreach (var name in new[] { "Idle", "Walk", "Run", "Shoot", "Sit", "Death" })
                if (animator.GetCurrentAnimatorStateInfo(0).IsName(name)) return name;
            return "other";
        }

        [UnityTest]
        public IEnumerator A_scammer_wears_the_bandit_model_and_its_boxes_stay_as_hit_zones_only()
        {
            MakePlayer();
            var scammer = MakeScammer(30f);
            yield return null;

            var animator = scammer.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "the model is attached");
            Assert.AreEqual(9, animator.GetComponentsInChildren<SkinnedMeshRenderer>().Length);
            foreach (var zone in scammer.GetComponentsInChildren<HitZone>())
            {
                Assert.IsFalse(zone.GetComponent<MeshRenderer>().enabled, zone.name + " box is not drawn");
                Assert.IsTrue(zone.GetComponent<Collider>().enabled, zone.name + " still takes hits");
            }
        }

        RiderScammer MakeRider(float z)
        {
            var rider = PlayerSandbox.CreateRider(new Vector3(0f, 0f, z));
            rider.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            rider.patrolRadius = 0f;
            rider.shotDamage = 0f;
            rider.reactionTime = 0.1f;
            spawned.Add(rider.gameObject);
            return rider;
        }

        [UnityTest]
        public IEnumerator A_rider_sits_in_the_saddle_of_a_horse_model_and_the_boxes_stay_as_hit_zones_only()
        {
            MakePlayer();
            var rider = MakeRider(60f);
            rider.sightRange = 0f;
            yield return new WaitForSeconds(0.6f);

            var driver = rider.GetComponent<ScammerAnimation>();
            Assert.IsNotNull(driver, "models are attached");
            Assert.AreEqual(2, rider.GetComponentsInChildren<Animator>().Length, "one for the bandit, one for the horse");
            Assert.AreEqual("Sit", StateName(driver.Animator), "the riding pose");
            Assert.AreEqual(1f, driver.Animator.GetLayerWeight(1), "upper body is driven separately while riding");
            foreach (var zone in rider.GetComponentsInChildren<HitZone>())
            {
                Assert.IsFalse(zone.GetComponent<MeshRenderer>().enabled, zone.name + " box is not drawn");
                Assert.IsTrue(zone.GetComponent<Collider>().enabled, zone.name + " still takes hits");
            }
            Assert.AreEqual("Idle", StateName(driver.HorseAnimator), "a standing horse idles");
        }

        [UnityTest]
        public IEnumerator A_riding_rider_that_sees_the_player_aims_while_riding_and_the_horse_walks_faster()
        {
            MakePlayer();
            var rider = MakeRider(22f);
            var driver = rider.GetComponent<ScammerAnimation>();

            bool aimed = false;
            bool horseWalked = false;
            float fastestClip = 0f;
            for (float t = 0f; t < 5f; t += Time.deltaTime)
            {
                aimed |= driver.Animator.GetBool(ScammerAnimation.ShootingParameter) && StateName(driver.Animator) == "Sit";
                horseWalked |= StateName(driver.HorseAnimator) == "Walk";
                fastestClip = Mathf.Max(fastestClip, driver.HorseAnimator.speed);
                yield return null;
            }

            Assert.IsTrue(aimed, "the rider aims with its upper body without leaving the saddle pose");
            Assert.IsTrue(horseWalked, "the horse plays its walk while it moves");
            Assert.Greater(fastestClip, 2f, "a gallop plays the walk several times faster");
        }

        [UnityTest]
        public IEnumerator A_rider_shot_dead_in_the_saddle_falls_beside_the_horse_which_stays_standing()
        {
            MakePlayer();
            var rider = MakeRider(60f);
            rider.sightRange = 0f;
            var driver = rider.GetComponent<ScammerAnimation>();
            var horse = rider.transform.Find(RiderScammer.HorseName);
            yield return new WaitForSeconds(0.4f);

            rider.Health.TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual("Death", StateName(driver.Animator));
            Assert.IsFalse(rider.IsMounted);
            Assert.Greater(horse.up.y, 0.99f, "the horse is not tipped over with its rider");
            Assert.IsTrue(driver.HorseAnimator.enabled);
            Vector3 beside = rider.transform.Find(RiderScammer.RiderName).localPosition;
            Assert.AreEqual(0f, beside.y, 0.001f, "the body is on the ground, not in the air above the saddle");
            Assert.Greater(Mathf.Abs(beside.x), 0.8f, "and clear of the horse");
        }

        [UnityTest]
        public IEnumerator A_rider_whose_horse_goes_down_stands_up_on_foot_and_the_horse_lies_on_its_side()
        {
            MakePlayer();
            var rider = MakeRider(22f);
            rider.fallDuration = 0.1f;
            var driver = rider.GetComponent<ScammerAnimation>();
            var horse = rider.transform.Find(RiderScammer.HorseName);
            var seat = rider.transform.Find(RiderScammer.RiderName);
            yield return new WaitForSeconds(0.5f);

            rider.HorseHealth.TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
            yield return new WaitForSeconds(0.6f);

            Assert.IsFalse(rider.IsMounted);
            Assert.AreNotEqual("Sit", StateName(driver.Animator), "no longer the riding pose");
            Assert.AreEqual(0f, driver.Animator.GetLayerWeight(1), "the whole body is driven by the base layer on foot");
            Assert.IsFalse(driver.HorseAnimator.enabled);
            Assert.Less(Mathf.Abs(horse.up.y), 0.2f, "the horse lies on its side");
            Assert.Greater(horse.position.y, 0.3f, "with its flank on the floor, not sunk into it");
            Assert.AreEqual(0.7f, seat.Find("Body").localPosition.y, 0.01f, "the rider's body box is back to standing height");
        }

        [UnityTest]
        public IEnumerator A_scammer_that_has_noticed_nothing_stands_in_Idle()
        {
            MakePlayer();
            var scammer = MakeScammer(60f);
            scammer.sightRange = 0f;
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual("Idle", StateName(scammer.GetComponentInChildren<Animator>()));
        }

        [UnityTest]
        public IEnumerator Patrolling_plays_Walk()
        {
            MakePlayer();
            var scammer = MakeScammer(60f);
            scammer.patrolRadius = 8f;
            scammer.patrolPause = 0.1f;
            scammer.sightRange = 0f;
            var animator = scammer.GetComponentInChildren<Animator>();
            Random.InitState(3);

            bool walked = false;
            for (float t = 0f; t < 10f && !walked; t += Time.deltaTime)
            {
                walked = StateName(animator) == "Walk";
                yield return null;
            }

            Assert.IsTrue(walked, "a patrolling scammer plays Walk");
        }

        [UnityTest]
        public IEnumerator A_scammer_that_sees_the_player_runs_at_them_then_stands_and_shoots()
        {
            MakePlayer();
            var scammer = MakeScammer(22f); // inside the 25 m sight range
            var animator = scammer.GetComponentInChildren<Animator>();

            bool ran = false;
            bool shot = false;
            for (float t = 0f; t < 12f && !shot; t += Time.deltaTime)
            {
                string state = StateName(animator);
                ran |= state == "Run";
                shot |= ran && state == "Shoot";
                yield return null;
            }

            Assert.IsTrue(ran, "it runs while closing in");
            Assert.IsTrue(shot, "and settles into the firing stance once it stops at keepDistance");
        }

        [UnityTest]
        public IEnumerator A_dead_scammer_plays_the_death_clip_lying_down_in_place_then_sinks_away()
        {
            MakePlayer();
            var scammer = MakeScammer(30f);
            scammer.lingerTime = 0.3f;
            scammer.sinkDuration = 0.3f;
            var animator = scammer.GetComponentInChildren<Animator>();
            var driver = scammer.GetComponent<ScammerAnimation>();
            yield return null;
            Assert.Greater(driver.DeathSeconds, 2f, "the controller has the death clip");

            scammer.GetComponent<Health>().TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
            yield return new WaitForSeconds(1f);

            Assert.AreEqual("Death", StateName(animator));
            Assert.IsTrue(animator.enabled);
            Assert.Greater(scammer.transform.up.y, 0.99f, "upright: the clip lays the body down, it is not tipped over as well");
            Assert.IsFalse(scammer.GetComponentInChildren<Collider>().enabled, "a body takes no more shots");

            yield return new WaitForSeconds(driver.DeathSeconds + 1.2f);
            Assert.IsTrue(scammer == null, "removed after it has lain there");
        }

        [UnityTest]
        public IEnumerator Shooting_never_pulls_a_dead_scammer_back_up()
        {
            MakePlayer();
            var scammer = MakeScammer(22f); // sees the player at once, so it is in the firing stance when shot
            scammer.lingerTime = 5f;
            var animator = scammer.GetComponentInChildren<Animator>();
            yield return new WaitForSeconds(1f);

            scammer.GetComponent<Health>().TakeDamage(new DamageInfo(99f, HitZoneType.Body, Faction.Player, Vector3.zero));
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                yield return null;
                if (t > 0.3f) Assert.AreEqual("Death", StateName(animator));
            }
        }
    }
}
