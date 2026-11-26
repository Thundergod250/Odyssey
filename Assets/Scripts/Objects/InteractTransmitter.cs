using UnityEngine;

public class InteractTransmitter : MonoBehaviour
{
    [SerializeField] private Structures structures;
    private Interactables interactables;

    private void Awake() => TryGetComponent(out interactables);

    private void OnEnable()
    {
        if (interactables != null && structures != null)
            interactables.EvtOnInteract.AddListener(structures.StructureDebug);
    }

    private void OnDisable()
    {
        if (interactables != null && structures != null)
            interactables.EvtOnInteract.RemoveListener(structures.StructureDebug);
    }
}