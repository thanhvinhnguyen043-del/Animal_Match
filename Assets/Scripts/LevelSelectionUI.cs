using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    public RectTransform content;
    public Button levelButtonTemplate;
    public UIManager ui;
    public bool compactTiles;
    [SerializeField, HideInInspector] private Button[] buttons;
    private ScrollRect scroll;
    [SerializeField, HideInInspector] private StarRatingGraphic[] stars;
    private Sprite levelBadge;

    private void Awake()
    {
        levelBadge=Resources.Load<Sprite>("MenuIcons/LevelBadge");
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

    public void PrepareSceneView()
    {
        levelBadge=Resources.Load<Sprite>("MenuIcons/LevelBadge");
        ConfigureScrolling();RefreshView(true);
    }
    public void Refresh() => RefreshView(false);
    private void RefreshView(bool preview)
    {
        if (scroll == null) ConfigureScrolling();
        var database = Resources.Load<LevelDatabase>("LevelDatabase");
        if (database == null || database.levels == null) return;
        int count = database.levels.Count;
        if(compactTiles)
        {
            var footer=transform.Find("LevelPage");
            if(footer) footer.GetComponent<TextMeshProUGUI>().text=count+" LEVELS"+(count>10 ? " - SCROLL" : "");
        }
        if (buttons == null || buttons.Length != count)
        {
            if (buttons != null)
                foreach (var old in buttons) if (old)
                {
                    old.gameObject.SetActive(false);
                    if(Application.isPlaying) Destroy(old.gameObject);else DestroyImmediate(old.gameObject);
                }
            buttons = new Button[count];
            stars=new StarRatingGraphic[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var button = Instantiate(levelButtonTemplate, content);
                button.name = "Level_" + (i + 1);
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => ui.StartLevel(index));

                button.gameObject.SetActive(true);
                if(levelBadge)
                {
                    button.GetComponent<Image>().enabled=false;
                    var outline=button.GetComponent<Outline>();if(outline) outline.enabled=false;
                    var badge=new GameObject("LevelBadge",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
                    badge.transform.SetParent(button.transform,false);GameplayHudStyle.Place(badge.rectTransform,0,0,172,172);
                    badge.sprite=levelBadge;badge.preserveAspect=true;badge.transform.SetAsFirstSibling();button.targetGraphic=badge;
                }
                buttons[i] = button;
                stars[i]=button.GetComponentInChildren<StarRatingGraphic>(true);
            }
        }
        int unlocked = preview ? 0 : LevelProgress.HighestUnlocked(count);
        var career=preview ? null : EconomyStore.Inventory.career;
        for (int i = 0; i < count; i++)
        {
            int index=i;
            buttons[i].onClick=new Button.ButtonClickedEvent();
            if(!preview) buttons[i].onClick.AddListener(()=>ui.StartLevel(index));
            buttons[i].interactable = i <= unlocked;
            if(stars[i])
            {
                var record=career==null ? null : CareerStore.Record(career,CareerStore.LevelId(i,database));
                stars[i].SetStars(record==null ? 0 : record.stars);stars[i].gameObject.SetActive(i<=unlocked);
            }
            var label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            var lockIcon=buttons[i].transform.Find("LevelLockIcon");
            if(lockIcon) lockIcon.gameObject.SetActive(i>unlocked);
            if (label) label.text = "LEVEL " + (i + 1) + (i > unlocked && !lockIcon ? "\nLOCKED" : "");
            if(compactTiles && label)
            {
                bool locked=i>unlocked;
                label.text=(i+1).ToString();
                label.fontSize=locked ? 29 : 76;
                label.rectTransform.anchoredPosition=new Vector2(0,locked ? -144 : -66);
                label.rectTransform.sizeDelta=new Vector2(172,locked ? 38 : 95);
                if(levelBadge)
                {
                    label.color=new Color(.29f,.135f,.065f);
                    label.rectTransform.anchoredPosition=new Vector2(0,locked ? -108 : -70);
                    label.rectTransform.sizeDelta=new Vector2(140,locked ? 38 : 75);
                    label.fontSize=locked ? 32 : 64;
                    if(lockIcon) GameplayHudStyle.Place((RectTransform)lockIcon,62,35,48,48);
                    var badge=buttons[i].transform.Find("LevelBadge").GetComponent<Image>();
                    badge.color=locked ? new Color(.72f,.69f,.63f) : Color.white;
                }
                var status=buttons[i].transform.Find("LevelStatus");
                if(status)
                {
                    status.gameObject.SetActive(!locked && !levelBadge);
                    status.GetComponent<TextMeshProUGUI>().text=i==(preview ? 0 : LevelProgress.Latest(count)) ? "PLAY" : "OPEN";
                }
                buttons[i].GetComponent<Image>().color=locked ? new Color(.93f,.88f,.8f) : i==(preview ? 0 : LevelProgress.Latest(count)) ? new Color(.87f,.94f,.72f) : new Color(1,.98f,.91f);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
