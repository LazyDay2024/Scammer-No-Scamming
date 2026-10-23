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

        static readonly string[] Icons = { "icon_gene_fast", "icon_gene_armored", "icon_gene_tracker", "icon_gene_stealth" };

        public void Show(GeneCatalog geneCatalog, GeneLoadout current, Action<GeneLoadout> done)
        {
            catalog = geneCatalog;
            loadout = current ?? new GeneLoadout();
            onDone = done;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            ThaiGui.Background(canvas, "bg_case_board", 0.7f);
            ThaiGui.Kicker(new Rect(110f, 70f, 0f, 0f), "วางแผน");
            ThaiGui.Label(new Rect(110f, 130f, 1700f, 90f), "แต่งยีนม้า", ThaiGui.Text(72, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.Label(new Rect(110f, 230f, 1700f, 60f),
                $"เลือกได้ฟรี {loadout.FreeSlots} ยีน สูงสุด {loadout.MaxSlots} ยีน ยีนที่เกินจะทำให้ Trouble เพิ่ม และทุกยีนมีข้อเสีย",
                ThaiGui.Text(32, ThaiGui.Gold));

            float cardW = 400f, gap = 30f, x = 110f, y = 320f;
            for (int i = 0; i < catalog.Genes.Count; i++)
            {
                var gene = catalog.Genes[i];
                bool chosen = loadout.Contains(gene);
                var r = new Rect(x + i * (cardW + gap), y, cardW, 520f);
                GUI.DrawTexture(r, ThaiGui.Solid(chosen ? new Color(0.85f, 0.47f, 0.26f, 0.35f) : new Color(0.07f, 0.1f, 0.11f, 0.88f)));
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 8f), ThaiGui.Solid(chosen ? ThaiGui.Orange : ThaiGui.Teal));
                var icon = ThaiGui.Art(i < Icons.Length ? Icons[i] : null);
                if (icon != null) GUI.DrawTexture(new Rect(r.center.x - 90f, r.y + 40f, 180f, 180f), icon, ScaleMode.ScaleToFit);
                ThaiGui.Label(new Rect(r.x + 30f, r.y + 240f, r.width - 60f, 70f), gene.DisplayName,
                    ThaiGui.Text(48, ThaiGui.Gold, FontStyle.Bold, TextAnchor.UpperCenter));
                ThaiGui.Label(new Rect(r.x + 30f, r.y + 310f, r.width - 60f, 120f), gene.Description,
                    ThaiGui.Text(30, ThaiGui.Bone, FontStyle.Normal, TextAnchor.UpperCenter));
                string label = chosen ? "เอาออก" : (loadout.IsFull ? "เต็มแล้ว" : "เลือก");
                GUI.enabled = chosen || !loadout.IsFull;
                if (GUI.Button(new Rect(r.x + 30f, r.yMax - 100f, r.width - 60f, 76f), label, ThaiGui.Button(32)))
                    loadout.Toggle(gene);
                GUI.enabled = true;
            }

            string status = $"เลือกแล้ว {loadout.Count}/{loadout.MaxSlots}";
            if (loadout.IsOverBudget) status += $"   ·   เกินโควตา {loadout.ExtraCount} ยีน: Trouble +{loadout.OverBudgetTrouble:0}";
            ThaiGui.Label(new Rect(110f, 880f, 1100f, 60f), status,
                ThaiGui.Text(34, loadout.IsOverBudget ? ThaiGui.Orange : ThaiGui.Bone, FontStyle.Bold));
            if (GUI.Button(new Rect(1330f, 860f, 480f, 100f), "ออกล่า!", ThaiGui.Button(42)))
            {
                Close();
                onDone?.Invoke(loadout);
            }
        }
    }
}
