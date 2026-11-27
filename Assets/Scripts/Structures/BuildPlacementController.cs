using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildPlacementController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject greenGhostPrefab;
    [SerializeField] private GameObject redGhostPrefab;
    [SerializeField] private GameObject structurePrefab;

    [Header("Raycast & Masks")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float verticalOffset = 0f;

    [Header("Ground, Ledge & Wall Check")]
    [SerializeField] private float maxSlopeAngle = 15f;
    [SerializeField] private float maxCornerHeightDiff = 0.25f;
    [SerializeField] private float bottomProbeOffset = 0.05f;
    [SerializeField] private float wallCheckLift = 0.3f;
    [SerializeField] private float wallCheckPadding = 0.02f;
    [SerializeField] private bool exitModeAfterPlace = false;

    private GameObject _ghost;
    private bool _isPlacing;
    private bool _canPlace;
    private Vector3 _center;
    private Vector3 _halfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    private Quaternion _rot = Quaternion.identity;
    private Material _greenMat;
    private Material _redMat;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (greenGhostPrefab != null) _greenMat = greenGhostPrefab.GetComponentInChildren<Renderer>()?.sharedMaterial;
        if (redGhostPrefab != null) _redMat = redGhostPrefab.GetComponentInChildren<Renderer>()?.sharedMaterial;
    }

    public void StartPlacement()
    {
        StopPlacement();
        _isPlacing = true;
        var prefab = greenGhostPrefab != null ? greenGhostPrefab : redGhostPrefab;
        if (prefab == null) return;

        _ghost = Instantiate(prefab);
        foreach (var col in _ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    public void StopPlacement()
    {
        _isPlacing = false;
        _canPlace = false;
        if (_ghost != null) Destroy(_ghost);
    }

    private void Update()
    {
        if (!_isPlacing || targetCamera == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetVisible(false);
            _canPlace = false;
            return;
        }

        Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        if (!Physics.Raycast(targetCamera.ScreenPointToRay(pointer), out RaycastHit hit, 500f, groundMask))
        {
            SetVisible(false);
            _canPlace = false;
            return;
        }

        SetVisible(true);
        Vector3 pos = hit.point + Vector3.up * verticalOffset;
        if (_ghost != null) _ghost.transform.position = pos;

        SyncBoxFromGhost();

        bool hasObstacleInside = Physics.CheckBox(_center, _halfExtents, _rot, obstacleMask, QueryTriggerInteraction.Ignore);
        bool hasSolidBottom = HasSolidGroundBottom(hit);
        bool hitsWall = HitsWallAboveFloor(hit.point.y);

        _canPlace = !hasObstacleInside && hasSolidBottom && !hitsWall;
        ApplyGhostMaterial(_canPlace);

        bool clicked = Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
        if (clicked && _canPlace && structurePrefab != null)
        {
            Instantiate(structurePrefab, pos, _rot);
            if (exitModeAfterPlace) StopPlacement();
        }
    }

    private void SyncBoxFromGhost()
    {
        if (_ghost == null) return;
        BoxCollider box = _ghost.GetComponentInChildren<BoxCollider>();
        if (box != null)
        {
            _center = box.transform.TransformPoint(box.center);
            _rot = box.transform.rotation;
            Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);
            _halfExtents = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * 0.5f;
        }
    }

    private bool HasSolidGroundBottom(RaycastHit centerHit)
    {
        if (Vector3.Angle(centerHit.normal, Vector3.up) > maxSlopeAngle) return false;

        float refY = centerHit.point.y;
        float rayStartOffset = Mathf.Max(0.25f, bottomProbeOffset);
        float rayDistance = rayStartOffset + Mathf.Max(0.25f, bottomProbeOffset * 2f);

        Vector3[] corners =
        {
            new Vector3(-_halfExtents.x, 0f, -_halfExtents.z),
            new Vector3( _halfExtents.x, 0f, -_halfExtents.z),
            new Vector3(-_halfExtents.x, 0f,  _halfExtents.z),
            new Vector3( _halfExtents.x, 0f,  _halfExtents.z)
        };

        foreach (var c in corners)
        {
            Vector3 worldCornerXZ = _rot * c + new Vector3(_center.x, 0f, _center.z);
            Vector3 origin = new Vector3(worldCornerXZ.x, refY + rayStartOffset, worldCornerXZ.z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundMask))
                return false;

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
                return false;

            if (Mathf.Abs(hit.point.y - refY) > maxCornerHeightDiff)
                return false;
        }

        return true;
    }

    private bool HitsWallAboveFloor(float floorY)
    {
        float liftedBottomY = floorY + wallCheckLift;
        float boxTopY = _center.y + _halfExtents.y;
        if (boxTopY <= liftedBottomY) return false;

        float liftedHalfY = (boxTopY - liftedBottomY) * 0.5f;
        float liftedCenterY = liftedBottomY + liftedHalfY;

        Vector3 liftedCenter = new Vector3(_center.x, liftedCenterY, _center.z);
        Vector3 liftedExtents = new Vector3(
            _halfExtents.x + wallCheckPadding,
            liftedHalfY,
            _halfExtents.z + wallCheckPadding
        );

        return Physics.CheckBox(liftedCenter, liftedExtents, _rot, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyGhostMaterial(bool valid)
    {
        if (_ghost == null) return;
        Material target = valid ? _greenMat : _redMat;
        Renderer r = _ghost.GetComponentInChildren<Renderer>();
        if (target != null && r != null && r.sharedMaterial != target) r.sharedMaterial = target;
    }

    private void SetVisible(bool visible)
    {
        if (_ghost != null && _ghost.activeSelf != visible) _ghost.SetActive(visible);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _canPlace ? Color.green : Color.red;
        Gizmos.matrix = Matrix4x4.TRS(_center, _rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, _halfExtents * 2f);
    }
}