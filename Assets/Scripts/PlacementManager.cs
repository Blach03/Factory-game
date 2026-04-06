using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject minerPrefab;
    public GameObject conveyorBeltPrefab;
    public GameObject overheadConveyorPrefab;
    public GameObject furnacePrefab;
    public GameObject assemblerPrefab;
    public GameObject minerExtenderPrefab;

    public GameObject pipePrefab;

    [Header("UI Reference")]
    public List<GameObject> machinePrefabs;

    [Header("State")]
    private GameObject selectedPrefab;
    private GameObject previewObject;
    private GridObject selectedGridObjectComponent;
    private Transform buildingsContainer;

    private int currentRotationIndex = 1;

    private LayerMask itemLayerMask;
    private bool isPlacingConveyor = false;
    private Vector2Int lastPlacedGridPos = new Vector2Int(int.MaxValue, int.MaxValue);

    private const string BuildingsContainerName = "--BUILDINGS--";

    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceData resource;
        public int amount;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameObject containerGO = GameObject.Find(BuildingsContainerName);
        if (containerGO == null)
        {
            containerGO = new GameObject(BuildingsContainerName);
            containerGO.transform.position = Vector3.zero;
        }
        buildingsContainer = containerGO.transform;

        itemLayerMask = LayerMask.GetMask("Item");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            TryDestroyItem();
        }

        if (Input.GetKeyDown(KeyCode.X) && selectedPrefab == null)
        {
            TryRemoveBuilding();
        }

        if (Input.GetMouseButtonDown(0) && selectedPrefab != null && !isPlacingConveyor)
        {
            TryPlaceBuilding();
        }

        if (Input.GetMouseButton(0) && selectedPrefab != null && isPlacingConveyor)
        {
            TryPlaceBuildingContinuously();
        }

        if (Input.GetMouseButtonUp(0))
        {
            lastPlacedGridPos = new Vector2Int(int.MaxValue, int.MaxValue);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (selectedPrefab != null)
            {
                RotateSelectedBuilding();
            }
            else
            {
                TryRotatePlacedBuilding();
            }
        }

        UpdatePreview();
    }


    private void TryDestroyItem()
    {
        if (Camera.main == null) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, itemLayerMask);

        if (hit.collider != null)
        {
            Item item = hit.collider.GetComponent<Item>();

            if (item != null)
            {
                if (item.isBeingMoved)
                {
                    Debug.LogWarning("Nie mo�na podnie�� przedmiotu: jest w trakcie przenoszenia przez ta�moci�g.");
                    return;
                }

                // --- NOWA LOGIKA PODNOSZENIA ---
                if (PlayerInventory.Instance != null)
                {
                    // Dodajemy przedmiot do ekwipunku przed zniszczeniem obiektu
                    PlayerInventory.Instance.AddItem(item.itemData, 1);
                    Debug.Log($"Podniesiono przedmiot: {item.itemData.resourceName}");
                }
                else
                {
                    Debug.LogError("B��d: Nie znaleziono PlayerInventory.Instance! Przedmiot zosta� usuni�ty bez dodania do ekwipunku.");
                }

                Destroy(item.gameObject);
            }
        }
    }


    private void RotateSelectedBuilding()
    {
        currentRotationIndex = (currentRotationIndex + 1) % 4;

        if (previewObject != null && selectedGridObjectComponent.size.x > 1)
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);
            Vector3 worldPosition = GridManager.Instance.GridToWorld(gridPosition);

            float offset = GridManager.Instance.tileSize / 2f;
            worldPosition += new Vector3(offset, offset, 0);

            previewObject.transform.position = worldPosition;
        }

        if (previewObject != null)
        {
            MinerBuilding minerComponent = previewObject.GetComponent<MinerBuilding>();
            ConveyorBelt beltComponent = previewObject.GetComponent<ConveyorBelt>();
            OverheadConveyor overheadComponent = previewObject.GetComponent<OverheadConveyor>();
            FurnaceBuilding furnaceComponent = previewObject.GetComponent<FurnaceBuilding>();
            AssemblerBuilding assemblerComponent = previewObject.GetComponent<AssemblerBuilding>();
            RefineryBuilding refineryComponent = previewObject.GetComponent<RefineryBuilding>(); // DODANO
            MinerExtender extender = previewObject.GetComponent<MinerExtender>();

            if (extender != null)
            {
                extender.RotateBuilding(GetNextDirection(extender.outputDirection));
            }

            if (minerComponent != null)
            {
                minerComponent.RotateMiner((MinerBuilding.Direction)currentRotationIndex);
            }

            if (beltComponent != null)
            {
                beltComponent.RotateBelt((ConveyorBelt.Direction)currentRotationIndex);
            }

            if (overheadComponent != null)
            {
                overheadComponent.RotateBelt((ConveyorBelt.Direction)currentRotationIndex);
            }

            if (furnaceComponent != null)
            {
                furnaceComponent.RotateFurnace((FurnaceBuilding.Direction)currentRotationIndex);
            }

            if (refineryComponent != null)
            {
                refineryComponent.RotateBuilding((RefineryBuilding.Direction)currentRotationIndex);
            }

            if (assemblerComponent != null)
            {
                assemblerComponent.RotateBuilding((AssemblerBuilding.Direction)currentRotationIndex);
            }
        }
    }

    private void TryRotatePlacedBuilding()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;
        Vector2Int gridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);

        List<GridObject> placedObjects = GridManager.Instance.GetGridObjects(gridPosition);

        if (placedObjects.Count == 0) return;

        GridObject targetObject = placedObjects
            .Where(o => o.objectType != GridObjectType.ResourceDeposit)
            .OrderByDescending(o => o.objectType == GridObjectType.OverheadConveyor ? 3 :
                                    o.objectType == GridObjectType.Building ? 2 :
                                    o.objectType == GridObjectType.ConveyorBelt ? 1 : 0)
            .FirstOrDefault();

        if (targetObject != null)
        {
            MinerBuilding miner = targetObject.GetComponent<MinerBuilding>();
            ConveyorBelt belt = targetObject.GetComponent<ConveyorBelt>();
            OverheadConveyor overhead = targetObject.GetComponent<OverheadConveyor>();
            FurnaceBuilding furnace = targetObject.GetComponent<FurnaceBuilding>();
            AssemblerBuilding assembler = targetObject.GetComponent<AssemblerBuilding>();
            RefineryBuilding refinery = targetObject.GetComponent<RefineryBuilding>(); // DODANO
            MinerExtender extender = targetObject.GetComponent<MinerExtender>();
            if (extender != null)
            {
                extender.RotateBuilding(GetNextDirection(extender.outputDirection));
            }

            if (miner != null)
            {
                MinerBuilding.Direction currentDir = miner.outputDirection;
                MinerBuilding.Direction newDir = GetNextDirection(currentDir);
                miner.RotateMiner(newDir);
            }
            else if (belt != null)
            {
                ConveyorBelt.Direction currentDir = belt.travelDirection;
                ConveyorBelt.Direction newDir = GetNextDirection(currentDir);
                belt.RotateBelt(newDir);
            }
            else if (overhead != null)
            {
                ConveyorBelt.Direction currentDir = overhead.travelDirection;
                ConveyorBelt.Direction newDir = GetNextDirection(currentDir);
                overhead.RotateBelt(newDir);
            }
            else if (furnace != null)
            {
                FurnaceBuilding.Direction currentDir = furnace.outputDirection;
                FurnaceBuilding.Direction newDir = GetNextDirection(currentDir);
                furnace.RotateFurnace(newDir);
            }
            else if (assembler != null)
            {
                AssemblerBuilding.Direction currentDir = assembler.outputDirection;
                AssemblerBuilding.Direction newDir = GetNextDirection(currentDir);
                assembler.RotateBuilding(newDir);
            }
            else if (refinery != null)
            {
                RefineryBuilding.Direction currentDir = refinery.outputDirection;
                RefineryBuilding.Direction newDir = GetNextDirection(currentDir);
                refinery.RotateBuilding(newDir);
            }
        }
    }

    private T GetNextDirection<T>(T currentDir) where T : Enum
    {
        Array directions = Enum.GetValues(typeof(T));
        int index = Array.IndexOf(directions, currentDir);
        int nextIndex = (index + 1) % directions.Length;
        return (T)directions.GetValue(nextIndex);
    }


    private void TryPlaceBuilding()
    {
        if (selectedPrefab == null || GridManager.Instance == null || selectedGridObjectComponent == null) return;

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;
        Vector2Int gridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);

        TryPlaceObjectAtPosition(gridPosition);
    }

    private void TryPlaceBuildingContinuously()
    {
        if (selectedPrefab == null || GridManager.Instance == null || selectedGridObjectComponent == null) return;

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;
        Vector2Int currentGridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);

        if (currentGridPosition == lastPlacedGridPos) return;

        if (selectedGridObjectComponent.size.x > 1 || selectedGridObjectComponent.size.y > 1) return;

        if (TryPlaceObjectAtPosition(currentGridPosition))
        {
            lastPlacedGridPos = currentGridPosition;
        }
    }

    private bool TryPlaceObjectAtPosition(Vector2Int gridPosition)
    {
        List<Vector2Int> occupiedTiles;

        bool hasResources = HasRequiredResources(selectedGridObjectComponent);

        if (!CanPlaceBuildingAtPosition(gridPosition, selectedGridObjectComponent, out occupiedTiles) || !hasResources)
        {
            if (!hasResources) Debug.LogWarning("Brak surowc�w do budowy!");
            return false;
        }

        Vector3 worldPosition = GridManager.Instance.GridToWorld(gridPosition);

        if (selectedGridObjectComponent.size.x > 1 || selectedGridObjectComponent.size.y > 1)
        {
            float offset = GridManager.Instance.tileSize / 2f;
            worldPosition += new Vector3(offset, offset, 0);
        }

        GameObject newBuildingObject = Instantiate(selectedPrefab, worldPosition, Quaternion.identity, buildingsContainer);
        GridObject gridObject = newBuildingObject.GetComponent<GridObject>();

        if (newBuildingObject.GetComponent<MinerExtender>() != null || newBuildingObject.GetComponent<MinerBuilding>() != null)
        {
            foreach (var m in FindObjectsOfType<MinerBuilding>()) m.RecalculateBoost();
        }

        if (gridObject != null)
        {
            gridObject.Initialize(gridPosition);
            SpendResources(gridObject);

            MinerBuilding miner = newBuildingObject.GetComponent<MinerBuilding>();
            ConveyorBelt belt = newBuildingObject.GetComponent<ConveyorBelt>();
            OverheadConveyor overhead = newBuildingObject.GetComponent<OverheadConveyor>();
            FurnaceBuilding furnace = newBuildingObject.GetComponent<FurnaceBuilding>();
            AssemblerBuilding assembler = newBuildingObject.GetComponent<AssemblerBuilding>();
            MinerExtender extender = newBuildingObject.GetComponent<MinerExtender>();
            RefineryBuilding refinery = newBuildingObject.GetComponent<RefineryBuilding>();
            if (refinery != null)
            {
                refinery.RotateBuilding((RefineryBuilding.Direction)currentRotationIndex);
            }
            if (extender != null)
            {
                extender.SetupRotation((MinerBuilding.Direction)currentRotationIndex);
            }

            if (miner != null)
            {
                miner.RotateMiner((MinerBuilding.Direction)currentRotationIndex);
            }
            else if (belt != null)
            {
                belt.RotateBelt((ConveyorBelt.Direction)currentRotationIndex);
            }
            else if (overhead != null)
            {
                overhead.RotateBelt((ConveyorBelt.Direction)currentRotationIndex);
            }
            else if (furnace != null)
            {
                furnace.RotateFurnace((FurnaceBuilding.Direction)currentRotationIndex);
            }
            else if (assembler != null)
            {
                assembler.RotateBuilding((AssemblerBuilding.Direction)currentRotationIndex);
            }
            UpdateCostUI();
            return true;
        }
        else
        {
            Destroy(newBuildingObject);
            return false;
        }
    }


    public void SelectBuilding(GameObject prefab)
    {
        if (selectedPrefab == prefab)
        {
            CancelPlacement();
            return;
        }

        CancelPlacement();
        selectedPrefab = prefab;
        currentRotationIndex = 1;

        selectedGridObjectComponent = selectedPrefab.GetComponent<GridObject>();

        isPlacingConveyor = selectedGridObjectComponent.objectType == GridObjectType.ConveyorBelt ||
                        selectedGridObjectComponent.objectType == GridObjectType.OverheadConveyor ||
                        selectedGridObjectComponent is PipeBuilding; // <--- DODANO TO

        UpdateCostUI();

        UpdatePreview(true);

        if (UIManager.Instance != null && machinePrefabs != null)
        {
            int selectedIndex = machinePrefabs.IndexOf(prefab);
            UIManager.Instance.UpdateMachineSelection(selectedIndex);
        }
    }

private bool CanPlaceBuildingAtPosition(Vector2Int gridPosition, GridObject prefabGridObject, out List<Vector2Int> occupiedTiles)
{
    occupiedTiles = GetOccupiedGridPositions(gridPosition, prefabGridObject.size);
    if (GridManager.Instance == null) return false;

    // --- 1. WALIDACJA PODŁOŻA (WYMAGANE ZŁOŻA) ---
    // Pobieramy obiekty z głównego kafelka (lewy dolny róg)
    List<GridObject> objectsAtMainTile = GridManager.Instance.GetGridObjects(gridPosition);
    ResourceDeposit deposit = objectsAtMainTile.OfType<ResourceDeposit>().FirstOrDefault();

    // Sprawdzenie dla Pumpjacka (musi być Oil)
    if (prefabGridObject is PumpjackBuilding)
    {
        if (deposit == null) return false;
        if (deposit.resourceData.resourceName != "Water" && deposit.resourceData.resourceName != "Oil") return false;
        }

    // Sprawdzenie dla Minera i Extendera (surowce stałe)
    bool isMiner = prefabGridObject.GetComponent<MinerBuilding>() != null;
    bool isExtender = prefabGridObject.GetComponent<MinerExtender>() != null;
    if (isMiner || isExtender)
    {
        string[] validResources = { "Iron Ore", "Copper Ore", "Coal Ore", "Sulfur Ore" };
        if (deposit == null || !validResources.Contains(deposit.resourceData.resourceName)) return false;
    }

    // --- 2. SPRAWDZANIE KOLIZJI (CZY POLE JEST WOLNE) ---
    bool isPlacingOverhead = prefabGridObject.GetComponent<OverheadConveyor>() != null;

    foreach (Vector2Int tile in occupiedTiles)
    {
        List<GridObject> objectsOnTile = GridManager.Instance.GetGridObjects(tile);
        if (objectsOnTile == null) continue;

        foreach (GridObject existing in objectsOnTile)
        {
            // Zasoby (Ore/Oil) nigdy nie blokują stawiania
            if (existing.objectType == GridObjectType.ResourceDeposit) continue;

            // Wyjątek: Wiadukt (Overhead) może stać na zwykłej taśmie
            if (isPlacingOverhead && existing.objectType == GridObjectType.ConveyorBelt) continue;

            // Wyjątek: Zwykła taśma może być pod wiaduktem (jeśli stawiasz taśmę pod istniejącym wiaduktem)
            bool isPlacingBelt = prefabGridObject.objectType == GridObjectType.ConveyorBelt;
            if (isPlacingBelt && existing.objectType == GridObjectType.OverheadConveyor) continue;

            // Jeśli to rura, taśma lub budynek - blokujemy
            // Sprawdzamy też flagę isBlockingPlacement dla pewności
            if (existing.isBlockingPlacement || 
                existing.objectType == GridObjectType.Building || 
                existing.objectType == GridObjectType.ConveyorBelt || 
                existing.objectType == GridObjectType.OverheadConveyor)
            {
                return false; 
            }
        }
    }

    return true;
}

    private void TryRemoveBuilding()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;
        Vector2Int gridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);

        List<GridObject> placedObjects = GridManager.Instance.GetGridObjects(gridPosition);

        if (placedObjects.Count == 0)
        {
            Debug.LogWarning($"Pr�ba usuni�cia: Na polu {gridPosition.x}, {gridPosition.y} nie ma �adnego obiektu.");
            return;
        }

        GridObject objectToRemove = placedObjects
            .Where(o => o.objectType != GridObjectType.ResourceDeposit)
            .OrderByDescending(o => o.objectType == GridObjectType.OverheadConveyor ? 3 :
                                     o.objectType == GridObjectType.Building ? 2 :
                                     o.objectType == GridObjectType.ConveyorBelt ? 1 : 0)
            .FirstOrDefault();


        if (objectToRemove != null)
        {
            if (objectToRemove.objectType == GridObjectType.Building ||
                objectToRemove.objectType == GridObjectType.ConveyorBelt ||
                objectToRemove.objectType == GridObjectType.OverheadConveyor ||
                objectToRemove.isBlockingPlacement)
            {

                foreach (var cost in objectToRemove.constructionCost)
                {
                    PlayerInventory.Instance.AddItem(cost.resource, cost.amount);
                }

                GridManager.Instance.RemoveGridObject(objectToRemove, gridPosition);
                Destroy(objectToRemove.gameObject);
            }
            else
            {
                return;
            }
        }
    }


    private List<Vector2Int> GetOccupiedGridPositions(Vector2Int basePosition, Vector2Int size)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                positions.Add(basePosition + new Vector2Int(x, y));
            }
        }
        return positions;
    }

    private void UpdatePreview(bool forceCreate = false)
    {
        if (selectedPrefab == null || GridManager.Instance == null || selectedGridObjectComponent == null)
        {
            if (previewObject != null) Destroy(previewObject);
            return;
        }

        if (previewObject == null || forceCreate)
        {
            if (previewObject != null) Destroy(previewObject);

            GameObject previewPrefab = selectedPrefab;
            previewObject = Instantiate(previewPrefab);
            previewObject.name = "Preview: " + selectedPrefab.name;

            Component[] components = previewObject.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c is GridObject || c is MonoBehaviour)
                {
                    ((MonoBehaviour)c).enabled = false;
                }
                if (c is Collider2D)
                {
                    ((Collider2D)c).enabled = false;
                }
            }

            SetPreviewOpacity(previewObject, 0.5f);
            RotateSelectedBuilding();
        }

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;

        Vector2Int gridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);

        Vector3 worldPosition = GridManager.Instance.GridToWorld(gridPosition);

        if (selectedGridObjectComponent.size.x > 1 || selectedGridObjectComponent.size.y > 1)
        {
            if (selectedGridObjectComponent.size == new Vector2Int(3, 3))
            {
                float offset = GridManager.Instance.tileSize;
                worldPosition += new Vector3(offset, offset, 0);
            }
            else
            {
                float offset = GridManager.Instance.tileSize / 2f;
                worldPosition += new Vector3(offset, offset, 0);
            }
        }

        previewObject.transform.position = worldPosition;

        List<Vector2Int> occupiedTiles;
        bool isBlocked = !CanPlaceBuildingAtPosition(gridPosition, selectedGridObjectComponent, out occupiedTiles);
        bool canAfford = HasRequiredResources(selectedGridObjectComponent);

        Color color = (isBlocked || !canAfford) ? Color.red : new Color(1f, 1f, 1f, 0.5f);
        SetPreviewColor(previewObject, color);

        UpdateCostUI();
    }

    private void UpdateCostUI()
    {
        UIManager.Instance?.UpdateCostDisplay(selectedGridObjectComponent);
    }

    private void SetPreviewOpacity(GameObject obj, float opacity)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = opacity;
            sr.color = color;
        }
    }

    private void SetPreviewColor(GameObject obj, Color color)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color newColor = color;
            newColor.a = sr.color.a;
            sr.color = newColor;
        }
    }

    public void CancelPlacement()
    {
        selectedPrefab = null;

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        UIManager.Instance?.UpdateCostDisplay(null);
        UIManager.Instance?.UpdateMachineSelection(-1);

        currentRotationIndex = 1;
        isPlacingConveyor = false;
        lastPlacedGridPos = new Vector2Int(int.MaxValue, int.MaxValue);
    }

    private bool HasRequiredResources(GridObject prefabGridObject)
    {
        if (prefabGridObject.constructionCost == null || prefabGridObject.constructionCost.Count == 0) return true;

        foreach (var cost in prefabGridObject.constructionCost)
        {
            if (PlayerInventory.Instance.GetItemCount(cost.resource) < cost.amount)
                return false;
        }
        return true;
    }

    private void SpendResources(GridObject prefabGridObject)
    {
        foreach (var cost in prefabGridObject.constructionCost)
        {
            PlayerInventory.Instance.RemoveItem(cost.resource, cost.amount);
        }
    }
}