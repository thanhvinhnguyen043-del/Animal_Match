using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    public RectTransform content;
    public Button levelButtonTemplate;
    public UIManager ui;
    private Button[] buttons;
    private ScrollRect scroll;

    private void Awake()
    {
        ConfigureScrolling();
    }

    private void ConfigureScrolling()
    {
        if (content == null) return;
        scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;
        scroll.content = content;
        if (scroll.viewport == null) scroll.viewport = content.parent as RectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.inertia = true;
        scroll.decelerationRate = .135f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45;

        // Clip rows outside the viewport and accept swipes even in gaps between buttons.
        var viewport = scroll.viewport;
        if (viewport != null)
        {
            if (!viewport.GetComponent<RectMask2D>() && !viewport.GetComponent<Mask>())
                viewport.gameObject.AddComponent<RectMask2D>();
            var surface = viewport.GetComponent<Graphic>();
            if (surface == null)
            {
                var image = viewport.gameObject.AddComponent<Image>();
                image.color = Color.clear;
                surface = image;
            }
            surface.raycastTarget = true;
        }
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void Start() { Refresh(); }

    public void Refresh()
    {
        if (scroll == null) ConfigureScrolling();
        var database = Resources.Load<LevelDatabase>("LevelDatabase");
        if (database == null || database.levels == null) return;
        int count = database.levels.Count;
        if (buttons == null || buttons.Length != count)
        {
            if (buttons != null)
                foreach (var old in buttons) if (old)
                {
                    old.gameObject.SetActive(false);
                    Destroy(old.gameObject);
                }
            buttons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var button = Instantiate(levelButtonTemplate, content);
                button.name = "Level_" + (i + 1);
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => ui.StartLevel(index));
                // A template is inactive; activate only after assigning its event.
                button.gameObject.SetActive(true);
                buttons[i] = button;
            }
        }
        int unlocked = LevelProgress.HighestUnlocked(count);
        for (int i = 0; i < count; i++)
        {
            buttons[i].interactable = i <= unlocked;
            var label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = "LEVEL " + (i + 1) + (i > unlocked ? "\nLOCKED" : "");
        }
        // Update the scrollable height immediately when the level count changes.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
