using UnityEngine;
using System.Collections.Generic;

public class OverheadConveyor : GridObject
{
    public float baseBeltSpeed = 4f;

    public float CurrentBeltSpeed => TechTreeManager.Instance != null
        ? baseBeltSpeed * TechTreeManager.Instance.GetConveyorSpeedMultiplier()
        : baseBeltSpeed;
    public ConveyorBelt.Direction travelDirection = ConveyorBelt.Direction.Right;

    private float checkTimer = 0f;
    public float checkInterval = 0.2f;

    private bool isStartSegment = false;
    private bool isEndSegment = false;

    public bool IsStartSegment => isStartSegment;

    public Item itemOnOverheadLayer = null;
    public Item itemToPickup = null;

    private const int ITEM_LAYER_ID = 8;
    private const int OVERHEAD_LAYER_ID = 11;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.OverheadConveyor;
        isBlockingPlacement = false;
        size = new Vector2Int(1, 1);

    }

    void Start()
    {
        UpdateVisualRotation();
        CheckChainState();
    }

    public override void Initialize(Vector2Int gridPosition)
    {
        base.Initialize(gridPosition);
        CheckChainState();
        if (GridManager.Instance != null)
        {
            GridManager.Instance.NotifyNeighborsOfChange(GetGridPosition());
        }
    }

    protected override void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.NotifyNeighborsOfChange(GetGridPosition());
        }
        base.OnDestroy();
    }

    public void OnNeighborChange()
    {
        CheckChainState();
    }

    public void RotateBelt(ConveyorBelt.Direction newDirection)
    {
        travelDirection = newDirection;

        UpdateVisualRotation();
        CheckChainState();

        if (GridManager.Instance != null)
        {
            GridManager.Instance.NotifyNeighborsOfChange(GetGridPosition());
        }
    }

    private void UpdateVisualRotation()
    {
        float angle = 0;
        switch (travelDirection)
        {
            case ConveyorBelt.Direction.Up: angle = 90; break;
            case ConveyorBelt.Direction.Down: angle = -90; break;
            case ConveyorBelt.Direction.Left: angle = 180; break;
            case ConveyorBelt.Direction.Right: angle = 0; break;
        }
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private Vector2Int GetPositionInDirection(Vector2Int currentGridPos, ConveyorBelt.Direction direction)
    {
        switch (direction)
        {
            case ConveyorBelt.Direction.Up: return new Vector2Int(currentGridPos.x, currentGridPos.y + 1);
            case ConveyorBelt.Direction.Down: return new Vector2Int(currentGridPos.x, currentGridPos.y - 1);
            case ConveyorBelt.Direction.Left: return new Vector2Int(currentGridPos.x - 1, currentGridPos.y);
            case ConveyorBelt.Direction.Right: return new Vector2Int(currentGridPos.x + 1, currentGridPos.y);
            default: return currentGridPos;
        }
    }

    private ConveyorBelt.Direction GetOppositeDirection(ConveyorBelt.Direction direction)
    {
        switch (direction)
        {
            case ConveyorBelt.Direction.Up: return ConveyorBelt.Direction.Down;
            case ConveyorBelt.Direction.Down: return ConveyorBelt.Direction.Up;
            case ConveyorBelt.Direction.Left: return ConveyorBelt.Direction.Right;
            case ConveyorBelt.Direction.Right: return ConveyorBelt.Direction.Left;
            default: return direction;
        }
    }

    private OverheadConveyor GetNeighborOverheadConveyor(Vector2Int gridPos)
    {
        if (GridManager.Instance == null) return null;

        List<GridObject> objects = GridManager.Instance.GetAllGridObjects(gridPos);

        if (objects == null) return null;

        for (int i = 0; i < objects.Count; i++)
        {
            OverheadConveyor conveyor = objects[i] as OverheadConveyor;
            if (conveyor != null && conveyor != this)
            {
                return conveyor;
            }
        }

        return null;
    }

    private void CheckChainState()
    {
        Vector2Int currentPos = GetGridPosition();

        Vector2Int prevPos = GetPositionInDirection(currentPos, GetOppositeDirection(travelDirection));
        OverheadConveyor prevConveyor = GetNeighborOverheadConveyor(prevPos);

        isStartSegment = !(prevConveyor != null && prevConveyor.travelDirection == travelDirection);

        Vector2Int nextPos = GetPositionInDirection(currentPos, travelDirection);
        OverheadConveyor nextConveyor = GetNeighborOverheadConveyor(nextPos);


        isEndSegment = !(nextConveyor != null && nextConveyor.travelDirection == travelDirection);

    }

    void OnEnable()
    {
        TransportTickManager.RegisterOverheadConveyor(this);
    }

    void OnDisable()
    {
        TransportTickManager.UnregisterOverheadConveyor(this);
    }

    public void TickTransport(float deltaTime)
    {
        checkTimer -= deltaTime;
        if (checkTimer <= 0f)
        {
            ForceCheckForMovement();
            checkTimer = checkInterval;
        }
    }

    private void ForceCheckForMovement()
    {
        Vector2Int currentPos = GetGridPosition();

        itemOnOverheadLayer = GridManager.Instance != null
            ? GridManager.Instance.GetOverheadItemAtGridSpot(currentPos)
            : null;

        if (isStartSegment)
        {
            itemToPickup = GridManager.Instance != null
                ? GridManager.Instance.GetItemAtGridSpot(currentPos)
                : null;
        }

        if (itemOnOverheadLayer != null && !itemOnOverheadLayer.isBeingMoved)
        {
            HandleOverheadMovement(itemOnOverheadLayer);
            return;
        }

        if (isStartSegment && itemToPickup != null && !itemToPickup.isBeingMoved)
        {
            if (CanPickupFromLowerLayer())
            {
                TryPickupItem(itemToPickup);
            }
        }
    }

    private bool CanPickupFromLowerLayer()
    {
        return true;
    }

    private void TryPickupItem(Item item)
    {
        Vector2Int currentGridPos = GetGridPosition();
        Vector2Int nextGridPosition = GetPositionInDirection(currentGridPos, travelDirection);

        OverheadConveyor nextConveyor = GetNeighborOverheadConveyor(nextGridPosition);

        if (nextConveyor != null && GridManager.Instance != null &&
            (GridManager.Instance.IsOverheadGridSpotOccupied(nextGridPosition) ||
             GridManager.Instance.IsOverheadGridSpotReserved(nextGridPosition)))
        {
            return;
        }

        item.SetLayerAndSortingOrderForOverhead();
        item.SetTargetPosition(transform.position, CurrentBeltSpeed);
        itemOnOverheadLayer = item;
        itemToPickup = null;
    }

    private void HandleOverheadMovement(Item itemToMove)
    {
        Vector2Int currentGridPos = GetGridPosition();
        Vector2Int nextGridPosition = GetPositionInDirection(currentGridPos, travelDirection);
        Vector3 nextWorldPosition = GridManager.Instance.GridToWorld(nextGridPosition);

        OverheadConveyor nextConveyor = GetNeighborOverheadConveyor(nextGridPosition);

        if (isEndSegment)
        {

            GridObject nextGridObj = GridManager.Instance.GetGridObject(nextGridPosition);

            bool canDropOnNextTile = nextGridObj == null || nextGridObj.objectType == GridObjectType.ConveyorBelt;

            if (canDropOnNextTile)
            {
                if (TryDropItem(itemToMove, nextGridPosition))
                {
                    itemOnOverheadLayer = null;
                }
            }
            return;
        }


        if (nextConveyor == null || nextConveyor.travelDirection != travelDirection)
        {
            return;
        }

        if (GridManager.Instance != null &&
            (GridManager.Instance.IsOverheadGridSpotOccupied(nextGridPosition) ||
             GridManager.Instance.IsOverheadGridSpotReserved(nextGridPosition)))
        {
            return;
        }

        itemToMove.SetTargetPosition(nextWorldPosition, CurrentBeltSpeed);
    }

    private bool TryDropItem(Item item, Vector2Int dropGridPosition)
    {
        Vector3 dropWorldPosition = GridManager.Instance.GridToWorld(dropGridPosition);

        if (IsLowerLayerBlocked(dropWorldPosition))
        {
            return false;
        }

        item.DropToConveyorAfterCurrentMove();
        item.SetTargetPosition(dropWorldPosition, CurrentBeltSpeed);

        return true;
    }

    private bool IsLowerLayerBlocked(Vector3 worldPosition)
    {
        if (GridManager.Instance == null) return false;

        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPosition);
        return GridManager.Instance.IsGridSpotReserved(gridPos) || GridManager.Instance.IsGridSpotOccupied(gridPos);
    }

    [System.Serializable]
    public class BuildingSaveData
    {
        public int outputDirectionInt;
    }

    public override string GetSerializedData()
    {
        BuildingSaveData data = new BuildingSaveData();

        // Rzutujemy Enum na int (Right=0, Down=1, itp.)
        data.outputDirectionInt = (int)this.travelDirection;

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        BuildingSaveData data = JsonUtility.FromJson<BuildingSaveData>(json);

        // Przywracamy Enum z inta i od�wie�amy wizualia strza�ki
        this.travelDirection = (ConveyorBelt.Direction)data.outputDirectionInt;

        // Wywo�ujemy Twoj� istniej�c� metod� wizualizacji
        RotateBelt(this.travelDirection);
    }
}