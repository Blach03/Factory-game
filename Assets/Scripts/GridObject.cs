using System.Collections.Generic;
using UnityEngine;
using static PlacementManager;

public abstract class GridObject : SavableEntity
{
    public GridObjectType objectType = GridObjectType.Empty;
    public bool isBlockingPlacement = false;
    public Vector2Int occupiedPosition;
    public Vector2Int size = new Vector2Int(1, 1);
    public List<ResourceCost> constructionCost;

    // Musimy nadpisaæ Awake, aby wywo³aæ generowanie ID z klasy bazowej
    protected override void Awake()
    {
        base.Awake(); // To wywo³a Awake z SavableEntity
    }


    public virtual void Initialize(Vector2Int gridPosition)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager nie jest dostêpny podczas inicjalizacji GridObject.");
            return;
        }

        occupiedPosition = gridPosition;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tile = gridPosition + new Vector2Int(x, y);
                GridManager.Instance.AddGridObject(this, tile);
            }
        }

        float tileSize = GridManager.Instance.tileSize;
        float offsetX = (size.x - 1) * tileSize / 2f;
        float offsetY = (size.y - 1) * tileSize / 2f;

        Vector3 worldCenter = GridManager.Instance.GridToWorld(gridPosition);
        worldCenter.x += offsetX;
        worldCenter.y += offsetY;
        transform.position = worldCenter;

        gameObject.name = $"{objectType.ToString()} ({gridPosition.x},{gridPosition.y}) Size:{size.x}x{size.y}";
    }

    protected virtual void OnDestroy()
    {
        if (GridManager.Instance != null && occupiedPosition != Vector2Int.zero)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int tile = occupiedPosition + new Vector2Int(x, y);

                    GridManager.Instance.RemoveGridObject(this, tile);
                }
            }

            for (int x = -1; x <= size.x; x++)
            {
                for (int y = -1; y <= size.y; y++)
                {
                    GridManager.Instance.NotifyNeighborsOfChange(occupiedPosition + new Vector2Int(x, y));
                }
            }
        }
    }

    public Vector2Int GetGridPosition()
    {
        return occupiedPosition;
    }
}