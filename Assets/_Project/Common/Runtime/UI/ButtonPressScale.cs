using PuzzlePair.Audio;
using PuzzlePair.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzlePair.UI
{
    /// <summary>Squashes a button while held, then springs back; plays the click sound and a light tick.</summary>
    public sealed class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float _pressedScale = 0.9f;
        [SerializeField] private float _speed = 22f;

        private float _target = 1f;
        private float _velocity;
        private Selectable _selectable;

        private void Awake() => _selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            SetTarget(_pressedScale);
            Sfx.Play(SfxId.Click);
            Haptics.Play(HapticType.Light);
        }

        public void OnPointerUp(PointerEventData eventData) => SetTarget(1f);
        public void OnPointerExit(PointerEventData eventData) => SetTarget(1f);

        private void SetTarget(float target)
        {
            _target = target;
            enabled = true;
        }

        private void OnDisable()
        {
            transform.localScale = Vector3.one;
            _velocity = 0f;
            _target = 1f;
        }

        private void Update()
        {
            // Critically under-damped spring: a small overshoot on release feels "bouncy".
            float s = transform.localScale.x;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _velocity += (_target - s) * _speed * _speed * dt;
            _velocity *= Mathf.Exp(-_speed * 0.9f * dt);
            s += _velocity * dt;
            if (Mathf.Abs(s - _target) < 0.0015f && Mathf.Abs(_velocity) < 0.01f)
            {
                s = _target;
                _velocity = 0f;
                enabled = false;
            }
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
