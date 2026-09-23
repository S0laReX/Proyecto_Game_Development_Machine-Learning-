using System;
using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    [Header("Configuración de Spawn")]
    public GameObject cellPrefab;
    public int maxCellsPerRound = 20;
    public float spawnAreaWidth = 15f;
    public float spawnAreaHeight = 8f;

    [Header("Dependencias")]
    public ScoreManager scoreManager;

    private CellController[] activeCells;

    private void Awake()
    {
        activeCells = new CellController[maxCellsPerRound];
    }

    public void SpawnCellsForRound(int roundNumber)
    {
        int cellsToSpawn = Mathf.Min(5 + (roundNumber * 2), maxCellsPerRound);

        for (int i = 0; i < cellsToSpawn; i++)
        {
            Vector2 spawnPos = new Vector2(
                UnityEngine.Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f),
                UnityEngine.Random.Range(-spawnAreaHeight / 2f, spawnAreaHeight / 2f)
            );

            if (cellPrefab == null) return;

            GameObject cellObj = Instantiate(cellPrefab, spawnPos, Quaternion.identity);
            CellController cellController = cellObj.GetComponent<CellController>();

            if (cellController != null)
            {
                float randomSize = UnityEngine.Random.Range(0.5f, 2f);
                Color randomColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

                cellController.Setup(i, this, randomSize, randomColor);
                activeCells[i] = cellController;
            }
        }
    }

    public void ReportCellKilled(int index)
    {
        if (index >= 0 && index < activeCells.Length && activeCells[index] != null)
        {
            if (scoreManager != null)
            {
                scoreManager.AddScore(10);
            }
            Destroy(activeCells[index].gameObject);
            activeCells[index] = null;
        }
    }

    public void ClearAllCells()
    {
        for (int i = 0; i < activeCells.Length; i++)
        {
            if (activeCells[i] != null)
            {
                Destroy(activeCells[i].gameObject);
                activeCells[i] = null;
            }
        }
    }
}