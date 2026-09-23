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
