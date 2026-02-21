using UnityEngine;
using System.Linq;

public class SteamTurbineBuilding : GridObject
{
    [Header("Ustawienia Turbiny")]
    public float steamConsumptionRate = 0.25f; // na sekundê
    public float powerOutput = 10f; // ile pr¹du daje
    public ResourceData steamResource; // Przypisz Steam w inspektorze

    [Header("Stan")]
    public bool isRunning;
    public PipeNetwork AttachedNetwork;

    protected override void Awake()
    {
        base.Awake();
        size = new Vector2Int(2, 2); // Turbina mo¿e byæ 2x2
        objectType = GridObjectType.Building;
    }

    void Update()
    {
        ConsumeSteamAndGeneratePower();
        isRunning = false;
        // Jeœli rura obok nas zarejestrowa³a nas w sieci
        if (AttachedNetwork != null && AttachedNetwork.FluidType == steamResource)
        {
            float toConsume = steamConsumptionRate * Time.deltaTime;
            if (AttachedNetwork.storedFluid >= toConsume)
            {
                AttachedNetwork.RequestFluid(toConsume);
                isRunning = true;
                PowerManager.Instance.RegisterProduction(powerOutput);
            }
        }
    }

    private void ConsumeSteamAndGeneratePower()
    {
        isRunning = false;

        // Szukamy rury w s¹siedztwie (uproszczony skan wokó³ budynku 2x2)
        PipeBuilding pipe = FindNearbyPipe();

        if (pipe != null && pipe.CurrentNetwork != null)
        {
            PipeNetwork net = pipe.CurrentNetwork;

            // Sprawdzamy czy w sieci jest para i czy jest jej wystarczaj¹co
            if (net.FluidType == steamResource && net.storedFluid > 0)
            {
                float toConsume = steamConsumptionRate * Time.deltaTime;

                // Pobieramy p³yn z sieci
                if (net.RequestFluid(toConsume))
                {
                    isRunning = true;
                    PowerManager.Instance.RegisterProduction(powerOutput);
                }
            }
        }
    }

    private PipeBuilding FindNearbyPipe()
    {
        // Sprawdza pola przylegaj¹ce do budynku 2x2
        for (int x = -1; x <= size.x; x++)
        {
            for (int y = -1; y <= size.y; y++)
            {
                if (x >= 0 && x < size.x && y >= 0 && y < size.y) continue;
                Vector2Int neighborPos = occupiedPosition + new Vector2Int(x, y);
                var pipe = GridManager.Instance.GetGridObjects(neighborPos)?.OfType<PipeBuilding>().FirstOrDefault();
                if (pipe != null) return pipe;
            }
        }
        return null;
    }
}