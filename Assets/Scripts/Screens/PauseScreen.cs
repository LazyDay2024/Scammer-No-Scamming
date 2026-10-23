using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScammerNoScamming
{
    public class PauseScreen : GameScreen
    {
        public Action OnQuitToMenu;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (IsOpen) Close();
            else if (!AnyOpen) Open();
        }

        protected override bool FillsScreen => false;
        protected override float FadeIn => 0.25f;

        public void DebugOpen() => Open();

        protected override void Draw(Rect canvas)
        {
            // The frozen game stays visible behind a dark veil.
            ThaiGui.Dim(0.62f);
            ThaiGui.VignetteOverlay(0.6f);
            var r = new Rect(canvas.center.x - 330f, 230f, 660f, 620f);
            ThaiGui.Panel(r, 0.9f);
            const float pad = 60f;
            ThaiGui.Kicker(new Rect(r.x + pad, r.y + 52f, 0f, 0f), "PAUSED");
            GUI.Label(new Rect(r.x + pad, r.y + 100f, r.width - pad * 2f, 90f), "พักเกม", ThaiGui.Headline(ThaiGui.H1, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.HRule(r.x + pad, r.y + 215f, r.width - pad * 2f, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));
            if (ThaiGui.Btn(new Rect(r.x + pad, r.y + 250f, r.width - pad * 2f, 76f), "เล่นต่อ", 32, true)) Close();
            MainMenuScreen.SensitivitySlider(new Rect(r.x + pad, r.y + 366f, r.width - pad * 2f, 70f));
            if (ThaiGui.Btn(new Rect(r.x + pad, r.y + 480f, r.width - pad * 2f, 70f), "กลับเมนูหลัก", 28))
            {
                Close();
                OnQuitToMenu?.Invoke();
            }
        }
    }
}
