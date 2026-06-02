using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Panel Navigation (optional)")]
    public GameObject settingsPanel;
    public GameObject mainPanel;
    public bool closeWithEscape = true;

    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider uiSfxSlider;
    public Slider buildSfxSlider;
    public Slider machineSlider;
    public Slider musicSlider;

    [Header("Percent Labels (optional)")]
    public TMP_Text masterVolumePercentText;
    public TMP_Text uiSfxPercentText;
    public TMP_Text buildSfxPercentText;
    public TMP_Text machinePercentText;
    public TMP_Text musicPercentText;

    private void Awake()
    {
        RegisterSlider(masterVolumeSlider, OnMasterVolumeChanged);
        RegisterSlider(uiSfxSlider, OnUiSfxChanged);
        RegisterSlider(buildSfxSlider, OnBuildSfxChanged);
        RegisterSlider(machineSlider, OnMachineChanged);
        RegisterSlider(musicSlider, OnMusicChanged);
    }

    private void OnEnable()
    {
        RefreshFromAudioManager();
    }

    public void RefreshFromAudioManager()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        SetSliderValue(masterVolumeSlider, audioManager.MasterVolume);
        SetSliderValue(uiSfxSlider, audioManager.UiSfxCategoryVolume);
        SetSliderValue(buildSfxSlider, audioManager.BuildSfxCategoryVolume);
        SetSliderValue(machineSlider, audioManager.MachineCategoryVolume);
        SetSliderValue(musicSlider, audioManager.MusicCategoryVolume);
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (ShouldHideMainPanelWhenOpeningSettings())
        {
            mainPanel.SetActive(false);
        }

        RefreshFromAudioManager();
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }

    public void ToggleSettingsPanel()
    {
        bool isOpen = settingsPanel != null && settingsPanel.activeSelf;
        if (isOpen)
        {
            CloseSettingsPanel();
        }
        else
        {
            OpenSettingsPanel();
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        UpdatePercentLabel(masterVolumePercentText, value);
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void OnUiSfxChanged(float value)
    {
        UpdatePercentLabel(uiSfxPercentText, value);
        AudioManager.Instance?.SetUiSfxCategoryVolume(value);
    }

    private void OnBuildSfxChanged(float value)
    {
        UpdatePercentLabel(buildSfxPercentText, value);
        AudioManager.Instance?.SetBuildSfxCategoryVolume(value);
    }

    private void OnMachineChanged(float value)
    {
        UpdatePercentLabel(machinePercentText, value);
        AudioManager.Instance?.SetMachineCategoryVolume(value);
    }

    private void OnMusicChanged(float value)
    {
        UpdatePercentLabel(musicPercentText, value);
        AudioManager.Instance?.SetMusicCategoryVolume(value);
    }

    private static void RegisterSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(callback);
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private static void UpdatePercentLabel(TMP_Text label, float value)
    {
        if (label == null)
        {
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        label.text = percent + "%";
    }

    private void LateUpdate()
    {
        // Keep labels in sync even if slider values are changed externally.
        UpdatePercentLabel(masterVolumePercentText, masterVolumeSlider != null ? masterVolumeSlider.value : 0f);
        UpdatePercentLabel(uiSfxPercentText, uiSfxSlider != null ? uiSfxSlider.value : 0f);
        UpdatePercentLabel(buildSfxPercentText, buildSfxSlider != null ? buildSfxSlider.value : 0f);
        UpdatePercentLabel(machinePercentText, machineSlider != null ? machineSlider.value : 0f);
        UpdatePercentLabel(musicPercentText, musicSlider != null ? musicSlider.value : 0f);
    }

    private void Update()
    {
        if (!closeWithEscape)
        {
            return;
        }

        if (settingsPanel == null || !settingsPanel.activeSelf)
        {
            return;
        }

        // In-game pause already has dedicated ESC handling in UIManager/PauseMenuUI.
        if (UIManager.Instance != null && UIManager.Instance.pauseMenuPanel != null && UIManager.Instance.pauseMenuPanel.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettingsPanel();
        }
    }

    private bool ShouldHideMainPanelWhenOpeningSettings()
    {
        if (mainPanel == null)
        {
            return false;
        }

        if (settingsPanel == null)
        {
            return true;
        }

        if (mainPanel == settingsPanel)
        {
            return false;
        }

        // If settings is nested under main panel, hiding main would hide settings too.
        if (settingsPanel.transform.IsChildOf(mainPanel.transform))
        {
            return false;
        }

        return true;
    }
}
