using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class RocketSiloBuilding : GridObject, IProductionBuilding
{
    [System.Serializable]
    public class SiloInput
    {
        public ResourceData resource;
        public int requiredAmount;
        public int currentAmount;
    }

    [Header("Ustawienia Budowy")]
    public SiloInput inputA;
    public SiloInput inputB;
    public SiloInput inputC;

    [Header("Parametry")]
    public float buildTime = 60f;
    public float powerConsumption = 100f;
    public int storageCapacity = 500;

    [Header("Grafika Silosu")]
    public SpriteRenderer siloRenderer;
    public Sprite emptySiloSprite;
    public Sprite readySiloSprite;
    public Sprite rocketIcon;

    [Header("Efekt Startu Rakiety")]
    public SpriteRenderer flyingRocketRenderer; // Sprite samej rakiety (dziecko silosu)
    public float acceleration = 5f;
    private bool isLaunching = false;
    private float currentLaunchSpeed = 0f;
    private Vector3 initialRocketLocalPos;

    private bool isBuilding = false;
    private float timer;
    private int rocketCount = 0;
    private static LayerMask itemLayerMask;
    private AssemblyRecipeData runtimeRecipe;

    [Header("Ustawienia Startu Wyk³adniczego")]
    public float startSpeed = 0.01f; // Prêdkoœæ pocz¹tkowa
    public float multiplierPerSecond = 2f; // Mno¿nik (podwajanie)
    private float launchTimer = 0f; // Czas od startu

    public IBuildingRecipe GetCurrentRecipe() => GetOrCreateRuntimeRecipe();

    public int GetInputCount(int slotIndex)
    {
        if (slotIndex == 0) return inputA.currentAmount;
        if (slotIndex == 1) return inputB.currentAmount;
        if (slotIndex == 2) return inputC.currentAmount;
        return 0;
    }

    public float GetProgressTimer() => timer;
    public int GetCurrentOutputAmount() => rocketCount;
    int IProductionBuilding.inputCapacity => storageCapacity;
    int IProductionBuilding.outputCapacity => 1;

    protected override void Awake()
    {
        base.Awake();
        size = new Vector2Int(5, 5);
        if (itemLayerMask == 0) itemLayerMask = LayerMask.GetMask("Item");

        // Zapamiêtujemy startow¹ pozycjê rakiety (wewn¹trz silosu)
        if (flyingRocketRenderer != null)
        {
            initialRocketLocalPos = flyingRocketRenderer.transform.localPosition;
            flyingRocketRenderer.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        timer = buildTime;
        UpdateVisuals();
    }

    void Update()
    {
        if (isLaunching)
        {
            launchTimer += Time.deltaTime;

            // Wzór: prêdkoœæ = startSpeed * (2 ^ czas)
            float currentLaunchSpeed = startSpeed * Mathf.Pow(multiplierPerSecond, launchTimer);

            // Przesuniêcie (v * dt)
            flyingRocketRenderer.transform.localPosition += Vector3.up * currentLaunchSpeed * Time.deltaTime;

            // Limit wysokoœci lub czasu (20 sekund zgodnie z proœb¹)
            if (launchTimer > 40f || flyingRocketRenderer.transform.localPosition.y > 10000f)
            {
                isLaunching = false;
                flyingRocketRenderer.gameObject.SetActive(false);
                flyingRocketRenderer.transform.localPosition = initialRocketLocalPos;
            }
        }

        if (rocketCount >= 1) return;

        if (powerConsumption > 0 && (isBuilding || CanStart()))
            PowerManager.Instance.RegisterConsumption((int)powerConsumption);

        TryConsumeFromWorld();

        if (!isBuilding)
        {
            if (CanStart() && PowerManager.Instance.HasEnoughPower())
            {
                ConsumeResources();
                isBuilding = true;
                timer = buildTime;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                isBuilding = false;
                rocketCount = 1;
                UpdateVisuals();
            }
        }
    }

    public void Launch()
    {
        if (rocketCount >= 1 && !isLaunching)
        {
            if (flyingRocketRenderer != null)
            {
                flyingRocketRenderer.transform.localPosition = initialRocketLocalPos;
                flyingRocketRenderer.gameObject.SetActive(true);
                isLaunching = true;
                launchTimer = 0f; // Resetujemy licznik czasu dla potêgowania
            }

            rocketCount = 0;
            timer = buildTime;
            UpdateVisuals();
            Debug.Log("RAKIETA WYSTARTOWA£A WYK£ADNICZO!");
        }
    }

    // --- RESZTA LOGIKI (BEZ ZMIAN) ---

    private bool CanStart() =>
        (inputA.resource == null || inputA.currentAmount >= inputA.requiredAmount) &&
        (inputB.resource == null || inputB.currentAmount >= inputB.requiredAmount) &&
        (inputC.resource == null || inputC.currentAmount >= inputC.requiredAmount);

    private void ConsumeResources()
    {
        inputA.currentAmount -= inputA.requiredAmount;
        if (inputB.resource != null) inputB.currentAmount -= inputB.requiredAmount;
        if (inputC.resource != null) inputC.currentAmount -= inputC.requiredAmount;
    }

    private void UpdateVisuals() => siloRenderer.sprite = (rocketCount >= 1) ? readySiloSprite : emptySiloSprite;

    private void TryConsumeFromWorld()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tilePos = occupiedPosition + new Vector2Int(x, y);
                Vector3 worldPos = GridManager.Instance.GridToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0f);
                Collider2D col = Physics2D.OverlapBox(worldPos, Vector2.one * 0.8f, 0f, itemLayerMask);

                if (col != null)
                {
                    Item item = col.GetComponent<Item>();
                    if (item != null && !item.isBeingMoved)
                    {
                        if (CheckAndPickup(item, inputA)) continue;
                        if (CheckAndPickup(item, inputB)) continue;
                        if (CheckAndPickup(item, inputC)) continue;
                    }
                }
            }
        }
    }

    private bool CheckAndPickup(Item item, SiloInput slot)
    {
        if (slot.resource != null && item.itemData == slot.resource && slot.currentAmount < storageCapacity)
        {
            slot.currentAmount++;
            Destroy(item.gameObject);
            return true;
        }
        return false;
    }

    public void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        UIManager.Instance.OpenStatusWindow(this);
    }

    private AssemblyRecipeData GetOrCreateRuntimeRecipe()
    {
        if (runtimeRecipe == null)
        {
            runtimeRecipe = ScriptableObject.CreateInstance<AssemblyRecipeData>();
            runtimeRecipe._recipeName = "Budowa Rakiety";
            runtimeRecipe._primaryInput = inputA.resource;
            runtimeRecipe._primaryInputAmount = inputA.requiredAmount;
            runtimeRecipe._secondaryInput = inputB.resource;
            runtimeRecipe._secondaryInputAmount = inputB.requiredAmount;
            runtimeRecipe.tertiaryInput = inputC.resource;
            runtimeRecipe.tertiaryInputAmount = inputC.requiredAmount;
            runtimeRecipe.assemblyTime = buildTime;
            runtimeRecipe.powerRequirement = powerConsumption;
            runtimeRecipe._outputItem = ScriptableObject.CreateInstance<ResourceData>();
            runtimeRecipe._outputItem.icon = rocketIcon;
            runtimeRecipe._outputItem.resourceName = "Rakieta";
        }
        return runtimeRecipe;
    }
}