using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Case-file page. In the current flow the lesson is read in the 3D station (StationInvestigation);
    // this screen is the fallback when no station exists, so it keeps the same design system.
    public class LessonScreen : GameScreen
    {
        LessonPage page;
        Action onDone;
        Vector2 scroll;
        float contentH = 900f;

        public void Show(LessonPage lessonPage, Action done)
        {
            page = lessonPage;
            onDone = done;
            scroll = Vector2.zero;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            ThaiGui.Backdrop();
            var area = new Rect(150f, 70f, 1620f, 760f);
            ThaiGui.Panel(area, 0.82f);
            DrawStamp(new Rect(area.xMax - 300f, area.y + 36f, 240f, 70f));

            var view = new Rect(area.x + 60f, area.y + 50f, area.width - 120f, area.height - 80f);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, view.width - 24f, contentH),
                false, false, GUIStyle.none, GUIStyle.none);
            float w = view.width - 380f;
            float y = 0f;
            ThaiGui.Kicker(new Rect(0f, y, 0f, 0f), "บทเรียนจากคดีนี้");
            y += 60f;
            y += ThaiGui.Label(new Rect(0f, y, w, 120f), page.Title, ThaiGui.Headline(ThaiGui.H1 - 8, ThaiGui.Bone, FontStyle.Bold)) + 22f;

            foreach (var note in page.TrapNotes)
            {
                ThaiGui.VRule(0f, y + 4f, 26f, ThaiGui.Gold, 2.5f);
                y += ThaiGui.Label(new Rect(22f, y, w - 22f, 200f), note, ThaiGui.Text(ThaiGui.Small + 2, ThaiGui.Gold)) + 14f;
            }
            if (page.Spotted.Count > 0)
                y += ThaiGui.Label(new Rect(0f, y, view.width - 60f, 100f), "คุณจับสัญญาณได้ทัน: " + string.Join(", ", page.Spotted),
                    ThaiGui.Text(ThaiGui.Small + 2, ThaiGui.Gold, FontStyle.Bold)) + 14f;
            y += 24f;

            float col = (view.width - 100f) * 0.5f;
            float hLeft = Section(new Rect(0f, y, col, 600f), "สัญญาณเตือน", page.RedFlags, ThaiGui.Danger);
            ThaiGui.VRule(col + 50f, y, Mathf.Max(hLeft, 80f), new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.16f));
            float hRight = Section(new Rect(col + 100f, y, col, 600f), "ถ้าเจอแบบนี้ ให้ทำแบบนี้", page.HowToRespond, ThaiGui.Teal);
            y += Mathf.Max(hLeft, hRight) + 26f;
            if (!string.IsNullOrEmpty(page.Footnote))
                y += ThaiGui.Label(new Rect(0f, y, view.width - 60f, 120f), page.Footnote, ThaiGui.Text(ThaiGui.Small, ThaiGui.Muted)) + 10f;
            contentH = Mathf.Max(view.height, y + 20f);
            GUI.EndScrollView();

            // Where to get help: a quiet strip, not a banner.
            ThaiGui.HRule(150f, 880f, 1620f, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));
            ThaiGui.Tracked(150f, 905f, "HELP", ThaiGui.Cap, ThaiGui.Gold);
            string help = (page.Footer ?? "") + (page.Hotlines.Count > 0 ? "\n" + string.Join("   ·   ", page.Hotlines) : "");
            GUI.Label(new Rect(150f, 940f, 1050f, 110f), help, ThaiGui.Text(ThaiGui.Cap + 3, ThaiGui.Bone));

            if (ThaiGui.Btn(new Rect(1370f, 930f, 400f, 76f), "ไปคดีต่อไป", 32, true))
            {
                Close();
                onDone?.Invoke();
            }
        }

        static void DrawStamp(Rect r)
        {
            var previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(-6f, r.center);
            var red = ThaiGui.Danger;
            ThaiGui.Surfaced(r, new Color(red.r, red.g, red.b, 0.06f), new Color(red.r, red.g, red.b, 0.75f));
            ThaiGui.Tracked(r.x + 28f, r.y + 14f, "SEALED", 26, new Color(red.r, red.g, red.b, 0.85f), 5f);
            GUI.matrix = previous;
        }

        static float Section(Rect r, string heading, List<string> items, Color tone)
        {
            float y = r.y;
            ThaiGui.Tracked(r.x, y, heading, ThaiGui.Cap + 3, tone, 0f, FontStyle.Bold);
            y += 46f;
            foreach (var item in items)
            {
                ThaiGui.Fill(new Rect(r.x, y + 22f, 10f, 2f), tone);
                y += ThaiGui.Label(new Rect(r.x + 26f, y, r.width - 26f, 200f), item, ThaiGui.Text(ThaiGui.Small + 3, ThaiGui.Bone)) + 12f;
            }
            return y - r.y;
        }
    }
}
