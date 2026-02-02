using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject mainPanel; // Opcjonalne, jeœli skrypt jest na panelu nadrzêdnym
    public GameObject saveWindow; // Przeci¹gnij tutaj panel zapisu
    public GameObject overwriteConfirmPanel; // Przeci¹gnij tutaj panel potwierdzenia

    [Header("Save List Settings")]
    public Transform saveListContainer;
    public GameObject saveSlotPrefab;
    public TMP_InputField newSaveInputField;

    private string selectedFileName; // Przechowuje nazwê wybranego save'a do nadpisania

    public void OnCancelClicked()
    {
        saveWindow.SetActive(false);
    }

    public void OnResumeClicked()
    {
        // Wywo³ujemy funkcjê z UIManager, która przywróci Time.timeScale = 1
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

    // Wywo³ywane przez przycisk "TAK" w oknie potwierdzenia
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
        newSaveInputField.text = ""; // Wyczyœæ pole
        RefreshSaveList();
        saveWindow.SetActive(false);
    }

    public void OnSettingsClicked()
    {
        // Miejsce na przysz³¹ logikê ustawieñ (dŸwiêk, grafika)
        Debug.Log("Otwieranie ustawieñ (funkcja w przygotowaniu).");
    }

    public void OnSaveandExitClicked()
    {
        Debug.Log("Zamykanie aplikacji...");
        SaveManager.Instance.SaveGame();

        // Zapisz przed wyjœciem (opcjonalnie)
        // SaveManager.Instance.SaveGame("AutoSave_Exit.json");

        Application.Quit();

        // Jeœli testujesz wewn¹trz Unity Editor:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}