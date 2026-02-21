using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PipeNetwork
{
    public HashSet<PipeBuilding> Pipes = new HashSet<PipeBuilding>();
    public HashSet<PumpjackBuilding> ConnectedPumps = new HashSet<PumpjackBuilding>();

    // NOWE: Listy dla turbin i rafinerii podłączonych do sieci
    public HashSet<SteamTurbineBuilding> ConnectedTurbines = new HashSet<SteamTurbineBuilding>();
    public HashSet<RefineryBuilding> ConnectedRefineries = new HashSet<RefineryBuilding>();

    public float storedFluid = 0f;
    public float MaxCapacity => Pipes.Count * 10f; // Każda rura mieści 10 jednostek

    public ResourceData FluidType;

    public void TickProduction()
    {
        if (Pipes.Count == 0) return;

        // Jeśli sieć jest prawie pusta, pozwól na zmianę płynu
        if (storedFluid < 0.1f)
        {
            FluidType = null;
        }

        // Czyszczenie martwych referencji
        ConnectedPumps.RemoveWhere(p => p == null);
        ConnectedTurbines.RemoveWhere(t => t == null);
        ConnectedRefineries.RemoveWhere(r => r == null);

        // Produkcja z pomp
        if (ConnectedPumps.Count > 0)
        {
            foreach (var pump in ConnectedPumps)
            {
                // Inicjalizacja typu płynu w sieci na podstawie pierwszej pompy
                if (FluidType == null && pump.currentExtractedResource != null)
                {
                    FluidType = pump.currentExtractedResource;
                }

                // Dodawaj płyn tylko jeśli typy się zgadzają
                if (FluidType != null && pump.currentExtractedResource == FluidType)
                {
                    storedFluid += pump.GetCurrentOutput() * Time.deltaTime;
                }
            }
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
            UpdateConnectionsForPipe(pipe); // Zmieniona nazwa na uniwersalną
        }
    }

    public float AddFluid(float amount)
    {
        float spaceAvailable = MaxCapacity - storedFluid;
        float canAdd = Mathf.Min(amount, spaceAvailable);

        storedFluid += canAdd;
        return canAdd;
    }

    public void Merge(PipeNetwork other)
    {
        if (other == null || other == this) return;

        foreach (var pipe in other.Pipes)
        {
            pipe.CurrentNetwork = this;
            Pipes.Add(pipe);
        }

        foreach (var pump in other.ConnectedPumps) ConnectedPumps.Add(pump);

        // NOWE: Kopiowanie turbin i rafinerii przy łączeniu sieci
        foreach (var turbine in other.ConnectedTurbines) ConnectedTurbines.Add(turbine);
        foreach (var refinery in other.ConnectedRefineries) ConnectedRefineries.Add(refinery);

        other.Pipes.Clear();
        other.ConnectedPumps.Clear();
        other.ConnectedTurbines.Clear();
        other.ConnectedRefineries.Clear();
    }

    // Zaktualizowana metoda szukająca wszystkich sąsiadujących budynków
    private void UpdateConnectionsForPipe(PipeBuilding pipe)
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
                // Rejestracja Pomp
                if (obj is PumpjackBuilding pump)
                {
                    ConnectedPumps.Add(pump);
                }
                // Rejestracja Turbin
                else if (obj is SteamTurbineBuilding turbine)
                {
                    ConnectedTurbines.Add(turbine);
                }
                // Rejestracja Rafinerii
                else if (obj is RefineryBuilding refinery)
                {
                    ConnectedRefineries.Add(refinery);
                }
            }
        }
    }
}