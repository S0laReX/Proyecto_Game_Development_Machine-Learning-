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
