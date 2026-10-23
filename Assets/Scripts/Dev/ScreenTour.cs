using System.Collections;
using System.IO;
using UnityEngine;

namespace ScammerNoScamming
{
    // Visual check for builds: run the game with  -screentour <folder>  and it opens every screen,
    // saves a screenshot of each to <folder>, then quits. Lets us see real Thai rendering without
    // clicking through by hand. Does nothing in a normal run.
    public class ScreenTour : MonoBehaviour
    {
        string folder;
        int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-screentour")
                {
                    Application.runInBackground = true;
                    var go = new GameObject("ScreenTour");
                    go.AddComponent<ScreenTour>().folder = args[i + 1];
                    DontDestroyOnLoad(go);
                }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(2f);
            var flow = FindAnyObjectByType<CaseFlowDemo>();
            if (flow == null) { Application.Quit(); yield break; }
            var db = CaseDatabase.LoadDefault();
            var first = db.Find("CASE_01_FAKE_SHOP");

            yield return Shot("menu");
            flow.GetComponent<MainMenuScreen>().Close();
            var story = flow.GetComponent<StoryScreen>();
            story.Show("bg_opening", "2035 · สยามภิรมย์", "ม้าเหล็กหมดน้ำมัน", db.Text.Opening, null, "รับคดีแรก", null);
            yield return Shot("opening");
            story.Close();

            var analysis = flow.GetComponent<AnalysisScreen>();
            analysis.Show(first, 10f, null);
            yield return Shot("analysis_story");
            analysis.DebugStage(1);
            yield return Shot("analysis_question");
            analysis.DebugAnswer(0);
            yield return Shot("analysis_feedback_wrong");
            analysis.DebugAnswer(2);
            analysis.DebugAnswer(1);
            analysis.DebugStage(3);
            yield return Shot("analysis_summary");
            analysis.Close();

            var genes = flow.GetComponent<GeneSelectionScreen>();
            var catalog = GeneCatalog.CreateDefault();
            var loadout = new GeneLoadout();
            loadout.TryAdd(catalog.Genes[0]);
            loadout.TryAdd(catalog.Genes[1]);
            loadout.TryAdd(catalog.Genes[2]);
            genes.Show(catalog, loadout, null);
            yield return Shot("genes");
            genes.Close();
            yield return null;

            flow.DebugStartAction(first);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("hud_world");

            var lesson = flow.GetComponent<LessonScreen>();
            lesson.Show(LessonPage.Build(first, db.Text, new[] { TrapKind.FakeSupply, TrapKind.CallCenter }, new[] { TrapKind.Disguised }), null);
            yield return Shot("lesson");
            lesson.Close();

            story.Show("bg_ending_clean", "ฉากจบ: สะอาด", "เมืองได้แสงกลับมา", db.Text.EndingClean,
                db.Text.Stat.Description + "\n(" + db.Text.Stat.SourceName + ")", "กลับเมนูหลัก", null);
            yield return Shot("ending_clean");
            story.Close();
            yield return null;

            var pause = flow.GetComponent<PauseScreen>();
            pause.DebugOpen();
            yield return Shot("pause");
            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{++shot:00}_{name}.png"));
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
