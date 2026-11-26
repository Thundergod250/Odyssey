using UnityEngine;

public class BaseInputs : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private UIController uiController;

    private void Start()
    {
        if (inputManager != null)
            inputManager.OnBuild.AddListener(HandleBuildInput);
        else
            Debug.LogWarning("[BaseInputs] InputManager reference is missing.");
    }

    private void OnDestroy()
    {
        if (inputManager != null)
            inputManager.OnBuild.RemoveListener(HandleBuildInput);
    }

    private void HandleBuildInput()
    {
        if (uiController != null)
            uiController.ToggleUI<UIBuilding>();
        else
            Debug.LogWarning("[BaseInputs] UIController reference is missing.");
    }
}