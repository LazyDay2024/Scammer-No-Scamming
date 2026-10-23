using UnityEngine;

namespace ScammerNoScamming
{
    // Someone the player must not shoot. Hurting one adds wrong-target Trouble (once per person, so a
    // stray burst is not punished several times), and gunfire nearby makes them cower, which also makes
    // them a smaller target. They never fight back.
    public abstract class Civilian : Npc
    {
        public float hearRange = 25f;
        // Cowering squashes the body to this share of its height.
        public float cowerHeight = 0.6f;

        bool reported;

        public bool IsCowering { get; private set; }

        protected abstract Faction Side { get; }

        protected override void Awake()
        {
            base.Awake();
            Faction = Side;
            Health.Damaged += OnHurt;
        }

        protected override void OnDestroy()
        {
            if (Health != null) Health.Damaged -= OnHurt;
            base.OnDestroy();
        }

        void OnEnable()
        {
            GameEvents.PlayerFired += OnGunshot;
        }

        void OnDisable()
        {
            GameEvents.PlayerFired -= OnGunshot;
        }

        // Civilians stay where they are; their behaviour is all reaction.
        protected override void Tick() { }

        protected void Cower()
        {
            if (IsCowering) return;
            IsCowering = true;
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(scale.x, scale.y * cowerHeight, scale.z);
        }

        void OnGunshot(Vector3 noise)
        {
            if (!Health.IsDead && (noise - transform.position).sqrMagnitude <= hearRange * hearRange) Cower();
        }

        void OnHurt(Health health, DamageInfo info)
        {
            if (info.SourceFaction == Faction.Player && !reported)
            {
                reported = true;
                TroubleMeter.Add(TroubleSource.WrongTarget, TroubleMeter.WrongTargetCost);
            }
            Cower();
        }
    }
}
