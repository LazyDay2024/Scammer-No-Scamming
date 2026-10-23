using UnityEngine;

namespace ScammerNoScamming
{
    public enum TroubleSource
    {
        WrongAnswer,  // analysis phase
        ExtraGene,    // planning phase, each gene past the free two
        Gunshot,      // action phase: loud shots
        Trap,         // action phase: falling for a trap
        WrongTarget   // action phase: shooting a victim or forced worker
    }

    // The Trouble meter. Owner's rules: Trouble comes from wrong answers, extra genes, loud shots
    // (and traps / wrong targets); when it fills, the corrupt police raid, which is not a loss.
    // After a death only the Trouble earned in the action phase resets; analysis and gene Trouble stay.
    // It is static so it survives the scene reload that replays the action phase.
    public static class TroubleMeter
    {
        // Placeholder numbers, not decided yet.
        public static float Max = 100f;
        public static float GunshotCost = 1f;
        public static float ExtraGeneCost = 10f;
        public static float WrongAnswerCost = 15f;
        public static float WrongTargetCost = 20f;

        public static float Carried { get; private set; }
        public static float Action { get; private set; }
        public static float Current => Mathf.Min(Max, Carried + Action);
        public static bool IsFull => Carried + Action >= Max;

        public static void Add(TroubleSource source, float amount)
        {
            if (amount <= 0f) return;
            bool wasFull = IsFull;
            if (source == TroubleSource.WrongAnswer || source == TroubleSource.ExtraGene) Carried += amount;
            else Action += amount;

            GameEvents.RaiseTroubleChanged(Current, Max);
            if (!wasFull && IsFull) GameEvents.RaiseTroubleFull();
        }

        public static void ResetAction()
        {
            Action = 0f;
            GameEvents.RaiseTroubleChanged(Current, Max);
        }

        // For the GameManager when a new case starts.
        public static void ResetAll()
        {
            Carried = 0f;
            ResetAction();
        }

        // Runs after GameEvents clears its listeners, once per play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Wire()
        {
            Carried = 0f;
            Action = 0f;
            GameEvents.PlayerFired += _ => Add(TroubleSource.Gunshot, GunshotCost);
            GameEvents.TrapSprung += trap => Add(TroubleSource.Trap, trap.troubleCost);
            GameEvents.ActionReplaying += ResetAction;
        }
    }
}
