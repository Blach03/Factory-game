using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CraftingDetailsPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI itemNameText;
    public Image itemIcon;
    public GameObject recipeSection; // Kontener na sk�adniki
    public Button craftButton;
    public Button craftX10Button;
    public TextMeshProUGUI outputCountText; // Przeci�gnij nowe pole w Inspektorze

    [Header("Progress UI")]
    public Slider progressBar;
    public TextMeshProUGUI queueText;

    [Header("Recipe Visualization")]
    public GameObject costElementPrefab; // Tw�j prefab z ikon� i tekstem
    public Transform ingredientsParent;  // Obiekt z Vertical/Horizontal Layout Group
    public TextMeshProUGUI craftTimeText;

    [Header("Button Settings")]
    public Color normalColor = Color.white;
    public Color errorColor = Color.red;

    private IBuildingRecipe selectedRecipe;
    private ResourceData selectedResource;

    public void DisplayItem(ResourceData resource)
    {

        if (resource == null) return;

        itemIcon.gameObject.SetActive(true); // Przywracamy widoczno�� ikony

        selectedResource = resource;
        itemNameText.text = resource.resourceName;
        itemIcon.sprite = resource.icon;

        // Szukamy receptury dla tego przedmiotu
        selectedRecipe = FindRecipeFor(resource);

        if (selectedRecipe != null)
        {
            recipeSection.SetActive(true);
            // Sprawd� research
            if (outputCountText != null)
            {
                outputCountText.text = $"Output Count: {selectedRecipe.outputAmount}";
            }
            UpdateRecipeUI(selectedRecipe);


            bool isUnlocked = true;

            // Pr�ba znalezienia managera, je�li instancja jest nullem
            if (TechTreeManager.Instance == null)
            {
                // Szukamy w ca�ej scenie, nawet obiekt�w nieaktywnych
                TechTreeManager foundManager = Resources.FindObjectsOfTypeAll<TechTreeManager>().Length > 0
                    ? Resources.FindObjectsOfTypeAll<TechTreeManager>()[0]
                    : null;

                if (foundManager != null)
                {
                    // Je�li znale�li�my, wymuszamy przypisanie instancji (Singleton)
                    TechTreeManager.Instance = foundManager;
                }
            }

            if (!string.IsNullOrEmpty(selectedRecipe.techRequirementId))
            {
                if (TechTreeManager.Instance != null)
                {
                    isUnlocked = TechTreeManager.Instance.IsResearched(selectedRecipe.techRequirementId);
                }
                else
                {
                    Debug.LogWarning($"[CraftingPanel] Nie znaleziono TechTreeManager (nawet wy��czonego)! Blokuj� receptur�: {selectedRecipe.recipeName}");
                    isUnlocked = false;
                }
            }

            craftButton.interactable = isUnlocked;
            craftX10Button.interactable = isUnlocked;
            // Tutaj zaktualizuj ikony sk�adnik�w (analogicznie do TooltipUI)
        }
        else
        {
            recipeSection.SetActive(false);
            craftButton.interactable = false;
            craftX10Button.interactable = false;
        }
        UpdateButtons(selectedRecipe);
    }

    private IBuildingRecipe FindRecipeFor(ResourceData resource)
    {
        // Przeszukaj wszystkie receptury w Resources
        var smelting = Resources.LoadAll<SmeltingRecipeData>("Recipes");
        foreach (var r in smelting) if (r.outputItem == resource) return r;

        var assembly = Resources.LoadAll<AssemblyRecipeData>("Recipes");
        foreach (var r in assembly) if (r.outputItem == resource) return r;

        return null;
    }

    public void OnCraftClicked(int amount)
    {
        if (selectedRecipe != null)
        {
            // Tutorial step: verify exact action "Iron Gear + Craft x10".
            if (amount == 10 && selectedRecipe.outputItem != null && selectedRecipe.outputItem.resourceName == "Iron Gear")
            {
                TutorialItemTracker.OnPressedCraftX10IronGear();
            }

            HandCraftingManager.Instance.AddToQueue(selectedRecipe, amount);
        }
    }

    private void OnEnable()
    {
        // Gdy panel zostaje ponownie włączony, odśwież dane
        if (selectedResource != null)
        {
            DisplayItem(selectedResource);
        }
    }

    private void OnDisable()
    {
        // Panel zostaje zamknięty - crafting powinien nadal postępować w tle
        // (HandleCraftingManager będzie go aktualizować przez statyczne zmienne)
    }

    private void Update()
    {
        // Sprawdzamy czy Manager istnieje
        if (HandCraftingManager.Instance == null) return;

        // Sprawdzamy czy UI jest przypisane zanim go użyjemy
        if (progressBar != null)
        {
            progressBar.value = HandCraftingManager.Instance.GetProgress();
        }

        if (queueText != null)
        {
            // Pobierz ilość elementów w kolejce
            int queueCount = HandCraftingManager.Instance.GetQueueCount();
            queueText.text = $"In Queue: {queueCount}";
        }
    }

    private void UpdateRecipeUI(IBuildingRecipe recipe)
    {
        // 1. Czy�cimy stare sk�adniki
        foreach (Transform child in ingredientsParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Wy�wietlamy czas (Logika zale�na od typu receptury)
        float time = 0;
        if (recipe is SmeltingRecipeData s) time = s.smeltingTime;
        else if (recipe is AssemblyRecipeData a) time = a.assemblyTime;
        craftTimeText.text = $"Time: {time:F1}s";

        // 3. Tworzymy elementy dla sk�adnik�w
        CreateCostElement(recipe.primaryInput, recipe.primaryInputAmount);
        CreateCostElement(recipe.secondaryInput, recipe.secondaryInputAmount);

        // Sprawdzamy trzeci sk�adnik tylko dla Assembly
        if (recipe is AssemblyRecipeData assembly && assembly.tertiaryInput != null)
        {
            CreateCostElement(assembly.tertiaryInput, assembly.tertiaryInputAmount);
        }
    }

    private void CreateCostElement(ResourceData resource, int amount)
    {
        if (resource == null || amount <= 0) return;

        GameObject element = Instantiate(costElementPrefab, ingredientsParent);

        // Szukamy komponent�w wewn�trz Twojego prefaba
        Image icon = element.GetComponentInChildren<Image>();
        TextMeshProUGUI text = element.GetComponentInChildren<TextMeshProUGUI>();

        if (icon != null) icon.sprite = resource.icon;
        if (text != null)
        {
            int playerHas = PlayerInventory.Instance.GetItemCount(resource);

            // Opcjonalnie: kolorowanie na czerwono, gdy brakuje surowc�w
            string colorTag = playerHas >= amount ? "<color=white>" : "<color=red>";
            text.text = $"{colorTag}{playerHas}/{amount}</color>";
        }
    }

    public void ClearDetails()
    {
        selectedResource = null;
        selectedRecipe = null;

        itemNameText.text = "Select item to craft";
        craftTimeText.text = "";
        if (outputCountText != null) outputCountText.text = "";

        // Ukrywamy ikon� i sekcj� receptury
        itemIcon.gameObject.SetActive(false);
        recipeSection.SetActive(false);

        // Blokujemy przyciski
        craftButton.interactable = false;
        craftX10Button.interactable = false;

        // Czy�cimy list� sk�adnik�w (opcjonalnie)
        foreach (Transform child in ingredientsParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void RefreshCurrentUI()
    {
        if (selectedResource != null)
        {
            // Ponownie wywo�ujemy DisplayItem, aby od�wie�y� liczby sk�adnik�w (np. 5/10)
            DisplayItem(selectedResource);
        }
    }

    private void UpdateButtons(IBuildingRecipe recipe)
    {
        if (recipe == null) return;

        // Sprawdzamy surowce dla x1 i x10
        bool canAfford1 = CanAffordAmount(recipe, 1);
        bool canAfford10 = CanAffordAmount(recipe, 10);

        // Sprawdzamy czy technologia jest odblokowana (u�ywamy Twojej zmiennej isUnlocked)
        bool isUnlocked = true;
        if (!string.IsNullOrEmpty(recipe.techRequirementId) && TechTreeManager.Instance != null)
            isUnlocked = TechTreeManager.Instance.IsResearched(recipe.techRequirementId);

        // Ustawiamy interaktywno��
        craftButton.interactable = canAfford1 && isUnlocked;
        craftX10Button.interactable = canAfford10 && isUnlocked;

        // Ustawiamy kolory (zmieniamy kolor tekstu lub obrazka przycisku)
        craftButton.GetComponentInChildren<TextMeshProUGUI>().color = canAfford1 ? normalColor : errorColor;
        craftX10Button.GetComponentInChildren<TextMeshProUGUI>().color = canAfford10 ? normalColor : errorColor;
    }

    private bool CanAffordAmount(IBuildingRecipe r, int amount)
    {
        var inv = PlayerInventory.Instance;
        bool p = inv.GetItemCount(r.primaryInput) >= (r.primaryInputAmount * amount);
        bool s = r.secondaryInput == null || inv.GetItemCount(r.secondaryInput) >= (r.secondaryInputAmount * amount);

        bool t = true;
        if (r is AssemblyRecipeData assembly && assembly.tertiaryInput != null)
            t = inv.GetItemCount(assembly.tertiaryInput) >= (assembly.tertiaryInputAmount * amount);

        return p && s && t;
    }
}