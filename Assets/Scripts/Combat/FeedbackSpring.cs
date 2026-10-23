using UnityEngine;

namespace ScammerNoScamming
{
    // Exact critically damped solution: stable at low frame rates, with no overshoot/drift.
    public struct FeedbackSpring
    {
        public Vector3 Value;
        public Vector3 Velocity;

        public void Step(float deltaTime, float frequency)
        {
            float dt = System.Math.Max(0f, deltaTime);
            float omega = System.Math.Max(0.01f, frequency);
            float decay = (float)System.Math.Exp(-omega * dt);
            Vector3 j = Velocity + Value * omega;
            Value = (Value + j * dt) * decay;
            Velocity = (Velocity - j * (omega * dt)) * decay;
        }
    }

    public static class FeedbackMotion
    {
        // Phase measured in strides. A vertical beat on each of the two footfalls.
        public static Vector3 Bob(float phase)
        {
            double angle = phase * System.Math.PI * 2;
            return new Vector3((float)System.Math.Sin(angle), (float)-System.Math.Cos(angle * 2), 0f);
        }
    }
}
