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
                    Application.runInBackground = true;
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
            var station = flow != null ? flow.GetComponent<StationInvestigation>() : null;
            var database = CaseDatabase.LoadDefault();
            var cases = database.PickRun(3, 2035);
            if (station != null && cases.Count > 0)
                station.BeginCase(cases[0], new GeneLoadout(), _ => { }, _ => { });

            yield return Look(player, new Vector3(0f, 0f, -24f), new Vector3(0f, 2f, 0f), "station");
            yield return Look(player, new Vector3(-5f, .5f, -5.5f), new Vector3(-5f, 1.8f, -1.5f), "station_testimony");
            yield return Look(player, new Vector3(0f, .5f, 1.8f), new Vector3(-5f, 2f, 5.7f), "station_clue_board");
            if (station != null)
            {
                // Exercise the same physical targets as E, including testimony and unlocked map pins.
                station.Victim.Interact();
                yield return Focus(player, station.Victim, "station_testimony_reading");
                for (int guard = 0; guard < 64 && station.StageName == "Testimony"; guard++) station.Victim.Interact();
                for (int guard = 0; guard < 12 && station.StageName == "Questions"; guard++)
                {
                    var question = station.Session.CurrentQuestion;
                    if (question == null) break;
                    int correct = question.CorrectIndex;
                    var target = question.Reward == IntelKind.HideoutLocation ? station.MapPins[correct] : station.AnswerTargets[correct];
                    yield return Focus(player, target, question.Reward == IntelKind.HideoutLocation ? "station_map_question" : "station_answer_question_" + guard);
                    target.Interact();
                    yield return Focus(player, station.AdvanceTarget, "station_answer_feedback_" + guard);
                    station.AdvanceTarget.Interact();
                }
                var genes = flow != null ? flow.GetComponent<GeneSelectionScreen>() : station.GetComponent<GeneSelectionScreen>();
                if (station.StageName == "Genes" && genes != null && genes.IsOpen)
                {
                    yield return new WaitForSecondsRealtime(.5f);
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{++shot:00}_station_gene_screen.png"));
                    genes.ChooseGene(0);
                    yield return new WaitForSecondsRealtime(.4f);
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{++shot:00}_station_gene_selected.png"));
                    genes.Confirm();
                    yield return null;
                }
            }
            var horse = HorseController.All.Count > 0 ? HorseController.All[0] : null;
            if (horse != null) yield return Look(player, horse.transform.position + new Vector3(-4f, 0f, 3f), horse.transform.position + Vector3.up, "horse");
            // Menus and investigation deliberately disable traps during normal play; tour all of them.
            foreach (var trap in FindObjectsByType<Trap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                trap.gameObject.SetActive(true);
                Vector3 t = trap.transform.position;
                Vector3 from = t + (new Vector3(-t.x, 0f, -t.z).normalized * 7f) + Vector3.up * 0f;
                yield return Look(player, from, t + Vector3.up * 0.8f, "trap_" + trap.Kind);
            }
            foreach (var hideout in Hideout.All)
            {
                hideout.Reveal();
                Vector3 t = hideout.transform.position;
                yield return Look(player, hideout.transform.TransformPoint(new Vector3(0f, .05f, -12f)),
                    t + Vector3.up * 2f, "hideout_" + hideout.hideoutId);
                foreach (var definition in cases)
                {
                    if (definition.HideoutId != hideout.hideoutId) continue;
                    if (flow != null) flow.DebugStartAction(definition);
                    break;
                }
                var raid = hideout.GetComponent<RaidEncounter>();
                if (raid != null) raid.BeginRaid();
                var health = player.GetComponent<Health>();
                if (health != null) health.SetMax(10000f);
                yield return Look(player, hideout.transform.TransformPoint(new Vector3(0f, .15f, -.5f)),
                    hideout.transform.TransformPoint(new Vector3(0f, 1.6f, 8f)), "interior_" + hideout.hideoutId);
                yield return Look(player, hideout.transform.TransformPoint(new Vector3(-6.5f, .15f, .4f)),
                    hideout.transform.TransformPoint(new Vector3(-7.8f, 1.5f, 3f)), "worker_" + hideout.hideoutId);
                yield return Look(player, hideout.transform.TransformPoint(new Vector3(0f, .15f, 10.5f)),
                    hideout.transform.TransformPoint(new Vector3(0f, 1.5f, 13.5f)), "evidence_" + hideout.hideoutId);
                if (raid != null) raid.ResetRaid();
            }
            // Wide view of the beacons from the plaza.
            yield return Look(player, new Vector3(0f, 0f, -24f), new Vector3(60f, 30f, 60f), "beacons");
            Application.Quit();
        }

        IEnumerator Focus(PlayerController player, WorldInteractable target, string name)
        {
            Vector3 aim = target.transform.position;
            if (aim.y < .8f) aim += Vector3.up * 1.35f;
            Vector3 from = aim - target.transform.forward * 2.1f;
            from.y = .25f;
            yield return Look(player, from, aim, name);
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
            yield return null; // Let WorldInteractor refresh the prompt under the new camera ray.
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{++shot:00}_{name}.png"));
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
