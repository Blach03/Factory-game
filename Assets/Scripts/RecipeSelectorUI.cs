// RecipeSelectionUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class RecipeSelectionUI : MonoBehaviour
{

    [Tooltip("Kontener, do kt�rego b�d� dodawane przyciski receptur.")]
    public Transform recipeGridContainer;

    [Tooltip("Prefab przycisku, kt�ry ma by� instancjonowany dla ka�dej receptury.")]
    public GameObject recipeButtonPrefab;

    public RecipeTooltipUI recipeTooltip;

    private GridObject currentBuilding;


    public void ShowRecipes(GridObject building, List<IBuildingRecipe> recipes)
    {
        currentBuilding = building;

        foreach (Transform child in recipeGridContainer)
        {
            Destroy(child.gameObject);
        }

        var tree = TechTreeManager.Instance;
        // Je�li z jakiego� powodu nie ma managera, poka�emy tylko receptury bez wymaga�

        List<IBuildingRecipe> unlockedRecipes = recipes.Where(r =>
        {
            // Je�li ID jest puste, receptura jest darmowa (dost�pna od pocz�tku)
            if (string.IsNullOrEmpty(r.techRequirementId)) return true;

            // W przeciwnym razie sprawd�, czy technologia jest zbadana
            return tree != null && tree.IsResearched(r.techRequirementId);
        }).ToList();

        // 3. Sprawdzenie czy mamy co wy�wietli� po filtracji
        if (unlockedRecipes.Count == 0)
        {
            Debug.LogWarning($"Brak ODBLOKOWANYCH receptur dla {building.GetType().Name}.");
            // Opcjonalnie: wy�wietl komunikat w UI: "Wymagane dalsze badania"
            return;
        }


        if (recipeTooltip != null)
        {
            recipeTooltip.Hide();
        }

        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogWarning($"Nie znaleziono dost�pnych receptur dla {building.GetType().Name}.");
            return;
        }

        foreach (var recipe in unlockedRecipes)
        {
            ResourceData outputResource = GetOutputResourceFromRecipe(recipe);
            if (outputResource == null || outputResource.icon == null) continue;

            GameObject buttonGO = Instantiate(recipeButtonPrefab, recipeGridContainer);
            Button button = buttonGO.GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(() => OnRecipeSelected(recipe));

                UpdateRecipeButtonIcon(buttonGO, outputResource);

                if (recipeTooltip != null)
                {
                    RecipeButtonHover hoverHandler = buttonGO.AddComponent<RecipeButtonHover>();
                    hoverHandler.Initialize(recipe, recipeTooltip);
                }
            }
        }

        gameObject.SetActive(true);
    }

    private ResourceData GetOutputResourceFromRecipe(IBuildingRecipe recipe)
    {
        if (recipe is SmeltingRecipeData smeltingRecipe) return smeltingRecipe.outputItem;
        if (recipe is AssemblyRecipeData assemblyRecipe) return assemblyRecipe.outputItem;
        
        if (recipe is RefineryRecipeData refinery) 
        {
            // Sprawdź w swoim skrypcie RefineryRecipeData czy to pole nazywa się outputResource czy outputItem
            return refinery.outputResource; 
        }
        return null;
    }

    private void UpdateRecipeButtonIcon(GameObject buttonGO, ResourceData outputResource)
    {
        Image buttonImage = buttonGO.GetComponent<Image>();

        if (buttonImage != null && outputResource.icon != null)
        {
            buttonImage.sprite = outputResource.icon;
            buttonImage.color = Color.white;
        }
    }


    private void OnRecipeSelected(IBuildingRecipe selectedRecipe)
    {
        UIManager.Instance.CloseAllUI();

        if (currentBuilding is FurnaceBuilding furnace && selectedRecipe is SmeltingRecipeData smeltingRecipe)
        {
            furnace.SetRecipe(smeltingRecipe);
            UIManager.Instance.OpenFurnaceStatus(furnace);
        }
        else if (currentBuilding is IProductionBuilding productionBuilding)
        {
            // To obsłuży zarówno AssemblerBuilding jak i RefineryBuilding
            if (currentBuilding is AssemblerBuilding assembler && selectedRecipe is AssemblyRecipeData assemblyRecipe)
            {
                assembler.SetRecipe(assemblyRecipe);
            }
            else if (currentBuilding is RefineryBuilding refinery && selectedRecipe is RefineryRecipeData refineryRecipe)
            {
                refinery.SetRecipe(refineryRecipe);
            }

            // Wywołujemy nową, wspólną metodę UI
            UIManager.Instance.OpenStatusWindow(productionBuilding);
        }

        if (recipeTooltip != null) recipeTooltip.Hide();
    }

    private void OnDisable()
    {
        if (recipeTooltip != null)
        {
            recipeTooltip.Hide();
        }
    }
}