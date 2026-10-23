using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    [CreateAssetMenu(menuName = "Scammer No Scamming/Gene Catalog")]
    public class GeneCatalog : ScriptableObject
    {
        public List<GeneDefinition> Genes = new List<GeneDefinition>();

        public GeneDefinition Find(string geneId) => Genes.Find(g => g != null && g.GeneId == geneId);

        // The four genes from the concept. Numbers are placeholders until playtesting; once a catalog
        // asset exists, tune it there instead of here.
        public static GeneCatalog CreateDefault()
        {
            var catalog = CreateInstance<GeneCatalog>();
            catalog.name = "DefaultGeneCatalog";
            catalog.Genes.Add(GeneDefinition.Create("fast", "เร็ว", "ควบเร็วขึ้น แต่เลี้ยวยาก", GeneEffect.None,
                StatModifier.Multiply(StatType.HorseGallopSpeed, 1.25f),
                StatModifier.Multiply(StatType.HorseTrotSpeed, 1.15f),
                StatModifier.Multiply(StatType.HorseTurnRate, 0.75f)));
            catalog.Genes.Add(GeneDefinition.Create("armored", "ทนกระสุน", "เลือดเพิ่ม แต่ช้าลง", GeneEffect.None,
                StatModifier.Multiply(StatType.PlayerMaxHealth, 1.5f),
                StatModifier.Multiply(StatType.HorseGallopSpeed, 0.85f),
                StatModifier.Multiply(StatType.HorseTrotSpeed, 0.85f)));
            catalog.Genes.Add(GeneDefinition.Create("tracker", "ดมตามรอย", "เห็นศัตรูจากไกล แต่ Trouble ขึ้นเร็ว", GeneEffect.ShowEnemyMarkers,
                StatModifier.Multiply(StatType.TroubleGain, 1.5f)));
            catalog.Genes.Add(GeneDefinition.Create("stealth", "ซ่อนตัว", "ศัตรูเห็นยาก แต่ม้าเปราะ", GeneEffect.None,
                StatModifier.Multiply(StatType.EnemySightRange, 0.5f),
                StatModifier.Multiply(StatType.DamageTaken, 1.5f)));
            return catalog;
        }
    }
}
