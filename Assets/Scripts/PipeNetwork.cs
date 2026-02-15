using UnityEngine;
using System.Collections.Generic;

public class PipeNetwork
{
    public HashSet<PipeBuilding> Pipes = new HashSet<PipeBuilding>();
    public HashSet<PumpjackBuilding> ConnectedPumps = new HashSet<PumpjackBuilding>();

    public float TotalProduction => ConnectedPumps.Count * 0.2f; // Przyk³adowa suma produkcji

    public void AddPipe(PipeBuilding pipe)
    {
        if (Pipes.Add(pipe))
        {
            pipe.CurrentNetwork = this;
            UpdatePumpsForPipe(pipe);
        }
    }

    private void UpdatePumpsForPipe(PipeBuilding pipe)
    {
        Vector2Int[] neighbors = {
            pipe.occupiedPosition + Vector2Int.up,
            pipe.occupiedPosition + Vector2Int.down,
            pipe.occupiedPosition + Vector2Int.left,
            pipe.occupiedPosition + Vector2Int.right
        };

        foreach (var pos in neighbors)
        {
            var objects = GridManager.Instance.GetGridObjects(pos);
            foreach (var obj in objects)
            {
                if (obj is PumpjackBuilding pump)
                {
                    ConnectedPumps.Add(pump);
                }
            }
        }
    }
}