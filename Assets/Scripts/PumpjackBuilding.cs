using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PumpjackBuilding : GridObject
{
    [Header("Ustawienia Wydobycia")]
    public ResourceData oilResourceData;
    public float productionPerSecond = 0.2f; // Iloœæ ropy dodawana do sieci

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
        // Sprawdzenie poprawnoœci z³o¿a
        isOnOilDeposit = CheckIfOnOilDeposit();

        if (!isOnOilDeposit)
        {
            Debug.LogWarning($"[Pumpjack] Postawiony na niew³aœciwym polu na {occupiedPosition}. Wymagane z³o¿e: Oil.");
        }
        else
        {
            // Poinformuj s¹siednie rury, ¿e siê pojawi³eœ
            NotifyAdjacentPipes();
        }
    }

    // Pumpjack nie potrzebuje Update() do wyrzucania przedmiotów, 
    // bo PipeNetwork bêdzie pobieraæ od niego TotalProduction.

    private bool CheckIfOnOilDeposit()
    {
        if (GridManager.Instance == null) return false;

        var deposit = GridManager.Instance.GetGridObjects(occupiedPosition)
                        .OfType<ResourceDeposit>().FirstOrDefault();

        return deposit != null && deposit.resourceData.resourceName == "Oil";
    }

    /// <summary>
    /// Metoda sprawdzaj¹ca, czy pumpjack jest aktywny i na w³aœciwym z³o¿u.
    /// Wywo³ywana przez PipeNetwork.
    /// </summary>
    public float GetCurrentOutput()
    {
        return isOnOilDeposit ? productionPerSecond : 0f;
    }

    /// <summary>
    /// Szuka rur wokó³ pumpjacka i wymusza na nich aktualizacjê sieci.
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
                // Nakazujemy rurze odœwie¿yæ swoj¹ sieæ, by uwzglêdni³a tego pumpjacka
                pipe.RefreshNetwork();
            }
        }
    }

    private void OnDestroy()
    {
        // Po usuniêciu pumpjacka, rury musz¹ wiedzieæ, ¿e znikn¹³ Ÿród³o
        NotifyAdjacentPipes();
    }
}