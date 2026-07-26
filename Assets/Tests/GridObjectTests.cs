using NUnit.Framework;
using UnityEngine;

public class GridObjectTests : FactoryGameTestBase
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
    public void Initialize_AddsObjectToGridAndPositionsTransform()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();
        belt.Initialize(new Vector2Int(2, 2));

        Assert.AreEqual(new Vector2Int(2, 2), belt.GetGridPosition());
        Assert.AreEqual(gridManager.GridToWorld(new Vector2Int(2, 2)), belt.transform.position);
        Assert.IsTrue(gridManager.GetGridObjects(new Vector2Int(2, 2)).Contains(belt));
    }

    [Test]
    public void Initialize_LargerSizeOccupiesMultipleGridPositions()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();
        belt.size = new Vector2Int(2, 2);
        belt.Initialize(new Vector2Int(0, 0));

        Assert.IsTrue(gridManager.GetGridObjects(new Vector2Int(0, 0)).Contains(belt));
        Assert.IsTrue(gridManager.GetGridObjects(new Vector2Int(1, 0)).Contains(belt));
        Assert.IsTrue(gridManager.GetGridObjects(new Vector2Int(0, 1)).Contains(belt));
        Assert.IsTrue(gridManager.GetGridObjects(new Vector2Int(1, 1)).Contains(belt));
    }

    [Test]
    public void OnDestroy_RemovesGridObjectFromAllPositions()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();
        belt.size = new Vector2Int(2, 1);
        belt.Initialize(new Vector2Int(3, 3));

        Object.DestroyImmediate(belt.gameObject);

        Assert.IsEmpty(gridManager.GetGridObjects(new Vector2Int(3, 3)));
        Assert.IsEmpty(gridManager.GetGridObjects(new Vector2Int(4, 3)));
    }

    [Test]
    public void GetGridPosition_ReturnsOccupiedPosition()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();
        belt.Initialize(new Vector2Int(5, 5));

        Assert.AreEqual(new Vector2Int(5, 5), belt.GetGridPosition());
    }

    [Test]
    public void Awake_GeneratesUniqueIDForSavableEntity()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();

        Assert.IsFalse(string.IsNullOrEmpty(belt.uniqueID));
    }

    [Test]
    public void Initialize_SetsGameObjectNameBasedOnGridPositionAndSize()
    {
        ConveyorBelt belt = CreateTestObject<ConveyorBelt>();
        belt.Awake();
        belt.Initialize(new Vector2Int(6, 6));

        Assert.IsTrue(belt.gameObject.name.Contains("(6,6)"));
        Assert.IsTrue(belt.gameObject.name.Contains("Size:1x1"));
    }

    [Test]
    public void Destroy_FromGridObject_BaseRemovesEverythingCleanly()
    {
        OverheadConveyor overhead = CreateTestObject<OverheadConveyor>();
        overhead.Awake();
        overhead.Initialize(new Vector2Int(7, 7));

        Object.DestroyImmediate(overhead.gameObject);

        Assert.IsEmpty(gridManager.GetGridObjects(new Vector2Int(7, 7)));
    }
}
