using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class FurnaceBuilding : GridObject
{
    [Header("Receptura Pieca")]
    public SmeltingRecipeData currentRecipe;

    public Direction outputDirection = Direction.Right;
    public enum Direction { Right, Down, Left, Up }

    [Header("Wizualizacja")]
    public GameObject outputIndicatorObject;
    public SpriteRenderer recipeIconRenderer;

    [Header("Ekwipunek Pieca")]
    public int inputOreCapacity = 10;
    public int inputCoalCapacity = 10;
    public int outputBarCapacity = 10;

    public int currentPrimaryInput = 0;
    public int currentSecondaryInput = 0;
    private int currentOutputAmount = 0;

    [Header("Parametry Techniczne")]
    public float outputSpeed = 3.0f;
    public float timer;
    private bool isSmelting = false;
    private static LayerMask itemLayerMask;

    public ResourceData ironOreResource;
    public ResourceData coalOreResource;
    public ResourceData ironBarResource;

    public int GetPrimaryInputCount() { return currentPrimaryInput; }
    public int GetSecondaryInputCount() { return currentSecondaryInput; }
    public int GetCurrentOutputAmount() { return currentOutputAmount; }
    public float GetSmeltingTimer() { return timer; }

    public int InputOreCapacity = 5;
    public int InputCoalCapacity = 5;
    public int OutputBarCapacity = 5;

    public SmeltingRecipeData GetCurrentRecipe() { return currentRecipe; }

    protected override void Awake()
    {
        base.Awake();

        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(2, 2);

        if (itemLayerMask == 0)
        {
            itemLayerMask = LayerMask.GetMask("Item");
        }
    }

    void Start()
    {
        if (currentRecipe != null)
        {
            timer = GetModifiedSmeltingTime();
            UpdateRecipeIcon();
        }

        if (GridManager.Instance != null && occupiedPosition != Vector2Int.zero)
        {
            RotateFurnace(outputDirection);
        }
    }

    private void UpdateRecipeIcon()
    {
        if (recipeIconRenderer == null) return;

        if (currentRecipe != null && currentRecipe.outputItem != null && currentRecipe.outputItem.icon != null)
        {
            recipeIconRenderer.sprite = currentRecipe.outputItem.icon;
            recipeIconRenderer.enabled = true;
        }
        else
        {
            recipeIconRenderer.enabled = false;
        }
    }


    void Update()
    {
        if (currentRecipe == null) return;

        TrySpitOutItem();
        TryConsumeFromWorld();
        TrySmelt();

        if (isSmelting)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                if (currentOutputAmount < outputBarCapacity)
                {
                    // --- LOGIKA PRODUCTIVITY ---
                    int totalOutput = currentRecipe.outputAmount;

                    if (TechTreeManager.Instance != null)
                    {
                        float prodChance = TechTreeManager.Instance.GetProductivityChance();
                        if (Random.value < prodChance)
                        {
                            totalOutput *= 2; // Bonus: podwajamy wynik produkcji
                            Debug.Log("<color=cyan>[Productivity]</color> Bonusowy przedmiot!");
                        }
                    }

                    currentOutputAmount += totalOutput;

                    isSmelting = false;

                    // --- LOGIKA PRODUCTION SPEED ---
                    // U¿ywamy zmodyfikowanego czasu zamiast surowego assemblyTime
                    timer = GetModifiedSmeltingTime();
                }
                else
                {
                    timer = 0.001f; // Czekamy, a¿ zwolni siê miejsce w ekwipunku
                }
            }
        }
    }

    private float GetModifiedSmeltingTime()
    {
        if (currentRecipe == null) return 1f;

        // Pobieramy mno¿nik z managera (domyœlnie 1.0)
        float speedMultiplier = 1.0f;
        if (TechTreeManager.Instance != null)
        {
            speedMultiplier = TechTreeManager.Instance.GetProductionSpeedMultiplier();
        }

        // Czas bazowy dzielimy przez mno¿nik (np. 5s / 1.2 = 4.16s)
        return currentRecipe.smeltingTime / speedMultiplier;
    }

    public void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (UIManager.Instance == null) return;

        if (currentRecipe == null)
        {
            UIManager.Instance.OpenRecipeSelection(this, UIManager.Instance.GetRecipesForBuilding(this));
        }
        else
        {
            UIManager.Instance.OpenFurnaceStatus(this);
        }
    }

    public void SetRecipe(SmeltingRecipeData newRecipe)
    {
        currentRecipe = newRecipe;
        timer = GetModifiedSmeltingTime();
        UpdateRecipeIcon();

        currentPrimaryInput = 0;
        currentSecondaryInput = 0;
        currentOutputAmount = 0;

        isSmelting = false;
        timer = GetModifiedSmeltingTime();
    }

    public List<SmeltingRecipeData> GetAllAvailableRecipes()
    {
        return Resources.LoadAll<SmeltingRecipeData>("Recipes").ToList();
    }

    private void TrySmelt()
    {
        if (isSmelting) return;
        if (currentRecipe == null) return;

        if (currentOutputAmount >= outputBarCapacity)
        {
            return;
        }

        if (currentPrimaryInput >= currentRecipe.primaryInputAmount &&
            currentSecondaryInput >= currentRecipe.secondaryInputAmount)
        {
            currentPrimaryInput -= currentRecipe.primaryInputAmount;
            currentSecondaryInput -= currentRecipe.secondaryInputAmount;

            isSmelting = true;
            timer = GetModifiedSmeltingTime();
        }
    }

    private void TryConsumeFromWorld()
    {
        if (currentRecipe == null) return;

        if (currentPrimaryInput >= inputOreCapacity && currentSecondaryInput >= inputCoalCapacity) return;

        List<Item> itemsToConsume = ScanAreaForItems();

        foreach (Item item in itemsToConsume)
        {
            if (item == null || item.isBeingMoved || item.itemData == null) continue;

            ResourceData itemData = item.itemData;

            if (itemData == currentRecipe.primaryInput && currentPrimaryInput < inputOreCapacity)
            {
                currentPrimaryInput++;
                Destroy(item.gameObject);
                continue;
            }

            if (itemData == currentRecipe.secondaryInput && currentSecondaryInput < inputCoalCapacity)
            {
                currentSecondaryInput++;
                Destroy(item.gameObject);
                continue;
            }
        }
    }

    private void TrySpitOutItem()
    {
        if (currentRecipe == null) return;

        if (currentOutputAmount <= 0)
        {
            return;
        }

        Vector2Int outputGridPos = GetOutputGridPosition();
        Vector3 outputWorldPosition = GridManager.Instance.GridToWorld(outputGridPos);

        if (IsOutputBlocked(outputWorldPosition))
        {
            return;
        }

        int amountToSpitOut = 1;

        currentOutputAmount -= amountToSpitOut;

        Item outputPrefab = currentRecipe.outputItem.itemPrefab;

        if (outputPrefab == null)
        {
            Debug.LogError($"Brak prefabu dla OutputItem: {currentRecipe.outputItem.resourceName} w recepturze: {currentRecipe.recipeName}");
            return;
        }

        Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);

        Item newBar = Instantiate(outputPrefab, spawnPosition, Quaternion.identity, transform.parent);

        Item itemComponent = newBar.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.Initialize(currentRecipe.outputItem);
            itemComponent.SetTargetPosition(outputWorldPosition, outputSpeed);
        }

    }

    public bool CanAcceptItem(ResourceData itemData)
    {
        if (currentRecipe == null) return false;

        if (itemData == currentRecipe.primaryInput)
        {
            return currentPrimaryInput < inputOreCapacity;
        }
        else if (itemData == currentRecipe.secondaryInput)
        {
            return currentSecondaryInput < inputCoalCapacity;
        }

        return false;
    }

    public void ReceiveItem(ResourceData itemData)
    {
        if (currentRecipe == null) return;

        if (itemData == currentRecipe.primaryInput)
        {
            currentPrimaryInput++;
        }
        else if (itemData == currentRecipe.secondaryInput)
        {
            currentSecondaryInput++;
        }
    }

    private List<Item> ScanAreaForItems()
    {
        HashSet<Item> foundItems = new HashSet<Item>();
        if (currentRecipe == null || GridManager.Instance == null) return foundItems.ToList();

        Vector2 scanSize = new Vector2(GridManager.Instance.tileSize, GridManager.Instance.tileSize);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tileGridPos = occupiedPosition + new Vector2Int(x, y);
                Vector2 tileWorldPos = GridManager.Instance.GridToWorld(tileGridPos);

                Collider2D[] foundColliders = Physics2D.OverlapBoxAll(
                    tileWorldPos,
                    scanSize * 0.9f,
                    0f,
                    itemLayerMask
                );

                foreach (Collider2D col in foundColliders)
                {
                    Item item = col.GetComponent<Item>();
                    if (item != null && !item.isBeingMoved)
                    {
                        if (item.itemData == currentRecipe.primaryInput ||
                            item.itemData == currentRecipe.secondaryInput)
                        {
                            foundItems.Add(item);
                        }
                    }
                }
            }
        }
        return foundItems.ToList();
    }

    private Vector2Int GetOutputGridPosition()
    {
        Vector2Int basePos = occupiedPosition;

        switch (outputDirection)
        {
            case Direction.Right: return new Vector2Int(basePos.x + 2, basePos.y);
            case Direction.Down: return new Vector2Int(basePos.x, basePos.y - 1);
            case Direction.Left: return new Vector2Int(basePos.x - 1, basePos.y + 1);
            case Direction.Up: return new Vector2Int(basePos.x + 1, basePos.y + 2);
            default: return basePos;
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

        return colliders.Length > 0;
    }

    public void RotateFurnace(Direction newDirection)
    {
        outputDirection = newDirection;

        if (outputIndicatorObject != null)
        {
            outputIndicatorObject.SetActive(true);

            SpriteRenderer sr = outputIndicatorObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
            }

            Vector3 localOffset = Vector3.zero;
            float targetRotationZ = 0f;

            float baseDistance = 0.60f;
            float localSideAdjustment = 0.25f;

            float finalMainOffset = baseDistance - 0.25f;
            float finalSideOffset = localSideAdjustment;

            switch (outputDirection)
            {
                case Direction.Right:
                    localOffset = new Vector3(finalMainOffset, -finalSideOffset, 0f);
                    targetRotationZ = 270f;
                    break;
                case Direction.Down:
                    localOffset = new Vector3(-finalSideOffset, -finalMainOffset, 0f);
                    targetRotationZ = 180f;
                    break;
                case Direction.Left:
                    localOffset = new Vector3(-finalMainOffset, finalSideOffset, 0f);
                    targetRotationZ = 90f;
                    break;
                case Direction.Up:
                    localOffset = new Vector3(finalSideOffset, finalMainOffset, 0f);
                    targetRotationZ = 0f;
                    break;
            }

            outputIndicatorObject.transform.localPosition = localOffset;
            outputIndicatorObject.transform.localRotation = Quaternion.Euler(0, 0, targetRotationZ);
        }
    }

    [System.Serializable]
    public class BuildingSaveData
    {
        public int outputDirectionInt;
        public string activeRecipeName;
        public int primaryCount;
        public int secondaryCount;
        public int outputCount;
        public float currentTimer;
        public bool working;
    }

    public override string GetSerializedData()
    {
        BuildingSaveData data = new BuildingSaveData();
        data.outputDirectionInt = (int)this.outputDirection;
        if (currentRecipe != null) data.activeRecipeName = currentRecipe.name;

        data.primaryCount = currentPrimaryInput;
        data.secondaryCount = currentSecondaryInput;
        data.outputCount = currentOutputAmount;
        data.currentTimer = timer;
        data.working = isSmelting;

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        BuildingSaveData data = JsonUtility.FromJson<BuildingSaveData>(json);

        this.outputDirection = (Direction)data.outputDirectionInt;
        RotateFurnace(this.outputDirection);

        if (!string.IsNullOrEmpty(data.activeRecipeName))
        {
            SmeltingRecipeData loadedRecipe = Resources.Load<SmeltingRecipeData>("Recipes/Furnace/" + data.activeRecipeName);
            if (loadedRecipe != null)
            {
                SetRecipe(loadedRecipe);

                this.currentPrimaryInput = data.primaryCount;
                this.currentSecondaryInput = data.secondaryCount;
                this.currentOutputAmount = data.outputCount;
                this.timer = data.currentTimer;
                this.isSmelting = data.working;
            }
        }
    }
}