using UnityEngine;
using UnityEngine.UI;

public class TutorialUIHighlight : MonoBehaviour
{
    public static TutorialUIHighlight Instance { get; private set; }

    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private float borderThickness = 3f;
    [SerializeField] private float padding = 5f;

    private GameObject currentBorderRoot = null;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void HighlightButton(string buttonName)
    {
        ClearHighlight();

        RectTransform target = FindRectTransformByName(buttonName);
        if (target == null)
        {
            Debug.LogWarning($"[TutorialUIHighlight] Nie znaleziono obiektu: {buttonName}. Podaj nazwę lub ścieżkę np. MainCanvas/BottomBar/MinerButton.");
            return;
        }

        // Tworzymy kontener ramki jako dziecko wskazanego obiektu
        currentBorderRoot = new GameObject("TutorialBorderHighlight");
        currentBorderRoot.transform.SetParent(target, false);

        RectTransform rootRt = currentBorderRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(-padding, -padding);
        rootRt.offsetMax = new Vector2(padding, padding);

        // Ustawiamy sibling index na koniec, żeby ramka była na wierzchu
        currentBorderRoot.transform.SetAsLastSibling();

        // Tworzymy 4 paski: góra, dół, lewo, prawo
        CreateStrip(currentBorderRoot.transform, "Top",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -borderThickness), new Vector2(0f, 0f));

        CreateStrip(currentBorderRoot.transform, "Bottom",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, borderThickness));

        CreateStrip(currentBorderRoot.transform, "Left",
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(borderThickness, 0f));

        CreateStrip(currentBorderRoot.transform, "Right",
            new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-borderThickness, 0f), new Vector2(0f, 0f));

        Debug.Log($"[TutorialUIHighlight] Podświetlono: {buttonName}");
    }

    public void HighlightFirstExisting(params string[] buttonNames)
    {
        ClearHighlight();

        if (buttonNames == null || buttonNames.Length == 0)
        {
            Debug.LogWarning("[TutorialUIHighlight] Brak nazw do podświetlenia.");
            return;
        }

        for (int i = 0; i < buttonNames.Length; i++)
        {
            string name = buttonNames[i];
            RectTransform target = FindRectTransformByName(name);
            if (target == null) continue;

            currentBorderRoot = new GameObject("TutorialBorderHighlight");
            currentBorderRoot.transform.SetParent(target, false);

            RectTransform rootRt = currentBorderRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = new Vector2(-padding, -padding);
            rootRt.offsetMax = new Vector2(padding, padding);
            currentBorderRoot.transform.SetAsLastSibling();

            CreateStrip(currentBorderRoot.transform, "Top",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -borderThickness), new Vector2(0f, 0f));

            CreateStrip(currentBorderRoot.transform, "Bottom",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, borderThickness));

            CreateStrip(currentBorderRoot.transform, "Left",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(borderThickness, 0f));

            CreateStrip(currentBorderRoot.transform, "Right",
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-borderThickness, 0f), new Vector2(0f, 0f));

            Debug.Log($"[TutorialUIHighlight] Podświetlono: {name}");
            return;
        }

        Debug.LogWarning("[TutorialUIHighlight] Nie znaleziono żadnego przycisku z podanej listy nazw.");
    }

    private void CreateStrip(Transform parent, string stripName,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject strip = new GameObject(stripName);
        strip.transform.SetParent(parent, false);

        RectTransform rt = strip.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Image img = strip.AddComponent<Image>();
        img.color = highlightColor;
        img.raycastTarget = false;
    }

    public void ClearHighlight()
    {
        if (currentBorderRoot != null)
        {
            Destroy(currentBorderRoot);
            currentBorderRoot = null;
        }
    }

    private RectTransform FindRectTransformByName(string name)
    {
        // Próba ścieżki hierarchii (np. "MainCanvas/BottomBar/MinerButton")
        Transform t = FindTransformByPath(name);
        if (t != null) return t as RectTransform ?? t.GetComponent<RectTransform>();

        // Próba przez GameObject.Find (działa na aktywnych)
        GameObject go = GameObject.Find(name);
        if (go != null) return go.GetComponent<RectTransform>();

        // Szukamy po nazwie wśród wszystkich RectTransform, łącznie z nieaktywnymi
        RectTransform[] all = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RectTransform rt in all)
        {
            if (rt.gameObject.name == name)
                return rt;
        }

        return null;
    }

    private Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.Contains('/'))
            return null;

        string[] segments = path.Split('/');
        GameObject root = GameObject.Find(segments[0]);
        if (root == null) return null;

        Transform current = root.transform;
        for (int i = 1; i < segments.Length; i++)
        {
            current = current.Find(segments[i]);
            if (current == null) return null;
        }

        return current;
    }
}
