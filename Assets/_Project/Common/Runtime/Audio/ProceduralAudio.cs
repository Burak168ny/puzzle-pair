using UnityEngine;

namespace PuzzlePair.Audio
{
    /// <summary>
    /// Synthesizes the short sound effects of both games at startup, so the repository ships
    /// without audio assets. Every clip is generated once and cached by <see cref="Sfx"/>.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 44100;

        public static AudioClip Create(SfxId id)
        {
            switch (id)
            {
                case SfxId.Click:
                    return Synth("Click", 0.05f, (t, d) => Tone(1500f, t) * Decay(t, 70f) * Attack(t) * 0.7f);
                case SfxId.Whoosh:
                    return Synth("Whoosh", 0.22f, (t, d) =>
                        (Sweep(420f, 1700f, t, d) * 0.45f + Noise() * 0.35f) * Envelope(t, d, 0.015f) * 0.6f);
                case SfxId.Bump:
                    return Synth("Bump", 0.16f, (t, d) =>
                        (Sweep(260f, 90f, t, d) * 0.9f + Noise() * 0.2f * Decay(t, 60f)) * Decay(t, 20f) * Attack(t));
                case SfxId.HeartLost:
                    return Notes("HeartLost", new[] { 330f, 247f }, 0.09f, 0.22f, 0.4f);
                case SfxId.Hint:
                    return Notes("Hint", new[] { 988f, 1319f, 1760f }, 0.06f, 0.25f, 0.28f);
                case SfxId.Undo:
                    return Synth("Undo", 0.18f, (t, d) => Sweep(1400f, 500f, t, d) * Envelope(t, d, 0.01f) * 0.4f);
                case SfxId.Pop:
                    return Synth("Pop", 0.07f, (t, d) =>
                        (Tone(720f, t) + 0.45f * Tone(1440f, t)) * Decay(t, 65f) * Attack(t) * 0.55f);
                case SfxId.Shoot:
                    return Synth("Shoot", 0.06f, (t, d) => Sweep(1800f, 700f, t, d) * Decay(t, 55f) * Attack(t) * 0.35f);
                case SfxId.Launch:
                    return Synth("Launch", 0.16f, (t, d) => Sweep(300f, 900f, t, d) * Decay(t, 16f) * Attack(t) * 0.55f);
                case SfxId.Land:
                    return Synth("Land", 0.1f, (t, d) =>
                        (Sweep(240f, 140f, t, d) + Noise() * 0.15f) * Decay(t, 35f) * Attack(t) * 0.7f);
                case SfxId.Slot:
                    return Synth("Slot", 0.12f, (t, d) => (Tone(523f, t) + 0.5f * Tone(784f, t)) * Decay(t, 28f) * Attack(t) * 0.4f);
                case SfxId.Warning:
                    return Notes("Warning", new[] { 440f, 440f }, 0.11f, 0.1f, 0.3f);
                case SfxId.Spent:
                    return Synth("Spent", 0.16f, (t, d) => Sweep(500f, 1600f, t, d) * Decay(t, 20f) * Attack(t) * 0.4f);
                case SfxId.Win:
                    return Notes("Win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.09f, 0.45f, 0.33f);
                case SfxId.Fail:
                    return Notes("Fail", new[] { 392f, 349.23f, 293.66f, 246.94f }, 0.15f, 0.35f, 0.3f);
                case SfxId.Appear:
                    return Synth("Appear", 0.05f, (t, d) => Tone(900f, t) * Decay(t, 80f) * Attack(t) * 0.35f);
                default:
                    return null;
            }
        }

        private delegate float Generator(float t, float duration);

        private static AudioClip Synth(string name, float duration, Generator gen)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(gen((float)i / SampleRate, duration), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Notes(string name, float[] freqs, float noteLength, float tail, float gain)
        {
            float duration = freqs.Length * noteLength + tail;
            return Synth(name, duration, (t, d) =>
            {
                float v = 0f;
                for (int i = 0; i < freqs.Length; i++)
                {
                    float local = t - i * noteLength;
                    if (local < 0f) break;
                    // Sine with a soft second harmonic and a plucked envelope.
                    v += (Tone(freqs[i], local) + 0.3f * Tone(freqs[i] * 2f, local)) * Decay(local, 9f) * Attack(local);
                }
                return v * gain;
            });
        }

        private static float Tone(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        private static float Sweep(float from, float to, float t, float duration)
        {
            // Integrated exponential sweep keeps the phase continuous.
            float k = Mathf.Log(to / from) / duration;
            float phase = from * (Mathf.Exp(k * t) - 1f) / k;
            return Mathf.Sin(2f * Mathf.PI * phase);
        }

        private static float Decay(float t, float rate) => Mathf.Exp(-rate * t);
        private static float Attack(float t) => Mathf.Clamp01(t / 0.004f);

        private static float Envelope(float t, float duration, float attack) =>
            Mathf.Clamp01(t / attack) * Mathf.Clamp01((duration - t) / (duration * 0.6f));

        private static float Noise() => Random.value * 2f - 1f;
    }
}
