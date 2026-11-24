using UnityEngine;

namespace PuzzlePair.Fx
{
    /// <summary>Tutorial "tap here" marker in world space: a pulsing ring around a dot.</summary>
    public sealed class TapPrompt : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ring;
        [SerializeField] private SpriteRenderer _dot;
        [SerializeField] private float _size = 1f;

        private float _time;

        public void Show(Vector3 position, float size)
        {
            transform.position = position;
            _size = size;
            _time = 0f;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            _time += Time.deltaTime;
            float t = Mathf.Repeat(_time, 1.1f) / 1.1f;
            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * (_size * Mathf.Lerp(0.5f, 1.4f, Easing.OutCubic(t)));
                var c = _ring.color;
                c.a = 1f - t;
                _ring.color = c;
            }
            if (_dot != null)
                _dot.transform.localScale = Vector3.one * (_size * 0.32f * (1f + 0.15f * Mathf.Sin(_time * 8f)));
        }
    }
}
