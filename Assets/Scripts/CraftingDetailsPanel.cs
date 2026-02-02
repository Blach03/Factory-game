using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CraftingDetailsPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI itemNameText;
    public Image itemIcon;
    public GameObject recipeSection; // Kontener na sk³adniki
    public Button craftButton;
    public Button craftX10Button;
    public TextMeshProUGUI outputCountText; // Przeci¹gnij nowe pole w Inspektorze

    [Header("Progress UI")]
    public Slider progressBar;
    public TextMeshProUGUI queueText;

    [Header("Recipe Visualization")]
    public GameObject costElementPrefab; // Twój prefab z ikon¹ i tekstem
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

        itemIcon.gameObject.SetActive(true); // Przywracamy widocznoœæ ikony

        selectedResource = resource;
        itemNameText.text = resource.resourceName;
        itemIcon.sprite = resource.icon;

        // Szukamy receptury dla tego przedmiotu
        selectedRecipe = FindRecipeFor(resource);

        if (selectedRecipe != null)
        {
            recipeSection.SetActive(true);
            // SprawdŸ research
            if (outputCountText != null)
            {
                outputCountText.text = $"Output Count: {selectedRecipe.outputAmount}";
            }
            UpdateRecipeUI(selectedRecipe);


            bool isUnlocked = true;

            // Próba znalezienia managera, jeœli instancja jest nullem
            if (TechTreeManager.Instance == null)
            {
                // Szukamy w ca³ej scenie, nawet obiektów nieaktywnych
                TechTreeManager foundManager = Resources.FindObjectsOfTypeAll<TechTreeManager>().Length > 0
                    ? Resources.FindObjectsOfTypeAll<TechTreeManager>()[0]
                    : null;

                if (foundManager != null)
                {
                    // Jeœli znaleŸliœmy, wymuszamy przypisanie instancji (Singleton)
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
                    Debug.LogWarning($"[CraftingPanel] Nie znaleziono TechTreeManager (nawet wy³¹czonego)! Blokujê recepturê: {selectedRecipe.recipeName}");
                    isUnlocked = false;
                }
            }

            craftButton.interactable = isUnlocked;
            craftX10Button.interactable = isUnlocked;
            // Tutaj zaktualizuj ikony sk³adników (analogicznie do TooltipUI)
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
        if (selectedRecipe != null) HandCraftingManager.Instance.AddToQueue(selectedRecipe, amount);
    }

    private void Update()
    {
        // Sprawdzamy czy Manager istnieje
        if (HandCraftingManager.Instance == null) return;

        // Sprawdzamy czy UI jest przypisane zanim go u¿yjemy
        if (progressBar != null)
        {
            progressBar.value = HandCraftingManager.Instance.GetProgress();
        }

        if (queueText != null)
        {
            queueText.text = $"In Queue: {HandCraftingManager.Instance.GetQueueCount()}";
        }
    }

    private void UpdateRecipeUI(IBuildingRecipe recipe)
    {
        // 1. Czyœcimy stare sk³adniki
        foreach (Transform child in ingredientsParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Wyœwietlamy czas (Logika zale¿na od typu receptury)
        float time = 0;
        if (recipe is SmeltingRecipeData s) time = s.smeltingTime;
        else if (recipe is AssemblyRecipeData a) time = a.assemblyTime;
        craftTimeText.text = $"Time: {time:F1}s";

        // 3. Tworzymy elementy dla sk³adników
        CreateCostElement(recipe.primaryInput, recipe.primaryInputAmount);
        CreateCostElement(recipe.secondaryInput, recipe.secondaryInputAmount);

        // Sprawdzamy trzeci sk³adnik tylko dla Assembly
        if (recipe is AssemblyRecipeData assembly && assembly.tertiaryInput != null)
        {
            CreateCostElement(assembly.tertiaryInput, assembly.tertiaryInputAmount);
        }
    }

    private void CreateCostElement(ResourceData resource, int amount)
    {
        if (resource == null || amount <= 0) return;

        GameObject element = Instantiate(costElementPrefab, ingredientsParent);

        // Szukamy komponentów wewn¹trz Twojego prefaba
        Image icon = element.GetComponentInChildren<Image>();
        TextMeshProUGUI text = element.GetComponentInChildren<TextMeshProUGUI>();

        if (icon != null) icon.sprite = resource.icon;
        if (text != null)
        {
            int playerHas = PlayerInventory.Instance.GetItemCount(resource);

            // Opcjonalnie: kolorowanie na czerwono, gdy brakuje surowców
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

        // Ukrywamy ikonê i sekcjê receptury
        itemIcon.gameObject.SetActive(false);
        recipeSection.SetActive(false);

        // Blokujemy przyciski
        craftButton.interactable = false;
        craftX10Button.interactable = false;

        // Czyœcimy listê sk³adników (opcjonalnie)
        foreach (Transform child in ingredientsParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void RefreshCurrentUI()
    {
        if (selectedResource != null)
        {
            // Ponownie wywo³ujemy DisplayItem, aby odœwie¿yæ liczby sk³adników (np. 5/10)
            DisplayItem(selectedResource);
        }
    }

    private void UpdateButtons(IBuildingRecipe recipe)
    {
        if (recipe == null) return;

        // Sprawdzamy surowce dla x1 i x10
        bool canAfford1 = CanAffordAmount(recipe, 1);
        bool canAfford10 = CanAffordAmount(recipe, 10);

        // Sprawdzamy czy technologia jest odblokowana (u¿ywamy Twojej zmiennej isUnlocked)
        bool isUnlocked = true;
        if (!string.IsNullOrEmpty(recipe.techRequirementId) && TechTreeManager.Instance != null)
            isUnlocked = TechTreeManager.Instance.IsResearched(recipe.techRequirementId);

        // Ustawiamy interaktywnoœæ
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