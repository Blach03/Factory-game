using UnityEngine;
using TMPro;

public class PowerUI : MonoBehaviour
{
    public TextMeshProUGUI powerText;

    void Update()
    {
        if (PowerManager.Instance == null) return;

        float prod = PowerManager.Instance.GlobalProduction;
        float cons = PowerManager.Instance.GlobalConsumption;

        // Kolorowanie tekstu: czerwony je�li brakuje pr�du
        string color = prod >= cons ? "white" : "red";

        powerText.text = $"<color={color}>{cons:F0}</color> / {prod:F0} MW";
    }
}