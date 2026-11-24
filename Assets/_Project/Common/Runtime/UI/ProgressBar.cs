using UnityEngine;
using UnityEngine.UI;

namespace PuzzlePair.UI
{
    /// <summary>Horizontal bar that eases towards its value, with a small glow tick on increases.</summary>
    public sealed class ProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private float _speed = 10f;

        private float _value;
        private float _shown;

        public void SetValue(float value, bool instant = false)
        {
            _value = Mathf.Clamp01(value);
            if (instant)
            {
                _shown = _value;
                Apply();
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(_shown, _value)) return;
            _shown = Mathf.Lerp(_shown, _value, 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(_shown - _value) < 0.001f) _shown = _value;
            Apply();
        }

        private void Apply()
        {
            if (_fill == null) return;
            _fill.anchorMax = new Vector2(Mathf.Max(0.0001f, _shown), 1f);
            var image = _fill.GetComponent<Image>();
            if (image != null) image.enabled = _shown > 0.001f;
        }
    }
}
