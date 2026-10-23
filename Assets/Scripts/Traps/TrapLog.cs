using System.Collections.Generic;

namespace ScammerNoScamming
{
    // What the player fell for or saw through during the current case, read by the lesson screen
    // ("you just fell for a fake supply box; in real life that is..."). Reset when a case starts.
    public static class TrapLog
    {
        static readonly List<TrapKind> sprung = new List<TrapKind>();
        static readonly List<TrapKind> defused = new List<TrapKind>();

        public static IReadOnlyList<TrapKind> SprungKinds => sprung;
        public static IReadOnlyList<TrapKind> DefusedKinds => defused;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            sprung.Clear();
            defused.Clear();
        }

        public static void RecordSprung(TrapKind kind)
        {
            if (!sprung.Contains(kind)) sprung.Add(kind);
        }

        public static void RecordDefused(TrapKind kind)
        {
            if (!defused.Contains(kind)) defused.Add(kind);
        }
    }
}
