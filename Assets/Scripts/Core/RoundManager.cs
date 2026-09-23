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