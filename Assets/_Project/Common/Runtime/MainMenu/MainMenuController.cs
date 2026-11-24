using System;
using System.Collections;
using PuzzlePair.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzlePair.MainMenu
{
    /// <summary>Game picker: one card per game with its campaign progress, plus sound / vibration toggles.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Serializable]
        public sealed class GameCard
        {
            public string GameId;
            public string SceneName;
            public int LevelCount;
            public Button Button;
            public TMP_Text Progress;
            public RectTransform Icon;
        }

        [SerializeField] private GameCard[] _cards = Array.Empty<GameCard>();
        [SerializeField] private ToggleButton _sound;
        [SerializeField] private ToggleButton _haptics;
        [SerializeField] private RectTransform _title;

        private void Start()
        {
            Application.targetFrameRate = 60;

            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card.Progress != null) card.Progress.text = ProgressText(card);
                if (card.Button != null)
                {
                    string scene = card.SceneName;
                    card.Button.onClick.AddListener(() => ScreenFader.LoadScene(scene));
                    StartCoroutine(PopIn((RectTransform)card.Button.transform, 0.08f + i * 0.09f));
                }
            }

            if (_sound != null) _sound.Bind(GameSettings.SoundOn, on => GameSettings.SoundOn = on);
            if (_haptics != null) _haptics.Bind(GameSettings.HapticsOn, on => GameSettings.HapticsOn = on);
            if (_title != null) StartCoroutine(PopIn(_title, 0f));
        }

        private static string ProgressText(GameCard card)
        {
            int cleared = Progress.GetCleared(card.GameId);
            if (cleared >= card.LevelCount && card.LevelCount > 0) return "Completed! Replay";
            return $"Level {Progress.GetLevel(card.GameId, card.LevelCount) + 1} / {card.LevelCount}";
        }

        private static IEnumerator PopIn(RectTransform target, float delay)
        {
            target.localScale = Vector3.zero;
            yield return new WaitForSecondsRealtime(delay);
            yield return Tween.Run(0.45f, t => target.localScale = Vector3.one * Easing.OutBack(t, 2f), null, true);
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            for (int i = 0; i < _cards.Length; i++)
            {
                var icon = _cards[i].Icon;
                if (icon == null) continue;
                float phase = time * 2.1f + i * 1.7f;
                icon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * 5f);
                icon.localScale = Vector3.one * (1f + Mathf.Sin(phase * 1.3f) * 0.035f);
            }
        }
    }
}
