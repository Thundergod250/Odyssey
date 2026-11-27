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

            QueryTriggerInteraction triggerMode = includeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            bool isBlocked = Physics.CheckBox(
                _checkCenter,
                _checkHalfExtents,
                _checkRotation,
                obstacleMask,
                triggerMode
            );

            _canPlace = !isBlocked;
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