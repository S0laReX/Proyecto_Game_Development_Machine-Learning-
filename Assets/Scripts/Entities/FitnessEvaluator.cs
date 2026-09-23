using UnityEngine;

public static class FitnessEvaluator
{
    // Sobrevivir vale +1; morir tarde reduce el castigo, pero nunca lo vuelve positivo.
    public static float Evaluate(bool survived, float aliveSeconds, float roundSeconds)
    {
        return survived ? 1f : -1f + 0.5f * Mathf.Clamp01(aliveSeconds / Mathf.Max(0.01f, roundSeconds));
    }
}
