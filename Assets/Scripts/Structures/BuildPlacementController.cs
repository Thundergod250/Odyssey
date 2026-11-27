using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildPlacementController : MonoBehaviour
{
    [Header("Prefabs & Visuals")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject structurePrefab;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Raycast & Layers")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Rules")]
    [SerializeField] private float groundOffset = 0.5f;
    [SerializeField] private float maxSlopeAngle = 15f;
    [SerializeField] private float ledgeTolerance = 0.25f;
    [SerializeField] private bool exitModeAfterPlace = false;

    private GameObject ghost;
    private Renderer ghostRenderer;
    private bool isPlacing;
    private bool canPlace;
    private Vector3 center;
    private Vector3 halfExtents = new(0.5f, 0.5f, 0.5f);
    private Quaternion rot = Quaternion.identity;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void StartPlacement()
    {
        StopPlacement();
        if (ghostPrefab == null) return;

        isPlacing = true;
        ghost = Instantiate(ghostPrefab);
        ghostRenderer = ghost.GetComponentInChildren<Renderer>();
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    public void StopPlacement()
    {
        isPlacing = false;
        canPlace = false;
        if (ghost != null) Destroy(ghost);
    }

    private void Update()
    {
        if (!isPlacing || ghost == null) return;

        if (!TryGetGroundHit(out RaycastHit hit))
        {
            ghost.SetActive(false);
            canPlace = false;
            return;
        }

        Vector3 spawnPos = hit.point + Vector3.up * groundOffset;
        ghost.SetActive(true);
        ghost.transform.position = spawnPos;
        SyncBounds();

        canPlace = CanPlaceAt(hit);
        if (ghostRenderer != null)
            ghostRenderer.sharedMaterial = canPlace ? validMaterial : invalidMaterial;

        if (canPlace && WasLeftClickPressed() && structurePrefab != null)
        {
            Instantiate(structurePrefab, spawnPos, rot);
            if (exitModeAfterPlace) StopPlacement();
        }
    }

    private bool TryGetGroundHit(out RaycastHit hit)
    {
        hit = default;
        if (targetCamera == null) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        return Physics.Raycast(targetCamera.ScreenPointToRay(pointer), out hit, 500f, groundMask);
    }

    private void SyncBounds()
    {
        BoxCollider box = ghost.GetComponentInChildren<BoxCollider>();
        if (box == null) return;

        center = box.transform.TransformPoint(box.center);
        rot = box.transform.rotation;
        Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);
        halfExtents = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * 0.5f;
    }

    private bool CanPlaceAt(RaycastHit hit)
    {
        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle) return false;

        // 1. Reject if an obstacle is inside the building volume
        if (Physics.CheckBox(center, halfExtents, rot, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        // 2. Reject if a cliff wall rises above the floor into the building's sides
        float wallBottomY = hit.point.y + ledgeTolerance;
        float boxTopY = center.y + halfExtents.y;
        if (boxTopY > wallBottomY)
        {
            float halfH = (boxTopY - wallBottomY) * 0.5f;
            Vector3 wallCenter = new Vector3(center.x, wallBottomY + halfH, center.z);
            Vector3 wallExtents = new Vector3(halfExtents.x + 0.02f, halfH, halfExtents.z + 0.02f);
            if (Physics.CheckBox(wallCenter, wallExtents, rot, groundMask, QueryTriggerInteraction.Ignore))
                return false;
        }

        // 3. Reject if any bottom corner hangs over a ledge or uneven step
        Vector3[] corners =
        {
            new(-halfExtents.x, 0f, -halfExtents.z),
            new( halfExtents.x, 0f, -halfExtents.z),
            new(-halfExtents.x, 0f,  halfExtents.z),
            new( halfExtents.x, 0f,  halfExtents.z)
        };

        foreach (var c in corners)
        {
            Vector3 xz = rot * c + new Vector3(center.x, 0f, center.z);
            if (!Physics.Raycast(new Vector3(xz.x, hit.point.y + 0.25f, xz.z), Vector3.down, out RaycastHit cHit, 0.5f, groundMask))
                return false;
            if (Vector3.Angle(cHit.normal, Vector3.up) > maxSlopeAngle) return false;
            if (Mathf.Abs(cHit.point.y - hit.point.y) > ledgeTolerance) return false;
        }

        return true;
    }

    private static bool WasLeftClickPressed()
    {
        if (GameManager.Instance != null && GameManager.Instance.InputManager != null)
            return GameManager.Instance.InputManager.WasQuickClickReleased;

        return Mouse.current != null
            ? Mouse.current.leftButton.wasReleasedThisFrame
            : Input.GetMouseButtonUp(0);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = canPlace ? Color.green : Color.red;
        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
    }
}