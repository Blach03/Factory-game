using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PipeNetworkUI : MonoBehaviour
{
    public static PipeNetworkUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject windowPanel;
    public TextMeshProUGUI statusText;
    public Image fluidIcon;
    public Slider fillSlider;

    private PipeNetwork currentNetwork;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Zapobiega duplikatom UI
        }
    }

    void Start()
    {
        // Ukrywamy okno dopiero w pierwszej klatce gry
        // Dzięki temu Awake() na pewno się wykonało
        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (windowPanel.activeSelf)
        {
            UpdateDisplay();
        }
    }

    public void OpenWindow(PipeNetwork network)
    {
        currentNetwork = network;
        windowPanel.SetActive(true);
        UpdateDisplay();
    }

    public void CloseWindow() => windowPanel.SetActive(false);

    private void UpdateDisplay()
    {
        if (currentNetwork == null) return;

        statusText.text = $"Network Storage: {currentNetwork.storedFluid:F1} / {currentNetwork.MaxCapacity} units";
        fillSlider.maxValue = currentNetwork.MaxCapacity;
        fillSlider.value = currentNetwork.storedFluid;

        if (currentNetwork.FluidType != null)
        {
            fluidIcon.sprite = currentNetwork.FluidType.icon;
            fluidIcon.enabled = true;
        }
        else
        {
            fluidIcon.enabled = false;
        }
    }
}