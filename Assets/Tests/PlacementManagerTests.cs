using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlacementManagerTests : FactoryGameTestBase
{
    [Test]
    public void Awake_SetsSingletonInstance()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        Assert.AreSame(manager, PlacementManager.Instance);
    }

    [Test]
    public void Awake_DestroysDuplicateInstance()
    {
        PlacementManager first = CreateTestObject<PlacementManager>();
        first.Awake();
        PlacementManager duplicate = CreateTestObject<PlacementManager>();
        duplicate.Awake();

        // In edit-mode `Destroy` may not remove GameObject immediately; ensure immediate destruction for the test.
        Object.DestroyImmediate(duplicate.gameObject);

        Assert.AreSame(PlacementManager.Instance, first);
        Assert.IsTrue(duplicate == null || duplicate.gameObject == null || duplicate.gameObject.scene.name == null);
    }

    [Test]
    public void Start_CreatesBuildingsContainerWhenMissing()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();
        manager.Start();

        FieldInfo containerField = typeof(PlacementManager).GetField("buildingsContainer", BindingFlags.Instance | BindingFlags.NonPublic);
        Transform container = containerField.GetValue(manager) as Transform;

        Assert.IsNotNull(container);
        Assert.AreEqual("--BUILDINGS--", container.name);
    }

    [Test]
    public void Start_ReusesExistingBuildingsContainer()
    {
        GameObject existing = new GameObject("--BUILDINGS--");
        existing.transform.position = Vector3.one;

        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();
        manager.Start();

        FieldInfo containerField = typeof(PlacementManager).GetField("buildingsContainer", BindingFlags.Instance | BindingFlags.NonPublic);
        Transform container = containerField.GetValue(manager) as Transform;

        Assert.IsNotNull(container);
        Assert.AreSame(existing.transform, container);
    }

    [Test]
    public void EnsureSelectionBorderTexture_CreatesTextureWhenNoneExists()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        MethodInfo method = typeof(PlacementManager).GetMethod("EnsureSelectionBorderTexture", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(manager, null);
        FieldInfo textureField = typeof(PlacementManager).GetField("selectionBorderTexture", BindingFlags.Instance | BindingFlags.NonPublic);
        Texture2D texture = textureField.GetValue(manager) as Texture2D;

        Assert.IsNotNull(texture);
        Assert.AreEqual(1, texture.width);
        Assert.AreEqual(1, texture.height);
    }

    [Test]
    public void SelectedPrefab_DefaultsToNull()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        FieldInfo selectedPrefab = typeof(PlacementManager).GetField("selectedPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNull(selectedPrefab.GetValue(manager));
    }

    [Test]
    public void CanRotateWithoutSelectedPrefab_DoesNotThrow()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        // Ensure a main camera exists and a GridManager is present so the method can safely query the scene.
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";

        GridManager gridManager = CreateTestObject<GridManager>();
        gridManager.Awake();

        Assert.DoesNotThrow(() => manager.GetType().GetMethod("TryRotatePlacedBuilding", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(manager, null));
    }

    [Test]
    public void CanCancelPlacementWithoutSelectedPrefab_DoesNotThrow()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        Assert.DoesNotThrow(() => manager.GetType().GetMethod("CancelPlacement", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(manager, null));
    }

    [Test]
    public void UndoHistory_StartsEmpty()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        FieldInfo undoHistoryField = typeof(PlacementManager).GetField("undoHistory", BindingFlags.Instance | BindingFlags.NonPublic);
        var undoHistory = undoHistoryField.GetValue(manager) as System.Collections.IList;

        Assert.IsNotNull(undoHistory);
        Assert.AreEqual(0, undoHistory.Count);
    }

    [Test]
    public void AreaClipboardEntries_StartEmpty()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        FieldInfo areaClipboardField = typeof(PlacementManager).GetField("areaClipboardEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        var entries = areaClipboardField.GetValue(manager) as System.Collections.IList;

        Assert.IsNotNull(entries);
        Assert.AreEqual(0, entries.Count);
    }

    [Test]
    public void Start_CreatesBuildingsContainerWithCorrectName()
    {
        GameObject container = new GameObject("TestContainer");
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();
        manager.Start();

        FieldInfo containerField = typeof(PlacementManager).GetField("buildingsContainer", BindingFlags.Instance | BindingFlags.NonPublic);
        Transform buildingsContainer = containerField.GetValue(manager) as Transform;

        Assert.IsNotNull(buildingsContainer);
        Assert.AreEqual("--BUILDINGS--", buildingsContainer.name);
    }

    [Test]
    public void Awake_DoesNotDestroyFirstInstanceWhenOnlyOneExists()
    {
        PlacementManager first = CreateTestObject<PlacementManager>();
        first.Awake();

        Assert.AreSame(first, PlacementManager.Instance);
    }

    [Test]
    public void CancelPlacement_RemovesPreviewObjectAndClearsFlags()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        GameObject preview = CreateGameObject("Preview");
        var previewField = typeof(PlacementManager).GetField("previewObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        previewField.SetValue(manager, preview);

        manager.CancelPlacement();

        Assert.IsNull(previewField.GetValue(manager));
        Assert.IsFalse(manager.IsPlacementOrAreaToolActive());
    }

    [Test]
    public void IsPlacementOrAreaToolActive_ReturnsFalseByDefault()
    {
        PlacementManager manager = CreateTestObject<PlacementManager>();
        manager.Awake();

        Assert.IsFalse(manager.IsPlacementOrAreaToolActive());
    }
}
