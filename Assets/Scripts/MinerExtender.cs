using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MinerExtender : GridObject
{
    // Korzystamy z enum zdefiniowanego w MinerBuilding, aby unikn¹æ b³êdów rzutowania
    public MinerBuilding.Direction outputDirection = MinerBuilding.Direction.Right;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    /// <summary>
    /// Metoda wywo³ywana przez PlacementManager podczas stawiania budynku, 
    /// aby przekazaæ rotacjê wybran¹ w podgl¹dzie (preview).
    /// </summary>
    public void SetupRotation(MinerBuilding.Direction dir)
    {
        outputDirection = dir;
        UpdateVisualRotation();

        // Log diagnostyczny - poka¿e w konsoli, gdzie dok³adnie celuje extender
        Debug.Log($"[Extender] Postawiony na {GetGridPosition()}, celuje w: {GetTargetGridPosition()}");

        NotifyNearbyMiners();
    }

    /// <summary>
    /// Metoda do obracania ju¿ postawionego budynku (np. klawiszem R)
    /// </summary>
    public void RotateBuilding(MinerBuilding.Direction newDirection)
    {
        outputDirection = newDirection;
        UpdateVisualRotation();
        NotifyNearbyMiners();
    }

    public void UpdateVisualRotation()
    {
        float angle = 0;
        switch (outputDirection)
        {
            case MinerBuilding.Direction.Up: angle = 180; break;
            case MinerBuilding.Direction.Down: angle = 0; break;
            case MinerBuilding.Direction.Left: angle = -90; break;
            case MinerBuilding.Direction.Right: angle = 90; break;
        }
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Twoja oryginalna metoda obliczaj¹ca pole, na które wskazuje extender.
    /// </summary>
    public Vector2Int GetTargetGridPosition()
    {
        Vector2Int pos = GetGridPosition();
        switch (outputDirection)
        {
            case MinerBuilding.Direction.Up: return pos + Vector2Int.up;
            case MinerBuilding.Direction.Down: return pos + Vector2Int.down;
            case MinerBuilding.Direction.Left: return pos + Vector2Int.left;
            case MinerBuilding.Direction.Right: return pos + Vector2Int.right;
            default: return pos;
        }
    }

    private void NotifyNearbyMiners()
    {
        // Znajduje wszystkie minery i wymusza przeliczenie boosta
        MinerBuilding[] allMiners = FindObjectsOfType<MinerBuilding>();
        foreach (var miner in allMiners)
        {
            miner.RecalculateBoost();
        }
    }

    private void OnDestroy()
    {
        NotifyNearbyMiners();
    }
}