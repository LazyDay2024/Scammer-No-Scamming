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
            yield return new WaitForSecondsRealtime(9f);
            yield return Shot("menu_drifted");
            flow.GetComponent<MainMenuScreen>().DebugSettings(true);
            yield return Shot("menu_settings");
            flow.GetComponent<MainMenuScreen>().Close();
            var story = flow.GetComponent<StoryScreen>();
            story.Show("bg_opening", "2035 · สยามภิรมย์", "ม้าเหล็กหมดน้ำมัน", db.Text.Opening, null, "รับคดีแรก", null);
            yield return Shot("opening");
            story.Close();

            // Station: the 3D investigation, with the interaction prompt and the subtitle panel.
            var station = flow.station != null ? flow.station : FindAnyObjectByType<StationInvestigation>();
            var player = PlayerLocator.Player;
            if (station != null && player != null)
            {
                station.BeginCase(first, new GeneLoadout(), null, null);
                yield return null;
                Place(player, new Vector3(-5f, 0.1f, -5.2f), 0f, 10f);
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("station_victim");
                Place(player, new Vector3(-6.6f, 0.1f, 2.6f), 0f, 4f);
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("station_clue");
                station.Reset();
            }

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

            story.Show("bg_ending_messy", "ล่าถอยจากพื้นที่", "ยังมีโอกาสกลับไปช่วยพวกเขา",
                "คุณบาดเจ็บจนบุกต่อไม่ไหว กลับมาที่สถานีแล้วเตรียมลงมือใหม่ คำตอบวิเคราะห์และยีนที่เลือกยังอยู่ครบ", null, "บุกอีกครั้ง", null);
            yield return Shot("death");
            story.Close();
            yield return null;

            story.Show("bg_ending_clean", "ฉากจบ: สะอาด", "เมืองได้แสงกลับมา", db.Text.EndingClean,
                db.Text.Stat.Description + System.Environment.NewLine + "(" + db.Text.Stat.SourceName + ")", "กลับเมนูหลัก", null);
            yield return Shot("ending_clean");
            story.Close();
            yield return null;

            var pause = flow.GetComponent<PauseScreen>();
            pause.DebugOpen();
            yield return Shot("pause");
            pause.DebugSettings();
            yield return Shot("pause_settings");
            Application.Quit();
        }

        // Dev-only: put the player somewhere and aim the camera (yaw/pitch are private in PlayerController).
        static void Place(PlayerController player, Vector3 position, float yaw, float pitch)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(PlayerController).GetField("yaw", flags)?.SetValue(player, yaw);
            typeof(PlayerController).GetField("pitch", flags)?.SetValue(player, pitch);
            if (player.cameraPivot != null) player.cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{++shot:00}_{name}.png"));
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
