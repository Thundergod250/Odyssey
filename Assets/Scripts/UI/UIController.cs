using UnityEngine;

public class UIController : MonoBehaviour
{
    public UIMainHUD UImainHUD;
    public UIBuilding UIBuilding;
    public UiFloodPump UIFloodPump;

    private void Start()
    {
        CloseAllUI();
    }
    
    public void CloseAllUI()
    {
        if (UIBuilding != null) UIBuilding.gameObject.SetActive(false);
        if (UIFloodPump != null) UIFloodPump.gameObject.SetActive(false);

        if (UImainHUD != null) UImainHUD.gameObject.SetActive(true);
    }

    public void OpenBuildingUI()
    {
        if (UImainHUD != null) UImainHUD.gameObject.SetActive(false);
        if (UIFloodPump != null) UIFloodPump.gameObject.SetActive(false);

        if (UIBuilding != null) UIBuilding.gameObject.SetActive(true);
    }

    public void OpenFloodPumpUI()
    {
        if (UImainHUD != null) UImainHUD.gameObject.SetActive(false);
        if (UIBuilding != null) UIBuilding.gameObject.SetActive(false);

        if (UIFloodPump != null) UIFloodPump.gameObject.SetActive(true);
    }
}