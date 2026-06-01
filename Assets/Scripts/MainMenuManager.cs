using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loadWindow;
    public Transform saveListContainer; // Content w Scroll View

    [Header("Prefabs")]
    public GameObject saveSlotPrefab; // Przycisk z tekstem nazwy save'a

    [Header("Save Info")]
    public TextMeshProUGUI saveLocationText;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (loadWindow.activeSelf)
            {
                CloseLoadWindow();
            }
        }
    }

    public void NewGame()
    {
        SaveManager saveManager = SaveManager.EnsureInstanceExists();
        SaveManager.QueueNewGameRequest();
        saveManager.StartNewGameFromMenu();
    }

    public void OpenLoadWindow()
    {
        loadWindow.SetActive(true);

        if (saveLocationText != null && SaveManager.Instance != null)
        {
            saveLocationText.text = $"Save location: {SaveManager.Instance.SaveFolderPath}";
        }
        // Czy�cimy star� list�
        foreach (Transform child in saveListContainer) { Destroy(child.gameObject); }

        // Pobieramy pliki z SaveManagera
        FileInfo[] saves = SaveManager.Instance.GetAllSaveFiles();

        foreach (FileInfo save in saves)
        {
            GameObject slot = Instantiate(saveSlotPrefab, saveListContainer);

            // Formatowanie wy�wietlanej nazwy (usuwamy .json dla estetyki)
            string displayName = save.Name.Replace(".json", "");
            slot.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{displayName}\n<size=50%>{save.LastWriteTime}</size>";

            slot.GetComponent<Button>().onClick.AddListener(() => {
                SelectSave(save.Name);
            });
        }
    }

    private void SelectSave(string fileName)
    {
        SaveManager saveManager = SaveManager.EnsureInstanceExists();
        SaveManager.QueueLoadGameRequest(fileName);
        saveManager.StartLoadGameFromMenu(fileName);
    }

    public void CloseLoadWindow() => loadWindow.SetActive(false);

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}