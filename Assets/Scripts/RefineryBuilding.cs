using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class RefineryBuilding : GridObject, IProductionBuilding 
{
    // --- IMPLEMENTACJA INTERFEJSU ---
    public IBuildingRecipe GetCurrentRecipe() => currentRecipe;
    
    public int GetInputCount(int slotIndex) {
        if (slotIndex == 0) return currentItemInput;
        if (slotIndex == 1) return Mathf.FloorToInt(currentFluidAmount); 
        return 0;
    }
    public float GetProgressTimer() => timer;
    public int GetCurrentOutputAmount() => currentOutputAmount;
    [Header("Receptura")]
    public RefineryRecipeData currentRecipe;
    public Direction outputDirection = Direction.Right;
    public enum Direction { Right, Down, Left, Up }

    [Header("Wizualizacja")]
    public GameObject outputIndicatorObject;
    public SpriteRenderer recipeIconRenderer;

    [Header("Ekwipunek")]
    public float currentFluidAmount = 0f;
    public int currentItemInput = 0;
    public int currentOutputAmount = 0;
    
    public int inputCapacity = 30;
    public float fluidCapacity = 100f;
    public int outputCapacity = 20;

    int IProductionBuilding.inputCapacity => inputCapacity;
    int IProductionBuilding.outputCapacity => outputCapacity;

    [Header("Parametry Techniczne")]
    public float outputSpeed = 3.0f;
    public float timer;
    private bool isProcessing = false;
    private static LayerMask itemLayerMask;

    protected override void Awake()
    {
        base.Awake();
        size = new Vector2Int(3, 3);
        objectType = GridObjectType.Building;
        if (itemLayerMask == 0) itemLayerMask = LayerMask.GetMask("Item");
    }

    void Start()
    {
        UpdateRecipeIcon();
        RotateBuilding(outputDirection);
        NotifyNeighboringPipes();
    }

    void Update()
    {
        if (currentRecipe == null) return;

        TryConsumeFluidFromNetwork();
        TryConsumeItemsFromWorld();
        
        if (!isProcessing && CanStartProduction())
        {
            StartProduction();
        }

        if (isProcessing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0) FinishProduction();
        }

        if (currentOutputAmount > 0) TrySpitOutItem();
    }

    private void NotifyNeighboringPipes()
    {
        // Sprawdzamy obwód budynku 3x3 (od -1 do 3)
        for (int x = -1; x <= size.x; x++)
        {
            for (int y = -1; y <= size.y; y++)
            {
                // Interesują nas tylko krawędzie
                if (x >= 0 && x < size.x && y >= 0 && y < size.y) continue;

                Vector2Int neighborPos = occupiedPosition + new Vector2Int(x, y);
                var pipe = GridManager.Instance.GetGridObjects(neighborPos)?.OfType<PipeBuilding>().FirstOrDefault();
                if (pipe != null)
                {
                    // Wywołaj metodę aktualizacji grafiki rury (nazwa zależy od Twojego skryptu rur, np. UpdatePipeVisuals)
                    pipe.UpdatePipeVisuals(); 
                }
            }
        }
    }

    private void TryConsumeFluidFromNetwork()
    {
        if (currentFluidAmount >= fluidCapacity) return;

        Vector2Int inputPos = GetInputGridPosition();
        var pipe = GridManager.Instance.GetGridObjects(inputPos)?.OfType<PipeBuilding>().FirstOrDefault();
        
        if (pipe != null && pipe.CurrentNetwork != null)
        {
            float space = fluidCapacity - currentFluidAmount;
            // Pobieramy płyn z sieci (max 10 na sekundę dla balansu)
            float toTake = Mathf.Min(space, pipe.CurrentNetwork.storedFluid, 10f * Time.deltaTime);
            if (pipe.CurrentNetwork.RequestFluid(toTake))
            {
                currentFluidAmount += toTake;
            }
        }
    }

    private bool CanStartProduction()
    {
        return currentFluidAmount >= currentRecipe.fluidAmount &&
               currentItemInput >= currentRecipe.inputItemAmount &&
               currentOutputAmount + currentRecipe.outputResultAmount <= outputCapacity;
    }

    private void StartProduction()
    {
        currentFluidAmount -= currentRecipe.fluidAmount;
        currentItemInput -= currentRecipe.inputItemAmount;
        timer = currentRecipe.processTime;
        isProcessing = true;
    }

    private void FinishProduction()
    {
        isProcessing = false;
        currentOutputAmount += currentRecipe.outputResultAmount;
    }

    private void TryConsumeItemsFromWorld()
    {
        if (currentItemInput >= inputCapacity) return;

        // Skanujemy obszar 3x3 budynku
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tilePos = occupiedPosition + new Vector2Int(x, y);
                Vector3 worldPos = GridManager.Instance.GridToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0f);
                
                Collider2D col = Physics2D.OverlapBox(worldPos, Vector2.one * 0.8f, 0, itemLayerMask);
                if (col != null)
                {
                    Item item = col.GetComponent<Item>();
                    if (item != null && !item.isBeingMoved && item.itemData == currentRecipe.inputItem)
                    {
                        currentItemInput++;
                        Destroy(item.gameObject);
                        if (currentItemInput >= inputCapacity) return;
                    }
                }
            }
        }
    }

    private void TrySpitOutItem()
    {
        Vector2Int outGridPos = GetOutputGridPosition();
        Vector3 outWorldPos = GridManager.Instance.GridToWorld(outGridPos);

        // Sprawdź czy wyjście nie jest zablokowane (logika z Twojego Assemblera)
        if (IsOutputBlocked(outWorldPos)) return;

        currentOutputAmount--;
        Item newObj = Instantiate(currentRecipe.outputResource.itemPrefab, transform.position, Quaternion.identity, transform.parent);
        newObj.Initialize(currentRecipe.outputResource);
        newObj.SetTargetPosition(outWorldPos, outputSpeed);
    }

    // --- UI I ROTACJA ---

    public void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentRecipe == null)
        {
            // Ładowanie receptur dla rafinerii
            var recipes = Resources.LoadAll<RefineryRecipeData>("Recipes/Refinery").Cast<IBuildingRecipe>().ToList();
            UIManager.Instance.OpenRecipeSelection(this, recipes);
        }
        else
        {
            // Otwieramy to samo okno co dla Assemblera!
            UIManager.Instance.OpenStatusWindow(this); 
        }
    }

    public void SetRecipe(RefineryRecipeData recipe)
    {
        currentRecipe = recipe;
        currentItemInput = 0;
        currentOutputAmount = 0;
        currentFluidAmount = 0;
        isProcessing = false;
        UpdateRecipeIcon();
    }

    private void UpdateRecipeIcon()
    {
        if (recipeIconRenderer == null) return;
        recipeIconRenderer.sprite = currentRecipe?.outputResource?.icon;
        recipeIconRenderer.enabled = currentRecipe != null;
    }

    public void RotateBuilding(Direction newDirection)
    {
        outputDirection = newDirection;

        if (outputIndicatorObject != null)
        {
            outputIndicatorObject.SetActive(true);
            SpriteRenderer sr = outputIndicatorObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            // Dla budynku 3x3, dystans 1.1f z Assemblera może być za mały, 
            // jeśli Assembler też ma 3x3 i tam działa, zostaw 1.1f. 
            // Jeśli jednak jest "wewnątrz" budynku, zwiększ do ok 1.6f.
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

    private Vector2Int GetInputGridPosition()
    {
        // Wejście jest zawsze po przeciwnej stronie niż Direction wyjścia
        switch (outputDirection)
        {
            case Direction.Right: return occupiedPosition + new Vector2Int(-1, 1);
            case Direction.Down:  return occupiedPosition + new Vector2Int(1, 3);
            case Direction.Left:  return occupiedPosition + new Vector2Int(3, 1);
            case Direction.Up:    return occupiedPosition + new Vector2Int(1, -1);
            default: return occupiedPosition;
        }
    }

    private Vector2Int GetOutputGridPosition()
    {
        switch (outputDirection)
        {
            case Direction.Right: return occupiedPosition + new Vector2Int(3, 1);
            case Direction.Down:  return occupiedPosition + new Vector2Int(1, -1);
            case Direction.Left:  return occupiedPosition + new Vector2Int(-1, 1);
            case Direction.Up:    return occupiedPosition + new Vector2Int(1, 3);
            default: return occupiedPosition;
        }
    }

    private bool IsOutputBlocked(Vector3 pos)
    {
        return Physics2D.OverlapCircle(pos, 0.1f, itemLayerMask) != null;
    }
}