using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject mainPanel; // Opcjonalne, je�li skrypt jest na panelu nadrz�dnym
    public GameObject saveWindow; // Przeci�gnij tutaj panel zapisu
    public GameObject overwriteConfirmPanel; // Przeci�gnij tutaj panel potwierdzenia
    public GameObject settingsPanel;
    public AudioSettingsUI audioSettingsUI;

    [Header("Save List Settings")]
    public Transform saveListContainer;
    public GameObject saveSlotPrefab;
    public TMP_InputField newSaveInputField;

    private string selectedFileName; // Przechowuje nazw� wybranego save'a do nadpisania

    public void OnCancelClicked()
    {
        saveWindow.SetActive(false);
    }

    public void OnResumeClicked()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);

        // Wywo�ujemy funkcj� z UIManager, kt�ra przywr�ci Time.timeScale = 1
        UIManager.Instance.ClosePauseMenu();
    }

    public void OnOpenSaveWindowClicked()
    {
        saveWindow.SetActive(true);
        RefreshSaveList();
    }

    public void RefreshSaveList()
    {
        foreach (Transform child in saveListContainer) Destroy(child.gameObject);

        FileInfo[] saves = SaveManager.Instance.GetAllSaveFiles();
        foreach (FileInfo save in saves)
        {
            GameObject slot = Instantiate(saveSlotPrefab, saveListContainer);
            string cleanName = save.Name.Replace(".json", "");
            slot.GetComponentInChildren<TextMeshProUGUI>().text = $"{cleanName}\n<size=50%>{save.LastWriteTime}</size>";

            slot.GetComponent<Button>().onClick.AddListener(() => {
                PrepareOverwrite(save.Name);
            });
        }
    }

    private void PrepareOverwrite(string fileName)
    {
        selectedFileName = fileName;
        overwriteConfirmPanel.SetActive(true);
    }

    // Wywo�ywane przez przycisk "TAK" w oknie potwierdzenia
    public void ConfirmOverwrite()
    {
        SaveManager.Instance.SaveGameWithName(selectedFileName);
        overwriteConfirmPanel.SetActive(false);
        RefreshSaveList();
        saveWindow.SetActive(false);
    }

    public void DeclineOverwrite()
    {
        overwriteConfirmPanel.SetActive(false);
    }

    public void OnSaveClicked()
    {
        string newName = newSaveInputField.text;
        if (string.IsNullOrWhiteSpace(newName)) return;

        SaveManager.Instance.SaveGameWithName(newName);
        newSaveInputField.text = ""; // Wyczy�� pole
        RefreshSaveList();
        saveWindow.SetActive(false);
    }

    public void OnSettingsClicked()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (ShouldHideMainPanelWhenOpeningSettings())
        {
            mainPanel.SetActive(false);
        }

        audioSettingsUI?.RefreshFromAudioManager();
    }

    public void OnSettingsBackClicked()
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

    public bool HandleEscapeInPauseMenu()
    {
        if (overwriteConfirmPanel != null && overwriteConfirmPanel.activeSelf)
        {
            DeclineOverwrite();
            return true;
        }

        if (saveWindow != null && saveWindow.activeSelf)
        {
            OnCancelClicked();
            return true;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            OnSettingsBackClicked();
            return true;
        }

        return false;
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

        // If settings is nested under main panel, hiding main would also hide settings.
        if (settingsPanel.transform.IsChildOf(mainPanel.transform))
        {
            return false;
        }

        return true;
    }

    public void OnSaveandExitClicked()
    {
        Debug.Log("Zamykanie aplikacji...");
        SaveManager.Instance.SaveGame();

        // Zapisz przed wyj�ciem (opcjonalnie)
        // SaveManager.Instance.SaveGame("AutoSave_Exit.json");

        Application.Quit();

        // Je�li testujesz wewn�trz Unity Editor:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}