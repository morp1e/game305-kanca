using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor.Build
{
    public static class BuildScript
    {
        [MenuItem("Tools/Kanca/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Kanca.exe");

        [MenuItem("Tools/Kanca/Build/WebGL")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        static void Build(BuildTarget target, string output)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) Fail("Build Settings içinde etkin sahne yok.");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = target, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) Fail($"{target} build başarısız: {report.summary.result}, hata: {report.summary.totalErrors}.");
            Debug.Log($"{target} build tamamlandı: {output}, boyut: {report.summary.totalSize} bayt, süre: {report.summary.totalTime}.");
        }

        static void Fail(string message)
        {
            Debug.LogError(message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }
    }
}
