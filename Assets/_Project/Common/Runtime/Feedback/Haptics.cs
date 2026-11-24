using UnityEngine;

namespace PuzzlePair.Feedback
{
    public enum HapticType
    {
        Light,
        Medium,
        Heavy,
        Success,
        Failure,
    }

    /// <summary>
    /// Short native vibrations. Android: <c>VibrationEffect.createOneShot</c> with amplitude (API 26+),
    /// plain <c>vibrate(ms)</c> below. iOS: only strong events use <c>Handheld.Vibrate</c> (no plugin).
    /// Editor and desktop: no-op. Respects <see cref="GameSettings.HapticsOn"/>.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdk;
        private static bool _initialized;
#endif

        public static void Play(HapticType type)
        {
            if (!GameSettings.HapticsOn) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(type);
#elif UNITY_IOS && !UNITY_EDITOR
            if (type == HapticType.Failure || type == HapticType.Heavy) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void PlayAndroid(HapticType type)
        {
            if (!_initialized) InitAndroid();

            long ms;
            int amplitude;
            switch (type)
            {
                case HapticType.Light: ms = 10; amplitude = 50; break;
                case HapticType.Medium: ms = 20; amplitude = 120; break;
                case HapticType.Heavy: ms = 35; amplitude = 220; break;
                case HapticType.Success: ms = 30; amplitude = 160; break;
                default: ms = 70; amplitude = 255; break;
            }

            try
            {
                if (_vibrator == null) return;
                if (_sdk >= 26)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", ms);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Haptics] {e.Message}");
                _vibrator = null;
            }
        }

        private static void InitAndroid()
        {
            _initialized = true;
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdk = version.GetStatic<int>("SDK_INT");
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Haptics] Vibrator unavailable: {e.Message}");
                _vibrator = null;
            }
        }
#endif
    }
}
