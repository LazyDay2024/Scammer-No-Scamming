using System.Collections;
using System.IO;
using UnityEngine;

namespace ScammerNoScamming
{
    // Visual check of the 3D world from a build: run with  -worldtour <folder>  and the player is moved to
    // each trap, hideout and the station in turn, looking at it, and a screenshot is saved. Then it quits.
    public class WorldTour : MonoBehaviour
    {
        string folder;
        int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-worldtour")
                {
                    var go = new GameObject("WorldTour");
                    go.AddComponent<WorldTour>().folder = args[i + 1];
                    DontDestroyOnLoad(go);
                }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(2f);
            var flow = FindAnyObjectByType<CaseFlowDemo>();
            if (flow != null) flow.GetComponent<MainMenuScreen>().Close();
            yield return null;
            yield return null;
            Time.timeScale = 1f;

            var player = PlayerLocator.Player;
            if (player == null) { Application.Quit(); yield break; }

            yield return Look(player, new Vector3(0f, 0f, -24f), new Vector3(0f, 2f, 0f), "station");
            var horse = HorseController.All.Count > 0 ? HorseController.All[0] : null;
            if (horse != null) yield return Look(player, horse.transform.position + new Vector3(-4f, 0f, 3f), horse.transform.position + Vector3.up, "horse");
            foreach (var trap in FindObjectsByType<Trap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 t = trap.transform.position;
                Vector3 from = t + (new Vector3(-t.x, 0f, -t.z).normalized * 7f) + Vector3.up * 0f;
                yield return Look(player, from, t + Vector3.up * 0.8f, "trap_" + trap.Kind);
            }
            foreach (var hideout in Hideout.All)
            {
                hideout.Reveal();
                Vector3 t = hideout.transform.position;
                Vector3 inward = new Vector3(-t.x, 0f, -t.z).normalized;
                yield return Look(player, t + inward * 12f, t + Vector3.up * 2f, "hideout_" + hideout.hideoutId);
            }
            // Wide view of the beacons from the plaza.
            yield return Look(player, new Vector3(0f, 0f, -24f), new Vector3(60f, 30f, 60f), "beacons");
            Application.Quit();
        }

        IEnumerator Look(PlayerController player, Vector3 position, Vector3 target, string name)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            position.y = Mathf.Max(position.y, 0.05f);
            player.transform.position = position;
            Vector3 flat = target - position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(flat);
            if (cc != null) cc.enabled = true;
            var cam = player.GetComponentInChildren<Camera>();
            yield return new WaitForSecondsRealtime(0.4f);
            if (cam != null) cam.transform.LookAt(target);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{++shot:00}_{name}.png"));
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
