using UnityEngine;
using System.Collections.Generic;

public class Item : SavableEntity
{
    public ResourceData itemData;
    public float defaultMoveSpeed = 4f;
    public bool isBeingMoved = false;

    private const int CONVEYOR_SORTING_ORDER = 10;
    private const int OVERHEAD_SORTING_ORDER = 50;

    private const int CONVEYOR_LAYER_ID = 8;
    private const int OVERHEAD_LAYER_ID = 11;

    public bool isOnOverheadLayer = false;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D itemCollider;

    private Vector3 targetWorldPosition;
    private float currentMoveSpeed;
    private Vector2Int currentReservedGridPos = Vector2Int.zero;
    private bool dropToConveyorOnArrival = false;

    protected override void Awake()
    {
        base.Awake();

        // Disable 2D physics before the first simulation step.
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.simulated = false;
        }

        // Physics callbacks are not used for item transport.
        itemCollider = GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Preserve the serialized layer set by SaveManager during load.
        // If this object was saved on overhead, do not force it back to conveyor.
        if (gameObject.layer == OVERHEAD_LAYER_ID)
        {
            SetLayerAndSortingOrderForOverhead();
        }
        else
        {
            SetLayerAndSortingOrderForConveyor();
        }

        // Register occupation for items that start stationary (e.g. loaded from a save).
        if (!isBeingMoved)
        {
            RegisterCurrentGridOccupation();

            // Kick-start belts after load so stationary items don't wait for incidental updates.
            if (gameObject.layer == CONVEYOR_LAYER_ID && GridManager.Instance != null)
            {
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(transform.position);
                ConveyorBelt belt = GetConveyorAtGridPosition(gridPos);
                if (belt != null)
                {
                    belt.NotifyItemArrived(this);
                }
            }
        }
    }

    void OnEnable()
    {
        if (isBeingMoved)
        {
            TransportTickManager.RegisterMovingItem(this);
        }
    }

    void OnDisable()
    {
        TransportTickManager.UnregisterMovingItem(this);
    }

    public void Initialize(ResourceData data)
    {
        itemData = data;
        gameObject.name = $"{itemData.resourceName} Item";
        currentMoveSpeed = defaultMoveSpeed;
    }

    public void SetLayerAndSortingOrderForConveyor()
    {
        isOnOverheadLayer = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = CONVEYOR_SORTING_ORDER;
        }
        gameObject.layer = CONVEYOR_LAYER_ID;

        if (!isBeingMoved)
        {
            RegisterCurrentGridOccupation();
        }
    }

    public void SetLayerAndSortingOrderForOverhead()
    {
        isOnOverheadLayer = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = OVERHEAD_SORTING_ORDER;
        }
        gameObject.layer = OVERHEAD_LAYER_ID;

        if (!isBeingMoved)
        {
            RegisterCurrentGridOccupation();
        }
    }

    public bool IsOnOverheadLayer()
    {
        return isOnOverheadLayer;
    }

    public void DropToConveyorAfterCurrentMove()
    {
        dropToConveyorOnArrival = true;
    }

    private void ApplyConveyorLayerAfterArrival()
    {
        dropToConveyorOnArrival = false;
        isOnOverheadLayer = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = CONVEYOR_SORTING_ORDER;
        }
        gameObject.layer = CONVEYOR_LAYER_ID;
    }

    private ConveyorBelt GetConveyorAtGridPosition(Vector2Int gridPos)
    {
        if (GridManager.Instance == null) return null;

        List<GridObject> objects = GridManager.Instance.GetAllGridObjects(gridPos);
        if (objects == null) return null;

        for (int i = 0; i < objects.Count; i++)
        {
            ConveyorBelt belt = objects[i] as ConveyorBelt;
            if (belt != null)
            {
                return belt;
            }
        }

        return null;
    }

    private void RegisterCurrentGridOccupation()
    {
        if (GridManager.Instance == null) return;

        Vector2Int gridPos = GridManager.Instance.WorldToGrid(transform.position);
        if (gameObject.layer == OVERHEAD_LAYER_ID)
        {
            GridManager.Instance.OccupyOverheadGridSpot(gridPos, this);
            GridManager.Instance.ClearOccupiedGridSpot(gridPos, this);
        }
        else
        {
            GridManager.Instance.OccupyGridSpot(gridPos, this);
            GridManager.Instance.ClearOccupiedOverheadGridSpot(gridPos, this);
        }
    }

    private void ClearCurrentGridOccupation()
    {
        if (GridManager.Instance == null) return;

        Vector2Int gridPos = GridManager.Instance.WorldToGrid(transform.position);
        if (gameObject.layer == OVERHEAD_LAYER_ID)
        {
            GridManager.Instance.ClearOccupiedOverheadGridSpot(gridPos, this);
        }
        else
        {
            GridManager.Instance.ClearOccupiedGridSpot(gridPos, this);
        }
    }

    public void SetTargetPosition(Vector3 worldPosition, float speed = 0f)
    {
        if (GridManager.Instance != null)
        {
            // Release the grid cell this item is currently occupying.
            ClearCurrentGridOccupation();

            Vector2Int targetGridPos = GridManager.Instance.WorldToGrid(worldPosition);
            GridManager.Instance.ReserveGridSpot(targetGridPos, this);
            currentReservedGridPos = targetGridPos;
        }

        targetWorldPosition = worldPosition;

        if (speed > 0f)
        {
            currentMoveSpeed = speed;
        }

        isBeingMoved = true;
        TransportTickManager.RegisterMovingItem(this);
    }

    public void SetMoveSpeed(float speed)
    {
        currentMoveSpeed = speed;
    }

    public bool TickTransport(float deltaTime)
    {
        if (!isBeingMoved) return false;

        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetWorldPosition,
            currentMoveSpeed * deltaTime
        );

        bool movedThisTick = (nextPosition - currentPosition).sqrMagnitude > 0.00000001f;
        if (movedThisTick)
        {
            transform.position = nextPosition;
        }

        if ((transform.position - targetWorldPosition).sqrMagnitude < 0.000001f)
        {
            if ((transform.position - targetWorldPosition).sqrMagnitude > 0f)
            {
                transform.position = targetWorldPosition;
                movedThisTick = true;
            }

            // Mark as stationary before notifying conveyor logic.
            isBeingMoved = false;
            Vector2Int arrivedGridPos = currentReservedGridPos;

            if (dropToConveyorOnArrival)
            {
                ApplyConveyorLayerAfterArrival();
            }

            if (GridManager.Instance != null)
            {
                RegisterCurrentGridOccupation();
                GridManager.Instance.FinalizeItemPlacement(arrivedGridPos, this);

                // Notify lower conveyor only for lower-layer items.
                if (gameObject.layer == CONVEYOR_LAYER_ID)
                {
                    ConveyorBelt belt = GetConveyorAtGridPosition(arrivedGridPos);
                    if (belt != null)
                    {
                        belt.NotifyItemArrived(this);
                    }
                }
            }

            // If conveyor logic already scheduled a new move, do not wipe the new reservation.
            if (currentReservedGridPos == arrivedGridPos)
            {
                currentReservedGridPos = Vector2Int.zero;
            }
        }

        return movedThisTick;
    }

    void OnDestroy()
    {
        TransportTickManager.UnregisterMovingItem(this);

        if (GridManager.Instance != null)
        {
            ClearCurrentGridOccupation();
            if (currentReservedGridPos != Vector2Int.zero)
            {
                GridManager.Instance.FinalizeItemPlacement(currentReservedGridPos, this);
            }
        }
    }

    [System.Serializable]
    public class ItemSaveData
    {
        public string resourceName;
        public float[] pos; // float[] is easier for JsonUtility than Vector3
        public float[] targetPos;
        public bool moving;
        public float speed;
    }

    public override string GetSerializedData()
    {
        ItemSaveData data = new ItemSaveData();
        data.resourceName = itemData.resourceName;
        data.pos = new float[] { transform.position.x, transform.position.y, transform.position.z };
        data.targetPos = new float[] { targetWorldPosition.x, targetWorldPosition.y, targetWorldPosition.z };
        data.moving = isBeingMoved;
        data.speed = currentMoveSpeed;

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        ItemSaveData data = JsonUtility.FromJson<ItemSaveData>(json);

        transform.position = new Vector3(data.pos[0], data.pos[1], data.pos[2]);

        if (data.moving)
        {
            Vector3 target = new Vector3(data.targetPos[0], data.targetPos[1], data.targetPos[2]);
            SetTargetPosition(target, data.speed);
        }
    }
}
