using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class ConveyorBelt : GridObject
{
    [Header("Settings")]
    public float baseBeltSpeed = 2f; // Prêdkoœæ bazowa

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

    private static LayerMask itemLayerMask;
    private const int OVERHEAD_LAYER_ID = 11;

    private bool isHoldingItem = false;
    private int overheadDelayFrames = 0;
    private const int MAX_OVERHEAD_DELAY_FRAMES = 20;
    private float checkTimer = 0f;
    public float checkInterval = 0.05f;


    void Update()
    {
        if (isHoldingItem)
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                ForceCheckForMovement();
                checkTimer = checkInterval;
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        objectType = GridObjectType.ConveyorBelt;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);

        if (itemLayerMask == 0)
        {
            itemLayerMask = LayerMask.GetMask("Item");
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        UpdateVisualRotation();
    }

    public void RotateBelt(Direction newDirection)
    {
        travelDirection = newDirection;
        UpdateVisualRotation();
    }

    private OverheadConveyor GetOverheadConveyorOnThisSpot()
    {
        if (GridManager.Instance == null) return null;

        List<GridObject> objects = GridManager.Instance.GetAllGridObjects(GetGridPosition());

        if (objects == null) return null;

        return objects.OfType<OverheadConveyor>().FirstOrDefault();
    }

    private bool CanOverheadAcceptItem(OverheadConveyor overhead)
    {
        if (overhead == null) return false;

        Vector2Int nextGridPosition = GetPositionInDirection(overhead.GetGridPosition(), overhead.travelDirection);

        OverheadConveyor nextConveyor = GetNeighborOverheadConveyor(nextGridPosition);

        if (nextConveyor != null && nextConveyor.travelDirection == overhead.travelDirection)
        {
            return nextConveyor.itemOnOverheadLayer == null;
        }
        return true;
    }

    private OverheadConveyor GetNeighborOverheadConveyor(Vector2Int gridPos)
    {
        if (GridManager.Instance == null) return null;

        List<GridObject> objects = GridManager.Instance.GetAllGridObjects(gridPos);

        if (objects == null) return null;

        return objects.OfType<OverheadConveyor>().FirstOrDefault();
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


    private void ForceCheckForMovement()
    {
        float overlapRadius = 0.1f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, overlapRadius, itemLayerMask);

        Item itemToMove = null;

        foreach (Collider2D col in colliders)
        {
            Item item = col.GetComponent<Item>();
            if (item != null && !item.isBeingMoved && item.gameObject.layer == 8)
            {
                itemToMove = item;
                break;
            }
        }

        if (itemToMove == null)
        {
            isHoldingItem = false;
            overheadDelayFrames = 0;
            return;
        }


        if (overheadDelayFrames > 0)
        {
            overheadDelayFrames--;

            if (overheadDelayFrames > 0)
            {
                Debug.Log($"[CB-DELAYED] OpóŸnienie: pozosta³o {overheadDelayFrames} klatek. Zatrzymujê.");
                return;
            }

            Debug.Log($"[CB-DELAYED] Licznik opóŸnienia siê wyczerpa³. Wymuszam ruch.");
        }
        else
        {
            OverheadConveyor overhead = GetOverheadConveyorOnThisSpot();

            if (overhead != null)
            {
                if (overhead.itemOnOverheadLayer != null)
                {
                    Debug.Log($"[CB-OH-BLOCKED] Overhead jest ZAJÊTY. Pcham dalej.");
                }
                else
                {
                    overheadDelayFrames = MAX_OVERHEAD_DELAY_FRAMES;
                    Debug.Log($"[CB-OH-ACCEPT] Overhead jest WOLNY. Rozpoczynam opóŸnienie ({MAX_OVERHEAD_DELAY_FRAMES} klatek).");
                    return;
                }
            }
        }

        Vector2Int nextGridPosition = GetNextGridPosition();
        Vector3 nextWorldPosition = GridManager.Instance.GridToWorld(nextGridPosition);

        if (IsOutputBlocked(nextWorldPosition))
        {
            isHoldingItem = true;
            return;
        }

        itemToMove.SetTargetPosition(nextWorldPosition, CurrentBeltSpeed);
        isHoldingItem = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Item item = other.GetComponent<Item>();
        if (item == null) return;

        if (item.gameObject.layer == OVERHEAD_LAYER_ID)
        {
            return;
        }

        if (item.isBeingMoved && Vector3.Distance(item.transform.position, transform.position) > 0.1f)
        {
            return;
        }

        if (Vector3.Distance(item.transform.position, transform.position) > 0.1f)
        {
            return;
        }

        isHoldingItem = true;
        ForceCheckForMovement();
    }


    private bool IsOutputBlocked(Vector3 outputWorldPosition)
    {
        Vector2Int outputGridPosition = GridManager.Instance.WorldToGrid(outputWorldPosition);

        if (GridManager.Instance.IsGridSpotReserved(outputGridPosition))
        {
            return true;
        }

        float overlapRadius = 0.1f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(outputWorldPosition, overlapRadius, itemLayerMask);

        if (colliders.Length > 0)
        {
            foreach (Collider2D col in colliders)
            {
                Item item = col.GetComponent<Item>();
                if (item != null && item.gameObject.layer == 8)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateVisualRotation()
    {
        float angle = 0;
        switch (travelDirection)
        {
            case Direction.Up: angle = 0; break;
            case Direction.Down: angle = 180; break;
            case Direction.Left: angle = 90; break;
            case Direction.Right: angle = -90; break;
        }
        transform.rotation = Quaternion.Euler(0, 0, angle);
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
        BuildingSaveData data = new BuildingSaveData();

        // Rzutujemy Enum na int (Right=0, Down=1, itp.)
        data.outputDirectionInt = (int)this.travelDirection;

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        BuildingSaveData data = JsonUtility.FromJson<BuildingSaveData>(json);

        // Przywracamy Enum z inta i odœwie¿amy wizualia strza³ki
        this.travelDirection = (Direction)data.outputDirectionInt;

        // Wywo³ujemy Twoj¹ istniej¹c¹ metodê wizualizacji
        RotateBelt(this.travelDirection);

    }
}