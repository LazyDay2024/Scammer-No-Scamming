using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScammerNoScamming
{
    public class PauseScreen : GameScreen
    {
        public Action OnQuitToMenu;
        bool showSettings;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (IsOpen && showSettings) showSettings = false;
            else if (IsOpen) Close();
            else if (!AnyOpen) Open();
        }

        protected override bool FillsScreen => false;
        protected override float FadeIn => 0.25f;

        public void DebugOpen() { showSettings = false; Open(); }
        public void DebugSettings() { Open(); showSettings = true; }

        protected override void Draw(Rect canvas)
        {
            // The frozen game stays visible behind a dark veil.
            ThaiGui.Dim(0.62f);
            ThaiGui.VignetteOverlay(0.6f);
            if (showSettings)
            {
                if (SettingsPanel.Draw(canvas)) showSettings = false;
                return;
            }
            var r = new Rect(canvas.center.x - 330f, 290f, 660f, 470f);
            ThaiGui.Panel(r, 0.9f);
            const float pad = 60f;
            GUI.Label(new Rect(r.x + pad - 4f, r.y + 48f, r.width - pad * 2f, 90f), "พักเกม", ThaiGui.Headline(ThaiGui.H1 - 6, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.HRule(r.x + pad, r.y + 150f, r.width - pad * 2f, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));
            float w = r.width - pad * 2f;
            if (ThaiGui.Btn(new Rect(r.x + pad, r.y + 184f, w, 72f), "เล่นต่อ", 30, true)) Close();
            if (ThaiGui.Btn(new Rect(r.x + pad, r.y + 280f, w, 68f), "ตั้งค่า", 28)) showSettings = true;
            if (ThaiGui.Btn(new Rect(r.x + pad, r.y + 372f, w, 68f), "กลับเมนูหลัก", 28))
            {
                Close();
                OnQuitToMenu?.Invoke();
            }
        }
    }
}
