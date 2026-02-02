using UnityEngine;
using UnityEngine.SceneManagement;
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
        SaveManager.Instance.saveToLoad = ""; // Czyœcimy, aby gra wiedzia³a, ¿e to nowa gra
        SceneManager.LoadScene("GameScene"); // Zmieñ na nazwê Twojej sceny z gr¹
    }

    public void OpenLoadWindow()
    {
        loadWindow.SetActive(true);

        if (saveLocationText != null && SaveManager.Instance != null)
        {
            saveLocationText.text = $"Save location: {SaveManager.Instance.SaveFolderPath}";
        }
        // Czyœcimy star¹ listê
        foreach (Transform child in saveListContainer) { Destroy(child.gameObject); }

        // Pobieramy pliki z SaveManagera
        FileInfo[] saves = SaveManager.Instance.GetAllSaveFiles();

        foreach (FileInfo save in saves)
        {
            GameObject slot = Instantiate(saveSlotPrefab, saveListContainer);

            // Formatowanie wyœwietlanej nazwy (usuwamy .json dla estetyki)
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
        SaveManager.Instance.SetSaveToLoad(fileName);
        SceneManager.LoadScene("GameScene");
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