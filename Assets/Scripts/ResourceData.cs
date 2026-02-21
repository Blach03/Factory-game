using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Factory Game/Resource Data")]
public class ResourceData : ScriptableObject
{
    public string resourceName = "Iron Ore";
    public Sprite icon;

    public Item itemPrefab;
    public bool canBeStoredInInventory = true;
    public string requiredTechId = "";

    public bool isFluid = false;
}