using UnityEngine;

public class ResourceDeposit : GridObject
{
    public ResourceData resourceData;

    protected override void Awake()
    {
        base.Awake();

        objectType = GridObjectType.ResourceDeposit;
        isBlockingPlacement = false;

        if (occupiedPosition == Vector2Int.zero && GridManager.Instance != null && GridManager.Instance.GetGridObject(Vector2Int.zero) == null)
        {

        }
    }

    public Item GetMinedItemPrefab()
    {
        if (resourceData == null)
        {
            Debug.LogError($"ResourceDeposit na polu {GetGridPosition()} nie ma przypisanych ResourceData!");
            return null;
        }

        if (resourceData.itemPrefab == null)
        {
            Debug.LogError($"ResourceData ({resourceData.resourceName}) nie ma przypisanego prefaba Item!");
            return null;
        }

        return resourceData.itemPrefab;
    }
}