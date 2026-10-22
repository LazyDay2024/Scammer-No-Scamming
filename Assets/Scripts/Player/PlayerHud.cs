using UnityEngine;

namespace WeRInTrouble
{
    // Placeholder IMGUI overlay: crosshair, mount prompt and a controls hint.
    // OnGUI's default font has no Thai glyphs, so the text stays English until a UI font is added.
    public class PlayerHud : MonoBehaviour
    {
        PlayerMount mount;
        Gun gun;
        GUIStyle promptStyle;
        GUIStyle hintStyle;

        void Awake()
        {
            mount = GetComponent<PlayerMount>();
            gun = GetComponent<Gun>();
        }

        void OnGUI()
        {
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
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
                "WASD move / ride   Mouse look   LMB fire   Space jump   Shift sprint   E mount", hintStyle);
        }
    }
}
