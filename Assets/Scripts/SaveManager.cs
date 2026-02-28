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

    private string baseSaveFolder;

    public string SaveFolderPath => baseSaveFolder;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Œcie¿ka do AppData
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

    // Obs³uga zdarzeñ ³adowania sceny
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
                    Debug.Log("Pomyœlnie podpiêto funkcjê Save pod przycisk.");
                }
            }

            // 2. Uruchomienie procedury wczytywania/generowania z opóŸnieniem
            StartCoroutine(LoadProcessRoutine());
        }
    }

    private System.Collections.IEnumerator LoadProcessRoutine()
    {
        // Czekamy jedn¹ klatkê, aby upewniæ siê, ¿e Awake() we wszystkich 
        // nowych obiektach (np. PlayerInventory) zosta³ wykonany.
        yield return null;

        if (string.IsNullOrEmpty(saveToLoad))
        {
            // --- TUTAJ ZMIANA: Szukamy nowego WorldGeneratora zamiast ResourceGenerator ---
            WorldGenerator generator = Object.FindFirstObjectByType<WorldGenerator>();
            if (generator != null)
            {
                generator.InitializeWorld();
                Debug.Log("<color=green>SaveManager:</color> Rozpoczêto generowanie nowej mapy (System Chunkowy).");
            }
            else
            {
                Debug.LogError("<color=red>SaveManager:</color> Nie znaleziono WorldGenerator na scenie!");
            }
        }
        else
        {
            // Wczytywanie istniej¹cej gry
            LoadGame(saveToLoad);
        }

        yield return null; // Jeszcze jedna klatka, by UnlockManager zd¹¿y³ siê zainicjalizowaæ
        UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
        if (unlocker != null) unlocker.RefreshUnlocks();
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

        // 2. Encje (Budynki, Przedmioty, Z³o¿a)
        SavableEntity[] entities = Object.FindObjectsByType<SavableEntity>(FindObjectsSortMode.None);
        foreach (SavableEntity entity in entities)
        {
            EntityData eData = entity.Save();

            // DODAJ TÊ LINIÊ: Pobiera JSON z Pieca/Assemblera
            eData.jsonComponentData = entity.GetSerializedData();

            saveData.entityDatas.Add(eData);
        }

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

        // 1. Wczytaj Ekwipunek
        if (PlayerInventory.Instance != null && data.inventoryData != null)
        {
            PlayerInventory.Instance.LoadFromSave(data.inventoryData);
        }

        // Szukamy WSZYSTKICH managerów na scenie, nawet tych nieaktywnych (true)
        TechTreeManager tree = Object.FindObjectsByType<TechTreeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

        if (tree != null)
        {
            Debug.Log($"<color=green>[SaveManager]</color> Znaleziono drzewko! Przekazujê {data.researchedTechnologyIds.Count} ID.");
            tree.LoadFromSave(data.researchedTechnologyIds);
        }
        else
        {
            // Jeœli to wyskoczy, to znaczy, ¿e w hierarchii sceny GameScene w ogóle nie ma skryptu TechTreeManager
            Debug.LogError("<color=red>[SaveManager] KRYTYCZNY B£¥D:</color> Skrypt TechTreeManager nie zosta³ znaleziony nigdzie na scenie!");
        }

        // 2. Wyczyœæ scenê
        SavableEntity[] existing = Object.FindObjectsByType<SavableEntity>(FindObjectsSortMode.None);
        foreach (var e in existing) Destroy(e.gameObject);

        // 3. Spawnowanie obiektów
        foreach (EntityData entityData in data.entityDatas)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + entityData.prefabName);

            if (prefab != null)
            {
                Vector3 pos = new Vector3(entityData.worldPosition[0], entityData.worldPosition[1], entityData.worldPosition[2]);

                // U¿ywamy rotacji z zapisu, jeœli jest dostêpna (wa¿ne dla taœmoci¹gów)
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

                    // --- KLUCZOWA KOLEJNOŒÆ DLA BUDYNKÓW ---
                    GridObject gridObj = newObj.GetComponent<GridObject>();
                    if (gridObj != null)
                    {
                        Vector2Int gridPos = new Vector2Int(entityData.gridPosition[0], entityData.gridPosition[1]);
                        gridObj.Initialize(gridPos); // Najpierw ustawiamy go na siatce
                    }

                    // Na koñcu wczytujemy dane specyficzne (receptury, kierunki wyjœæ, ruch przedmiotów)
                    // Robimy to po Initialize, ¿eby dane z zapisu mia³y "ostatnie s³owo"
                    savable.LoadComponentData(entityData.jsonComponentData);
                }
            }
            else
            {
                Debug.LogWarning($"Nie znaleziono prefaba o nazwie: {entityData.prefabName}");
            }
        }

        saveToLoad = "";
        Debug.Log("<color=blue>WCZYTYWANIE ZAKOÑCZONE!</color>");
    }
}