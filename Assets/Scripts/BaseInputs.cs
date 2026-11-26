using UnityEngine;

public class BaseInputs : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    
    private void Start()
    {
        if (inputManager != null)
        {
            inputManager.OnBuild.AddListener(HandleBuildInput);
        }
        else
        {
            Debug.LogWarning("[BaseInputs] GameManager or InputManager reference is missing.");
        }
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnBuild.RemoveListener(HandleBuildInput);
        }
    }

    private void HandleBuildInput()
    {
        Debug.Log("[BaseInputs] 'B' (Build) pressed!");
    }
}