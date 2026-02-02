using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechNodeUI : MonoBehaviour
{
    public Image backgroundImage; // Obrazek t³a kafelka
    public TextMeshProUGUI titleText;
    public Image iconImage;

    // --- DODAJ TÊ LINIKÊ ---
    private TechnologyNode currentNode;
    // -----------------------

    public void Setup(TechnologyNode node)
    {
        // Zapisujemy referencjê do danych, aby OnNodeClicked móg³ z nich skorzystaæ
        currentNode = node;

        titleText.text = node.name;

        // Szukanie ikony (Twoja obecna logika)
        ResourceData data = Resources.Load<ResourceData>("Items/" + node.iconName);

        if (data != null)
        {
            iconImage.sprite = data.icon;
        }
        else
        {
            Sprite techSprite = Resources.Load<Sprite>("Sprites/" + node.iconName);
            if (techSprite != null)
            {
                iconImage.sprite = techSprite;
            }
            else
            {
                Debug.LogWarning($"Nie znaleziono grafiki '{node.iconName}' ani w Items/ ani w Sprites/");
            }
        }
        RefreshColor();
    }

    public void RefreshColor()
    {
        // Zabezpieczenie: jeœli manager jeszcze nie istnieje, przerwij
        if (TechTreeManager.Instance == null) return;

        if (TechTreeManager.Instance.IsResearched(currentNode.id))
        {
            backgroundImage.color = Color.green;
        }
        else if (TechTreeManager.Instance.CanResearch(currentNode))
        {
            backgroundImage.color = new Color(1f, 0.6f, 0f); // Pomarañczowy
        }
    }

    public void OnNodeClicked()
    {
        if (currentNode != null)
        {
            TechDetailsUI.Instance.Open(currentNode);
        }
    }
}