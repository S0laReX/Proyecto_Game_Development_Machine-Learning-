using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CellBenchmarkBuild
{
    // Ejecutar en un Editor separado con -batchmode -executeMethod CellBenchmarkBuild.Run -quit.
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Usa batchmode para no sustituir la escena abierta.");
        string temporaryScene = AssetDatabase.GenerateUniqueAssetPath("Assets/CellBenchmarkTemporary.unity");
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (game == null || game.cellSpawner == null) throw new Exception("Falta GameManager o CellSpawner");
            game.cellSpawner.maxCellsPerRound = 100;
            game.cellSpawner.useFullPopulation = true;
            game.roundManager.roundDuration = 10f;
            CellPerformanceProbe probe = game.gameObject.AddComponent<CellPerformanceProbe>();
            probe.spawner = game.cellSpawner;
            probe.quitWhenComplete = true;
            probe.runIntegrationChecks = true;
            EditorSceneManager.SaveScene(scene, temporaryScene);
            Directory.CreateDirectory("Builds/CellBenchmark");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { temporaryScene },
                locationPathName = "Builds/CellBenchmark/CellBenchmark.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Falló el build de medición");
        }
        finally
        {
            AssetDatabase.DeleteAsset(temporaryScene);
        }
    }
}
