namespace ScammerNoScamming
{
    public enum StatType
    {
        HorseTrotSpeed,
        HorseGallopSpeed,
        HorseTurnRate,
        PlayerMaxHealth,
        EnemySightRange,
        TroubleGain,
        DamageTaken
    }

    public enum ModifierMode
    {
        Multiply,
        Add
    }

    public enum GeneEffect
    {
        None,
        ShowEnemyMarkers
    }

    [System.Serializable]
    public struct StatModifier
    {
        public StatType Stat;
        public ModifierMode Mode;
        public float Value;

        public StatModifier(StatType stat, ModifierMode mode, float value)
        {
            Stat = stat;
            Mode = mode;
            Value = value;
        }

        public static StatModifier Multiply(StatType stat, float value) => new StatModifier(stat, ModifierMode.Multiply, value);
    }
}
