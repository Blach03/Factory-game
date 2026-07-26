using NUnit.Framework;
using UnityEngine;

public class ConveyorAndItemTests : FactoryGameTestBase
{
    private GridManager gridManager;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        gridManager = CreateTestObject<GridManager>();
        gridManager.Awake();
    }

    [TestCase(ConveyorBelt.Direction.Right, 270f)]
    [TestCase(ConveyorBelt.Direction.Left, 90f)]
    [TestCase(ConveyorBelt.Direction.Up, 0f)]
    [TestCase(ConveyorBelt.Direction.Down, 180f)]
    public void ConveyorBelt_RotateBelt_ChangesDirectionAndSetsRotation(ConveyorBelt.Direction direction, float expectedAngle)
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();
        belt.RotateBelt(direction);

        Assert.AreEqual(direction, belt.travelDirection);
        Assert.AreEqual(expectedAngle, belt.transform.rotation.eulerAngles.z, 1e-4f);
    }

    [Test]
    public void Item_IsOnOverheadLayer_ReturnsExpectedFlag()
    {
        Item item = CreateTestObject<Item>();
        item.SetLayerAndSortingOrderForOverhead();

        Assert.IsTrue(item.IsOnOverheadLayer());
    }

    [Test]
    public void ConveyorBelt_ProcessTransportStep_WhenNoItem_ReturnsFalse()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        bool result = belt.ProcessTransportStep(out int retryDelay);

        Assert.IsFalse(result);
        Assert.GreaterOrEqual(retryDelay, 1);
    }

    [Test]
    public void ConveyorBelt_ProcessTransportStep_WhenOutputBlocked_ReturnsWaiting()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));

        gridManager.ReserveGridSpot(new Vector2Int(1, 0), CreateTestObject<Item>());
        belt.NotifyItemArrived(item);

        bool result = belt.ProcessTransportStep(out int retryDelay);
        Assert.IsTrue(result);
        Assert.GreaterOrEqual(retryDelay, 1);
    }

    [Test]
    public void ConveyorBelt_ProcessTransportStep_WhenTargetFree_MovesItem()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));

        belt.NotifyItemArrived(item);
        bool result = belt.ProcessTransportStep(out int retryDelay);

        Assert.IsFalse(result);
        Assert.GreaterOrEqual(retryDelay, 1);
    }

    [Test]
    public void Item_SetTargetPosition_ReservesTargetGridSpotAndMovesState()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));

        item.SetTargetPosition(gridManager.GridToWorld(new Vector2Int(1, 0)), 10f);

        Assert.IsTrue(item.isBeingMoved);
        Assert.IsTrue(gridManager.IsGridSpotReserved(new Vector2Int(1, 0)));
    }

    [Test]
    public void Item_TickTransport_MovesItemTowardTarget()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        item.SetTargetPosition(gridManager.GridToWorld(new Vector2Int(1, 0)), 4f);

        bool moved = item.TickTransport(0.25f);

        Assert.IsTrue(moved);
        Assert.AreNotEqual(gridManager.GridToWorld(new Vector2Int(0, 0)), item.transform.position);
    }

    [Test]
    public void Item_TickTransport_FinishesOnTargetAndReleasesReservation()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        Vector2Int targetGrid = new Vector2Int(1, 0);
        item.SetTargetPosition(gridManager.GridToWorld(targetGrid), 100f);

        bool moved = item.TickTransport(0.5f);

        Assert.IsFalse(item.isBeingMoved);
        Assert.AreEqual(gridManager.GridToWorld(targetGrid), item.transform.position);
        Assert.IsFalse(gridManager.IsGridSpotReserved(targetGrid));
        Assert.IsTrue(gridManager.IsGridSpotOccupied(targetGrid));
    }

    [Test]
    public void Item_SetLayerAndSortingOrderForConveyor_RegistersGridOccupation()
    {
        Item item = CreateTestObject<Item>();
        item.transform.position = gridManager.GridToWorld(new Vector2Int(2, 2));
        item.SetLayerAndSortingOrderForConveyor();

        Assert.AreEqual(8, item.gameObject.layer);
        Assert.IsTrue(gridManager.IsGridSpotOccupied(new Vector2Int(2, 2)));
    }

    [Test]
    public void Item_SetLayerAndSortingOrderForOverhead_RegistersOverheadOccupation()
    {
        Item item = CreateTestObject<Item>();
        item.transform.position = gridManager.GridToWorld(new Vector2Int(3, 3));
        item.SetLayerAndSortingOrderForOverhead();

        Assert.AreEqual(11, item.gameObject.layer);
        Assert.IsTrue(gridManager.IsOverheadGridSpotOccupied(new Vector2Int(3, 3)));
    }

    [Test]
    public void Item_ReconcileGridOccupationFromWorldPosition_UpdatesOccupationToNewLocation()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        // Ensure item lifecycle/registration runs so initial occupation is recorded.
        item.Awake();
        item.SetLayerAndSortingOrderForConveyor();

        // Move and reconcile; Reconcile should clear any previous occupation entries and register the new one.
        item.transform.position = gridManager.GridToWorld(new Vector2Int(1, 0));
        item.ReconcileGridOccupationFromWorldPosition();

        Assert.IsFalse(gridManager.IsGridSpotOccupied(new Vector2Int(0, 0)));
        Assert.IsTrue(gridManager.IsGridSpotOccupied(new Vector2Int(1, 0)));
    }

    [Test]
    public void Item_GetSerializedData_IncludesResourceNameAndPosition()
    {
        ResourceData resourceData = ScriptableObject.CreateInstance<ResourceData>();
        resourceData.resourceName = "TestResource";
        Item item = CreateTestObject<Item>();
        item.Initialize(resourceData);
        item.transform.position = new Vector3(0.1f, 0.2f, 0f);

        string json = item.GetSerializedData();

        Assert.IsTrue(json.Contains("TestResource"));
        Assert.IsTrue(json.Contains("0.1"));
    }

    [Test]
    public void Item_LoadComponentData_SetsTargetAndMovementState()
    {
        ResourceData resourceData = ScriptableObject.CreateInstance<ResourceData>();
        resourceData.resourceName = "LoadResource";
        Item item = CreateTestObject<Item>();
        item.Initialize(resourceData);
        item.transform.position = Vector3.zero;

        string json = "{\"resourceName\":\"LoadResource\",\"pos\":[0,0,0],\"targetPos\":[1,0,0],\"moving\":true,\"speed\":5.0}";

        item.LoadComponentData(json);

        // Simulate Unity lifecycle to ensure layer/sorting and registration occur.
        item.Awake();
        item.Start();

        Assert.IsTrue(item.isBeingMoved);
        Assert.AreEqual(8, item.gameObject.layer);
    }

    [Test]
    public void Item_OnDestroy_FreesReservedGridSpot()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 8;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        item.SetTargetPosition(gridManager.GridToWorld(new Vector2Int(1, 0)), 10f);

        Vector2Int reserved = new Vector2Int(1, 0);
        Assert.IsTrue(gridManager.IsGridSpotReserved(reserved));
        Object.DestroyImmediate(item.gameObject);

        Assert.IsFalse(gridManager.IsGridSpotReserved(reserved));
    }

    [Test]
    public void Item_DropToConveyorAfterCurrentMove_SwitchesToConveyorLayerOnArrival()
    {
        Item item = CreateTestObject<Item>();
        item.gameObject.layer = 11;
        item.transform.position = gridManager.GridToWorld(new Vector2Int(0, 0));
        item.SetLayerAndSortingOrderForOverhead();
        item.DropToConveyorAfterCurrentMove();

        Vector2Int targetGrid = new Vector2Int(1, 0);
        item.SetTargetPosition(gridManager.GridToWorld(targetGrid), 100f);
        item.TickTransport(1f);

        Assert.AreEqual(8, item.gameObject.layer);
        Assert.IsFalse(gridManager.IsOverheadGridSpotOccupied(targetGrid));
        Assert.IsTrue(gridManager.IsGridSpotOccupied(targetGrid));
    }
}