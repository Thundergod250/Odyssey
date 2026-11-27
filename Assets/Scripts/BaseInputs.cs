using UnityEngine;

public class BaseInputs : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private UIController uiController;

    private void Start()
    {
        if (inputManager != null)
        {
            inputManager.OnBuild.AddListener(OnBuildButtonPressed);
            inputManager.OnEscape.AddListener(OnEscapeButtonPressed);
        }
        else
        {
            Debug.LogWarning("[BaseInputs] InputManager reference is missing.");
        }
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnBuild.RemoveListener(OnBuildButtonPressed);
            inputManager.OnEscape.RemoveListener(OnEscapeButtonPressed);
        }
    }

    private void OnBuildButtonPressed()
    {
        if (uiController != null)
            uiController.ToggleUI<UIBuilding>(this);
        else
            Debug.LogWarning("[BaseInputs] UIController reference is missing.", this);
    }

    private void OnEscapeButtonPressed()
    {
        if (uiController != null)
            uiController.CloseAllUI(this);
        else
            Debug.LogWarning("[BaseInputs] UIController reference is missing.", this);
    }
}