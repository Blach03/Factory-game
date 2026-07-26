using NUnit.Framework;
using UnityEngine;

public class OverheadConveyorTests : FactoryGameTestBase
{
    private GridManager gridManager;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        gridManager = CreateTestObject<GridManager>();
        gridManager.Awake();
    }

    [Test]
    public void SingleOverheadConveyor_IsStartAndEndSegment()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        overhead.travelDirection = ConveyorBelt.Direction.Right;
        overhead.Initialize(new Vector2Int(0, 0));

        Assert.IsTrue(overhead.IsStartSegment);
        Assert.IsTrue(overhead.IsEndSegment);
        Assert.AreEqual(OverheadConveyor.SegmentRole.Single, overhead.CurrentSegmentRole);
    }

    [Test]
    public void ChainOfOverheadConveyors_RecognizesStartMiddleEndRoles()
    {
        OverheadConveyor head = CreateTestObject<OverheadConveyor>();
        OverheadConveyor middle = CreateTestObject<OverheadConveyor>();
        OverheadConveyor tail = CreateTestObject<OverheadConveyor>();

        head.travelDirection = ConveyorBelt.Direction.Right;
        middle.travelDirection = ConveyorBelt.Direction.Right;
        tail.travelDirection = ConveyorBelt.Direction.Right;

        head.Initialize(new Vector2Int(0, 0));
        middle.Initialize(new Vector2Int(1, 0));
        tail.Initialize(new Vector2Int(2, 0));

        Assert.AreEqual(OverheadConveyor.SegmentRole.Start, head.CurrentSegmentRole);
        Assert.AreEqual(OverheadConveyor.SegmentRole.Middle, middle.CurrentSegmentRole);
        Assert.AreEqual(OverheadConveyor.SegmentRole.End, tail.CurrentSegmentRole);
    }

    [Test]
    public void RotateBelt_OnOverheadConveyor_RefreshesNeighborState()
    {
        OverheadConveyor first = CreateTestObject<OverheadConveyor>();
        OverheadConveyor second = CreateTestObject<OverheadConveyor>();

        first.travelDirection = ConveyorBelt.Direction.Right;
        second.travelDirection = ConveyorBelt.Direction.Right;

        first.Initialize(new Vector2Int(0, 0));
        second.Initialize(new Vector2Int(1, 0));

        second.RotateBelt(ConveyorBelt.Direction.Down);

        Assert.AreEqual(OverheadConveyor.SegmentRole.Single, first.CurrentSegmentRole);
        Assert.IsTrue(second.IsStartSegment);
        Assert.IsTrue(second.IsEndSegment);
    }

    [Test]
    public void OnNeighborChange_UpdatesSegmentStateWhenNeighborDirectionChanges()
    {
        OverheadConveyor first = CreateTestObject<OverheadConveyor>();
        OverheadConveyor second = CreateTestObject<OverheadConveyor>();

        first.travelDirection = ConveyorBelt.Direction.Right;
        second.travelDirection = ConveyorBelt.Direction.Right;

        first.Initialize(new Vector2Int(0, 0));
        second.Initialize(new Vector2Int(1, 0));

        second.travelDirection = ConveyorBelt.Direction.Up;
        first.OnNeighborChange(new Vector2Int(1, 0));

        Assert.AreEqual(OverheadConveyor.SegmentRole.Single, first.CurrentSegmentRole);
    }

    [TestCase(ConveyorBelt.Direction.Right, 0f)]
    [TestCase(ConveyorBelt.Direction.Left, 180f)]
    [TestCase(ConveyorBelt.Direction.Up, 90f)]
    [TestCase(ConveyorBelt.Direction.Down, 270f)]
    public void RotateBelt_SetsExpectedVisualRotation(ConveyorBelt.Direction direction, float expectedAngle)
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        overhead.Awake();
        overhead.RotateBelt(direction);

        Assert.AreEqual(expectedAngle, overhead.transform.rotation.eulerAngles.z, 1e-4f);
    }

    [Test]
    public void NotifyLowerLayerItemAvailable_ResetsCheckTimerToZero()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        overhead.Initialize(new Vector2Int(0, 0));
        SetPrivateInstanceFieldValue(overhead, "checkTimer", 5f);

        overhead.NotifyLowerLayerItemAvailable();

        float checkTimer = GetPrivateInstanceFieldValue<float>(overhead, "checkTimer");
        Assert.AreEqual(0f, checkTimer, 1e-5f);
    }

    [Test]
    public void TickTransport_WhenTimerExpires_ResetsCheckTimer()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        overhead.Initialize(new Vector2Int(0, 0));
        SetPrivateInstanceFieldValue(overhead, "checkTimer", 0f);

        overhead.TickTransport(0.25f);

        float checkTimer = GetPrivateInstanceFieldValue<float>(overhead, "checkTimer");
        Assert.AreEqual(overhead.checkInterval, checkTimer, 1e-4f);
    }
}