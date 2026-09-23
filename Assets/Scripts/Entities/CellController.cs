using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class CellController : MonoBehaviour
{
    private int arrayIndex;
    private CellSpawner spawnerRef;
    private SpriteRenderer spriteRenderer;
    private ICellAI cellAI;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(int index, CellSpawner spawner, float size, Color color)
    {
        arrayIndex = index;
        spawnerRef = spawner;

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

    private void OnMouseDown()
    {
        if (spawnerRef != null)
        {
            spawnerRef.ReportCellKilled(arrayIndex);
        }
    }
}