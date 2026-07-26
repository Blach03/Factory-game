using UnityEngine;

public class ResourceDeposit : GridObject
{
    public ResourceData resourceData;

    public override void Awake()
    {
        // Wywo�ujemy base.Awake(), aby zainicjowa� podstawowe parametry GridObject
        base.Awake();

        objectType = GridObjectType.ResourceDeposit;
        // Z�o�a nie blokuj� budowania (mo�na na nich stawia� g�rniki/pasy)
        isBlockingPlacement = false;
    }

    /// <summary>
    /// Kluczowa metoda wywo�ywana przez WorldGenerator
    /// </summary>
    public override void Initialize(Vector2Int gridPos)
    {
        // Wywo�ujemy logik� bazow� (je�li GridObject co� tam ustawia, np. occupiedPosition)
        base.Initialize(gridPos);

        // Nasza specyficzna logika dla z�o�a
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GridToWorld(gridPos);

            // Rejestracja w s�owniku zasob�w
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

    // Opcjonalnie: Zwraca nazw� surowca dla UI
    public string GetResourceName() => resourceData != null ? resourceData.resourceName : "Unknown";
}