using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuzzlePair.Editor
{
    /// <summary>Small editor helpers: README screenshots and jumping to a level while testing.</summary>
    public static class DevTools
    {
        private const string MediaFolder = "docs/media";

        [MenuItem("Tools/Puzzle Pair/Capture Screenshot _F12", priority = 30)]
        public static void Capture()
        {
            Directory.CreateDirectory(MediaFolder);
            string path = Path.Combine(MediaFolder, $"shot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log($"[PuzzlePair] Screenshot will be written to {Path.GetFullPath(path)} at the end of the frame.");
        }

        [MenuItem("Tools/Puzzle Pair/Capture Screenshot _F12", true)]
        public static bool CanCapture() => EditorApplication.isPlaying;

        [MenuItem("Tools/Puzzle Pair/Start Every Game At Last Level", priority = 21)]
        public static void JumpToLastLevel()
        {
            foreach (var game in ProjectSetup.CreateAll<IGameEntry>())
                Progress.SetLevel(game.GameId, Mathf.Max(0, game.LevelCount - 1));
            Debug.Log("[PuzzlePair] Every game will start at its last level.");
        }
    }
}
