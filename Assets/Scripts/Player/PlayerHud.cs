using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Placeholder IMGUI overlay: crosshair, mount prompt, health, and the damage that was dealt or taken.
    // OnGUI's default font has no Thai glyphs, so the text stays English until a UI font is added.
    public class PlayerHud : MonoBehaviour
    {
        const float PopupLife = 1.2f;
        const float LogLife = 4f;
        const int LogLines = 5;

        struct Entry
        {
            public Vector3 Position;
            public string Text;
            public float Time;
            public Color Color;
        }

        PlayerMount mount;
        PlayerWeaponController gun;
        Health health;
        Camera viewCamera;
        ActionReplay replay;
        GUIStyle promptStyle;
        GUIStyle hintStyle;
        GUIStyle popupStyle;
        readonly List<Entry> popups = new List<Entry>();
        readonly List<Entry> taken = new List<Entry>();

        void Awake()
        {
            mount = GetComponent<PlayerMount>();
            gun = GetComponent<PlayerWeaponController>();
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            GameEvents.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            GameEvents.Damaged -= OnDamaged;
        }

        void OnDamaged(Health target, DamageInfo info)
        {
            string text = string.Format("-{0:0.#}  {1}", info.Amount, info.Zone);
            if (target == health)
            {
                taken.Add(new Entry { Text = text + "  from " + info.SourceFaction, Time = Time.time, Color = new Color(1f, 0.35f, 0.3f) });
                if (taken.Count > LogLines) taken.RemoveAt(0);
            }
            else
            {
                var color = info.Zone == HitZoneType.Head ? new Color(1f, 0.9f, 0.3f) : Color.white;
                popups.Add(new Entry { Position = info.Point, Text = text, Time = Time.time, Color = color });
            }
        }

        void OnGUI()
        {
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                popupStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            if (health != null && health.IsDead)
            {
                DrawDeath(cy);
                return;
            }

            bool justHit = gun != null && Time.time - gun.LastHitTime < 0.15f;

            Color previous = GUI.color;
            GUI.color = justHit ? Color.red : Color.white;
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 16f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.color = previous;

            string prompt = null;
            if (mount != null)
                prompt = mount.IsMounted ? "[E] Dismount" : mount.Candidate != null ? "[E] Mount horse" : null;
            if (prompt != null)
                GUI.Label(new Rect(0f, cy + 60f, Screen.width, 40f), prompt, promptStyle);

            GUI.Label(new Rect(12f, 10f, Screen.width, 24f),
                "WASD move / ride   Mouse look   LMB fire   R reload   Space jump   Shift sprint   E mount", hintStyle);

            if (gun != null)
                GUI.Label(new Rect(Screen.width - 320f, Screen.height - 40f, 300f, 28f),
                    gun.Reloading ? "RELOADING..." : string.Format("AMMO {0} / {1}", gun.Loaded, gun.Reserve), popupStyle);
            DrawHealthAndLog();
            DrawTrouble();
            DrawPopups();
        }

        void DrawDeath(float cy)
        {
            if (replay == null) replay = FindFirstObjectByType<ActionReplay>();
            string line = replay != null && replay.SecondsLeft >= 0f
                ? string.Format("Replaying the raid in {0:0}...", Mathf.Ceil(replay.SecondsLeft))
                : "Replaying the raid...";

            Color previous = GUI.color;
            GUI.color = new Color(0.5f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0f, cy - 40f, Screen.width, 40f), "YOU DIED", promptStyle);
            GUI.Label(new Rect(0f, cy, Screen.width, 40f), line, promptStyle);
            GUI.color = previous;
        }

        void DrawTrouble()
        {
            const float width = 220f;
            float x = Screen.width - width - 16f;
            float fill = TroubleMeter.Max > 0f ? TroubleMeter.Current / TroubleMeter.Max : 0f;

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(x, 40f, width, 14f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.9f, 0.1f, 0.1f), fill);
            GUI.DrawTexture(new Rect(x, 40f, width * fill, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string label = TroubleMeter.IsFull
                ? "TROUBLE FULL - corrupt police incoming!"
                : string.Format("TROUBLE {0:0}/{1:0}", TroubleMeter.Current, TroubleMeter.Max);
            GUI.Label(new Rect(x, 14f, width + 16f, 24f), label, hintStyle);
            GUI.color = previous;
        }

        void DrawHealthAndLog()
        {
            if (health != null)
                GUI.Label(new Rect(12f, 34f, 300f, 28f), string.Format("HP {0:0}/{1:0}", health.Current, health.max), popupStyle);

            taken.RemoveAll(entry => Time.time - entry.Time > LogLife);
            Color previous = GUI.color;
            for (int i = 0; i < taken.Count; i++)
            {
                GUI.color = taken[i].Color;
                GUI.Label(new Rect(12f, 64f + i * 22f, 400f, 24f), taken[i].Text, hintStyle);
            }
            GUI.color = previous;
        }

        void DrawPopups()
        {
            popups.RemoveAll(entry => Time.time - entry.Time > PopupLife);
            if (popups.Count == 0) return;
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (viewCamera == null) return;

            Color previous = GUI.color;
            foreach (var entry in popups)
            {
                Vector3 screen = viewCamera.WorldToScreenPoint(entry.Position);
                if (screen.z <= 0f) continue;

                float age = Time.time - entry.Time;
                float y = Screen.height - screen.y - age * 50f;
                var color = entry.Color;
                color.a = 1f - age / PopupLife;
                GUI.color = color;
                GUI.Label(new Rect(screen.x - 80f, y - 12f, 160f, 24f), entry.Text, popupStyle);
            }
            GUI.color = previous;
        }
    }
}
