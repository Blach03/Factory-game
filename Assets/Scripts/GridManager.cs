using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    [SerializeField] public float tileSize = 1f;

    public Transform itemsContainer;

    private Dictionary<Vector2Int, GridObject> blockingObjects = new Dictionary<Vector2Int, GridObject>();
    private Dictionary<Vector2Int, ResourceDeposit> resourceDeposits = new Dictionary<Vector2Int, ResourceDeposit>();
    private Dictionary<Vector2Int, Item> reservedGridSpots = new Dictionary<Vector2Int, Item>(); // NOWE POLE
    private Dictionary<Vector2Int, List<GridObject>> allPlacedObjects = new Dictionary<Vector2Int, List<GridObject>>();
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (itemsContainer == null)
        {
            GameObject containerGO = GameObject.Find("--ITEMS--");
            if (containerGO == null)
            {
                containerGO = new GameObject("--ITEMS--");
                containerGO.transform.position = Vector3.zero;
            }
            itemsContainer = containerGO.transform;
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        float x = (float)gridPosition.x * tileSize + tileSize / 2f;
        float y = (float)gridPosition.y * tileSize + tileSize / 2f;

        return new Vector3(x, y, 0);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / tileSize);
        int y = Mathf.FloorToInt(worldPosition.y / tileSize);

        return new Vector2Int(x, y);
    }

    public bool IsPlacementBlocked(Vector2Int gridPosition)
    {
        return blockingObjects.ContainsKey(gridPosition);
    }


    public bool IsGridSpotReserved(Vector2Int gridPosition)
    {
        return reservedGridSpots.ContainsKey(gridPosition);
    }

    public void ReserveGridSpot(Vector2Int gridPosition, Item item)
    {
        if (!reservedGridSpots.ContainsKey(gridPosition))
        {
            reservedGridSpots.Add(gridPosition, item);
        }
    }

    public void FinalizeItemPlacement(Vector2Int gridPosition)
    {
        if (reservedGridSpots.ContainsKey(gridPosition))
        {
            reservedGridSpots.Remove(gridPosition);
        }
    }


    public ResourceDeposit GetResourceDeposit(Vector2Int gridPosition)
    {
        if (resourceDeposits.TryGetValue(gridPosition, out ResourceDeposit deposit))
        {
            return deposit;
        }
        return null;
    }

    public List<GridObject> GetGridObjects(Vector2Int gridPosition)
    {
        if (allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            return placedObjects;
        }
        return new List<GridObject>();
    }
    public GridObject GetGridObject(Vector2Int gridPosition)
    {
        if (allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            GridObject lowerLayerObject = placedObjects.FirstOrDefault(obj => obj.objectType != GridObjectType.OverheadConveyor);

            if (lowerLayerObject != null)
            {
                return lowerLayerObject;
            }

            return placedObjects.FirstOrDefault();
        }
        return null;
    }


    public void AddGridObject(GridObject gridObject, Vector2Int gridPosition)
    {
        if (!allPlacedObjects.ContainsKey(gridPosition))
        {
            allPlacedObjects.Add(gridPosition, new List<GridObject>());
        }

        allPlacedObjects[gridPosition].Add(gridObject);


        if (gridObject.objectType == GridObjectType.ResourceDeposit)
        {
            if (!resourceDeposits.ContainsKey(gridPosition))
            {
                resourceDeposits.Add(gridPosition, (ResourceDeposit)gridObject);
            }
        }

        if (gridObject.isBlockingPlacement || gridObject.objectType == GridObjectType.ConveyorBelt || gridObject.objectType == GridObjectType.OverheadConveyor)
        {
            if (gridObject.isBlockingPlacement && !blockingObjects.ContainsKey(gridPosition))
            {
                blockingObjects.Add(gridPosition, gridObject);
            }
        }
    }

    public bool TryReserveGridSpot(Vector2Int gridPosition, Item item)
    {
        if (!reservedGridSpots.ContainsKey(gridPosition))
        {
            reservedGridSpots.Add(gridPosition, item);
            return true;
        }
        return false;
    }

    public void RemoveGridObject(GridObject gridObject, Vector2Int gridPosition)
    {
        if (allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            placedObjects.Remove(gridObject);

            if (placedObjects.Count == 0)
            {
                allPlacedObjects.Remove(gridPosition);
            }
        }

        blockingObjects.Remove(gridPosition);
        resourceDeposits.Remove(gridPosition);
    }

    public void NotifyNeighborsOfChange(Vector2Int centerPosition)
    {
        Vector2Int[] neighbors = new Vector2Int[]
        {
        new Vector2Int(centerPosition.x + 1, centerPosition.y),
        new Vector2Int(centerPosition.x - 1, centerPosition.y),
        new Vector2Int(centerPosition.x, centerPosition.y + 1),
        new Vector2Int(centerPosition.x, centerPosition.y - 1)
        };

        foreach (Vector2Int pos in neighbors)
        {
            List<GridObject> objects = GetGridObjects(pos);
            if (objects != null)
            {
                foreach (GridObject obj in objects)
                {
                    if (obj.objectType == GridObjectType.OverheadConveyor)
                    {
                        OverheadConveyor conveyor = obj.GetComponent<OverheadConveyor>();
                        if (conveyor != null)
                        {
                            conveyor.OnNeighborChange();
                        }
                    }
                }
            }
        }
    }

    public List<GridObject> GetAllGridObjects(Vector2Int gridPosition)
    {
        if (allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            return placedObjects;
        }
        return new List<GridObject>();
    }

}