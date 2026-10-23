using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class GeneTests
    {
        GeneCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = GeneCatalog.CreateDefault();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var gene in catalog.Genes) Object.DestroyImmediate(gene);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void DefaultCatalogHasFourGenesEachWithADownside()
        {
            Assert.AreEqual(4, catalog.Genes.Count);
            foreach (var gene in catalog.Genes)
            {
                var loadout = new GeneLoadout();
                loadout.TryAdd(gene);
                var m = loadout.BuildModifiers();
                bool hasDownside = m.HorseTrotSpeed < 1f || m.HorseGallopSpeed < 1f || m.HorseTurnRate < 1f
                    || m.PlayerMaxHealth < 1f || m.TroubleGain > 1f || m.DamageTaken > 1f || m.EnemySightRange > 1f;
                bool hasUpside = m.HorseTrotSpeed > 1f || m.HorseGallopSpeed > 1f || m.PlayerMaxHealth > 1f
                    || m.EnemySightRange < 1f || m.ShowEnemyMarkers;
                Assert.IsTrue(hasDownside, gene.GeneId + " has no downside");
                Assert.IsTrue(hasUpside, gene.GeneId + " has no upside");
            }
        }

        [Test]
        public void TwoGenesAreFreeAndExtraGenesAddTrouble()
        {
            var loadout = new GeneLoadout(2, 4, 20f);
            loadout.TryAdd(catalog.Find("fast"));
            loadout.TryAdd(catalog.Find("armored"));
            Assert.IsFalse(loadout.IsOverBudget);
            Assert.AreEqual(0f, loadout.OverBudgetTrouble);

            loadout.TryAdd(catalog.Find("tracker"));
            loadout.TryAdd(catalog.Find("stealth"));
            Assert.AreEqual(2, loadout.ExtraCount);
            Assert.AreEqual(40f, loadout.OverBudgetTrouble);
        }

        [Test]
        public void CannotExceedMaxSlotsOrPickTheSameGeneTwice()
        {
            var loadout = new GeneLoadout(2, 3);
            Assert.IsTrue(loadout.TryAdd(catalog.Genes[0]));
            Assert.IsFalse(loadout.TryAdd(catalog.Genes[0]));
            Assert.IsTrue(loadout.TryAdd(catalog.Genes[1]));
            Assert.IsTrue(loadout.TryAdd(catalog.Genes[2]));
            Assert.IsFalse(loadout.TryAdd(catalog.Genes[3]));
            Assert.AreEqual(3, loadout.Count);
        }

        [Test]
        public void ToggleAddsThenRemoves()
        {
            var loadout = new GeneLoadout();
            var gene = catalog.Find("fast");
            Assert.IsTrue(loadout.Toggle(gene));
            Assert.IsFalse(loadout.Toggle(gene));
            Assert.AreEqual(0, loadout.Count);
        }

        [Test]
        public void ModifiersStackMultiplicativelyAndEffectsAreApplied()
        {
            var loadout = new GeneLoadout();
            loadout.TryAdd(catalog.Find("fast"));
            loadout.TryAdd(catalog.Find("armored"));
            loadout.TryAdd(catalog.Find("tracker"));
            var m = loadout.BuildModifiers();
            Assert.AreEqual(1.25f * 0.85f, m.HorseGallopSpeed, 1e-4f);
            Assert.AreEqual(1.5f, m.PlayerMaxHealth, 1e-4f);
            Assert.IsTrue(m.ShowEnemyMarkers);
        }

        [Test]
        public void HorseGenesScaleFromBaseValuesWithoutCompounding()
        {
            var go = new GameObject("Horse");
            try
            {
                go.AddComponent<CharacterController>();
                var horse = go.AddComponent<HorseController>();
                horse.gallopSpeed = 12f;
                horse.turnRate = 100f;
                var genes = go.AddComponent<HorseGenes>();

                var loadout = new GeneLoadout();
                loadout.TryAdd(catalog.Find("fast"));
                genes.Apply(loadout.BuildModifiers());
                genes.Apply(loadout.BuildModifiers());
                Assert.AreEqual(15f, horse.gallopSpeed, 1e-4f);
                Assert.AreEqual(75f, horse.turnRate, 1e-4f);

                genes.Apply(RunModifiers.None);
                Assert.AreEqual(12f, horse.gallopSpeed, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
