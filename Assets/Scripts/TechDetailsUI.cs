using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TechDetailsUI : MonoBehaviour
{
    public static TechDetailsUI Instance;

    [Header("UI Elements")]
    public Image mainIcon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public GameObject costPrefab; // Ma³y prefab: Ikona surowca + tekst iloœci
    public Transform costContainer;
    public Image[] previewImages; // Tablica 3 obrazków
    public GameObject panel;
    public Image explanationImage; // Przeci¹gnij obiekt Image z panelu

    public Button researchButton;
    public TextMeshProUGUI costHeaderLabel; // Napis "COST:" lub "RESEARCHED"
    public GameObject costScrollArea; // Kontener na koszty (by go ukryæ)

    private TechnologyNode activeNode;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject); // Zapobiega duplikatom
    }

    void Start()
    {
        panel.SetActive(false); // Wy³¹cza panel dopiero po tym, jak Instance zosta³ ustawiony
    }

    public void Open(TechnologyNode node)
    {
        activeNode = node;
        panel.SetActive(true);

        // 1. Podstawowe dane: Tytu³, Opis, Ikona G³ówna
        titleText.text = node.name;
        descriptionText.text = node.description;
        mainIcon.sprite = LoadSprite(node.iconName);

        if (!string.IsNullOrEmpty(node.explanationSpriteName))
        {
            Sprite expSprite = LoadSprite(node.explanationSpriteName);
            if (expSprite != null)
            {
                explanationImage.gameObject.SetActive(true);
                explanationImage.sprite = expSprite;
            }
            else
            {
                explanationImage.gameObject.SetActive(false);
            }
        }
        else
        {
            explanationImage.gameObject.SetActive(false);
        }

        // 2. Czyszczenie i generowanie kosztów (na wzór UIManager)
        foreach (Transform child in costContainer) Destroy(child.gameObject);

        if (node.cost != null)
        {
            foreach (var cost in node.cost)
            {
                // Tworzymy element kosztu korzystaj¹c z prefaba
                GameObject go = Instantiate(costPrefab, costContainer);

                // Pobieramy komponenty z prefaba (ikona i tekst)
                var icon = go.GetComponentInChildren<Image>();
                var text = go.GetComponentInChildren<TextMeshProUGUI>();

                // £adujemy dane o surowcu z Resources

                string fullPath = "Items/" + cost.resource;
                Debug.Log($"Próbujê za³adowaæ: [{fullPath}]");
                ResourceData data = Resources.Load<ResourceData>(fullPath);


                if (data != null)
                {
                    // Sprawdzamy iloœæ w ekwipunku przez PlayerInventory
                    int invCount = PlayerInventory.Instance.GetItemCount(data);

                    icon.sprite = data.icon;
                    text.text = $"{invCount}/{cost.amount}";

                    // Kolorowanie tekstu: Zielony (staæ nas), Czerwony (brak surowców)
                    text.color = invCount >= cost.amount ? Color.green : Color.red;
                }
                else
                {
                    Debug.LogWarning($"TechDetailsUI: Nie znaleziono ResourceData dla: {cost.resource}");
                    Destroy(go);
                }
            }
        }

        // 3. Podgl¹d 3 obrazków (receptury / odblokowania)
        for (int i = 0; i < previewImages.Length; i++)
        {
            // Sprawdzamy czy mamy zdefiniowane ikony podgl¹du w JSON dla tego indeksu
            if (node.unlockPreviewIcons != null && i < node.unlockPreviewIcons.Count)
            {
                Sprite s = LoadSprite(node.unlockPreviewIcons[i]);
                if (s != null)
                {
                    previewImages[i].gameObject.SetActive(true);
                    previewImages[i].sprite = s;
                }
                else
                {
                    previewImages[i].gameObject.SetActive(false);
                }
            }
            else
            {
                // Wy³¹czamy Image, jeœli nie ma nic do wyœwietlenia
                previewImages[i].gameObject.SetActive(false);
            }
        }
        UpdateUIState(node);
    }

    private void UpdateUIState(TechnologyNode node)
    {
        bool researched = TechTreeManager.Instance.IsResearched(node.id);
        bool canResearch = TechTreeManager.Instance.CanResearch(node);
        bool hasResources = RefreshCostIcons(node); // Metoda zwraca true/false

        if (researched)
        {
            costHeaderLabel.text = "RESEARCHED";
            costScrollArea.SetActive(false); // Ukrywamy pole z kosztami
            researchButton.gameObject.SetActive(false); // Ukrywamy guzik
        }
        else
        {
            costHeaderLabel.text = "COST:";
            costScrollArea.SetActive(true);
            researchButton.gameObject.SetActive(true);

            if (!canResearch)
            {
                SetButtonState(Color.gray, false); // Szary - zablokowane przez drzewko
            }
            else if (hasResources)
            {
                SetButtonState(Color.green, true); // Zielony - mo¿na badaæ
            }
            else
            {
                SetButtonState(Color.red, false); // Czerwony - brak surowców
            }
        }
    }

    private bool RefreshCostIcons(TechnologyNode node)
    {
        bool allMet = true;
        foreach (Transform child in costContainer) Destroy(child.gameObject);

        if (node.cost != null)
        {
            foreach (var cost in node.cost)
            {
                GameObject go = Instantiate(costPrefab, costContainer);
                var icon = go.GetComponentInChildren<Image>();
                var text = go.GetComponentInChildren<TextMeshProUGUI>();
                ResourceData data = Resources.Load<ResourceData>("Items/" + cost.resource);

                if (data != null)
                {
                    int invCount = PlayerInventory.Instance.GetItemCount(data);
                    icon.sprite = data.icon;
                    text.text = $"{invCount}/{cost.amount}";

                    if (invCount < cost.amount)
                    {
                        text.color = Color.red;
                        allMet = false;
                    }
                    else text.color = Color.green;
                }
            }
        }
        return allMet;
    }

    private void SetButtonState(Color col, bool interactable)
    {
        researchButton.image.color = col;
        researchButton.interactable = interactable;
    }

    public void OnResearchClick()
    {
        if (activeNode == null) return;

        // Zabierz surowce
        foreach (var cost in activeNode.cost)
        {
            ResourceData data = Resources.Load<ResourceData>("Items/" + cost.resource);
            PlayerInventory.Instance.RemoveItem(data, cost.amount);
        }

        TechTreeManager.Instance.ResearchTechnology(activeNode);
        UpdateUIState(activeNode);
    }

    private bool CheckResources(TechnologyNode node)
    {
        if (node.cost == null) return true;
        foreach (var cost in node.cost)
        {
            ResourceData data = Resources.Load<ResourceData>("Items/" + cost.resource);
            if (PlayerInventory.Instance.GetItemCount(data) < cost.amount) return false;
        }
        return true;
    }

    // Wywo³ywane przez OnClick na guziku Research


    private Sprite LoadSprite(string name)
    {
        // Twoja istniej¹ca logika szukania w Items/ i Sprites/
        ResourceData res = Resources.Load<ResourceData>("Items/" + name);
        if (res != null) return res.icon;
        return Resources.Load<Sprite>("Sprites/" + name);
    }

    public void Close() => panel.SetActive(false);
}