using System;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.UI;

public sealed class LeaderboardUI : MonoBehaviour
{
    private ProgressUI progress;
    private GameObject panel;
    private TextMeshProUGUI status;
    private readonly ScoreRow[] rows = new ScoreRow[10];

    private sealed class ScoreRow
    {
        public Image Background;
        public TextMeshProUGUI Rank;
        public TextMeshProUGUI Player;
        public TextMeshProUGUI Score;
    }

    private static readonly Color Ink = new Color(.25f, .16f, .32f);
    private static readonly Color Lavender = new Color(.93f, .89f, .96f);
    private static readonly Color Ivory = new Color(1f, .96f, .89f);
    private Button refresh;
    private bool busy,oldInput,oldRaycasts;
    private int request;
    private GameManager game;
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);GameplayHudStyle.Place(rect,x,y,w,h);return rect;}
    private TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size)
    {
        var text=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=GameTypography.Current.font;
        text.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;text.color=new Color(.29f,.135f,.065f);text.text=value;text.richText=false;text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;
        text.fontSize=text.fontSizeMax=size;text.fontSizeMin=size-3;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    private Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=progress.filters[0].GetComponent<Image>().sprite;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        if (image.sprite && image.sprite.border.x > 0)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = image.sprite.rect.height / h;
        }
        Text(image.transform,"Label",caption,8,5,w-16,h-10,27).color = Color.white;
        return button;
    }
    private void Start()
    {
        progress=GetComponent<ProgressUI>();game=progress.grid.Session;game.GameEnded+=Ended;game.StateChanged+=StateChanged;
        var menu=PrepareSceneView();menu.onClick.RemoveListener(Open);menu.onClick.AddListener(Open);
        PreparePanelSceneView();
        BindPanel();
        panel.SetActive(false);
    }

    public void PreparePanelSceneView()
    {
        progress = GetComponent<ProgressUI>();
        if (transform.Find("OnlineLeaderboard")) return;

        var root = Rect(transform, "OnlineLeaderboard", 0, 0, 1920, 1080);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
        root.anchoredPosition = Vector2.zero;
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 510;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var shade = root.gameObject.AddComponent<Image>();
        shade.color = new Color(.13f, .08f, .16f, .66f);

        var frame = Rect(root, "Surface", 120, 85, 1680, 945);
        if (!CafeFrameLayouts.Panel(frame, "Leaderboard")) CafePanelStyle.Frame(frame);
        var header = Rect(root, "Header", 548, 46, 824, 137).gameObject.AddComponent<Image>();
        header.sprite = progress.panel.transform.Find("Popup/Header").GetComponent<Image>().sprite;
        header.raycastTarget = false;
        header.type = Image.Type.Sliced;
        header.pixelsPerUnitMultiplier = header.sprite.rect.height / 137f;
        Text(root, "Title", "LEADERBOARD", 610, 78, 700, 66, 48).color = Color.white;

        Text(root, "Level", "LEVEL 1  /  TOP 10", 530, 225, 860, 35, 27).color = Ink;
        var columns = Surface(root, "Columns", 385, 269, 1150, 41, new Color(.82f, .75f, .9f));
        Text(columns, "Rank", "RANK", 22, 3, 146, 37, 23).color = Ink;
        Text(columns, "Player", "PLAYER", 180, 3, 650, 37, 23).alignment = TextAlignmentOptions.MidlineLeft;
        Text(columns, "Score", "BEST SCORE", 850, 3, 250, 37, 23).alignment = TextAlignmentOptions.MidlineRight;

        var entries = Rect(root, "Scores", 385, 320, 1150, 467);
        for (int i = 0; i < rows.Length; i++)
        {
            var row = Surface(entries, "Row" + i, 0, i * 46, 1150, 43, i % 2 == 0 ? Lavender : Ivory);
            var rank = Surface(row, "RankBadge", 48, 4, 94, 35, RankColor(i));
            Text(rank, "Rank", (i + 1).ToString(), 0, 0, 94, 35, 24).color = Ink;
            Text(row, "Player", "—", 180, 2, 650, 39, 26).alignment = TextAlignmentOptions.MidlineLeft;
            Text(row, "Score", "—", 850, 2, 250, 39, 26).alignment = TextAlignmentOptions.MidlineRight;
        }

        var caption = Text(root, "Status", "Win Level 1 to join the ranking.", 300, 788, 1320, 48, 23);
        caption.enableWordWrapping = true;
        Button(root, "Refresh", "REFRESH", 659, 843, 282, 52, Reload);
        Button(root, "Close", "CLOSE", 979, 843, 282, 52, Close);
        root.gameObject.SetActive(false);
    }

    private RectTransform Surface(Transform parent, string name, float x, float y, float width, float height, Color color)
    {
        var rect = Rect(parent, name, x, y, width, height);
        var image = rect.gameObject.AddComponent<RoundedImage>();
        image.cornerRadius = 13;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Color RankColor(int index)
    {
        switch (index)
        {
            case 0: return new Color(1f, .81f, .36f);
            case 1: return new Color(.87f, .87f, .94f);
            case 2: return new Color(.95f, .74f, .55f);
            default: return new Color(.89f, .83f, .94f);
        }
    }

    private void BindPanel()
    {
        var root = transform.Find("OnlineLeaderboard");
        panel = root.gameObject;
        status = root.Find("Status").GetComponent<TextMeshProUGUI>();
        refresh = root.Find("Refresh").GetComponent<Button>();
        refresh.onClick.RemoveListener(Reload);
        refresh.onClick.AddListener(Reload);
        var close = root.Find("Close").GetComponent<Button>();
        close.onClick.RemoveListener(Close);
        close.onClick.AddListener(Close);
        for (int i = 0; i < rows.Length; i++)
        {
            var row = root.Find("Scores/Row" + i);
            rows[i] = new ScoreRow
            {
                Background = row.GetComponent<Image>(),
                Rank = row.Find("RankBadge/Rank").GetComponent<TextMeshProUGUI>(),
                Player = row.Find("Player").GetComponent<TextMeshProUGUI>(),
                Score = row.Find("Score").GetComponent<TextMeshProUGUI>()
            };
        }
    }

    private void SetRow(int index, int rank, string player, string score, bool own)
    {
        var row = rows[index];
        row.Rank.text = rank.ToString();
        row.Player.text = player;
        row.Score.text = score;
        row.Background.color = own ? new Color(.81f, .93f, .8f) : index % 2 == 0 ? Lavender : Ivory;
    }

    public Button PrepareSceneView()
    {
        progress=GetComponent<ProgressUI>();
        var existing=progress.ui.mainMenuPanel.Find("LeaderboardButton");
        if(existing) return existing.GetComponent<Button>();
        var menu=Button(progress.ui.mainMenuPanel,"LeaderboardButton","LEADERBOARD",65,745,160,160,Open);
        var menuSprite=Resources.Load<Sprite>("MenuIcons/Leaderboard");
        if(menuSprite)
        {
            var icon=menu.GetComponent<Image>();icon.sprite=menuSprite;icon.type=Image.Type.Simple;icon.preserveAspect=true;
            menu.transform.Find("Label").gameObject.SetActive(false);
        }
        return menu;
    }
    private void Ended(bool won)
    {
        if(won) OnlineLeaderboard.Instance.QueueWin(CareerStore.LevelId(progress.grid.levelIndex,progress.grid.levelDatabase),game.Score);
    }
    public void Open()
    {
        if(game.State!=GameState.Menu || progress.ui.settingsPanel.gameObject.activeSelf) return;
        if(!game.OpenLeaderboard()) return;
        var input=progress.menuInput;oldInput=input.interactable;oldRaycasts=input.blocksRaycasts;input.interactable=input.blocksRaycasts=false;
        panel.SetActive(true);panel.transform.SetAsLastSibling();Reload();
    }
    public void Close()
    {
        if(!panel.activeSelf) return;panel.SetActive(false);request++;busy=false;
        progress.menuInput.interactable=oldInput;progress.menuInput.blocksRaycasts=oldRaycasts;game.CloseLeaderboard();
    }
    private async void Reload()
    {
        if(busy || !panel.activeSelf) return;busy=true;refresh.interactable=false;int token=++request;
        status.text="Connecting...";
        for (int i = 0; i < rows.Length; i++) SetRow(i, i + 1, "—", "—", false);
        try
        {
            await OnlineLeaderboard.Instance.Connect();await OnlineLeaderboard.Instance.Flush();
            var scores=await LeaderboardsService.Instance.GetScoresAsync(OnlineLeaderboard.BoardId,new GetScoresOptions {Limit=10});
            if(!this || token!=request || !panel.activeSelf) return;
            int index=0;foreach(var entry in scores.Results)
            {
                if(index==rows.Length) break;string name=string.IsNullOrEmpty(entry.PlayerName) ? "Player "+entry.PlayerId.Substring(0,Math.Min(6,entry.PlayerId.Length)) : entry.PlayerName;
                name=name.Replace("<","").Replace(">","").Replace("\n"," ").Replace("\r"," ");if(name.Length>24) name=name.Substring(0,24);
                bool own = entry.PlayerId == AuthenticationService.Instance.PlayerId;
                SetRow(index++, entry.Rank + 1, own ? name + " (YOU)" : name, entry.Score.ToString("N0"), own);
            }
            status.text=index==0 ? "No scores yet. Win Level 1 to join!" : "Top 10 players - best score per player.\n"+OnlineLeaderboard.Instance.LastStatus;
        }
        catch(Exception) {if(this && token==request) status.text="Could not load the online ranking. Check your connection and try REFRESH.";}
        finally {if(this && token==request) {busy=false;refresh.interactable=true;}}
    }
    private void StateChanged(GameState state) {if(panel && panel.activeSelf && state!=GameState.Leaderboard) Close();}
    private void OnDestroy() {request++;if(game) {game.GameEnded-=Ended;game.StateChanged-=StateChanged;}}
}
