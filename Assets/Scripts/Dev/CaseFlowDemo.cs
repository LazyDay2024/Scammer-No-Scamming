using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ScammerNoScamming
{
    // Plays chanawee's part end to end so it can be tested before the core systems land:
    // menu -> opening -> per case: analysis -> genes -> ride/raid -> lesson -> ... -> ending.
    // The raid here is a stand-in: hold E at the hideout door to take the evidence, then hold E at the
    // station's evidence counter to Seal it. When the friend's GameManager / CaseSession / Trouble / enemies
    // (items 1-7) arrive, they replace RunAction() and the Trouble tally below; the screens are reused as is.
    public class CaseFlowDemo : MonoBehaviour
    {
        public Transform sealPoint;
        public float doorRange = 6f;
        public float sealRange = 5f;
        public float holdSeconds = 1.5f;
        public int regularCases = 3;
        public float troublePerWrongAnswer = 10f;
        public float troublePerExtraGene = 20f;

        enum Step { Idle, ToHideout, ToStation, Done }

        CaseDatabase db;
        GeneCatalog catalog;
        MainMenuScreen menu;
        StoryScreen story;
        AnalysisScreen analysis;
        GeneSelectionScreen genes;
        LessonScreen lesson;
        PauseScreen pause;

        List<CaseDefinition> run;
        int caseIndex;
        CaseDefinition current;
        GeneLoadout loadout;
        Step step;
        float hold;
        float trouble;
        int tutorialLine;
        float tutorialTimer;

        public float Trouble => trouble;
        public CaseDefinition Current => current;

        void Awake()
        {
            db = CaseDatabase.LoadDefault();
            catalog = GeneCatalog.CreateDefault();
            menu = gameObject.AddComponent<MainMenuScreen>();
            story = gameObject.AddComponent<StoryScreen>();
            analysis = gameObject.AddComponent<AnalysisScreen>();
            genes = gameObject.AddComponent<GeneSelectionScreen>();
            lesson = gameObject.AddComponent<LessonScreen>();
            pause = gameObject.AddComponent<PauseScreen>();
            pause.OnQuitToMenu = () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void Start()
        {
            Hideout.HideAll();
            menu.Show(() => story.Show("bg_opening", "2035 · สยามภิรมย์", "ม้าเหล็กหมดน้ำมัน", db.Text.Opening, null,
                "รับคดีแรก", BeginRun));
        }

        void BeginRun()
        {
            run = db.PickRun(regularCases, System.Environment.TickCount);
            caseIndex = 0;
            trouble = 0f;
            StartCase();
        }

        void StartCase()
        {
            current = run[caseIndex];
            TrapLog.Reset();
            Hideout.HideAll();
            step = Step.Idle;
            analysis.Show(current, troublePerWrongAnswer, OnAnalysed);
        }

        void OnAnalysed(AnalysisResult result)
        {
            trouble += result.TroublePenalty;
            var hideout = Hideout.Find(current.HideoutId);
            if (hideout != null && result.IsUnlocked(IntelKind.HideoutLocation)) hideout.Reveal();
            genes.Show(catalog, loadout ?? new GeneLoadout(GeneLoadout.DefaultFreeSlots, GeneLoadout.DefaultMaxSlots, troublePerExtraGene), OnGenes);
        }

        void OnGenes(GeneLoadout chosen)
        {
            loadout = chosen;
            trouble += chosen.OverBudgetTrouble;
            var modifiers = chosen.BuildModifiers();
            foreach (var horse in HorseController.All)
            {
                var horseGenes = horse.GetComponent<HorseGenes>();
                if (horseGenes == null) horseGenes = horse.gameObject.AddComponent<HorseGenes>();
                horseGenes.Apply(modifiers);
            }
            step = Step.ToHideout;
            hold = 0f;
            tutorialLine = 0;
            tutorialTimer = 0f;
        }

        void Update()
        {
            if (step == Step.Idle || step == Step.Done || GameScreen.AnyOpen) return;
            var player = PlayerLocator.Player;
            if (player == null) return;

            if (caseIndex == 0 && db.Text.Tutorial.Count > 0)
            {
                tutorialTimer += Time.deltaTime;
                if (tutorialTimer > 7f && tutorialLine < db.Text.Tutorial.Count - 1)
                {
                    tutorialLine++;
                    tutorialTimer = 0f;
                }
            }

            Vector3 target = step == Step.ToHideout ? HideoutPosition() : SealPosition();
            float range = step == Step.ToHideout ? doorRange : sealRange;
            bool inRange = Flat(player.transform.position - target) <= range;
            bool holding = inRange && !player.IsMounted && Keyboard.current != null && Keyboard.current.eKey.isPressed;
            hold = holding ? hold + Time.deltaTime : 0f;
            if (hold < holdSeconds) return;

            hold = 0f;
            if (step == Step.ToHideout)
            {
                step = Step.ToStation;
            }
            else
            {
                step = Step.Done;
                lesson.Show(LessonPage.Build(current, db.Text, TrapLog.SprungKinds, TrapLog.DefusedKinds), NextCase);
            }
        }

        void NextCase()
        {
            caseIndex++;
            if (caseIndex < run.Count)
            {
                StartCase();
                return;
            }
            var ending = EndingEvaluator.Evaluate(0, trouble);
            bool clean = ending == EndingType.Clean;
            string stat = db.Text.Stat != null ? db.Text.Stat.Description + "\n(" + db.Text.Stat.SourceName + ")" : null;
            story.Show(clean ? "bg_ending_clean" : "bg_ending_messy", clean ? "ฉากจบ: สะอาด" : "ฉากจบ: เละ",
                clean ? "เมืองได้แสงกลับมา" : "ชนะ แต่ยังไม่จบ", clean ? db.Text.EndingClean : db.Text.EndingMessy,
                stat, "กลับเมนูหลัก", () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }

        Vector3 HideoutPosition()
        {
            var hideout = Hideout.Find(current.HideoutId);
            return hideout != null ? hideout.transform.position : Vector3.zero;
        }

        Vector3 SealPosition() => sealPoint != null ? sealPoint.position : Vector3.zero;

        static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }

        void OnGUI()
        {
            if (step == Step.Idle || step == Step.Done || GameScreen.AnyOpen || current == null) return;
            var canvas = ThaiGui.BeginCanvas();
            var hud = new Rect(40f, 36f, 760f, 200f);
            GUI.DrawTexture(hud, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.7f)));
            GUI.DrawTexture(new Rect(hud.x, hud.y, 6f, hud.height), ThaiGui.Solid(ThaiGui.Orange));
            GUI.Label(new Rect(hud.x + 26f, hud.y + 14f, 720f, 50f), current.Title, ThaiGui.Text(34, ThaiGui.Gold, FontStyle.Bold));
            var hideout = Hideout.Find(current.HideoutId);
            string objective = step == Step.ToHideout
                ? (hideout != null && hideout.IsRevealed ? "ขี่ม้าไปที่ลำแสงสีทอง แล้วกด E ค้างที่ประตูรังเพื่อเก็บหลักฐาน"
                                                         : "รังอยู่แถว " + current.ZoneHint + " ออกตามหา แล้วกด E ค้างที่ประตูรัง")
                : "นำหลักฐานกลับสถานีตำรวจกลางเมือง แล้วกด E ค้างที่โต๊ะ Seal หลักฐาน";
            GUI.Label(new Rect(hud.x + 26f, hud.y + 66f, 720f, 90f), objective, ThaiGui.Text(28, ThaiGui.Bone));
            GUI.Label(new Rect(hud.x + 26f, hud.y + 152f, 720f, 40f), $"Trouble {trouble:0}", ThaiGui.Text(26, ThaiGui.Orange, FontStyle.Bold));

            if (hold > 0f)
            {
                var bar = new Rect(canvas.width * 0.5f - 250f, canvas.height * 0.62f, 500f, 24f);
                GUI.DrawTexture(bar, ThaiGui.Solid(new Color(0f, 0f, 0f, 0.6f)));
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(hold / holdSeconds), bar.height), ThaiGui.Solid(ThaiGui.Gold));
            }

            if (caseIndex == 0 && tutorialLine < db.Text.Tutorial.Count)
            {
                var tip = new Rect(canvas.width - 820f, canvas.height - 170f, 780f, 120f);
                GUI.DrawTexture(tip, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.78f)));
                GUI.Label(new Rect(tip.x + 24f, tip.y + 12f, tip.width - 48f, tip.height - 24f),
                    $"สอนเล่น {tutorialLine + 1}/{db.Text.Tutorial.Count}: {db.Text.Tutorial[tutorialLine]}",
                    ThaiGui.Text(28, ThaiGui.Bone, FontStyle.Normal, TextAnchor.MiddleLeft));
            }
            ThaiGui.EndCanvas();
        }
    }
}
