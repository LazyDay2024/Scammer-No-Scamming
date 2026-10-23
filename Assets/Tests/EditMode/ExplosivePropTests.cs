using NUnit.Framework;

namespace ScammerNoScamming.Tests
{
    public class ExplosivePropTests
    {
        [TestCase(0f, 6f)]
        [TestCase(2f, 4f)]
        [TestCase(3f, 3f)]
        [TestCase(6f, 0f)]
        [TestCase(9f, 0f)]
        [TestCase(-1f, 6f)]
        public void BlastFallsOffLinearlyAndStopsAtRadius(float distance, float expected)
        {
            Assert.That(ExplosiveProp.DamageAtDistance(distance, 6f, 6f), Is.EqualTo(expected).Within(.0001f));
        }

        [TestCase(1f, 0f, 6f)]
        [TestCase(1f, -2f, 6f)]
        [TestCase(1f, 6f, -1f)]
        [TestCase(float.NaN, 6f, 6f)]
        [TestCase(float.PositiveInfinity, 6f, 6f)]
        [TestCase(1f, float.PositiveInfinity, 6f)]
        [TestCase(1f, 6f, float.NaN)]
        public void InvalidBlastParametersDoNoDamage(float distance, float radius, float maximum)
        {
            Assert.That(ExplosiveProp.DamageAtDistance(distance, radius, maximum), Is.Zero);
        }
    }
}
