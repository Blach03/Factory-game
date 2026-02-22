using UnityEngine;

public class PowerManager : MonoBehaviour
{
    public static PowerManager Instance { get; private set; }

    public float GlobalProduction { get; private set; }
    public float GlobalConsumption { get; private set; }

    private float frameProduction;
    private float frameConsumption;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Maszyny wywo³uj¹ to co klatkê w Update/Tick
    public void RegisterProduction(float amount) => frameProduction += amount;
    public void RegisterConsumption(float amount) => frameConsumption += amount;

    public bool HasEnoughPower()
    {
        // Maszyny sprawdzaj¹ stan z poprzedniej klatki (stabilny odczyt)
        // lub bie¿¹cy, jeœli chcemy natychmiastowej reakcji.
        return frameConsumption < GlobalProduction;
    }

    private void LateUpdate()
    {
        GlobalProduction = frameProduction;
        GlobalConsumption = frameConsumption;

        frameProduction = 0;
        frameConsumption = 0;
    }
}