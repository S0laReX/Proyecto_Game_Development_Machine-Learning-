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
