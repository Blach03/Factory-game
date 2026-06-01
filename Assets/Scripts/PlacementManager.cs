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

    private bool hasPipettePlacementState = false;
    private int pipetteRotationIndex = 1;
    private SmeltingRecipeData pipetteFurnaceRecipe;
    private AssemblyRecipeData pipetteAssemblerRecipe;
    private RefineryRecipeData pipetteRefineryRecipe;
    private bool pipetteHasStorageLimit = false;
    private int pipetteStorageLimit = 100;

    private enum AreaToolMode
    {
        None,
        CopySelecting,
        DeleteSelecting,
        AreaPaste
    }

    private class AreaClipboardEntry
    {
        public GameObject prefab;
        public Vector2Int baseRelativePosition;
        public int baseRotationIndex;

        public SmeltingRecipeData furnaceRecipe;
        public AssemblyRecipeData assemblerRecipe;
        public RefineryRecipeData refineryRecipe;
        public bool hasStorageLimit;
        public int storageLimit;
    }

    private struct PlannedPlacementInfo
    {
        public bool isOverhead;
        public bool isBelt;
        public bool isPipe;
        public bool isBlocking;
        public bool isBuilding;
    }

    private class PlacedObjectSnapshot
    {
        public Vector2Int gridPosition;
        public Type gridObjectType;
        public List<ResourceCost> costs;
    }

    private class RemovedObjectSnapshot
    {
        public GameObject prefab;
        public Vector2Int gridPosition;
        public int rotationIndex;
        public List<ResourceCost> costs;
        public SmeltingRecipeData furnaceRecipe;
        public AssemblyRecipeData assemblerRecipe;
        public RefineryRecipeData refineryRecipe;
        public bool hasStorageLimit;
        public int storageLimit;
    }

    private class UndoAction
    {
        public List<PlacedObjectSnapshot> placed = new List<PlacedObjectSnapshot>();
        public List<RemovedObjectSnapshot> removed = new List<RemovedObjectSnapshot>();
    }

    private AreaToolMode areaToolMode = AreaToolMode.None;
    private bool isAreaDragActive = false;
    private Vector2Int areaDragStartGrid;
    private Vector2Int areaDragCurrentGrid;

    private List<AreaClipboardEntry> areaClipboardEntries = new List<AreaClipboardEntry>();
    private List<GameObject> areaPreviewObjects = new List<GameObject>();
    private Dictionary<ResourceData, int> areaTotalCosts = new Dictionary<ResourceData, int>();

    private Vector2Int areaBoundsSize = Vector2Int.one;
    private int areaRotationIndex = 0;
    private bool areaPlacementIsValid = false;
    private bool areaPlacementCanAfford = false;

    private const int MaxUndoHistory = 50;
    private List<UndoAction> undoHistory = new List<UndoAction>();
    private UndoAction currentDragBatchAction = null;

    private Texture2D selectionBorderTexture;

    private bool tutorialAreaCopySelectionMultiUsed = false;
    private bool tutorialAreaMultiPlaced = false;
    private bool tutorialPipetteUsed = false;
    private bool tutorialAreaDeleteUsed = false;

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
        EnsureSelectionBorderTexture();
    }

    void Update()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrlHeld && Input.GetKeyDown(KeyCode.Z))
        {
            TryUndoLastAction();
            return;
        }

        if (ctrlHeld && Input.GetKeyDown(KeyCode.C))
        {
            EnterCopySelectionMode();
            return;
        }

        if (ctrlHeld && Input.GetKeyDown(KeyCode.X))
        {
            EnterDeleteSelectionMode();
            return;
        }

        if (HandleAreaToolInput())
        {
            return;
        }

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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryPipetteUnderCursor();
        }

        if (Input.GetMouseButtonDown(0) && selectedPrefab != null && !isPlacingConveyor)
        {
            TryPlaceBuilding();
        }

        if (Input.GetMouseButtonDown(0) && selectedPrefab != null && isPlacingConveyor)
        {
            currentDragBatchAction = new UndoAction();
        }

        if (Input.GetMouseButton(0) && selectedPrefab != null && isPlacingConveyor)
        {
            TryPlaceBuildingContinuously();
        }

        if (Input.GetMouseButtonUp(0))
        {
            lastPlacedGridPos = new Vector2Int(int.MaxValue, int.MaxValue);
            if (currentDragBatchAction != null && currentDragBatchAction.placed.Count > 0)
                PushUndoAction(currentDragBatchAction);
            currentDragBatchAction = null;
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

    private bool HandleAreaToolInput()
    {
        if (areaToolMode == AreaToolMode.None) return false;

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return true;
        }

        if (areaToolMode == AreaToolMode.CopySelecting || areaToolMode == AreaToolMode.DeleteSelecting)
        {
            if (Camera.main == null || GridManager.Instance == null) return true;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            Vector2Int mouseGrid = GridManager.Instance.WorldToGrid(mouseWorld);

            if (Input.GetMouseButtonDown(0))
            {
                isAreaDragActive = true;
                areaDragStartGrid = mouseGrid;
                areaDragCurrentGrid = mouseGrid;
            }

            if (isAreaDragActive)
            {
                areaDragCurrentGrid = mouseGrid;

                if (Input.GetMouseButtonUp(0))
                {
                    isAreaDragActive = false;

                    if (areaToolMode == AreaToolMode.CopySelecting)
                    {
                        CommitAreaCopySelection();
                    }
                    else if (areaToolMode == AreaToolMode.DeleteSelecting)
                    {
                        CommitAreaDeleteSelection();
                    }
                }
            }

            return true;
        }

        if (areaToolMode == AreaToolMode.AreaPaste)
        {
            UpdateAreaPreview();

            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateAreaClipboard();
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceAreaClipboard();
            }

            return true;
        }

        return false;
    }


    private void TryDestroyItem()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Zamiast raycast'a, sprawdzamy wszys Item obiekty i szukamy tego najbliżej kursora
        Item[] allItems = FindObjectsOfType<Item>();
        Item closestItem = null;
        float closestDistance = float.MaxValue;

        foreach (Item item in allItems)
        {
            if (item == null || item.itemData == null) continue;
            if (item.isBeingMoved) continue;

            float dist = Vector3.Distance(item.transform.position, mouseWorldPos);
            if (dist < closestDistance && dist < 1f) // 1f = grube przybliżenie na 1 tilek
            {
                closestDistance = dist;
                closestItem = item;
            }
        }

        if (closestItem != null)
        {
            PlayerInventory.Instance.AddItem(closestItem.itemData, 1);
            Debug.Log($"Podniesiono przedmiot: {closestItem.itemData.resourceName}");
            Destroy(closestItem.gameObject);
        }
    }

    private void EnterCopySelectionMode()
    {
        CancelPlacement();
        areaToolMode = AreaToolMode.CopySelecting;
        isAreaDragActive = false;
    }

    private void EnterDeleteSelectionMode()
    {
        CancelPlacement();
        areaToolMode = AreaToolMode.DeleteSelecting;
        isAreaDragActive = false;
    }

    private void CommitAreaCopySelection()
    {
        List<GridObject> objects = GetUniqueObjectsInSelectionRect(areaDragStartGrid, areaDragCurrentGrid);
        if (objects.Count == 0)
        {
            areaToolMode = AreaToolMode.None;
            return;
        }

        if (objects.Count > 1)
            tutorialAreaCopySelectionMultiUsed = true;

        BuildAreaClipboardFromObjects(objects);
        PrepareAreaPreviewObjects();

        areaToolMode = AreaToolMode.AreaPaste;
        areaRotationIndex = 0;
        UpdateAreaPreview();
    }

    private void CommitAreaDeleteSelection()
    {
        List<GridObject> objects = GetUniqueObjectsInSelectionRect(areaDragStartGrid, areaDragCurrentGrid);

        var areaDeleteUndo = new UndoAction();
        foreach (GridObject obj in objects)
        {
            if (obj == null) continue;

            RemovedObjectSnapshot snap = CreateRemovedSnapshot(obj);
            if (snap != null) areaDeleteUndo.removed.Add(snap);

            if (obj.constructionCost != null)
            {
                foreach (var cost in obj.constructionCost)
                {
                    PlayerInventory.Instance.AddItem(cost.resource, cost.amount);
                }
            }

            GridManager.Instance.RemoveGridObject(obj, obj.GetGridPosition());
            Destroy(obj.gameObject);
        }

        int removedItemsCount = PickupItemsInSelectionRect(areaDragStartGrid, areaDragCurrentGrid);

        if (objects.Count > 0 || removedItemsCount > 0)
            tutorialAreaDeleteUsed = true;

        if (areaDeleteUndo.removed.Count > 0)
            PushUndoAction(areaDeleteUndo);

        areaToolMode = AreaToolMode.None;
    }

    private List<GridObject> GetUniqueObjectsInSelectionRect(Vector2Int a, Vector2Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        HashSet<GridObject> unique = new HashSet<GridObject>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int p = new Vector2Int(x, y);
                List<GridObject> atPos = GridManager.Instance.GetGridObjects(p);
                if (atPos == null) continue;

                foreach (GridObject obj in atPos)
                {
                    if (obj == null) continue;
                    if (obj.objectType == GridObjectType.ResourceDeposit) continue;
                    unique.Add(obj);
                }
            }
        }

        return unique.ToList();
    }

    private void BuildAreaClipboardFromObjects(List<GridObject> objects)
    {
        areaClipboardEntries.Clear();
        areaTotalCosts.Clear();

        int minX = objects.Min(o => o.GetGridPosition().x);
        int minY = objects.Min(o => o.GetGridPosition().y);

        foreach (GridObject obj in objects)
        {
            GameObject prefab = ResolvePrefabForPipette(obj);
            if (prefab == null) continue;

            AreaClipboardEntry entry = new AreaClipboardEntry();
            entry.prefab = prefab;
            entry.baseRelativePosition = obj.GetGridPosition() - new Vector2Int(minX, minY);
            entry.baseRotationIndex = GetRotationIndexForObject(obj);

            if (obj is FurnaceBuilding furnace) entry.furnaceRecipe = furnace.currentRecipe;
            if (obj is AssemblerBuilding assembler) entry.assemblerRecipe = assembler.currentRecipe;
            if (obj is RefineryBuilding refinery) entry.refineryRecipe = refinery.currentRecipe;
            if (obj is StorageContainer storage)
            {
                entry.hasStorageLimit = true;
                entry.storageLimit = storage.itemLimit;
            }

            GridObject prefabGridObject = prefab.GetComponent<GridObject>();
            if (prefabGridObject != null && prefabGridObject.constructionCost != null)
            {
                foreach (var cost in prefabGridObject.constructionCost)
                {
                    if (cost.resource == null || cost.amount <= 0) continue;

                    if (!areaTotalCosts.ContainsKey(cost.resource))
                        areaTotalCosts[cost.resource] = 0;

                    areaTotalCosts[cost.resource] += cost.amount;
                }
            }

            areaClipboardEntries.Add(entry);
        }

        int maxRight = 0;
        int maxTop = 0;
        foreach (var e in areaClipboardEntries)
        {
            GridObject g = e.prefab.GetComponent<GridObject>();
            if (g == null) continue;

            maxRight = Mathf.Max(maxRight, e.baseRelativePosition.x + g.size.x);
            maxTop = Mathf.Max(maxTop, e.baseRelativePosition.y + g.size.y);
        }
        areaBoundsSize = new Vector2Int(Mathf.Max(1, maxRight), Mathf.Max(1, maxTop));
    }

    private void PrepareAreaPreviewObjects()
    {
        ClearAreaPreviewObjects();

        foreach (var entry in areaClipboardEntries)
        {
            if (entry.prefab == null) continue;

            GameObject preview = Instantiate(entry.prefab);
            preview.name = "AreaPreview: " + entry.prefab.name;

            Component[] components = preview.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c is GridObject || c is MonoBehaviour)
                    ((MonoBehaviour)c).enabled = false;
                if (c is Collider2D)
                    ((Collider2D)c).enabled = false;
            }

            SetPreviewOpacity(preview, 0.5f);
            areaPreviewObjects.Add(preview);
        }
    }

    private void ClearAreaPreviewObjects()
    {
        foreach (var p in areaPreviewObjects)
        {
            if (p != null) Destroy(p);
        }
        areaPreviewObjects.Clear();
    }

    private int PickupItemsInSelectionRect(Vector2Int a, Vector2Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        int removedCount = 0;

        Item[] items = FindObjectsOfType<Item>();
        foreach (Item item in items)
        {
            if (item == null || item.itemData == null) continue;
            if (item.isBeingMoved) continue;

            Vector2Int gp = GridManager.Instance.WorldToGrid(item.transform.position);
            if (gp.x >= minX && gp.x <= maxX && gp.y >= minY && gp.y <= maxY)
            {
                PlayerInventory.Instance.AddItem(item.itemData, 1);
                Destroy(item.gameObject);
                removedCount++;
            }
        }

        return removedCount;
    }


    private void RotateSelectedBuilding()
    {
        if (areaToolMode == AreaToolMode.AreaPaste)
        {
            RotateAreaClipboard();
            return;
        }

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

        ApplyCurrentRotationToPreviewObject();
    }

    private void ApplyCurrentRotationToPreviewObject()
    {
        if (previewObject == null) return;

        MinerBuilding minerComponent = previewObject.GetComponent<MinerBuilding>();
        ConveyorBelt beltComponent = previewObject.GetComponent<ConveyorBelt>();
        OverheadConveyor overheadComponent = previewObject.GetComponent<OverheadConveyor>();
        FurnaceBuilding furnaceComponent = previewObject.GetComponent<FurnaceBuilding>();
        AssemblerBuilding assemblerComponent = previewObject.GetComponent<AssemblerBuilding>();
        RefineryBuilding refineryComponent = previewObject.GetComponent<RefineryBuilding>();
        MinerExtender extender = previewObject.GetComponent<MinerExtender>();

        if (extender != null)
        {
            extender.RotateBuilding((MinerBuilding.Direction)currentRotationIndex);
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

    private void RotateAreaClipboard()
    {
        areaRotationIndex = (areaRotationIndex + 1) % 4;
        UpdateAreaPreview();
    }

    private Vector2Int RotateOffset(AreaClipboardEntry entry, int rot)
    {
        int r = ((rot % 4) + 4) % 4;

        GridObject g = entry.prefab.GetComponent<GridObject>();
        int w = g != null ? g.size.x : 1;
        int h = g != null ? g.size.y : 1;

        int x = entry.baseRelativePosition.x;
        int y = entry.baseRelativePosition.y;

        int W = areaBoundsSize.x;
        int H = areaBoundsSize.y;

        switch (r)
        {
            case 1:
                // 90° CW z uwzględnieniem wymiarów footprintu obiektu.
                return new Vector2Int(y, W - (x + w));
            case 2:
                return new Vector2Int(W - (x + w), H - (y + h));
            case 3:
                return new Vector2Int(H - (y + h), x);
            default:
                return new Vector2Int(x, y);
        }
    }

    private int RotateDirectionIndex(int baseDir, int rot)
    {
        return (baseDir + rot) % 4;
    }

    private void UpdateAreaPreview()
    {
        if (Camera.main == null || GridManager.Instance == null || areaClipboardEntries.Count == 0) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector2Int anchor = GridManager.Instance.WorldToGrid(mouseWorld);

        areaPlacementIsValid = ValidateAreaPlacement(anchor);
        areaPlacementCanAfford = CanAffordAreaCosts();

        Color color = (areaPlacementIsValid && areaPlacementCanAfford)
            ? new Color(1f, 1f, 1f, 0.5f)
            : new Color(1f, 0.3f, 0.3f, 0.6f);

        for (int i = 0; i < areaClipboardEntries.Count && i < areaPreviewObjects.Count; i++)
        {
            var entry = areaClipboardEntries[i];
            var preview = areaPreviewObjects[i];
            if (entry == null || preview == null) continue;

            GridObject g = entry.prefab.GetComponent<GridObject>();
            if (g == null) continue;

            Vector2Int rotatedOffset = RotateOffset(entry, areaRotationIndex);
            Vector2Int placeBase = anchor + rotatedOffset;
            Vector3 world = GridManager.Instance.GridToWorld(placeBase);

            if (g.size.x > 1 || g.size.y > 1)
            {
                if (g.size == new Vector2Int(3, 3))
                    world += new Vector3(GridManager.Instance.tileSize, GridManager.Instance.tileSize, 0);
                else if (g.size == new Vector2Int(5, 5))
                    world += new Vector3(GridManager.Instance.tileSize * 2f, GridManager.Instance.tileSize * 2f, 0);
                else
                    world += new Vector3(GridManager.Instance.tileSize / 2f, GridManager.Instance.tileSize / 2f, 0);
            }

            preview.transform.position = world;

            int finalDir = RotateDirectionIndex(entry.baseRotationIndex, areaRotationIndex);
            ApplyRotationToObject(preview, finalDir);
            SetPreviewColor(preview, color);
        }

        UpdateAreaCostUI();
    }

    private void ApplyRotationToObject(GameObject obj, int rotationIndex)
    {
        if (obj == null) return;

        MinerBuilding miner = obj.GetComponent<MinerBuilding>();
        ConveyorBelt belt = obj.GetComponent<ConveyorBelt>();
        OverheadConveyor overhead = obj.GetComponent<OverheadConveyor>();
        FurnaceBuilding furnace = obj.GetComponent<FurnaceBuilding>();
        AssemblerBuilding assembler = obj.GetComponent<AssemblerBuilding>();
        RefineryBuilding refinery = obj.GetComponent<RefineryBuilding>();
        MinerExtender extender = obj.GetComponent<MinerExtender>();

        if (extender != null) extender.RotateBuilding((MinerBuilding.Direction)rotationIndex);
        if (miner != null) miner.RotateMiner((MinerBuilding.Direction)rotationIndex);
        if (belt != null) belt.RotateBelt((ConveyorBelt.Direction)rotationIndex);
        if (overhead != null) overhead.RotateBelt((ConveyorBelt.Direction)rotationIndex);
        if (furnace != null) furnace.RotateFurnace((FurnaceBuilding.Direction)rotationIndex);
        if (assembler != null) assembler.RotateBuilding((AssemblerBuilding.Direction)rotationIndex);
        if (refinery != null) refinery.RotateBuilding((RefineryBuilding.Direction)rotationIndex);
    }

    private bool ValidateAreaPlacement(Vector2Int anchor)
    {
        HashSet<Vector2Int> plannedTiles = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, PlannedPlacementInfo> plannedInfo = new Dictionary<Vector2Int, PlannedPlacementInfo>();

        foreach (var entry in areaClipboardEntries)
        {
            if (entry == null || entry.prefab == null) return false;

            GridObject prefabGridObject = entry.prefab.GetComponent<GridObject>();
            if (prefabGridObject == null) return false;

            Vector2Int rotatedOffset = RotateOffset(entry, areaRotationIndex);
            Vector2Int basePos = anchor + rotatedOffset;

            List<Vector2Int> occupied = GetOccupiedGridPositions(basePos, prefabGridObject.size);
            bool isPlacingOverhead = prefabGridObject.GetComponent<OverheadConveyor>() != null;
            bool isPlacingBelt = prefabGridObject.objectType == GridObjectType.ConveyorBelt;
            bool isPlacingPipe = prefabGridObject.GetComponent<PipeBuilding>() != null;

            // Walidacja z�o�a pod miner/pumpjack
            var objectsAtMain = GridManager.Instance.GetGridObjects(basePos);
            ResourceDeposit deposit = objectsAtMain.OfType<ResourceDeposit>().FirstOrDefault();

            if (prefabGridObject is PumpjackBuilding)
            {
                if (deposit == null) return false;
                if (deposit.resourceData.resourceName != "Water" && deposit.resourceData.resourceName != "Oil") return false;
            }

            bool isMiner = prefabGridObject.GetComponent<MinerBuilding>() != null;
            bool isExtender = prefabGridObject.GetComponent<MinerExtender>() != null;
            if (isMiner || isExtender)
            {
                string[] valid = { "Iron Ore", "Copper Ore", "Coal Ore", "Sulfur Ore" };
                if (deposit == null || !valid.Contains(deposit.resourceData.resourceName)) return false;
            }

            // Wewn�trzna kolizja grupy
            foreach (var tile in occupied)
            {
                if (plannedTiles.Contains(tile))
                {
                    // Wyj�tki analogiczne do pojedynczego placementu
                    PlannedPlacementInfo prev = plannedInfo[tile];
                    bool allowed = false;

                    if (isPlacingOverhead && (prev.isBelt || prev.isPipe)) allowed = true;
                    if (prev.isOverhead && (isPlacingBelt || isPlacingPipe)) allowed = true;

                    if (!allowed) return false;
                }
            }

            foreach (var tile in occupied)
            {
                plannedTiles.Add(tile);
                plannedInfo[tile] = new PlannedPlacementInfo
                {
                    isOverhead = isPlacingOverhead,
                    isBelt = isPlacingBelt,
                    isPipe = isPlacingPipe,
                    isBlocking = prefabGridObject.isBlockingPlacement,
                    isBuilding = prefabGridObject.objectType == GridObjectType.Building
                };
            }

            // Kolizja ze �wiatem
            foreach (var tile in occupied)
            {
                List<GridObject> objectsOnTile = GridManager.Instance.GetGridObjects(tile);
                if (objectsOnTile == null) continue;

                foreach (GridObject existing in objectsOnTile)
                {
                    if (existing.objectType == GridObjectType.ResourceDeposit) continue;

                    bool existingIsPipe = existing.GetComponent<PipeBuilding>() != null;

                    if (isPlacingOverhead)
                    {
                        if (existing.objectType == GridObjectType.ConveyorBelt || existingIsPipe) continue;
                    }

                    if (isPlacingBelt)
                    {
                        if (existing.objectType == GridObjectType.OverheadConveyor) continue;
                    }

                    if (isPlacingPipe)
                    {
                        if (existing.objectType == GridObjectType.OverheadConveyor) continue;
                    }

                    if (existing.isBlockingPlacement ||
                        existing.objectType == GridObjectType.Building ||
                        existing.objectType == GridObjectType.ConveyorBelt ||
                        existing.objectType == GridObjectType.OverheadConveyor)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool CanAffordAreaCosts()
    {
        foreach (var kv in areaTotalCosts)
        {
            if (PlayerInventory.Instance.GetItemCount(kv.Key) < kv.Value)
                return false;
        }
        return true;
    }

    private void TryPlaceAreaClipboard()
    {
        if (Camera.main == null || GridManager.Instance == null) return;
        if (areaClipboardEntries.Count == 0) return;
        if (!areaPlacementIsValid || !areaPlacementCanAfford) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector2Int anchor = GridManager.Instance.WorldToGrid(mouseWorld);

        List<GameObject> placedObjects = new List<GameObject>();

        foreach (var entry in areaClipboardEntries)
        {
            GridObject prefabGridObject = entry.prefab.GetComponent<GridObject>();
            if (prefabGridObject == null) continue;

            Vector2Int rotatedOffset = RotateOffset(entry, areaRotationIndex);
            Vector2Int basePos = anchor + rotatedOffset;

            Vector3 world = GridManager.Instance.GridToWorld(basePos);
            if (prefabGridObject.size.x > 1 || prefabGridObject.size.y > 1)
            {
                if (prefabGridObject.size == new Vector2Int(3, 3))
                    world += new Vector3(GridManager.Instance.tileSize, GridManager.Instance.tileSize, 0);
                else if (prefabGridObject.size == new Vector2Int(5, 5))
                    world += new Vector3(GridManager.Instance.tileSize * 2f, GridManager.Instance.tileSize * 2f, 0);
                else
                    world += new Vector3(GridManager.Instance.tileSize / 2f, GridManager.Instance.tileSize / 2f, 0);
            }

            GameObject newObj = Instantiate(entry.prefab, world, Quaternion.identity, buildingsContainer);
            GridObject gridObj = newObj.GetComponent<GridObject>();
            if (gridObj == null)
            {
                Destroy(newObj);
                continue;
            }

            gridObj.Initialize(basePos);

            int finalDir = RotateDirectionIndex(entry.baseRotationIndex, areaRotationIndex);
            ApplyRotationToObject(newObj, finalDir);

            // Receptury / limity
            var furnace = newObj.GetComponent<FurnaceBuilding>();
            if (furnace != null && entry.furnaceRecipe != null) furnace.SetRecipe(entry.furnaceRecipe);

            var assembler = newObj.GetComponent<AssemblerBuilding>();
            if (assembler != null && entry.assemblerRecipe != null) assembler.SetRecipe(entry.assemblerRecipe);

            var refinery = newObj.GetComponent<RefineryBuilding>();
            if (refinery != null && entry.refineryRecipe != null) refinery.SetRecipe(entry.refineryRecipe);

            var storage = newObj.GetComponent<StorageContainer>();
            if (storage != null && entry.hasStorageLimit) storage.SetLimit(entry.storageLimit);

            placedObjects.Add(newObj);
        }

        foreach (var kv in areaTotalCosts)
        {
            PlayerInventory.Instance.RemoveItem(kv.Key, kv.Value);
        }

        if (placedObjects.Count > 0)
        {
            if (placedObjects.Count > 1)
                tutorialAreaMultiPlaced = true;

            var areaPlaceUndo = new UndoAction();
            foreach (var obj in placedObjects)
            {
                GridObject go = obj != null ? obj.GetComponent<GridObject>() : null;
                if (go == null) continue;
                areaPlaceUndo.placed.Add(new PlacedObjectSnapshot
                {
                    gridPosition = go.GetGridPosition(),
                    gridObjectType = go.GetType(),
                    costs = go.constructionCost != null
                        ? new List<ResourceCost>(go.constructionCost)
                        : new List<ResourceCost>()
                });
            }
            PushUndoAction(areaPlaceUndo);
        }

        // Zachowujemy tryb AreaPaste, aby stawia� kolejne kopie obszaru bez ponownego zaznaczania.
        UpdateAreaPreview();
    }

    private void UpdateAreaCostUI()
    {
        if (areaClipboardEntries.Count == 0)
        {
            UIManager.Instance?.UpdateCostDisplay(null);
            return;
        }

        UIManager.Instance?.UpdateCostDisplayFromCosts(areaTotalCosts);
    }

    private void TryPipetteUnderCursor()
    {
        if (Camera.main == null || GridManager.Instance == null) return;

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;
        Vector2Int gridPosition = GridManager.Instance.WorldToGrid(mouseWorldPosition);

        GridObject sourceObject = GetTopPlaceableObjectAt(gridPosition);
        if (sourceObject == null) return;

        GameObject prefabToSelect = ResolvePrefabForPipette(sourceObject);
        if (prefabToSelect == null) return;

        SelectBuildingInternal(prefabToSelect, false);
        CopyPlacementStateFromSource(sourceObject);

        tutorialPipetteUsed = true;

        currentRotationIndex = pipetteRotationIndex;
        ApplyCurrentRotationToPreviewObject();
    }

    public void ResetTutorialShortcutMilestones()
    {
        tutorialAreaCopySelectionMultiUsed = false;
        tutorialAreaMultiPlaced = false;
        tutorialPipetteUsed = false;
        tutorialAreaDeleteUsed = false;
    }

    public bool HasTutorialMultiObjectCopyPasteDone()
    {
        return tutorialAreaCopySelectionMultiUsed && tutorialAreaMultiPlaced;
    }

    public bool HasTutorialPipetteBeenUsed()
    {
        return tutorialPipetteUsed;
    }

    public bool HasTutorialAreaDeleteBeenUsed()
    {
        return tutorialAreaDeleteUsed;
    }

    private GridObject GetTopPlaceableObjectAt(Vector2Int gridPosition)
    {
        List<GridObject> placedObjects = GridManager.Instance.GetGridObjects(gridPosition);
        if (placedObjects == null || placedObjects.Count == 0) return null;

        return placedObjects
            .Where(o => o.objectType != GridObjectType.ResourceDeposit)
            .OrderByDescending(o => o.objectType == GridObjectType.OverheadConveyor ? 3 :
                                    o.objectType == GridObjectType.Building ? 2 :
                                    o.objectType == GridObjectType.ConveyorBelt ? 1 : 0)
            .FirstOrDefault();
    }

    private GameObject ResolvePrefabForPipette(GridObject sourceObject)
    {
        SavableEntity savable = sourceObject.GetComponent<SavableEntity>();
        if (savable != null && !string.IsNullOrEmpty(savable.prefabNameForSave))
        {
            GameObject prefabFromResources = Resources.Load<GameObject>("Prefabs/" + savable.prefabNameForSave);
            if (prefabFromResources != null)
            {
                return prefabFromResources;
            }
        }

        if (machinePrefabs != null)
        {
            foreach (GameObject prefab in machinePrefabs)
            {
                if (prefab == null) continue;
                GridObject prefabGridObj = prefab.GetComponent<GridObject>();
                if (prefabGridObj == null) continue;

                if (prefabGridObj.GetType() == sourceObject.GetType())
                {
                    return prefab;
                }
            }
        }

        return null;
    }

    private void CopyPlacementStateFromSource(GridObject sourceObject)
    {
        ResetPipettePlacementState();

        hasPipettePlacementState = true;
        pipetteRotationIndex = GetRotationIndexForObject(sourceObject);

        if (sourceObject is FurnaceBuilding furnace)
        {
            pipetteFurnaceRecipe = furnace.currentRecipe;
        }
        else if (sourceObject is AssemblerBuilding assembler)
        {
            pipetteAssemblerRecipe = assembler.currentRecipe;
        }
        else if (sourceObject is RefineryBuilding refinery)
        {
            pipetteRefineryRecipe = refinery.currentRecipe;
        }
        else if (sourceObject is StorageContainer storage)
        {
            pipetteHasStorageLimit = true;
            pipetteStorageLimit = storage.itemLimit;
        }
    }

    private int GetRotationIndexForObject(GridObject sourceObject)
    {
        if (sourceObject is MinerBuilding miner) return (int)miner.outputDirection;
        if (sourceObject is ConveyorBelt belt) return (int)belt.travelDirection;
        if (sourceObject is OverheadConveyor overhead) return (int)overhead.travelDirection;
        if (sourceObject is FurnaceBuilding furnace) return (int)furnace.outputDirection;
        if (sourceObject is AssemblerBuilding assembler) return (int)assembler.outputDirection;
        if (sourceObject is RefineryBuilding refinery) return (int)refinery.outputDirection;
        if (sourceObject is MinerExtender extender) return (int)extender.outputDirection;
        return 1;
    }

    private void ApplyPipetteStateToPlacedObject(GameObject placedObject)
    {
        if (!hasPipettePlacementState || placedObject == null) return;

        FurnaceBuilding furnace = placedObject.GetComponent<FurnaceBuilding>();
        if (furnace != null && pipetteFurnaceRecipe != null)
        {
            furnace.SetRecipe(pipetteFurnaceRecipe);
        }

        AssemblerBuilding assembler = placedObject.GetComponent<AssemblerBuilding>();
        if (assembler != null && pipetteAssemblerRecipe != null)
        {
            assembler.SetRecipe(pipetteAssemblerRecipe);
        }

        RefineryBuilding refinery = placedObject.GetComponent<RefineryBuilding>();
        if (refinery != null && pipetteRefineryRecipe != null)
        {
            refinery.SetRecipe(pipetteRefineryRecipe);
        }

        StorageContainer storage = placedObject.GetComponent<StorageContainer>();
        if (storage != null && pipetteHasStorageLimit)
        {
            storage.SetLimit(pipetteStorageLimit);
        }
    }

    private void ResetPipettePlacementState()
    {
        hasPipettePlacementState = false;
        pipetteRotationIndex = 1;
        pipetteFurnaceRecipe = null;
        pipetteAssemblerRecipe = null;
        pipetteRefineryRecipe = null;
        pipetteHasStorageLimit = false;
        pipetteStorageLimit = 100;
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
            if (selectedGridObjectComponent.size == new Vector2Int(3, 3))
            {
                worldPosition += new Vector3(GridManager.Instance.tileSize, GridManager.Instance.tileSize, 0);
            }
            else if (selectedGridObjectComponent.size == new Vector2Int(5, 5))
            {
                // To naprawi stawianie "za wysoko i za bardzo w prawo"
                float offset = GridManager.Instance.tileSize * 2f;
                worldPosition += new Vector3(offset, offset, 0);
            }
            else
            {
                float offset = GridManager.Instance.tileSize / 2f;
                worldPosition += new Vector3(offset, offset, 0);
            }
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

            ApplyPipetteStateToPlacedObject(newBuildingObject);

            // Undo snapshot
            var undoSnap = new PlacedObjectSnapshot
            {
                gridPosition = gridPosition,
                gridObjectType = gridObject.GetType(),
                costs = gridObject.constructionCost != null
                    ? new List<ResourceCost>(gridObject.constructionCost)
                    : new List<ResourceCost>()
            };
            if (currentDragBatchAction != null)
                currentDragBatchAction.placed.Add(undoSnap);
            else
            {
                var undoAct = new UndoAction();
                undoAct.placed.Add(undoSnap);
                PushUndoAction(undoAct);
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
        SelectBuildingInternal(prefab, true);
    }

    private void SelectBuildingInternal(GameObject prefab, bool allowToggleOff)
    {
        if (allowToggleOff && selectedPrefab == prefab)
        {
            CancelPlacement();
            return;
        }

        CancelPlacement();
        selectedPrefab = prefab;
        currentRotationIndex = 1;

        selectedGridObjectComponent = selectedPrefab.GetComponent<GridObject>();

        ResetPipettePlacementState();

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
        List<GridObject> objectsAtMainTile = GridManager.Instance.GetGridObjects(gridPosition);
        ResourceDeposit deposit = objectsAtMainTile.OfType<ResourceDeposit>().FirstOrDefault();

        if (prefabGridObject is PumpjackBuilding)
        {
            if (deposit == null) return false;
            if (deposit.resourceData.resourceName != "Water" && deposit.resourceData.resourceName != "Oil") return false;
        }

        bool isMiner = prefabGridObject.GetComponent<MinerBuilding>() != null;
        bool isExtender = prefabGridObject.GetComponent<MinerExtender>() != null;
        if (isMiner || isExtender)
        {
            string[] validResources = { "Iron Ore", "Copper Ore", "Coal Ore", "Sulfur Ore" };
            if (deposit == null || !validResources.Contains(deposit.resourceData.resourceName)) return false;
        }

        // --- 2. SPRAWDZANIE KOLIZJI (CZY POLE JEST WOLNE) ---
        bool isPlacingOverhead = prefabGridObject.GetComponent<OverheadConveyor>() != null;
        bool isPlacingBelt = prefabGridObject.objectType == GridObjectType.ConveyorBelt;
        // Sprawdzamy czy stawiany obiekt to rura (zakładając komponent PipeBuilding)
        bool isPlacingPipe = prefabGridObject.GetComponent<PipeBuilding>() != null;

        foreach (Vector2Int tile in occupiedTiles)
        {
            List<GridObject> objectsOnTile = GridManager.Instance.GetGridObjects(tile);
            if (objectsOnTile == null) continue;

            foreach (GridObject existing in objectsOnTile)
            {
                // Zasoby nigdy nie blokują
                if (existing.objectType == GridObjectType.ResourceDeposit) continue;

                // --- SYSTEM WYJĄTKÓW DLA WIADUKTU (OVERHEAD) ---

                // Pobieramy informację czy istniejący obiekt to rura
                bool existingIsPipe = existing.GetComponent<PipeBuilding>() != null;

                // Jeśli stawiamy WIADUKT, ignorujemy: taśmy i rury
                if (isPlacingOverhead)
                {
                    if (existing.objectType == GridObjectType.ConveyorBelt || existingIsPipe) continue;
                }

                // Jeśli stawiamy TAŚMĘ, ignorujemy: istniejące wiadukty
                if (isPlacingBelt)
                {
                    if (existing.objectType == GridObjectType.OverheadConveyor) continue;
                }

                // Jeśli stawiamy RURĘ, ignorujemy: istniejące wiadukty
                if (isPlacingPipe)
                {
                    if (existing.objectType == GridObjectType.OverheadConveyor) continue;
                }

                // --- STANDARDOWA BLOKADA ---
                // Jeśli obiekt nie załapał się na powyższe wyjątki i blokuje stawianie - zwróć false
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
                RemovedObjectSnapshot snap = CreateRemovedSnapshot(objectToRemove);

                foreach (var cost in objectToRemove.constructionCost)
                {
                    PlayerInventory.Instance.AddItem(cost.resource, cost.amount);
                }

                GridManager.Instance.RemoveGridObject(objectToRemove, gridPosition);
                Destroy(objectToRemove.gameObject);

                if (snap != null)
                {
                    var undoAct = new UndoAction();
                    undoAct.removed.Add(snap);
                    PushUndoAction(undoAct);
                }
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
            ApplyCurrentRotationToPreviewObject();
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
            else if (selectedGridObjectComponent.size == new Vector2Int(5, 5))
            {
                // Dla 5x5 środek jest 2 kratki od rogu + pół kratki (czyli 2.5f)
                float offset = GridManager.Instance.tileSize * 2f;
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
        ResetPipettePlacementState();
        ClearAreaToolState();
    }

    private void ClearAreaToolState()
    {
        areaToolMode = AreaToolMode.None;
        isAreaDragActive = false;
        areaClipboardEntries.Clear();
        areaTotalCosts.Clear();
        areaRotationIndex = 0;
        areaPlacementIsValid = false;
        areaPlacementCanAfford = false;
        ClearAreaPreviewObjects();
    }

    private void EnsureSelectionBorderTexture()
    {
        if (selectionBorderTexture != null) return;

        selectionBorderTexture = new Texture2D(1, 1);
        selectionBorderTexture.SetPixel(0, 0, Color.white);
        selectionBorderTexture.Apply();
    }

    private Rect GetSelectionScreenRect()
    {
        if (Camera.main == null || GridManager.Instance == null) return Rect.zero;

        int minX = Mathf.Min(areaDragStartGrid.x, areaDragCurrentGrid.x);
        int maxX = Mathf.Max(areaDragStartGrid.x, areaDragCurrentGrid.x);
        int minY = Mathf.Min(areaDragStartGrid.y, areaDragCurrentGrid.y);
        int maxY = Mathf.Max(areaDragStartGrid.y, areaDragCurrentGrid.y);

        Vector3 worldMin = GridManager.Instance.GridToWorld(new Vector2Int(minX, minY));
        Vector3 worldMax = GridManager.Instance.GridToWorld(new Vector2Int(maxX + 1, maxY + 1));

        Vector3 s0 = Camera.main.WorldToScreenPoint(worldMin);
        Vector3 s1 = Camera.main.WorldToScreenPoint(worldMax);

        float x = Mathf.Min(s0.x, s1.x);
        float y = Mathf.Min(s0.y, s1.y);
        float w = Mathf.Abs(s1.x - s0.x);
        float h = Mathf.Abs(s1.y - s0.y);

        // GUI y-axis is top-down
        return new Rect(x, Screen.height - (y + h), w, h);
    }

    private void DrawScreenRectBorder(Rect rect, Color color, float thickness = 2f)
    {
        if (selectionBorderTexture == null) return;

        Color prev = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), selectionBorderTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), selectionBorderTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), selectionBorderTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), selectionBorderTexture);

        GUI.color = prev;
    }

    private void OnGUI()
    {
        if (!isAreaDragActive) return;
        if (areaToolMode != AreaToolMode.CopySelecting && areaToolMode != AreaToolMode.DeleteSelecting) return;

        Rect rect = GetSelectionScreenRect();
        Color border = areaToolMode == AreaToolMode.CopySelecting ? Color.green : Color.red;
        DrawScreenRectBorder(rect, border, 2f);
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

    // ─── UNDO SYSTEM ───────────────────────────────────────────────────────────

    private void PushUndoAction(UndoAction action)
    {
        undoHistory.Add(action);
        if (undoHistory.Count > MaxUndoHistory)
            undoHistory.RemoveAt(0);
    }

    private RemovedObjectSnapshot CreateRemovedSnapshot(GridObject obj)
    {
        if (obj == null) return null;

        GameObject prefab = ResolvePrefabForPipette(obj);
        if (prefab == null) return null;

        var snap = new RemovedObjectSnapshot
        {
            prefab = prefab,
            gridPosition = obj.GetGridPosition(),
            rotationIndex = GetRotationIndexForObject(obj),
            costs = obj.constructionCost != null
                ? new List<ResourceCost>(obj.constructionCost)
                : new List<ResourceCost>()
        };

        if (obj is FurnaceBuilding furnace) snap.furnaceRecipe = furnace.currentRecipe;
        else if (obj is AssemblerBuilding assembler) snap.assemblerRecipe = assembler.currentRecipe;
        else if (obj is RefineryBuilding refinery) snap.refineryRecipe = refinery.currentRecipe;
        if (obj is StorageContainer storage)
        {
            snap.hasStorageLimit = true;
            snap.storageLimit = storage.itemLimit;
        }

        return snap;
    }

    private void TryUndoLastAction()
    {
        if (undoHistory.Count == 0) return;

        UndoAction action = undoHistory[undoHistory.Count - 1];
        undoHistory.RemoveAt(undoHistory.Count - 1);

        // Undo placed objects: find by grid position + type, destroy them and refund costs
        foreach (var snap in action.placed)
        {
            List<GridObject> atPos = GridManager.Instance?.GetGridObjects(snap.gridPosition);
            GridObject gridObj = atPos?.FirstOrDefault(o => o != null && o.GetType() == snap.gridObjectType);
            if (gridObj == null) continue;

            GridManager.Instance.RemoveGridObject(gridObj, snap.gridPosition);

            if (snap.costs != null)
                foreach (var cost in snap.costs)
                    PlayerInventory.Instance.AddItem(cost.resource, cost.amount);

            Destroy(gridObj.gameObject);
        }

        // Undo removed objects: re-place them and deduct costs
        foreach (var snap in action.removed)
        {
            if (snap.prefab == null) continue;

            GridObject prefabGrid = snap.prefab.GetComponent<GridObject>();
            if (prefabGrid == null) continue;

            Vector3 world = GridManager.Instance.GridToWorld(snap.gridPosition);
            if (prefabGrid.size.x > 1 || prefabGrid.size.y > 1)
            {
                if (prefabGrid.size == new Vector2Int(3, 3))
                    world += new Vector3(GridManager.Instance.tileSize, GridManager.Instance.tileSize, 0);
                else if (prefabGrid.size == new Vector2Int(5, 5))
                    world += new Vector3(GridManager.Instance.tileSize * 2f, GridManager.Instance.tileSize * 2f, 0);
                else
                    world += new Vector3(GridManager.Instance.tileSize / 2f, GridManager.Instance.tileSize / 2f, 0);
            }

            GameObject newObj = Instantiate(snap.prefab, world, Quaternion.identity, buildingsContainer);
            GridObject gridObj = newObj.GetComponent<GridObject>();
            if (gridObj == null) { Destroy(newObj); continue; }

            gridObj.Initialize(snap.gridPosition);
            ApplyRotationToObject(newObj, snap.rotationIndex);

            var furnace = newObj.GetComponent<FurnaceBuilding>();
            if (furnace != null && snap.furnaceRecipe != null) furnace.SetRecipe(snap.furnaceRecipe);
            var assembler = newObj.GetComponent<AssemblerBuilding>();
            if (assembler != null && snap.assemblerRecipe != null) assembler.SetRecipe(snap.assemblerRecipe);
            var refinery = newObj.GetComponent<RefineryBuilding>();
            if (refinery != null && snap.refineryRecipe != null) refinery.SetRecipe(snap.refineryRecipe);
            var storageComp = newObj.GetComponent<StorageContainer>();
            if (storageComp != null && snap.hasStorageLimit) storageComp.SetLimit(snap.storageLimit);

            if (newObj.GetComponent<MinerBuilding>() != null || newObj.GetComponent<MinerExtender>() != null)
                foreach (var m in FindObjectsOfType<MinerBuilding>()) m.RecalculateBoost();

            if (snap.costs != null)
                foreach (var cost in snap.costs)
                    PlayerInventory.Instance.RemoveItem(cost.resource, cost.amount);
        }
    }

    public void ClearUndoHistory()
    {
        undoHistory.Clear();
        currentDragBatchAction = null;
    }
}