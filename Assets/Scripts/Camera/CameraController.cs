using UnityEngine;
using System.IO;

public class CameraController : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float movementSpeed = 0.08f;
    [SerializeField] private float zoomSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float dragSpeed = 0.0014f;

    [Header("Camera Settings")]
    [SerializeField] private float distance = 100.0f;
    [SerializeField] private bool orthographic = true;

    [Header("Constraints")]
    [SerializeField] private float minZoom = 1.0f;
    [SerializeField] private float maxZoom = 10.0f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = -20f;

    // Current values (no lerping)
    private Vector3 origin = Vector3.zero;
    private float zoom = 5.0f;
    private float yawRotation = 0.0f;
    private float pitchRotation = -45.0f;

    // Mouse drag state
    private bool draggingLeft = false;
    private bool draggingRight = false;
    private Vector2 lastMousePos = Vector2.zero;

    private Camera cam;
    private LevelManager levelManager;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
        }
        
        if (orthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = zoom;
        }

        levelManager = FindObjectOfType<LevelManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ExportCameraToCurrentLevel();
        }

        HandleInput();
        CheckOriginMovement();
        CheckYawRotation();
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        // Mouse button input
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            draggingRight = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            draggingRight = false;
        }

        if (Input.GetMouseButtonDown(0)) // Left click
        {
            draggingLeft = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            draggingLeft = false;
        }

        // Scroll wheel zoom
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            zoom -= scroll * zoomSpeed * 0.2f;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        // Mouse motion - drag directly affects origin/rotation
        if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 delta = currentMousePos - lastMousePos;
            lastMousePos = currentMousePos;

            if (draggingRight)
            {
                // Drag directly affects origin
                float yawRad = yawRotation * Mathf.Deg2Rad;
                float deltaX = delta.x * dragSpeed * zoom;
                float deltaZ = delta.y * dragSpeed * zoom;
                
                origin.x += deltaX * Mathf.Cos(yawRad) + deltaZ * Mathf.Sin(yawRad);
                origin.z += deltaZ * Mathf.Cos(yawRad) - deltaX * Mathf.Sin(yawRad);
            }
            else if (draggingLeft)
            {
                yawRotation += delta.x * rotationSpeed;
                pitchRotation += delta.y * rotationSpeed;
                pitchRotation = Mathf.Clamp(pitchRotation, minPitch, maxPitch);
            }
        }
    }

    private void CheckOriginMovement()
    {
        // Arrow keys move origin directly
        if (Input.GetKey(KeyCode.LeftArrow))
            origin.x -= movementSpeed;
        if (Input.GetKey(KeyCode.RightArrow))
            origin.x += movementSpeed;
        if (Input.GetKey(KeyCode.UpArrow))
            origin.z -= movementSpeed;
        if (Input.GetKey(KeyCode.DownArrow))
            origin.z += movementSpeed;
    }

    private void CheckYawRotation()
    {
        if (Input.GetKey(KeyCode.Comma))
        {
            yawRotation -= rotationSpeed * Time.deltaTime * 100f;
        }
        if (Input.GetKey(KeyCode.Period))
        {
            yawRotation += rotationSpeed * Time.deltaTime * 100f;
        }
    }

    private void UpdateCameraPosition()
    {
        // Calculate rotational offset using spherical coordinates
        float yawRad = yawRotation * Mathf.Deg2Rad;
        float pitchRad = pitchRotation * Mathf.Deg2Rad;
        
        Vector3 rotationalOffset = new Vector3(
            distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            distance * Mathf.Sin(-pitchRad),
            distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        );

        // Update camera size for orthographic
        if (orthographic && cam.orthographic)
        {
            cam.orthographicSize = zoom;
        }

        // Set position and look at target
        transform.position = origin + rotationalOffset;
        transform.LookAt(origin);
    }

    // ============= Public API =============

    public CameraData ExportCameraData()
    {
        return new CameraData
        {
            origin = origin,
            offset = Vector3.zero,
            zoom = zoom,
            yawRotation = yawRotation,
            pitchRotation = pitchRotation
        };
    }

    public void SetCameraData(CameraData data)
    {
        origin = data.origin;
        zoom = data.zoom;
        yawRotation = data.yawRotation;
        pitchRotation = data.pitchRotation;
    }

    public float GetYawRotation()
    {
        return yawRotation;
    }

    private void ExportCameraToCurrentLevel()
    {
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        if (levelManager == null || levelManager.levelFiles == null || levelManager.levelFiles.Length == 0)
        {
            Debug.LogError("Cannot export camera: LevelManager or levelFiles not found!");
            return;
        }

        int currentLevel = levelManager.currentLevelIndex;
        if (currentLevel < 0 || currentLevel >= levelManager.levelFiles.Length)
        {
            Debug.LogError($"Cannot export camera: Invalid level index {currentLevel}");
            return;
        }

        TextAsset levelAsset = levelManager.levelFiles[currentLevel];
        
#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(levelAsset);
        
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Cannot export camera: Could not find asset path!");
            return;
        }

        string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
        
        try
        {
            // Read current JSON
            string json = File.ReadAllText(fullPath);
            
            // Parse and update
            LevelJsonData levelData = JsonUtility.FromJson<LevelJsonData>(json);
            if (levelData == null || levelData.tiles == null)
            {
                Debug.LogError("Failed to parse level JSON!");
                return;
            }

            // Update camera settings
            levelData.camera = new SerializableCameraSettings
            {
                origin = new SerializableVec3 { x = origin.x, y = origin.y, z = origin.z },
                offset = new SerializableVec3 { x = 0, y = 0, z = 0 },
                zoom = zoom,
                yawRotation = yawRotation,
                pitchRotation = pitchRotation
            };

            // Write back
            string updatedJson = JsonUtility.ToJson(levelData, true);
            File.WriteAllText(fullPath, updatedJson);
            
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log($"✓ Camera settings exported to {levelAsset.name}:\n" +
                     $"  Origin: ({FormatFloat(origin.x)}, {FormatFloat(origin.y)}, {FormatFloat(origin.z)})\n" +
                     $"  Zoom: {FormatFloat(zoom)}\n" +
                     $"  Yaw: {FormatFloat(yawRotation)}\n" +
                     $"  Pitch: {FormatFloat(pitchRotation)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to export camera settings: {e.Message}");
        }
#else
        Debug.LogWarning("Camera export only works in Unity Editor!");
#endif
    }

    private string FormatFloat(float value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Serialization classes for JSON export
    [System.Serializable]
    private class SerializableVec3
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    private class SerializableCameraSettings
    {
        public SerializableVec3 origin;
        public SerializableVec3 offset;
        public float zoom;
        public float yawRotation;
        public float pitchRotation;
    }

    [System.Serializable]
    private class LevelTilePosition
    {
        public int x;
        public int y;
        public int z;
    }

    [System.Serializable]
    private class LevelTileEntry
    {
        public LevelTilePosition position;
        public int tile_id;
    }

    [System.Serializable]
    private class LevelJsonData
    {
        public LevelTileEntry[] tiles;
        public SerializableCameraSettings camera;
    }
}

[System.Serializable]
public class CameraData
{
    public Vector3 origin;
    public Vector3 offset;
    public float zoom;
    public float yawRotation;
    public float pitchRotation;
}
