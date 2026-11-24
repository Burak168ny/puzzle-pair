using System;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzlePair.UI
{
    /// <summary>Icon button with an on/off state (a slash and dimmed icon when off).</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ToggleButton : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _offMark;

        private bool _value;
        private Action<bool> _onChanged;

        public void Bind(bool value, Action<bool> onChanged)
        {
            _value = value;
            _onChanged = onChanged;
            GetComponent<Button>().onClick.RemoveListener(Toggle);
            GetComponent<Button>().onClick.AddListener(Toggle);
            Refresh();
        }

        private void Toggle()
        {
            _value = !_value;
            Refresh();
            _onChanged?.Invoke(_value);
        }

        private void Refresh()
        {
            if (_offMark != null) _offMark.SetActive(!_value);
            if (_icon != null)
            {
                var c = _icon.color;
                c.a = _value ? 1f : 0.45f;
                _icon.color = c;
            }
        }
    }
}
