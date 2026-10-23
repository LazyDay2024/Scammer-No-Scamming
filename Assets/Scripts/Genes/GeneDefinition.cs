using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // A gene is just a list of stat modifiers: its upside and downside live in the same list.
    [CreateAssetMenu(menuName = "Scammer No Scamming/Gene")]
    public class GeneDefinition : ScriptableObject
    {
        public string GeneId;
        public string DisplayName;
        [TextArea] public string Description;
        public List<StatModifier> Modifiers = new List<StatModifier>();
        public GeneEffect Special;

        public static GeneDefinition Create(string id, string displayName, string description, GeneEffect special,
            params StatModifier[] modifiers)
        {
            var gene = CreateInstance<GeneDefinition>();
            gene.name = id;
            gene.GeneId = id;
            gene.DisplayName = displayName;
            gene.Description = description;
            gene.Special = special;
            gene.Modifiers.AddRange(modifiers);
            return gene;
        }
    }
}
