using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TechTreeManager : MonoBehaviour
{
    public static TechTreeManager Instance;
    public TextAsset jsonFile; // Przypisz plik JSON w inspektorze
    public GameObject techNodePrefab; // Prefab okienka technologii
    public RectTransform contentTransform; // Content ze Scroll View
    public GameObject linePrefab; // Prosty Image (czarny), który bêdzie lini¹

    private List<TechnologyNode> allNodes;
    private Dictionary<string, GameObject> spawnedNodes = new Dictionary<string, GameObject>();

    private HashSet<string> researchedIds = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Opcjonalnie: DontDestroyOnLoad(gameObject); // Jeœli chcesz, by drzewko ¿y³o miêdzy scenami
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        Debug.Log("<color=cyan>[TechTreeManager]</color> Start: Generowanie drzewka...");
        GenerateTree();

        if (pendingLoadIds != null)
        {
            Debug.Log($"<color=cyan>[TechTreeManager]</color> Start: Wczytujê dane z poczekalni ({pendingLoadIds.Count} ID).");
            researchedIds = new HashSet<string>(pendingLoadIds);
            pendingLoadIds = null;
        }

        RefreshAllNodes();

        // DODAJ TO NA KOÑCU STARTU:
        UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
        if (unlocker != null)
        {
            unlocker.RefreshUnlocks();
        }
    }

    void GenerateTree()
    {
        TechnologyTreeData data = JsonUtility.FromJson<TechnologyTreeData>(jsonFile.text);
        allNodes = data.technologies;

        CalculateDepths();
        CalculateXPositions();
        SpawnNodes();
        DrawConnections();
    }

    private void CalculateDepths()
    {
        foreach (var node in allNodes)
        {
            node.depth = GetMaxDepth(node);
        }
    }

    private int GetMaxDepth(TechnologyNode node)
    {
        if (node.requiredIds == null || node.requiredIds.Count == 0) return 0;

        int max = 0;
        foreach (string id in node.requiredIds)
        {
            var req = allNodes.Find(n => n.id == id);
            max = Mathf.Max(max, GetMaxDepth(req) + 1);
        }
        return max;
    }

    private void CalculateXPositions()
    {
        // 1. Ustawiamy pozycjê X dla technologii startowych (depth 0) na œrodku (0)
        var rootNodes = allNodes.Where(n => n.depth == 0).ToList();
        float rootSpacing = 300f;
        float startX = -(rootNodes.Count - 1) * rootSpacing / 2f;
        for (int i = 0; i < rootNodes.Count; i++)
        {
            rootNodes[i].xPos = startX + (i * rootSpacing);
        }

        // 2. Przechodzimy przez kolejne poziomy (od 1 w górê)
        int maxDepth = allNodes.Max(n => n.depth);
        for (int d = 1; d <= maxDepth; d++)
        {
            var nodesAtDepth = allNodes.Where(n => n.depth == d).ToList();

            foreach (var parentNode in allNodes.Where(n => n.depth == d - 1))
            {
                // ZnajdŸ dzieci tego konkretnego rodzica
                var children = allNodes.Where(n => n.requiredIds.Contains(parentNode.id) && n.depth == d).ToList();

                if (children.Count == 0) continue;

                // Rozsuñ dzieci symetrycznie pod rodzicem
                float childSpacing = 450f;
                float offset = -(children.Count - 1) * childSpacing / 2f;

                for (int i = 0; i < children.Count; i++)
                {
                    // Pozycja dziecka = Pozycja rodzica + offset
                    children[i].xPos = parentNode.xPos + offset + (i * childSpacing);
                }

                // Opcjonalne: Wyœrodkowanie dla technologii z wieloma rodzicami
                foreach (var node in allNodes.Where(n => n.requiredIds.Count > 1))
                {
                    float sumX = 0;
                    foreach (var reqId in node.requiredIds)
                    {
                        sumX += allNodes.Find(n => n.id == reqId).xPos;
                    }
                    node.xPos = sumX / node.requiredIds.Count;
                }
            }
        }
    }

    public bool IsResearched(string id) => researchedIds.Contains(id);

    public bool CanResearch(TechnologyNode node)
    {
        if (IsResearched(node.id)) return false;
        if (node.requiredIds == null || node.requiredIds.Count == 0) return true;

        foreach (string reqId in node.requiredIds)
        {
            if (!IsResearched(reqId)) return false;
        }
        return true;
    }

    public void ResearchTechnology(TechnologyNode node)
    {
        if (!researchedIds.Contains(node.id))
        {
            researchedIds.Add(node.id);
            RefreshAllNodes(); // Odœwie¿a kolory w drzewku
        }

        UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
        if (unlocker != null)
        {
            unlocker.RefreshUnlocks();
        }
    }

    public void RefreshAllNodes()
    {
        foreach (var kvp in spawnedNodes)
        {
            kvp.Value.GetComponent<TechNodeUI>().RefreshColor();
        }
    }

    [Header("Position Settings")]
    public Vector2 startPosition = new Vector2(0, -100); // Punkt startowy (x, y)
    public float ySpacing = 200f; // Odstêp miêdzy poziomami

    private void SpawnNodes()
    {
        float minHeight = 0; // Przechowuje najni¿szy punkt (najbardziej ujemny Y)

        foreach (var node in allNodes)
        {
            // Dodajemy startPosition do wyliczonych koordynatów
            Vector2 pos = new Vector2(node.xPos + startPosition.x, (-node.depth * ySpacing) + startPosition.y);

            GameObject go = Instantiate(techNodePrefab, contentTransform);
            RectTransform rt = go.GetComponent<RectTransform>();

            // Upewniamy siê, ¿e kotwice s¹ na œrodku u góry, aby pozycjonowanie by³o przewidywalne
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;


            TechNodeUI ui = go.GetComponent<TechNodeUI>();
            if (ui != null)
            {
                ui.Setup(node);
            }

            spawnedNodes.Add(node.id, go);

            if (pos.y < minHeight) minHeight = pos.y;
        }

        float finalHeight = Mathf.Abs(minHeight) + 200f;
        contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, finalHeight);
    }

    private void DrawConnections()
    {
        foreach (var node in allNodes)
        {
            foreach (string reqId in node.requiredIds)
            {
                if (spawnedNodes.ContainsKey(reqId))
                {
                    CreateLine(spawnedNodes[reqId].GetComponent<RectTransform>(),
                               spawnedNodes[node.id].GetComponent<RectTransform>());
                }
            }
        }
    }

    private void CreateLine(RectTransform start, RectTransform end)
    {
        GameObject lineObj = Instantiate(linePrefab, contentTransform);
        lineObj.transform.SetAsFirstSibling(); // Linie pod okienkami
        RectTransform rect = lineObj.GetComponent<RectTransform>();

        // KLUCZ: Ustawiamy te same kotwice i pivot co w ikonach
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Obliczamy wektor kierunku i odleg³oœæ
        Vector2 dir = (end.anchoredPosition - start.anchoredPosition).normalized;
        float distance = Vector2.Distance(start.anchoredPosition, end.anchoredPosition);

        // Ustawiamy pozycjê dok³adnie w po³owie drogi miêdzy startem a koñcem
        rect.anchoredPosition = start.anchoredPosition + (end.anchoredPosition - start.anchoredPosition) / 2;

        // Szerokoœæ linii to odleg³oœæ miêdzy punktami, wysokoœæ to gruboœæ linii (np. 5px)
        rect.sizeDelta = new Vector2(distance, 5f);

        // Obracamy liniê w stronê celu
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
    }

    public List<string> GetResearchedIds()
    {
        return researchedIds.ToList();
    }

    // Ta metoda przyjmuje listê z wczytanego pliku i aktualizuje stan drzewka
    private List<string> pendingLoadIds = null;

    public void LoadFromSave(List<string> loadedIds)
    {
        if (loadedIds != null && loadedIds.Count > 0)
        {
            Debug.Log($"<color=cyan>[TechTreeManager]</color> Otrzymano z zapisu {loadedIds.Count} technologii: {string.Join(", ", loadedIds)}");
            researchedIds = new HashSet<string>(loadedIds);

            if (spawnedNodes != null && spawnedNodes.Count > 0)
            {
                Debug.Log("<color=cyan>[TechTreeManager]</color> Drzewko ju¿ istnieje, odœwie¿am kolory.");
                RefreshAllNodes();
            }
            else
            {
                Debug.Log("<color=cyan>[TechTreeManager]</color> Drzewko jeszcze nie istnieje, zapisujê ID do poczekalni.");
                pendingLoadIds = loadedIds;
            }

            // DODAJ TO TUTAJ:
            // Wymuszamy odœwie¿enie przycisków zaraz po za³adowaniu listy ID
            UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
            if (unlocker != null)
            {
                unlocker.RefreshUnlocks();
            }
        }
        else
        {
            Debug.LogWarning("<color=cyan>[TechTreeManager]</color> Otrzymana lista technologii jest pusta lub null!");
        }
    }

    public float GetConveyorSpeedMultiplier()
    {
        float multiplier = 1.0f;

        // Sprawdzamy konkretne technologie ulepszaj¹ce prêdkoœæ
        if (IsResearched("t9")) multiplier += 0.5f;
        if (IsResearched("t19")) multiplier += 0.5f;
        if (IsResearched("t20")) multiplier += 0.5f;
        if (IsResearched("t26")) multiplier += 0.5f;
        return multiplier;
    }

    // Wewn¹trz klasy TechTreeManager
    public float GetProductionSpeedMultiplier()
    {
        float multiplier = 1.0f;
        if (IsResearched("t16")) multiplier += 0.20f;
        return multiplier;
    }

    public float GetProductivityChance()
    {
        float chance = 0f;
        if (IsResearched("t21")) chance += 0.10f;
        if (IsResearched("t27")) chance += 0.10f;
        return chance;
    }
}