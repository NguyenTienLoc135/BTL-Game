#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BTLGame.EditorTools
{
    /// <summary>Build bản Windows để chơi thử luồng MainMenu → Gameplay (Tools ▸ CoTyPhu ▸ Build Playtest).</summary>
    public static class BuildPlaytest
    {
        private const string Output = "Builds/Playtest/CoTyPhuRPG.exe";

        [MenuItem("Tools/CoTyPhu/Build Playtest (Windows)")]
        public static void Build()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Game/Scenes/MainMenu.unity", "Assets/_Game/Scenes/Gameplay.unity" }, // index 0, 1 theo AGENTS.md
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[BuildPlaytest] " + report.summary.result + " — " + report.summary.totalErrors + " lỗi, " + report.summary.totalWarnings + " cảnh báo → " + Output);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
#endif
