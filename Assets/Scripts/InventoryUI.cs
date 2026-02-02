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
    private string lastSelectedResourceName; // Zmienna do zapamiêtania wyboru

    public void SetupInventory()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (PlayerInventory.Instance == null) return;

        List<ResourceData> allResources = PlayerInventory.Instance.GetAllGameResources();


        if (craftingDetailsPanel != null) craftingDetailsPanel.ClearDetails();
        

        foreach (ResourceData resource in allResources)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, contentParent);

            Image itemIcon = slotGO.transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI nameText = slotGO.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI countText = slotGO.transform.Find("CountText").GetComponent<TextMeshProUGUI>();

            nameText.text = resource.resourceName;

            if (resource.icon != null)
            {
                itemIcon.sprite = resource.icon;
            }

            int count = PlayerInventory.Instance.GetItemCount(resource);
            countText.text = count.ToString();

            Button slotButton = slotGO.GetComponentInChildren<Button>();

            if (slotButton != null)
            {
                ResourceData capturedResource = resource;
                slotButton.onClick.AddListener(() =>
                {
                    SelectResource(capturedResource);
                });

                // NOWOŒÆ: Jeœli ten przedmiot by³ wczeœniej wybrany, odœwie¿ panel od razu
                if (lastSelectedResourceName == resource.resourceName)
                {
                    craftingDetailsPanel.DisplayItem(resource);
                    // Opcjonalnie: tutaj mo¿esz dodaæ kod wizualnego podœwietlenia ramki slotu
                }
            }
            if (slotButton != null && craftingDetailsPanel != null)
            {
                ResourceData capturedResource = resource;
                slotButton.onClick.AddListener(() => craftingDetailsPanel.DisplayItem(capturedResource));
            }
        }

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