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
