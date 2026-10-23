using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Item 11 screen: pick horse genes. Two are free, up to four allowed, each extra one costs Trouble.
    public class GeneSelectionScreen : GameScreen
    {
        GeneCatalog catalog;
        GeneLoadout loadout;
        Action<GeneLoadout> onDone;

        public void Show(GeneCatalog geneCatalog, GeneLoadout current, Action<GeneLoadout> done)
        {
            catalog = geneCatalog;
            loadout = current ?? new GeneLoadout();
            onDone = done;
            Open();
        }

        public void ChooseGene(int index)
        {
            if (!IsOpen || catalog == null || index < 0 || index >= catalog.Genes.Count) return;
            loadout.Toggle(catalog.Genes[index]);
        }

        public void Confirm()
        {
            if (!IsOpen) return;
            var done = onDone;
            var chosen = loadout;
            onDone = null;
            Close();
            done?.Invoke(chosen);
        }

        public void Cancel()
        {
            onDone = null;
            Close();
        }

        protected override void OnDisable()
        {
            onDone = null;
            base.OnDisable();
        }

        protected override void Draw(Rect canvas)
        {
            ThaiGui.Background(canvas, "bg_case_board", 0.7f);
            ThaiGui.Kicker(new Rect(110f, 70f, 0f, 0f), "วางแผน");
            ThaiGui.Label(new Rect(110f, 130f, 1700f, 90f), "เลือกยีนม้า", ThaiGui.Text(72, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.Label(new Rect(110f, 230f, 1700f, 60f),
                $"เลือกได้ฟรี {loadout.FreeSlots} ยีน สูงสุด {loadout.MaxSlots} ยีน ยีนที่เกินจะทำให้ Trouble เพิ่ม และทุกยีนมีข้อเสีย",
                ThaiGui.Text(32, ThaiGui.Gold));

            float cardW = 400f, gap = 30f, x = 110f, y = 320f;
            for (int i = 0; i < catalog.Genes.Count; i++)
            {
                var gene = catalog.Genes[i];
                bool chosen = loadout.Contains(gene);
                var r = new Rect(x + i * (cardW + gap), y, cardW, 520f);
                ThaiGui.Surfaced(r, chosen ? new Color(ThaiGui.Gold.r, ThaiGui.Gold.g, ThaiGui.Gold.b, 0.14f) : new Color(ThaiGui.Surface.r, ThaiGui.Surface.g, ThaiGui.Surface.b, 0.88f),
                    chosen ? ThaiGui.Gold : new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.16f));
                ThaiGui.Tracked(r.center.x - 20f, r.y + 90f, (i + 1).ToString("00"), 80, chosen ? ThaiGui.Gold : ThaiGui.Muted, 0f, FontStyle.Normal);
                ThaiGui.Label(new Rect(r.x + 30f, r.y + 240f, r.width - 60f, 70f), gene.DisplayName,
                    ThaiGui.Text(44, ThaiGui.Gold, FontStyle.Bold, TextAnchor.UpperCenter));
                ThaiGui.Label(new Rect(r.x + 30f, r.y + 310f, r.width - 60f, 120f), gene.Description,
                    ThaiGui.Text(30, ThaiGui.Bone, FontStyle.Normal, TextAnchor.UpperCenter));
                string label = chosen ? "เอาออก" : (loadout.IsFull ? "เต็มแล้ว" : "เลือก");
                GUI.enabled = chosen || !loadout.IsFull;
                if (GUI.Button(new Rect(r.x + 30f, r.yMax - 100f, r.width - 60f, 76f), label, ThaiGui.Button(32)))
                    ChooseGene(i);
                GUI.enabled = true;
            }

            string status = $"เลือกแล้ว {loadout.Count}/{loadout.MaxSlots}";
            if (loadout.IsOverBudget) status += $"   ·   เกินโควตา {loadout.ExtraCount} ยีน: Trouble +{loadout.OverBudgetTrouble:0}";
            ThaiGui.Label(new Rect(110f, 880f, 1100f, 60f), status,
                ThaiGui.Text(34, loadout.IsOverBudget ? ThaiGui.Orange : ThaiGui.Bone, FontStyle.Bold));
            if (GUI.Button(new Rect(1330f, 860f, 480f, 100f), "ออกปฏิบัติการ", ThaiGui.Button(42))) Confirm();
        }
    }
}
