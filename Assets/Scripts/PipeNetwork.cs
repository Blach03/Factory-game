using UnityEngine;
using System.Collections.Generic;

public class PipeNetwork
{
    public HashSet<PipeBuilding> Pipes = new HashSet<PipeBuilding>();
    public HashSet<PumpjackBuilding> ConnectedPumps = new HashSet<PumpjackBuilding>();

    public float TotalProduction => ConnectedPumps.Count * 0.2f;
    
    public float storedFluid = 0f;
    public float MaxCapacity => Pipes.Count * 10f; // Każda rura mieści 10 jednostek

    public void TickProduction()
    {
        foreach (var pump in ConnectedPumps)
        {
            storedFluid += pump.GetCurrentOutput() * Time.deltaTime;
        }
        storedFluid = Mathf.Clamp(storedFluid, 0, MaxCapacity);
    }

    public bool RequestFluid(float amount)
    {
        if (storedFluid >= amount)
        {
            storedFluid -= amount;
            return true;
        }
        return false;
    }
    

    public void AddPipe(PipeBuilding pipe)
    {
        if (pipe == null) return;
        
        if (Pipes.Add(pipe))
        {
            pipe.CurrentNetwork = this;
            UpdatePumpsForPipe(pipe);
        }
    }

    // TA METODA NAPRAWI BŁĄD CS1061
    public void Merge(PipeNetwork other)
    {
        if (other == null || other == this) return;

        // Skopiuj wszystkie rury z tamtej sieci do tej
        foreach (var pipe in other.Pipes)
        {
            pipe.CurrentNetwork = this;
            Pipes.Add(pipe);
        }

        // Skopiuj pompy
        foreach (var pump in other.ConnectedPumps)
        {
            ConnectedPumps.Add(pump);
        }

        // Wyczyść tamtą sieć, by nie wisiała w pamięci
        other.Pipes.Clear();
        other.ConnectedPumps.Clear();
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
            if (objects == null) continue;

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