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
