using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // M map: the city from above, where the player is, and the candidate hideouts. In select mode a pin is
    // chosen and confirmed as the next raid; otherwise it is just a map.
    public class CityMapScreen : GameScreen
    {
        Action<MapSite> onConfirm;
        HashSet<string> wrong = new HashSet<string>();
        string markedSiteId;
        bool selecting;
        MapSite selected;

        public void Show(bool select, HashSet<string> excluded, string marked, Action<MapSite> confirm)
        {
            selecting = select;
            wrong = excluded ?? new HashSet<string>();
            markedSiteId = marked;
            onConfirm = confirm;
            selected = null;
            CityMapView.Instance?.Prepare();
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.M) && Age > .25f) { Close(); e.Use(); return; }

            var map = new Rect(60f, 60f, 960f, 960f);
            GUI.DrawTexture(map, ThaiGui.Solid(new Color(.1f, .09f, .08f)));
            var view = CityMapView.Instance;
            if (view != null && view.Picture != null) GUI.DrawTexture(map, view.Picture, ScaleMode.StretchToFill, true);
            else ThaiGui.Label(new Rect(map.x, map.center.y - 20f, map.width, 40f), "กำลังคลี่แผนที่...", Centered(ThaiGui.Body, ThaiGui.Muted));
            ThaiGui.Surfaced(map, new Color(0f, 0f, 0f, 0f), new Color(ThaiGui.Gold.r, ThaiGui.Gold.g, ThaiGui.Gold.b, .6f));

            if (view != null)
                foreach (var site in MapSite.All)
                {
                    Vector2 uv = view.ToUv(site.transform.position);
                    var pin = new Rect(map.x + Mathf.Clamp01(uv.x) * map.width - 22f, map.y + (1f - Mathf.Clamp01(uv.y)) * map.height - 22f, 44f, 44f);
                    bool bad = wrong.Contains(site.siteId), target = site.siteId == markedSiteId;
                    ThaiGui.Surfaced(pin, bad ? ThaiGui.Rust : target ? ThaiGui.Orange : ThaiGui.Gold, site == selected ? ThaiGui.Bone : ThaiGui.Night);
                    ThaiGui.Label(pin, bad ? "x" : "!", Centered(30, ThaiGui.Night));
                    if (GUI.Button(pin, GUIContent.none, GUIStyle.none)) selected = site;
                }

            DrawPlayer(map, view);

            float x = 1090f;
            ThaiGui.Kicker(new Rect(x, 70f, 0f, 0f), "SIAM PHIRAM");
            ThaiGui.Label(new Rect(x, 110f, 760f, 80f), "แผนที่เมืองสยามภิรมย์", ThaiGui.Headline(ThaiGui.H2, ThaiGui.Bone, FontStyle.Bold));
            string hint = selecting
                ? "เทียบหลักฐานในแฟ้มคดีกับแก๊งแต่ละที่ แล้วเลือกรังที่จะบุก เลือกผิดจะมีสแกมเมอร์บุกสถานี"
                : "ลูกศรคือตำแหน่งของคุณ จุดเครื่องหมายคือรังต้องสงสัย (M หรือ Esc เพื่อปิด)";
            ThaiGui.Label(new Rect(x, 200f, 760f, 140f), hint, ThaiGui.Text(ThaiGui.Small + 2, ThaiGui.Muted));

            DrawLegend(x, 350f);

            float y = 560f;
            if (selected != null)
            {
                bool bad = wrong.Contains(selected.siteId);
                ThaiGui.Panel(new Rect(x, y, 760f, 230f), .8f);
                ThaiGui.Label(new Rect(x + 30f, y + 22f, 700f, 60f), selected.displayName, ThaiGui.Text(ThaiGui.Lead, ThaiGui.Gold, FontStyle.Bold));
                ThaiGui.Label(new Rect(x + 30f, y + 84f, 700f, 120f), "รังของ: " + selected.ScamLabel + (bad ? "\n(ตรวจแล้ว: ไม่ใช่รังของคดีนี้)" : ""),
                    ThaiGui.Text(ThaiGui.Body, ThaiGui.Bone));
                y += 250f;
                if (selecting && !bad && ThaiGui.Btn(new Rect(x, y, 760f, 80f), "ยืนยัน: บุกรังนี้", 34, true))
                {
                    var chosen = selected;
                    Close();
                    onConfirm?.Invoke(chosen);
                }
            }
            else ThaiGui.Label(new Rect(x, y, 760f, 80f), "คลิกจุดบนแผนที่เพื่อดูรายละเอียด", ThaiGui.Text(ThaiGui.Body, ThaiGui.Muted));

            if (ThaiGui.Btn(new Rect(x, 950f, 360f, 70f), "ปิดแผนที่", 30)) Close();
        }

        static void DrawLegend(float x, float y)
        {
            ThaiGui.Kicker(new Rect(x, y, 0f, 0f), "LANDMARKS", ThaiGui.Gold);
            var entries = new[]
            {
                ("TOWER", new Color32(0x22, 0x9B, 0xE9, 0xFF)),
                ("VET", new Color32(0x34, 0xD4, 0x76, 0xFF)),
                ("WEAPONS", new Color32(0xE5, 0x43, 0x4C, 0xFF)),
                ("AUCTION", new Color32(0xFF, 0xC9, 0x28, 0xFF)),
                ("POLICE", new Color32(0x33, 0x59, 0xFA, 0xFF)),
                ("STABLE", new Color32(0xA4, 0x63, 0x27, 0xFF))
            };

            for (int i = 0; i < entries.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                float left = x + column * 280f;
                float top = y + 42f + row * 40f;
                ThaiGui.Fill(new Rect(left, top + 9f, 12f, 12f), entries[i].Item2);
                GUI.Label(new Rect(left + 24f, top, 240f, 30f), entries[i].Item1,
                    ThaiGui.Text(ThaiGui.Cap, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleLeft));
            }
        }
        static void DrawPlayer(Rect map, CityMapView view)
        {
            var player = PlayerLocator.Player;
            if (view == null || player == null) return;
            var camera = player.GetComponentInChildren<Camera>();
            float yaw = camera != null ? camera.transform.eulerAngles.y : player.transform.eulerAngles.y;
            Vector2 uv = view.ToUv(player.transform.position);
            var at = new Vector2(map.x + Mathf.Clamp01(uv.x) * map.width, map.y + (1f - Mathf.Clamp01(uv.y)) * map.height);
            var saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(yaw, at);
            var arrow = ThaiGui.Art("player_marker");
            if (arrow != null) GUI.DrawTexture(new Rect(at.x - 20f, at.y - 21f, 40f, 42f), arrow, ScaleMode.StretchToFill, true);
            else
            {
                GUI.DrawTexture(new Rect(at.x - 6f, at.y - 12f, 12f, 28f), ThaiGui.Solid(ThaiGui.Teal));
                GUI.DrawTexture(new Rect(at.x - 11f, at.y - 18f, 22f, 9f), ThaiGui.Solid(ThaiGui.Bone));
            }
            GUI.matrix = saved;
            ThaiGui.Label(new Rect(at.x - 40f, at.y + 24f, 80f, 30f), "คุณ", Centered(22, ThaiGui.Bone));
        }

        static GUIStyle Centered(int size, Color color)
        {
            var style = ThaiGui.Text(size, color, FontStyle.Bold);
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        }
    }
}
