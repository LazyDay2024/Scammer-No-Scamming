using System.Collections.Generic;

namespace ScammerNoScamming
{
    // Result of the analysis phase. Kept on retry (decided: dying only replays the action phase).
    public class AnalysisResult
    {
        public readonly List<int> ChosenOptionIndex = new List<int>();
        public int CorrectCount;
        public int WrongCount;
        public float TroublePenalty;
        readonly HashSet<IntelKind> unlocked = new HashSet<IntelKind>();

        public bool IsUnlocked(IntelKind kind) => unlocked.Contains(kind);
        internal void Unlock(IntelKind kind)
        {
            if (kind != IntelKind.None) unlocked.Add(kind);
        }
    }

    // Each question is answered once. A right answer unlocks its intel (the location question reveals
    // the hideout); a wrong one adds Trouble and the player is shown the right answer and why.
    public class AnalysisSession
    {
        public readonly CaseDefinition Case;
        public readonly float TroublePerWrong;
        public readonly AnalysisResult Result = new AnalysisResult();

        public AnalysisSession(CaseDefinition caseDefinition, float troublePerWrong = 10f)
        {
            Case = caseDefinition;
            TroublePerWrong = troublePerWrong;
        }

        public int Current => Result.ChosenOptionIndex.Count;
        public bool IsComplete => Current >= Case.Questions.Count;
        public AnalysisQuestion CurrentQuestion => IsComplete ? null : Case.Questions[Current];

        // Returns whether the chosen option was right.
        public bool Answer(int optionIndex)
        {
            var question = CurrentQuestion;
            if (question == null) return false;
            bool correct = optionIndex >= 0 && optionIndex < question.Options.Count && question.Options[optionIndex].IsCorrect;
            Result.ChosenOptionIndex.Add(optionIndex);
            if (correct)
            {
                Result.CorrectCount++;
                Result.Unlock(question.Reward);
            }
            else
            {
                Result.WrongCount++;
                Result.TroublePenalty += TroublePerWrong;
            }
            return correct;
        }
    }
}
