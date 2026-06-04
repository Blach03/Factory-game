using System.Collections.Generic;
using UnityEngine;

public class TransportTickManager : MonoBehaviour
{
    private static TransportTickManager instance;

    private static readonly List<ConveyorBelt> conveyors = new List<ConveyorBelt>();
    private static readonly List<OverheadConveyor> overheadConveyors = new List<OverheadConveyor>();
    private static readonly List<Item> movingItems = new List<Item>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
        conveyors.Clear();
        overheadConveyors.Clear();
        movingItems.Clear();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject go = new GameObject("TransportTickManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TransportTickManager>();
    }

    public static void RegisterConveyor(ConveyorBelt conveyor)
    {
        if (conveyor == null || conveyors.Contains(conveyor)) return;
        EnsureInstance();
        conveyors.Add(conveyor);
    }

    public static void UnregisterConveyor(ConveyorBelt conveyor)
    {
        if (conveyor == null) return;
        conveyors.Remove(conveyor);
    }

    public static void RegisterOverheadConveyor(OverheadConveyor conveyor)
    {
        if (conveyor == null || overheadConveyors.Contains(conveyor)) return;
        EnsureInstance();
        overheadConveyors.Add(conveyor);
    }

    public static void UnregisterOverheadConveyor(OverheadConveyor conveyor)
    {
        if (conveyor == null) return;
        overheadConveyors.Remove(conveyor);
    }

    public static void RegisterMovingItem(Item item)
    {
        if (item == null || movingItems.Contains(item)) return;
        EnsureInstance();
        movingItems.Add(item);
    }

    public static void UnregisterMovingItem(Item item)
    {
        if (item == null) return;
        movingItems.Remove(item);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = conveyors.Count - 1; i >= 0; i--)
        {
            ConveyorBelt conveyor = conveyors[i];
            if (conveyor == null)
            {
                conveyors.RemoveAt(i);
                continue;
            }

            conveyor.TickTransport(deltaTime);
        }

        for (int i = overheadConveyors.Count - 1; i >= 0; i--)
        {
            OverheadConveyor overhead = overheadConveyors[i];
            if (overhead == null)
            {
                overheadConveyors.RemoveAt(i);
                continue;
            }

            overhead.TickTransport(deltaTime);
        }

        for (int i = movingItems.Count - 1; i >= 0; i--)
        {
            Item item = movingItems[i];
            if (item == null)
            {
                movingItems.RemoveAt(i);
                continue;
            }

            item.TickTransport(deltaTime);

            if (!item.isBeingMoved)
            {
                movingItems.RemoveAt(i);
            }
        }
    }
}