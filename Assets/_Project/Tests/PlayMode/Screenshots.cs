using System;
using System.Collections;
using System.IO;
using ArrowSlide;
using ArrowSlide.Model;
using NUnit.Framework;
using PixelParade;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PuzzlePair.PlayTests
{
    /// <summary>
    /// Renders portrait 1080x1920 screenshots of every scene (README media, visual review in CI).
    /// Skipped unless the PUZZLEPAIR_SHOTS environment variable names an output folder.
    /// </summary>
    public sealed class Screenshots
    {
        private static string Folder => Environment.GetEnvironmentVariable("PUZZLEPAIR_SHOTS");

        private static readonly string[] Keys =
        {
            ArrowSlideGame.GameId + ".level", ArrowSlideGame.GameId + ".cleared",
            PixelParadeGame.GameId + ".level", PixelParadeGame.GameId + ".cleared",
        };

        private readonly int[] _saved = new int[Keys.Length];

        [SetUp]
        public void SaveProgress()
        {
            for (int i = 0; i < Keys.Length; i++) _saved[i] = PlayerPrefs.GetInt(Keys[i], 0);
        }

        [TearDown]
        public void RestoreProgress()
        {
            for (int i = 0; i < Keys.Length; i++) PlayerPrefs.SetInt(Keys[i], _saved[i]);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator CaptureEveryScene()
        {
            if (string.IsNullOrEmpty(Folder))
            {
                Assert.Ignore("Set PUZZLEPAIR_SHOTS to an output folder to capture screenshots.");
                yield break;
            }
            Directory.CreateDirectory(Folder);

            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return new WaitForSeconds(1f);
            yield return Shot("menu");

            foreach (int level in new[] { 0, 2, 4 })
            {
                Progress.SetLevel(ArrowSlideGame.GameId, level);
                yield return SceneManager.LoadSceneAsync("ArrowSlide");
                var arrows = Object.FindFirstObjectByType<ArrowSlideGame>();
                yield return WaitFor(() => arrows.AcceptsInput);
                yield return Shot($"arrow_{level + 1}");
            }

            // Mid-move on the last board: one arrow escaping, one bumping.
            var game = Object.FindFirstObjectByType<ArrowSlideGame>();
            int blocked = -1;
            foreach (var a in game.Session.Board.Arrows)
                if (!game.Session.Board.Check(a.Id).IsFree) { blocked = a.Id; break; }
            game.TapArrow(game.Session.FindHint());
            if (blocked >= 0) game.TapArrow(blocked);
            yield return new WaitForSeconds(0.12f);
            yield return Shot("arrow_moves");

            while (game.Session.State == SessionState.Playing)
            {
                game.TapArrow(game.Session.FindHint());
                yield return new WaitForSeconds(0.05f);
            }
            yield return new WaitForSeconds(1.6f);
            yield return Shot("arrow_win");

            foreach (int level in new[] { 0, 2, 4 })
            {
                Progress.SetLevel(PixelParadeGame.GameId, level);
                yield return SceneManager.LoadSceneAsync("PixelParade");
                var pixels = Object.FindFirstObjectByType<PixelParadeGame>();
                yield return WaitFor(() => pixels.AcceptsInput);
                yield return Shot($"pixel_{level + 1}");

                var sim = pixels.Sim;
                for (int lane = 0; lane < sim.LaneCount; lane++)
                    if (sim.LaneFront(lane) != null) pixels.TapShooter(sim.LaneFront(lane).Id);
                yield return new WaitForSeconds(1.4f);
                for (int lane = 0; lane < sim.LaneCount; lane++)
                    if (sim.LaneFront(lane) != null) pixels.TapShooter(sim.LaneFront(lane).Id);
                yield return new WaitForSeconds(0.9f);
                yield return Shot($"pixel_{level + 1}_play");
            }
        }

        private static IEnumerator Shot(string name)
        {
            const int width = 1080, height = 1920;
            var cam = Camera.main;
            var rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var orders = new int[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                orders[i] = canvases[i].sortingOrder;
                if (!canvases[i].isRootCanvas || canvases[i].renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 5f;
                canvases[i].sortingOrder = 500 + orders[i];
            }

            cam.targetTexture = rt;
            yield return null; // camera rig refits to the texture, canvases rebuild
            yield return null;
            cam.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());

            cam.targetTexture = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                canvases[i].renderMode = modes[i];
                canvases[i].sortingOrder = orders[i];
            }
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            float end = Time.realtimeSinceStartup + 10f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out.");
                yield return null;
            }
        }
    }
}
