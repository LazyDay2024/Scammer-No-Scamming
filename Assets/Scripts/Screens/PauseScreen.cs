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

        protected override void Draw(Rect canvas)
        {
            GUI.DrawTexture(canvas, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.75f)));
            var r = new Rect(660f, 260f, 600f, 560f);
            ThaiGui.Panel(r);
            ThaiGui.Label(new Rect(r.x + 50f, r.y + 40f, 500f, 80f), "พักเกม", ThaiGui.Text(64, ThaiGui.Gold, FontStyle.Bold));
            if (GUI.Button(new Rect(r.x + 50f, r.y + 160f, 500f, 90f), "เล่นต่อ", ThaiGui.Button(38))) Close();
            MainMenuScreen.SensitivitySlider(new Rect(r.x + 50f, r.y + 280f, 500f, 90f));
            if (GUI.Button(new Rect(r.x + 50f, r.y + 420f, 500f, 80f), "กลับเมนูหลัก", ThaiGui.Button(30)))
            {
                Close();
                OnQuitToMenu?.Invoke();
            }
        }
    }
}
