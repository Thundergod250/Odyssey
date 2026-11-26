using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIController : MonoBehaviour
{
    [Header("Persistent HUD")]
    public UIMainHUD UImainHUD;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private readonly Dictionary<Type, UIPanel> _panels = new Dictionary<Type, UIPanel>();
    private UIPanel _currentPanel;

    private void Awake()
    {
        foreach (UIPanel panel in GetComponentsInChildren<UIPanel>(true))
        {
            Type type = panel.GetType();
            if (!_panels.TryAdd(type, panel))
                UIDebugLogger.Warn(enableDebugLogs, $"Duplicate UIPanel of type {type.Name} found.", panel);
        }
    }

    private void Start() => CloseAllUI(this);

    private void SetCameraMovementActive(bool isActive)
    {
        if (GameManager.Instance != null && GameManager.Instance.CameraMovement != null)
            GameManager.Instance.CameraMovement.SetMovementEnabled(isActive);
    }

    public void CloseAllUI(Object source = null, [CallerMemberName] string caller = "")
    {
        foreach (UIPanel panel in _panels.Values)
            panel.Close();

        _currentPanel = null;

        if (UImainHUD != null)
            UImainHUD.gameObject.SetActive(true);

        SetCameraMovementActive(true);
        UIDebugLogger.Log(enableDebugLogs, source, this, caller, "ClosedAllUI (HUD active)");
    }

    public T OpenUI<T>(Object source = null, [CallerMemberName] string caller = "") where T : UIPanel
    {
        if (!_panels.TryGetValue(typeof(T), out UIPanel targetPanel))
        {
            UIDebugLogger.Error(enableDebugLogs, source, this, caller, $"tried to OpenUI<{typeof(T).Name}>, but panel was not found!");
            return null;
        }

        if (UImainHUD != null)
            UImainHUD.gameObject.SetActive(false);

        if (_currentPanel != null && _currentPanel != targetPanel)
            _currentPanel.Close();

        targetPanel.Open();
        _currentPanel = targetPanel;
        SetCameraMovementActive(false);

        UIDebugLogger.Log(enableDebugLogs, source, this, caller, $"OpenedUI ({typeof(T).Name})");
        return (T)targetPanel;
    }

    public void ToggleUI<T>(Object source = null, [CallerMemberName] string caller = "") where T : UIPanel
    {
        bool willClose = _currentPanel is T;
        UIDebugLogger.Log(enableDebugLogs, source, this, caller, $"ToggledUI ({typeof(T).Name}) to {(willClose ? "Closing to HUD" : "Opening")}");

        if (willClose)
            CloseAllUI(source, caller);
        else
            OpenUI<T>(source, caller);
    }
}