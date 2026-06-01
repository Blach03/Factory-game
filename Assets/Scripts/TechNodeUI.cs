using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechNodeUI : MonoBehaviour
{
    public Image backgroundImage; // Obrazek t�a kafelka
    public TextMeshProUGUI titleText;
    public Image iconImage;

    // --- DODAJ T� LINIK� ---
    private TechnologyNode currentNode;
    // -----------------------

    public void Setup(TechnologyNode node)
    {
        // Zapisujemy referencj� do danych, aby OnNodeClicked m�g� z nich skorzysta�
        currentNode = node;

        titleText.text = node.name;

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = null;
        }

        // Niektóre technologie nie mają jawnie ustawionej ikony w JSON.
        // W takim przypadku nie logujemy warningu i po prostu ukrywamy ikonę.
        if (string.IsNullOrWhiteSpace(node.iconName))
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
            RefreshColor();
            return;
        }

        // Szukanie ikony (Twoja obecna logika)
        ResourceData data = Resources.Load<ResourceData>("Items/" + node.iconName);

        if (data != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = data.icon != null;
            }
        }
        else
        {
            Sprite techSprite = Resources.Load<Sprite>("Sprites/" + node.iconName);
            if (techSprite != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = techSprite;
                    iconImage.enabled = true;
                }
            }
            else
            {
                Debug.LogWarning($"Nie znaleziono grafiki '{node.iconName}' ani w Items/ ani w Sprites/");
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }
            }
        }
        RefreshColor();
    }

    public void RefreshColor()
    {
        // Zabezpieczenie: je�li manager jeszcze nie istnieje, przerwij
        if (TechTreeManager.Instance == null) return;

        if (TechTreeManager.Instance.IsResearched(currentNode.id))
        {
            backgroundImage.color = Color.green;
        }
        else if (TechTreeManager.Instance.CanResearch(currentNode))
        {
            backgroundImage.color = new Color(1f, 0.6f, 0f); // Pomara�czowy
        }
        else
        {
            // Domyślny kolor dla technologii zablokowanych.
            backgroundImage.color = new Color(0.35f, 0.35f, 0.35f, 1f);
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