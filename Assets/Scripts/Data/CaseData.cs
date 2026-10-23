using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    public enum ScamType
    {
        OnlineShopping,
        InvestmentOrJob,
        FakeOfficial,
        PhishingLink,
        Investment,
        Romance
    }

    public enum IntelKind
    {
        None,
        GangProfile,
        HideoutLocation,
        ForcedWorkerMarks
    }

    [Serializable]
    public class AnswerOption
    {
        public string Text;
        public bool IsCorrect;
    }

    [Serializable]
    public class AnalysisQuestion
    {
        public string Prompt;
        public List<AnswerOption> Options = new List<AnswerOption>();
        public string Explanation;
        public IntelKind Reward;

        public int CorrectIndex => Options.FindIndex(o => o.IsCorrect);
    }

    [Serializable]
    public class LessonContent
    {
        public string ScamName;
        public List<string> RedFlags = new List<string>();
        public List<string> HowToRespond = new List<string>();
        public string Footnote;
    }

    [Serializable]
    public class OfficialStat
    {
        public string Description;
        public string SourceName;
        public string SourceUrl;
        public string Period;
    }

    // One scam case. Content lives in Resources/Content/game_content.json so writers can edit text
    // without touching code; CaseDatabase turns each entry into one of these.
    public class CaseDefinition : ScriptableObject
    {
        public string CaseId;
        public string Title;
        public ScamType ScamType;
        public bool IsBoss;
        public string VictimName;
        public string VictimRole;
        public string Portrait;
        [TextArea] public string VictimStory;
        public List<string> Clues = new List<string>();
        public List<AnalysisQuestion> Questions = new List<AnalysisQuestion>();
        public List<TrapKind> Traps = new List<TrapKind>();
        public string HideoutId;
        public string ZoneHint;
        public LessonContent Lesson = new LessonContent();
    }

    [Serializable]
    public class TrapLessonText
    {
        public TrapKind Kind;
        public string Name;
        public string Lesson;
    }

    // Shared text that is not tied to one case.
    [Serializable]
    public class GameText
    {
        public string Opening;
        public List<string> Tutorial = new List<string>();
        public string EndingClean;
        public string EndingMessy;
        public string LessonFooter;
        public List<string> Hotlines = new List<string>();
        public OfficialStat Stat = new OfficialStat();
        public List<TrapLessonText> TrapLessons = new List<TrapLessonText>();

        public TrapLessonText TrapLesson(TrapKind kind) => TrapLessons.Find(t => t.Kind == kind);
    }
}
