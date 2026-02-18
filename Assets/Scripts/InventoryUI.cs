using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Referencje UI")]
    public Transform contentParent;
    public GameObject inventorySlotPrefab;

    public CraftingDetailsPanel craftingDetailsPanel;
    private string lastSelectedResourceName; // Zmienna do zapami�tania wyboru

    public void SetupInventory()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (PlayerInventory.Instance == null) return;
        if (craftingDetailsPanel != null) craftingDetailsPanel.ClearDetails();

        List<ResourceData> allResources = PlayerInventory.Instance.GetAllGameResources();

        foreach (ResourceData resource in allResources)
        {
            // 1. FILTR: Czy przedmiot można trzymać w EQ (np. Oil ma to odznaczone)
            if (resource.canBeStoredInInventory == false) continue;

            // 2. FILTR: Czy technologia jest odblokowana
            if (!IsResourceUnlocked(resource)) continue;

            GameObject slotGO = Instantiate(inventorySlotPrefab, contentParent);

            // Szukanie komponentów (zachowujemy Twoją strukturę)
            Image itemIcon = slotGO.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI nameText = slotGO.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI countText = slotGO.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();
            Button slotButton = slotGO.GetComponentInChildren<Button>();

            if (nameText != null) nameText.text = resource.resourceName;
            if (itemIcon != null && resource.icon != null) itemIcon.sprite = resource.icon;

            int count = PlayerInventory.Instance.GetItemCount(resource);
            if (countText != null) countText.text = count.ToString();

            if (slotButton != null)
            {
                ResourceData capturedResource = resource;
                slotButton.onClick.AddListener(() => SelectResource(capturedResource));

                if (lastSelectedResourceName == resource.resourceName)
                {
                    craftingDetailsPanel.DisplayItem(resource);
                }
            }
        }
    }

    private bool IsResourceUnlocked(ResourceData resource)
    {
        // Jeśli nie wymaga technologii, zwróć true
        if (string.IsNullOrEmpty(resource.requiredTechId)) return true;

        // Sprawdzamy w Twoim TechTreeManager
        if (TechTreeManager.Instance != null)
        {
            return TechTreeManager.Instance.IsResearched(resource.requiredTechId);
        }

        return false; // Jeśli managera nie ma, ukryj przedmiot dla bezpieczeństwa
    }


    private void SelectResource(ResourceData resource)
    {
        lastSelectedResourceName = resource.resourceName;
        craftingDetailsPanel.DisplayItem(resource);
    }


    public void UpdateInventoryCounts()
    {
        if (PlayerInventory.Instance == null) return;

        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform slot = contentParent.GetChild(i);

        }

    }
}