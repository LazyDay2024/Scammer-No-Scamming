using System.Collections.Generic;

namespace ScammerNoScamming
{
    // The end-of-case lesson: the case's scam, then what the player fell for or saw through on the road
    // ("you just fell for... in real life it is..."), then where to get help.
    public class LessonPage
    {
        public string Title;
        public List<string> RedFlags = new List<string>();
        public List<string> HowToRespond = new List<string>();
        public List<string> TrapNotes = new List<string>();
        public List<string> Spotted = new List<string>();
        public string Footnote;
        public string Footer;
        public List<string> Hotlines = new List<string>();

        public static LessonPage Build(CaseDefinition caseDefinition, GameText text,
            IReadOnlyList<TrapKind> sprung, IReadOnlyList<TrapKind> defused)
        {
            var lesson = caseDefinition.Lesson ?? new LessonContent();
            var page = new LessonPage
            {
                Title = lesson.ScamName,
                Footnote = lesson.Footnote,
                Footer = text?.LessonFooter,
            };
            page.RedFlags.AddRange(lesson.RedFlags);
            page.HowToRespond.AddRange(lesson.HowToRespond);
            if (text != null) page.Hotlines.AddRange(text.Hotlines);

            if (sprung != null)
                foreach (var kind in sprung)
                {
                    var trap = text?.TrapLesson(kind);
                    if (trap != null && !string.IsNullOrEmpty(trap.Lesson)) page.TrapNotes.Add(trap.Lesson);
                }
            if (defused != null)
                foreach (var kind in defused)
                {
                    var trap = text?.TrapLesson(kind);
                    if (trap != null) page.Spotted.Add(trap.Name);
                }
            return page;
        }
    }

    public enum EndingType
    {
        Clean,
        Messy
    }

    // Proposal (not decided by the team yet): clean only if no innocent was shot and total Trouble
    // stayed under the threshold. Both inputs come from the friend's RunState/Trouble systems.
    public static class EndingEvaluator
    {
        public static EndingType Evaluate(int innocentsHit, float totalTrouble, float messyTroubleThreshold = 100f)
        {
            return innocentsHit == 0 && totalTrouble < messyTroubleThreshold ? EndingType.Clean : EndingType.Messy;
        }
    }
}
