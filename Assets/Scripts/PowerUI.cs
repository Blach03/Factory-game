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

        // Kolorowanie tekstu: czerwony jeœli brakuje pr¹du
        string color = prod >= cons ? "white" : "red";

        powerText.text = $"Power: <color={color}>{cons:F1}</color> / {prod:F1} MW";
    }
}