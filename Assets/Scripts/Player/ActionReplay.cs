using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScammerNoScamming
{
    // Owner's rule: when the player dies, replay the action phase of the same case. Genes and analysis
    // answers are kept, and only the Trouble earned during the action phase is reset (TroubleMeter
    // does that on GameEvents.ActionReplaying).
    // Reloading the action scene resets everything in it (enemies, traps, horse, ammo, health), so whatever
    // must survive a death (genes, answers, pre-action Trouble) has to live outside the scene, for example
    // in a DontDestroyOnLoad GameManager. A boss fight in its own scene restarts just the boss this way.
    public class ActionReplay : MonoBehaviour
    {
        public float delay = 3f;

        float replayAt = -1f;
        bool replayed;

        // Seconds until the replay starts, or -1 while the player is alive.
        public float SecondsLeft => replayAt < 0f ? -1f : Mathf.Max(0f, replayAt - Time.time);

        void Update()
        {
            var player = Interaction.Player;
            if (player == null) return;

            if (!player.Health.IsDead)
            {
                replayed = false;
                return;
            }

            // The dead player lingers until the reload finishes, so one death must not schedule a second replay.
            if (replayed) return;
            if (replayAt < 0f) replayAt = Time.time + delay;
            if (Time.time < replayAt) return;

            replayAt = -1f;
            replayed = true;
            GameEvents.RaiseActionReplaying();
            Replay();
        }

        protected virtual void Replay()
        {
            SceneLoader.Load(SceneManager.GetActiveScene().path);
        }
    }
}
