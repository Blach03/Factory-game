using UnityEngine;
using System.Collections.Generic;

public class ConveyorBelt : GridObject
{
    [Header("Settings")]
    public float baseBeltSpeed = 2f;
    [SerializeField] private int blockedRetryDelayFrames = 2;

    public float CurrentBeltSpeed
    {
        get
        {
            if (TechTreeManager.Instance != null)
            {
                return baseBeltSpeed * TechTreeManager.Instance.GetConveyorSpeedMultiplier();
            }

            return baseBeltSpeed;
        }
    }

    public Direction travelDirection = Direction.Right;
    public enum Direction { Right, Down, Left, Up }

    private bool isHoldingItem = false;
    private Item heldItem = null;
    private int overheadDelayFrames = 0;
    private const int MAX_OVERHEAD_DELAY_FRAMES = 20;

    void OnEnable()
    {
        TransportTickManager.RegisterConveyor(this);
    }

    void OnDisable()
    {
        TransportTickManager.UnregisterConveyor(this);
    }

    // Conveyor logic is now queued by TransportTickManager.
    public void TickTransport(float deltaTime) { }

    protected override void Awake()
    {
        base.Awake();

        objectType = GridObjectType.ConveyorBelt;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    void Start()
    {
        UpdateVisualRotation();
        StartCoroutine(CheckForExistingItemDelayed());
    }

    private System.Collections.IEnumerator CheckForExistingItemDelayed()
    {
        yield return null;

        if (GridManager.Instance != null)
        {
            Item existing = GridManager.Instance.GetItemAtGridSpot(GetGridPosition());
            if (existing != null && !existing.isBeingMoved)
            {
                NotifyItemArrived(existing);
            }
        }
    }

    public void NotifyItemArrived(Item item)
    {
        heldItem = item;
        isHoldingItem = true;
        TransportTickManager.RequestConveyorTick(this, 0);

        OverheadConveyor overhead = GetOverheadConveyorOnThisSpot();
        if (overhead != null && overhead.IsStartSegment)
        {
            overhead.NotifyLowerLayerItemAvailable();
        }
    }

    public bool ProcessTransportStep(out int retryDelayFrames)
    {
        retryDelayFrames = Mathf.Max(1, blockedRetryDelayFrames);

        if (!isHoldingItem)
        {
            return false;
        }

        ConveyorStepResult result = ForceCheckForMovement();
        return result == ConveyorStepResult.Waiting;
    }

    public void RotateBelt(Direction newDirection)
    {
        travelDirection = newDirection;
        UpdateVisualRotation();
    }

    private OverheadConveyor GetOverheadConveyorOnThisSpot()
    {
        if (GridManager.Instance != null && GridManager.Instance.TryGetOverheadConveyorAt(GetGridPosition(), out OverheadConveyor conveyor))
        {
            return conveyor;
        }

        return null;
    }

    private bool CanOverheadAcceptItem(OverheadConveyor overhead)
    {
        if (overhead == null || GridManager.Instance == null)
        {
            return false;
        }

        Vector2Int overheadPos = overhead.GetGridPosition();
        if (GridManager.Instance.IsOverheadGridSpotOccupied(overheadPos) ||
            GridManager.Instance.IsOverheadGridSpotReserved(overheadPos))
        {
            return false;
        }

        Vector2Int nextGridPosition = GetPositionInDirection(overheadPos, overhead.travelDirection);
        OverheadConveyor nextConveyor = GetNeighborOverheadConveyor(nextGridPosition);

        if (nextConveyor != null && nextConveyor.travelDirection == overhead.travelDirection)
        {
            return !GridManager.Instance.IsOverheadGridSpotOccupied(nextGridPosition) &&
                   !GridManager.Instance.IsOverheadGridSpotReserved(nextGridPosition);
        }

        return true;
    }

    private OverheadConveyor GetNeighborOverheadConveyor(Vector2Int gridPos)
    {
        if (GridManager.Instance != null && GridManager.Instance.TryGetOverheadConveyorAt(gridPos, out OverheadConveyor conveyor))
        {
            return conveyor;
        }

        return null;
    }

    private Vector2Int GetPositionInDirection(Vector2Int currentGridPos, Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return new Vector2Int(currentGridPos.x, currentGridPos.y + 1);
            case Direction.Down: return new Vector2Int(currentGridPos.x, currentGridPos.y - 1);
            case Direction.Left: return new Vector2Int(currentGridPos.x - 1, currentGridPos.y);
            case Direction.Right: return new Vector2Int(currentGridPos.x + 1, currentGridPos.y);
            default: return currentGridPos;
        }
    }

    private enum ConveyorStepResult
    {
        Idle,
        Waiting,
        Moved
    }

    private ConveyorStepResult ForceCheckForMovement()
    {
        if (GridManager.Instance == null)
        {
            return ConveyorStepResult.Waiting;
        }

        Item itemToMove = heldItem;
        if (itemToMove == null || itemToMove.isBeingMoved || itemToMove.gameObject.layer != 8)
        {
            itemToMove = GridManager.Instance.GetItemAtGridSpot(GetGridPosition());
            if (itemToMove != null && (itemToMove.isBeingMoved || itemToMove.gameObject.layer != 8))
            {
                itemToMove = null;
            }
        }

        if (itemToMove == null)
        {
            isHoldingItem = false;
            heldItem = null;
            overheadDelayFrames = 0;
            return ConveyorStepResult.Idle;
        }

        if (overheadDelayFrames > 0)
        {
            overheadDelayFrames--;
            if (overheadDelayFrames > 0)
            {
                return ConveyorStepResult.Waiting;
            }
        }
        else
        {
            OverheadConveyor overhead = GetOverheadConveyorOnThisSpot();
            if (overhead != null && overhead.itemOnOverheadLayer == null && overhead.IsStartSegment && CanOverheadAcceptItem(overhead))
            {
                overheadDelayFrames = MAX_OVERHEAD_DELAY_FRAMES;
                return ConveyorStepResult.Waiting;
            }
        }

        Vector2Int nextGridPosition = GetNextGridPosition();
        Vector3 nextWorldPosition = GridManager.Instance.GridToWorld(nextGridPosition);

        if (IsOutputBlocked(nextWorldPosition))
        {
            return ConveyorStepResult.Waiting;
        }

        itemToMove.SetTargetPosition(nextWorldPosition, CurrentBeltSpeed);
        TutorialItemTracker.OnItemMovedByConveyor();
        isHoldingItem = false;
        heldItem = null;
        return ConveyorStepResult.Moved;
    }

    private bool IsOutputBlocked(Vector3 outputWorldPosition)
    {
        Vector2Int outputGridPosition = GridManager.Instance.WorldToGrid(outputWorldPosition);
        return GridManager.Instance.IsGridSpotReserved(outputGridPosition) ||
               GridManager.Instance.IsGridSpotOccupied(outputGridPosition);
    }

    private void UpdateVisualRotation()
    {
        float angle = 0f;
        switch (travelDirection)
        {
            case Direction.Up: angle = 0f; break;
            case Direction.Down: angle = 180f; break;
            case Direction.Left: angle = 90f; break;
            case Direction.Right: angle = -90f; break;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2Int GetNextGridPosition()
    {
        Vector2Int currentGridPos = GetGridPosition();
        switch (travelDirection)
        {
            case Direction.Up: return new Vector2Int(currentGridPos.x, currentGridPos.y + 1);
            case Direction.Down: return new Vector2Int(currentGridPos.x, currentGridPos.y - 1);
            case Direction.Left: return new Vector2Int(currentGridPos.x - 1, currentGridPos.y);
            case Direction.Right: return new Vector2Int(currentGridPos.x + 1, currentGridPos.y);
            default: return currentGridPos;
        }
    }

    [System.Serializable]
    public class BuildingSaveData
    {
        public int outputDirectionInt;
    }

    public override string GetSerializedData()
    {
        BuildingSaveData data = new BuildingSaveData
        {
            outputDirectionInt = (int)travelDirection
        };

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        BuildingSaveData data = JsonUtility.FromJson<BuildingSaveData>(json);
        travelDirection = (Direction)data.outputDirectionInt;
        RotateBelt(travelDirection);
    }
}
