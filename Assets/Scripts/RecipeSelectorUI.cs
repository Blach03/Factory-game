// RecipeSelectionUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class RecipeSelectionUI : MonoBehaviour
{

    [Tooltip("Kontener, do którego bêd¹ dodawane przyciski receptur.")]
    public Transform recipeGridContainer;

    [Tooltip("Prefab przycisku, który ma byæ instancjonowany dla ka¿dej receptury.")]
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
        // Jeœli z jakiegoœ powodu nie ma managera, poka¿emy tylko receptury bez wymagañ

        List<IBuildingRecipe> unlockedRecipes = recipes.Where(r =>
        {
            // Jeœli ID jest puste, receptura jest darmowa (dostêpna od pocz¹tku)
            if (string.IsNullOrEmpty(r.techRequirementId)) return true;

            // W przeciwnym razie sprawdŸ, czy technologia jest zbadana
            return tree != null && tree.IsResearched(r.techRequirementId);
        }).ToList();

        // 3. Sprawdzenie czy mamy co wyœwietliæ po filtracji
        if (unlockedRecipes.Count == 0)
        {
            Debug.LogWarning($"Brak ODBLOKOWANYCH receptur dla {building.GetType().Name}.");
            // Opcjonalnie: wyœwietl komunikat w UI: "Wymagane dalsze badania"
            return;
        }


        if (recipeTooltip != null)
        {
            recipeTooltip.Hide();
        }

        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogWarning($"Nie znaleziono dostêpnych receptur dla {building.GetType().Name}.");
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
        if (recipe is SmeltingRecipeData smeltingRecipe)
        {
            return smeltingRecipe.outputItem;
        }
        else if (recipe is AssemblyRecipeData assemblyRecipe)
        {
            return assemblyRecipe.outputItem;
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
        if (currentBuilding is FurnaceBuilding furnace && selectedRecipe is SmeltingRecipeData smeltingRecipe)
        {
            furnace.SetRecipe(smeltingRecipe);
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenFurnaceStatus(furnace);
        }
        else if (currentBuilding is AssemblerBuilding assembler && selectedRecipe is AssemblyRecipeData assemblyRecipe)
        {
            assembler.SetRecipe(assemblyRecipe);
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenAssemblerStatus(assembler);
        }
        else
        {
            Debug.LogError($"B³¹d: Nie mo¿na przypisaæ receptury {selectedRecipe.GetType().Name} do budynku {currentBuilding.GetType().Name}.");
        }

        if (recipeTooltip != null)
        {
            recipeTooltip.Hide();
        }
    }

    private void OnDisable()
    {
        if (recipeTooltip != null)
        {
            recipeTooltip.Hide();
        }
    }
}