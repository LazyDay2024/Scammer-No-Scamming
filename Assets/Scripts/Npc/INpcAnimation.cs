namespace ScammerNoScamming
{
    public interface INpcAnimation
    {
        void SetSpeed(float metersPerSecond);
        void Aim(bool aiming);
        void Shoot();
        void Die();
        void HandsUp(bool up);
        void Cower(bool cower);
        void Hit();
    }
}
