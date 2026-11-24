using System.Collections;
using System.Collections.Generic;
using PuzzlePair;
using UnityEngine;

namespace PixelParade
{
    /// <summary>The waiting pads under the belt. Pulses red when almost full.</summary>
    public sealed class SlotsView : MonoBehaviour
    {
        private readonly List<SpriteRenderer> _pads = new List<SpriteRenderer>();
        private PixelTheme _theme;
        private PixelLayout _layout;
        private bool _warning;

        public void Build(int count, PixelLayout layout, PixelTheme theme)
        {
            Clear();
            _theme = theme;
            _layout = layout;
            for (int i = 0; i < count; i++) AddPad(false);
            Arrange(false);
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var p in _pads)
                if (p != null) Destroy(p.gameObject);
            _pads.Clear();
            _warning = false;
        }

        /// <summary>Booster: a new pad pops in and the row re-centres.</summary>
        public void AddSlot()
        {
            AddPad(true);
            Arrange(true);
        }

        private void AddPad(bool animate)
        {
            var go = new GameObject($"Slot {_pads.Count}");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _theme.Rounded;
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = Vector2.one * (_theme.SlotSpacing - 0.18f);
            r.color = _theme.Slot;
            r.sortingOrder = -2;
            _pads.Add(r);
            if (animate) StartCoroutine(Tween.Run(0.35f, t => go.transform.localScale = Vector3.one * Easing.OutBack(t, 2.5f)));
        }

        private void Arrange(bool animate)
        {
            for (int i = 0; i < _pads.Count; i++)
            {
                var t = _pads[i].transform;
                Vector2 target = _layout.Slot(i, _pads.Count);
                if (animate) StartCoroutine(Slide(t, target));
                else t.localPosition = target;
            }
        }

        private static IEnumerator Slide(Transform t, Vector2 target)
        {
            Vector2 start = t.localPosition;
            if (start == Vector2.zero) start = target;
            yield return Tween.Run(0.25f, k => t.localPosition = Vector2.Lerp(start, target, Easing.OutCubic(k)));
        }

        public void SetWarning(bool on) => _warning = on;

        private void Update()
        {
            if (_theme == null) return;
            float pulse = _warning ? 0.5f + 0.5f * Mathf.Sin(Time.time * 9f) : 0f;
            var c = Color.Lerp(_theme.Slot, _theme.SlotWarning, pulse * 0.75f);
            foreach (var p in _pads) p.color = c;
        }
    }
}
