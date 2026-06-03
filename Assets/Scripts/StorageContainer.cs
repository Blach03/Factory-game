using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

public class StorageContainer : GridObject
{
    [Header("Ustawienia Magazynu")]
    public float checkInterval = 0.5f;
    private float checkTimer;

    [Header("Limit i Stan")]
    public int itemLimit = 100;
    private ResourceData trackedResource = null;

    protected override void Awake()
    {
        // 1. Wywo�aj Awake z GridObject (kt�ry wywo�a Awake z SavableEntity)
        // Jest to niezb�dne, aby wygenerowa� uniqueID dla zapisu!
        base.Awake();

        // 2. Twoja dotychczasowa logika
        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    void Start()
    {
        checkTimer = checkInterval;
    }

    void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            TryCollectItemFromWorld();
            checkTimer = checkInterval;
        }
    }

    private List<Item> ScanAreaForItems()
    {
        List<Item> foundItems = new List<Item>();
        if (GridManager.Instance == null) return foundItems;

        Item item = GridManager.Instance.GetItemAtGridSpot(occupiedPosition);
        if (item != null && !item.isBeingMoved)
        {
            foundItems.Add(item);
        }

        return foundItems;
    }

    private void TryCollectItemFromWorld()
    {
        if (PlayerInventory.Instance == null) return;

        List<Item> itemsToCollect = ScanAreaForItems();

        Item itemToCollect = itemsToCollect.FirstOrDefault();

        if (itemToCollect == null || itemToCollect.itemData == null)
        {
            return;
        }

        ResourceData newItemResource = itemToCollect.itemData;

        if (trackedResource == null)
        {
            trackedResource = newItemResource;
        }
        else if (trackedResource != newItemResource)
        {
            return;
        }

        int currentCount = PlayerInventory.Instance.GetItemCount(trackedResource);

        if (currentCount < itemLimit)
        {
            PlayerInventory.Instance.AddItem(trackedResource, 1);
            TutorialItemTracker.OnItemMovedByStorageToInventory();
            if (trackedResource != null && trackedResource.resourceName == "Copper Bar")
            {
                TutorialItemTracker.OnCopperBarMovedByStorageToInventory();
            }
            Destroy(itemToCollect.gameObject);

            Debug.Log($"[Storage: {occupiedPosition}] Zebrano {newItemResource.resourceName}. Stan: {currentCount + 1}/{itemLimit} (Skanowanie).");
        }
    }

    public void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenStorageLimitUI(this);
        }
    }

    public void SetLimit(int newLimit)
    {
        if (newLimit < 0) newLimit = 0;
        itemLimit = newLimit;

        checkTimer = 0f;

        Debug.Log($"Limit dla {trackedResource?.resourceName ?? "Nieustawiony"} ustawiony na: {itemLimit}");
    }

    public ResourceData GetTrackedResource()
    {
        return trackedResource;
    }

    public override string SaveComponentData()
    {
        StorageContainerComponentData data = new StorageContainerComponentData
        {
            hasItemLimit = true,
            itemLimit = this.itemLimit,
            trackedResourceName = this.trackedResource != null ? this.trackedResource.resourceName : null
        };

        // U�ywamy wbudowanej metody Unity
        return JsonUtility.ToJson(data);
    }

    public override string GetSerializedData()
    {
        return SaveComponentData();
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            StorageContainerComponentData data = JsonUtility.FromJson<StorageContainerComponentData>(json);

            // Backward compatibility: starsze save'y mogły nie mieć pola itemLimit.
            if (data != null && data.hasItemLimit)
            {
                this.itemLimit = Mathf.Max(0, data.itemLimit);
            }

            if (!string.IsNullOrEmpty(data.trackedResourceName))
            {
                // Dodajemy sprawdzenie, czy Instance istnieje
                if (PlayerInventory.Instance != null)
                {
                    this.trackedResource = FindResourceByName(data.trackedResourceName);
                }
                else
                {
                    // Je�li inventory jeszcze nie ma, spr�bujmy za�adowa� z Resources bezpo�rednio
                    this.trackedResource = Resources.Load<ResourceData>("Items/" + data.trackedResourceName);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"B��d wczytywania danych magazynu: {e.Message}");
        }
    }

    private ResourceData FindResourceByName(string name)
    {
        // U�yj PlayerInventory.Instance.GetAllGameResources(), aby znale�� ResourceData
        return PlayerInventory.Instance.GetAllGameResources().FirstOrDefault(r => r.resourceName == name);
    }
}