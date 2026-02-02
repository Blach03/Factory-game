
using UnityEngine;
using UnityEngine.EventSystems;

public class RecipeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private IBuildingRecipe recipe;
    private RecipeTooltipUI tooltip;

    public void Initialize(IBuildingRecipe recipeData, RecipeTooltipUI tooltipUI)
    {
        recipe = recipeData;
        tooltip = tooltipUI;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip == null || recipe == null) return;

        if (recipe is SmeltingRecipeData smeltingRecipe)
        {
            tooltip.ShowSmeltingRecipe(smeltingRecipe);
        }
        else if (recipe is AssemblyRecipeData assemblyRecipe)
        {
            tooltip.ShowAssemblyRecipe(assemblyRecipe);
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
}