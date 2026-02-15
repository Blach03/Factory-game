using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MinerBuilding : GridObject
{
    public float baseProductionSpeed = 1.0f;
    public float currentProductionSpeed;
    public float outputInterval => 1.0f / currentProductionSpeed;


    [Header("Miner Output Settings")]
    public float outputSpeed = 3.0f;

    private float timer;
    private ResourceDeposit depositToMine;

    private static LayerMask itemLayerMask;
    private static Transform itemsContainer;

    public Direction outputDirection = Direction.Right;
    public enum Direction { Right, Down, Left, Up }

    [Header("Extender Settings")]
    public float boostPerExtender = 1.0f; // 100% bonusu
    private int activeExtendersCount = 0;


    protected override void Awake()
    {
        base.Awake();

        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);

        if (itemLayerMask == 0)
        {
            itemLayerMask = LayerMask.GetMask("Item");
        }
    }

    void Start()
    {
        RecalculateBoost();
        currentProductionSpeed = baseProductionSpeed;
        timer = outputInterval;

        if (GridManager.Instance != null)
        {
            RotateMiner(outputDirection);
        }

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

        FindResourceDeposit();
    }

    void Update()
    {
        if (depositToMine == null)
        {
            FindResourceDeposit();
            if (depositToMine == null) return;

            timer = outputInterval;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            MineResource();
            timer = outputInterval;
        }
    }

    private void FindResourceDeposit()
    {
        if (GridManager.Instance == null) return;

        List<GridObject> objectsOnTile = GridManager.Instance.GetGridObjects(GetGridPosition());

        if (objectsOnTile != null)
        {
            ResourceDeposit deposit = objectsOnTile
                .Select(o => o.GetComponent<ResourceDeposit>())
                .FirstOrDefault(d => d != null);

            depositToMine = deposit;

            if (depositToMine != null)
            {
                Debug.Log($"MinerBuilding na pozycji {GetGridPosition()} znalaz³ z³o¿e: {depositToMine.resourceData.resourceName}");
            }
        }
    }

    private void MineResource()
    {
        if (depositToMine == null) return;

        Item itemPrefabToSpawn = depositToMine.GetMinedItemPrefab();
        if (itemPrefabToSpawn == null) return;

        Vector2Int minerGridPosition = GetGridPosition();
        Vector2Int outputGridPosition = GetOutputGridPosition();

        Vector3 outputWorldPosition = GridManager.Instance.GridToWorld(outputGridPosition);

        if (IsOutputBlocked(outputWorldPosition))
        {
            return;
        }

        Vector3 minerWorldPosition = GridManager.Instance.GridToWorld(minerGridPosition);
        Vector3 outputVector = outputWorldPosition - minerWorldPosition;
        Vector3 startWorldPosition = minerWorldPosition + (outputVector / 2.0f);

        Item newItem = Instantiate(itemPrefabToSpawn, startWorldPosition, Quaternion.identity, itemsContainer);


        if (newItem != null)
        {


            newItem.SetTargetPosition(outputWorldPosition, outputSpeed);
        }
        else
        {
            Destroy(newItem.gameObject);
        }
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
            return true;
        }

        return false;
    }

    public void RotateMiner(Direction newDirection)
    {
        outputDirection = newDirection;
        float angle = 0;
        switch (outputDirection)
        {
            case Direction.Up: angle = 180; break;
            case Direction.Down: angle = 0; break;
            case Direction.Left: angle = -90; break;
            case Direction.Right: angle = 90; break;
        }
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private Vector2Int GetOutputGridPosition()
    {
        Vector2Int currentGridPos = GetGridPosition();
        switch (outputDirection)
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
        // Zmieniamy na int, aby ³atwo zapisaæ Enum Direction
        public int outputDirectionInt;
        public string activeRecipeName;
    }

    public override string GetSerializedData()
    {
        BuildingSaveData data = new BuildingSaveData();

        // Rzutujemy Enum na int (Right=0, Down=1, itp.)
        data.outputDirectionInt = (int)this.outputDirection;

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        BuildingSaveData data = JsonUtility.FromJson<BuildingSaveData>(json);

        // Przywracamy Enum z inta i odœwie¿amy wizualia strza³ki
        this.outputDirection = (Direction)data.outputDirectionInt;

        // Wywo³ujemy Twoj¹ istniej¹c¹ metodê wizualizacji
        RotateMiner(this.outputDirection);

    }

    public void RecalculateBoost()
    {
        // HashSet przechowuje unikalne pozycje, które ju¿ policzyliœmy
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Vector2Int myPos = GetGridPosition();
        visited.Add(myPos); // Nie licz minera jako extendera

        // Rozpoczynamy rekurencjê od Minera
        int totalExtenders = CountAllConnectedExtenders(myPos, visited);

        // Limit 100 extenderów (zabezpieczenie wydajnoœciowe)
        totalExtenders = Mathf.Min(totalExtenders, 100);

        float multiplier = 1f + totalExtenders;
        currentProductionSpeed = baseProductionSpeed * multiplier;
        timer = Mathf.Min(timer, outputInterval);

        Debug.Log($"[Miner {myPos}] Znaleziono ³¹cznie {totalExtenders} extenderów w sieci. Speed: {currentProductionSpeed}");
    }

    private int CountAllConnectedExtenders(Vector2Int currentPoint, HashSet<Vector2Int> visited)
    {
        if (visited.Count > 100) return 0; // Twardy limit rekurencji

        int count = 0;
        Vector2Int[] neighbors = {
        currentPoint + Vector2Int.up,
        currentPoint + Vector2Int.down,
        currentPoint + Vector2Int.left,
        currentPoint + Vector2Int.right
    };

        foreach (Vector2Int nPos in neighbors)
        {
            // Jeœli ju¿ tu byliœmy, skaczemy dalej
            if (visited.Contains(nPos)) continue;

            var objects = GridManager.Instance.GetGridObjects(nPos);
            if (objects == null) continue;

            MinerExtender extender = objects.OfType<MinerExtender>().FirstOrDefault();

            // Sprawdzamy: czy to extender, czy celuje w currentPoint i czy jest na tym samym z³o¿u
            if (extender != null && extender.GetTargetGridPosition() == currentPoint)
            {
                if (IsSameDepositType(nPos))
                {
                    visited.Add(nPos); // Oznaczamy jako odwiedzony
                    count += 1 + CountAllConnectedExtenders(nPos, visited);
                }
                else
                {
                    Debug.Log($"[Miner] Extender na {nPos} jest na innym z³o¿u - ignorujê.");
                }
            }
        }
        return count;
    }

    private bool IsSameDepositType(Vector2Int pos)
    {
        // Pobieramy z³o¿e pod minerem
        var myDeposit = GridManager.Instance.GetGridObjects(GetGridPosition())
                        .OfType<ResourceDeposit>().FirstOrDefault();

        // Pobieramy z³o¿e pod sprawdzanym polem
        var targetDeposit = GridManager.Instance.GetGridObjects(pos)
                            .OfType<ResourceDeposit>().FirstOrDefault();

        if (myDeposit == null || targetDeposit == null) return false;

        // Porównujemy nazwy surowców (np. "Coal" == "Coal")
        return myDeposit.resourceData.resourceName == targetDeposit.resourceData.resourceName;
    }
}