using static PlacementManager;
using System.Collections.Generic;

[System.Serializable]
public struct TechResourceCost // Nowa struktura specjalnie pod JSON
{
    public string resource; // Tutaj trafi tekst "CoalOre"
    public int amount;
}

[System.Serializable]
public class TechnologyNode
{
    public string id;
    public string name;
    public string description;
    public string iconName;
    public string explanationSpriteName; // NOWE: Nazwa sprite'a objaœniaj¹cego
    public List<TechResourceCost> cost; // U¿ywamy nowej struktury ze stringiem
    public List<string> requiredIds;
    public List<string> unlockPreviewIcons;

    [System.NonSerialized] public int depth = 0;
    [System.NonSerialized] public float xPos = 0;
}

[System.Serializable]
public class TechnologyTreeData
{
    public List<TechnologyNode> technologies;
}