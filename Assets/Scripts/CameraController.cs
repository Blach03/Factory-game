using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float baseMoveSpeed = 15f;
    private float minX = -1180f;
    private float maxX = 1180f;
    private float minY = -1530f;
    private float maxY = 1530f;

    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

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

        currentPosition.x = Mathf.Clamp(currentPosition.x, minX, maxX);
        currentPosition.y = Mathf.Clamp(currentPosition.y, minY, maxY);

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