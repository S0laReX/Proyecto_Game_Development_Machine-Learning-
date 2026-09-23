using UnityEngine;

public interface ICellAI 
{
    void Initialize(CellController cell);
    void OnSurvive();
    void OnKilled();
    void Mutate();
}