using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // The victim's statement as a plain Thai police form: white paper, black Angsana New text, no emblem.
    public class CaseDocumentScreen : GameScreen
    {
        public const string StationName = "สยามภิรมย์";
        public const string Investigator = "พ.ต.ท. ปรีชา วงศ์สุวรรณ";
        public const string Witness = "ร.ต.อ. สมศักดิ์ ใจดี";
        const string InterviewDate = "23 ตุลาคม 2578";
        const string InterviewTime = "09.30";

        static readonly Color Paper = Color.white;
        static readonly Color Ink = Color.black;

        // Angsana New ships with Windows; like the rest of the UI it is taken from the OS, not redistributed.
        // shortcut: no font file is bundled, so on a machine without Angsana the fallbacks are used (WebGL has no OS fonts).
        static Font angsana;
        static Font Angsana => angsana != null ? angsana
            : angsana = Font.CreateDynamicFontFromOSFont(new[] { "Angsana New", "AngsanaUPC", "TH Sarabun New", "Leelawadee UI", "Tahoma" }, 32);
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

        CaseDefinition current;
        Action onClosed;
        Vector2 scroll;
        float contentHeight = 1400f;

        public void Show(CaseDefinition definition, Action closed)
        {
            current = definition;
            onClosed = closed;
            scroll = Vector2.zero;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && Age > .2f) { Finish(); return; }

            var sheet = new Rect(410f, 20f, 1100f, 960f);
            GUI.DrawTexture(new Rect(sheet.x + 8f, sheet.y + 10f, sheet.width, sheet.height), ThaiGui.Solid(new Color(0f, 0f, 0f, .45f)));
            GUI.DrawTexture(sheet, ThaiGui.Solid(Paper));

            var view = new Rect(sheet.x + 70f, sheet.y + 50f, sheet.width - 140f, sheet.height - 100f);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, view.width - 24f, contentHeight), false, false, GUIStyle.none, GUIStyle.none);
            float w = view.width - 24f, y = 0f;
            y += ThaiGui.Label(new Rect(0f, y, w, 80f), "บันทึกถ้อยคำผู้เสียหาย", Style(54, FontStyle.Bold, TextAnchor.UpperCenter)) + 14f;
            var line = Style(38, FontStyle.Normal, TextAnchor.UpperLeft);
            y += ThaiGui.Label(new Rect(0f, y, w, 60f), "สถานีตำรวจภูธร: " + StationName, line) + 2f;
            y += ThaiGui.Label(new Rect(0f, y, w, 60f), "วันที่: " + InterviewDate + "    เวลา: " + InterviewTime + " น.", line) + 2f;
            y += ThaiGui.Label(new Rect(0f, y, w, 100f), "คดีอาญาระหว่าง " + Victim() + " ผู้เสียหาย กับ " + Suspect() + " ผู้ต้องหา", line) + 22f;
            y += ThaiGui.Label(new Rect(0f, y, w, 140f), Indent + "ข้าพเจ้า " + Investigator + " ตำแหน่ง พนักงานสอบสวน ได้ทำการสอบสวนปากคำ "
                + current.VictimName + " ซึ่งเป็นผู้เสียหายในคดีนี้ โดยผู้เสียหายให้การว่า", line) + 12f;
            y += ThaiGui.Label(new Rect(0f, y, w, 1200f), Indent + Statement(), line) + 50f;

            float col = w / 3f;
            for (int i = 0; i < 3; i++)
            {
                string role = i == 0 ? "ผู้เสียหาย" : i == 1 ? "พนักงานสอบสวน" : "พยาน";
                string name = i == 0 ? Victim() : i == 1 ? Investigator : Witness;
                float x = i * col, width = col - 20f;
                ThaiGui.Label(new Rect(x, y, width, 50f), "ลงชื่อ ....................", Style(32, FontStyle.Normal, TextAnchor.UpperCenter));
                ThaiGui.Label(new Rect(x, y + 44f, width, 50f), "(" + name + ")", Style(32, FontStyle.Normal, TextAnchor.UpperCenter));
                ThaiGui.Label(new Rect(x, y + 88f, width, 50f), role, Style(32, FontStyle.Bold, TextAnchor.UpperCenter));
            }
            y += 170f;
            contentHeight = y;
            GUI.EndScrollView();

            if (contentHeight > view.height)
                ThaiGui.Label(new Rect(sheet.x, sheet.yMax - 52f, sheet.width, 40f), "เลื่อนเมาส์เพื่ออ่านต่อ", Style(30, FontStyle.Bold, TextAnchor.UpperCenter));
            if (ThaiGui.Btn(new Rect(sheet.center.x - 200f, sheet.yMax + 10f, 400f, 60f), "ปิดแฟ้ม", 32, true)) Finish();
        }

        void Finish()
        {
            Close();
            onClosed?.Invoke();
        }

        string Victim() => (current.Honorific ?? "") + current.VictimName;
        string Suspect() => string.IsNullOrEmpty(current.Suspect) ? "บุคคลไม่ทราบชื่อ" : current.Suspect;

        string Statement()
        {
            string age = current.Age > 0 ? " อายุ " + current.Age + " ปี" : "";
            string address = string.IsNullOrEmpty(current.Address) ? "" : " อาศัยอยู่บ้านเลขที่ " + current.Address;
            string when = string.IsNullOrEmpty(current.IncidentDate) ? "" : " เมื่อวันที่ " + current.IncidentDate
                + (string.IsNullOrEmpty(current.IncidentTime) ? "" : " เวลาประมาณ " + current.IncidentTime + " น.");
            return "ข้าพเจ้า " + Victim() + age + " ประกอบอาชีพ " + current.VictimRole + address + " ขอให้การว่า" + when
                + " " + current.VictimStory.Replace("\n", " ");
        }

        const string Indent = "  "; // two em spaces: first-line indent of a Thai form

        // Plain black on white; the hover and active states repeat the normal one so the text never changes colour.
        static GUIStyle Style(int size, FontStyle fontStyle, TextAnchor anchor)
        {
            string key = size + "_" + fontStyle + "_" + anchor;
            if (!styles.TryGetValue(key, out var style) || style.font == null)
            {
                style = new GUIStyle(ThaiGui.Text(size, Ink, fontStyle, anchor)) { font = Angsana };
                style.hover.textColor = style.active.textColor = style.focused.textColor = style.normal.textColor;
                styles[key] = style;
            }
            return style;
        }
    }
}
