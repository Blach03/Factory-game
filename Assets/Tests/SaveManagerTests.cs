using NUnit.Framework;
using UnityEngine;

public class SaveManagerTests : FactoryGameTestBase
{
    [Test]
    public void EnsureInstanceExists_CreatesSingletonInstance()
    {
        SaveManager manager = SaveManager.EnsureInstanceExists();

        Assert.IsNotNull(manager);
        Assert.AreSame(manager, SaveManager.Instance);
    }

    [Test]
    public void EnsureInstanceExists_ReturnsExistingInstance()
    {
        SaveManager first = SaveManager.EnsureInstanceExists();
        SaveManager second = SaveManager.EnsureInstanceExists();

        Assert.AreSame(first, second);
    }

    [Test]
    public void Awake_DestroyDuplicateInstanceIfAlreadyExists()
    {
        SaveManager first = CreateTestObject<SaveManager>();
        first.Awake();
        SaveManager duplicate = CreateTestObject<SaveManager>();
        duplicate.Awake();

        var allManagers = Object.FindObjectsOfType<SaveManager>();
        Assert.AreEqual(1, allManagers.Length);
        Assert.AreSame(SaveManager.Instance, allManagers[0]);
    }

    [Test]
    public void SaveFolderPath_UsesPersistentDataPath()
    {
        SaveManager manager = SaveManager.EnsureInstanceExists();
        Assert.IsTrue(manager.SaveFolderPath.Contains(Application.persistentDataPath));
        Assert.IsTrue(System.IO.Directory.Exists(manager.SaveFolderPath));
    }

    [Test]
    public void TotalPlayTimeSeconds_StartsAtZeroAfterEnsureInstance()
    {
        SaveManager manager = SaveManager.EnsureInstanceExists();
        Assert.AreEqual(0f, manager.TotalPlayTimeSeconds, 1e-4f);
    }
}