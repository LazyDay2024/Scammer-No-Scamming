using UnityEngine;

namespace ScammerNoScamming
{
    // Stand-in for the lesson page and the endings (built by the UI owners). Shows the case lesson,
    // then the ending once the boss is done. Ending criteria are not decided yet.
    public class DebriefPlaceholder : MonoBehaviour
    {
        GUIStyle text;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            var game = GameManager.Current;
            if (game == null) return;
            if (text == null) text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };

            GUILayout.BeginArea(new Rect(40f, 30f, Screen.width - 80f, Screen.height - 60f));
            if (game.Stage == GameManager.Phase.Ending)
            {
                GUILayout.Label("THE END [placeholder - two endings, criteria not decided]", text);
                GUILayout.Label(string.Format("Trouble at the end: {0:0}/{1:0}", TroubleMeter.Current, TroubleMeter.Max), text);
                if (GUILayout.Button("Play again", GUILayout.Height(40f))) game.PlayAgain();
            }
            else if (game.CurrentCase != null)
            {
                GUILayout.Label("CASE CLOSED: " + game.CurrentCase.title, text);
                GUILayout.Space(10f);
                GUILayout.Label(game.CurrentCase.lesson, text);
                GUILayout.Space(20f);
                if (GUILayout.Button(game.CurrentCase.isBoss ? "Finish" : "Next: the boss", GUILayout.Height(40f))) game.Continue();
            }
            GUILayout.EndArea();
        }
    }
}
