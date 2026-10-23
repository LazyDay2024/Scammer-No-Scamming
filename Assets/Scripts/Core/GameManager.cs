using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScammerNoScamming
{
    // Runs one play-through: a random case from the pool, then the boss case, then the ending.
    // Each case goes Briefing (CaseBoard scene: analysis + genes) -> Action (Raid scene) -> Debrief.
    // Lives across scene loads, so it is where state that must survive a death belongs
    // (hideout found, genes chosen); dying only reloads the Raid scene.
    public class GameManager : MonoBehaviour
    {
        public const string CaseBoardScene = "Assets/Scenes/CaseBoard.unity";
        public const string RaidScene = "Assets/Scenes/Raid.unity";
        public const string DebriefScene = "Assets/Scenes/Debrief.unity";
        public const string CaseFolder = "Cases"; // under Resources

        public enum Phase
        {
            Briefing,
            Action,
            Debrief,
            Ending
        }

        // Swapped by tests so the flow can be checked without loading scenes.
        public static Action<string> LoadScene = SceneLoader.Load;

        public static GameManager Current { get; private set; }

        readonly List<CaseDefinition> pool = new List<CaseDefinition>();
        CaseDefinition boss;

        public Phase Stage { get; private set; }
        public CaseDefinition CurrentCase { get; private set; }
        // Set by the analysis step once the player has worked out where the scammers are.
        public bool HideoutRevealed { get; set; }

        // Starts a run from the case assets in Resources/Cases, unless one is already going.
        public static GameManager EnsureRun()
        {
            return Current != null ? Current : StartRun(Resources.LoadAll<CaseDefinition>(CaseFolder));
        }

        public static GameManager StartRun(IEnumerable<CaseDefinition> cases)
        {
            if (Current == null)
            {
                var host = new GameObject("GameManager");
                DontDestroyOnLoad(host);
                Current = host.AddComponent<GameManager>();
            }

            var all = cases.Where(c => c != null).ToList();
            Current.pool.Clear();
            Current.pool.AddRange(all.Where(c => !c.isBoss));
            Current.boss = all.FirstOrDefault(c => c.isBoss);
            if (Current.pool.Count == 0 && Current.boss == null)
                Debug.LogError("GameManager: no cases found in Resources/" + CaseFolder);

            Current.Brief(Current.pool.Count > 0 ? Current.pool[UnityEngine.Random.Range(0, Current.pool.Count)] : Current.boss);
            return Current;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Brief(CaseDefinition next)
        {
            CurrentCase = next;
            HideoutRevealed = false;
            TroubleMeter.ResetAll();
            Stage = Phase.Briefing;
            // A run started from the case board itself must not reload it.
            if (SceneManager.GetActiveScene().path != CaseBoardScene) LoadScene(CaseBoardScene);
        }

        public void BeginAction()
        {
            Stage = Phase.Action;
            LoadScene(RaidScene);
        }

        public void CompleteCase()
        {
            Stage = Phase.Debrief;
            LoadScene(DebriefScene);
        }

        // From the debrief: on to the boss after a normal case, or to the ending after the boss.
        public void Continue()
        {
            if (Stage != Phase.Debrief) return;
            bool bossDone = CurrentCase == null || CurrentCase.isBoss || boss == null;
            if (bossDone) Stage = Phase.Ending; // the Debrief scene shows the ending; criteria not decided yet
            else Brief(boss);
        }

        public void PlayAgain()
        {
            StartRun(pool.Concat(boss != null ? new[] { boss } : new CaseDefinition[0]).ToList());
        }
    }
}
