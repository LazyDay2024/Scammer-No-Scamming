using UnityEngine;

namespace ScammerNoScamming
{
    // Stand-in for the real case board (analysis + genes, built by the UI owners). Shows the case and
    // lets the flow continue so the game can be played end to end. English only: IMGUI has no Thai font.
    public class CaseBoardPlaceholder : MonoBehaviour
    {
        GUIStyle text;

        void Start()
        {
            GameManager.EnsureRun();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            var game = GameManager.Current;
            if (game == null || game.CurrentCase == null) return;
            if (text == null) text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };

            var c = game.CurrentCase;
            GUILayout.BeginArea(new Rect(40f, 30f, Screen.width - 80f, Screen.height - 60f));
            GUILayout.Label((c.isBoss ? "BOSS CASE: " : "CASE: ") + c.title + "  (" + c.scamType + ")", text);
            GUILayout.Space(10f);
            GUILayout.Label(c.victimStory, text);
            foreach (var clue in c.clues) GUILayout.Label("- " + clue, text);
            GUILayout.Space(20f);

            if (!game.HideoutRevealed)
            {
                // The real analysis step decides this and adds Trouble for wrong answers.
                if (GUILayout.Button("[placeholder] Analysis done: reveal the hideout", GUILayout.Height(40f)))
                    game.HideoutRevealed = true;
            }
            else
            {
                GUILayout.Label("Hideout found: " + RaidSandbox.Corners[Mathf.Clamp(c.hideoutIndex, 0, RaidSandbox.Corners.Length - 1)].label, text);
            }

            if (GUILayout.Button("Start the raid", GUILayout.Height(40f))) game.BeginAction();
            GUILayout.EndArea();
        }
    }
}
