using UnityEngine;

namespace PuzzlePair
{
    /// <summary>Player preferences shared by both games (persisted in PlayerPrefs).</summary>
    public static class GameSettings
    {
        private const string SoundKey = "settings.sound";
        private const string HapticsKey = "settings.haptics";

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool HapticsOn
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set { PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }

    /// <summary>
    /// Per-game campaign progress. <see cref="GetLevel"/> is the level to play next (0-based);
    /// after the last level the campaign loops so the game never dead-ends.
    /// </summary>
    public static class Progress
    {
        private static string LevelKey(string gameId) => $"{gameId}.level";
        private static string ClearedKey(string gameId) => $"{gameId}.cleared";

        public static int GetLevel(string gameId, int levelCount)
        {
            int level = PlayerPrefs.GetInt(LevelKey(gameId), 0);
            return levelCount > 0 ? Mathf.Clamp(level, 0, levelCount - 1) : 0;
        }

        /// <summary>Highest number of distinct levels ever cleared (for the menu badge).</summary>
        public static int GetCleared(string gameId) => PlayerPrefs.GetInt(ClearedKey(gameId), 0);

        /// <summary>Marks <paramref name="level"/> cleared and advances (looping). Returns the next level.</summary>
        public static int CompleteLevel(string gameId, int level, int levelCount)
        {
            int next = (level + 1) % levelCount;
            PlayerPrefs.SetInt(LevelKey(gameId), next);
            PlayerPrefs.SetInt(ClearedKey(gameId), Mathf.Max(GetCleared(gameId), Mathf.Min(level + 1, levelCount)));
            PlayerPrefs.Save();
            return next;
        }

        public static void SetLevel(string gameId, int level)
        {
            PlayerPrefs.SetInt(LevelKey(gameId), level);
            PlayerPrefs.Save();
        }

        public static void Reset(string gameId)
        {
            PlayerPrefs.DeleteKey(LevelKey(gameId));
            PlayerPrefs.DeleteKey(ClearedKey(gameId));
            PlayerPrefs.Save();
        }
    }

    /// <summary>Scene names and game ids, shared by the menu and the games.</summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
    }
}
