using System;
using UnityEngine;

public class Structures : MonoBehaviour
{
    [SerializeField] private Interactables interactable;

    private void Awake()
    {
        interactable.EvtOnInteract.AddListener(StructureDebug);
    }

    public void StructureDebug()
    {
        GameManager.Instance.UIController.OpenUI<UIBuilding>();
    }
}
