using UnityEngine;

namespace PuzzlePair.Fx
{
    /// <summary>
    /// Screen-filling lighting layers that follow the camera: a soft top-lit gradient behind the
    /// scene and a gentle vignette over it, so flat colours read as a lit space.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _gradient;
        [SerializeField] private SpriteRenderer _vignette;

        private void LateUpdate()
        {
            if (_camera == null) return;
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            var p = _camera.transform.position;
            transform.position = new Vector3(p.x, p.y, 0f);
            var scale = new Vector3(width * 1.08f, height * 1.08f, 1f);
            if (_gradient != null) _gradient.transform.localScale = scale;
            if (_vignette != null) _vignette.transform.localScale = scale;
        }
    }
}
