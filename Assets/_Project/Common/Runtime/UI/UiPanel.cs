using System.Collections;
using UnityEngine;

namespace PuzzlePair.UI
{
    /// <summary>Modal panel: dim background fades in, the card pops with overshoot.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _card;

        private CanvasGroup _group;
        private Coroutine _routine;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            EnsureGroup();
            if (!IsOpen) HideImmediate();
        }

        private void EnsureGroup()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        public void Show()
        {
            EnsureGroup();
            IsOpen = true;
            gameObject.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Animate(true));
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (!gameObject.activeInHierarchy) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Animate(false));
        }

        public void HideImmediate()
        {
            IsOpen = false;
            EnsureGroup();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private IEnumerator Animate(bool show)
        {
            _group.blocksRaycasts = show;
            if (show)
            {
                yield return Tween.Run(0.32f, t =>
                {
                    _group.alpha = Mathf.Clamp01(t * 2.5f);
                    if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Easing.OutBack(t, 2.2f));
                }, null, true);
            }
            else
            {
                yield return Tween.Run(0.16f, t =>
                {
                    _group.alpha = 1f - t;
                    if (_card != null) _card.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t);
                }, null, true);
                gameObject.SetActive(false);
            }
            _routine = null;
        }
    }
}
