using UnityEngine;

namespace PuzzlePair.Feedback
{
    /// <summary>
    /// Fits an orthographic camera to a world rectangle inside the area the HUD leaves free,
    /// and adds trauma-based screen shake and a zoom punch on top of the fitted pose.
    /// HUD insets are given in canvas pixels of the 1080-wide reference layout plus the safe area,
    /// so the fit is right on every aspect ratio.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        private const float ReferenceWidth = 1080f;

        [Tooltip("Canvas pixels (1080-wide reference) covered by the top HUD.")]
        [SerializeField] private float _topInset = 400f;
        [Tooltip("Canvas pixels (1080-wide reference) covered by the bottom buttons.")]
        [SerializeField] private float _bottomInset = 260f;
        [SerializeField] private float _margin = 0.5f;
        [SerializeField] private float _maxShake = 0.3f;

        private Camera _camera;
        private Rect _target;
        private bool _hasTarget;
        private Vector3 _basePosition;
        private float _baseSize;
        private float _trauma;
        private float _zoomPunch;
        private Vector2Int _screen;

        public Camera Camera => _camera != null ? _camera : _camera = GetComponent<Camera>();

        /// <summary>Frames <paramref name="worldRect"/> in the free band between the HUD bars.</summary>
        public void Fit(Rect worldRect)
        {
            _target = worldRect;
            _hasTarget = true;
            _screen = PixelSize();

            // Camera pixels, not Screen: also correct when rendering into a RenderTexture.
            float width = Mathf.Max(1, _screen.x);
            float height = Mathf.Max(1, _screen.y);
            float aspect = width / height;
            float scale = width / ReferenceWidth; // canvas is width-matched
            var safe = Camera.targetTexture == null && Screen.width == _screen.x && Screen.height == _screen.y
                ? Screen.safeArea
                : new Rect(0f, 0f, width, height);
            float top = (_topInset * scale + (height - safe.yMax)) / height;
            float bottom = (_bottomInset * scale + safe.yMin) / height;
            float free = Mathf.Max(0.2f, 1f - top - bottom);

            float w = worldRect.width + _margin * 2f;
            float h = worldRect.height + _margin * 2f;
            _baseSize = Mathf.Max(h / (2f * free), w / (2f * aspect));
            Camera.orthographicSize = _baseSize;

            // Shift the camera so the rect centre lands in the middle of the free band.
            float bandCentre = bottom + free * 0.5f; // viewport y
            float offset = (bandCentre - 0.5f) * 2f * _baseSize;
            Vector2 c = worldRect.center;
            _basePosition = new Vector3(c.x, c.y - offset, transform.position.z);
            transform.position = _basePosition;
        }

        private Vector2Int PixelSize() => new Vector2Int(Camera.pixelWidth, Camera.pixelHeight);

        /// <summary>Adds shake; trauma is squared so small hits stay subtle.</summary>
        public void Shake(float trauma) => _trauma = Mathf.Clamp01(_trauma + trauma);

        /// <summary>Quick zoom-in kick that eases back, used on wins.</summary>
        public void ZoomPunch(float amount) => _zoomPunch = amount;

        private void LateUpdate()
        {
            if (!_hasTarget) return;
            if (_screen != PixelSize()) Fit(_target);

            Vector3 offset = Vector3.zero;
            if (_trauma > 0f)
            {
                float s = _trauma * _trauma * _maxShake;
                float time = Time.unscaledTime * 38f;
                offset = new Vector3(Mathf.PerlinNoise(time, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, time) - 0.5f, 0f) * (2f * s);
                _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 2.2f);
            }
            transform.position = _basePosition + offset;

            if (_zoomPunch > 0f)
            {
                _zoomPunch = Mathf.Lerp(_zoomPunch, 0f, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
                if (_zoomPunch < 0.0005f) _zoomPunch = 0f;
                Camera.orthographicSize = _baseSize * (1f - _zoomPunch);
            }
        }
    }
}
