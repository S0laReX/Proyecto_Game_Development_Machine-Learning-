using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class CellController : MonoBehaviour
{
    private int arrayIndex;
    private CellSpawner spawnerRef;
    private SpriteRenderer spriteRenderer;
    private ICellAI cellAI;
    private bool resolved;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(int index, CellSpawner spawner, float size, Color color)
    {
        arrayIndex = index;
        spawnerRef = spawner;
        resolved = false;

        transform.localScale = new Vector3(size, size, 1f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void AssignAI(ICellAI ai)
    {
        cellAI = ai;
        cellAI?.Initialize(this);
    }

    public ICellAI GetAI()
    {
        return cellAI;
    }

    public void ApplyGenome(CellGenome genome)
    {
        transform.localScale = new Vector3(genome.size, genome.size, 1f);
        spriteRenderer.color = genome.color;
    }

    public void Resolve(bool survived)
    {
        if (resolved) return;
        resolved = true;
        if (survived) cellAI?.OnSurvive();
        else cellAI?.OnKilled();
    }

    private void OnMouseDown()
    {
        if (!resolved && spawnerRef != null)
        {
            spawnerRef.ReportCellKilled(arrayIndex);
        }
    }
}
