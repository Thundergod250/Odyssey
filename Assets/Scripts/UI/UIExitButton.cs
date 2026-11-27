using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIExitButton : MonoBehaviour
{
    private Button exitButton;

    private void Awake()
    {
        if (exitButton == null)
            exitButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(HandleExitClicked);
        else
            Debug.LogWarning("[UIExitButton] Button reference is missing.", this);
    }

    private void OnDisable()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(HandleExitClicked);
    }

    private void HandleExitClicked()
    {
        if (GameManager.Instance != null && GameManager.Instance.UIController != null)
            GameManager.Instance.UIController.CloseAllUI(this);
        else
            Debug.LogWarning("[UIExitButton] GameManager.Instance or UIController is missing.", this);
    }
}