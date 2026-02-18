using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PipeBuilding : GridObject
{
    [Header("Wizualizacja - Sprite'y")]
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] public Sprite spriteStraight; // rura prosta (1-3)
    [SerializeField] public Sprite spriteCorner;   // zakręt L (1-2)
    [SerializeField] public Sprite spriteTShape;   // trójnik T (1-2-3)
    [SerializeField] public Sprite spriteCross;    // krzyżak + (1-2-3-4)
    [SerializeField] public Sprite spriteEnd;      // zakończenie (opcjonalne)

    public PipeNetwork CurrentNetwork;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    private void Start()
    {
        UpdatePipeVisuals();
        RefreshNetwork();
        
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

    public void RefreshNetwork()
    {
        List<PipeBuilding> neighborPipes = GetNeighborPipes();

        if (neighborPipes.Count == 0)
        {
            CurrentNetwork = new PipeNetwork();
            CurrentNetwork.AddPipe(this);
        }
        else
        {
            CurrentNetwork = neighborPipes[0].CurrentNetwork;
            CurrentNetwork.AddPipe(this);

            foreach (var neighbor in neighborPipes)
            {
                if (neighbor.CurrentNetwork != CurrentNetwork)
                {
                    CurrentNetwork.Merge(neighbor.CurrentNetwork);
                }
            }
        }
    }

    private List<PipeBuilding> GetNeighborPipes()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        List<PipeBuilding> neighbors = new List<PipeBuilding>();
        foreach (var dir in dirs)
        {
            var p = GridManager.Instance.GetGridObjects(occupiedPosition + dir)?.OfType<PipeBuilding>().FirstOrDefault();
            if (p != null) neighbors.Add(p);
        }
        return neighbors;
    }

    private void OnDestroy()
    {
        NotifyNeighborsToUpdateVisuals();
        // Tutaj logika rozdzielania sieci (opcjonalnie)
    }
}