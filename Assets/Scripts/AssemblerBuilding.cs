using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class AssemblerBuilding : GridObject, IProductionBuilding 
{
    // --- IMPLEMENTACJA INTERFEJSU ---
    public IBuildingRecipe GetCurrentRecipe() => currentRecipe;
    
    public int GetInputCount(int slotIndex) {
        if (slotIndex == 0) return currentPrimaryInput;
        if (slotIndex == 1) return currentSecondaryInput;
        if (slotIndex == 2) return currentTertiaryInput;
        return 0;
    }
    public float GetProgressTimer() => timer;
    // Pamiętaj, aby w interfejsie IProductionBuilding dodać:
    // int inputCapacity { get; }
    // int outputCapacity { get; }


    [Header("Receptura Assemblera")]
    public AssemblyRecipeData currentRecipe;

    public Direction outputDirection = Direction.Right;
    public enum Direction { Right, Down, Left, Up }

    [Header("Wizualizacja")]
    public GameObject outputIndicatorObject;
    public SpriteRenderer recipeIconRenderer;

    [Header("Ekwipunek Assemblera")]
    public int inputCapacity = 30;
    public int outputCapacity = 10;

    public int currentPrimaryInput = 0;
    public int currentSecondaryInput = 0;
    public int currentTertiaryInput = 0;

    private int currentOutputAmount = 0;

    int IProductionBuilding.inputCapacity => inputCapacity;
    int IProductionBuilding.outputCapacity => outputCapacity;

    [Header("Parametry Techniczne")]
    public float outputSpeed = 3.0f;
    public float timer;
    private bool isAssembling = false;
    private static LayerMask itemLayerMask;

    public int GetPrimaryInputCount() { return currentPrimaryInput; }
    public int GetSecondaryInputCount() { return currentSecondaryInput; }
    public int GetTertiaryInputCount() { return currentTertiaryInput; }
    public int GetCurrentOutputAmount() { return currentOutputAmount; }
    public float GetAssemblyTimer() { return timer; }

    protected override void Awake()
    {
        base.Awake();

        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(3, 3);

        if (itemLayerMask == 0)
        {
            itemLayerMask = LayerMask.GetMask("Item");
        }
    }

    void Start()
    {
        if (currentRecipe != null)
        {
            timer = GetModifiedAssemblyTime();
            UpdateRecipeIcon();
        }

        if (GridManager.Instance != null && occupiedPosition != Vector2Int.zero)
        {
            RotateBuilding(outputDirection);
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
        TryAssemble();

        if (isAssembling)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                if (currentOutputAmount < outputCapacity)
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

                    isAssembling = false;

                    // --- LOGIKA PRODUCTION SPEED ---
                    // U�ywamy zmodyfikowanego czasu zamiast surowego assemblyTime
                    timer = GetModifiedAssemblyTime();
                }
                else
                {
                    timer = 0.001f; // Czekamy, a� zwolni si� miejsce w ekwipunku
                }
            }
        }
    }

    private float GetModifiedAssemblyTime()
    {
        if (currentRecipe == null) return 1f;

        // Pobieramy mno�nik z managera (domy�lnie 1.0)
        float speedMultiplier = 1.0f;
        if (TechTreeManager.Instance != null)
        {
            speedMultiplier = TechTreeManager.Instance.GetProductionSpeedMultiplier();
        }

        return currentRecipe.assemblyTime / speedMultiplier;
    }

    public void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (UIManager.Instance == null) return;

        if (currentRecipe == null)
        {
            UIManager.Instance.OpenRecipeSelection(this, UIManager.Instance.GetRecipesForBuilding(this));
        }
        else
        {
            // Zmieniamy na uniwersalne wywołanie
            UIManager.Instance.OpenStatusWindow(this); 
        }
    }

    public void SetRecipe(AssemblyRecipeData newRecipe)
    {
        currentRecipe = newRecipe;
        timer = GetModifiedAssemblyTime();
        UpdateRecipeIcon();

        currentPrimaryInput = 0;
        currentSecondaryInput = 0;
        currentTertiaryInput = 0;
        currentOutputAmount = 0;

        isAssembling = false;
        timer = GetModifiedAssemblyTime();
    }

    public List<AssemblyRecipeData> GetAllAvailableRecipes()
    {
        return Resources.LoadAll<AssemblyRecipeData>("Recipes").ToList();
    }

    private void TryAssemble()
    {
        if (isAssembling) return;
        if (currentRecipe == null) return;
        if (currentOutputAmount >= outputCapacity) return;

        // Sprawdzenie dost�pno�ci wszystkich 3 surowc�w (lub mniej, je�li opcjonalne s� null)
        bool canCraft =
            currentPrimaryInput >= currentRecipe.primaryInputAmount &&
            (currentRecipe.secondaryInput == null || currentSecondaryInput >= currentRecipe.secondaryInputAmount) &&
            (currentRecipe.tertiaryInput == null || currentTertiaryInput >= currentRecipe.tertiaryInputAmount);

        if (canCraft)
        {
            currentPrimaryInput -= currentRecipe.primaryInputAmount;
            if (currentRecipe.secondaryInput != null) currentSecondaryInput -= currentRecipe.secondaryInputAmount;
            if (currentRecipe.tertiaryInput != null) currentTertiaryInput -= currentRecipe.tertiaryInputAmount;

            isAssembling = true;
            timer = GetModifiedAssemblyTime();
        }
    }

    private void TryConsumeFromWorld()
    {
        if (currentRecipe == null) return;

        if (currentPrimaryInput >= inputCapacity &&
            (currentRecipe.secondaryInput == null || currentSecondaryInput >= inputCapacity) &&
            (currentRecipe.tertiaryInput == null || currentTertiaryInput >= inputCapacity))
            return;

        List<Item> itemsToConsume = ScanAreaForItems();

        foreach (Item item in itemsToConsume)
        {
            if (item == null || item.isBeingMoved || item.itemData == null) continue;

            ResourceData itemData = item.itemData;

            if (itemData == currentRecipe.primaryInput && currentPrimaryInput < inputCapacity)
            {
                currentPrimaryInput++;
                Destroy(item.gameObject);
                continue;
            }

            if (currentRecipe.secondaryInput != null && itemData == currentRecipe.secondaryInput && currentSecondaryInput < inputCapacity)
            {
                currentSecondaryInput++;
                Destroy(item.gameObject);
                continue;
            }

            if (currentRecipe.tertiaryInput != null && itemData == currentRecipe.tertiaryInput && currentTertiaryInput < inputCapacity)
            {
                currentTertiaryInput++;
                Destroy(item.gameObject);
                continue;
            }
        }
    }

    private List<Item> ScanAreaForItems()
    {
        HashSet<Item> foundItems = new HashSet<Item>();
        if (GridManager.Instance == null) return foundItems.ToList();

        Vector2 scanSize = new Vector2(GridManager.Instance.tileSize, GridManager.Instance.tileSize);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tileGridPos = occupiedPosition + new Vector2Int(x, y);
                Vector3 tileWorldPos = GridManager.Instance.GridToWorld(tileGridPos) + new Vector3(GridManager.Instance.tileSize / 2f, GridManager.Instance.tileSize / 2f, 0f);

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
                            item.itemData == currentRecipe.secondaryInput ||
                            item.itemData == currentRecipe.tertiaryInput)
                        {
                            foundItems.Add(item);
                        }
                    }
                }
            }
        }
        return foundItems.ToList();
    }

    private void TrySpitOutItem()
    {
        if (currentRecipe == null) return;

        if (currentOutputAmount <= 0) return;

        Vector2Int outputGridPos = GetOutputGridPosition();
        Vector3 outputWorldPosition = GridManager.Instance.GridToWorld(outputGridPos);

        if (IsOutputBlocked(outputWorldPosition)) return;

        currentOutputAmount -= 1;

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

    private Vector2Int GetOutputGridPosition()
    {
        Vector2Int basePos = occupiedPosition;

        switch (outputDirection)
        {
            case Direction.Right: return new Vector2Int(basePos.x + 3, basePos.y + 1);
            case Direction.Down: return new Vector2Int(basePos.x + 1, basePos.y - 1);
            case Direction.Left: return new Vector2Int(basePos.x - 1, basePos.y + 1);
            case Direction.Up: return new Vector2Int(basePos.x + 1, basePos.y + 3);
            default: return basePos;
        }
    }

    public bool CanAcceptItem(ResourceData itemData)
    {
        if (currentRecipe == null) return false;

        if (itemData == currentRecipe.primaryInput)
        {
            return currentPrimaryInput < inputCapacity;
        }
        else if (itemData == currentRecipe.secondaryInput)
        {
            return currentSecondaryInput < inputCapacity;
        }
        else if (itemData == currentRecipe.tertiaryInput)
        {
            return currentTertiaryInput < inputCapacity;
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
        else if (itemData == currentRecipe.tertiaryInput)
        {
            currentTertiaryInput++;
        }
    }

    public void RotateBuilding(Direction newDirection)
    {
        outputDirection = newDirection;

        if (outputIndicatorObject != null)
        {
            outputIndicatorObject.SetActive(true);
            SpriteRenderer sr = outputIndicatorObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            float baseDistance = 1.1f;
            float targetRotationZ = 0f;

            switch (outputDirection)
            {
                case Direction.Right:
                    outputIndicatorObject.transform.localPosition = new Vector3(baseDistance, 0, 0f);
                    targetRotationZ = 270f;
                    break;
                case Direction.Down:
                    outputIndicatorObject.transform.localPosition = new Vector3(0, -baseDistance, 0f);
                    targetRotationZ = 180f;
                    break;
                case Direction.Left:
                    outputIndicatorObject.transform.localPosition = new Vector3(-baseDistance, 0, 0f);
                    targetRotationZ = 90f;
                    break;
                case Direction.Up:
                    outputIndicatorObject.transform.localPosition = new Vector3(0, baseDistance, 0f);
                    targetRotationZ = 0f;
                    break;
            }

            outputIndicatorObject.transform.localRotation = Quaternion.Euler(0, 0, targetRotationZ);
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

    [System.Serializable]
    public class BuildingSaveData
    {
        public int outputDirection;
        public string activeRecipeName;
        public int primaryCount;
        public int secondaryCount;
        public int tertiaryCount;
        public int outputCount;
        public float currentTimer;
        public bool working; // Flaga pracy
    }

    public override string GetSerializedData()
    {
        BuildingSaveData data = new BuildingSaveData();
        data.outputDirection = (int)this.outputDirection;
        if (currentRecipe != null) data.activeRecipeName = currentRecipe.name;

        data.primaryCount = currentPrimaryInput;
        data.secondaryCount = currentSecondaryInput;
        data.tertiaryCount = currentTertiaryInput;
        data.outputCount = currentOutputAmount;
        data.currentTimer = timer;
        data.working = isAssembling; // Zapisujemy stan pracy

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        BuildingSaveData data = JsonUtility.FromJson<BuildingSaveData>(json);

        this.outputDirection = (Direction)data.outputDirection;
        RotateBuilding(this.outputDirection);

        if (!string.IsNullOrEmpty(data.activeRecipeName))
        {
            AssemblyRecipeData loadedRecipe = Resources.Load<AssemblyRecipeData>("Recipes/Assembler/" + data.activeRecipeName);
            if (loadedRecipe != null)
            {
                SetRecipe(loadedRecipe); // To ustawia domy�lny timer i zeruje sk�adniki

                // Przywracamy dok�adny stan sprzed zapisu
                this.currentPrimaryInput = data.primaryCount;
                this.currentSecondaryInput = data.secondaryCount;
                this.currentTertiaryInput = data.tertiaryCount;
                this.currentOutputAmount = data.outputCount;
                this.timer = data.currentTimer;
                this.isAssembling = data.working; // Przywracamy stan pracy
            }
        }
    }
}