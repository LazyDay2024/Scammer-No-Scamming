using UnityEngine;

namespace ScammerNoScamming
{
    // One scam case as data, so new cases are assets rather than code. Put them in Resources/Cases;
    // GameManager picks one non-boss case at random per run, then plays the boss case.
    [CreateAssetMenu(menuName = "Scammer No Scamming/Case", fileName = "Case")]
    public class CaseDefinition : ScriptableObject
    {
        public string title = "Untitled case";
        public string scamType = "";
        public bool isBoss;

        [Header("Analysis (shown on the case board)")]
        [TextArea(3, 8)] public string victimStory = "";
        public string[] clues = new string[0];

        [Header("Raid")]
        // Which Hideout in the action scene the scammers use (Hideout.index).
        public int hideoutIndex;
        public int scammers = 3;
        public int riders;
        public int forcedWorkers = 1;
        public int victims = 1;

        [Header("Debrief")]
        [TextArea(3, 8)] public string lesson = "";
    }
}
