using UnityEngine;
using System.Collections.Generic;

public class HandCraftingManager : MonoBehaviour
{
    public static HandCraftingManager Instance;

    private List<IBuildingRecipe> craftQueue = new List<IBuildingRecipe>();
    private float currentCraftTimer = 0f;
    private bool isCrafting = false;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (craftQueue.Count > 0)
        {
            if (!isCrafting) StartNextItem();

            currentCraftTimer -= Time.deltaTime;
            if (currentCraftTimer <= 0)
            {
                FinishCrafting();
            }
        }
    }

    private void StartNextItem()
    {
        isCrafting = true;
        currentCraftTimer = GetCraftTime(craftQueue[0]);
    }

    private void FinishCrafting()
    {
        IBuildingRecipe finishedRecipe = craftQueue[0];
        PlayerInventory.Instance.AddItem(finishedRecipe.outputItem, finishedRecipe.outputAmount);

        craftQueue.RemoveAt(0);
        isCrafting = false;

        // Odœwie¿ UI ekwipunku po dodaniu przedmiotu
        FindObjectOfType<InventoryUI>().SetupInventory();
    }

    public void AddToQueue(IBuildingRecipe recipe, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // SprawdŸ czy staæ gracza (pobierz surowce od razu)
            if (CanAfford(recipe))
            {
                ConsumeResources(recipe);
                craftQueue.Add(recipe);
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
        // Tutaj mo¿na dodaæ tertiary dla AssemblyRecipeData
        return p && s;
    }

    private void ConsumeResources(IBuildingRecipe r)
    {
        PlayerInventory.Instance.RemoveItem(r.primaryInput, r.primaryInputAmount);
        if (r.secondaryInput != null) PlayerInventory.Instance.RemoveItem(r.secondaryInput, r.secondaryInputAmount);
    }

    private float GetCraftTime(IBuildingRecipe r)
    {
        if (r is SmeltingRecipeData s) return s.smeltingTime;
        if (r is AssemblyRecipeData a) return a.assemblyTime;
        return 1f;
    }

    public int GetQueueCount() => craftQueue.Count;
    public float GetProgress() => craftQueue.Count > 0 ? 1f - (currentCraftTimer / GetCraftTime(craftQueue[0])) : 0f;
}