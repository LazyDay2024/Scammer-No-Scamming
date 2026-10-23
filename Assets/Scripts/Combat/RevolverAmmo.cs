using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Opt in on the player's revolver only; other weapons retain their existing unlimited ammo.
    [DisallowMultipleComponent]
    public sealed class RevolverAmmo : MonoBehaviour
    {
        public const int Capacity = 6;
        public const int SupplyReserve = 36;

        [SerializeField, Range(0, Capacity)] int loaded = Capacity;
        [SerializeField, Min(0)] int reserve = SupplyReserve;
        [SerializeField, Min(0.01f)] float reloadSeconds = 1.6f;

        float elapsed;
        int mountTransitionFrame = -1;

        public int Loaded => loaded;
        public int Reserve => reserve;
        public bool Reloading { get; private set; }
        public float ReloadProgress => Reloading ? Mathf.Clamp01(elapsed / Duration) : 0f;

        public event Action Changed;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;
        public event Action ReloadInterrupted;
        public event Action EmptyTriggered;

        float Duration => Mathf.Max(0.01f, reloadSeconds);
        bool MountTransitionThisFrame => mountTransitionFrame == Time.frameCount;

        void Update() => AdvanceReload(Time.deltaTime);

        void OnValidate()
        {
            loaded = Mathf.Clamp(loaded, 0, Capacity);
            reserve = Mathf.Max(0, reserve);
            reloadSeconds = Mathf.Max(0.01f, reloadSeconds);
        }

        public bool TryConsumeRound()
        {
            if (Reloading || MountTransitionThisFrame) return false;
            if (loaded <= 0)
            {
                EmptyTriggered?.Invoke();
                TryReload();
                return false;
            }

            loaded--;
            Changed?.Invoke();
            return true;
        }

        public bool TryReload()
        {
            if (Reloading || MountTransitionThisFrame || loaded >= Capacity || reserve <= 0) return false;
            elapsed = 0f;
            Reloading = true;
            ReloadStarted?.Invoke();
            Changed?.Invoke();
            return true;
        }

        // Public stepping keeps the ammo rules deterministic in EditMode tests. Scaled Update time
        // pauses reload alongside gameplay, and no rounds move until the full motion has finished.
        public void AdvanceReload(float deltaTime)
        {
            if (!Reloading || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            elapsed += deltaTime;
            if (elapsed < Duration) return;

            int transfer = Mathf.Min(Capacity - loaded, reserve);
            loaded += transfer;
            reserve -= transfer;
            elapsed = 0f;
            Reloading = false;
            ReloadCompleted?.Invoke();
            Changed?.Invoke();
        }

        public void InterruptReload()
        {
            if (!Reloading) return;
            elapsed = 0f;
            Reloading = false;
            ReloadInterrupted?.Invoke();
            Changed?.Invoke();
        }

        // Called at the actual mount/dismount transition, so a later Update in the same frame
        // cannot immediately restart the reload with R or an empty trigger pull.
        public void NotifyMountTransition()
        {
            mountTransitionFrame = Time.frameCount;
            InterruptReload();
        }

        public void RefillReserve()
        {
            if (reserve >= SupplyReserve) return;
            reserve = SupplyReserve;
            Changed?.Invoke();
        }

        // A fake supply crate (Nithis's TrapConsequences) takes every round.
        public void Empty()
        {
            InterruptReload();
            loaded = 0;
            reserve = 0;
            Changed?.Invoke();
        }
    }
}
