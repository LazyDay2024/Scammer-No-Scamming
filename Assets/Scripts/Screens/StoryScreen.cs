using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public class StoryScreen : GameScreen
    {
        string art;
        string kicker;
        string title;
        string body;
        string extra;
        string button;
        Action onDone;

        public void Show(string backgroundArt, string kickerText, string titleText, string bodyText, string extraText,
            string buttonText, Action done)
        {
            art = backgroundArt;
            kicker = kickerText;
            title = titleText;
            body = bodyText;
            extra = extraText;
            button = buttonText;
            onDone = done;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            ThaiGui.Background(canvas, art, 0.2f);
            GUI.DrawTexture(new Rect(0f, 0f, 1060f, 1080f), ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.72f)));
            float y = 140f;
            if (!string.IsNullOrEmpty(kicker))
            {
                ThaiGui.Kicker(new Rect(110f, y, 0f, 0f), kicker);
                y += 80f;
            }
            if (!string.IsNullOrEmpty(title))
                y += ThaiGui.Label(new Rect(110f, y, 880f, 200f), title, ThaiGui.Text(76, ThaiGui.Bone, FontStyle.Bold)) + 30f;
            y += ThaiGui.Label(new Rect(110f, y, 880f, 500f), body, ThaiGui.Text(36, ThaiGui.Bone)) + 30f;
            if (!string.IsNullOrEmpty(extra))
                ThaiGui.Label(new Rect(110f, y, 880f, 300f), extra, ThaiGui.Text(28, ThaiGui.Gold));
            if (GUI.Button(new Rect(110f, 920f, 560f, 96f), button, ThaiGui.Button(38)))
            {
                Close();
                onDone?.Invoke();
            }
        }
    }
}
