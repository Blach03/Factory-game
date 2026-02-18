using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class ProductionStatusUI : MonoBehaviour
{
    [Header("Wejście Surowca A (Primary)")]
    public Image primaryInputIcon;
    public TMP_Text primaryInputCountText;

    [Header("Wejście Surowca B (Secondary)")]
    public Image secondaryInputIcon;
    public TMP_Text secondaryInputCountText;

    [Header("Wejście Surowca C (Tertiary)")]
    public Image tertiaryInputIcon;
    public TMP_Text tertiaryInputCountText;

    [Header("Wyjście Produktu")]
    public Image outputItemIcon;
    public TMP_Text outputItemCountText;

    [Header("Status i Kontrola")]
    public Slider productionProgressBar; // Zmieniona nazwa dla uniwersalności
    public Button changeRecipeButton;

    private IProductionBuilding currentBuilding; // Teraz GridObject, by obsłużyć oba typy
    private readonly Color RequiredColor = Color.white;
    private readonly Color NotEnoughColor = Color.red;

    public void ShowStatus(IProductionBuilding building) // Parametr jako interfejs
    {
        currentBuilding = building;

        changeRecipeButton.onClick.RemoveAllListeners();
        changeRecipeButton.onClick.AddListener(OnSwitchRecipeClicked);

        IBuildingRecipe recipe = building.GetCurrentRecipe();
        SetupRecipeData(recipe);

        gameObject.SetActive(true);
        UpdateUI();
    }

    private void SetupRecipeData(IBuildingRecipe recipe)
    {
        if (recipe == null) return;

        SetupInputIcon(primaryInputIcon, recipe.primaryInput);
        
        // Secondary
        bool hasSecondary = recipe.secondaryInput != null;
        secondaryInputIcon.gameObject.SetActive(hasSecondary);
        secondaryInputCountText.gameObject.SetActive(hasSecondary);
        if(hasSecondary) SetupInputIcon(secondaryInputIcon, recipe.secondaryInput);

        // Tertiary (Dostępne tylko jeśli to recepta assemblera)
        bool hasTertiary = false;
        if (recipe is AssemblyRecipeData assemblyRecipe && assemblyRecipe.tertiaryInput != null)
        {
            SetupInputIcon(tertiaryInputIcon, assemblyRecipe.tertiaryInput);
            hasTertiary = true;
        }
        
        tertiaryInputIcon.gameObject.SetActive(hasTertiary);
        tertiaryInputCountText.gameObject.SetActive(hasTertiary);

        // Output
        if (recipe.outputItem != null)
        {
            outputItemIcon.sprite = recipe.outputItem.icon;
            outputItemIcon.color = Color.white;
        }

        // Ustawienie max postępu (używamy rzutowania, by wyciągnąć czas)
        if (recipe is AssemblyRecipeData ard) productionProgressBar.maxValue = ard.assemblyTime;
        else if (recipe is RefineryRecipeData rrd) productionProgressBar.maxValue = rrd.processTime;
        else if (recipe is SmeltingRecipeData srd) productionProgressBar.maxValue = srd.smeltingTime;
    }

    private void UpdateUI()
    {
        if (currentBuilding == null) return;
        IBuildingRecipe recipe = currentBuilding.GetCurrentRecipe();
        if (recipe == null) { gameObject.SetActive(false); return; }

        // Wszystkie dane pobieramy teraz przez interfejs!
        int curA = currentBuilding.GetInputCount(0);
        int curB = currentBuilding.GetInputCount(1);
        int curC = currentBuilding.GetInputCount(2);
        int curOut = currentBuilding.GetCurrentOutputAmount();
        float timer = currentBuilding.GetProgressTimer();

        UpdateInputSlot(curA, recipe.primaryInputAmount, currentBuilding.inputCapacity, primaryInputCountText);

        if (recipe.secondaryInput != null)
            UpdateInputSlot(curB, recipe.secondaryInputAmount, currentBuilding.inputCapacity, secondaryInputCountText);

        if (recipe is AssemblyRecipeData ard && ard.tertiaryInput != null)
            UpdateInputSlot(curC, ard.tertiaryInputAmount, currentBuilding.inputCapacity, tertiaryInputCountText);

        outputItemCountText.text = $"{curOut} / {currentBuilding.outputCapacity}";
        outputItemCountText.color = curOut >= currentBuilding.outputCapacity ? NotEnoughColor : RequiredColor;

        productionProgressBar.value = productionProgressBar.maxValue - timer;
    }

    // ... Reszta metod (SetupInputIcon, UpdateInputSlot, OnSwitchRecipeClicked bez zmian) ...
    private void OnSwitchRecipeClicked()
    {
        // Rzutujemy z powrotem na GridObject tylko dla UIManager, bo on wymaga GridObject do szukania recept
        UIManager.Instance.OpenRecipeSelection((GridObject)currentBuilding, UIManager.Instance.GetRecipesForBuilding((GridObject)currentBuilding));
    }

    private IBuildingRecipe GetRecipeFromBuilding(GridObject building)
    {
        if (building is AssemblerBuilding assembler) return assembler.GetCurrentRecipe();
        if (building is RefineryBuilding refinery) return refinery.currentRecipe;
        return null;
    }

    private void SetupInputIcon(Image icon, ResourceData resource)
    {
        if (resource != null && resource.icon != null)
        {
            icon.sprite = resource.icon;
            icon.color = Color.white;
        }
        else
        {
            icon.sprite = null;
        }
    }

    void Update()
    {
        if (gameObject.activeSelf && currentBuilding != null)
        {
            UpdateUI();
        }
    }


    private void UpdateInputSlot(int currentAmount, int requiredAmount, int capacity, TMP_Text textComponent)
    {
        textComponent.text = $"{currentAmount} / {requiredAmount}";
        textComponent.color = (currentAmount < requiredAmount || currentAmount >= capacity) ? NotEnoughColor : RequiredColor;
    }

}