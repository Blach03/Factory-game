
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RecipeTooltipUI : MonoBehaviour
{
    [Header("G³ówne elementy")]
    public TextMeshProUGUI recipeNameText;
    public TextMeshProUGUI assemblyTimeText;
    public TextMeshProUGUI outputAmountText;
    public Image outputIcon;

    [Header("Input Primary")]
    public TextMeshProUGUI primaryInputAmountText;
    public Image primaryInputIcon;

    [Header("Input Secondary")]
    public TextMeshProUGUI secondaryInputAmountText;
    public Image secondaryInputIcon;

    [Header("Input Tertiary (Dla Assemblera)")]
    public GameObject tertiaryInputContainer;
    public TextMeshProUGUI tertiaryInputAmountText;
    public Image tertiaryInputIcon;

    private void Awake()
    {
        gameObject.SetActive(false);
    }


    public void ShowSmeltingRecipe(SmeltingRecipeData recipe)
    {
        recipeNameText.text = recipe.recipeName;
        assemblyTimeText.text = $"Creation Time: {recipe.smeltingTime:F1}s";
        outputAmountText.text = $"x{recipe.outputAmount}";
        outputIcon.sprite = recipe.outputItem.icon;

        primaryInputAmountText.text = $"x{recipe.primaryInputAmount}";
        primaryInputIcon.sprite = recipe.primaryInput.icon;

        UpdateInputSlot(recipe.secondaryInput, recipe.secondaryInputAmount, secondaryInputIcon, secondaryInputAmountText);

        if (tertiaryInputContainer != null)
        {
            tertiaryInputContainer.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    public void ShowAssemblyRecipe(AssemblyRecipeData recipe)
    {
        recipeNameText.text = recipe.recipeName;
        assemblyTimeText.text = $"Creation Time: {recipe.assemblyTime:F1}s";
        outputAmountText.text = $"x{recipe.outputAmount}";
        outputIcon.sprite = recipe.outputItem.icon;

        primaryInputAmountText.text = $"x{recipe.primaryInputAmount}";
        primaryInputIcon.sprite = recipe.primaryInput.icon;

        UpdateInputSlot(recipe.secondaryInput, recipe.secondaryInputAmount, secondaryInputIcon, secondaryInputAmountText);

        bool hasTertiary = recipe.tertiaryInput != null;
        if (tertiaryInputContainer != null)
        {
            tertiaryInputContainer.SetActive(hasTertiary);
            if (hasTertiary)
            {
                UpdateInputSlot(recipe.tertiaryInput, recipe.tertiaryInputAmount, tertiaryInputIcon, tertiaryInputAmountText);
            }
        }

        gameObject.SetActive(true);
    }


    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateInputSlot(ResourceData resource, int amount, Image iconRenderer, TextMeshProUGUI amountText)
    {
        if (resource != null)
        {
            iconRenderer.sprite = resource.icon;
            iconRenderer.color = Color.white;
            amountText.text = $"x{amount}";
        }
        else
        {
            iconRenderer.sprite = null;
            iconRenderer.color = Color.clear;
            amountText.text = "";
        }
    }
}