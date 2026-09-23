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
