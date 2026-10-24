using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Opening, retreat and ending pages: a typographic column over the live world.
    // The first argument used to name a painted background; it now only tells the mood (messy = grim).
    public class StoryScreen : GameScreen
    {
        string art;
        string kicker;
        string title;
        string body;
        string extra;
        string button;
        Action onDone;
        bool overGameView;

        protected override bool FillsScreen => false;
        // A death page stays on the player's own fallen view; cutting to the drifting backdrop camera there reads as a
        // frozen cutscene with no UI until the page fades in.
        protected override bool WantsLiveWorld => !overGameView;

        public void Show(string backgroundArt, string kickerText, string titleText, string bodyText, string extraText,
            string buttonText, Action done, bool overGameView = false)
        {
            this.overGameView = overGameView;
            art = backgroundArt;
            kicker = kickerText;
            title = titleText;
            body = bodyText;
            extra = extraText;
            button = buttonText;
            onDone = done;
            Open();
        }

        void Finish()
        {
            Close();
            onDone?.Invoke();
        }

        protected override void Draw(Rect canvas)
        {
            bool grim = art != null && art.Contains("messy");
            var tone = grim ? ThaiGui.Danger : ThaiGui.Gold;
            // The scrims fade in with the text; at full strength from the first frame they read as a black flash.
            using (ThaiGui.Fade(ThaiGui.Appear(Age, 0f, 0.45f)))
            {
                ThaiGui.Dim(grim ? 0.35f : 0.12f);
                ThaiGui.ScrimLeft(1350f, 0.95f);
                ThaiGui.ScrimBottom(320f, 0.5f);
                ThaiGui.VignetteOverlay(grim ? 0.8f : 0.55f);
                ThaiGui.Grain(0.035f);
            }

            const float x = 150f, w = 880f;
            float age = Age;
            float y = 130f;
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.1f)))
            {
                if (!string.IsNullOrEmpty(kicker))
                {
                    ThaiGui.Kicker(new Rect(x, y, 0f, 0f), kicker, tone);
                    y += 70f;
                }
            }
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.2f, 0.5f)))
            {
                if (!string.IsNullOrEmpty(title))
                {
                    y += ThaiGui.Label(new Rect(x, y, w + 120f, 200f), title, ThaiGui.Headline(ThaiGui.H1, ThaiGui.Bone, FontStyle.Bold)) + 26f;
                    ThaiGui.Fill(new Rect(x, y, 70f, 2f), tone);
                    y += 40f;
                }
            }
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.35f, 0.5f)))
            {
                y += ThaiGui.Paragraphs(new Rect(x, y, w, 0f), body, ThaiGui.Text(ThaiGui.Lead - 2, ThaiGui.Bone), 18f) + 34f;
                if (!string.IsNullOrEmpty(extra))
                {
                    var style = ThaiGui.Text(ThaiGui.Small, ThaiGui.Muted);
                    float h = style.CalcHeight(new GUIContent(extra), w - 36f);
                    ThaiGui.VRule(x, y + 2f, h - 4f, tone, 2.5f);
                    GUI.Label(new Rect(x + 28f, y, w - 36f, h), extra, style);
                }
            }
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.6f)))
            {
                var r = new Rect(x, canvas.height - 190f, 400f, 76f);
                if (ThaiGui.Btn(r, button, 32, true)) Finish();
                var e = Event.current;
                if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
                {
                    e.Use();
                    Finish();
                }
            }
        }
    }
}
