namespace ScammerNoScamming
{
    // Multipliers for one case attempt, built from the chosen genes. Each system reads the field it owns:
    // horse speeds/turn (HorseGenes), PlayerMaxHealth and DamageTaken (player), EnemySightRange (enemies),
    // TroubleGain (Trouble), ShowEnemyMarkers (HUD). Defaults mean "no genes".
    public class RunModifiers
    {
        public float HorseTrotSpeed = 1f;
        public float HorseGallopSpeed = 1f;
        public float HorseTurnRate = 1f;
        public float PlayerMaxHealth = 1f;
        public float EnemySightRange = 1f;
        public float TroubleGain = 1f;
        public float DamageTaken = 1f;
        public bool ShowEnemyMarkers;

        public static RunModifiers None => new RunModifiers();

        public void Apply(StatModifier modifier)
        {
            float current = Get(modifier.Stat);
            float next = modifier.Mode == ModifierMode.Multiply ? current * modifier.Value : current + modifier.Value;
            Set(modifier.Stat, next);
        }

        public void Apply(GeneEffect effect)
        {
            if (effect == GeneEffect.ShowEnemyMarkers) ShowEnemyMarkers = true;
        }

        public float Get(StatType stat)
        {
            switch (stat)
            {
                case StatType.HorseTrotSpeed: return HorseTrotSpeed;
                case StatType.HorseGallopSpeed: return HorseGallopSpeed;
                case StatType.HorseTurnRate: return HorseTurnRate;
                case StatType.PlayerMaxHealth: return PlayerMaxHealth;
                case StatType.EnemySightRange: return EnemySightRange;
                case StatType.TroubleGain: return TroubleGain;
                default: return DamageTaken;
            }
        }

        void Set(StatType stat, float value)
        {
            switch (stat)
            {
                case StatType.HorseTrotSpeed: HorseTrotSpeed = value; break;
                case StatType.HorseGallopSpeed: HorseGallopSpeed = value; break;
                case StatType.HorseTurnRate: HorseTurnRate = value; break;
                case StatType.PlayerMaxHealth: PlayerMaxHealth = value; break;
                case StatType.EnemySightRange: EnemySightRange = value; break;
                case StatType.TroubleGain: TroubleGain = value; break;
                default: DamageTaken = value; break;
            }
        }
    }
}
