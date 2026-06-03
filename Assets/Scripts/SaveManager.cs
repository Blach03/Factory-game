using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SaveManager : MonoBehaviour
{
    [System.Serializable]
    private class LegacyTechSaveProbe
    {
        public List<string> researchedTechnologyIds;
        public List<string> researchedIds;
        public List<string> researchedTechIds;
        public List<string> unlockedTechnologyIds;
        public List<string> unlockedTechIds;
    }

    public static SaveManager Instance { get; private set; }

    public event System.Action<bool, string> GameLoadCompleted;

    [HideInInspector]
    public string saveToLoad = ""; // Nazwa pliku przekazana z Main Menu

    [Header("Loading Screen (Optional)")]
    [SerializeField] private GameObject loadingScreenRoot;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private TextMeshProUGUI loadingStatusText;

    [Header("Auto Save")]
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveIntervalMinutes = 15f;

    private float totalPlayTimeSeconds = 0f;
    private bool isLoadInProgress = false;
    private Coroutine autoSaveCoroutine;

    private static bool hasPendingLoadRequest = false;
    private static string pendingSaveToLoad = "";

    private string baseSaveFolder;

    public string SaveFolderPath => baseSaveFolder;

    public float TotalPlayTimeSeconds => totalPlayTimeSeconds;
    public bool IsLoadInProgress => isLoadInProgress;

    public static SaveManager EnsureInstanceExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SaveManager existingInScene = Object.FindFirstObjectByType<SaveManager>(FindObjectsInactive.Include);
        if (existingInScene != null)
        {
            Instance = existingInScene;
            return Instance;
        }

        GameObject go = new GameObject("SaveManager_AutoCreated");
        return go.AddComponent<SaveManager>();
    }

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

            // Fallback: jeśli request ładowania przyszedł przed utworzeniem instancji,
            // zastosuj go od razu po starcie SaveManagera.
            ConsumePendingLoadRequest();
        }
        else
        {
            Destroy(gameObject);
        }

        if (autoSaveCoroutine == null)
        {
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
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
        bool loadSucceeded = true;
        string failReason = string.Empty;

        ShowLoadingScreen(true);

        if (!isLoadInProgress)
        {
            isLoadInProgress = true;
            SetLoadingProgress(0.5f, "Preparing data...");
        }

        // Czekamy jedn� klatk�, aby upewni� si�, �e Awake() we wszystkich 
        // nowych obiektach (np. PlayerInventory) zosta� wykonany.
        yield return null;

        bool isNewGame = string.IsNullOrEmpty(saveToLoad);

        if (isNewGame)
        {
            totalPlayTimeSeconds = 0f;
            HandCraftingManager.RestoreQueueSnapshot(null);
            SetLoadingProgress(0.6f, "Generating world...");

            // --- TUTAJ ZMIANA: Szukamy nowego WorldGeneratora zamiast ResourceGenerator ---
            WorldGenerator generator = Object.FindFirstObjectByType<WorldGenerator>();
            if (generator != null)
            {
                generator.InitializeWorld();
                Debug.Log("<color=green>SaveManager:</color> Rozpocz�to generowanie nowej mapy (System Chunkowy).");
                SetLoadingProgress(0.8f, "World generated.");
            }
            else
            {
                loadSucceeded = false;
                failReason = "WorldGenerator was not found in the scene.";
                Debug.LogError("<color=red>SaveManager:</color> Nie znaleziono WorldGenerator na scenie!");
            }
        }
        else
        {
            // Wczytywanie istniej�cej gry
            SetLoadingProgress(0.6f, "Loading save file...");
            loadSucceeded = LoadGame(saveToLoad);

            if (!loadSucceeded)
            {
                failReason = "There was a problem while loading the save file.";
            }

            SetLoadingProgress(0.85f, "Finalizing load...");
        }

        yield return null; // Jeszcze jedna klatka, by UnlockManager zd��y� si� zainicjalizowa�
        UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
        if (unlocker != null) unlocker.RefreshUnlocks();

        if (UIManager.Instance != null && UIManager.Instance.inventoryUI != null)
        {
            UIManager.Instance.inventoryUI.SetupInventory();
        }

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

        SetLoadingProgress(1f, loadSucceeded ? "Ready!" : "Load incomplete.");

        GameLoadCompleted?.Invoke(loadSucceeded, failReason);

        // Krótkie opóźnienie, aby gracz zdążył zobaczyć 100%.
        yield return null;

        isLoadInProgress = false;
        ShowLoadingScreen(false);
    }

    public void StartNewGameFromMenu()
    {
        if (isLoadInProgress)
        {
            return;
        }

        QueueNewGameRequest();
        saveToLoad = "";
        StartCoroutine(LoadGameSceneAsync());
    }

    public void StartLoadGameFromMenu(string fileName)
    {
        if (isLoadInProgress)
        {
            return;
        }

        QueueLoadGameRequest(fileName);
        SetSaveToLoad(fileName);
        StartCoroutine(LoadGameSceneAsync());
    }

    public static void QueueNewGameRequest()
    {
        hasPendingLoadRequest = true;
        pendingSaveToLoad = "";
    }

    public static void QueueLoadGameRequest(string fileName)
    {
        hasPendingLoadRequest = true;
        pendingSaveToLoad = fileName ?? "";
    }

    private void ConsumePendingLoadRequest()
    {
        if (!hasPendingLoadRequest)
        {
            return;
        }

        saveToLoad = pendingSaveToLoad;
        hasPendingLoadRequest = false;
        pendingSaveToLoad = "";
    }

    private System.Collections.IEnumerator LoadGameSceneAsync()
    {
        isLoadInProgress = true;
        ShowLoadingScreen(true);
        SetLoadingProgress(0f, "Loading scene...");

        AsyncOperation sceneLoad = SceneManager.LoadSceneAsync("GameScene");
        if (sceneLoad == null)
        {
            isLoadInProgress = false;
            ShowLoadingScreen(false);
            Debug.LogError("Nie udało się rozpocząć asynchronicznego ładowania sceny GameScene.");
            yield break;
        }

        sceneLoad.allowSceneActivation = false;

        while (sceneLoad.progress < 0.9f)
        {
            float normalized = Mathf.Clamp01(sceneLoad.progress / 0.9f);
            SetLoadingProgress(normalized * 0.5f, "Loading scene...");
            yield return null;
        }

        SetLoadingProgress(0.5f, "Scene ready, starting...");
        sceneLoad.allowSceneActivation = true;

        while (!sceneLoad.isDone)
        {
            yield return null;
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

        saveData.researchedTechnologyIds = GetResearchedTechnologyIdsForSave();
        saveData.handCraftingQueueData = HandCraftingManager.GetQueueSnapshotForSave();

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

    public bool LoadGame(string fileName)
    {
        string fullPath = Path.Combine(baseSaveFolder, fileName);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("Plik zapisu nie istnieje: " + fullPath);
            return false;
        }

        string json = File.ReadAllText(fullPath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null)
        {
            Debug.LogError("Nie udało się zdeserializować pliku zapisu.");
            return false;
        }

        List<string> researchedIdsToLoad = ResolveResearchedTechnologyIds(data, json);
        TechTreeManager.RestoreResearchedIdsSnapshot(researchedIdsToLoad);

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

        HandCraftingManager.RestoreQueueSnapshot(data.handCraftingQueueData);

        // Szukamy WSZYSTKICH manager�w na scenie, nawet tych nieaktywnych (true)
        TechTreeManager tree = Object.FindObjectsByType<TechTreeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

        if (tree != null)
        {
            Debug.Log($"<color=green>[SaveManager]</color> Znaleziono drzewko! Przekazuj� {researchedIdsToLoad.Count} ID.");
            tree.LoadFromSave(researchedIdsToLoad);
        }
        else
        {
            // Drzewko może być nieaktywne podczas ładowania; stan i tak trafi do cache statycznego.
            Debug.LogWarning("<color=yellow>[SaveManager]</color> TechTreeManager nie został znaleziony podczas load. Stan research zapisano do cache i zostanie zastosowany po inicjalizacji managera.");
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
        return true;
    }

    private void ShowLoadingScreen(bool visible)
    {
        if (loadingScreenRoot != null)
        {
            loadingScreenRoot.SetActive(visible);
        }
    }

    private void SetLoadingProgress(float value01, string status)
    {
        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = Mathf.Clamp01(value01);
        }

        if (loadingStatusText != null && !string.IsNullOrEmpty(status))
        {
            loadingStatusText.text = status;
        }
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

    private List<string> GetResearchedTechnologyIdsForSave()
    {
        TechTreeManager tree = TechTreeManager.Instance;
        if (tree == null)
        {
            tree = Object.FindObjectsByType<TechTreeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        }

        if (tree != null)
        {
            return tree.GetResearchedIds();
        }

        return TechTreeManager.GetResearchedIdsSnapshot();
    }

    private List<string> ResolveResearchedTechnologyIds(GameSaveData data, string rawJson)
    {
        if (data != null && data.researchedTechnologyIds != null && data.researchedTechnologyIds.Count > 0)
        {
            return data.researchedTechnologyIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        }

        LegacyTechSaveProbe legacy = JsonUtility.FromJson<LegacyTechSaveProbe>(rawJson);
        if (legacy == null)
        {
            return new List<string>();
        }

        List<string>[] candidates =
        {
            legacy.researchedTechnologyIds,
            legacy.researchedIds,
            legacy.researchedTechIds,
            legacy.unlockedTechnologyIds,
            legacy.unlockedTechIds
        };

        foreach (List<string> candidate in candidates)
        {
            if (candidate != null && candidate.Count > 0)
            {
                return candidate.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            }
        }

        return new List<string>();
    }

    private System.Collections.IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            float intervalSeconds = Mathf.Max(60f, autoSaveIntervalMinutes * 60f);
            yield return new WaitForSecondsRealtime(intervalSeconds);

            if (!autoSaveEnabled) continue;
            if (isLoadInProgress) continue;
            if (SceneManager.GetActiveScene().name != "GameScene") continue;

            SaveGameWithName("latest_save");
            Debug.Log("[AutoSave] Saved to latest_save.");
        }
    }
}