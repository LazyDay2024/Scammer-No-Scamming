using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ScammerNoScamming
{
    // Quaternius Generic rigs: clips are serialized by the scene builder, no controller required.
    public class NpcAnimator : MonoBehaviour, INpcAnimation
    {
        public Animator animator;
        public AnimationClip idle, walk, shoot, receiveHit, defeat, victory, sitDown;
        public float fadeSeconds = 0.14f;
        public float walkingClipSpeed = 2f;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClip[] clips = new AnimationClip[7];
        readonly AnimationClipPlayable[] players = new AnimationClipPlayable[7];
        int desired, previous;
        float fade, speed, oneShotUntil;
        bool dead, handsUp, cowering;

        void OnEnable()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
            clips[0] = idle; clips[1] = walk; clips[2] = shoot; clips[3] = receiveHit;
            clips[4] = defeat; clips[5] = victory; clips[6] = sitDown;
            graph = PlayableGraph.Create(name + " NPC Animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, clips.Length);
            var output = AnimationPlayableOutput.Create(graph, "Body", animator);
            output.SetSourcePlayable(mixer);
            animator.applyRootMotion = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null) continue;
                players[i] = AnimationClipPlayable.Create(graph, clips[i]);
                graph.Connect(players[i], 0, mixer, i);
                mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }
            desired = previous = 0;
            fade = 1f;
            graph.Play();
        }

        void OnDisable() { if (graph.IsValid()) graph.Destroy(); }

        void Update()
        {
            if (!graph.IsValid()) return;
            if (!dead && Time.time >= oneShotUntil) Select(handsUp ? 5 : cowering ? 6 : speed > 0.1f ? 1 : 0);
            fade = Mathf.Clamp01(fade + Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            for (int i = 0; i < clips.Length; i++)
            {
                if (!players[i].IsValid()) continue;
                mixer.SetInputWeight(i, i == desired ? fade : i == previous ? 1f - fade : 0f);
                if (i == 1) players[i].SetSpeed(Mathf.Clamp(speed / Mathf.Max(0.1f, walkingClipSpeed), 0.5f, 2f));
                // Generic one-shots are held on their final pose; never loop defeat/surrender.
                if (i >= 2 && players[i].GetTime() >= clips[i].length)
                {
                    players[i].SetTime(Mathf.Max(0f, clips[i].length - 0.001f));
                    players[i].SetSpeed(0f);
                }
            }
        }

        void Select(int index, bool restart = false)
        {
            if (!graph.IsValid() || clips[index] == null) return;
            if (desired == index && !restart) return;
            previous = desired;
            desired = index;
            fade = 0f;
            if (index >= 2)
            {
                players[index].SetTime(0);
                players[index].SetSpeed(1);
            }
        }

        void OneShot(int index)
        {
            if (dead || clips[index] == null) return;
            oneShotUntil = Time.time + clips[index].length;
            Select(index, true);
        }
        public void SetSpeed(float metersPerSecond) => speed = Mathf.Max(0f, metersPerSecond);
        public void ResetForAction()
        {
            dead = handsUp = cowering = false;
            speed = oneShotUntil = 0f;
            Select(0, true);
        }
        public void Aim(bool aiming) { }
        public void Shoot() => OneShot(2);
        public void Hit() => OneShot(3);
        public void Die() { dead = true; Select(4, true); }
        public void HandsUp(bool up) { handsUp = up; if (up && !dead) Select(5); }
        public void Cower(bool cower) { cowering = cower; if (cower && !dead) Select(6); }
    }
}
