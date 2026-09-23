using System;
using UnityEngine;

public static class CellRuntimeChecks
{
    // Prueba de integración previa al calentamiento del benchmark; nunca corre por frame.
    public static void Run(CellSpawner spawner)
    {
        spawner.SpawnCellsForRound(1, 10f);
        CellController[] initial = spawner.GetComponentsInChildren<CellController>(true);
        if (initial.Length != 100) throw new Exception("El pool debe contener 100 células");
        for (int round = 1; round <= 5; round++)
        {
            spawner.SpawnCellsForRound(round, 10f);
            int initialScore = spawner.scoreManager == null ? 0 : spawner.scoreManager.CurrentScore;
            int killed = round == 3 ? 100 : 25;
            for (int i = 0; i < killed; i++)
            {
                spawner.ReportCellKilled(i);
                spawner.ReportCellKilled(i); // La segunda notificación no debe contar.
            }
            if (spawner.ActiveCount != 100 - killed) throw new Exception("Conteo activo incorrecto");
            if (spawner.scoreManager != null && spawner.scoreManager.CurrentScore != initialScore + killed * 10)
                throw new Exception("Puntuación duplicada");
            spawner.FinishRound();
            spawner.FinishRound(); // Tampoco debe contar dos veces a las supervivientes.
            EvolutionManager manager = spawner.evolutionManager;
            if (manager.Killed != killed || manager.Survivors != 100 - killed || spawner.ActiveCount != 0)
                throw new Exception("Resultados de ronda incorrectos");
            for (int i = 0; i < initial.Length; i++)
                if (initial[i].gameObject.activeSelf) throw new Exception("Célula no devuelta al pool");
        }
        CellController[] final = spawner.GetComponentsInChildren<CellController>(true);
        if (final.Length != initial.Length) throw new Exception("El pool creció");
        for (int i = 0; i < initial.Length; i++)
            if (initial[i] != final[i]) throw new Exception("Se recreó un objeto del pool");
        spawner.SpawnCellsForRound(1, 10f);
        Debug.Log("PASS integración: cinco rondas, 100 objetos reutilizados, muertes/supervivencia únicas, puntuación y población totalmente eliminada.");
    }
}
