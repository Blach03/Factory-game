public static class TutorialItemTracker
{
    public static bool AnyItemMovedByConveyor { get; private set; }
    public static bool AnyItemMovedByStorageToInventory { get; private set; }
    public static bool CopperBarMovedByStorageToInventory { get; private set; }
    public static bool PressedCraftX10IronGear { get; private set; }

    public static void Reset()
    {
        AnyItemMovedByConveyor = false;
        AnyItemMovedByStorageToInventory = false;
        CopperBarMovedByStorageToInventory = false;
        PressedCraftX10IronGear = false;
    }

    public static void ResetStorageToInventory()
    {
        AnyItemMovedByStorageToInventory = false;
    }

    public static void ResetStorageCopperBarToInventory()
    {
        CopperBarMovedByStorageToInventory = false;
    }

    public static void ResetCraftX10IronGear()
    {
        PressedCraftX10IronGear = false;
    }

    public static void OnItemMovedByConveyor()
    {
        AnyItemMovedByConveyor = true;
    }

    public static void OnItemMovedByStorageToInventory()
    {
        AnyItemMovedByStorageToInventory = true;
    }

    public static void OnCopperBarMovedByStorageToInventory()
    {
        CopperBarMovedByStorageToInventory = true;
    }

    public static void OnPressedCraftX10IronGear()
    {
        PressedCraftX10IronGear = true;
    }
}
