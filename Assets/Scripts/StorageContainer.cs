using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

public class StorageContainer : GridObject
{
    [Header("Ustawienia Magazynu")]
    public float checkInterval = 0.5f;
    private float checkTimer;

    private static LayerMask itemLayerMask;

    [Header("Limit i Stan")]
    public int itemLimit = 100;
    private ResourceData trackedResource = null;

    protected override void Awake()
    {
        // 1. Wywo³aj Awake z GridObject (który wywo³a Awake z SavableEntity)
        // Jest to niezbêdne, aby wygenerowaæ uniqueID dla zapisu!
        base.Awake();

        // 2. Twoja dotychczasowa logika
        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);

        if (itemLayerMask == 0)
        {
            itemLayerMask = LayerMask.GetMask("Item");
        }
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

        float tileSize = GridManager.Instance.tileSize;
        Vector2 scanSize = new Vector2(tileSize, tileSize);
        Vector2Int tileGridPos = occupiedPosition;

        Vector3 tileWorldPos = GridManager.Instance.GridToWorld(tileGridPos) +
                               new Vector3(tileSize / 2f, tileSize / 2f, 0f);

        Collider2D[] foundColliders = Physics2D.OverlapBoxAll(
            tileWorldPos,
            scanSize * 0.9f,
            0f,
            itemLayerMask
        );

        foreach (Collider2D col in foundColliders)
        {
            Item item = col.GetComponent<Item>();

            if (item != null && !item.isBeingMoved)
            {
                foundItems.Add(item);
            }
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
            itemLimit = this.itemLimit,
            trackedResourceName = this.trackedResource != null ? this.trackedResource.resourceName : null
        };

        // U¿ywamy wbudowanej metody Unity
        return JsonUtility.ToJson(data);
    }

    public override void LoadComponentData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            StorageContainerComponentData data = JsonUtility.FromJson<StorageContainerComponentData>(json);
            this.itemLimit = data.itemLimit;

            if (!string.IsNullOrEmpty(data.trackedResourceName))
            {
                // Dodajemy sprawdzenie, czy Instance istnieje
                if (PlayerInventory.Instance != null)
                {
                    this.trackedResource = FindResourceByName(data.trackedResourceName);
                }
                else
                {
                    // Jeœli inventory jeszcze nie ma, spróbujmy za³adowaæ z Resources bezpoœrednio
                    this.trackedResource = Resources.Load<ResourceData>("Items/" + data.trackedResourceName);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"B³¹d wczytywania danych magazynu: {e.Message}");
        }
    }

    private ResourceData FindResourceByName(string name)
    {
        // U¿yj PlayerInventory.Instance.GetAllGameResources(), aby znaleŸæ ResourceData
        return PlayerInventory.Instance.GetAllGameResources().FirstOrDefault(r => r.resourceName == name);
    }
}