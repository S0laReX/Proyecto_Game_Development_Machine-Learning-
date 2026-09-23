using System;
using UnityEditor;
using UnityEngine;

public static class AdaptiveCellChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [MenuItem("Células/Verificar aprendizaje y límites")]
    public static void Run()
    {
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        CellConfig config = ScriptableObject.CreateInstance<CellConfig>();
        GameObject host = new GameObject("Prueba de evolución");
        try
        {
            UnityEngine.Random.InitState(12345);
            config.logStatistics = false;
            config.Validate();
            EvolutionManager manager = host.AddComponent<EvolutionManager>();
            manager.config = config;
            manager.Initialize();
            Check(FitnessEvaluator.Evaluate(true, 10, 10) > 0, "Supervivencia positiva");
            Check(FitnessEvaluator.Evaluate(false, 10, 10) < 0, "Eliminación negativa");
            CellGenome reference = new CellGenome(Color.red, config.minSize);
            CellMemory memory = new CellMemory(config);
            memory.Record(reference, 1);
            memory.Record(reference, -1);
            Check(!memory.TrySelect(out _), "El fracaso debe reducir la valoración");
            Check(memory.Count == 1, "Evitar duplicados cercanos");
            CellGenome[] last = new CellGenome[100];
            for (int round = 1; round <= 5; round++)
            {
                manager.BeginRound(round, 100);
                Check(manager.Exploited == (round == 1 ? 0 : 70), "Cuota de explotación");
                Check(manager.Explored == (round == 1 ? 100 : 30), "Cuota de exploración");
                int changed = 0, survived = 0;
                float mean = 0;
                for (int i = 0; i < 100; i++)
                {
                    CellGenome genome = manager.GenomeAt(i);
                    CheckBounds(genome, config);
                    if (round > 1 && genome.Distance(last[i], config) > 0.00001f) changed++;
                    last[i] = genome;
                    // Jugador simulado: elimina células grandes o con mucho rojo.
                    bool success = genome.size < 1.25f && genome.color.r < 0.5f;
                    if (success) survived++;
                    mean += genome.size;
                    manager.Record(genome, success, success ? 10 : 2, 10);
                }
                manager.EndRound();
                Check(manager.Survivors == survived && manager.Killed == 100 - survived, "Conteos");
                Check(round == 1 || changed > 0, "Los genes deben cambiar");
                Check(manager.Memory.Count <= config.memoryCapacity, "Memoria acotada");
                Debug.Log($"Prueba ronda {round}: sobreviven={survived}, genes cambiados={changed}, tamaño medio={mean / 100:F3}");
            }
            CellGenome stress = new CellGenome(new Color(-10, 20, 3), 100).Clamp(config);
            for (int i = 0; i < 10000; i++)
            { stress = stress.Mutated(config); CheckBounds(stress, config); }
            manager.BeginRound(6, 100);
            manager.CancelRound();
            manager.Record(reference, true, 10, 10);
            Check(manager.Survivors == 0, "Cancelar no produce supervivencia");
            Debug.Log("PASS: cinco generaciones, cuotas 70/30, castigos, diversidad de memoria, cancelación y 10000 mutaciones dentro de límites.");
        }
        finally
        {
            UnityEngine.Random.state = previous;
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    private static void CheckBounds(CellGenome genome, CellConfig config)
    {
        Check(genome.size >= config.minSize && genome.size <= config.maxSize, "Límite de tamaño");
        Check(genome.color.r >= 0 && genome.color.r <= 1 && genome.color.g >= 0 &&
            genome.color.g <= 1 && genome.color.b >= 0 && genome.color.b <= 1 && genome.color.a == 1,
            "Límites RGB y opacidad");
    }
}
