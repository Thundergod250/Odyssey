using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public static class UIDebugLogger
{
    private static string FormatOrigin(Object source, string caller)
    {
        if (source == null)
            return caller;

        string scriptName = source.GetType().Name;
        string objectName = source is Component comp ? comp.gameObject.name : source.name;
        return $"{scriptName} ({objectName}).{caller}";
    }

    private static bool CanLog(bool enabled) =>
        enabled && (Application.isEditor || Debug.isDebugBuild);

    [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
    public static void Log(bool enabled, Object source, Object fallbackContext, string caller, string message)
    {
        if (!CanLog(enabled)) return;
        Debug.Log($"[UIController] {FormatOrigin(source, caller)} -> {message}", source ?? fallbackContext);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
    public static void Warn(bool enabled, string message, Object context)
    {
        if (!CanLog(enabled)) return;
        Debug.LogWarning($"[UIController] {message}", context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
    public static void Error(bool enabled, Object source, Object fallbackContext, string caller, string message)
    {
        if (!CanLog(enabled)) return;
        Debug.LogError($"[UIController] {FormatOrigin(source, caller)} {message}", source ?? fallbackContext);
    }
}