using UnityEngine;

public class ResourceDeposit : GridObject
{
    public ResourceData resourceData;

    protected override void Awake()
    {
        // Wywo³ujemy base.Awake(), aby zainicjowaæ podstawowe parametry GridObject
        base.Awake();

        objectType = GridObjectType.ResourceDeposit;
        // Z³o¿a nie blokuj¹ budowania (mo¿na na nich stawiaæ górniki/pasy)
        isBlockingPlacement = false;
    }

    /// <summary>
    /// Kluczowa metoda wywo³ywana przez WorldGenerator
    /// </summary>
    public override void Initialize(Vector2Int gridPos)
    {
        // Wywo³ujemy logikê bazow¹ (jeœli GridObject coœ tam ustawia, np. occupiedPosition)
        base.Initialize(gridPos);

        // Nasza specyficzna logika dla z³o¿a
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GridToWorld(gridPos);

            // Rejestracja w s³owniku zasobów
            GridManager.Instance.AddGridObject(this, gridPos);
        }
    }

    public Item GetMinedItemPrefab()
    {
        if (resourceData == null)
        {
            Debug.LogError($"ResourceDeposit na polu {occupiedPosition} nie ma przypisanych ResourceData!");
            return null;
        }

        if (resourceData.itemPrefab == null)
        {
            Debug.LogError($"ResourceData ({resourceData.resourceName}) nie ma przypisanego prefaba Item!");
            return null;
        }

        return resourceData.itemPrefab;
    }

    // Opcjonalnie: Zwraca nazwê surowca dla UI
    public string GetResourceName() => resourceData != null ? resourceData.resourceName : "Unknown";
}