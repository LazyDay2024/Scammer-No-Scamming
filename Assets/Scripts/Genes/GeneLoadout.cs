using System.Collections.Generic;

namespace ScammerNoScamming
{
    // Decided rules: FreeSlots genes are free, up to MaxSlots are allowed, and every gene past
    // FreeSlots adds Trouble. The Trouble per extra gene is not decided yet, so it is a parameter.
    public class GeneLoadout
    {
        public const int DefaultFreeSlots = 2;
        public const int DefaultMaxSlots = 4;

        public readonly int FreeSlots;
        public readonly int MaxSlots;
        public readonly float TroublePerExtraGene;

        readonly List<GeneDefinition> selected = new List<GeneDefinition>();

        public GeneLoadout(int freeSlots = DefaultFreeSlots, int maxSlots = DefaultMaxSlots, float troublePerExtraGene = 20f)
        {
            FreeSlots = freeSlots;
            MaxSlots = maxSlots;
            TroublePerExtraGene = troublePerExtraGene;
        }

        public IReadOnlyList<GeneDefinition> Selected => selected;
        public int Count => selected.Count;
        public int ExtraCount => System.Math.Max(0, selected.Count - FreeSlots);
        public bool IsOverBudget => ExtraCount > 0;
        public bool IsFull => selected.Count >= MaxSlots;
        public float OverBudgetTrouble => ExtraCount * TroublePerExtraGene;

        public bool Contains(GeneDefinition gene) => selected.Contains(gene);

        public bool TryAdd(GeneDefinition gene)
        {
            if (gene == null || IsFull || selected.Contains(gene)) return false;
            selected.Add(gene);
            return true;
        }

        public bool Remove(GeneDefinition gene) => selected.Remove(gene);

        // Adds the gene if it is not chosen, removes it if it is. Returns whether it is chosen afterwards.
        public bool Toggle(GeneDefinition gene)
        {
            if (Remove(gene)) return false;
            return TryAdd(gene);
        }

        public void Clear() => selected.Clear();

        public RunModifiers BuildModifiers()
        {
            var modifiers = new RunModifiers();
            foreach (var gene in selected)
            {
                foreach (var modifier in gene.Modifiers)
                    modifiers.Apply(modifier);
                modifiers.Apply(gene.Special);
            }
            return modifiers;
        }
    }
}
