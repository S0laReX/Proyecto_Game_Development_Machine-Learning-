using UnityEngine;

public class EvolutionManager : MonoBehaviour
{
    public CellConfig config;
    public CellMemory Memory { get; private set; }
    public int Survivors { get; private set; }
    public int Killed { get; private set; }
    public int Exploited { get; private set; }
    public int Explored { get; private set; }
    public float MeanFitness => (Survivors + Killed) == 0 ? 0f : fitnessSum / (Survivors + Killed);
    public bool IsRoundOpen { get; private set; }
    private bool ownsConfig;
    private int round, population;
    private float fitnessSum, sizeSum;
    private Color colorSum;
    private CellGenome[] generation;

    public void Initialize()
    {
        if (Memory != null) return;
        if (config == null) { config = ScriptableObject.CreateInstance<CellConfig>(); ownsConfig = true; }
        config.Validate();
        Memory = new CellMemory(config);
        generation = new CellGenome[config.maxCells];
    }

    public void BeginRound(int number, int count)
    {
        Initialize();
        round = number;
        population = Mathf.Clamp(count, 0, generation.Length);
        Survivors = Killed = Exploited = Explored = 0;
        fitnessSum = sizeSum = 0f;
        colorSum = Color.clear;
        IsRoundOpen = true;
        int quota = Mathf.RoundToInt(population * config.exploitation);
        for (int i = 0; i < population; i++)
        {
            if (i < quota && Memory.TrySelect(out CellGenome parent))
            { generation[i] = parent.Mutated(config); Exploited++; }
            else { generation[i] = CellGenome.Random(config); Explored++; }
        }
        // Mezclar impide asociar exploración con una posición fija del pool.
        for (int i = population - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CellGenome temp = generation[i]; generation[i] = generation[j]; generation[j] = temp;
        }
    }

    public CellGenome GenomeAt(int index) => generation[index];

    public void Record(CellGenome genome, bool survived, float aliveSeconds, float duration)
    {
        if (!IsRoundOpen) return;
        float fitness = FitnessEvaluator.Evaluate(survived, aliveSeconds, duration);
        Memory.Record(genome, fitness);
        fitnessSum += fitness;
        if (survived) { Survivors++; sizeSum += genome.size; colorSum += genome.color; }
        else Killed++;
    }

    public void EndRound()
    {
        if (!IsRoundOpen) return;
        IsRoundOpen = false;
        if (!config.logStatistics) return;
        string best = Memory.TryBest(out CellGenome genome, out float fitness)
            ? $"RGB={genome.color}, tamaño={genome.size:F2}, fitness={fitness:F2}" : "sin genomas exitosos";
        string survivors = Survivors > 0
            ? $"RGB medio={colorSum / Survivors}, tamaño medio={sizeSum / Survivors:F2}" : "ninguna";
        Debug.Log($"Ronda {round}: sobreviven {Survivors}/{population}, eliminadas {Killed}, " +
            $"fitness medio={MeanFitness:F2}, explotación/exploración={Exploited}/{Explored}. " +
            $"Supervivientes: {survivors}. Mejor región histórica: {best}", this);
    }

    // Una limpieza o cancelación no equivale a supervivencia.
    public void CancelRound() => IsRoundOpen = false;

    private void OnDestroy()
    {
        if (ownsConfig && config != null) Destroy(config);
    }
}
