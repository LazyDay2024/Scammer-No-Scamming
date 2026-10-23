using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public class LessonScreen : GameScreen
    {
        LessonPage page;
        Action onDone;
        Vector2 scroll;

        public void Show(LessonPage lessonPage, Action done)
        {
            page = lessonPage;
            onDone = done;
            scroll = Vector2.zero;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            ThaiGui.Background(canvas, "bg_lesson", 0.55f);
            var area = new Rect(110f, 70f, 1700f, 800f);
            ThaiGui.Panel(area);
            var stamp = ThaiGui.Art("ui_stamp_sealed");
            if (stamp != null) GUI.DrawTexture(new Rect(area.xMax - 300f, area.y + 20f, 260f, 260f), stamp, ScaleMode.ScaleToFit);

            float w = area.width - 420f;
            var view = new Rect(area.x + 60f, area.y + 40f, area.width - 120f, area.height - 60f);
            float contentH = 1400f;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, view.width - 24f, contentH));
            float y = 0f;
            ThaiGui.Kicker(new Rect(0f, y, 0f, 0f), "บทเรียนจากคดีนี้");
            y += 70f;
            y += ThaiGui.Label(new Rect(0f, y, w, 120f), page.Title, ThaiGui.Text(60, ThaiGui.Gold, FontStyle.Bold)) + 24f;

            foreach (var note in page.TrapNotes)
                y += ThaiGui.Label(new Rect(0f, y, view.width - 60f, 200f), "▸ " + note, ThaiGui.Text(32, ThaiGui.Orange)) + 14f;
            if (page.Spotted.Count > 0)
                y += ThaiGui.Label(new Rect(0f, y, view.width - 60f, 100f), "คุณจับสัญญาณได้ทัน: " + string.Join(", ", page.Spotted),
                    ThaiGui.Text(32, ThaiGui.Gold, FontStyle.Bold)) + 14f;
            y += 16f;

            float col = (view.width - 80f) * 0.5f;
            float hLeft = Section(new Rect(0f, y, col, 600f), "สัญญาณเตือน", page.RedFlags);
            float hRight = Section(new Rect(col + 50f, y, col, 600f), "ถ้าเจอแบบนี้ ให้ทำแบบนี้", page.HowToRespond);
            y += Mathf.Max(hLeft, hRight) + 20f;
            if (!string.IsNullOrEmpty(page.Footnote))
                y += ThaiGui.Label(new Rect(0f, y, view.width - 60f, 120f), page.Footnote, ThaiGui.Text(26, ThaiGui.Bone)) + 10f;
            GUI.EndScrollView();

            var banner = new Rect(110f, 890f, 1100f, 140f);
            var bannerArt = ThaiGui.Art("ui_hotline_banner");
            if (bannerArt != null) GUI.DrawTexture(banner, bannerArt, ScaleMode.StretchToFill);
            else ThaiGui.Panel(banner);
            string help = (page.Footer ?? "") + (page.Hotlines.Count > 0 ? "\n" + string.Join("  ·  ", page.Hotlines) : "");
            GUI.Label(new Rect(banner.x + 220f, banner.y + 12f, banner.width - 260f, banner.height - 20f), help,
                ThaiGui.Text(28, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleLeft));

            if (GUI.Button(new Rect(1330f, 910f, 480f, 100f), "ไปคดีต่อไป", ThaiGui.Button(40)))
            {
                Close();
                onDone?.Invoke();
            }
        }

        static float Section(Rect r, string heading, System.Collections.Generic.List<string> items)
        {
            float y = r.y;
            y += ThaiGui.Label(new Rect(r.x, y, r.width, 60f), heading, ThaiGui.Text(38, ThaiGui.Teal, FontStyle.Bold)) + 10f;
            foreach (var item in items)
                y += ThaiGui.Label(new Rect(r.x, y, r.width, 200f), "• " + item, ThaiGui.Text(32, ThaiGui.Bone)) + 8f;
            return y - r.y;
        }
    }
}
