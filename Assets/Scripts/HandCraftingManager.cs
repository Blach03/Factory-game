using UnityEngine;
using System.Collections.Generic;

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
        // amount = ile przedmiotów finałnych chcemy stworzyć
        // Obliczamy ile razy trzeba wykonać recepturę na podstawie outputAmount
        int recipesToQueue = Mathf.CeilToInt((float)amount / recipe.outputAmount);
        
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
}