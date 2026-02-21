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

    private void LateUpdate()
    {
        // Na koniec klatki przepisujemy wartoœci i resetujemy liczniki dla nastêpnej klatki
        GlobalProduction = frameProduction;
        GlobalConsumption = frameConsumption;

        frameProduction = 0;
        frameConsumption = 0;
    }
}