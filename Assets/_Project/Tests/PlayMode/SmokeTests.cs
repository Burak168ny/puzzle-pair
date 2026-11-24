using System;
using System.Collections;
using ArrowSlide;
using ArrowSlide.Model;
using NUnit.Framework;
using PixelParade;
using PixelParade.Model;
using PuzzlePair.MainMenu;
using PuzzlePair.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PuzzlePair.PlayTests
{
    /// <summary>
    /// End-to-end smoke tests on the generated scenes: they play real levels through the same entry
    /// points as touch input. Any exception or error log during a test fails it.
    /// Requires the scenes from Tools > Puzzle Pair > Build All Scenes.
    /// </summary>
    public sealed class SmokeTests
    {
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
            Time.timeScale = 1f;
            for (int i = 0; i < Keys.Length; i++) PlayerPrefs.SetInt(Keys[i], _saved[i]);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator MainMenu_CardOpensItsGame()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return new WaitForSeconds(0.6f);
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>());

            var card = GameObject.Find("Arrow Slide");
            Assert.IsNotNull(card, "Arrow Slide card");
            card.GetComponent<Button>().onClick.Invoke();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "ArrowSlide", 5f);
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator ArrowSlide_BumpThenClearTheFirstBoard()
        {
            Progress.SetLevel(ArrowSlideGame.GameId, 0);
            yield return SceneManager.LoadSceneAsync("ArrowSlide");
            var game = Object.FindFirstObjectByType<ArrowSlideGame>();
            yield return WaitFor(() => game.AcceptsInput, 10f);

            var session = game.Session;
            int blocked = FindBlocked(session);
            Assert.GreaterOrEqual(blocked, 0, "level 1 should start with a blocked arrow");
            game.TapArrow(blocked);
            Assert.AreEqual(ArrowSession.DefaultLives - 1, session.Lives);
            yield return new WaitForSeconds(0.8f);

            while (session.State == SessionState.Playing)
            {
                game.TapArrow(session.FindHint());
                yield return new WaitForSeconds(0.12f);
            }
            Assert.AreEqual(SessionState.Won, session.State);
            yield return WaitFor(() => Panel("WinPanel").IsOpen, 5f);
            Assert.AreEqual(1, Progress.GetLevel(ArrowSlideGame.GameId, ArrowLevels.All.Count));
        }

        [UnityTest]
        public IEnumerator ArrowSlide_ThreeBumpsLose()
        {
            Progress.SetLevel(ArrowSlideGame.GameId, 1);
            yield return SceneManager.LoadSceneAsync("ArrowSlide");
            var game = Object.FindFirstObjectByType<ArrowSlideGame>();
            yield return WaitFor(() => game.AcceptsInput, 10f);

            int blocked = FindBlocked(game.Session);
            for (int i = 0; i < ArrowSession.DefaultLives; i++)
            {
                game.TapArrow(blocked);
                yield return new WaitForSeconds(0.8f);
            }
            Assert.AreEqual(SessionState.Lost, game.Session.State);
            yield return WaitFor(() => Panel("LosePanel").IsOpen, 5f);
        }

        [UnityTest]
        public IEnumerator PixelParade_LaunchEveryoneAndWinTheFirstPicture()
        {
            Progress.SetLevel(PixelParadeGame.GameId, 0);
            yield return SceneManager.LoadSceneAsync("PixelParade");
            var game = Object.FindFirstObjectByType<PixelParadeGame>();
            yield return WaitFor(() => game.AcceptsInput, 10f);

            game.UseSlotBooster();
            Assert.AreEqual(PixelLevels.All[0].Slots + 1, game.Sim.SlotCount);

            Time.timeScale = 4f;
            float end = Time.realtimeSinceStartup + 60f;
            while (game.Sim.State == SimState.Playing && Time.realtimeSinceStartup < end)
            {
                var sim = game.Sim;
                for (int lane = 0; lane < sim.LaneCount; lane++)
                    if (sim.LaneFront(lane) != null) game.TapShooter(sim.LaneFront(lane).Id);
                for (int slot = 0; slot < sim.SlotCount; slot++)
                    if (sim.SlotAt(slot) != null) game.TapShooter(sim.SlotAt(slot).Id);
                yield return null;
            }
            Assert.AreEqual(SimState.Won, game.Sim.State);
            yield return WaitFor(() => Panel("WinPanel").IsOpen, 15f);
            Assert.AreEqual(1, Progress.GetLevel(PixelParadeGame.GameId, PixelLevels.All.Count));
        }

        [UnityTest]
        public IEnumerator PixelParade_LastPicture_BotStylePlayDoesNotThrow()
        {
            Progress.SetLevel(PixelParadeGame.GameId, PixelLevels.All.Count - 1);
            yield return SceneManager.LoadSceneAsync("PixelParade");
            var game = Object.FindFirstObjectByType<PixelParadeGame>();
            yield return WaitFor(() => game.AcceptsInput, 10f);

            // Tap the back of a lane (denied) and spam launches: exercises deny, belt-full and slot paths.
            var sim = game.Sim;
            if (sim.Lane(0).Count > 1) game.TapShooter(sim.Lane(0)[1].Id);
            Time.timeScale = 4f;
            float end = Time.realtimeSinceStartup + 20f;
            while (sim.State == SimState.Playing && Time.realtimeSinceStartup < end)
            {
                for (int lane = 0; lane < sim.LaneCount; lane++)
                    if (sim.LaneFront(lane) != null) game.TapShooter(sim.LaneFront(lane).Id);
                yield return null;
            }
            Time.timeScale = 1f;
            if (sim.State == SimState.Lost) yield return WaitFor(() => Panel("LosePanel").IsOpen, 5f);
            if (sim.State == SimState.Won) yield return WaitFor(() => Panel("WinPanel").IsOpen, 15f);
        }

        // ------------------------------------------------------------------ helpers

        private static int FindBlocked(ArrowSession session)
        {
            foreach (var arrow in session.Board.Arrows)
                if (!session.Board.IsRemoved(arrow.Id) && !session.Board.Check(arrow.Id).IsFree) return arrow.Id;
            return -1;
        }

        private static ResultPanel Panel(string name)
        {
            foreach (var panel in Object.FindObjectsByType<ResultPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (panel.name == name) return panel;
            Assert.Fail($"No ResultPanel named {name}");
            return null;
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeoutSeconds)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for a condition.");
                yield return null;
            }
        }
    }
}
