using TMPro;
using UnityEngine;

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

    private Vector3 targetWorldPosition;
    private float currentMoveSpeed;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetLayerAndSortingOrderForConveyor();
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
    }

    public void SetLayerAndSortingOrderForOverhead()
    {
        isOnOverheadLayer = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = OVERHEAD_SORTING_ORDER;
        }
        gameObject.layer = OVERHEAD_LAYER_ID;
    }

    public bool IsOnOverheadLayer()
    {
        return isOnOverheadLayer;
    }

    private Vector2Int currentReservedGridPos = Vector2Int.zero;

    public void SetTargetPosition(Vector3 worldPosition, float speed = 0f)
    {
        if (GridManager.Instance != null)
        {
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
    }

    public void SetMoveSpeed(float speed)
    {
        currentMoveSpeed = speed;
    }

    void Update()
    {
        if (isBeingMoved)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, currentMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.001f)
            {
                transform.position = targetWorldPosition;

                if (GridManager.Instance != null)
                {
                    GridManager.Instance.FinalizeItemPlacement(currentReservedGridPos);
                    currentReservedGridPos = Vector2Int.zero;
                }

                isBeingMoved = false;
            }
        }
    }

    [System.Serializable]
    public class ItemSaveData
    {
        public string resourceName;
        public float[] pos; // float[] jest ³atwiejszy dla JsonUtility ni¿ Vector3
        public float[] targetPos;
        public bool moving;
        public float speed;
    }

    // DODAJ S£OWO override
    public override string GetSerializedData()
    {
        ItemSaveData data = new ItemSaveData();
        data.resourceName = itemData.resourceName;
        data.pos = new float[] { transform.position.x, transform.position.y, transform.position.z };
        data.targetPos = new float[] { targetWorldPosition.x, targetWorldPosition.y, targetWorldPosition.z };
        data.moving = isBeingMoved;
        data.speed = currentMoveSpeed; // upewnij siê, ¿e masz tak¹ zmienn¹

        return JsonUtility.ToJson(data);
    }

    // Tê metodê wywo³amy w SaveManagerze podczas wczytywania
    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        ItemSaveData data = JsonUtility.FromJson<ItemSaveData>(json);

        // Przywracamy fizyczn¹ pozycjê
        transform.position = new Vector3(data.pos[0], data.pos[1], data.pos[2]);

        // Przywracamy cel ruchu
        if (data.moving)
        {
            Vector3 target = new Vector3(data.targetPos[0], data.targetPos[1], data.targetPos[2]);
            SetTargetPosition(target, data.speed);
        }
    }
}