# Código completo por archivo
Incluye los scripts de la segunda mitad, la integración, los contratos existentes y las pruebas. Sustituye cada archivo en su ruta; no crees clases duplicadas. Los scripts de Editor deben permanecer en Assets/Editor.

## Assets/Scripts/Entities/CellConfig.cs

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Células/Configuración adaptativa")]
public class CellConfig : ScriptableObject
{
    [Min(0.05f)] public float minSize = 0.5f;
    [Min(0.05f)] public float maxSize = 2f;
    [Range(0f, 1f)] public float exploitation = 0.7f;
    [Range(0.001f, 0.5f)] public float colorMutation = 0.12f;
    [Range(0.001f, 1f)] public float sizeMutation = 0.15f;
    [Range(0.001f, 0.5f)] public float diversityDistance = 0.08f;
    [Range(1, 1024)] public int memoryCapacity = 64;
    [Range(1, 1000)] public int maxCells = 100;
    public bool logStatistics = true;

    // También se valida en ejecución; el Inspector no es la única entrada.
    public void Validate()
    {
        minSize = Mathf.Max(0.05f, minSize);
        maxSize = Mathf.Max(minSize, maxSize);
        exploitation = Mathf.Clamp01(exploitation);
        colorMutation = Mathf.Clamp(colorMutation, 0.001f, 0.5f);
        sizeMutation = Mathf.Clamp(sizeMutation, 0.001f, 1f);
        diversityDistance = Mathf.Clamp(diversityDistance, 0.001f, 0.5f);
        memoryCapacity = Mathf.Clamp(memoryCapacity, 1, 1024);
        maxCells = Mathf.Clamp(maxCells, 1, 1000);
    }

    private void OnValidate() => Validate();
}
```

## Assets/Scripts/Entities/CellGenome.cs

```csharp
using System;
using UnityEngine;

[Serializable]
public struct CellGenome
{
    public Color color;
    public float size;

    public CellGenome(Color color, float size)
    {
        this.color = color;
        this.size = size;
    }

    public CellGenome Clamp(CellConfig config)
    {
        return new CellGenome(new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g),
            Mathf.Clamp01(color.b), 1f), Mathf.Clamp(size, config.minSize, config.maxSize));
    }

    public static CellGenome Random(CellConfig config)
    {
        return new CellGenome(new Color(UnityEngine.Random.value, UnityEngine.Random.value,
            UnityEngine.Random.value), UnityEngine.Random.Range(config.minSize, config.maxSize));
    }

    public CellGenome Mutated(CellConfig config)
    {
        float c = config.colorMutation;
        float s = config.sizeMutation * (config.maxSize - config.minSize);
        return new CellGenome(new Color(color.r + UnityEngine.Random.Range(-c, c),
            color.g + UnityEngine.Random.Range(-c, c), color.b + UnityEngine.Random.Range(-c, c)),
            size + UnityEngine.Random.Range(-s, s)).Clamp(config);
    }

    // Distancia normalizada entre cuatro genes; evita duplicados cercanos en memoria.
    public float Distance(CellGenome other, CellConfig config)
    {
        float r = color.r - other.color.r, g = color.g - other.color.g;
        float b = color.b - other.color.b;
        float s = (size - other.size) / Mathf.Max(0.001f, config.maxSize - config.minSize);
        return Mathf.Sqrt((r * r + g * g + b * b + s * s) / 4f);
    }
}
```

## Assets/Scripts/Entities/FitnessEvaluator.cs

```csharp
using UnityEngine;

public static class FitnessEvaluator
{
    // Sobrevivir vale +1; morir tarde reduce el castigo, pero nunca lo vuelve positivo.
    public static float Evaluate(bool survived, float aliveSeconds, float roundSeconds)
    {
        return survived ? 1f : -1f + 0.5f * Mathf.Clamp01(aliveSeconds / Mathf.Max(0.01f, roundSeconds));
    }
}
```

## Assets/Scripts/Entities/CellMemory.cs

```csharp
using UnityEngine;

public sealed class CellMemory
{
    private struct Entry
    {
        public CellGenome genome;
        public float fitness;
        public int samples;
    }

    private readonly Entry[] entries;
    private readonly CellConfig config;
    public int Count { get; private set; }

    public CellMemory(CellConfig config)
    {
        this.config = config;
        entries = new Entry[config.memoryCapacity];
    }

    public void Record(CellGenome genome, float fitness)
    {
        int nearest = -1;
        float distance = config.diversityDistance;
        for (int i = 0; i < Count; i++)
        {
            float d = genome.Distance(entries[i].genome, config);
            if (d <= distance) { nearest = i; distance = d; }
        }
        if (nearest >= 0)
        {
            Entry e = entries[nearest];
            // Media incremental: los fracasos también reducen el éxito de una región.
            e.samples++;
            e.fitness += (fitness - e.fitness) / e.samples;
            entries[nearest] = e;
            return;
        }
        if (fitness <= 0f) return;
        int slot = Count;
        if (Count == entries.Length)
        {
            slot = 0;
            for (int i = 1; i < Count; i++)
                if (entries[i].fitness < entries[slot].fitness) slot = i;
            if (fitness < entries[slot].fitness) return;
        }
        else Count++;
        entries[slot] = new Entry { genome = genome, fitness = fitness, samples = 1 };
    }

    public bool TryBest(out CellGenome genome, out float fitness)
    {
        genome = default;
        fitness = 0f;
        bool found = false;
        for (int i = 0; i < Count; i++)
            if (entries[i].fitness > fitness)
            {
                genome = entries[i].genome;
                fitness = entries[i].fitness;
                found = true;
            }
        return found;
    }

    public bool TrySelect(out CellGenome genome)
    {
        if (!TryBest(out genome, out float best)) return false;
        // Muestreo ponderado entre regiones con al menos el 80 % del mejor fitness.
        float total = 0f;
        for (int i = 0; i < Count; i++)
            if (entries[i].fitness >= best * 0.8f) total += entries[i].fitness;
        float pick = Random.value * total;
        for (int i = 0; i < Count; i++)
        {
            if (entries[i].fitness < best * 0.8f) continue;
            pick -= entries[i].fitness;
            if (pick <= 0f) { genome = entries[i].genome; return true; }
        }
        return true;
    }
}
```

## Assets/Scripts/Entities/EvolutionManager.cs

```csharp
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
```

## Assets/Scripts/Entities/AdaptiveCellAI.cs

```csharp
using UnityEngine;

public sealed class AdaptiveCellAI : ICellAI
{
    public CellGenome Genome { get; private set; }
    private CellController cell;
    private EvolutionManager evolution;
    private float bornAt, duration;
    private bool reported;

    public void Prepare(EvolutionManager manager, CellGenome genome, float roundDuration)
    {
        evolution = manager;
        Genome = genome.Clamp(manager.config);
        duration = roundDuration;
        bornAt = Time.time;
        reported = false;
    }

    public void Initialize(CellController controller)
    {
        cell = controller;
        cell.ApplyGenome(Genome);
    }

    public void OnSurvive() => Report(true);
    public void OnKilled() => Report(false);

    private void Report(bool survived)
    {
        if (reported || evolution == null) return;
        reported = true;
        evolution.Record(Genome, survived, Time.time - bornAt, duration);
    }

    public void Mutate()
    {
        // Los genes permanecen fijos durante la experiencia que se evalúa.
        if (evolution == null || evolution.IsRoundOpen) return;
        Genome = Genome.Mutated(evolution.config);
        if (cell != null) cell.ApplyGenome(Genome);
    }
}
```

## Assets/Scripts/Entities/ICellAI.cs

```csharp
using UnityEngine;

public interface ICellAI 
{
    void Initialize(CellController cell);
    void OnSurvive();
    void OnKilled();
    void Mutate();
}
```

## Assets/Scripts/Entities/CellController.cs

```csharp
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class CellController : MonoBehaviour
{
    private int arrayIndex;
    private CellSpawner spawnerRef;
    private SpriteRenderer spriteRenderer;
    private ICellAI cellAI;
    private bool resolved;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(int index, CellSpawner spawner, float size, Color color)
    {
        arrayIndex = index;
        spawnerRef = spawner;
        resolved = false;

        transform.localScale = new Vector3(size, size, 1f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void AssignAI(ICellAI ai)
    {
        cellAI = ai;
        cellAI?.Initialize(this);
    }

    public ICellAI GetAI()
    {
        return cellAI;
    }

    public void ApplyGenome(CellGenome genome)
    {
        transform.localScale = new Vector3(genome.size, genome.size, 1f);
        spriteRenderer.color = genome.color;
    }

    public void Resolve(bool survived)
    {
        if (resolved) return;
        resolved = true;
        if (survived) cellAI?.OnSurvive();
        else cellAI?.OnKilled();
    }

    private void OnMouseDown()
    {
        if (!resolved && spawnerRef != null)
        {
            spawnerRef.ReportCellKilled(arrayIndex);
        }
    }
}
```

## Assets/Scripts/Entities/CellSpawner.cs

```csharp
using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    [Header("Configuración de aparición")]
    public GameObject cellPrefab;
    [Min(1)] public int maxCellsPerRound = 20;
    public float spawnAreaWidth = 15f;
    public float spawnAreaHeight = 8f;
    [Tooltip("Usar toda la capacidad para la prueba con 100 células.")]
    public bool useFullPopulation;
    [Header("Dependencias")]
    public ScoreManager scoreManager;
    public EvolutionManager evolutionManager;

    private CellController[] pool;
    private CellController[] activeCells;
    private AdaptiveCellAI[] brains;
    public int ActiveCount { get; private set; }
    public int PoolCapacity => pool == null ? 0 : pool.Length;

    private bool EnsurePool()
    {
        if (pool != null) return true;
        if (cellPrefab == null || cellPrefab.GetComponent<CellController>() == null)
        {
            Debug.LogError("Asigna un prefab con CellController al CellSpawner.", this);
            return false;
        }
        if (evolutionManager == null) evolutionManager = GetComponent<EvolutionManager>();
        if (evolutionManager == null) evolutionManager = gameObject.AddComponent<EvolutionManager>();
        evolutionManager.Initialize();
        int capacity = Mathf.Clamp(maxCellsPerRound, 1, evolutionManager.config.maxCells);
        pool = new CellController[capacity];
        activeCells = new CellController[capacity];
        brains = new AdaptiveCellAI[capacity];
        for (int i = 0; i < capacity; i++)
        {
            pool[i] = Instantiate(cellPrefab, transform).GetComponent<CellController>();
            pool[i].gameObject.SetActive(false);
            brains[i] = new AdaptiveCellAI();
        }
        return true;
    }

    // Se conserva la llamada de la primera mitad.
    public void SpawnCellsForRound(int roundNumber) => SpawnCellsForRound(roundNumber, 10f);

    public void SpawnCellsForRound(int roundNumber, float duration)
    {
        if (!EnsurePool()) return;
        ClearAllCells();
        int count = useFullPopulation ? pool.Length : Mathf.Min(5 + Mathf.Max(0, roundNumber) * 2, pool.Length);
        evolutionManager.BeginRound(roundNumber, count);
        for (int i = 0; i < count; i++)
        {
            CellController cell = pool[i];
            // Activar primero garantiza Awake incluso con un prefab inactivo.
            cell.gameObject.SetActive(true);
            cell.transform.position = transform.position + new Vector3(
                Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f),
                Random.Range(-spawnAreaHeight / 2f, spawnAreaHeight / 2f), 0f);
            cell.Setup(i, this, 1f, Color.white);
            brains[i].Prepare(evolutionManager, evolutionManager.GenomeAt(i), duration);
            cell.AssignAI(brains[i]);
            activeCells[i] = cell;
        }
        ActiveCount = count;
    }

    public void ReportCellKilled(int index)
    {
        if (activeCells == null || index < 0 || index >= activeCells.Length || activeCells[index] == null) return;
        activeCells[index].Resolve(false);
        activeCells[index].gameObject.SetActive(false);
        activeCells[index] = null;
        ActiveCount--;
        if (scoreManager != null) scoreManager.AddScore(10);
    }

    public void FinishRound()
    {
        if (pool == null || !evolutionManager.IsRoundOpen) return;
        for (int i = 0; i < activeCells.Length; i++)
            if (activeCells[i] != null) activeCells[i].Resolve(true);
        evolutionManager.EndRound();
        ClearAllCells();
    }

    public void ClearAllCells()
    {
        if (pool == null) return;
        evolutionManager.CancelRound();
        for (int i = 0; i < activeCells.Length; i++)
        {
            if (activeCells[i] != null) activeCells[i].gameObject.SetActive(false);
            activeCells[i] = null;
        }
        ActiveCount = 0;
    }

    private void OnDisable() => ClearAllCells();
}
```

## Assets/Scripts/Core/RoundManager.cs

```csharp
using System;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public float roundDuration = 10f;
    private float currentTimer;
    private int currentRound;
    private bool isRoundActive = false;

    public event Action<int> OnRoundStarted;
    public event Action OnRoundEnded;
    public event Action<float> OnTimerUpdated;

    public void StartFirstRound()
    {
        currentRound = 0;
        StartNextRound();
    }

    public void StartNextRound()
    {
        currentRound++;
        currentTimer = roundDuration;
        isRoundActive = true;
        OnRoundStarted?.Invoke(currentRound);
    }

    private void Update()
    {
        if (!isRoundActive) return;

        currentTimer -= Time.deltaTime;
        OnTimerUpdated?.Invoke(Mathf.Max(currentTimer, 0f));

        if (currentTimer <= 0)
        {
            isRoundActive = false;
            OnRoundEnded?.Invoke();
        }
    }
}
```

## Assets/Scripts/Core/GameManager.cs

```csharp
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Manager References")]
    public RoundManager roundManager;
    public CellSpawner cellSpawner;
    public ScoreManager scoreManager;
    public UIController uiController;
    private bool nextRoundPending;

    private void Start()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundStarted += HandleRoundStart;
            roundManager.OnRoundEnded += HandleRoundEnd;
        }

        if (scoreManager != null && uiController != null)
        {
            scoreManager.OnScoreChanged += uiController.UpdateScore;
        }

        if (roundManager != null)
        {
            roundManager.StartFirstRound();
        }
    }

    private void HandleRoundStart(int roundNumber)
    {
        if (uiController != null) uiController.UpdateRound(roundNumber);
        if (cellSpawner != null) cellSpawner.SpawnCellsForRound(roundNumber, roundManager.roundDuration);
    }

    private void HandleRoundEnd()
    {
        if (cellSpawner != null) cellSpawner.FinishRound();
        nextRoundPending = true;
    }

    private void LateUpdate()
    {
        // Todos los oyentes del fin de ronda terminan antes de comenzar la siguiente.
        if (!nextRoundPending) return;
        nextRoundPending = false;
        if (roundManager != null) roundManager.StartNextRound();
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundStarted -= HandleRoundStart;
            roundManager.OnRoundEnded -= HandleRoundEnd;
        }

        if (scoreManager != null && uiController != null)
        {
            scoreManager.OnScoreChanged -= uiController.UpdateScore;
        }
    }
}
```

## Assets/Scripts/UI/UIController.cs

```csharp
using System;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("Referencias TextMeshPro")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;

    [Header("Dependencias")]
    public RoundManager roundManager;

    private void Start()
    {
        if (roundManager != null)
        {
            roundManager.OnTimerUpdated += UpdateTimer;
        }
        UpdateScore(0);
    }

    public void UpdateScore(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Puntuación: {newScore}";
    }

    public void UpdateRound(int roundNumber)
    {
        if (roundText != null)
            roundText.text = $"Ronda: {roundNumber}";
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
            timerText.SetText("Tiempo: {0:1}s", timeRemaining);
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnTimerUpdated -= UpdateTimer;
        }
    }
}
```

## Assets/Scripts/UI/ScoreManager.cs

```csharp
using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int CurrentScore { get; private set; }
    public event Action<int> OnScoreChanged;

    public void AddScore(int points)
    {
        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
```

## Assets/Scripts/Entities/CellPerformanceProbe.cs

```csharp
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(100)]
public class CellPerformanceProbe : MonoBehaviour
{
    public CellSpawner spawner;
    [Min(1f)] public float warmupSeconds = 5f;
    [Min(1f)] public float sampleSeconds = 50f;
    [Tooltip("Solo para el ejecutable de medición automática.")]
    public bool quitWhenComplete;
    public bool runIntegrationChecks;
    private float warmup, elapsed, worstFrame;
    private int frames, slowFrames, wrongPopulationFrames;
    private int renderedFrames, lastRenderedFrame = -1;
    private bool completed;

    private void Start()
    {
        RenderPipelineManager.endCameraRendering += OnCameraRendered;
        if (quitWhenComplete)
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 120;
        }
        if (!runIntegrationChecks) return;
        try { CellRuntimeChecks.Run(spawner); }
        catch (System.Exception error)
        {
            completed = true;
            Debug.LogException(error, this);
            if (quitWhenComplete) Application.Quit(2);
        }
    }

    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        if (frames == 0 || completed || lastRenderedFrame == Time.frameCount) return;
        lastRenderedFrame = Time.frameCount;
        renderedFrames++;
    }

    private void OnDestroy() => RenderPipelineManager.endCameraRendering -= OnCameraRendered;

    private void LateUpdate()
    {
        if (completed || spawner == null) return;
        float dt = Time.unscaledDeltaTime;
        warmup += dt;
        if (warmup < warmupSeconds) return;
        elapsed += dt;
        frames++;
        if (dt > worstFrame) worstFrame = dt;
        if (dt > 1f / 30f) slowFrames++;
        if (spawner.ActiveCount != 100) wrongPopulationFrames++;
        if (elapsed < sampleSeconds) return;
        completed = true;
        // LateUpdate precede al renderizado del último frame de la muestra.
        bool passed = slowFrames == 0 && wrongPopulationFrames == 0 && renderedFrames >= frames - 1;
        Debug.Log($"Rendimiento {(passed ? "PASS" : "FAIL")}: FPS medio={frames / elapsed:F1}, " +
            $"FPS mínimo={1f / worstFrame:F1}, frames >33.33ms={slowFrames}/{frames}, " +
            $"frames sin 100 células={wrongPopulationFrames}, frames renderizados={renderedFrames}. " +
            $"Resolución={Screen.width}x{Screen.height}, dispositivo={SystemInfo.graphicsDeviceName}", this);
        if (quitWhenComplete) Application.Quit(passed ? 0 : 1);
    }
}
```

## Assets/Scripts/Entities/CellRuntimeChecks.cs

```csharp
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
```

## Assets/Editor/AdaptiveCellChecks.cs

```csharp
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
```

## Assets/Editor/CellBenchmarkBuild.cs

```csharp
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
```
