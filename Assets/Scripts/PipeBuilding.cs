using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PipeBuilding : GridObject
{
    public PipeNetwork CurrentNetwork;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.Building; // Mo¿esz dodaæ GridObjectType.Pipe jeœli wolisz
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    private void Start()
    {
        RefreshNetwork();
    }

    public void RefreshNetwork()
    {
        // ZnajdŸ s¹siednie rury
        List<PipeBuilding> neighborPipes = GetNeighborPipes();

        if (neighborPipes.Count == 0)
        {
            // Nowa, izolowana sieæ
            CurrentNetwork = new PipeNetwork();
            CurrentNetwork.AddPipe(this);
        }
        else
        {
            // Do³¹cz do pierwszej znalezionej sieci
            CurrentNetwork = neighborPipes[0].CurrentNetwork;
            CurrentNetwork.AddPipe(this);

            // Jeœli rura ³¹czy dwa ró¿ne systemy, scal je
            foreach (var neighbor in neighborPipes)
            {
                if (neighbor.CurrentNetwork != CurrentNetwork)
                {
                    MergeNetworks(CurrentNetwork, neighbor.CurrentNetwork);
                }
            }
        }
    }

    private void MergeNetworks(PipeNetwork main, PipeNetwork other)
    {
        foreach (var pipe in other.Pipes)
        {
            pipe.CurrentNetwork = main;
            main.Pipes.Add(pipe);
        }
        foreach (var pump in other.ConnectedPumps)
        {
            main.ConnectedPumps.Add(pump);
        }
    }

    private List<PipeBuilding> GetNeighborPipes()
    {
        List<PipeBuilding> neighbors = new List<PipeBuilding>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            var objs = GridManager.Instance.GetGridObjects(occupiedPosition + dir);
            var pipe = objs?.OfType<PipeBuilding>().FirstOrDefault();
            if (pipe != null) neighbors.Add(pipe);
        }
        return neighbors;
    }

    private void OnDestroy()
    {
        // Przy zniszczeniu rury najbezpieczniej jest rozbiæ sieæ i kazaæ s¹siadom zbudowaæ j¹ od nowa
        if (CurrentNetwork != null)
        {
            List<PipeBuilding> neighbors = GetNeighborPipes();
            CurrentNetwork = null;
            foreach (var n in neighbors)
            {
                // Rekurencyjne przebudowanie sieci (Flood Fill)
                RebuildNetworkFrom(n);
            }
        }
    }

    private void RebuildNetworkFrom(PipeBuilding startPipe)
    {
        // Tutaj powinna nast¹piæ logika Flood Fill, aby sprawdziæ co jest po³¹czone
        // Dla uproszczenia: usuwamy stare sieci s¹siadów i wymuszamy RefreshNetwork()
    }
}