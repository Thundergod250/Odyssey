using System;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Persistent HUD")]
    public UIMainHUD UImainHUD;

    private readonly Dictionary<Type, UIPanel> _panels = new();
    private UIPanel _currentPanel;

    private void Awake()
    {
        foreach (UIPanel panel in GetComponentsInChildren<UIPanel>(true))
        {
            Type type = panel.GetType();
            if (!_panels.TryAdd(type, panel))
                Debug.LogWarning($"[UIController] Duplicate UIPanel of type {type.Name} found.");
        }
    }

    private void Start() => CloseAllUI();

    public void CloseAllUI()
    {
        foreach (UIPanel panel in _panels.Values)
            panel.Close();

        _currentPanel = null;

        if (UImainHUD != null)
            UImainHUD.gameObject.SetActive(true);
    }

    public T OpenUI<T>() where T : UIPanel
    {
        if (!_panels.TryGetValue(typeof(T), out UIPanel targetPanel))
        {
            Debug.LogError($"[UIController] Panel of type {typeof(T).Name} not found under UIController!");
            return null;
        }

        if (UImainHUD != null)
            UImainHUD.gameObject.SetActive(false);

        if (_currentPanel != null && _currentPanel != targetPanel)
            _currentPanel.Close();

        targetPanel.Open();
        _currentPanel = targetPanel;

        return (T)targetPanel;
    }
    
    public void ToggleUI<T>() where T : UIPanel
    {
        if (_currentPanel is T)
            CloseAllUI();
        else
            OpenUI<T>();
    }
}