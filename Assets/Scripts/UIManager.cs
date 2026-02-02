using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI; // <-- TO JEST KLUCZOWE DLA KOMPONENTU IMAGE
using UnityEngine.EventSystems; // DODAJ TÊ DYREKTYWÊ NA GÓRZE PLIKU

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panele UI - Budynki")]
    public RecipeSelectionUI recipeSelectionPanel;
    public FurnaceStatusUI furnaceStatusPanel;
    public AssemblerStatusUI assemblerStatusPanel;

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
        // Jeœli gracz klika/pisze w jakimkolwiek InputField, nie reaguj na skróty klawiszowe
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
            // Tutaj równie¿ warto dodaæ warunek, aby nie otwieraæ drzewka, 
            // gdy inne panele s¹ ju¿ otwarte (podobnie jak robisz to z inventory)
            if (!isTechTreeOpen && IsAnyPanelOpen()) return;

            ToggleTechnologyTree();
        }
    }

    /// <summary>
    /// Logika obs³ugi klawisza ESC: najpierw zamyka aktywne okna, 
    /// a jeœli nic nie jest otwarte - otwiera/zamyka Menu Pauzy.
    /// </summary>
    /// 
    public void ToggleTechnologyTree()
    {

        if (!isTechTreeOpen)
        {
            // Jeœli otwieramy drzewko, zamknijmy inne panele (np. piec, assembler)
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
        // 1. Jeœli otwarte jest Menu Pauzy - zamknij je
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            ClosePauseMenu();
            return;
        }

        // 2. NOWOŒÆ: Najpierw sprawdŸ, czy otwarte jest okno SZCZEGÓ£ÓW technologii
        // U¿ywamy Instance, bo TechDetailsUI to Singleton
        if (TechDetailsUI.Instance != null && TechDetailsUI.Instance.panel.activeSelf)
        {
            TechDetailsUI.Instance.Close();
            return; // Przerywamy, dziêki czemu drzewko (isTechTreeOpen) pozostanie otwarte
        }

        // 3. Jeœli otwarte jest g³ówne drzewko technologii - zamknij je
        if (isTechTreeOpen)
        {
            ToggleTechnologyTree();
            return;
        }

        // 4. Jeœli otwarte s¹ inne panele gry (ekwipunek, budynki) - zamknij je
        if (IsAnyPanelOpen())
        {
            CloseAllUI();
        }
        // 5. Jeœli nic nie jest otwarte - otwórz Menu Pauzy
        else
        {
            OpenPauseMenu();
        }
    }

    private void HandleInventoryToggle()
    {
        // Nie pozwól otworzyæ ekwipunku, gdy Menu Pauzy jest aktywne
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

    // --- ZARZ¥DZANIE MENU PAUZY ---

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

    // --- ZARZ¥DZANIE PANELAMI ---

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
               (assemblerStatusPanel != null && assemblerStatusPanel.gameObject.activeSelf) ||
               (storageLimitPanel != null && storageLimitPanel.activeSelf) ||
               inventoryPanel.activeSelf ||
               isTechTreeOpen;
    }

    public void CloseAllUI()
    {
        // Zamyka budynki
        if (recipeSelectionPanel != null) recipeSelectionPanel.gameObject.SetActive(false);
        if (furnaceStatusPanel != null) furnaceStatusPanel.gameObject.SetActive(false);
        if (assemblerStatusPanel != null) assemblerStatusPanel.gameObject.SetActive(false);

        // Zamyka storage i inventory
        CloseStorageLimitUI();
        CloseInventory();

        // Upewniamy siê, ¿e czas p³ynie (na wypadek gdyby zamkniêto coœ si³owo)
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

    // --- OTWIERANIE KONKRETNYCH STATUSÓW ---

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

    public void OpenAssemblerStatus(AssemblerBuilding assembler)
    {
        CloseAllUI();
        if (assemblerStatusPanel != null)
        {
            assemblerStatusPanel.ShowStatus(assembler);
        }
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
        // Wyczyœæ stare ikony
        foreach (Transform child in costContainer) Destroy(child.gameObject);

        foreach (var cost in selectedBuilding.constructionCost)
        {
            GameObject go = Instantiate(costElementPrefab, costContainer);
            var icon = go.GetComponentInChildren<Image>(); // ZnajdŸ obrazek ikony
            var text = go.GetComponentInChildren<TextMeshProUGUI>(); // ZnajdŸ tekst

            int invCount = PlayerInventory.Instance.GetItemCount(cost.resource);

            icon.sprite = cost.resource.icon; // Upewnij siê, ¿e ResourceData ma pole 'icon'
            text.text = $"{invCount}/{cost.amount}";

            // Kolorowanie tekstu
            text.color = invCount >= cost.amount ? Color.green : Color.red;
        }
    }
}