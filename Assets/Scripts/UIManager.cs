using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI; // <-- TO JEST KLUCZOWE DLA KOMPONENTU IMAGE
using UnityEngine.EventSystems; // DODAJ T� DYREKTYW� NA G�RZE PLIKU

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panele UI - Budynki")]
    public RecipeSelectionUI recipeSelectionPanel;
    public FurnaceStatusUI furnaceStatusPanel;
    public ProductionStatusUI productionStatusPanel;

    [Header("Panele UI - Storage")]
    public GameObject storageLimitPanel;
    public TMP_InputField limitInputField;

    [Header("Panele UI - System")]
    public GameObject inventoryPanel;
    public InventoryUI inventoryUI;
    public GameObject pauseMenuPanel; // Przypisz nowy panel menu pod Escape

    private StorageContainer currentStorage;
    private bool isInventoryOpen = false;

    public GameObject costPanel; // Przypisz w inspektorze
    public GameObject costElementPrefab; // Przypisz w inspektorze
    public Transform costContainer;

    [Header("Technology Tree")]
    public GameObject technologyPanel; // Przypisz TechnologyMenu z hierarchii
    private bool isTechTreeOpen = false;

    [Header("Machine Selection UI")]
    // Tutaj w Inspektorze przeciągnij wszystkie obiekty "Frame" (Image) z Twoich guzików
    public List<Image> machineButtonFrames;
    public Color activeFrameColor = Color.green;
    public Color inactiveFrameColor = new Color(0.2f, 0.2f, 0.2f);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Jeśli gracz klika/pisze w jakimkolwiek InputField, nie reaguj na skróty klawiszowe
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
            {
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeLogic();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInventoryToggle();
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            // Tutaj również warto dodać warunek, aby nie otwierać drzewka, 
            // gdy inne panele są już otwarte (podobnie jak robisz to z inventory)
            if (!isTechTreeOpen && IsAnyPanelOpen()) return;

            ToggleTechnologyTree();
        }

        // Aktualizuj crafting w tle, nawet jeśli HandCraftingManager jest wyłączony
        HandCraftingManager.UpdateCraftingBackground(Time.deltaTime);
    }

    /// <summary>
    /// Logika obs�ugi klawisza ESC: najpierw zamyka aktywne okna, 
    /// a je�li nic nie jest otwarte - otwiera/zamyka Menu Pauzy.
    /// </summary>
    /// 
    public void ToggleTechnologyTree()
    {

        if (!isTechTreeOpen)
        {
            // Je�li otwieramy drzewko, zamknijmy inne panele (np. piec, assembler)
            CloseAllUI();
        }

        isTechTreeOpen = !isTechTreeOpen;
        technologyPanel.SetActive(isTechTreeOpen);

        // Opcjonalnie: Zablokuj ruch kamery lub budowanie, gdy drzewko jest otwarte
        if (isTechTreeOpen)
        {
            PlacementManager.Instance?.CancelPlacement();
            Time.timeScale = 0f; // Pauza gry
        }
        else
        {
            Time.timeScale = 1f; // Wznowienie gry
        }
    }

    private void HandleEscapeLogic()
    {
        // 1. Je�li otwarte jest Menu Pauzy - zamknij je
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            ClosePauseMenu();
            return;
        }

        if (PipeNetworkUI.Instance != null && PipeNetworkUI.Instance.windowPanel.activeSelf)
        {
            PipeNetworkUI.Instance.CloseWindow();
            return; // Zamykamy tylko to okno i przerywamy, żeby nie otwierać pauzy
        }

        // 2. NOWO��: Najpierw sprawd�, czy otwarte jest okno SZCZEGӣ�W technologii
        // U�ywamy Instance, bo TechDetailsUI to Singleton
        if (TechDetailsUI.Instance != null && TechDetailsUI.Instance.panel.activeSelf)
        {
            TechDetailsUI.Instance.Close();
            return; // Przerywamy, dzi�ki czemu drzewko (isTechTreeOpen) pozostanie otwarte
        }

        // 3. Je�li otwarte jest g��wne drzewko technologii - zamknij je
        if (isTechTreeOpen)
        {
            ToggleTechnologyTree();
            return;
        }

        // 4. Je�li otwarte s� inne panele gry (ekwipunek, budynki) - zamknij je
        if (IsAnyPanelOpen())
        {
            CloseAllUI();
        }
        // 5. Je�li nic nie jest otwarte - otw�rz Menu Pauzy
        else
        {
            OpenPauseMenu();
        }
    }

    private void HandleInventoryToggle()
    {
        // Nie pozw�l otworzy� ekwipunku, gdy Menu Pauzy jest aktywne
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf) return;

        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else if (!IsAnyPanelOpen())
        {
            OpenInventory();
        }
    }

    // --- ZARZ�DZANIE MENU PAUZY ---

    public void OpenPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f; // Zatrzymuje czas w grze
        }
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; // Przywraca czas w grze
        }
    }

    // --- ZARZ�DZANIE PANELAMI ---

    public void OpenInventory()
    {
        CloseAllUI();
        inventoryPanel.SetActive(true);
        isInventoryOpen = true;

        if (inventoryUI != null)
        {
            inventoryUI.SetupInventory();
        }
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        isInventoryOpen = false;
    }

    public bool IsAnyPanelOpen()
    {
        return (recipeSelectionPanel != null && recipeSelectionPanel.gameObject.activeSelf) ||
               (furnaceStatusPanel != null && furnaceStatusPanel.gameObject.activeSelf) ||
               (productionStatusPanel != null && productionStatusPanel.gameObject.activeSelf) ||
               (storageLimitPanel != null && storageLimitPanel.activeSelf) ||
               (PipeNetworkUI.Instance != null && PipeNetworkUI.Instance.windowPanel.activeSelf) ||
               inventoryPanel.activeSelf ||
               isTechTreeOpen;
    }

    public void CloseAllUI()
    {
        // Zamyka budynki
        if (recipeSelectionPanel != null) recipeSelectionPanel.gameObject.SetActive(false);
        if (furnaceStatusPanel != null) furnaceStatusPanel.gameObject.SetActive(false);
        if (productionStatusPanel != null) productionStatusPanel.gameObject.SetActive(false);

        // Zamyka storage i inventory
        CloseStorageLimitUI();
        CloseInventory();

        // Upewniamy si�, �e czas p�ynie (na wypadek gdyby zamkni�to co� si�owo)
        if (pauseMenuPanel != null && !pauseMenuPanel.activeSelf)
        {
            Time.timeScale = 1f;
        }
    }

    public void CloseStorageLimitUI()
    {
        if (storageLimitPanel != null)
        {
            storageLimitPanel.SetActive(false);
        }
        currentStorage = null;
    }

    // --- OTWIERANIE KONKRETNYCH STATUS�W ---

    public void OpenRecipeSelection(GridObject building, List<IBuildingRecipe> availableRecipes)
    {
        CloseAllUI();
        if (recipeSelectionPanel != null)
        {
            recipeSelectionPanel.ShowRecipes(building, availableRecipes);
        }
    }

    public void OpenFurnaceStatus(FurnaceBuilding furnace)
    {
        CloseAllUI();
        if (furnaceStatusPanel != null)
        {
            furnaceStatusPanel.ShowStatus(furnace);
        }
    }

    public void OpenStatusWindow(IProductionBuilding building)
    {
        CloseAllUI();
        productionStatusPanel.ShowStatus(building); // productionStatusPanel to Twój stary assemblerStatusPanel
    }

    public void OpenStorageLimitUI(StorageContainer storage)
    {
        CloseAllUI();
        currentStorage = storage;
        if (storageLimitPanel != null)
        {
            storageLimitPanel.SetActive(true);
            if (limitInputField != null && currentStorage != null)
            {
                limitInputField.text = currentStorage.itemLimit.ToString();
            }
        }
    }

    // --- LOGIKA RECEPTUR (Resources) ---

    public List<SmeltingRecipeData> GetAllSmeltingRecipes()
    {
        return Resources.LoadAll<SmeltingRecipeData>("Recipes").ToList();
    }

    public List<AssemblyRecipeData> GetAllAssemblyRecipes()
    {
        return Resources.LoadAll<AssemblyRecipeData>("Recipes").ToList();
    }

    public List<RefineryRecipeData> GetAllRefineryRecipes()
    {
        return Resources.LoadAll<RefineryRecipeData>("Recipes").ToList();
    }

    public List<IBuildingRecipe> GetRecipesForBuilding(GridObject building)
    {
        if (building is FurnaceBuilding)
        {
            return GetAllSmeltingRecipes().Cast<IBuildingRecipe>().ToList();
        }
        else if (building is AssemblerBuilding)
        {
            return GetAllAssemblyRecipes().Cast<IBuildingRecipe>().ToList();
        }
        else if (building is RefineryBuilding)
        {
            return GetAllRefineryRecipes().Cast<IBuildingRecipe>().ToList();
        }
        return new List<IBuildingRecipe>();
    }

    // --- STORAGE ---

    public void ConfirmNewLimit()
    {
        if (currentStorage == null || limitInputField == null)
        {
            CloseStorageLimitUI();
            return;
        }

        if (int.TryParse(limitInputField.text, out int newLimit))
        {
            currentStorage.SetLimit(Mathf.Max(0, newLimit));
        }
        CloseStorageLimitUI();
    }

    public void UpdateCostDisplay(GridObject selectedBuilding)
    {
        if (selectedBuilding == null)
        {
            costPanel.SetActive(false);
            return;
        }

        if (costPanel != null) costPanel.SetActive(true);
        // Wyczy�� stare ikony
        foreach (Transform child in costContainer) Destroy(child.gameObject);

        foreach (var cost in selectedBuilding.constructionCost)
        {
            GameObject go = Instantiate(costElementPrefab, costContainer);
            var icon = go.GetComponentInChildren<Image>(); // Znajd� obrazek ikony
            var text = go.GetComponentInChildren<TextMeshProUGUI>(); // Znajd� tekst

            int invCount = PlayerInventory.Instance.GetItemCount(cost.resource);

            icon.sprite = cost.resource.icon; // Upewnij si�, �e ResourceData ma pole 'icon'
            text.text = $"{invCount}/{cost.amount}";

            // Kolorowanie tekstu
            text.color = invCount >= cost.amount ? Color.green : Color.red;
        }
    }

    public void UpdateCostDisplayFromCosts(Dictionary<ResourceData, int> costs)
    {
        if (costs == null || costs.Count == 0)
        {
            costPanel.SetActive(false);
            return;
        }

        if (costPanel != null) costPanel.SetActive(true);
        foreach (Transform child in costContainer) Destroy(child.gameObject);

        foreach (var kv in costs)
        {
            ResourceData resource = kv.Key;
            int amount = kv.Value;
            if (resource == null || amount <= 0) continue;

            GameObject go = Instantiate(costElementPrefab, costContainer);
            var icon = go.GetComponentInChildren<Image>();
            var text = go.GetComponentInChildren<TextMeshProUGUI>();

            int invCount = PlayerInventory.Instance.GetItemCount(resource);

            icon.sprite = resource.icon;
            text.text = $"{invCount}/{amount}";
            text.color = invCount >= amount ? Color.green : Color.red;
        }
    }

    public void UpdateMachineSelection(int selectedIndex)
    {
        if (machineButtonFrames == null || machineButtonFrames.Count == 0) return;

        for (int i = 0; i < machineButtonFrames.Count; i++)
        {
            if (i == selectedIndex)
            {
                machineButtonFrames[i].color = activeFrameColor;
                // Opcjonalnie: machineButtonFrames[i].gameObject.SetActive(true);
            }
            else
            {
                machineButtonFrames[i].color = inactiveFrameColor;
                // Opcjonalnie: machineButtonFrames[i].gameObject.SetActive(false);
            }
        }
    }
}