using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HandCraftingManager : MonoBehaviour
{
    public static HandCraftingManager Instance;

    // Używamy JEDYNIE statycznych zmiennych jako główny system craftu
    private static List<IBuildingRecipe> staticCraftQueue = new List<IBuildingRecipe>();
    private static float staticCraftTimer = 0f;
    private static bool staticIsCrafting = false;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(true);
    }

    private void Start()
    {
        gameObject.SetActive(true);
    }

    private void Update()
    {
        // Normalnie aktualizuj crafting z głównej statycznej listy
        UpdateCraftingLogic(Time.deltaTime);
    }

    /// <summary>
    /// Statyczna metoda aktualizacji craftu, którą można wywoływać z zewnątrz
    /// Pozwala na aktualizację craftu nawet gdy MainCraftingManager jest wyłączony
    /// </summary>
    public static void UpdateCraftingBackground(float deltaTime)
    {
        if (staticCraftQueue.Count > 0)
        {
            if (!staticIsCrafting)
            {
                staticIsCrafting = true;
                staticCraftTimer = GetCraftTimeStatic(staticCraftQueue[0]);
            }

            staticCraftTimer -= deltaTime;
            if (staticCraftTimer <= 0)
            {
                FinishCraftingStatic();
            }
        }
    }

    private void UpdateCraftingLogic(float deltaTime)
    {
        UpdateCraftingBackground(deltaTime);
    }

    private static void FinishCraftingStatic()
    {
        if (staticCraftQueue.Count > 0)
        {
            IBuildingRecipe finishedRecipe = staticCraftQueue[0];
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(finishedRecipe.outputItem, finishedRecipe.outputAmount);
            }

            staticCraftQueue.RemoveAt(0);
            staticIsCrafting = false;

            // Odśwież UI
            InventoryUI invUI = FindObjectOfType<InventoryUI>();
            if (invUI != null) invUI.SetupInventory();
        }
    }

    public void AddToQueue(IBuildingRecipe recipe, int amount)
    {
        // amount = ile razy chcemy wykonać recepturę (np. x10 = 10 craftów)
        int recipesToQueue = amount;
        
        for (int i = 0; i < recipesToQueue; i++)
        {
            // Sprawdź czy stać gracza (pobierz surowce od razu)
            if (CanAfford(recipe))
            {
                ConsumeResources(recipe);
                staticCraftQueue.Add(recipe);
            }
            else break;
        }
        var details = FindObjectOfType<CraftingDetailsPanel>();
        if (details != null) details.RefreshCurrentUI();

        var invUI = FindObjectOfType<InventoryUI>();
        if (invUI != null) invUI.SetupInventory();
    }

    private bool CanAfford(IBuildingRecipe r)
    {
        bool p = PlayerInventory.Instance.GetItemCount(r.primaryInput) >= r.primaryInputAmount;
        bool s = r.secondaryInput == null || PlayerInventory.Instance.GetItemCount(r.secondaryInput) >= r.secondaryInputAmount;
        
        // Sprawdź trzeci komponent dla AssemblyRecipeData
        bool t = true;
        if (r is AssemblyRecipeData assembly && assembly.tertiaryInput != null)
            t = PlayerInventory.Instance.GetItemCount(assembly.tertiaryInput) >= assembly.tertiaryInputAmount;
        
        return p && s && t;
    }

    private void ConsumeResources(IBuildingRecipe r)
    {
        PlayerInventory.Instance.RemoveItem(r.primaryInput, r.primaryInputAmount);
        if (r.secondaryInput != null) 
            PlayerInventory.Instance.RemoveItem(r.secondaryInput, r.secondaryInputAmount);
        
        // Usuń trzeci komponent dla AssemblyRecipeData
        if (r is AssemblyRecipeData assembly && assembly.tertiaryInput != null)
            PlayerInventory.Instance.RemoveItem(assembly.tertiaryInput, assembly.tertiaryInputAmount);
    }

    private static float GetCraftTimeStatic(IBuildingRecipe r)
    {
        if (r is SmeltingRecipeData s) return s.smeltingTime;
        if (r is AssemblyRecipeData a) return a.assemblyTime;
        return 1f;
    }

    public int GetQueueCount() => staticCraftQueue.Count;
    public float GetProgress() => staticCraftQueue.Count > 0 ? 1f - (staticCraftTimer / GetCraftTimeStatic(staticCraftQueue[0])) : 0f;

    public static HandCraftingQueueSaveData GetQueueSnapshotForSave()
    {
        HandCraftingQueueSaveData data = new HandCraftingQueueSaveData();

        foreach (IBuildingRecipe recipe in staticCraftQueue)
        {
            if (recipe == null)
            {
                continue;
            }

            data.entries.Add(new HandCraftingQueueEntrySaveData
            {
                recipeType = GetRecipeType(recipe),
                recipeName = recipe.recipeName,
                recipeAssetName = (recipe as Object)?.name,
                outputResourceName = recipe.outputItem != null ? recipe.outputItem.resourceName : string.Empty
            });
        }

        if (staticCraftQueue.Count > 0)
        {
            data.currentRecipeRemainingTimeSeconds = Mathf.Max(0f, staticCraftTimer);
        }

        return data;
    }

    public static void RestoreQueueSnapshot(HandCraftingQueueSaveData data)
    {
        staticCraftQueue.Clear();
        staticCraftTimer = 0f;
        staticIsCrafting = false;

        if (data == null || data.entries == null || data.entries.Count == 0)
        {
            RefreshCraftingUi();
            return;
        }

        foreach (HandCraftingQueueEntrySaveData entry in data.entries)
        {
            IBuildingRecipe recipe = ResolveRecipe(entry);
            if (recipe != null)
            {
                staticCraftQueue.Add(recipe);
            }
        }

        if (staticCraftQueue.Count > 0)
        {
            float fallbackCraftTime = GetCraftTimeStatic(staticCraftQueue[0]);
            staticCraftTimer = data.currentRecipeRemainingTimeSeconds > 0.001f
                ? Mathf.Clamp(data.currentRecipeRemainingTimeSeconds, 0f, fallbackCraftTime)
                : fallbackCraftTime;
            staticIsCrafting = staticCraftTimer > 0.001f;
        }

        RefreshCraftingUi();
    }

    private static string GetRecipeType(IBuildingRecipe recipe)
    {
        if (recipe is SmeltingRecipeData)
        {
            return "Smelting";
        }

        if (recipe is AssemblyRecipeData)
        {
            return "Assembly";
        }

        return string.Empty;
    }

    private static IBuildingRecipe ResolveRecipe(HandCraftingQueueEntrySaveData entry)
    {
        if (entry == null)
        {
            return null;
        }

        bool preferSmelting = string.Equals(entry.recipeType, "Smelting", System.StringComparison.OrdinalIgnoreCase);
        bool preferAssembly = string.Equals(entry.recipeType, "Assembly", System.StringComparison.OrdinalIgnoreCase);

        if (!preferAssembly)
        {
            SmeltingRecipeData smeltingMatch = FindRecipe(Resources.LoadAll<SmeltingRecipeData>("Recipes"), entry) as SmeltingRecipeData;
            if (smeltingMatch != null)
            {
                return smeltingMatch;
            }
        }

        if (!preferSmelting)
        {
            AssemblyRecipeData assemblyMatch = FindRecipe(Resources.LoadAll<AssemblyRecipeData>("Recipes"), entry) as AssemblyRecipeData;
            if (assemblyMatch != null)
            {
                return assemblyMatch;
            }
        }

        return null;
    }

    private static IBuildingRecipe FindRecipe<T>(T[] recipes, HandCraftingQueueEntrySaveData entry) where T : Object, IBuildingRecipe
    {
        if (recipes == null || recipes.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(entry.recipeName))
        {
            T matchByRecipeName = recipes.FirstOrDefault(r => r != null &&
                string.Equals(r.recipeName, entry.recipeName, System.StringComparison.OrdinalIgnoreCase));
            if (matchByRecipeName != null)
            {
                return matchByRecipeName;
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.recipeAssetName))
        {
            T matchByAssetName = recipes.FirstOrDefault(r => r != null &&
                string.Equals(r.name, entry.recipeAssetName, System.StringComparison.OrdinalIgnoreCase));
            if (matchByAssetName != null)
            {
                return matchByAssetName;
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.outputResourceName))
        {
            T matchByOutput = recipes.FirstOrDefault(r =>
                r != null &&
                r.outputItem != null &&
                string.Equals(r.outputItem.resourceName, entry.outputResourceName, System.StringComparison.OrdinalIgnoreCase));
            if (matchByOutput != null)
            {
                return matchByOutput;
            }
        }

        return null;
    }

    private static void RefreshCraftingUi()
    {
        InventoryUI invUI = FindObjectOfType<InventoryUI>();
        if (invUI != null)
        {
            invUI.SetupInventory();
        }

        CraftingDetailsPanel detailsPanel = FindObjectOfType<CraftingDetailsPanel>();
        if (detailsPanel != null)
        {
            detailsPanel.RefreshCurrentUI();
        }
    }
}