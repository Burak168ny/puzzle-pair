using System.Collections;
using System.Collections.Generic;
using PixelParade.Model;
using PuzzlePair;
using PuzzlePair.Audio;
using PuzzlePair.Fx;
using UnityEngine;

namespace PixelParade
{
    /// <summary>
    /// The pixel picture: one glossy block per pixel (tinted bevel + white gloss layer) on a white
    /// frame. A light sweep runs across the blocks now and then. Pops are purely visual.
    /// </summary>
    public sealed class PictureView : MonoBehaviour
    {
        private const float BlockSize = 0.96f;
        private const float ShineAlpha = 0.6f;
        private const float SweepPeriod = 4.5f;
        private const float SweepSpeed = 26f; // diagonal cells per second

        private readonly Dictionary<int, SpriteRenderer> _blocks = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> _shines = new Dictionary<int, SpriteRenderer>();
        private readonly List<SpriteRenderer> _decor = new List<SpriteRenderer>();
        private PixelTheme _theme;
        private PixelLayout _layout;
        private PixelPicture _picture;
        private float _sweepClock;
        private bool _sweeping;

        /// <summary>Pixels the player has seen pop (lags the model by the projectile flight).</summary>
        public int Popped { get; private set; }

        public void Build(PixelPicture picture, PixelLayout layout, PixelTheme theme)
        {
            Clear();
            _theme = theme;
            _layout = layout;
            _picture = picture;

            var frameSize = new Vector2(picture.Width + 0.7f, picture.Height + 0.7f);
            _decor.Add(Sliced("FrameShadow", theme.Shadow, -4, frameSize + new Vector2(0.2f, 0.2f), new Vector2(0f, -0.28f)));
            _decor.Add(Sliced("Frame", theme.Frame, -3, frameSize, Vector2.zero));
            // A faint inner well makes the picture sit "in" the frame.
            _decor.Add(Sliced("Well", new Color(0f, 0f, 0f, 0.05f), -2, frameSize - new Vector2(0.3f, 0.3f), new Vector2(0f, -0.04f)));

            for (int y = 0; y < picture.Height; y++)
            for (int x = 0; x < picture.Width; x++)
            {
                int color = picture.Get(x, y);
                if (color < 0) continue;
                var go = new GameObject($"Px {x},{y}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = layout.Cell(x, y);
                go.transform.localScale = Vector3.one * BlockSize;
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = theme.Block;
                r.color = theme.ColorOf(color);
                r.sortingOrder = 0;

                var shineGo = new GameObject("Shine");
                shineGo.transform.SetParent(go.transform, false);
                var shine = shineGo.AddComponent<SpriteRenderer>();
                shine.sprite = theme.BlockShine;
                shine.color = new Color(1f, 1f, 1f, ShineAlpha);
                shine.sortingOrder = 1;

                int index = picture.Index(x, y);
                _blocks[index] = r;
                _shines[index] = shine;
            }
            _sweepClock = SweepPeriod - 0.6f;
        }

        private SpriteRenderer Sliced(string name, Color color, int order, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _theme.Rounded;
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = size;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        public void Clear()
        {
            StopAllCoroutines();
            transform.localScale = Vector3.one;
            foreach (var r in _blocks.Values)
                if (r != null) Destroy(r.gameObject);
            foreach (var r in _decor)
                if (r != null) Destroy(r.gameObject);
            _blocks.Clear();
            _shines.Clear();
            _decor.Clear();
            Popped = 0;
        }

        public Vector3 PixelWorld(int x, int y) => transform.TransformPoint(_layout.Cell(x, y));

        // ------------------------------------------------------------------ light sweep

        private void Update()
        {
            if (_picture == null || _shines.Count == 0) return;
            _sweepClock += Time.deltaTime;
            float span = _picture.Width + _picture.Height;
            float position = (_sweepClock - SweepPeriod) * SweepSpeed - 4f;
            if (_sweepClock >= SweepPeriod && position > span + 4f)
            {
                _sweepClock = 0f;
                if (_sweeping) ResetShine();
                _sweeping = false;
                return;
            }
            if (_sweepClock < SweepPeriod) return;

            _sweeping = true;
            foreach (var pair in _shines)
            {
                if (pair.Value == null) continue;
                float u = _picture.XOf(pair.Key) + (_picture.Height - 1 - _picture.YOf(pair.Key)) * 0.7f;
                float k = Mathf.Exp(-(u - position) * (u - position) / 3.5f);
                pair.Value.color = new Color(1f, 1f, 1f, ShineAlpha + (1f - ShineAlpha) * k);
            }
        }

        private void ResetShine()
        {
            foreach (var s in _shines.Values)
                if (s != null) s.color = new Color(1f, 1f, 1f, ShineAlpha);
        }

        /// <summary>Starts a light sweep right away (after the intro, on wins).</summary>
        public void Sweep() => _sweepClock = SweepPeriod;

        // ------------------------------------------------------------------ intro / pops / win

        /// <summary>Rows pop in from the bottom with a small overshoot, then light sweeps across.</summary>
        public IEnumerator PlayIntro()
        {
            foreach (var pair in _blocks) pair.Value.transform.localScale = Vector3.zero;
            float longest = 0f;
            foreach (var pair in _blocks)
            {
                int x = _picture.XOf(pair.Key), y = _picture.YOf(pair.Key);
                float delay = y * 0.03f + x * 0.01f;
                longest = Mathf.Max(longest, delay);
                StartCoroutine(PopIn(pair.Value.transform, delay));
            }
            yield return new WaitForSeconds(longest + 0.3f);
            Sweep();
        }

        private static IEnumerator PopIn(Transform t, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return Tween.Run(0.28f, k =>
            {
                if (t != null) t.localScale = Vector3.one * (BlockSize * Easing.OutBack(k, 2.2f));
            });
        }

        /// <summary>
        /// The hit pixel flashes white, swells and bursts into spinning cube debris and a sparkle.
        /// <paramref name="combo"/> (0..1) scales the flash and debris on fast chains.
        /// </summary>
        public void Pop(int x, int y, float pitch, float combo)
        {
            int index = _picture.Index(x, y);
            if (!_blocks.TryGetValue(index, out var block) || block == null) return;
            _blocks.Remove(index);
            _shines.Remove(index);
            Popped++;
            StartCoroutine(PopRoutine(block));

            var fx = FxPool.Instance;
            if (fx != null)
            {
                Vector2 p = block.transform.position;
                Color c = block.color;
                fx.Burst(p, c, 4 + Mathf.RoundToInt(combo * 3f), 8f + combo * 4f, 0.34f, 0.55f, -26f, FxShape.Block);
                fx.Burst(p, Color.Lerp(c, Color.white, 0.5f), 3, 5f, 0.16f, 0.35f, -10f, FxShape.Square);
                fx.Flash(p, new Color(1f, 1f, 1f, 0.85f), 0.5f, 1.4f + combo * 0.8f, 0.18f, FxShape.Circle);
                if (Random.value < 0.25f + combo * 0.4f) fx.Sparkles(p, new Color(1f, 0.97f, 0.8f), 1, 0.4f, 0.55f, 0.45f);
            }
            Sfx.Play(SfxId.Pop, pitch);
        }

        private static IEnumerator PopRoutine(SpriteRenderer block)
        {
            var t = block.transform;
            Color start = block.color;
            block.sortingOrder = 5;
            yield return Tween.Run(0.06f, k =>
            {
                t.localScale = Vector3.one * (BlockSize * (1f + 0.35f * k));
                block.color = Color.Lerp(start, Color.white, k * 0.85f);
            });
            yield return Tween.Run(0.1f, k => t.localScale = Vector3.one * (BlockSize * 1.35f * (1f - k)), Easing.InQuad);
            Destroy(block.gameObject);
        }

        /// <summary>Win: the empty frame flashes, bounces and throws sparkles.</summary>
        public IEnumerator PlayWinSweep()
        {
            if (_decor.Count < 2) yield break;
            var frame = _decor[1];
            Color baseColor = frame.color;
            var fx = FxPool.Instance;
            if (fx != null)
            {
                float hx = _picture.Width * 0.5f, hy = _picture.Height * 0.5f;
                for (int i = 0; i < 10; i++)
                    fx.Sparkles(transform.TransformPoint(new Vector2(Random.Range(-hx, hx), Random.Range(-hy, hy))), Color.white, 1, 0.5f, 1.1f, 0.8f);
            }
            StartCoroutine(Tween.Punch(transform, 0.06f, 0.4f, Vector3.one));
            yield return Tween.Run(0.6f, k => frame.color = Color.Lerp(new Color(1f, 0.98f, 0.85f), baseColor, k));
        }
    }
}
