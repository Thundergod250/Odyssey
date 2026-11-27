using UnityEngine;
using UnityEngine.UI;

public class UIBuilding : UIPanel
{
    [SerializeField] private Button buildButton;
    [SerializeField] private BuildPlacementController placementController;
    [SerializeField] private UIController uiController;

    private void Awake()
    {
        if (uiController == null && GameManager.Instance != null)
            uiController = GameManager.Instance.UIController;
    }

    private void OnEnable()
    {
        if (buildButton != null)
            buildButton.onClick.AddListener(HandleBuildButtonClicked);
    }

    private void OnDisable()
    {
        if (buildButton != null)
            buildButton.onClick.RemoveListener(HandleBuildButtonClicked);
    }

    private void HandleBuildButtonClicked()
    {
        if (placementController != null)
            placementController.StartPlacement();
        else
            Debug.LogWarning("[UIBuilding] BuildPlacementController reference is missing.", this);

        if (uiController != null)
            uiController.CloseAllUI(this);
        else if (GameManager.Instance != null && GameManager.Instance.UIController != null)
            GameManager.Instance.UIController.CloseAllUI(this);
    }
}