using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public PlayerInventoryData inventoryData = new PlayerInventoryData();

    public List<EntityData> entityDatas = new List<EntityData>();
    public List<string> researchedTechnologyIds = new List<string>();
    public HandCraftingQueueSaveData handCraftingQueueData = new HandCraftingQueueSaveData();
    public float seedX;
    public float seedY;
    public float totalPlayTimeSeconds;
    public List<ChunkSaveData> generatedResourceChunks = new List<ChunkSaveData>();
}

[System.Serializable]
public class HandCraftingQueueSaveData
{
    public List<HandCraftingQueueEntrySaveData> entries = new List<HandCraftingQueueEntrySaveData>();
    public float currentRecipeRemainingTimeSeconds;
}

[System.Serializable]
public class HandCraftingQueueEntrySaveData
{
    public string recipeType;
    public string recipeName;
    public string recipeAssetName;
    public string outputResourceName;
}

[System.Serializable]
public class ChunkSaveData
{
    public int x;
    public int y;

    public ChunkSaveData() { }

    public ChunkSaveData(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

[System.Serializable]
public class EntityData
{
    public string uniqueID;

    public string prefabName;

    public float[] worldPosition = new float[3];

    public float[] worldRotation;

    public int[] gridPosition = new int[2];

    public int layer;

    public string jsonComponentData;
}

[System.Serializable]
public class PlayerInventoryData
{
    public List<string> resourceNames = new List<string>();
    public List<int> amounts = new List<int>();
}