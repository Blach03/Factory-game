using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PipeBuilding : GridObject
{
    [System.Serializable]
    private class PipeSaveData
    {
        public int saveVersion = 1;
        public float networkStoredFluid;
        public string networkFluidTypeName;
    }

    [Header("Wizualizacja - Sprite'y")]
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] public Sprite spriteStraight; // rura prosta (1-3)
    [SerializeField] public Sprite spriteCorner;   // zakręt L (1-2)
    [SerializeField] public Sprite spriteTShape;   // trójnik T (1-2-3)
    [SerializeField] public Sprite spriteCross;    // krzyżak + (1-2-3-4)
    [SerializeField] public Sprite spriteEnd;      // zakończenie (opcjonalne)

    public PipeNetwork CurrentNetwork;

    private bool hasPendingNetworkState;
    private float pendingStoredFluid;
    private string pendingFluidTypeName;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.Pipe;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    void Update()
    {
        TryApplyPendingNetworkState();

        // Tylko jedna rura z danej sieci musi wywoływać TickProduction
        // Sprawdzamy, czy jesteśmy "pierwszą" rurą w zbiorze HashSet
        if (CurrentNetwork != null && CurrentNetwork.Pipes.First() == this)
        {
            CurrentNetwork.TickProduction();
        }
    }

    private void Start()
    {
        UpdatePipeVisuals();
        RefreshNetwork();
        TryApplyPendingNetworkState();
        
        // Powiadom sąsiadów, żeby też się zaktualizowali graficznie
        NotifyNeighborsToUpdateVisuals();
    }

    // --- LOGIKA WIZUALNA (Auto-Tiling) ---

    public void UpdatePipeVisuals()
    {
        // Sprawdzamy sąsiadów (Góra, Prawo, Dół, Lewo)
        bool U = CheckForConnection(Vector2Int.up);
        bool R = CheckForConnection(Vector2Int.right);
        bool D = CheckForConnection(Vector2Int.down);
        bool L = CheckForConnection(Vector2Int.left);

        int connectionCount = (U ? 1 : 0) + (R ? 1 : 0) + (D ? 1 : 0) + (L ? 1 : 0);

        switch (connectionCount)
        {
            case 0: // Brak połączeń - domyślnie pozioma
                SetPipe(spriteStraight, 90f);
                break;
            case 1: // Zakończenie
                if (U) SetPipe(spriteEnd ?? spriteStraight, 0f);
                if (R) SetPipe(spriteEnd ?? spriteStraight, 90f);
                if (D) SetPipe(spriteEnd ?? spriteStraight, 180f);
                if (L) SetPipe(spriteEnd ?? spriteStraight, 270f);
                break;
            case 2:
                if (U && D) SetPipe(spriteStraight, 0f);
                else if (R && L) SetPipe(spriteStraight, 90f);
                // Zakręty
                else if (U && R) SetPipe(spriteCorner, 0f);
                else if (R && D) SetPipe(spriteCorner, 90f);
                else if (D && L) SetPipe(spriteCorner, 180f);
                else if (L && U) SetPipe(spriteCorner, 270f);
                break;
            case 3:
                if (L && U && R) SetPipe(spriteTShape, 180f); 
                if (U && R && D) SetPipe(spriteTShape, 270f); 
                if (R && D && L) SetPipe(spriteTShape, 0f);   
                if (D && L && U) SetPipe(spriteTShape, 90f);  
                break;
            case 4:
                SetPipe(spriteCross, 0f);
                break;
        }
    }

    private bool CheckForConnection(Vector2Int direction)
    {
        Vector2Int checkPos = occupiedPosition + direction;
        var objects = GridManager.Instance.GetGridObjects(checkPos);
        if (objects == null) return false;

        return objects.Any(obj => 
            obj is PipeBuilding || 
            obj is PumpjackBuilding ||
            obj is SteamTurbineBuilding ||
            obj is RefineryBuilding); // Dodano Rafinerię
    }
    private void SetPipe(Sprite s, float rotation)
    {
        spriteRenderer.sprite = s;
        spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, -rotation);
    }

    private void NotifyNeighborsToUpdateVisuals()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var dir in dirs)
        {
            var pipe = GridManager.Instance.GetGridObjects(occupiedPosition + dir)?.OfType<PipeBuilding>().FirstOrDefault();
            if (pipe != null) pipe.UpdatePipeVisuals();
        }
    }

    // --- LOGIKA SIECIOWA ---

    public void HardReset()
    {
        if (this == null || gameObject == null) return;

        // Całkowite odcięcie od starej sieci
        CurrentNetwork = null;

        // Wizualny reset (opcjonalnie)
        UpdatePipeVisuals();

        // Wywołujemy RefreshNetwork w następnej klatce, aby upewnić się, 
        // że wszystkie niszczone rury już zniknęły z GridManager-a
        Invoke(nameof(DelayedRefresh), 0.05f);
    }

    private void DelayedRefresh()
    {
        if (this != null) RefreshNetwork();
    }

    public void RefreshNetwork()
    {
        // BEZPIECZNIK: Jeśli obiekt jest właśnie niszczony, nie odświeżaj go
        if(this == null || gameObject == null) return;

        List<PipeBuilding> neighborPipes = GetNeighborPipes();

        // Filtrujemy sąsiadów, aby brać pod uwagę tylko tych, którzy mają przypisaną sieć
        // i nie są właśnie usuwani
        var validNeighbors = neighborPipes.Where(n => n != null && n.CurrentNetwork != null).ToList();

        if (validNeighbors.Count == 0)
        {
            CurrentNetwork = new PipeNetwork();
            CurrentNetwork.AddPipe(this);
        }
        else
        {
            // Bierzemy sieć od pierwszego poprawnego sąsiada
            CurrentNetwork = validNeighbors[0].CurrentNetwork;
            CurrentNetwork.AddPipe(this);

            foreach (var neighbor in validNeighbors)
            {
                // Łączymy sieci tylko jeśli są różne i obie istnieją
                if (neighbor.CurrentNetwork != null && neighbor.CurrentNetwork != CurrentNetwork)
                {
                    CurrentNetwork.Merge(neighbor.CurrentNetwork);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;

        if (CurrentNetwork != null)
        {
            // 1. Kopiujemy listę rur, które zostają na mapie
            var pipesToReset = CurrentNetwork.Pipes.Where(p => p != null && p != this).ToList();

            // 2. Czyścimy starą sieć, żeby przestała istnieć w pamięci
            CurrentNetwork.Pipes.Clear();
            CurrentNetwork.ConnectedPumps.Clear();
            CurrentNetwork = null;

            // 3. Każda rura, która została, musi przejść twardy reset
            foreach (var pipe in pipesToReset)
            {
                pipe.HardReset();
            }
        }

        // Odśwież sąsiadów (grafika)
        NotifyNeighborsToUpdateVisuals();
    }

    private List<PipeBuilding> GetNeighborPipes()
    {
        List<PipeBuilding> neighbors = new List<PipeBuilding>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            var obj = GridManager.Instance.GetGridObjects(occupiedPosition + dir)
                      ?.Find(o => o is PipeBuilding) as PipeBuilding;
            if (obj != null) neighbors.Add(obj);
        }
        return neighbors;
    }

    private void OnMouseDown()
    {
        // Sprawdź czy nie klikamy przez UI
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (CurrentNetwork != null)
        {
            PipeNetworkUI.Instance.OpenWindow(CurrentNetwork);
        }
    }

    public override string GetSerializedData()
    {
        PipeSaveData data = new PipeSaveData();

        if (CurrentNetwork != null)
        {
            data.networkStoredFluid = Mathf.Max(0f, CurrentNetwork.storedFluid);
            data.networkFluidTypeName = CurrentNetwork.FluidType != null
                ? CurrentNetwork.FluidType.resourceName
                : string.Empty;
        }

        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        PipeSaveData data = JsonUtility.FromJson<PipeSaveData>(json);
        if (data == null)
        {
            return;
        }

        // Backward compatibility: stare save'y nie miały danych sieci rur.
        bool hasMeaningfulState = data.networkStoredFluid > 0.001f || !string.IsNullOrWhiteSpace(data.networkFluidTypeName);
        if (!hasMeaningfulState)
        {
            return;
        }

        pendingStoredFluid = Mathf.Max(0f, data.networkStoredFluid);
        pendingFluidTypeName = data.networkFluidTypeName;
        hasPendingNetworkState = true;
    }

    private void TryApplyPendingNetworkState()
    {
        if (!hasPendingNetworkState || CurrentNetwork == null)
        {
            return;
        }

        CurrentNetwork.storedFluid = Mathf.Max(CurrentNetwork.storedFluid, pendingStoredFluid);

        if (CurrentNetwork.FluidType == null && !string.IsNullOrWhiteSpace(pendingFluidTypeName))
        {
            CurrentNetwork.FluidType = ResolveFluidByName(pendingFluidTypeName);
        }

        hasPendingNetworkState = false;
    }

    private ResourceData ResolveFluidByName(string fluidName)
    {
        ResourceData[] allResources = Resources.LoadAll<ResourceData>("Items");
        if (allResources == null || allResources.Length == 0)
        {
            return null;
        }

        foreach (ResourceData resource in allResources)
        {
            if (resource == null)
            {
                continue;
            }

            if (string.Equals(resource.resourceName, fluidName, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resource.name, fluidName, System.StringComparison.OrdinalIgnoreCase))
            {
                return resource;
            }
        }

        return null;
    }
}