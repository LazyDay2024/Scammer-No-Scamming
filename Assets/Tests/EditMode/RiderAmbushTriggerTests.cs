using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class RiderAmbushTriggerTests
    {
        GameObject scene;

        [TearDown]
        public void TearDown()
        {
            if (scene != null) Object.DestroyImmediate(scene);
            TrapLog.Reset();
        }

        [Test]
        public void VolumeUsesItsRotatedScaledLocalBoxAndAcceptsMountedSeatHeight()
        {
            scene = new GameObject("Ambush fixture");
            scene.transform.SetPositionAndRotation(new Vector3(30f, 12f, -90f), Quaternion.Euler(0f, 62f, 0f));
            scene.transform.localScale = new Vector3(2f, 1f, .5f);
            var trigger = scene.AddComponent<RiderAmbushTrigger>();
            var box = scene.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, 3f, 0f);
            box.size = new Vector3(22f, 12f, 10f);
            Assert.That(trigger.Contains(scene.transform.TransformPoint(new Vector3(10f, 2.5f, 4f))), Is.True);
            Assert.That(trigger.Contains(scene.transform.TransformPoint(new Vector3(12f, 2.5f, 4f))), Is.False);
            Assert.That(trigger.Contains(scene.transform.TransformPoint(new Vector3(0f, 10f, 0f))), Is.False);
        }

        [Test]
        public void RetryClonesFreshHealthAndPreservesTemplateAndWorldPlacement()
        {
            scene = new GameObject("Ambush fixture");
            var source = new GameObject("Immutable template");
            source.transform.SetParent(scene.transform, false);
            source.transform.position = new Vector3(8f, 14f, -120f);
            source.AddComponent<Health>().max = 6f;
            source.SetActive(false);
            var triggerObject = new GameObject("Trigger");
            triggerObject.transform.SetParent(scene.transform, false);
            triggerObject.transform.position = new Vector3(22f, 15f, -135f);
            var trigger = triggerObject.AddComponent<RiderAmbushTrigger>();
            trigger.templateRoot = source.transform;

            trigger.Spring(null);
            var first = trigger.LivePack;
            Assert.That(first.transform.position, Is.EqualTo(source.transform.position));
            Assert.That(first.GetComponent<Health>().Current, Is.EqualTo(6f));
            first.GetComponent<Health>().TakeDamage(new DamageInfo(20f, HitZoneType.Body, Faction.Player, Vector3.zero));
            trigger.ResetForAction();
            Assert.That(trigger.HasSprung, Is.False);
            Assert.That(trigger.LivePack, Is.Null);
            Assert.That(first == null, Is.True);
            Assert.That(source.activeSelf, Is.False);

            trigger.Spring(null);
            Assert.That(trigger.LivePack.GetComponent<Health>().IsDead, Is.False);
            Assert.That(trigger.LivePack.GetComponent<Health>().Current, Is.EqualTo(6f));
            trigger.ResetTrap();
            Assert.That(trigger.LivePack, Is.Null);
            Assert.That(trigger.HasSprung, Is.False);
        }
    }
}
