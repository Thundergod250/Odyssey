using UnityEngine;
using UnityEngine.Events;

public class Interactables : MonoBehaviour
{
    public UnityEvent EvtOnInteract;

    public void Interact()
    {
        EvtOnInteract?.Invoke();
        Debug.Log($"Interacted with {gameObject.name}");
    }
}