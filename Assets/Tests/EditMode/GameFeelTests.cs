using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class GameFeelTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void SpringRecoversAfterImpulseAtDifferentFrameRates(int fps)
        {
            var spring = new FeedbackSpring();
            spring.Velocity = new Vector3(1f, -3f, 5f);
            float largest = 0f;
            for (int i = 0; i < fps * 2; i++)
            {
                spring.Step(1f / fps, 18f);
                largest = Mathf.Max(largest, spring.Value.magnitude);
            }
            Assert.That(largest, Is.GreaterThan(0.05f), "impulse must visibly displace the spring");
            Assert.That(spring.Value.magnitude, Is.LessThan(0.0001f));
            Assert.That(spring.Velocity.magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void SpringIntegrationIsIndependentOfStepSizeAndHoldsWhilePaused()
        {
            var single = new FeedbackSpring { Value = Vector3.one, Velocity = Vector3.forward * 2f };
            var split = single;
            single.Step(0.4f, 15f);
            for (int i = 0; i < 40; i++) split.Step(0.01f, 15f);
            Assert.That(Vector3.Distance(single.Value, split.Value), Is.LessThan(0.00001f));
            var before = split;
            split.Step(0f, 15f);
            Assert.That(split.Value, Is.EqualTo(before.Value));
            Assert.That(split.Velocity, Is.EqualTo(before.Velocity));
        }

        [Test]
        public void BobIsPeriodicBoundedAndHasNoForwardDisplacement()
        {
            for (int i = 0; i < 100; i++)
            {
                float phase = i / 100f;
                Vector3 bob = FeedbackMotion.Bob(phase);
                Assert.That(Vector3.Distance(bob, FeedbackMotion.Bob(phase + 1f)), Is.LessThan(0.00001f));
                Assert.That(Mathf.Abs(bob.x), Is.LessThanOrEqualTo(1f));
                Assert.That(Mathf.Abs(bob.y), Is.LessThanOrEqualTo(1f));
                Assert.That(bob.z, Is.Zero);
            }
        }

        [TestCase(FeelSound.Shot, 0.42f)]
        [TestCase(FeelSound.Impact, 0.12f)]
        [TestCase(FeelSound.Hoof, 0.16f)]
        [TestCase(FeelSound.Snort, 0.65f)]
        [TestCase(FeelSound.Footstep, 0.18f)]
        [TestCase(FeelSound.Wind, 4f)]
        public void GeneratedAudioHasExpectedLengthHeadroomAndCleanEndpoints(FeelSound sound, float seconds)
        {
            float[] samples = ProceduralFeelAudio.Generate(sound, 22050);
            Assert.That(samples.Length, Is.EqualTo(Mathf.CeilToInt(seconds * 22050)));
            float energy = 0f;
            foreach (float sample in samples)
            {
                Assert.That(float.IsNaN(sample) || float.IsInfinity(sample), Is.False);
                Assert.That(Mathf.Abs(sample), Is.LessThanOrEqualTo(0.95f));
                energy += sample * sample;
            }
            Assert.That(energy, Is.GreaterThan(0.01f));
            Assert.That(Mathf.Abs(samples[0]), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(0.001f));
        }

        [Test]
        public void HitStopDoesNotAcquirePauseOrRestoreOverScreen()
        {
            var stop = new HitStopClock();
            Assert.That(stop.Begin(10f, 0.05f, 0f, false), Is.False);
            Assert.That(stop.Begin(10f, 0.05f, 1f, true), Is.False);
            Assert.That(stop.Begin(10f, 0.05f, 0.5f, false), Is.True);
            Assert.That(stop.TryRelease(10.02f, 0f, false, out _), Is.False);
            Assert.That(stop.TryRelease(10.06f, 0f, true, out _), Is.False);
            Assert.That(stop.TryRelease(11f, 0f, false, out _), Is.False, "screen took ownership");
            Assert.That(stop.Begin(12f, 0.05f, 0.5f, false), Is.True);
            Assert.That(stop.TryRelease(12.06f, 0f, false, out float scale), Is.True);
            Assert.That(scale, Is.EqualTo(0.5f));
        }

        [Test]
        public void HitStopYieldsWhenAnotherSystemChangesTimeScale()
        {
            var stop = new HitStopClock();
            Assert.That(stop.Begin(0f, 0.05f, 1f, false), Is.True);
            Assert.That(stop.TryRelease(0.02f, 0.25f, false, out _), Is.False);
            Assert.That(stop.TryRelease(1f, 0f, false, out _), Is.False);
        }

        [Test]
        public void AudioClipIsCachedAndHasGeneratedSampleCount()
        {
            AudioClip first = ProceduralFeelAudio.Get(FeelSound.Shot);
            Assert.That(ProceduralFeelAudio.Get(FeelSound.Shot), Is.SameAs(first));
            Assert.That(first.channels, Is.EqualTo(1));
            Assert.That(first.samples, Is.EqualTo(ProceduralFeelAudio.Generate(FeelSound.Shot, first.frequency).Length));
        }
    }
}
