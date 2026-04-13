using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public PlayerInventoryData inventoryData = new PlayerInventoryData();

    public List<EntityData> entityDatas = new List<EntityData>();
    public List<string> researchedTechnologyIds = new List<string>();
    public float seedX;
    public float seedY;
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