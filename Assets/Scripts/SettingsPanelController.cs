using UnityEngine;
using UnityEngine.UI;


public class SettingsPanelController : MonoBehaviour
{
    public Button closeButton;
    public Button restartButton;
    public Button homeButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(() => FindObjectOfType<UIManager>()?.CloseSettings());
        restartButton.onClick.AddListener(() => FindObjectOfType<UIManager>()?.RestartFromSettings());
        homeButton.onClick.AddListener(() => FindObjectOfType<UIManager>()?.ReturnHomeFromSettings());
    }
}
