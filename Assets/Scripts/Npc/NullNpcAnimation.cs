namespace ScammerNoScamming
{
    // Keeps gameplay independent of the character art and its Animator controller.
    public sealed class NullNpcAnimation : INpcAnimation
    {
        public static readonly NullNpcAnimation Instance = new NullNpcAnimation();

        public void SetSpeed(float metersPerSecond) { }
        public void Aim(bool aiming) { }
        public void Shoot() { }
        public void Die() { }
        public void HandsUp(bool up) { }
        public void Cower(bool cower) { }
        public void Hit() { }
    }
}
