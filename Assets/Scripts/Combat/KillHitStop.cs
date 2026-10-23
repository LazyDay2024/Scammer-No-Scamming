using UnityEngine;

namespace ScammerNoScamming
{
    // Owns only the freeze it acquired. A GameScreen or another time-scale writer takes over.
    public struct HitStopClock
    {
        bool ownsFreeze;
        float until, savedScale;

        public bool Begin(float now, float seconds, float currentScale, bool screenOpen)
        {
            if (screenOpen || currentScale <= 0f || ownsFreeze) return false;
            savedScale = currentScale;
            until = now + System.Math.Max(0.04f, System.Math.Min(0.06f, seconds));
            ownsFreeze = true;
            return true;
        }

        public bool TryRelease(float now, float currentScale, bool screenOpen, out float restore)
        {
            restore = savedScale;
            if (!ownsFreeze) return false;
            if (screenOpen || currentScale != 0f) { ownsFreeze = false; return false; }
            if (now < until) return false;
            ownsFreeze = false;
            return true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class KillHitStop : MonoBehaviour
    {
        [Range(0.04f, 0.06f)] public float duration = 0.05f;
        Weapon weapon;
        HitStopClock clock;
        // GameScreen keeps listener pause until its deferred focus release, including the
        // frame between closing one screen and opening the next. Respect that handoff too.
        static bool PauseActive => GameScreen.AnyOpen || AudioListener.pause;

        void Awake() => weapon = GetComponent<Weapon>();
        void OnEnable() { if (weapon != null) weapon.Fired += OnFired; }
        void OnFired(ShotResult shot)
        {
            if (shot.Killed && clock.Begin(Time.unscaledTime, duration, Time.timeScale, PauseActive))
                Time.timeScale = 0f;
        }
        void Update() => Release(Time.unscaledTime);
        void Release(float now)
        {
            if (clock.TryRelease(now, Time.timeScale, PauseActive, out float scale)) Time.timeScale = scale;
        }
        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnFired;
            Release(float.PositiveInfinity);
        }
    }
}
