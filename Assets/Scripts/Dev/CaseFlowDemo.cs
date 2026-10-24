using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Case data and analysis remain the source of truth; the action now happens in the city.
    public class CaseFlowDemo : MonoBehaviour
    {
        public Transform sealPoint;
        public float doorRange = 6f;
        public float sealRange = 5f;
        public float holdSeconds = 1.5f;
        public int regularCases = 3;
        [Tooltip("Off for the one-case vertical slice; turn on to end the run with the boss case.")]
        public bool includeBoss = true;
        public float troublePerWrongAnswer = 10f;
        public float troublePerExtraGene = 20f;
        public float troublePerShot = 1.5f;
        public float troublePerTrap = 12f;
        public float troublePerInnocent = 20f;
        public float policeThreshold = 100f;
        public StationInvestigation station;
        [Tooltip("Optional: case file, map and raid-site choice at the start of a case, instead of the station boards.")]
        public CaseBriefing briefing;

        enum Step { Idle, ToHideout, ToStation, Dead, Done }
        CaseDatabase db;
        GeneCatalog catalog;
        MainMenuScreen menu;
        StoryScreen story;
        AnalysisScreen analysis;
        GeneSelectionScreen genes;
        LessonScreen lesson;
        PauseScreen pause;
        List<CaseDefinition> run;
        int caseIndex, innocentsHit, innocentsBeforeAction;
        CaseDefinition current;
        GeneLoadout loadout;
        RunModifiers modifiers = RunModifiers.None;
        Step step;
        float trouble, troubleBeforeAction;
        Vector3 playerStart, horseStart;
        Quaternion playerStartRot, horseStartRot;
        HorseController horse;
        PlayerVitals vitals;
        PlayerWeaponController playerWeapon;
        RaidEncounter raid;
        WorldInteractable sealInteraction;
        readonly HashSet<Npc> hurtInnocents = new HashSet<Npc>();

        public float Trouble => trouble;
        public CaseDefinition Current => current;
        public bool HasEvidence => raid != null && raid.EvidenceCollected;
        public bool ActionActive => step == Step.ToHideout || step == Step.ToStation;
        public int InnocentsHit => innocentsHit;
        public GeneLoadout Loadout => loadout;
        public AnalysisResult CurrentAnalysis { get; private set; }

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
            pause.OnQuitToMenu = ReturnToMenu;
        }

        void Start()
        {
            MainMenuScreen.LoadSensitivity();
            var player = PlayerLocator.Player;
            if (player != null)
            {
                playerStart = player.transform.position;
                playerStartRot = player.transform.rotation;
                vitals = player.GetComponent<PlayerVitals>();
                if (vitals == null) vitals = player.gameObject.AddComponent<PlayerVitals>();
                if (player.GetComponent<HitFeedback>() == null) player.gameObject.AddComponent<HitFeedback>();
                if (player.GetComponent<WorldInteractor>() == null) player.gameObject.AddComponent<WorldInteractor>();
                vitals.Died += OnPlayerDied;
                playerWeapon = player.GetComponent<PlayerWeaponController>();
                if (playerWeapon != null) playerWeapon.Fired += OnPlayerFired;
            }
            horse = HorseController.All.Count > 0 ? HorseController.All[0] : null;
            if (horse != null)
            {
                horseStart = horse.transform.position;
                horseStartRot = horse.transform.rotation;
            }
            if (station == null) station = FindAnyObjectByType<StationInvestigation>();
            if (sealPoint != null)
            {
                sealInteraction = sealPoint.GetComponent<WorldInteractable>();
                if (sealInteraction == null) sealInteraction = sealPoint.gameObject.AddComponent<WorldInteractable>();
                sealInteraction.Prompt = "Seal หลักฐานคดี";
                sealInteraction.Range = sealRange;
                sealInteraction.OnInteract = () => CompleteSeal();
                sealInteraction.Available = false;
            }
            Npc.Shot += OnNpcShot;
            Trap.Sprung += OnTrapSprung;
            ShowMenu();
        }

        void OnDestroy()
        {
            if (vitals != null) vitals.Died -= OnPlayerDied;
            if (playerWeapon != null) playerWeapon.Fired -= OnPlayerFired;
            Npc.Shot -= OnNpcShot;
            Trap.Sprung -= OnTrapSprung;
        }

        void ResetEncounters()
        {
            foreach (var encounter in FindObjectsByType<RaidEncounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                encounter.ResetRaid();
            raid = null;
        }

        void ShowMenu()
        {
            step = Step.Idle;
            ResetEncounters();
            Hideout.HideAll();
            DisableTraps();
            if (station != null) station.Close();
            if (briefing != null) briefing.Stop();
            menu.Show(() => story.Show("bg_opening", "2035 · สยามภิรมย์", "ม้าเหล็กหมดน้ำมัน", db.Text.Opening, null,
                "รับคดีแรก", BeginRun));
        }

        void ReturnToMenu()
        {
            loadout = null;
            ResetPlayerPosition();
            if (vitals != null) vitals.ResetForAction(RunModifiers.None);
            if (station != null) station.Reset();
            ShowMenu();
        }

        // For CaseBriefing: heal and return to the station start after dying there.
        public void RespawnPlayerAtStart()
        {
            ResetPlayerPosition();
            if (vitals != null) vitals.ResetForAction(RunModifiers.None);
            if (playerWeapon != null) playerWeapon.FullRestock();
        }

        public void ShowStory(string title, string subtitle, string body, string button, System.Action onDone) =>
            story.Show("bg_ending_messy", title, subtitle, body, null, button, onDone, true);

        void ResetPlayerPosition()
        {
            var player = PlayerLocator.Player;
            if (player != null)
            {
                var mount = player.GetComponent<PlayerMount>();
                if (mount != null && mount.IsMounted) mount.Dismount();
                if (horse != null) horse.ClearRider();
                Teleport(player.gameObject, playerStart, player.transform.rotation);
            }
            if (horse != null) Teleport(horse.gameObject, horseStart, horseStartRot);
        }

        static void Teleport(GameObject go, Vector3 position, Quaternion rotation)
        {
            var cc = go.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            go.transform.SetPositionAndRotation(position, rotation);
            if (cc != null) cc.enabled = true;
        }

        void ArmTraps(CaseDefinition definition)
        {
            foreach (var trap in FindObjectsByType<Trap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                trap.ResetForAction();
                if (IsRealCrate(trap)) { trap.gameObject.SetActive(true); continue; } // ammo crates are scenery, not part of a case
                bool wanted = definition == null || definition.Traps.Count == 0 || definition.Traps.Contains(trap.Kind);
                if (!trap.IsDefused) trap.gameObject.SetActive(wanted);
            }
        }

        void DisableTraps()
        {
            foreach (var trap in FindObjectsByType<Trap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                trap.ResetForAction();
                trap.gameObject.SetActive(IsRealCrate(trap));
            }
        }

        static bool IsRealCrate(Trap trap) => trap is SupplyCrate crate && !crate.isFake;

        static string lastFirstCaseId; // survives returning to the menu; reset only when the app restarts

        void BeginRun()
        {
            // A fresh draw every time a game starts, never opening on the case the last game opened on.
            run = db.PickRun(regularCases, System.Environment.TickCount);
            if (run.Count > 1 && !run[0].IsBoss && run[0].CaseId == lastFirstCaseId)
            {
                int swap = run.FindIndex(1, c => !c.IsBoss);
                if (swap > 0) { var first = run[0]; run[0] = run[swap]; run[swap] = first; }
            }
            lastFirstCaseId = run[0].CaseId;
            if (!includeBoss) run.RemoveAll(c => c.IsBoss);
            caseIndex = 0;
            trouble = 0f;
            innocentsHit = 0;
            StartCase();
        }

        void StartCase()
        {
            current = run[caseIndex];
            TrapLog.Reset();
            ResetEncounters();
            Hideout.HideAll();
            DisableTraps();
            step = Step.Idle;
            CurrentAnalysis = null;
            if (briefing != null) { briefing.Begin(current, OnAnalysed); return; }
            if (station != null)
            {
                station.TroublePerWrongAnswer = troublePerWrongAnswer;
                station.BeginCase(current, loadout ?? new GeneLoadout(GeneLoadout.DefaultFreeSlots,
                    GeneLoadout.DefaultMaxSlots, troublePerExtraGene), OnAnalysed, OnGenes);
            }
            else analysis.Show(current, troublePerWrongAnswer, OnAnalysed);
        }

        public void OnAnalysed(AnalysisResult result)
        {
            CurrentAnalysis = result;
            trouble += result.TroublePenalty;
            var hideout = Hideout.Find(current.HideoutId);
            if (hideout != null && result.IsUnlocked(IntelKind.HideoutLocation)) hideout.Reveal();
            if (station == null)
                genes.Show(catalog, loadout ?? new GeneLoadout(GeneLoadout.DefaultFreeSlots,
                    GeneLoadout.DefaultMaxSlots, troublePerExtraGene), OnGenes);
        }

        public void OnGenes(GeneLoadout chosen)
        {
            loadout = chosen;
            trouble += chosen.OverBudgetTrouble;
            modifiers = chosen.BuildModifiers();
            foreach (var mount in HorseController.All)
            {
                var horseGenes = mount.GetComponent<HorseGenes>();
                if (horseGenes == null) horseGenes = mount.gameObject.AddComponent<HorseGenes>();
                horseGenes.Apply(modifiers);
            }
            troubleBeforeAction = trouble;
            innocentsBeforeAction = innocentsHit;
            BeginAction();
        }

        void BeginAction()
        {
            ResetEncounters();
            TrapLog.Reset();
            ArmTraps(current);
            hurtInnocents.Clear();
            if (vitals != null) vitals.ResetForAction(modifiers);
            if (playerWeapon != null) playerWeapon.FullRestock(); // a retry after dying starts with full ammo
            ArmedEnemy.SightMultiplier = Mathf.Max(0f, modifiers.EnemySightRange);
            var hideout = Hideout.Find(current.HideoutId);
            raid = hideout != null ? hideout.GetComponent<RaidEncounter>() : null;
            if (raid != null) raid.BeginRaid();
            if (briefing != null) briefing.OnActionBegan();
            step = Step.ToHideout;
            CheckPolice();
        }

        void Update()
        {
            if (step == Step.ToHideout && HasEvidence) step = Step.ToStation;
            if (sealInteraction != null) sealInteraction.Available = step == Step.ToStation && HasEvidence;
        }

        void OnPlayerFired(Vector3 position) { if (ActionActive) AddActionTrouble(troublePerShot); }
        void OnTrapSprung(Trap trap) { if (ActionActive) AddActionTrouble(troublePerTrap); }
        void OnNpcShot(Npc npc, DamageInfo info)
        {
            if (!ActionActive || npc == null || (npc.Faction != Faction.ForcedWorker && npc.Faction != Faction.Victim)) return;
            if (hurtInnocents.Add(npc)) innocentsHit++;
            AddActionTrouble(troublePerInnocent);
        }
        public void AddActionTrouble(float amount)
        {
            if (!ActionActive) return;
            trouble += Mathf.Max(0f, amount) * Mathf.Max(0f, modifiers.TroubleGain);
            CheckPolice();
        }
        void CheckPolice() { if (raid != null && trouble >= policeThreshold) raid.SpawnCorruptPolice(); }

        // Seconds the player's view spends falling before the retreat page appears.
        const float DeathViewSeconds = 1.8f;

        void OnPlayerDied() => StartCoroutine(DeathRoutine());

        // Every death, in any phase, plays the fall (PlayerCharacter) and then shows a page, so a death can never leave a
        // frozen camera with no UI. The page's button respawns the player with full health and ammo.
        System.Collections.IEnumerator DeathRoutine()
        {
            yield return new WaitForSecondsRealtime(DeathViewSeconds);
            if (vitals == null || !vitals.Health.IsDead) yield break; // revived meanwhile (debug, restart)
            if (briefing != null && briefing.HandleDeath()) yield break;
            if (ActionActive)
            {
                step = Step.Dead;
                story.Show("bg_ending_messy", "ล่าถอยจากพื้นที่", "ยังมีโอกาสกลับไปช่วยพวกเขา",
                    "คุณบาดเจ็บจนบุกต่อไม่ไหว กลับมาที่สถานีแล้วเตรียมลงมือใหม่ คำตอบวิเคราะห์และยีนที่เลือกยังอยู่ครบ",
                    null, "บุกอีกครั้ง", RetryAction, true);
                yield break;
            }
            story.Show("bg_ending_messy", "คุณล้มลง", "ยังไม่จบแค่นี้",
                "คุณฟื้นขึ้นมาที่สถานีพร้อมเลือดและกระสุนเต็ม คดียังดำเนินต่อจากเดิม",
                null, "ลุกขึ้นสู้ต่อ", () => { story.Close(); RespawnPlayerAtStart(); }, true);
        }

        public void RetryAction()
        {
            if (step != Step.Dead) return;
            story.Close();
            trouble = troubleBeforeAction;
            innocentsHit = innocentsBeforeAction;
            ResetPlayerPosition();
            BeginAction();
        }

        public bool CompleteSeal()
        {
            if (!ActionActive || !HasEvidence) return false;
            step = Step.Done;
            if (sealInteraction != null) sealInteraction.Available = false;
            var page = LessonPage.Build(current, db.Text, TrapLog.SprungKinds, TrapLog.DefusedKinds);
            if (raid != null) raid.ResetRaid();
            if (station != null) station.ShowLesson(page, NextCase);
            else lesson.Show(page, NextCase);
            return true;
        }

        // Tour hook: keep the same runtime combat contract as a real action.
        public void DebugStartAction(CaseDefinition definition)
        {
            current = definition;
            run = new List<CaseDefinition> { definition };
            caseIndex = 0;
            modifiers = RunModifiers.None;
            troubleBeforeAction = trouble;
            innocentsBeforeAction = innocentsHit;
            var hideout = Hideout.Find(current.HideoutId);
            if (hideout != null) hideout.Reveal();
            BeginAction();
        }

        void NextCase()
        {
            caseIndex++;
            if (caseIndex < run.Count) { StartCase(); return; }
            var ending = EndingEvaluator.Evaluate(innocentsHit, trouble);
            bool clean = ending == EndingType.Clean;
            string stat = db.Text.Stat != null ? db.Text.Stat.Description + "\n(" + db.Text.Stat.SourceName + ")" : null;
            story.Show(clean ? "bg_ending_clean" : "bg_ending_messy", clean ? "ฉากจบ: สะอาด" : "ฉากจบ: เละ",
                clean ? "เมืองได้แสงกลับมา" : "ชนะ แต่ยังไม่จบ", clean ? db.Text.EndingClean : db.Text.EndingMessy,
                stat, "กลับเมนูหลัก", ReturnToMenu);
        }

        void OnGUI()
        {
            if (!ActionActive || GameScreen.AnyOpen || current == null) return;
            string objective = briefing != null && briefing.ActionObjective(HasEvidence) is string briefed ? briefed
                : HasEvidence ? "นำหลักฐานกลับสถานี แล้ว Seal ที่โต๊ะเก็บหลักฐาน"
                : raid != null && raid.Cleared ? "ปลอดภัยแล้ว เก็บหลักฐานจากเครื่องของบอส (กด E) · " + raid.EvidenceTaken + "/" + raid.EvidenceTotal
                : $"บุก {current.ZoneHint} · ศัตรูเหลือ {(raid != null ? raid.RemainingEnemies : 0)} คน";
            Vector3? waypoint = null;
            if (HasEvidence)
            {
                if (sealPoint != null) waypoint = sealPoint.position;
            }
            else
            {
                var entrance = HideoutEntrance.Find(current.HideoutId);
                var hideout = Hideout.Find(current.HideoutId);
                if (entrance != null) waypoint = entrance.transform.position;
                else if (hideout != null) waypoint = hideout.transform.position;
            }
            GameHud.Draw(current.Title, objective, vitals != null ? vitals.Health.Current : 100f,
                vitals != null ? vitals.Health.max : 100f, trouble, policeThreshold, raid != null && raid.PoliceSpawned, waypoint);
        }
    }
}
