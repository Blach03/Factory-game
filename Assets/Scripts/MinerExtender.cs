using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MinerExtender : GridObject
{
    public MinerBuilding.Direction outputDirection = MinerBuilding.Direction.Right;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    public void SetupRotation(MinerBuilding.Direction dir)
    {
        outputDirection = dir;
        UpdateVisualRotation();
        Debug.Log($"[Extender] Postawiony na {GetGridPosition()}, celuje w: {GetTargetGridPosition()}");
        NotifyNearbyMiners();
    }

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
        // Zaktualizowane na FindObjectsByType
        MinerBuilding[] allMiners = FindObjectsByType<MinerBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var miner in allMiners)
        {
            miner.RecalculateBoost();
        }
    }

    private void OnDestroy()
    {
        NotifyNearbyMiners();
    }

    [System.Serializable]
    public class ExtenderSaveData
    {
        public int outputDirectionInt;
    }

    public override string GetSerializedData()
    {
        ExtenderSaveData data = new ExtenderSaveData
        {
            outputDirectionInt = (int)outputDirection
        };
        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        ExtenderSaveData data = JsonUtility.FromJson<ExtenderSaveData>(json);
        outputDirection = (MinerBuilding.Direction)data.outputDirectionInt;
        UpdateVisualRotation();
    }
}
