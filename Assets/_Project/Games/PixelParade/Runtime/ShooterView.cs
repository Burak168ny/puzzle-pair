using System.Collections;
using PuzzlePair;
using PuzzlePair.Fx;
using TMPro;
using UnityEngine;

namespace PixelParade
{
    /// <summary>
    /// A round little shooter: body in its colour, a barrel that turns towards the picture,
    /// eyes that look where it aims, and its ammo count. Squash, stretch and recoil sell every action.
    /// </summary>
    public sealed class ShooterView : MonoBehaviour
    {
        private PixelTheme _theme;
        private Transform _visual;
        private Transform _barrelPivot;
        private Transform _barrel;
        private Transform[] _pupils;
        private Transform[] _eyes;
        private float _blinkTimer;
        private float _blink;
        private bool _vanishing;
        private SpriteRenderer _body;
        private SpriteRenderer _outline;
        private SpriteRenderer _shadow;
        private TextMeshPro _label;
        private Color _color;
        private Vector2 _facing = Vector2.up;
        private Vector2 _targetFacing = Vector2.up;
        private Coroutine _moveRoutine;
        private Coroutine _squashRoutine;
        private float _recoil;
        private bool _idle;
        private float _idlePhase;

        public int Id { get; private set; }
        public bool Moving => _moveRoutine != null;
        public Vector2 Position => transform.localPosition;

        public void Init(int id, int paletteIndex, int ammo, PixelTheme theme)
        {
            Id = id;
            _theme = theme;
            _color = theme.ColorOf(paletteIndex);
            float size = theme.ShooterSize;
            _idlePhase = id * 0.73f;

            _shadow = Sprite(transform, "Shadow", theme.SoftShadow, theme.Shadow, new Vector2(0f, -0.42f) * size, new Vector2(1.15f, 0.55f) * size, 8);

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _barrelPivot = new GameObject("BarrelPivot").transform;
            _barrelPivot.SetParent(_visual, false);
            Color dark = Color.Lerp(_color, Color.black, 0.35f);
            var barrel = Sprite(_barrelPivot, "Barrel", theme.Square, dark, new Vector2(0f, 0.48f) * size, new Vector2(0.3f, 0.36f) * size, 9);
            _barrel = barrel.transform;

            _outline = Sprite(_visual, "Outline", theme.Circle, dark, Vector2.zero, Vector2.one * size, 10);
            _body = Sprite(_visual, "Body", theme.Circle, _color, Vector2.zero, Vector2.one * (size * 0.84f), 11);
            Sprite(_visual, "Shine", theme.Circle, new Color(1f, 1f, 1f, 0.28f), new Vector2(-0.17f, 0.19f) * size, Vector2.one * (size * 0.3f), 12);

            _pupils = new Transform[2];
            _eyes = new Transform[2];
            _blinkTimer = Random.Range(1.5f, 4f);
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -0.15f : 0.15f) * size;
                _eyes[i] = Sprite(_visual, "Eye", theme.Circle, Color.white, new Vector2(x, 0.13f * size), Vector2.one * (size * 0.22f), 13).transform;
                _pupils[i] = Sprite(_visual, "Pupil", theme.Circle, new Color(0.12f, 0.13f, 0.18f), new Vector2(x, 0.13f * size), Vector2.one * (size * 0.11f), 14).transform;
            }

            var labelGo = new GameObject("Ammo");
            labelGo.transform.SetParent(_visual, false);
            labelGo.transform.localPosition = new Vector3(0f, -0.17f * size, 0f);
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 3.6f * size;
            _label.fontStyle = FontStyles.Bold;
            _label.color = Color.white;
            _label.outlineWidth = 0.25f;
            _label.outlineColor = (Color32)Color.Lerp(_color, Color.black, 0.55f);
            _label.rectTransform.sizeDelta = new Vector2(1.2f, 0.6f) * size;
            _label.sortingOrder = 15;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            SetAmmo(ammo, false);
        }

        private void Blink()
        {
            _blinkTimer -= Time.deltaTime;
            if (_blinkTimer <= 0f)
            {
                _blink = 0.14f;
                _blinkTimer = Random.Range(2f, 5f);
            }
            float open = 1f;
            if (_blink > 0f)
            {
                _blink -= Time.deltaTime;
                open = Mathf.Lerp(1f, 0.12f, Easing.Arc(Mathf.Clamp01(1f - _blink / 0.14f)));
            }
            float size = _theme.ShooterSize;
            for (int i = 0; i < _eyes.Length; i++)
            {
                _eyes[i].localScale = new Vector3(size * 0.22f, size * 0.22f * open, 1f);
                _pupils[i].localScale = new Vector3(size * 0.11f, size * 0.11f * open, 1f);
            }
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        // ------------------------------------------------------------------ state

        public void SetAmmo(int ammo, bool punch)
        {
            _label.text = ammo.ToString();
            if (punch) StartCoroutine(Tween.Punch(_label.transform, 0.35f, 0.15f, Vector3.one));
        }

        /// <summary>Front-of-lane and parked shooters bob gently: "tap me".</summary>
        public void SetIdle(bool idle) => _idle = idle;

        public void SetFacing(Vector2 direction, bool instant = false)
        {
            if (direction.sqrMagnitude < 1e-4f) return;
            _targetFacing = direction.normalized;
            if (instant) _facing = _targetFacing;
        }

        public void SetPosition(Vector2 position)
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
                _visual.localScale = Vector3.one; // a jump may have been cut mid-stretch
            }
            transform.localPosition = position;
        }

        /// <summary>Sorting boost while airborne or riding, so it passes over pads and the queue.</summary>
        public void SetLayer(int boost)
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = (r.sortingOrder % 100) + boost;
            _label.sortingOrder = 15 + boost;
        }

        // ------------------------------------------------------------------ motion

        public void MoveTo(Vector2 target, float duration)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine(target, duration, 0f));
        }

        public void JumpTo(Vector2 target, float duration, float height)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine(target, duration, height));
        }

        private IEnumerator MoveRoutine(Vector2 target, float duration, float height)
        {
            Vector2 start = transform.localPosition;
            if (height > 0f) Squash(new Vector2(1.2f, 0.8f), 0.08f);
            yield return Tween.Run(duration, t =>
            {
                Vector2 p = Vector2.LerpUnclamped(start, target, height > 0f ? Easing.InOutQuad(t) : Easing.OutCubic(t));
                p.y += Easing.Arc(t) * height;
                transform.localPosition = p;
                if (height > 0f) _visual.localScale = new Vector3(1f - 0.12f * Easing.Arc(t), 1f + 0.18f * Easing.Arc(t), 1f);
            });
            transform.localPosition = target;
            _visual.localScale = Vector3.one;
            if (height > 0f)
            {
                Squash(new Vector2(1.25f, 0.75f), 0.18f);
                Puff();
            }
            _moveRoutine = null;
        }

        private void Squash(Vector2 amount, float duration)
        {
            if (_squashRoutine != null) StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(SquashRoutine(amount, duration));
        }

        private IEnumerator SquashRoutine(Vector2 amount, float duration)
        {
            yield return Tween.Run(duration, t =>
            {
                float k = 1f - Easing.OutCubic(t);
                _visual.localScale = new Vector3(Mathf.Lerp(1f, amount.x, k), Mathf.Lerp(1f, amount.y, k), 1f);
            });
            _visual.localScale = Vector3.one;
            _squashRoutine = null;
        }

        /// <summary>Landing dust under the feet.</summary>
        public void Puff()
        {
            var fx = FxPool.Instance;
            if (fx == null) return;
            Vector2 feet = (Vector2)transform.position + Vector2.down * (0.4f * _theme.ShooterSize);
            fx.Burst(feet, new Color(1f, 1f, 1f, 0.75f), 5, 3.2f, 0.32f * _theme.ShooterSize, 0.35f, 0f, FxShape.Circle, Vector2.up, 160f);
            fx.Flash(feet, new Color(1f, 1f, 1f, 0.45f), 0.3f, 1.4f * _theme.ShooterSize, 0.25f);
        }

        /// <summary>Kick back from a shot and punch the barrel.</summary>
        public void Recoil(Vector2 towardTarget)
        {
            SetFacing(towardTarget, true);
            _recoil = 1f;
        }

        public void Deny()
        {
            StartCoroutine(DenyRoutine());
        }

        private IEnumerator DenyRoutine()
        {
            if (_moveRoutine == null) StartCoroutine(Tween.Shake(transform, 0.12f, 0.3f));
            yield return Tween.Run(0.3f, t => _body.color = Color.Lerp(_color, _theme.SlotWarning, Easing.Punch(t)));
            _body.color = _color;
        }

        /// <summary>Out of ammo: a happy pop.</summary>
        public IEnumerator Vanish()
        {
            _vanishing = true;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = null;
            yield return Tween.Run(0.14f, t => _visual.localScale = new Vector3(1f + 0.45f * t, 1f + 0.25f * t, 1f), Easing.OutQuad);
            var fx = FxPool.Instance;
            if (fx != null)
            {
                Vector2 p = transform.position;
                fx.Burst(p, _color, 14, 8f, 0.26f, 0.6f, -12f, FxShape.Circle);
                fx.Flash(p, Color.white, 0.6f, 2.2f, 0.2f, FxShape.Circle);
                fx.Flash(p, _color, 0.5f, 2.4f, 0.35f);
                fx.Sparkles(p, new Color(1f, 0.97f, 0.8f), 3, 0.6f, 0.7f, 0.6f);
            }
            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (_vanishing) return;
            Blink();

            // Smoothly turn the barrel and eyes towards the target direction.
            _facing = Vector3.Slerp(_facing, _targetFacing, 1f - Mathf.Exp(-14f * Time.deltaTime));
            float angle = Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg - 90f;
            _barrelPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 look = (Vector3)_facing * (0.045f * _theme.ShooterSize);
            for (int i = 0; i < _pupils.Length; i++)
                _pupils[i].localPosition = new Vector3((i == 0 ? -0.15f : 0.15f) * _theme.ShooterSize, 0.13f * _theme.ShooterSize, 0f) + look;

            if (_recoil > 0f)
            {
                _recoil = Mathf.Max(0f, _recoil - Time.deltaTime * 9f);
                float k = Easing.OutQuad(_recoil);
                _visual.localPosition = (Vector3)(-_facing * (0.13f * k));
                _barrel.localScale = new Vector3(0.3f * (1f + 0.4f * k), 0.36f * (1f - 0.3f * k), 1f) * _theme.ShooterSize;
            }
            else if (_idle && _moveRoutine == null)
            {
                // Gentle breathing: bob and squash in opposite phase.
                float bob = Mathf.Sin(Time.time * 4f + _idlePhase);
                _visual.localPosition = new Vector3(0f, 0.04f * bob, 0f);
                if (_squashRoutine == null) _visual.localScale = new Vector3(1f - 0.025f * bob, 1f + 0.035f * bob, 1f);
            }
            else if (_squashRoutine == null && _moveRoutine == null)
            {
                _visual.localPosition = Vector3.zero;
            }
        }
    }
}
