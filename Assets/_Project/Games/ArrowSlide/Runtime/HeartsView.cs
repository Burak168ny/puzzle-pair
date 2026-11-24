using System.Collections;
using PuzzlePair;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowSlide
{
    /// <summary>Row of hearts in the HUD. A lost heart punches, cracks and greys out.</summary>
    public sealed class HeartsView : MonoBehaviour
    {
        [SerializeField] private Image[] _hearts = new Image[0];
        [SerializeField] private Sprite _full;
        [SerializeField] private Sprite _broken;
        [SerializeField] private Color _fullColor = new Color(0.93f, 0.27f, 0.33f);
        [SerializeField] private Color _emptyColor = new Color(0.75f, 0.72f, 0.68f);

        /// <summary>Screen position of a heart (the canvas is screen-space overlay).</summary>
        public Vector2 HeartScreenPoint(int index) =>
            RectTransformUtility.WorldToScreenPoint(null, _hearts[Mathf.Clamp(index, 0, _hearts.Length - 1)].transform.position);

        public void SetLives(int lives, bool animate)
        {
            StopAllCoroutines();
            for (int i = 0; i < _hearts.Length; i++)
            {
                bool alive = i < lives;
                var heart = _hearts[i];
                heart.transform.localRotation = Quaternion.identity;
                if (animate && !alive && heart.sprite == _full)
                {
                    StartCoroutine(Break(heart));
                    continue;
                }
                heart.sprite = alive ? _full : _broken;
                heart.color = alive ? _fullColor : _emptyColor;
                heart.transform.localScale = Vector3.one;
                if (animate && alive) StartCoroutine(Tween.Punch(heart.transform, 0.08f, 0.25f, Vector3.one));
            }
        }

        private IEnumerator Break(Image heart)
        {
            var t = heart.transform;
            yield return Tween.Run(0.12f, k => t.localScale = Vector3.one * (1f + 0.45f * k), Easing.OutQuad);
            heart.sprite = _broken;
            heart.color = _emptyColor;
            yield return Tween.Run(0.35f, k =>
            {
                t.localScale = Vector3.one * Mathf.LerpUnclamped(1.45f, 1f, Easing.OutBack(k));
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 30f) * 14f * (1f - k));
            });
            t.localRotation = Quaternion.identity;
        }
    }
}
