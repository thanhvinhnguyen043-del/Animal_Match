using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class UIButtonClickSound : MonoBehaviour
{
    public EventReference clickSound;

    private Button button;
    private Button.ButtonClickedEvent originalClick;

    private void Awake()
    {
        button = GetComponent<Button>();
        // Play before navigation callbacks can unload this scene.
        originalClick = button.onClick;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() =>
        {
            PlayClick();
            originalClick.Invoke();
        });
    }

    private void PlayClick()
    {
        if (!clickSound.IsNull) RuntimeManager.PlayOneShot(clickSound);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick = originalClick;
    }
}
