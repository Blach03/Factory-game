using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TransportTickManagerTests : FactoryGameTestBase
{
    private TransportTickManager manager;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        manager = CreateTestObject<TransportTickManager>();
        manager.Awake();
    }

    [Test]
    public void RegisterConveyor_AddsConveyorToRegistry()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();

        TransportTickManager.RegisterConveyor(conveyor);
        var registry = GetPrivateStaticFieldValue<HashSet<ConveyorBelt>>(typeof(TransportTickManager), "conveyorRegistry");

        Assert.IsTrue(registry.Contains(conveyor));
    }

    [Test]
    public void UnregisterConveyor_RemovesConveyorFromRegistry()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();

        TransportTickManager.RegisterConveyor(conveyor);
        TransportTickManager.UnregisterConveyor(conveyor);
        var registry = GetPrivateStaticFieldValue<HashSet<ConveyorBelt>>(typeof(TransportTickManager), "conveyorRegistry");

        Assert.IsFalse(registry.Contains(conveyor));
    }

    [Test]
    public void RequestConveyorTick_ImmediateEnqueuesReadyConveyor()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        TransportTickManager.RegisterConveyor(conveyor);

        TransportTickManager.RequestConveyorTick(conveyor, 0);
        var readyQueue = GetPrivateStaticFieldValue<Queue<ConveyorBelt>>(typeof(TransportTickManager), "readyConveyors");

        Assert.AreEqual(1, readyQueue.Count);
        Assert.AreSame(conveyor, readyQueue.Peek());
    }

    [Test]
    public void RequestConveyorTick_DelayedSchedulesConveyor()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        TransportTickManager.RegisterConveyor(conveyor);

        TransportTickManager.RequestConveyorTick(conveyor, 2);
        var delayedBuckets = GetPrivateStaticFieldValue<Dictionary<int, List<ConveyorBelt>>>(typeof(TransportTickManager), "delayedConveyorBuckets");
        var scheduledSet = GetPrivateStaticFieldValue<HashSet<ConveyorBelt>>(typeof(TransportTickManager), "scheduledConveyors");

        Assert.AreEqual(1, delayedBuckets.Count);
        Assert.IsTrue(scheduledSet.Contains(conveyor));
    }

    [Test]
    public void RequestConveyorTick_AlreadyScheduledDoesNotDuplicate()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        TransportTickManager.RegisterConveyor(conveyor);

        TransportTickManager.RequestConveyorTick(conveyor, 1);
        TransportTickManager.RequestConveyorTick(conveyor, 0);

        var scheduledSet = GetPrivateStaticFieldValue<HashSet<ConveyorBelt>>(typeof(TransportTickManager), "scheduledConveyors");
        Assert.AreEqual(1, scheduledSet.Count);
    }

    [Test]
    public void RegisterOverheadConveyor_AddsConveyorToActiveList()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();

        TransportTickManager.RegisterOverheadConveyor(overhead);
        var activeList = GetPrivateStaticFieldValue<List<OverheadConveyor>>(typeof(TransportTickManager), "overheadConveyors");

        Assert.Contains(overhead, activeList);
    }

    [Test]
    public void UnregisterOverheadConveyor_RemovesConveyorFromActiveList()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();

        TransportTickManager.RegisterOverheadConveyor(overhead);
        TransportTickManager.UnregisterOverheadConveyor(overhead);
        var activeList = GetPrivateStaticFieldValue<List<OverheadConveyor>>(typeof(TransportTickManager), "overheadConveyors");

        Assert.IsFalse(activeList.Contains(overhead));
    }

    [Test]
    public void RegisterMovingItem_AddsItemToActiveList()
    {
        Item item = CreateTestObject<Item>();

        TransportTickManager.RegisterMovingItem(item);
        var activeList = GetPrivateStaticFieldValue<List<Item>>(typeof(TransportTickManager), "movingItems");

        Assert.Contains(item, activeList);
    }

    [Test]
    public void UnregisterMovingItem_RemovesItemFromActiveList()
    {
        Item item = CreateTestObject<Item>();

        TransportTickManager.RegisterMovingItem(item);
        TransportTickManager.UnregisterMovingItem(item);
        var activeList = GetPrivateStaticFieldValue<List<Item>>(typeof(TransportTickManager), "movingItems");

        Assert.IsFalse(activeList.Contains(item));
    }

    [Test]
    public void Update_ProcessesReadyConveyorQueue()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        TransportTickManager.RegisterConveyor(conveyor);
        TransportTickManager.RequestConveyorTick(conveyor, 0);

        InvokePrivateInstanceMethod(manager, "Update");

        var readyQueue = GetPrivateStaticFieldValue<Queue<ConveyorBelt>>(typeof(TransportTickManager), "readyConveyors");
        var scheduledSet = GetPrivateStaticFieldValue<HashSet<ConveyorBelt>>(typeof(TransportTickManager), "scheduledConveyors");

        Assert.AreEqual(0, readyQueue.Count);
        Assert.IsFalse(scheduledSet.Contains(conveyor));
    }

    [Test]
    public void Update_ProcessesDelayedConveyorsWhenDue()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        TransportTickManager.RegisterConveyor(conveyor);
        TransportTickManager.RequestConveyorTick(conveyor, 1);

        InvokePrivateInstanceMethod(manager, "Update");

        var delayedBuckets = GetPrivateStaticFieldValue<Dictionary<int, List<ConveyorBelt>>>(typeof(TransportTickManager), "delayedConveyorBuckets");
        Assert.AreEqual(0, delayedBuckets.Count);
    }

    [Test]
    public void Update_TicksOverheadConveyorsOnStride()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        overhead.checkInterval = 0.5f;
        TransportTickManager.RegisterOverheadConveyor(overhead);

        SetPrivateInstanceFieldValue(overhead, "checkTimer", 0f);
        InvokePrivateInstanceMethod(manager, "Update");

        float checkTimer = GetPrivateInstanceFieldValue<float>(overhead, "checkTimer");
        Assert.AreEqual(overhead.checkInterval, checkTimer, 1e-5f);
    }

    [Test]
    public void Update_TicksMovingItemsOnStride()
    {
        Item item = CreateTestObject<Item>();
        GridManager gridManager = CreateTestObject<GridManager>();
        gridManager.Awake();
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        item.SetTargetPosition(gridManager.GridToWorld(new Vector2Int(1, 0)), 10f);

        TransportTickManager.RegisterMovingItem(item);
        InvokePrivateInstanceMethod(manager, "Update");

        Assert.AreNotEqual(gridManager.GridToWorld(new Vector2Int(0, 0)), item.transform.position);
    }

    [Test]
    public void RequestConveyorTick_NullConveyorIsIgnored()
    {
        Assert.DoesNotThrow(() => TransportTickManager.RequestConveyorTick(null, 0));
    }

    [Test]
    public void RegisterOverheadConveyor_NullConveyorIsIgnored()
    {
        Assert.DoesNotThrow(() => TransportTickManager.RegisterOverheadConveyor(null));
    }

    [Test]
    public void RegisterMovingItem_NullItemIsIgnored()
    {
        Assert.DoesNotThrow(() => TransportTickManager.RegisterMovingItem(null));
    }

    [Test]
    public void UnregisterConveyor_NullConveyorIsIgnored()
    {
        Assert.DoesNotThrow(() => TransportTickManager.UnregisterConveyor(null));
    }

    [Test]
    public void UnregisterOverheadConveyor_NullConveyorIsIgnored()
    {
        Assert.DoesNotThrow(() => TransportTickManager.UnregisterOverheadConveyor(null));
    }

    [Test]
    public void UnregisterMovingItem_NullItemIsIgnored()
    {
        Assert.DoesNotThrow(() => TransportTickManager.UnregisterMovingItem(null));
    }
}