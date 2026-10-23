using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    public class HitFeedback : MonoBehaviour
    {
        public float markerDuration = 0.15f;
        public float markerSize = 8f;
        public float markerThickness = 2f;
        public float killWindow = 0.1f;
        public float gunshotVolume = 0.35f;
        public float gunshotDuration = 0.09f;
        public int sampleRate = 22050;

        public bool MarkerVisible => Time.time < markerUntil;
        public bool IsKillMarker { get; private set; }

        class RecentHit
        {
            public Npc Npc;
            public Vector3 Point;
            public float Time;
            public bool Killed;
            public bool Resolved;
        }

        readonly List<RecentHit> hits = new List<RecentHit>();
        PlayerWeaponController controller;
        Weapon weapon;
        AudioSource audioSource;
        AudioClip gunshot;
        float markerUntil = float.NegativeInfinity;
        float lastShotTime = float.NegativeInfinity;
        Vector3 lastHitPoint;
        int weaponShotFrame = -1;

        void Awake()
        {
            controller = GetComponent<PlayerWeaponController>();
            weapon = GetComponent<Weapon>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            gunshot = CreateGunshot();
        }

        void OnEnable()
        {
            if (weapon != null) weapon.Fired += OnFired;
            if (controller != null) controller.Fired += OnControllerFired;
            Npc.Shot += OnNpcShot;
            Npc.Killed += OnNpcKilled;
        }

        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnFired;
            if (controller != null) controller.Fired -= OnControllerFired;
            Npc.Shot -= OnNpcShot;
            Npc.Killed -= OnNpcKilled;
            hits.Clear();
            markerUntil = float.NegativeInfinity;
        }

        void OnDestroy()
        {
            if (gunshot != null) Destroy(gunshot);
            if (audioSource != null) Destroy(audioSource);
        }

        void OnNpcShot(Npc npc, DamageInfo damage)
        {
            PruneHits();
            hits.Add(new RecentHit { Npc = npc, Point = damage.Point, Time = Time.time });
            if (hits.Count > 32) hits.RemoveAt(0);
        }

        void OnNpcKilled(Npc npc)
        {
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                RecentHit hit = hits[i];
                if (hit.Npc != npc || Time.time - hit.Time > killWindow) continue;
                hit.Killed = true;
                // Also supports damage that completes shortly after the Fired event.
                if (MarkerVisible && Time.time - lastShotTime <= killWindow && SamePoint(hit.Point, lastHitPoint))
                    IsKillMarker = true;
                break;
            }
        }

        void OnFired(ShotResult shot)
        {
            weaponShotFrame = Time.frameCount;
            PlayGunshot();
            if (!shot.HitDamageable) return;
            PruneHits();
            lastShotTime = Time.time;
            lastHitPoint = shot.EndPoint;
            markerUntil = Time.time + markerDuration;
            IsKillMarker = false;
            // Hitscan kills are raised during Resolve, before Weapon.Fired arrives.
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                RecentHit hit = hits[i];
                if (hit.Resolved || !SamePoint(hit.Point, shot.EndPoint)) continue;
                hit.Resolved = true;
                IsKillMarker = hit.Killed;
                break;
            }
        }

        void OnControllerFired(Vector3 position)
        {
            if (weaponShotFrame != Time.frameCount) PlayGunshot();
        }

        static bool SamePoint(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < 0.0001f;
        }

        void PruneHits()
        {
            for (int i = hits.Count - 1; i >= 0; i--)
                if (hits[i].Npc == null || Time.time - hits[i].Time > killWindow) hits.RemoveAt(i);
        }

        void PlayGunshot()
        {
            if (audioSource != null && gunshot != null) audioSource.PlayOneShot(gunshot, gunshotVolume);
        }

        AudioClip CreateGunshot()
        {
            int frequency = Mathf.Clamp(sampleRate, 8000, 96000);
            int count = Mathf.Max(1, Mathf.CeilToInt(Mathf.Clamp(gunshotDuration, 0.01f, 1f) * frequency));
            var samples = new float[count];
            var random = new System.Random(7319);
            for (int i = 0; i < count; i++)
            {
                float progress = (float)i / count;
                samples[i] = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-progress * 9f);
            }
            var clip = AudioClip.Create("Procedural Gunshot", count, 1, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnGUI()
        {
            if (!MarkerVisible) return;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            GUI.color = IsKillMarker ? Color.red : Color.white;
            GUIUtility.RotateAroundPivot(45f, centre);
            GUI.DrawTexture(new Rect(centre.x - markerSize, centre.y - markerThickness * 0.5f,
                markerSize * 2f, markerThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centre.x - markerThickness * 0.5f, centre.y - markerSize,
                markerThickness, markerSize * 2f), Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
    }
}
