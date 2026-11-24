using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzlePair.UI
{
    /// <summary>
    /// Full-screen colour fade between scenes. Built at runtime on first use and kept alive,
    /// so scene transitions need no setup in any scene.
    /// </summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        private const float FadeTime = 0.22f;

        private static ScreenFader _instance;
        private CanvasGroup _group;
        private bool _busy;

        public static void LoadScene(string sceneName, Color? color = null)
        {
            var fader = Instance;
            if (fader._busy) return;
            fader.StartCoroutine(fader.Transition(sceneName, color ?? new Color(0.09f, 0.1f, 0.14f)));
        }

        private static ScreenFader Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[ScreenFader]");
                DontDestroyOnLoad(go);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                go.AddComponent<GraphicRaycaster>();
                _instance = go.AddComponent<ScreenFader>();
                _instance._group = go.AddComponent<CanvasGroup>();
                _instance._group.alpha = 0f;
                _instance._group.blocksRaycasts = false;

                var imageGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                imageGo.transform.SetParent(go.transform, false);
                var rt = (RectTransform)imageGo.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                return _instance;
            }
        }

        private IEnumerator Transition(string sceneName, Color color)
        {
            _busy = true;
            GetComponentInChildren<Image>().color = color;
            _group.blocksRaycasts = true;
            yield return Tween.Run(FadeTime, t => _group.alpha = t, Easing.InOutQuad, true);

            var load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone) yield return null;
            yield return null; // let the new scene run Start before revealing it

            yield return Tween.Run(FadeTime * 1.3f, t => _group.alpha = 1f - t, Easing.InOutQuad, true);
            _group.blocksRaycasts = false;
            _busy = false;
        }
    }
}
