using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global References")]
    public ObjectPooling objectPooling;
    public InputManager InputManager;
    public CursorManager CursorManager;
    public ClickInteractor ClickInteractor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}