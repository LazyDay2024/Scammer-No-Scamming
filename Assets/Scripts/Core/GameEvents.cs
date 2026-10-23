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
        public static event Action<Trap> TrapSprung;
        public static event Action<float, float> TroubleChanged;
        public static event Action TroubleFull;

        public static void RaiseDamaged(Health target, DamageInfo info) => Damaged?.Invoke(target, info);
        public static void RaisePlayerFired(Vector3 noisePosition) => PlayerFired?.Invoke(noisePosition);
        public static void RaisePlayerDied(PlayerCharacter player) => PlayerDied?.Invoke(player);
        public static void RaiseActionReplaying() => ActionReplaying?.Invoke();
        public static void RaiseTrapSprung(Trap trap) => TrapSprung?.Invoke(trap);
        public static void RaiseTroubleChanged(float current, float max) => TroubleChanged?.Invoke(current, max);
        public static void RaiseTroubleFull() => TroubleFull?.Invoke();

        // Static events survive between play sessions when domain reload is off, so start each session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearListeners()
        {
            Damaged = null;
            PlayerFired = null;
            PlayerDied = null;
            ActionReplaying = null;
            TrapSprung = null;
            TroubleChanged = null;
            TroubleFull = null;
        }
    }
}
