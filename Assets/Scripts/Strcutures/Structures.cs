using UnityEngine;

public abstract class Structures : MonoBehaviour
{
    [Header("Submersion Settings")]
    [SerializeField] protected bool ignoreSubmersion = false;

    [Header("Destruction Settings")]
    [Tooltip("The Buildable/Debris prefab spawned when this structure is destroyed.")]
    [SerializeField] protected GameObject BuildablePrefab;
    
    private Collider structureCollider;
    protected bool isSubmerged = false;

    protected virtual void Awake()
    {
        if (structureCollider == null)
            structureCollider = GetComponent<Collider>();
    }
}