using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PuzzlePair.Input
{
    /// <summary>
    /// Reads "pointer went down this frame" from the Input System for every touch (multi-touch taps
    /// are all reported) and for the mouse, skipping presses that land on UI.
    /// </summary>
    public static class PointerInput
    {
        private static readonly List<RaycastResult> Hits = new List<RaycastResult>();
        private static PointerEventData _eventData;
        private static EventSystem _eventSystem;

        /// <summary>Fills <paramref name="presses"/> with the screen positions pressed this frame.</summary>
        public static void GetPresses(List<Vector2> presses)
        {
            presses.Clear();

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame) continue;
                    Vector2 position = touch.position.ReadValue();
                    if (!IsOverUi(position)) presses.Add(position);
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 position = mouse.position.ReadValue();
                if (!IsOverUi(position) && !presses.Contains(position)) presses.Add(position);
            }
        }

        public static bool IsOverUi(Vector2 screenPosition)
        {
            var system = EventSystem.current;
            if (system == null) return false;
            if (_eventData == null || _eventSystem != system)
            {
                _eventSystem = system;
                _eventData = new PointerEventData(system);
            }
            _eventData.position = screenPosition;
            Hits.Clear();
            system.RaycastAll(_eventData, Hits);
            return Hits.Count > 0;
        }
    }
}
