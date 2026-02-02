using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class AssemblerStatusUI : MonoBehaviour
{
    [Header("Wejœcie Surowca A (Primary)")]
    public Image primaryInputIcon;
    public TMP_Text primaryInputCountText;

    [Header("Wejœcie Surowca B (Secondary)")]
    public Image secondaryInputIcon;
    public TMP_Text secondaryInputCountText;

    [Header("Wejœcie Surowca C (Tertiary)")]
    public Image tertiaryInputIcon;
    public TMP_Text tertiaryInputCountText;

    [Header("Wyjœcie Produktu")]
    public Image outputItemIcon;
    public TMP_Text outputItemCountText;

    [Header("Status i Kontrola")]
    public Slider assemblyProgressBar;
    public Button changeRecipeButton;

    private AssemblerBuilding currentAssembler;
    private readonly Color RequiredColor = Color.white;
    private readonly Color NotEnoughColor = Color.red;

    public void ShowStatus(AssemblerBuilding assembler)
    {
        currentAssembler = assembler;

        changeRecipeButton.onClick.RemoveAllListeners();
        changeRecipeButton.onClick.AddListener(OnSwitchRecipeClicked);

        SetupRecipeData(assembler.GetCurrentRecipe());

        gameObject.SetActive(true);
        UpdateUI();
    }

    private void SetupRecipeData(AssemblyRecipeData recipe)
    {
        if (recipe == null) return;

        SetupInputIcon(primaryInputIcon, recipe.primaryInput);

        SetupInputIcon(secondaryInputIcon, recipe.secondaryInput);
        secondaryInputIcon.gameObject.SetActive(recipe.secondaryInput != null);
        secondaryInputCountText.gameObject.SetActive(recipe.secondaryInput != null);

        SetupInputIcon(tertiaryInputIcon, recipe.tertiaryInput);
        tertiaryInputIcon.gameObject.SetActive(recipe.tertiaryInput != null);
        tertiaryInputCountText.gameObject.SetActive(recipe.tertiaryInput != null);

        if (recipe.outputItem != null && recipe.outputItem.icon != null)
        {
            outputItemIcon.sprite = recipe.outputItem.icon;
            outputItemIcon.color = Color.white;
        }

        assemblyProgressBar.maxValue = recipe.assemblyTime;
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
        if (gameObject.activeSelf && currentAssembler != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        AssemblyRecipeData recipe = currentAssembler.GetCurrentRecipe();
        if (recipe == null)
        {
            UIManager.Instance.CloseAllUI();
            return;
        }

        int capacity = currentAssembler.inputCapacity;

        UpdateInputSlot(
            currentAssembler.GetPrimaryInputCount(),
            recipe.primaryInputAmount,
            capacity,
            primaryInputCountText);

        if (recipe.secondaryInput != null)
        {
            UpdateInputSlot(
                currentAssembler.GetSecondaryInputCount(),
                recipe.secondaryInputAmount,
                capacity,
                secondaryInputCountText);
        }

        if (recipe.tertiaryInput != null)
        {
            UpdateInputSlot(
                currentAssembler.GetTertiaryInputCount(),
                recipe.tertiaryInputAmount,
                capacity,
                tertiaryInputCountText);
        }

        int currentOutput = currentAssembler.GetCurrentOutputAmount();
        int capacityOutput = currentAssembler.outputCapacity;

        outputItemCountText.text = $"{currentOutput} / {recipe.outputAmount}";
        if (currentOutput >= capacityOutput)
        {
            outputItemCountText.color = NotEnoughColor;
        }
        else
        {
            outputItemCountText.color = RequiredColor;
        }

        float timerValue = currentAssembler.GetAssemblyTimer();
        float totalTime = recipe.assemblyTime;
        assemblyProgressBar.value = totalTime - timerValue;
    }

    private void UpdateInputSlot(int currentAmount, int requiredAmount, int capacity, TMP_Text textComponent)
    {
        textComponent.text = $"{currentAmount} / {requiredAmount}";

        if (currentAmount < requiredAmount || currentAmount == capacity)
        {
            textComponent.color = NotEnoughColor;
        }
        else
        {
            textComponent.color = RequiredColor;
        }
    }

    private void OnSwitchRecipeClicked()
    {
        List<IBuildingRecipe> availableRecipes = UIManager.Instance.GetRecipesForBuilding(currentAssembler);

        UIManager.Instance.OpenRecipeSelection(currentAssembler, availableRecipes);
    }
}