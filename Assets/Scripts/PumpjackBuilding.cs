using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PumpjackBuilding : GridObject
{
    [Header("Ustawienia Wydobycia")]
    public ResourceData oilResourceData;
    public float productionPerSecond = 0.2f; // Ilo�� ropy dodawana do sieci

    private bool isOnOilDeposit = false;

    protected override void Awake()
    {
        base.Awake();
        objectType = GridObjectType.Building;
        isBlockingPlacement = true;
        size = new Vector2Int(1, 1);
    }

    void Start()
    {
        // Sprawdzenie poprawno�ci z�o�a
        isOnOilDeposit = CheckIfOnOilDeposit();

        if (!isOnOilDeposit)
        {
            Debug.LogWarning($"[Pumpjack] Postawiony na niew�a�ciwym polu na {occupiedPosition}. Wymagane z�o�e: Oil.");
        }
        else
        {
            // Poinformuj s�siednie rury, �e si� pojawi�e�
            NotifyAdjacentPipes();
        }
    }

    // Pumpjack nie potrzebuje Update() do wyrzucania przedmiot�w, 
    // bo PipeNetwork b�dzie pobiera� od niego TotalProduction.

    private bool CheckIfOnOilDeposit()
    {
        if (GridManager.Instance == null) return false;

        var deposit = GridManager.Instance.GetGridObjects(occupiedPosition)
                        .OfType<ResourceDeposit>().FirstOrDefault();

        return deposit != null && deposit.resourceData.resourceName == "Oil";
    }

    /// <summary>
    /// Metoda sprawdzaj�ca, czy pumpjack jest aktywny i na w�a�ciwym z�o�u.
    /// Wywo�ywana przez PipeNetwork.
    /// </summary>
    public float GetCurrentOutput()
    {
        return isOnOilDeposit ? productionPerSecond : 0f;
    }

    /// <summary>
    /// Szuka rur wok� pumpjacka i wymusza na nich aktualizacj� sieci.
    /// </summary>
    public void NotifyAdjacentPipes()
    {
        Vector2Int[] neighbors = {
            occupiedPosition + Vector2Int.up,
            occupiedPosition + Vector2Int.down,
            occupiedPosition + Vector2Int.left,
            occupiedPosition + Vector2Int.right
        };

        foreach (Vector2Int nPos in neighbors)
        {
            var pipe = GridManager.Instance.GetGridObjects(nPos)?.OfType<PipeBuilding>().FirstOrDefault();
            if (pipe != null)
            {
                pipe.UpdatePipeVisuals();
                pipe.RefreshNetwork();
            }
        }
    }

    private void OnDestroy()
    {
        // Po usuni�ciu pumpjacka, rury musz� wiedzie�, �e znikn�� �r�d�o
        NotifyAdjacentPipes();
    }
}