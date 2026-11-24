using System.Collections.Generic;
using UnityEngine;

namespace PuzzlePair.Audio
{
    public enum SfxId
    {
        Click,
        Whoosh,
        Bump,
        HeartLost,
        Hint,
        Undo,
        Pop,
        Shoot,
        Launch,
        Land,
        Slot,
        Warning,
        Spent,
        Win,
        Fail,
        Appear,
    }

    /// <summary>
    /// One-shot sound effects through a small round-robin pool of AudioSources.
    /// Created on first use and kept across scenes, so no scene needs an audio object.
    /// Rapid repeats of the same id are throttled to avoid phasing (pixel pops can fire many per frame).
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        private const int Voices = 12;
        private const float MinRepeatInterval = 0.025f;

        private static Sfx _instance;

        private AudioSource[] _sources;
        private int _next;
        private readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
        private readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();

        public static void Play(SfxId id, float pitch = 1f, float volume = 1f)
        {
            if (!GameSettings.SoundOn) return;
            Instance.PlayInternal(id, pitch, volume);
        }

        private static Sfx Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[Sfx]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Sfx>();
                return _instance;
            }
        }

        private void Awake()
        {
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
        }

        private void PlayInternal(SfxId id, float pitch, float volume)
        {
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < MinRepeatInterval) return;
            _lastPlayed[id] = now;

            if (!_clips.TryGetValue(id, out var clip))
            {
                clip = ProceduralAudio.Create(id);
                _clips[id] = clip;
            }
            if (clip == null) return;

            var source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            source.pitch = pitch * Random.Range(0.97f, 1.03f);
            source.PlayOneShot(clip, 0.8f * volume);
        }

        private void OnDestroy()
        {
            foreach (var clip in _clips.Values)
                if (clip != null) Destroy(clip);
            if (_instance == this) _instance = null;
        }
    }
}
