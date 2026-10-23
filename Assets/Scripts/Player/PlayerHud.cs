using UnityEngine;

namespace ScammerNoScamming
{
    // Always-on HUD: crosshair, mount prompt and a short controls reminder for the first seconds of play.
    // Styled with the shared ThaiGui design system; hidden while any full screen is open.
    public class PlayerHud : MonoBehaviour
    {
        const float HintSeconds = 22f;

        PlayerMount mount;
        PlayerWeaponController gun;
        RevolverAmmo ammo;
        float playSeconds;

        void Awake()
        {
            mount = GetComponent<PlayerMount>();
            gun = GetComponent<PlayerWeaponController>();
            ammo = GetComponent<RevolverAmmo>();
        }

        void Update()
        {
            if (!GameScreen.AnyOpen) playSeconds += Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            if (GameScreen.AnyOpen) return;
            var canvas = ThaiGui.BeginCanvas();
            var center = canvas.center;

            bool justHit = gun != null && Time.time - gun.LastHitTime < 0.15f;
            var tone = justHit ? ThaiGui.Danger : ThaiGui.Bone;
            if (!WorldInteractor.HasFocusedInteraction)
            {
                // Dot with four short ticks: reads on both bright sky and dark interiors.
                var shadow = new Color(0f, 0f, 0f, 0.45f);
                ThaiGui.Fill(new Rect(center.x - 2.5f, center.y - 2.5f, 5f, 5f), shadow);
                ThaiGui.Fill(new Rect(center.x - 1.5f, center.y - 1.5f, 3f, 3f), tone);
                float gap = justHit ? 12f : 15f;
                foreach (var sign in new[] { -1f, 1f })
                {
                    ThaiGui.Fill(new Rect(center.x + sign * gap - (sign > 0 ? 0f : 8f), center.y - 1f, 8f, 2f), tone);
                    ThaiGui.Fill(new Rect(center.x - 1f, center.y + sign * gap - (sign > 0 ? 0f : 8f), 2f, 8f), tone);
                }
            }

            string prompt = null;
            if (mount != null)
                prompt = mount.IsMounted ? "ลงจากม้า" : mount.Candidate != null ? "ขึ้นม้า" : null;
            if (prompt != null && !WorldInteractor.HasFocusedInteraction)
            {
                var r = new Rect(center.x - 190f, canvas.height - 125f, 380f, 60f);
                ThaiGui.Panel(r, 0.9f);
                GUI.Label(r, "E  ·  " + prompt, ThaiGui.Text(28, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleCenter));
            }

            if (playSeconds < HintSeconds)
            {
                float a = Mathf.Clamp01((HintSeconds - playSeconds) / 3f);
                using (ThaiGui.Fade(a * ThaiGui.Appear(playSeconds, 0.5f)))
                    MainMenuScreen.Controls(56f, canvas.height - 64f);
            }
            ThaiGui.EndCanvas();
            // The controller can add the ammo component during its own Awake.
            if (ammo == null) ammo = GetComponent<RevolverAmmo>();
            if (ammo != null) GameHud.DrawAmmo(ammo.Loaded, ammo.Reserve, ammo.Reloading);
        }
    }
}
