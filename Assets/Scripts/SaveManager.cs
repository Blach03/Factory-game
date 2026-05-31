using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [HideInInspector]
    public string saveToLoad = ""; // Nazwa pliku przekazana z Main Menu

    private float totalPlayTimeSeconds = 0f;

    private string baseSaveFolder;

    public string SaveFolderPath => baseSaveFolder;

    public float TotalPlayTimeSeconds => totalPlayTimeSeconds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // �cie�ka do AppData
            baseSaveFolder = Application.persistentDataPath;

            if (!Directory.Exists(baseSaveFolder))
            {
                Directory.CreateDirectory(baseSaveFolder);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Obs�uga zdarze� �adowania sceny
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            // 1. Podpinanie przycisku zapisu
            GameObject saveBtnObj = GameObject.Find("SaveButton");
            if (saveBtnObj != null)
            {
                UnityEngine.UI.Button btn = saveBtnObj.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(SaveGame);
                    Debug.Log("Pomy�lnie podpi�to funkcj� Save pod przycisk.");
                }
            }

            // 2. Uruchomienie procedury wczytywania/generowania z op�nieniem
            StartCoroutine(LoadProcessRoutine());
        }
    }

    private void Update()
    {
        // Liczymy tylko realny czas rozgrywki w scenie gry, niezale�nie od Time.timeScale.
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == "GameScene")
        {
            totalPlayTimeSeconds += Time.unscaledDeltaTime;
        }
    }

    private System.Collections.IEnumerator LoadProcessRoutine()
    {
        // Czekamy jedn� klatk�, aby upewni� si�, �e Awake() we wszystkich 
        // nowych obiektach (np. PlayerInventory) zosta� wykonany.
        yield return null;

        bool isNewGame = string.IsNullOrEmpty(saveToLoad);

        if (isNewGame)
        {
            totalPlayTimeSeconds = 0f;

            // --- TUTAJ ZMIANA: Szukamy nowego WorldGeneratora zamiast ResourceGenerator ---
            WorldGenerator generator = Object.FindFirstObjectByType<WorldGenerator>();
            if (generator != null)
            {
                generator.InitializeWorld();
                Debug.Log("<color=green>SaveManager:</color> Rozpocz�to generowanie nowej mapy (System Chunkowy).");
            }
            else
            {
                Debug.LogError("<color=red>SaveManager:</color> Nie znaleziono WorldGenerator na scenie!");
            }
        }
        else
        {
            // Wczytywanie istniej�cej gry
            LoadGame(saveToLoad);
        }

        yield return null; // Jeszcze jedna klatka, by UnlockManager zd��y� si� zainicjalizowa�
        UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
        if (unlocker != null) unlocker.RefreshUnlocks();

        // Uruchom tutorial TYLKO dla nowej gry
        if (isNewGame)
        {
            yield return null; // Czekaj jeszcze jedną klatkę
            TutorialManager tutorialMgr = Object.FindAnyObjectByType<TutorialManager>();
            if (tutorialMgr != null)
            {
                tutorialMgr.StartTutorial();
            }
        }
    }

    // --- PUBLICZNE METODY DLA MAIN MENU ---

    public FileInfo[] GetAllSaveFiles()
    {
        DirectoryInfo info = new DirectoryInfo(baseSaveFolder);
        return info.GetFiles("*.json")
                   .OrderByDescending(f => f.LastWriteTime)
                   .ToArray();
    }

    public void SetSaveToLoad(string fileName)
    {
        saveToLoad = fileName;
    }

    public string GetFullSavePath(string fileName)
    {
        return Path.Combine(baseSaveFolder, fileName);
    }

    // --- LOGIKA ZAPISU ---

    public void SaveGame()
    {
        Debug.Log("button pressed");

        SaveGameWithName("latest_save");
    }

    public void SaveGameWithName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        if (!fileName.EndsWith(".json")) fileName += ".json";
        string fullPath = Path.Combine(baseSaveFolder, fileName);

        GameSaveData saveData = new GameSaveData();

        // 1. Inwentarz
        if (PlayerInventory.Instance != null)
        {
            saveData.inventoryData = PlayerInventory.Instance.GetSaveData();
        }

        if (TechTreeManager.Instance != null)
        {
            saveData.researchedTechnologyIds = TechTreeManager.Instance.GetResearchedIds();
        }

        // 2. Encje (Budynki, Przedmioty, Z�o�a)
        SavableEntity[] entities = Object.FindObjectsByType<SavableEntity>(FindObjectsSortMode.None);
        foreach (SavableEntity entity in entities)
        {
            EntityData eData = entity.Save();

            // DODAJ T� LINI�: Pobiera JSON z Pieca/Assemblera
            eData.jsonComponentData = entity.GetSerializedData();

            saveData.entityDatas.Add(eData);
        }

        WorldGenerator worldGen = Object.FindFirstObjectByType<WorldGenerator>();
        if (worldGen != null)
        {
            saveData.seedX = worldGen.seedX;
            saveData.seedY = worldGen.seedY;
            saveData.generatedResourceChunks = worldGen.GetGeneratedResourceChunksData();
        }

        saveData.totalPlayTimeSeconds = totalPlayTimeSeconds;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(fullPath, json);

        Debug.Log($"<color=green>ZAPISANO:</color> {fullPath}");
    }

    // --- LOGIKA WCZYTYWANIA ---

    public void LoadGame(string fileName)
    {
        string fullPath = Path.Combine(baseSaveFolder, fileName);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("Plik zapisu nie istnieje: " + fullPath);
            return;
        }

        string json = File.ReadAllText(fullPath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        WorldGenerator worldGen = Object.FindFirstObjectByType<WorldGenerator>();
        if (worldGen != null)
        {
            worldGen.LoadSeed(data.seedX, data.seedY);
            worldGen.LoadGeneratedResourceChunksData(data.generatedResourceChunks);
        }

        totalPlayTimeSeconds = Mathf.Max(0f, data.totalPlayTimeSeconds);

        // 1. Wczytaj Ekwipunek
        if (PlayerInventory.Instance != null && data.inventoryData != null)
        {
            PlayerInventory.Instance.LoadFromSave(data.inventoryData);
        }

        // Szukamy WSZYSTKICH manager�w na scenie, nawet tych nieaktywnych (true)
        TechTreeManager tree = Object.FindObjectsByType<TechTreeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

        if (tree != null)
        {
            Debug.Log($"<color=green>[SaveManager]</color> Znaleziono drzewko! Przekazuj� {data.researchedTechnologyIds.Count} ID.");
            tree.LoadFromSave(data.researchedTechnologyIds);
        }
        else
        {
            // Je�li to wyskoczy, to znaczy, �e w hierarchii sceny GameScene w og�le nie ma skryptu TechTreeManager
            Debug.LogError("<color=red>[SaveManager] KRYTYCZNY B��D:</color> Skrypt TechTreeManager nie zosta� znaleziony nigdzie na scenie!");
        }

        // 2. Wyczy�� scen�
        SavableEntity[] existing = Object.FindObjectsByType<SavableEntity>(FindObjectsSortMode.None);
        foreach (var e in existing) Destroy(e.gameObject);

        // 3. Spawnowanie obiekt�w
        foreach (EntityData entityData in data.entityDatas)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + entityData.prefabName);

            if (prefab != null)
            {
                Vector3 pos = new Vector3(entityData.worldPosition[0], entityData.worldPosition[1], entityData.worldPosition[2]);

                // U�ywamy rotacji z zapisu, je�li jest dost�pna (wa�ne dla ta�moci�g�w)
                Quaternion rot = Quaternion.identity;
                if (entityData.worldRotation != null && entityData.worldRotation.Length == 3)
                {
                    rot = Quaternion.Euler(entityData.worldRotation[0], entityData.worldRotation[1], entityData.worldRotation[2]);
                }

                GameObject newObj = Instantiate(prefab, pos, rot);

                newObj.layer = entityData.layer;

                SavableEntity savable = newObj.GetComponent<SavableEntity>();
                if (savable != null)
                {
                    savable.uniqueID = entityData.uniqueID;

                    // --- KLUCZOWA KOLEJNO�� DLA BUDYNK�W ---
                    GridObject gridObj = newObj.GetComponent<GridObject>();
                    if (gridObj != null)
                    {
                        Vector2Int gridPos = new Vector2Int(entityData.gridPosition[0], entityData.gridPosition[1]);
                        gridObj.Initialize(gridPos); // Najpierw ustawiamy go na siatce
                    }

                    // Na ko�cu wczytujemy dane specyficzne (receptury, kierunki wyj��, ruch przedmiot�w)
                    // Robimy to po Initialize, �eby dane z zapisu mia�y "ostatnie s�owo"
                    savable.LoadComponentData(entityData.jsonComponentData);
                }
            }
            else
            {
                Debug.LogWarning($"Nie znaleziono prefaba o nazwie: {entityData.prefabName}");
            }
        }

        CameraController camController = Object.FindFirstObjectByType<CameraController>();
        if (camController != null)
        {
            camController.ForceRefreshChunks();
        }

        if (worldGen != null && (data.generatedResourceChunks == null || data.generatedResourceChunks.Count == 0))
        {
            worldGen.RebuildGeneratedResourceChunksFromSceneDeposits(true);
        }

        saveToLoad = "";
        Debug.Log("<color=blue>WCZYTYWANIE ZAKO�CZONE!</color>");
    }

    public string GetFormattedTotalPlayTime()
    {
        return FormatSeconds(totalPlayTimeSeconds);
    }

    public static string FormatSeconds(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;
        return $"{hours:00}:{minutes:00}:{secs:00}";
    }
}