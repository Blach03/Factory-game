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
    private Dictionary<Vector2Int, Item> reservedOverheadGridSpots = new Dictionary<Vector2Int, Item>();
    private Dictionary<Vector2Int, Item> occupiedGridSpots = new Dictionary<Vector2Int, Item>();
    private Dictionary<Vector2Int, Item> occupiedOverheadGridSpots = new Dictionary<Vector2Int, Item>();
    private Dictionary<Vector2Int, List<GridObject>> allPlacedObjects = new Dictionary<Vector2Int, List<GridObject>>();

    private bool CleanupDeadReferences(Vector2Int gridPosition, List<GridObject> placedObjects)
    {
        if (placedObjects == null) return false;

        bool removedAny = false;
        for (int i = placedObjects.Count - 1; i >= 0; i--)
        {
            if (placedObjects[i] == null)
            {
                placedObjects.RemoveAt(i);
                removedAny = true;
            }
        }

        if (placedObjects.Count == 0)
        {
            allPlacedObjects.Remove(gridPosition);
        }

        return removedAny;
    }
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
        Time.fixedDeltaTime = 1f / 30f;
        Time.maximumDeltaTime = 1f / 10f;

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

    public bool IsOverheadGridSpotReserved(Vector2Int gridPosition)
    {
        return reservedOverheadGridSpots.ContainsKey(gridPosition);
    }

    public void ReserveGridSpot(Vector2Int gridPosition, Item item)
    {
        if (item != null && item.gameObject.layer == 11)
        {
            if (!reservedOverheadGridSpots.ContainsKey(gridPosition))
            {
                reservedOverheadGridSpots.Add(gridPosition, item);
            }
            return;
        }

        if (!reservedGridSpots.ContainsKey(gridPosition))
        {
            reservedGridSpots.Add(gridPosition, item);
        }
    }

    public void FinalizeItemPlacement(Vector2Int gridPosition, Item item = null)
    {
        if (item == null)
        {
            reservedGridSpots.Remove(gridPosition);
            reservedOverheadGridSpots.Remove(gridPosition);
            return;
        }

        if (item.gameObject.layer == 11)
        {
            if (reservedOverheadGridSpots.TryGetValue(gridPosition, out Item current) && current == item)
            {
                reservedOverheadGridSpots.Remove(gridPosition);
            }
        }
        else
        {
            if (reservedGridSpots.TryGetValue(gridPosition, out Item current) && current == item)
            {
                reservedGridSpots.Remove(gridPosition);
            }
        }
    }

    // --- Grid occupancy (stationary items) ---
    public void OccupyGridSpot(Vector2Int gridPosition, Item item)
    {
        occupiedGridSpots[gridPosition] = item;
    }

    public void ClearOccupiedGridSpot(Vector2Int gridPosition, Item item = null)
    {
        if (item == null)
        {
            occupiedGridSpots.Remove(gridPosition);
            return;
        }

        if (occupiedGridSpots.TryGetValue(gridPosition, out Item current) && current == item)
        {
            occupiedGridSpots.Remove(gridPosition);
        }
    }

    public bool IsGridSpotOccupied(Vector2Int gridPosition)
    {
        return occupiedGridSpots.ContainsKey(gridPosition);
    }

    public Item GetItemAtGridSpot(Vector2Int gridPosition)
    {
        occupiedGridSpots.TryGetValue(gridPosition, out Item item);
        return item;
    }

    public void OccupyOverheadGridSpot(Vector2Int gridPosition, Item item)
    {
        occupiedOverheadGridSpots[gridPosition] = item;
    }

    public void ClearOccupiedOverheadGridSpot(Vector2Int gridPosition, Item item = null)
    {
        if (item == null)
        {
            occupiedOverheadGridSpots.Remove(gridPosition);
            return;
        }

        if (occupiedOverheadGridSpots.TryGetValue(gridPosition, out Item current) && current == item)
        {
            occupiedOverheadGridSpots.Remove(gridPosition);
        }
    }

    public bool IsOverheadGridSpotOccupied(Vector2Int gridPosition)
    {
        return occupiedOverheadGridSpots.ContainsKey(gridPosition);
    }

    public Item GetOverheadItemAtGridSpot(Vector2Int gridPosition)
    {
        occupiedOverheadGridSpots.TryGetValue(gridPosition, out Item item);
        return item;
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
            CleanupDeadReferences(gridPosition, placedObjects);

            if (placedObjects.Count == 0)
            {
                return new List<GridObject>();
            }

            return placedObjects;
        }
        return new List<GridObject>();
    }
    public GridObject GetGridObject(Vector2Int gridPosition)
    {
        if (allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            CleanupDeadReferences(gridPosition, placedObjects);
            if (placedObjects.Count == 0)
            {
                return null;
            }

            GridObject lowerLayerObject = placedObjects.FirstOrDefault(obj => obj != null && obj.objectType != GridObjectType.OverheadConveyor);

            if (lowerLayerObject != null)
            {
                return lowerLayerObject;
            }

            return placedObjects.FirstOrDefault(obj => obj != null);
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
        if (item != null && item.gameObject.layer == 11)
        {
            if (!reservedOverheadGridSpots.ContainsKey(gridPosition))
            {
                reservedOverheadGridSpots.Add(gridPosition, item);
                return true;
            }
            return false;
        }

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

                    if (obj is PipeBuilding pipe)
                    {
                        pipe.UpdatePipeVisuals();
                        pipe.RefreshNetwork();
                    }
                }
            }
        }
    }

    public List<GridObject> GetAllGridObjects(Vector2Int gridPosition)
    {
        if (allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            CleanupDeadReferences(gridPosition, placedObjects);

            if (placedObjects.Count == 0)
            {
                return new List<GridObject>();
            }

            return placedObjects;
        }
        return new List<GridObject>();
    }

    public bool TryGetConveyorAt(Vector2Int gridPosition, out ConveyorBelt conveyor)
    {
        conveyor = null;

        if (!allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            return false;
        }

        CleanupDeadReferences(gridPosition, placedObjects);

        for (int i = 0; i < placedObjects.Count; i++)
        {
            ConveyorBelt current = placedObjects[i] as ConveyorBelt;
            if (current != null)
            {
                conveyor = current;
                return true;
            }
        }

        return false;
    }

    public bool TryGetOverheadConveyorAt(Vector2Int gridPosition, out OverheadConveyor conveyor, OverheadConveyor exclude = null)
    {
        conveyor = null;

        if (!allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            return false;
        }

        CleanupDeadReferences(gridPosition, placedObjects);

        for (int i = 0; i < placedObjects.Count; i++)
        {
            OverheadConveyor current = placedObjects[i] as OverheadConveyor;
            if (current == null || current == exclude)
            {
                continue;
            }

            conveyor = current;
            return true;
        }

        return false;
    }

    public bool TryGetLowerLayerObjectAt(Vector2Int gridPosition, out GridObject lowerLayerObject)
    {
        lowerLayerObject = null;

        if (!allPlacedObjects.TryGetValue(gridPosition, out List<GridObject> placedObjects))
        {
            return false;
        }

        CleanupDeadReferences(gridPosition, placedObjects);

        for (int i = 0; i < placedObjects.Count; i++)
        {
            GridObject current = placedObjects[i];
            if (current == null || current.objectType == GridObjectType.OverheadConveyor)
            {
                continue;
            }

            lowerLayerObject = current;
            return true;
        }

        return false;
    }

}