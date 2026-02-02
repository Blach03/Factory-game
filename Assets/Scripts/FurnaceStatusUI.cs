using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class FurnaceStatusUI : MonoBehaviour
{

    [Header("Wejœcie Surowca A (Primary)")]
    public Image primaryInputIcon;
    public TMP_Text primaryInputCountText; 

    [Header("Wejœcie Surowca B (Secondary)")]
    public Image secondaryInputIcon;
    public TMP_Text secondaryInputCountText;

    [Header("Wyjœcie Produktu")]
    public Image outputItemIcon;
    public TMP_Text outputItemCountText;

    [Header("Status i Kontrola")]
    public Slider smeltingProgressBar;
    public Button changeRecipeButton;

    private FurnaceBuilding currentFurnace;

    private readonly Color RequiredColor = Color.white;
    private readonly Color NotEnoughColor = Color.red;

    public void ShowStatus(FurnaceBuilding furnace)
    {
        currentFurnace = furnace;

        changeRecipeButton.onClick.RemoveAllListeners();
        changeRecipeButton.onClick.AddListener(OnSwitchRecipeClicked);

        SetupRecipeData(furnace.GetCurrentRecipe());

        gameObject.SetActive(true);
        UpdateUI();
    }

    private void SetupRecipeData(SmeltingRecipeData recipe)
    {
        if (recipe == null) return;

        if (recipe.primaryInput != null && recipe.primaryInput.icon != null)
        {
            primaryInputIcon.sprite = recipe.primaryInput.icon;
            primaryInputIcon.color = Color.white;
        }

        if (recipe.secondaryInput != null && recipe.secondaryInput.icon != null)
        {
            secondaryInputIcon.sprite = recipe.secondaryInput.icon;
            secondaryInputIcon.color = Color.white;
        }

        if (recipe.outputItem != null && recipe.outputItem.icon != null)
        {
            outputItemIcon.sprite = recipe.outputItem.icon;
            outputItemIcon.color = Color.white;
        }

        smeltingProgressBar.maxValue = recipe.smeltingTime;
    }

    void Update()
    {
        if (gameObject.activeSelf && currentFurnace != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        SmeltingRecipeData recipe = currentFurnace.GetCurrentRecipe();
        if (recipe == null)
        {
            UIManager.Instance.CloseAllUI();
            return;
        }

        int currentPrimary = currentFurnace.GetPrimaryInputCount();
        int requiredPrimary = recipe.primaryInputAmount;
        int capacityPrimary = currentFurnace.InputOreCapacity;

        primaryInputCountText.text = $"{currentPrimary} / {requiredPrimary}";
        if (currentPrimary < requiredPrimary || currentPrimary == capacityPrimary)
        {
            primaryInputCountText.color = NotEnoughColor;
        }
        else
        {
            primaryInputCountText.color = RequiredColor;
        }


        int currentSecondary = currentFurnace.GetSecondaryInputCount();
        int requiredSecondary = recipe.secondaryInputAmount;
        int capacitySecondary = currentFurnace.InputCoalCapacity;

        secondaryInputCountText.text = $"{currentSecondary} / {requiredSecondary}";
        if (currentSecondary < requiredSecondary || currentSecondary == capacitySecondary)
        {
            secondaryInputCountText.color = NotEnoughColor;
        }
        else
        {
            secondaryInputCountText.color = RequiredColor;
        }


        int currentOutput = currentFurnace.GetCurrentOutputAmount();
        int producedOutput = recipe.outputAmount;
        int capacityOutput = currentFurnace.OutputBarCapacity;

        outputItemCountText.text = $"{currentOutput} / {producedOutput}";
        if (currentOutput >= capacityOutput)
        {
            outputItemCountText.color = NotEnoughColor;
        }
        else
        {
            outputItemCountText.color = RequiredColor;
        }

        float timerValue = currentFurnace.GetSmeltingTimer();
        float totalTime = recipe.smeltingTime;


        smeltingProgressBar.value = totalTime - timerValue;
    }

    private void OnSwitchRecipeClicked()
    {

        List<IBuildingRecipe> availableRecipes = UIManager.Instance.GetRecipesForBuilding(currentFurnace);

        UIManager.Instance.OpenRecipeSelection(currentFurnace, availableRecipes);
    }
}