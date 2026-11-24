using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzlePair.UI
{
    /// <summary>Level complete / level failed card: title, subtitle, a badge that spins in, one main button.</summary>
    public sealed class ResultPanel : UiPanel
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private RectTransform _badge;
        [Tooltip("Optional light rays that spin behind the badge.")]
        [SerializeField] private RectTransform _rays;
        [SerializeField] private Button _primary;
        [SerializeField] private TMP_Text _primaryLabel;

        private Action _onPrimary;

        protected override void Awake()
        {
            base.Awake();
            if (_primary != null) _primary.onClick.AddListener(OnPrimary);
        }

        public void Show(string title, string subtitle, string buttonLabel, Action onPrimary)
        {
            if (_title != null) _title.text = title;
            if (_subtitle != null) _subtitle.text = subtitle;
            if (_primaryLabel != null) _primaryLabel.text = buttonLabel;
            _onPrimary = onPrimary;
            if (_primary != null) _primary.interactable = true;
            Show();
            if (_badge != null) StartCoroutine(BadgeIn());
            if (_rays != null) StartCoroutine(RaysIn());
        }

        private IEnumerator RaysIn()
        {
            _rays.localScale = Vector3.zero;
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Tween.Run(0.6f, t => _rays.localScale = Vector3.one * Easing.OutBack(t, 1.4f), null, true);
        }

        private IEnumerator BadgeIn()
        {
            _badge.localScale = Vector3.zero;
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Tween.Run(0.5f, t =>
            {
                _badge.localScale = Vector3.one * Easing.OutElastic(t);
                _badge.localRotation = Quaternion.Euler(0f, 0f, (1f - Easing.OutCubic(t)) * -90f);
            }, null, true);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_badge != null) _badge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2.2f) * 6f);
            if (_rays != null) _rays.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 22f);
        }

        private void OnPrimary()
        {
            if (_primary != null) _primary.interactable = false; // no double taps
            var action = _onPrimary;
            _onPrimary = null;
            Hide();
            action?.Invoke();
        }
    }
}
