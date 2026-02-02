using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private Dictionary<ResourceData, int> itemAmounts = new Dictionary<ResourceData, int>();

    private List<ResourceData> allGameResources;

    [System.Serializable]
    public struct StartingItem
    {
        public ResourceData resource;
        public int amount;
    }

    [Header("Starting Gear")]
    public List<StartingItem> startingInventory;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            LoadAllResources();
            InitializeStartingItems();
        }
    }

    private void InitializeStartingItems()
    {
        // Sprawdzamy, czy SaveManager nie ma ustawionego save'a do wczytania.
        // Jeœli saveToLoad jest pusty, oznacza to Now¹ Grê.
        if (SaveManager.Instance != null && string.IsNullOrEmpty(SaveManager.Instance.saveToLoad))
        {
            Debug.Log("Nowa gra wykryta - dodajê przedmioty startowe.");
            foreach (var item in startingInventory)
            {
                if (item.resource != null && item.amount > 0)
                {
                    AddItem(item.resource, item.amount);
                }
            }
        }
    }

    private void LoadAllResources()
    {
        ResourceData[] loadedResources = Resources.LoadAll<ResourceData>("Items");
        allGameResources = new List<ResourceData>(loadedResources);

        allGameResources = allGameResources.OrderBy(r => r.resourceName).ToList();

    }

    public List<ResourceData> GetAllGameResources()
    {
        return allGameResources;
    }

    public void AddItem(ResourceData resource, int amount = 1)
    {
        if (resource == null) return;

        if (itemAmounts.ContainsKey(resource))
        {
            itemAmounts[resource] += amount;
        }
        else
        {
            itemAmounts.Add(resource, amount);
        }

        UIManager.Instance?.inventoryUI?.UpdateInventoryCounts();
    }

    public bool RemoveItem(ResourceData resource, int amount = 1)
    {
        if (resource == null || !itemAmounts.ContainsKey(resource) || itemAmounts[resource] < amount)
        {
            return false;
        }

        itemAmounts[resource] -= amount;
        if (itemAmounts[resource] <= 0)
        {
            itemAmounts.Remove(resource);
        }

        UIManager.Instance?.inventoryUI?.UpdateInventoryCounts();

        return true;
    }

    public int GetItemCount(ResourceData resource)
    {
        if (resource == null || !itemAmounts.ContainsKey(resource))
        {
            return 0;
        }
        return itemAmounts[resource];
    }

    public Dictionary<ResourceData, int> GetAllItems()
    {
        return new Dictionary<ResourceData, int>(itemAmounts);
    }

    public PlayerInventoryData GetSaveData()
    {
        PlayerInventoryData data = new PlayerInventoryData();
        foreach (var pair in itemAmounts)
        {
            data.resourceNames.Add(pair.Key.resourceName);
            data.amounts.Add(pair.Value);
        }
        return data;
    }

    public void LoadFromSave(PlayerInventoryData data)
    {
        itemAmounts.Clear();

        // £adujemy wszystkie ResourceData z folderu Resources, aby móc je dopasowaæ po nazwie
        ResourceData[] allResources = Resources.LoadAll<ResourceData>("Items");

        for (int i = 0; i < data.resourceNames.Count; i++)
        {
            string rName = data.resourceNames[i];
            int rAmount = data.amounts[i];

            // Szukamy zasobu, którego nazwa (resourceName) zgadza siê z t¹ z JSONa
            ResourceData matchingResource = System.Array.Find(allResources, r => r.resourceName == rName);

            if (matchingResource != null)
            {
                itemAmounts[matchingResource] = rAmount;
            }
        }

        Debug.Log("Inwentarz gracza zosta³ wczytany.");
        // Jeœli masz UI, tutaj wywo³aj jego odœwie¿enie
    }
}