using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PuzzlePair.Editor
{
    /// <summary>
    /// One-click project setup: player settings, render settings, sprites and every scene
    /// (main menu + one per game), then the build list. Idempotent: run it any time.
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Tools/Puzzle Pair/Build All Scenes", priority = 0)]
        public static void BuildAllMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        /// <summary>Entry point for batch mode: <c>-executeMethod PuzzlePair.Editor.ProjectSetup.BuildAll</c>.</summary>
        public static void BuildAll()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
                Debug.LogError("[PuzzlePair] TextMeshPro Essential Resources are missing (Window > TextMeshPro > Import TMP Essential Resources).");

            ConfigurePlayer();
            ConfigureRendering();
            foreach (SpriteBaker.Shape shape in Enum.GetValues(typeof(SpriteBaker.Shape))) SpriteBaker.GetOrBake(shape, true);

            var builders = CreateAll<ISceneBuilder>().OrderBy(b => b.Order).ToList();
            foreach (var builder in builders)
            {
                builder.Build();
                Debug.Log($"[PuzzlePair] Built {builder.ScenePath}");
            }

            EditorBuildSettings.scenes = builders.Select(b => new EditorBuildSettingsScene(b.ScenePath, true)).ToArray();
            AssetDatabase.SaveAssets();
            if (builders.Count > 0 && !Application.isBatchMode) EditorSceneManager.OpenScene(builders[0].ScenePath);
            Debug.Log($"[PuzzlePair] {builders.Count} scenes ready. Open MainMenu and press Play.");
        }

        [MenuItem("Tools/Puzzle Pair/Reset Progress", priority = 20)]
        public static void ResetProgress()
        {
            foreach (var game in CreateAll<IGameEntry>()) Progress.Reset(game.GameId);
            Debug.Log("[PuzzlePair] Progress reset: every game starts from level 1.");
        }

        internal static List<T> CreateAll<T>()
        {
            var result = new List<T>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null) continue;
                result.Add((T)Activator.CreateInstance(type));
            }
            return result;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Puzzle Pair";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.portfolio.puzzlepair");
        }

        private static void ConfigureRendering()
        {
            var assets = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main) assets.Add(main);
            for (int i = 0; i < QualitySettings.count; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q) assets.Add(q);

            foreach (var asset in assets)
            {
                // Thin arrow lines alias without MSAA; 4x is cheap on tile-based mobile GPUs.
                asset.msaaSampleCount = 4;
                asset.supportsHDR = false;
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
