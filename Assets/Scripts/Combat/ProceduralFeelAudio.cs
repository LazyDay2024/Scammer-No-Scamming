using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    public enum FeelSound { Shot, Impact, Hoof, Snort, Footstep, Wind, CylinderClick, ShellDrop, Cock }

    // One deterministic mono clip per sound, shared by all players/enemies/horses.
    public static class ProceduralFeelAudio
    {
        const int SampleRate = 22050;
        static readonly Dictionary<FeelSound, AudioClip> clips = new Dictionary<FeelSound, AudioClip>();

        public static AudioClip Get(FeelSound sound)
        {
            if (clips.TryGetValue(sound, out var clip) && clip != null) return clip;
            float[] samples = Generate(sound, SampleRate);
            clip = AudioClip.Create("Feel " + sound, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            clips[sound] = clip;
            return clip;
        }

        public static float Duration(FeelSound sound)
        {
            switch (sound)
            {
                case FeelSound.Shot: return 0.42f;
                case FeelSound.Impact: return 0.12f;
                case FeelSound.Hoof: return 0.16f;
                case FeelSound.Snort: return 0.65f;
                case FeelSound.Footstep: return 0.18f;
                case FeelSound.CylinderClick: return 0.09f;
                case FeelSound.ShellDrop: return 0.3f;
                case FeelSound.Cock: return 0.13f;
                default: return 4f;
            }
        }

        // No Unity native calls: can also be validated outside the Editor.
        public static float[] Generate(FeelSound sound, int sampleRate)
        {
            if (sampleRate < 8000 || sampleRate > 96000) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            int count = (int)Math.Ceiling(Duration(sound) * sampleRate);
            var samples = new float[count];
            var random = new System.Random(7319 + (int)sound);
            double low = 0, phase = 0;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double noise = random.NextDouble() * 2.0 - 1.0;
                low += (noise - low) * (sound == FeelSound.Wind ? 0.012 : 0.13);
                double signal;
                switch (sound)
                {
                    case FeelSound.Shot:
                        signal = noise * 0.65 * Math.Exp(-t * 105) // crack
                            + Math.Sin(2 * Math.PI * (92 * t - 34 * t * t)) * 0.55 * Math.Exp(-t * 24)
                            + low * 0.7 * Math.Exp(-t * 12); // warm outdoor tail
                        break;
                    case FeelSound.Impact:
                        phase += 2 * Math.PI * (2400 - 1000 * t) / sampleRate;
                        signal = (noise * 0.5 + Math.Sin(phase) * 0.25) * Math.Exp(-t * 60);
                        break;
                    case FeelSound.Hoof:
                        signal = (Math.Sin(2 * Math.PI * 125 * t) * 0.55 + noise * 0.3) * Math.Exp(-t * 40);
                        // Paired clop, with a softer second contact.
                        if (t > 0.045) signal += (low * 0.8 + Math.Sin(2 * Math.PI * 190 * t) * 0.25) * Math.Exp(-(t - 0.045) * 60);
                        break;
                    case FeelSound.Snort:
                        signal = (low * 2 + noise * 0.12) * Math.Sin(Math.PI * i / (count - 1))
                            * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 26 * t));
                        break;
                    case FeelSound.Footstep:
                        signal = (low * 1.5 + Math.Sin(2 * Math.PI * 70 * t) * 0.3) * Math.Exp(-t * 35);
                        break;
                    case FeelSound.CylinderClick:
                        signal = (noise * 0.45 + Math.Sin(2 * Math.PI * 2400 * t) * 0.3) * Math.Exp(-t * 105);
                        if (t > 0.025) signal += (noise * 0.22 + Math.Sin(2 * Math.PI * 1650 * t) * 0.18) * Math.Exp(-(t - 0.025) * 120);
                        break;
                    case FeelSound.ShellDrop:
                        signal = 0;
                        // Three offset brass contacts read as a handful of spent cases.
                        for (int shell = 0; shell < 3; shell++)
                        {
                            double contact = t - shell * 0.057;
                            if (contact < 0) continue;
                            signal += (noise * 0.2 + Math.Sin(2 * Math.PI * (3100 + shell * 470) * contact) * 0.25
                                + Math.Sin(2 * Math.PI * 4700 * contact) * 0.12) * Math.Exp(-contact * 40);
                        }
                        break;
                    case FeelSound.Cock:
                        signal = (low * 0.7 + noise * 0.25) * Math.Exp(-t * 60);
                        if (t > 0.042) signal += (noise * 0.38 + Math.Sin(2 * Math.PI * 1850 * t) * 0.24) * Math.Exp(-(t - 0.042) * 110);
                        break;
                    default:
                        signal = low * (0.6 + 0.15 * Math.Sin(2 * Math.PI * t / 4));
                        break;
                }
                double edge = Math.Min(1, Math.Min(i / (sampleRate * 0.0015), (count - 1 - i) / (sampleRate * 0.008)));
                samples[i] = (float)(Math.Max(-0.95, Math.Min(0.95, signal)) * edge);
            }
            return samples;
        }

        public static AudioSource Source(GameObject owner, bool spatial = true, float distance = 35f)
        {
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = distance;
            source.dopplerLevel = 0f;
            return source;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            foreach (var clip in clips.Values) if (clip != null) UnityEngine.Object.Destroy(clip);
            clips.Clear();
        }
    }
}
