using NUnit.Framework;
using UnityEngine;

public class GridManagerTests : FactoryGameTestBase
{
    private GridManager gridManager;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        gridManager = CreateTestObject<GridManager>();
        gridManager.Awake();
    }

    [TestCase(0, 0, 0, 0)]
    [TestCase(1, 1, 1, 1)]
    [TestCase(2, 3, 2, 3)]
    public void GridToWorld_ReturnsCenterForGridPosition(int x, int y, int expectedX, int expectedY)
    {
        Vector3 worldPosition = gridManager.GridToWorld(new Vector2Int(x, y));
        Assert.AreEqual(expectedX + 0.5f, worldPosition.x, 1e-5f);
        Assert.AreEqual(expectedY + 0.5f, worldPosition.y, 1e-5f);
    }

    [TestCase(0.1f, 0.9f, 0, 0)]
    [TestCase(1.0f, 1.0f, 1, 1)]
    [TestCase(-0.1f, -0.1f, -1, -1)]
    public void WorldToGrid_ReturnsExpectedCoordinates(float worldX, float worldY, int expectedX, int expectedY)
    {
        Vector2Int gridPosition = gridManager.WorldToGrid(new Vector3(worldX, worldY, 0f));
        Assert.AreEqual(expectedX, gridPosition.x);
        Assert.AreEqual(expectedY, gridPosition.y);
    }

    [Test]
    public void ReserveGridSpot_RetainsReservationForNormalLayer()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        Vector2Int gridPosition = new Vector2Int(2, 4);

        gridManager.ReserveGridSpot(gridPosition, item);
        Assert.IsTrue(gridManager.IsGridSpotReserved(gridPosition));
    }

    [Test]
    public void ReserveGridSpot_RetainsReservationForOverheadLayer()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 11;
        Vector2Int gridPosition = new Vector2Int(3, 5);

        gridManager.ReserveGridSpot(gridPosition, item);
        Assert.IsTrue(gridManager.IsOverheadGridSpotReserved(gridPosition));
    }

    [Test]
    public void FinalizeItemPlacement_RemovesNormalReservation()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        Vector2Int gridPosition = new Vector2Int(1, 1);

        gridManager.ReserveGridSpot(gridPosition, item);
        gridManager.FinalizeItemPlacement(gridPosition, item);

        Assert.IsFalse(gridManager.IsGridSpotReserved(gridPosition));
    }

    [Test]
    public void FinalizeItemPlacement_RemovesOverheadReservation()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 11;
        Vector2Int gridPosition = new Vector2Int(1, 2);

        gridManager.ReserveGridSpot(gridPosition, item);
        gridManager.FinalizeItemPlacement(gridPosition, item);

        Assert.IsFalse(gridManager.IsOverheadGridSpotReserved(gridPosition));
    }

    [Test]
    public void OccupyAndClearGridSpot_TracksNormalItemOccupation()
    {
        Item item = CreateTestObject<Item>();
        Vector2Int gridPosition = new Vector2Int(0, 0);

        gridManager.OccupyGridSpot(gridPosition, item);
        Assert.IsTrue(gridManager.IsGridSpotOccupied(gridPosition));
        Assert.AreSame(item, gridManager.GetItemAtGridSpot(gridPosition));

        gridManager.ClearOccupiedGridSpot(gridPosition, item);
        Assert.IsFalse(gridManager.IsGridSpotOccupied(gridPosition));
    }

    [Test]
    public void OccupyAndClearOverheadGridSpot_TracksOverheadItemOccupation()
    {
        Item item = CreateTestObject<Item>();
        Vector2Int gridPosition = new Vector2Int(5, 5);

        gridManager.OccupyOverheadGridSpot(gridPosition, item);
        Assert.IsTrue(gridManager.IsOverheadGridSpotOccupied(gridPosition));
        Assert.AreSame(item, gridManager.GetOverheadItemAtGridSpot(gridPosition));

        gridManager.ClearOccupiedOverheadGridSpot(gridPosition, item);
        Assert.IsFalse(gridManager.IsOverheadGridSpotOccupied(gridPosition));
    }

    [Test]
    public void FindStationaryItemAtGridSpot_ReturnsChildItemInContainer()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.isBeingMoved = false;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(2, 3));
        item.transform.SetParent(gridManager.itemsContainer);

        Item found = gridManager.FindStationaryItemAtGridSpot(new Vector2Int(2, 3), false);
        Assert.AreSame(item, found);
    }

    [Test]
    public void GetGridObjects_ReturnsObjectsAddedAtPosition()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        Vector2Int position = new Vector2Int(4, 4);

        gridManager.AddGridObject(conveyor, position);
        var objects = gridManager.GetGridObjects(position);

        Assert.AreEqual(1, objects.Count);
        Assert.Contains(conveyor, objects);
    }

    [Test]
    public void GetGridObject_ReturnsLowerLayerObjectWhenOverheadExists()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        Vector2Int position = new Vector2Int(6, 6);

        gridManager.AddGridObject(overhead, position);
        gridManager.AddGridObject(conveyor, position);

        GridObject result = gridManager.GetGridObject(position);
        Assert.AreSame(conveyor, result);
    }

    [Test]
    public void TryGetConveyorAt_ReturnsConveyorAfterAdd()
    {
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        Vector2Int position = new Vector2Int(7, 7);

        gridManager.AddGridObject(conveyor, position);

        bool found = gridManager.TryGetConveyorAt(position, out ConveyorBelt result);
        Assert.IsTrue(found);
        Assert.AreSame(conveyor, result);
    }

    [Test]
    public void TryGetOverheadConveyorAt_ReturnsOverheadConveyorAfterAdd()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        Vector2Int position = new Vector2Int(8, 8);

        gridManager.AddGridObject(overhead, position);

        bool found = gridManager.TryGetOverheadConveyorAt(position, out OverheadConveyor result);
        Assert.IsTrue(found);
        Assert.AreSame(overhead, result);
    }

    [Test]
    public void TryGetLowerLayerObjectAt_SkipsOverheadConveyors()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        ConveyorBelt conveyor = CreateTestObject<ConveyorBelt>();
        Vector2Int position = new Vector2Int(9, 9);

        gridManager.AddGridObject(overhead, position);
        gridManager.AddGridObject(conveyor, position);

        bool found = gridManager.TryGetLowerLayerObjectAt(position, out GridObject result);
        Assert.IsTrue(found);
        Assert.AreSame(conveyor, result);
    }

    [Test]
    public void TryGetConveyorAt_ReturnsFalseWhenNoConveyorAtPosition()
    {
        bool found = gridManager.TryGetConveyorAt(new Vector2Int(100, 100), out ConveyorBelt result);

        Assert.IsFalse(found);
        Assert.IsNull(result);
    }

    [Test]
    public void TryGetOverheadConveyorAt_ReturnsFalseWhenNoOverheadAtPosition()
    {
        bool found = gridManager.TryGetOverheadConveyorAt(new Vector2Int(101, 101), out OverheadConveyor result);

        Assert.IsFalse(found);
        Assert.IsNull(result);
    }

    [Test]
    public void TryGetLowerLayerObjectAt_ReturnsFalseWhenOnlyOverheadExists()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        Vector2Int position = new Vector2Int(102, 102);

        gridManager.AddGridObject(overhead, position);

        bool found = gridManager.TryGetLowerLayerObjectAt(position, out GridObject result);
        Assert.IsFalse(found);
        Assert.IsNull(result);
    }

    [Test]
    public void GetGridObjects_ReturnsEmptyListWhenNoObjects()
    {
        var objects = gridManager.GetGridObjects(new Vector2Int(103, 103));
        Assert.IsNotNull(objects);
        Assert.IsEmpty(objects);
    }

    [Test]
    public void GetAllGridObjects_ReturnsEmptyListWhenNoObjects()
    {
        var objects = gridManager.GetAllGridObjects(new Vector2Int(104, 104));
        Assert.IsNotNull(objects);
        Assert.IsEmpty(objects);
    }

    [Test]
    public void GetAllGridObjects_ReturnsAllObjectsAtPosition()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        Vector2Int position = new Vector2Int(10, 10);

        gridManager.AddGridObject(belt, position);
        gridManager.AddGridObject(overhead, position);

        var allObjects = gridManager.GetAllGridObjects(position);
        Assert.AreEqual(2, allObjects.Count);
        Assert.Contains(belt, allObjects);
        Assert.Contains(overhead, allObjects);
    }

    [Test]
    public void RemoveGridObject_RemovesObjectAndClearsBlockingEntries()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        Vector2Int position = new Vector2Int(11, 11);

        gridManager.AddGridObject(belt, position);
        gridManager.RemoveGridObject(belt, position);

        Assert.IsFalse(gridManager.GetGridObjects(position).Contains(belt));
        Assert.IsFalse(gridManager.IsPlacementBlocked(position));
    }

    [Test]
    public void TryReserveGridSpot_ReturnsFalseWhenAlreadyReserved()
    {
        Item first = CreateTestObject<Item>();
        first.gameObject.layer = 8;
        Item second = CreateTestObject<Item>();
        second.gameObject.layer = 8;
        Vector2Int position = new Vector2Int(12, 12);

        Assert.IsTrue(gridManager.TryReserveGridSpot(position, first));
        Assert.IsFalse(gridManager.TryReserveGridSpot(position, second));
    }

    [Test]
    public void CleanupInvalidItemReferenceAt_RemovesItemWhenLayerIsWrong()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 9;
        Vector2Int position = new Vector2Int(13, 13);

        gridManager.OccupyGridSpot(position, item);
        bool cleaned = gridManager.CleanupInvalidItemReferenceAt(position, false);

        Assert.IsTrue(cleaned);
        Assert.IsFalse(gridManager.IsGridSpotOccupied(position));
    }
}