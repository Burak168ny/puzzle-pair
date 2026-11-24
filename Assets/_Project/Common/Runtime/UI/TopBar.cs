using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzlePair.UI
{
    /// <summary>Shared in-game header: home button, level title and a one-line tip for tutorial levels.</summary>
    public sealed class TopBar : MonoBehaviour
    {
        [SerializeField] private Button _home;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private TMP_Text _tip;

        private Coroutine _tipRoutine;

        private void Awake()
        {
            if (_home != null) _home.onClick.AddListener(() => ScreenFader.LoadScene(SceneNames.MainMenu));
            if (_tip != null) _tip.alpha = 0f;
        }

        public void SetLevel(int levelIndex, string levelName)
        {
            if (_level == null) return;
            _level.text = string.IsNullOrEmpty(levelName) ? $"Level {levelIndex + 1}" : $"Level {levelIndex + 1}  <size=70%><alpha=#99>{levelName}";
            StartCoroutine(Tween.Punch(_level.transform, 0.18f, 0.35f, Vector3.one));
        }

        public void ShowTip(string text)
        {
            if (_tip == null) return;
            _tip.text = text;
            Fade(1f);
        }

        public void HideTip()
        {
            if (_tip == null || _tip.alpha <= 0f) return;
            Fade(0f);
        }

        private void Fade(float target)
        {
            if (_tipRoutine != null) StopCoroutine(_tipRoutine);
            _tipRoutine = StartCoroutine(FadeRoutine(target));
        }

        private IEnumerator FadeRoutine(float target)
        {
            float from = _tip.alpha;
            yield return Tween.Run(0.25f, t => _tip.alpha = Mathf.Lerp(from, target, t), Easing.OutQuad);
            _tipRoutine = null;
        }
    }
}
