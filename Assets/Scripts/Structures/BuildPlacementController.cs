using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildPlacementController : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject greenGhostPrefab;
    [SerializeField] private GameObject redGhostPrefab;
    [SerializeField] private GameObject structurePrefab;

    [Header("Raycast & Collision")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool includeTriggers = false;
    [SerializeField] private float verticalOffset = 0f;
    [Tooltip("Lifts the bottom of the overlap check slightly above ground so flat floor contact is ignored while wall/side collisions are caught.")]
    [SerializeField] private float groundClearance = 0.05f;

    [Header("Slope & Ledge / Overhang Check")]
    [SerializeField] private float maxSlopeAngle = 15f;
    [SerializeField] private bool checkCornersForOverhang = true;
    [SerializeField] private float maxCornerHeightDiff = 0.25f;
    [SerializeField] private float cornerProbeHeight = 1.0f;
    [SerializeField] private float cornerProbeDistance = 2.0f;

    [Header("Placement Rules")]
    [SerializeField] private bool exitModeAfterPlace = false;

    private GameObject _activeGhost;
    private bool _isPlacing;
    private bool _canPlace;
    private Vector3 _currentPoint;
    private Vector3 _checkCenter;
    private Vector3 _checkHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    private Quaternion _checkRotation = Quaternion.identity;

    private Material _greenMaterial;
    private Material _redMaterial;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        CacheGhostMaterials();
    }

    private void CacheGhostMaterials()
    {
        if (greenGhostPrefab != null)
        {
            Renderer r = greenGhostPrefab.GetComponentInChildren<Renderer>();
            if (r != null) _greenMaterial = r.sharedMaterial;
        }

        if (redGhostPrefab != null)
        {
            Renderer r = redGhostPrefab.GetComponentInChildren<Renderer>();
            if (r != null) _redMaterial = r.sharedMaterial;
        }
    }

    public void StartPlacement()
    {
        StopPlacement();
        CacheGhostMaterials();
        _isPlacing = true;

        GameObject prefabToSpawn = greenGhostPrefab != null ? greenGhostPrefab : redGhostPrefab;
        if (prefabToSpawn != null)
        {
            _activeGhost = Instantiate(prefabToSpawn);
            DisableGhostColliders(_activeGhost);
        }
    }

    public void StopPlacement()
    {
        _isPlacing = false;
        _canPlace = false;

        if (_activeGhost != null)
        {
            Destroy(_activeGhost);
            _activeGhost = null;
        }
    }

    private void Update()
    {
        if (!_isPlacing || targetCamera == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetGhostVisible(false);
            _canPlace = false;
            return;
        }

        Vector2 pointerPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        Ray ray = targetCamera.ScreenPointToRay(pointerPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
        {
            SetGhostVisible(true);
            _currentPoint = hit.point + Vector3.up * verticalOffset;

            if (_activeGhost != null)
            {
                _activeGhost.transform.position = _currentPoint;
                _checkRotation = _activeGhost.transform.rotation;
                UpdateCheckBoxFromGhost();
            }
            else
            {
                _checkRotation = Quaternion.identity;
                _checkCenter = _currentPoint + Vector3.up * 0.5f;
                _checkHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
            }

            // 1. Check ground slope at hit point
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            bool isFlatGround = slopeAngle <= maxSlopeAngle;

            // 2. Check corners so placement doesn't hang over cliff ledges or clip steps
            bool hasSolidFootprint = !checkCornersForOverhang || IsFootprintSupported(hit.point.y);

            // 3. Check side/body overlap against obstacles (lifted slightly by groundClearance)
            Vector3 liftedCenter = _checkCenter + Vector3.up * groundClearance;
            Vector3 liftedExtents = new Vector3(
                _checkHalfExtents.x,
                Mathf.Max(0.01f, _checkHalfExtents.y - groundClearance * 0.5f),
                _checkHalfExtents.z
            );

            QueryTriggerInteraction triggerMode = includeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            bool isBlocked = Physics.CheckBox(
                liftedCenter,
                liftedExtents,
                _checkRotation,
                obstacleMask,
                triggerMode
            );

            _canPlace = isFlatGround && hasSolidFootprint && !isBlocked;
            UpdateGhostVisual(_canPlace);

            bool leftClicked = Mouse.current != null
                ? Mouse.current.leftButton.wasPressedThisFrame
                : Input.GetMouseButtonDown(0);

            if (leftClicked && _canPlace)
            {
                PlaceStructure(_currentPoint);
            }
        }
        else
        {
            SetGhostVisible(false);
            _canPlace = false;
        }
    }

    private bool IsFootprintSupported(float centerGroundY)
    {
        float hx = _checkHalfExtents.x;
        float hz = _checkHalfExtents.z;

        Vector3[] localCorners =
        {
            new Vector3(-hx, 0f, -hz),
            new Vector3( hx, 0f, -hz),
            new Vector3(-hx, 0f,  hz),
            new Vector3( hx, 0f,  hz)
        };

        foreach (Vector3 localCorner in localCorners)
        {
            Vector3 worldCorner = _checkRotation * localCorner + new Vector3(_checkCenter.x, centerGroundY, _checkCenter.z);
            Vector3 rayOrigin = worldCorner + Vector3.up * cornerProbeHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit cornerHit, cornerProbeHeight + cornerProbeDistance, groundMask))
                return false;

            if (Vector3.Angle(cornerHit.normal, Vector3.up) > maxSlopeAngle)
                return false;

            if (Mathf.Abs(cornerHit.point.y - centerGroundY) > maxCornerHeightDiff)
                return false;
        }

        return true;
    }

    private void UpdateCheckBoxFromGhost()
    {
        if (_activeGhost == null)
            return;

        BoxCollider box = _activeGhost.GetComponentInChildren<BoxCollider>();
        if (box != null)
        {
            Transform boxTransform = box.transform;
            _checkCenter = boxTransform.TransformPoint(box.center);
            _checkRotation = boxTransform.rotation;

            Vector3 scaledSize = Vector3.Scale(box.size, boxTransform.lossyScale);
            _checkHalfExtents = new Vector3(
                Mathf.Abs(scaledSize.x) * 0.5f,
                Mathf.Abs(scaledSize.y) * 0.5f,
                Mathf.Abs(scaledSize.z) * 0.5f
            );
            return;
        }

        Collider col = _activeGhost.GetComponentInChildren<Collider>();
        if (col != null)
        {
            _checkCenter = col.bounds.center;
            _checkHalfExtents = col.bounds.extents;
            _checkRotation = Quaternion.identity;
        }
    }

    private void PlaceStructure(Vector3 position)
    {
        if (structurePrefab != null)
            Instantiate(structurePrefab, position, _activeGhost != null ? _activeGhost.transform.rotation : Quaternion.identity);

        if (exitModeAfterPlace)
            StopPlacement();
    }

    private void UpdateGhostVisual(bool isValid)
    {
        if (_activeGhost == null)
            return;

        Material targetMat = isValid ? _greenMaterial : _redMaterial;
        if (targetMat == null)
            return;

        Renderer currentRenderer = _activeGhost.GetComponentInChildren<Renderer>();
        if (currentRenderer != null && currentRenderer.sharedMaterial != targetMat)
        {
            currentRenderer.sharedMaterial = targetMat;
        }
    }

    private void SetGhostVisible(bool visible)
    {
        if (_activeGhost != null && _activeGhost.activeSelf != visible)
            _activeGhost.SetActive(visible);
    }

    private void DisableGhostColliders(GameObject ghost)
    {
        foreach (Collider col in ghost.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _canPlace ? Color.green : Color.red;
        Gizmos.matrix = Matrix4x4.TRS(_checkCenter, _checkRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, _checkHalfExtents * 2f);
    }
}