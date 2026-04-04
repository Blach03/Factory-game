using UnityEngine;
using System.Linq;

public class SteamTurbineBuilding : GridObject
{
    [Header("Ustawienia Turbiny")]
    public float steamConsumptionRate = 0.25f;
    public float powerOutput = 10f;
    public ResourceData steamResource;

    [Header("Wizualizacja i Animacja")]
    public SpriteRenderer turbineRenderer; // Przypisz w Inspektorze
    private Sprite[] animFrames;
    private Sprite idleSprite;
    private int currentFrame = 0;
    private float animTimer = 0f;
    private float frameDuration = 0.1f; // 10 FPS

    [Header("Stan")]
    public bool isRunning;
    public PipeNetwork AttachedNetwork;

    protected override void Awake()
    {
        base.Awake();
        size = new Vector2Int(2, 2);
        objectType = GridObjectType.Building;
    }

    void Start()
    {
        // £adowanie animacji
        animFrames = Resources.LoadAll<Sprite>("TurbineAnim");

        if (turbineRenderer != null)
        {
            idleSprite = turbineRenderer.sprite;
        }
    }

    void Update()
    {
        // Wykonaj logikê produkcji
        ConsumeSteamAndGeneratePower();

        // Obs³u¿ wygl¹d turbiny
        HandleAnimation();
    }

    private void ConsumeSteamAndGeneratePower()
    {
        isRunning = false;

        // Najpierw sprawdŸ sieæ przypisan¹ (AttachedNetwork)
        if (AttachedNetwork != null && AttachedNetwork.FluidType == steamResource)
        {
            float toConsume = steamConsumptionRate * Time.deltaTime;
            if (AttachedNetwork.storedFluid >= toConsume)
            {
                AttachedNetwork.RequestFluid(toConsume);
                isRunning = true;
                PowerManager.Instance.RegisterProduction(powerOutput);
                return; // ZnaleŸliœmy paliwo, koñczymy metodê
            }
        }

        // Jeœli nie ma AttachedNetwork, szukamy rury obok (zapasowo)
        PipeBuilding pipe = FindNearbyPipe();
        if (pipe != null && pipe.CurrentNetwork != null)
        {
            PipeNetwork net = pipe.CurrentNetwork;
            if (net.FluidType == steamResource)
            {
                float toConsume = steamConsumptionRate * Time.deltaTime;
                if (net.RequestFluid(toConsume))
                {
                    isRunning = true;
                    PowerManager.Instance.RegisterProduction(powerOutput);
                }
            }
        }
    }

    private void HandleAnimation()
    {
        if (turbineRenderer == null || animFrames == null || animFrames.Length == 0) return;

        if (isRunning)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= frameDuration)
            {
                animTimer = 0f;
                currentFrame++;

                if (currentFrame >= animFrames.Length)
                    currentFrame = 0;

                turbineRenderer.sprite = animFrames[currentFrame];
            }
        }
        else
        {
            // Turbina stoi
            if (turbineRenderer.sprite != idleSprite)
            {
                turbineRenderer.sprite = idleSprite;
                currentFrame = 0;
                animTimer = 0f;
            }
        }
    }

    private PipeBuilding FindNearbyPipe()
    {
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