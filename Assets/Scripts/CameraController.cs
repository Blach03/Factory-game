using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float baseMoveSpeed = 15f;

    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    [Header("Infinite Generation")]
    public WorldGenerator worldGenerator;
    public int viewDistanceChunks = 2; // Ile chunków widaæ przed kamer¹
    private Vector2Int lastChunkCoords = new Vector2Int(-999, -999);

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController wymaga komponentu Camera na tym samym obiekcie.");
            enabled = false;
        }
        if (minZoom <= 0f)
        {
            minZoom = 1f;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        CheckForNewChunks();

        if (Input.GetKeyDown(KeyCode.H))
        {
            GoToHome();
        }
    }

    public void GoToHome()
    {
        // Przenosimy kamerê do 0,0 (zachowuj¹c jej Z, zazwyczaj -10)
        transform.position = new Vector3(0, 0, transform.position.z);

        // Opcjonalnie: resetujemy zoom do domyœlnego (np. 15)
        if (cam != null) cam.orthographicSize = 15f;

        Debug.Log("Kamera powróci³a do punktu 0,0");
    }

    void CheckForNewChunks()
    {
        if (worldGenerator == null) return;

        // Obliczamy w jakim chunku jest obecnie œrodek kamery
        // Zak³adamy, ¿e chunkSize w WorldGenerator to 100
        int chunkSize = worldGenerator.chunkSize;
        int currentChunkX = Mathf.RoundToInt(transform.position.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(transform.position.y / chunkSize);

        Vector2Int currentCoords = new Vector2Int(currentChunkX, currentChunkY);

        // Sprawdzamy tylko jeœli kamera przesunê³a siê do innego chunka
        if (currentCoords != lastChunkCoords)
        {
            lastChunkCoords = currentCoords;

            // Pêtla sprawdzaj¹ca chunki w promieniu viewDistance
            for (int x = -viewDistanceChunks; x <= viewDistanceChunks; x++)
            {
                for (int y = -viewDistanceChunks; y <= viewDistanceChunks; y++)
                {
                    ChunkCoords targetCoords = new ChunkCoords(currentChunkX + x, currentChunkY + y);
                    worldGenerator.TryGenerateChunk(targetCoords);
                }
            }
        }
    }

    void HandleMovement()
    {
        float currentZoomLevel = cam.orthographicSize;

        float speedMultiplier = currentZoomLevel / minZoom;
        float currentMoveSpeed = baseMoveSpeed * speedMultiplier;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0) * currentMoveSpeed * Time.deltaTime;
        transform.position += movement;

        Vector3 currentPosition = transform.position;

        transform.position = currentPosition;
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            float newSize = cam.orthographicSize - scrollInput * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}