using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ScammerNoScamming
{
    // The visual model follows the existing movement controller without changing its physics.
    public sealed class HorseVisual : MonoBehaviour
    {
        public HorseController horse;
        public Animator animator;
        public AnimationClip idle;
        public AnimationClip walk;
        public AnimationClip run;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] clips = new AnimationClipPlayable[3];
        readonly float[] weights = new float[3];

        void OnEnable()
        {
            if (horse == null) horse = GetComponent<HorseController>();
            if (animator == null || idle == null || walk == null || run == null || animator.runtimeAnimatorController != null) return;
            animator.applyRootMotion = false;
            graph = PlayableGraph.Create("Horse locomotion");
            mixer = AnimationMixerPlayable.Create(graph, 3);
            var animations = new[] { idle, walk, run };
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = AnimationClipPlayable.Create(graph, animations[i]);
                clips[i].SetApplyFootIK(false);
                graph.Connect(clips[i], 0, mixer, i);
                weights[i] = i == 0 ? 1f : 0f;
                mixer.SetInputWeight(i, weights[i]);
            }
            AnimationPlayableOutput.Create(graph, "Horse", animator).SetSourcePlayable(mixer);
            graph.Play();
        }

        void Update()
        {
            // Nithis's realistic horse has its own controller (Speed parameter, Idle/Walk): play the walk faster at a gallop.
            if (!graph.IsValid() && animator != null && animator.runtimeAnimatorController != null && horse != null)
            {
                float v = Mathf.Abs(horse.Speed);
                animator.SetFloat("Speed", v);
                animator.speed = v > .3f ? Mathf.Clamp(v / 2.5f, 1f, 3.2f) : 1f;
                return;
            }
            if (!graph.IsValid() || horse == null) return;
            float speed = Mathf.Abs(horse.Speed);
            float runBlend = Mathf.InverseLerp(horse.trotSpeed * .8f, horse.gallopSpeed, speed);
            float moving = Mathf.Clamp01(speed / .6f);
            var targets = new[] { 1f - moving, moving * (1f - runBlend), moving * runBlend };
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Lerp(weights[i], targets[i], 1f - Mathf.Exp(-8f * Time.deltaTime));
                mixer.SetInputWeight(i, weights[i]);
            }
            clips[1].SetSpeed(Mathf.Clamp(speed / 4f, .65f, 1.6f));
            clips[2].SetSpeed(Mathf.Clamp(speed / 10f, .8f, 1.4f));
        }

        void OnDisable()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
