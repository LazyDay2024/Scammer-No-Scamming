using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Item 9. Stage 1: the victim tells what happened and the clue cards are laid out.
    // Stage 2: one question at a time; after each answer the right answer and the reason are shown.
    // Stage 3: summary, including whether the hideout location was found.
    public class AnalysisScreen : GameScreen
    {
        enum Stage { Story, Question, Feedback, Summary }

        AnalysisSession session;
        Action<AnalysisResult> onDone;
        Stage stage;
        int lastChoice;
        bool lastCorrect;
        Vector2 storyScroll;

        public AnalysisSession Session => session;

        // ScreenTour hooks: 0 story, 1 question, 2 feedback, 3 summary.
        public void DebugStage(int s) => stage = (Stage)s;

        public void DebugAnswer(int option)
        {
            lastChoice = option;
            lastCorrect = session.Answer(option);
            stage = Stage.Feedback;
        }

        public void Show(CaseDefinition caseDefinition, float troublePerWrong, Action<AnalysisResult> done)
        {
            session = new AnalysisSession(caseDefinition, troublePerWrong);
            onDone = done;
            stage = Stage.Story;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            var c = session.Case;
            ThaiGui.Background(canvas, "bg_case_board", 0.62f);

            // Left: victim portrait and story, always visible.
            var left = new Rect(90f, 80f, 760f, 900f);
            ThaiGui.Panel(left);
            ThaiGui.Kicker(new Rect(left.x + 40f, left.y + 36f, 0f, 0f), c.IsBoss ? "คดีสุดท้าย" : "คดี: " + c.Title);
            var portrait = ThaiGui.Art(c.Portrait);
            float y = left.y + 110f;
            if (portrait != null)
            {
                GUI.DrawTexture(new Rect(left.x + 40f, y, 200f, 200f), portrait, ScaleMode.ScaleAndCrop);
                ThaiGui.Label(new Rect(left.x + 270f, y + 40f, 450f, 60f), c.VictimName, ThaiGui.Text(44, ThaiGui.Gold, FontStyle.Bold));
                ThaiGui.Label(new Rect(left.x + 270f, y + 110f, 450f, 60f), c.VictimRole, ThaiGui.Text(28, ThaiGui.Bone));
                y += 230f;
            }
            else
            {
                y += ThaiGui.Label(new Rect(left.x + 40f, y, 680f, 60f), $"{c.VictimName} ({c.VictimRole})", ThaiGui.Text(40, ThaiGui.Gold, FontStyle.Bold)) + 20f;
            }
            var storyArea = new Rect(left.x + 40f, y, 680f, left.yMax - y - 30f);
            var storyStyle = ThaiGui.Text(30, ThaiGui.Bone);
            float storyH = storyStyle.CalcHeight(new GUIContent(c.VictimStory), storyArea.width - 24f);
            storyScroll = GUI.BeginScrollView(storyArea, storyScroll, new Rect(0f, 0f, storyArea.width - 24f, storyH));
            GUI.Label(new Rect(0f, 0f, storyArea.width - 24f, storyH), "“" + c.VictimStory + "”", storyStyle);
            GUI.EndScrollView();

            var right = new Rect(900f, 80f, 930f, 900f);
            switch (stage)
            {
                case Stage.Story: DrawClues(right, true); break;
                case Stage.Question: DrawQuestion(right); break;
                case Stage.Feedback: DrawFeedback(right); break;
                case Stage.Summary: DrawSummary(right); break;
            }
        }

        void DrawClues(Rect area, bool withStart)
        {
            ThaiGui.Kicker(new Rect(area.x, area.y, 0f, 0f), "เบาะแส");
            float y = area.y + 70f;
            var card = ThaiGui.Art("ui_card");
            var style = ThaiGui.Text(30, card != null ? ThaiGui.Night : ThaiGui.Bone);
            foreach (var clue in session.Case.Clues)
            {
                float h = Mathf.Max(90f, style.CalcHeight(new GUIContent(clue), area.width - 80f) + 40f);
                var r = new Rect(area.x, y, area.width, h);
                if (card != null) GUI.DrawTexture(r, card, ScaleMode.StretchToFill);
                else ThaiGui.Panel(r, 0.8f);
                GUI.Label(new Rect(r.x + 40f, r.y + 20f, r.width - 80f, r.height - 30f), clue, style);
                y += h + 16f;
            }
            if (withStart && GUI.Button(new Rect(area.x, area.yMax - 100f, area.width, 90f),
                    "เริ่มวิเคราะห์ (" + session.Case.Questions.Count + " คำถาม)", ThaiGui.Button(36)))
                stage = Stage.Question;
        }

        void DrawQuestion(Rect area)
        {
            var q = session.CurrentQuestion;
            ThaiGui.Kicker(new Rect(area.x, area.y, 0f, 0f), $"คำถาม {session.Current + 1}/{session.Case.Questions.Count}");
            float y = area.y + 80f;
            y += ThaiGui.Label(new Rect(area.x, y, area.width, 200f), q.Prompt, ThaiGui.Text(42, ThaiGui.Bone, FontStyle.Bold)) + 40f;
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (GUI.Button(new Rect(area.x, y, area.width, 130f), q.Options[i].Text, ThaiGui.Button(30)))
                {
                    lastChoice = i;
                    lastCorrect = session.Answer(i);
                    stage = Stage.Feedback;
                }
                y += 148f;
            }
            if (GUI.Button(new Rect(area.x, area.yMax - 80f, 320f, 70f), "ดูเบาะแสอีกครั้ง", ThaiGui.Button(26)))
                stage = Stage.Story;
        }

        void DrawFeedback(Rect area)
        {
            var q = session.Case.Questions[session.Current - 1];
            ThaiGui.Panel(area);
            var mark = ThaiGui.Art(lastCorrect ? "icon_check" : "icon_cross");
            float x = area.x + 50f;
            if (mark != null) GUI.DrawTexture(new Rect(x, area.y + 50f, 120f, 120f), mark);
            ThaiGui.Label(new Rect(x + (mark != null ? 150f : 0f), area.y + 80f, 600f, 80f),
                lastCorrect ? "ถูกต้อง" : "ยังไม่ใช่", ThaiGui.Text(56, lastCorrect ? ThaiGui.Gold : ThaiGui.Orange, FontStyle.Bold));
            float y = area.y + 210f;
            if (!lastCorrect)
                y += ThaiGui.Label(new Rect(x, y, area.width - 100f, 200f), "คำตอบที่ถูก: " + q.Options[q.CorrectIndex].Text,
                    ThaiGui.Text(34, ThaiGui.Gold)) + 24f;
            y += ThaiGui.Label(new Rect(x, y, area.width - 100f, 300f), q.Explanation, ThaiGui.Text(34, ThaiGui.Bone)) + 24f;
            if (!lastCorrect)
                ThaiGui.Label(new Rect(x, y, area.width - 100f, 80f), $"Trouble +{session.TroublePerWrong:0}", ThaiGui.Text(30, ThaiGui.Orange));

            string next = session.IsComplete ? "สรุปผลการวิเคราะห์" : "คำถามต่อไป";
            if (GUI.Button(new Rect(area.x + 50f, area.yMax - 130f, area.width - 100f, 90f), next, ThaiGui.Button(36)))
                stage = session.IsComplete ? Stage.Summary : Stage.Question;
        }

        void DrawSummary(Rect area)
        {
            var r = session.Result;
            ThaiGui.Panel(area);
            float x = area.x + 50f, w = area.width - 100f;
            float y = area.y + 50f;
            y += ThaiGui.Label(new Rect(x, y, w, 80f), "ผลการวิเคราะห์", ThaiGui.Text(52, ThaiGui.Gold, FontStyle.Bold)) + 20f;
            y += ThaiGui.Label(new Rect(x, y, w, 60f), $"ตอบถูก {r.CorrectCount} จาก {session.Case.Questions.Count} ข้อ",
                ThaiGui.Text(36, ThaiGui.Bone)) + 12f;
            if (r.TroublePenalty > 0f)
                y += ThaiGui.Label(new Rect(x, y, w, 60f), $"Trouble จากการตอบผิด +{r.TroublePenalty:0}", ThaiGui.Text(32, ThaiGui.Orange)) + 12f;
            y += 20f;
            bool found = r.IsUnlocked(IntelKind.HideoutLocation);
            string where = found
                ? "พบตำแหน่งรังแล้ว มองหาลำแสงสีทองเหนือเมือง แล้วขี่ม้าไปได้เลย"
                : "ยังระบุตำแหน่งรังไม่ได้ รู้แค่ว่าอยู่แถว " + session.Case.ZoneHint + " ต้องออกไปตามหาเอง";
            ThaiGui.Label(new Rect(x, y, w, 200f), where, ThaiGui.Text(36, found ? ThaiGui.Gold : ThaiGui.Bone, FontStyle.Bold));

            if (GUI.Button(new Rect(x, area.yMax - 130f, w, 90f), "ต่อไป: แต่งยีนม้า", ThaiGui.Button(36)))
            {
                Close();
                onDone?.Invoke(r);
            }
        }
    }
}
