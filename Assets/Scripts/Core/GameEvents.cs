using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // One place for game-wide happenings, so systems (Trouble, HUD, the future GameManager) listen here
    // instead of finding and wiring each other. Listeners that are MonoBehaviours subscribe in OnEnable
    // and unsubscribe in OnDisable; the events are static and outlive scene reloads.
    public static class GameEvents
    {
        public static event Action<Health, DamageInfo> Damaged;
        public static event Action<Vector3> PlayerFired;
        public static event Action<PlayerCharacter> PlayerDied;
        // Raised just before the action phase is replayed after a death.
        public static event Action ActionReplaying;
        public static event Action<float, float> TroubleChanged;
        public static event Action TroubleFull;
        // Items of evidence in hand, Sealed so far, and the total for the case.
        public static event Action<int, int, int> EvidenceChanged;
        // Every item of evidence is Sealed.
        public static event Action MissionComplete;

        public static void RaiseDamaged(Health target, DamageInfo info) => Damaged?.Invoke(target, info);
        public static void RaisePlayerFired(Vector3 noisePosition) => PlayerFired?.Invoke(noisePosition);
        public static void RaisePlayerDied(PlayerCharacter player) => PlayerDied?.Invoke(player);
        public static void RaiseActionReplaying() => ActionReplaying?.Invoke();
        public static void RaiseTroubleChanged(float current, float max) => TroubleChanged?.Invoke(current, max);
        public static void RaiseTroubleFull() => TroubleFull?.Invoke();
        public static void RaiseEvidenceChanged(int carried, int sealedCount, int total) => EvidenceChanged?.Invoke(carried, sealedCount, total);
        public static void RaiseMissionComplete() => MissionComplete?.Invoke();

        // Static events survive between play sessions when domain reload is off, so start each session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearListeners()
        {
            Damaged = null;
            PlayerFired = null;
            PlayerDied = null;
            ActionReplaying = null;
            TroubleChanged = null;
            TroubleFull = null;
            EvidenceChanged = null;
            MissionComplete = null;
        }
    }
}
