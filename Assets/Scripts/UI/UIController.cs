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
