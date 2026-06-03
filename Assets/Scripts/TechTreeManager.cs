using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class TechTreeManager : MonoBehaviour
{
    public static TechTreeManager Instance;
    private static HashSet<string> cachedResearchedIds = new HashSet<string>();
    private static bool hasCachedResearchState;

    private static TextAsset cachedJsonFile;
    private static GameObject cachedTechNodePrefab;
    private static GameObject cachedLinePrefab;
    private static Vector2 cachedStartPosition;
    private static float cachedYSpacing;
    private static bool hasCachedLayoutSettings;
    public TextAsset jsonFile; // Przypisz plik JSON w inspektorze
    public GameObject techNodePrefab; // Prefab okienka technologii
    public RectTransform contentTransform; // Content ze Scroll View
    public GameObject linePrefab; // Prosty Image (czarny), kt�ry b�dzie lini�

    private List<TechnologyNode> allNodes;
    private Dictionary<string, GameObject> spawnedNodes = new Dictionary<string, GameObject>();

    private HashSet<string> researchedIds = new HashSet<string>();
    private bool treeGenerated = false;

    void Awake()
    {
        CacheRuntimeConfig();
        ApplyCachedResearchStateIfAvailable();

        if (Instance == null)
        {
            Instance = this;
            // Opcjonalnie: DontDestroyOnLoad(gameObject); // Je�li chcesz, by drzewko �y�o mi�dzy scenami
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        CacheRuntimeConfig();
        ApplyCachedResearchStateIfAvailable();

        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CacheRuntimeConfig()
    {
        if (jsonFile != null)
        {
            cachedJsonFile = jsonFile;
        }

        if (techNodePrefab != null)
        {
            cachedTechNodePrefab = techNodePrefab;
        }

        if (linePrefab != null)
        {
            cachedLinePrefab = linePrefab;
        }

        // Preserve inspector-tuned layout so runtime self-heal does not reset it to defaults.
        if (!hasCachedLayoutSettings || startPosition != new Vector2(0f, -100f) || !Mathf.Approximately(ySpacing, 200f))
        {
            cachedStartPosition = startPosition;
            cachedYSpacing = ySpacing;
            hasCachedLayoutSettings = true;
        }
    }

    public static TechTreeManager EnsureInstanceFromPanel(GameObject technologyPanel)
    {
        if (Instance != null)
        {
            return Instance;
        }

        TechTreeManager mgr = Object.FindFirstObjectByType<TechTreeManager>(FindObjectsInactive.Include);
        if (mgr != null)
        {
            Instance = mgr;
            return mgr;
        }

        if (technologyPanel == null)
        {
            return null;
        }

        mgr = technologyPanel.GetComponentInChildren<TechTreeManager>(true);
        if (mgr != null)
        {
            Instance = mgr;
            return mgr;
        }

        Transform viewport = technologyPanel.transform.Find("ViewPort");
        if (viewport == null)
        {
            viewport = technologyPanel.transform.Find("Viewport");
        }

        if (viewport == null)
        {
            Debug.LogError("<color=red>[TechTreeManager]</color> EnsureInstanceFromPanel: ViewPort/Viewport not found.");
            return null;
        }

        Transform content = viewport.Find("Content");
        if (content == null)
        {
            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.SetParent(viewport, false);
            ApplyContentRectDefaults(contentRT);

            RectTransform viewportRect = viewport as RectTransform ?? viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                contentRT.sizeDelta = new Vector2(viewportRect.rect.width, viewportRect.rect.height);
            }
            else
            {
                contentRT.sizeDelta = new Vector2(1920f, 1080f);
            }

            content = contentRT;
            Debug.LogWarning("<color=yellow>[TechTreeManager]</color> EnsureInstanceFromPanel: recreated missing Content.");
        }

        mgr = content.GetComponent<TechTreeManager>();
        if (mgr == null)
        {
            mgr = content.gameObject.AddComponent<TechTreeManager>();
            Debug.LogWarning("<color=yellow>[TechTreeManager]</color> EnsureInstanceFromPanel: recreated missing TechTreeManager component on Content.");
        }

        if (mgr.contentTransform == null)
        {
            mgr.contentTransform = content as RectTransform ?? content.GetComponent<RectTransform>();
        }

        if (mgr.jsonFile == null)
        {
            mgr.jsonFile = cachedJsonFile;
        }

        if (mgr.techNodePrefab == null)
        {
            mgr.techNodePrefab = cachedTechNodePrefab;
        }

        if (mgr.linePrefab == null)
        {
            mgr.linePrefab = cachedLinePrefab;
        }

        if (hasCachedLayoutSettings)
        {
            mgr.startPosition = cachedStartPosition;
            mgr.ySpacing = cachedYSpacing;
        }

        Instance = mgr;
        return mgr;
    }
    void Start()
    {
        EnsureTreeInitialized();

        if (pendingLoadIds != null)
        {
            researchedIds = new HashSet<string>(pendingLoadIds);
            pendingLoadIds = null;
        }

        CacheResearchState();

        RefreshAllNodes();

        // DODAJ TO NA KO�CU STARTU:
        UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
        if (unlocker != null)
        {
            unlocker.RefreshUnlocks();
        }
    }

    public void EnsureTreeInitialized()
    {
        if (jsonFile == null)
        {
            Debug.LogError("<color=red>[TechTreeManager]</color> EnsureTreeInitialized: jsonFile is null.");
            return;
        }

        if (!EnsureContentTransformReady())
        {
            Debug.LogError("<color=red>[TechTreeManager]</color> EnsureTreeInitialized: contentTransform is not ready.");
            return;
        }

        // Jeśli drzewko już istnieje i węzły są obecne, tylko odświeżamy kolory.
        if (treeGenerated && spawnedNodes != null && spawnedNodes.Count > 0)
        {
            RefreshAllNodes();
            return;
        }

        // Jeśli coś wyczyściło Content lub słownik, regenerujemy od zera.
        if (spawnedNodes == null)
        {
            spawnedNodes = new Dictionary<string, GameObject>();
        }

        foreach (Transform child in contentTransform)
        {
            if (child == transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
        spawnedNodes.Clear();

        GenerateTree();
        treeGenerated = true;
    }

    private bool EnsureContentTransformReady()
    {
        Transform viewport = transform.Find("ViewPort");
        if (viewport == null)
        {
            viewport = transform.Find("Viewport");
        }

        NormalizeContentReference(viewport);

        // Jeśli mamy referencję, ale ktoś odpiął Content od hierarchii ViewPort, napraw.
        if (contentTransform != null)
        {
            if (viewport != null && contentTransform.parent != viewport)
            {
                contentTransform.SetParent(viewport, false);
            }

            ApplyContentRectDefaults(contentTransform);

            RebindScrollRectContent();
            return true;
        }

        // Spróbuj znaleźć istniejący Content pod viewportem.
        if (viewport != null)
        {
            Transform content = viewport.Find("Content");
            if (content != null)
            {
                contentTransform = content as RectTransform ?? content.GetComponent<RectTransform>();
                if (contentTransform != null)
                {
                    ApplyContentRectDefaults(contentTransform);
                    RebindScrollRectContent();
                    return true;
                }
            }

            // Jeśli Content zniknął, twórz go automatycznie.
            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            RectTransform rt = contentGO.GetComponent<RectTransform>();
            rt.SetParent(viewport, false);
            ApplyContentRectDefaults(rt);

            RectTransform viewportRect = viewport as RectTransform ?? viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                rt.sizeDelta = new Vector2(viewportRect.rect.width, viewportRect.rect.height);
            }
            else
            {
                rt.sizeDelta = new Vector2(1920f, 1080f);
            }

            contentTransform = rt;
            Debug.LogWarning("<color=yellow>[TechTreeManager]</color> Content was missing. Created new ViewPort/Content at runtime.");
            RebindScrollRectContent();
            return true;
        }

        Debug.LogError("<color=red>[TechTreeManager]</color> Could not find ViewPort/Viewport under TechnologyMenu.");
        return false;
    }

    private void NormalizeContentReference(Transform viewport)
    {
        if (contentTransform == null)
        {
            return;
        }

        if (transform is RectTransform selfRect && selfRect.name == "Content" && transform.IsChildOf(contentTransform))
        {
            contentTransform = selfRect;
            return;
        }

        if (viewport != null && contentTransform == viewport)
        {
            Transform found = viewport.Find("Content");
            if (found is RectTransform foundRect)
            {
                contentTransform = foundRect;
                return;
            }
        }

        if (contentTransform.name != "Content")
        {
            Transform nested = contentTransform.Find("Content");
            if (nested is RectTransform nestedRect)
            {
                contentTransform = nestedRect;
            }
        }
    }

    private void RebindScrollRectContent()
    {
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>(true);
        if (scrollRect == null)
        {
            Transform parentTransform = transform.parent;
            if (parentTransform != null)
            {
                scrollRect = parentTransform.GetComponent<ScrollRect>();
                if (scrollRect == null)
                {
                    scrollRect = parentTransform.GetComponentInChildren<ScrollRect>(true);
                }
            }
        }

        if (scrollRect == null || contentTransform == null)
        {
            Debug.LogWarning("<color=yellow>[TechTreeManager]</color> RebindScrollRectContent: ScrollRect not found or contentTransform missing.");
            return;
        }

        if (scrollRect.content != contentTransform)
        {
            scrollRect.content = contentTransform;
        }

        Transform viewportTransform = contentTransform.parent;
        if (viewportTransform == null)
        {
            viewportTransform = scrollRect.transform.Find("ViewPort");
        }
        if (viewportTransform == null)
        {
            viewportTransform = scrollRect.transform.Find("Viewport");
        }

        RectTransform viewportRect = viewportTransform as RectTransform ?? viewportTransform?.GetComponent<RectTransform>();
        if (viewportRect != null && scrollRect.viewport != viewportRect)
        {
            scrollRect.viewport = viewportRect;
        }

        // Ensure drag + wheel scrolling behaves correctly after runtime self-heal.
        scrollRect.enabled = true;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 60f;
        scrollRect.decelerationRate = 0.12f;

        // Keep X locked to eliminate tiny horizontal jitter while dragging vertically.
        Vector2 lockedPos = contentTransform.anchoredPosition;
        if (!Mathf.Approximately(lockedPos.x, 0f))
        {
            lockedPos.x = 0f;
            contentTransform.anchoredPosition = lockedPos;
        }

        // Rebuild + reset internal bounds so drag/wheel starts working immediately.
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.Rebuild(CanvasUpdate.PostLayout);
    }

    private static void ApplyContentRectDefaults(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        // Keep content centered horizontally and top-aligned for predictable node placement.
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    void GenerateTree()
    {
        TechnologyTreeData data = JsonUtility.FromJson<TechnologyTreeData>(jsonFile.text);
        allNodes = data.technologies;

        if (allNodes == null)
        {
            Debug.LogError("<color=red>[TechTreeManager]</color> GenerateTree: allNodes is null after JSON parse.");
            allNodes = new List<TechnologyNode>();
        }

        CalculateDepths();
        CalculateXPositions();
        SpawnNodes();
        DrawConnections();
        RebindScrollRectContent();
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
        // 1. Ustawiamy pozycj� X dla technologii startowych (depth 0) na �rodku (0)
        var rootNodes = allNodes.Where(n => n.depth == 0).ToList();
        float rootSpacing = 300f;
        float startX = -(rootNodes.Count - 1) * rootSpacing / 2f;
        for (int i = 0; i < rootNodes.Count; i++)
        {
            rootNodes[i].xPos = startX + (i * rootSpacing);
        }

        // 2. Przechodzimy przez kolejne poziomy (od 1 w g�r�)
        int maxDepth = allNodes.Max(n => n.depth);
        for (int d = 1; d <= maxDepth; d++)
        {
            var nodesAtDepth = allNodes.Where(n => n.depth == d).ToList();

            foreach (var parentNode in allNodes.Where(n => n.depth == d - 1))
            {
                // Znajd� dzieci tego konkretnego rodzica
                var children = allNodes.Where(n => n.requiredIds.Contains(parentNode.id) && n.depth == d).ToList();

                if (children.Count == 0) continue;

                // Rozsu� dzieci symetrycznie pod rodzicem
                float childSpacing = 450f;
                float offset = -(children.Count - 1) * childSpacing / 2f;

                for (int i = 0; i < children.Count; i++)
                {
                    // Pozycja dziecka = Pozycja rodzica + offset
                    children[i].xPos = parentNode.xPos + offset + (i * childSpacing);
                }

                // Opcjonalne: Wy�rodkowanie dla technologii z wieloma rodzicami
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
            CacheResearchState();
            RefreshAllNodes(); // Od�wie�a kolory w drzewku
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
    public float ySpacing = 200f; // Odst�p mi�dzy poziomami

    private void SpawnNodes()
    {
        if (contentTransform == null)
        {
            Debug.LogError("<color=red>[TechTreeManager]</color> SpawnNodes: contentTransform is null.");
            return;
        }

        float minHeight = 0f; // Przechowuje najniższy punkt (najbardziej ujemny Y)
        float minX = 0f;
        float maxX = 0f;

        foreach (var node in allNodes)
        {
            // Dodajemy startPosition do wyliczonych koordynat�w
            Vector2 pos = new Vector2(node.xPos + startPosition.x, (-node.depth * ySpacing) + startPosition.y);

            GameObject go = Instantiate(techNodePrefab, contentTransform);
            RectTransform rt = go.GetComponent<RectTransform>();

            // Upewniamy si�, �e kotwice s� na �rodku u g�ry, aby pozycjonowanie by�o przewidywalne
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
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
        }

        RectTransform viewport = contentTransform.parent as RectTransform;
        float viewportWidth = viewport != null ? viewport.rect.width : 1920f;
        float viewportHeight = viewport != null ? viewport.rect.height : 1080f;

        float horizontalSpan = maxX - minX;
        float finalWidth = Mathf.Max(viewportWidth, horizontalSpan + 600f);
        float finalHeight = Mathf.Max(viewportHeight, Mathf.Abs(minHeight) + 300f);

        contentTransform.sizeDelta = new Vector2(finalWidth, finalHeight);
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

        // Obliczamy wektor kierunku i odleg�o��
        Vector2 dir = (end.anchoredPosition - start.anchoredPosition).normalized;
        float distance = Vector2.Distance(start.anchoredPosition, end.anchoredPosition);

        // Ustawiamy pozycj� dok�adnie w po�owie drogi mi�dzy startem a ko�cem
        rect.anchoredPosition = start.anchoredPosition + (end.anchoredPosition - start.anchoredPosition) / 2;

        // Szeroko�� linii to odleg�o�� mi�dzy punktami, wysoko�� to grubo�� linii (np. 5px)
        rect.sizeDelta = new Vector2(distance, 5f);

        // Obracamy lini� w stron� celu
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
    }

    public List<string> GetResearchedIds()
    {
        CacheResearchState();
        return researchedIds.ToList();
    }

    public static List<string> GetResearchedIdsSnapshot()
    {
        if (Instance != null)
        {
            return Instance.GetResearchedIds();
        }

        return hasCachedResearchState
            ? cachedResearchedIds.ToList()
            : new List<string>();
    }

    public static void RestoreResearchedIdsSnapshot(List<string> loadedIds)
    {
        HashSet<string> normalized = loadedIds != null
            ? new HashSet<string>(loadedIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            : new HashSet<string>();

        cachedResearchedIds = normalized;
        hasCachedResearchState = true;

        if (Instance != null)
        {
            Instance.ApplyLoadedResearchSet(normalized);
        }
    }

    // Ta metoda przyjmuje list� z wczytanego pliku i aktualizuje stan drzewka
    private List<string> pendingLoadIds = null;

    public void LoadFromSave(List<string> loadedIds)
    {
        if (loadedIds == null)
        {
            loadedIds = new List<string>();
        }

        if (loadedIds.Count > 0)
        {
            HashSet<string> normalized = new HashSet<string>(loadedIds.Where(id => !string.IsNullOrWhiteSpace(id)));
            ApplyLoadedResearchSet(normalized);

            if (spawnedNodes == null || spawnedNodes.Count == 0)
            {
                pendingLoadIds = normalized.ToList();
            }

            // DODAJ TO TUTAJ:
            // Wymuszamy od�wie�enie przycisk�w zaraz po za�adowaniu listy ID
            UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
            if (unlocker != null)
            {
                unlocker.RefreshUnlocks();
            }
        }
        else
        {
            researchedIds.Clear();
            pendingLoadIds = null;
            CacheResearchState();

            if (spawnedNodes != null && spawnedNodes.Count > 0)
            {
                RefreshAllNodes();
            }

            UnlockManager unlocker = Object.FindAnyObjectByType<UnlockManager>();
            if (unlocker != null)
            {
                unlocker.RefreshUnlocks();
            }
        }
    }

    private void ApplyLoadedResearchSet(HashSet<string> loadedSet)
    {
        researchedIds = loadedSet ?? new HashSet<string>();
        pendingLoadIds = null;
        CacheResearchState();

        if (spawnedNodes != null && spawnedNodes.Count > 0)
        {
            RefreshAllNodes();
        }
    }

    private void ApplyCachedResearchStateIfAvailable()
    {
        if (!hasCachedResearchState)
        {
            return;
        }

        researchedIds = new HashSet<string>(cachedResearchedIds);
    }

    private void CacheResearchState()
    {
        cachedResearchedIds = new HashSet<string>(researchedIds);
        hasCachedResearchState = true;
    }

    public float GetConveyorSpeedMultiplier()
    {
        float multiplier = 1.0f;

        // Sprawdzamy konkretne technologie ulepszaj�ce pr�dko��
        if (IsResearched("t9")) multiplier += 0.5f;
        if (IsResearched("t19")) multiplier += 0.5f;
        if (IsResearched("t20")) multiplier += 0.5f;
        if (IsResearched("t26")) multiplier += 0.5f;
        return multiplier;
    }

    // Wewn�trz klasy TechTreeManager
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