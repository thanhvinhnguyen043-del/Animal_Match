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
    private readonly TextMeshProUGUI[] rows=new TextMeshProUGUI[10];
    private Button refresh;
    private bool busy,oldInput,oldRaycasts;
    private int request;
    private GameManager game;
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);GameplayHudStyle.Place(rect,x,y,w,h);return rect;}
    private TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size)
    {
        var text=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=GameTypography.Current.font;
        text.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;text.color=new Color(.29f,.135f,.065f);text.text=value;
        text.fontSize=text.fontSizeMax=size;text.fontSizeMin=size-3;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    private Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=progress.filters[0].GetComponent<Image>().sprite;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        Text(image.transform,"Label",caption,8,8,w-16,h-16,27);return button;
    }
    private void Start()
    {
        progress=GetComponent<ProgressUI>();game=progress.grid.Session;game.GameEnded+=Ended;game.StateChanged+=StateChanged;
        var menu=Button(progress.ui.mainMenuPanel,"LeaderboardButton","LEADERBOARD",65,745,160,160,Open);
        var menuSprite=Resources.Load<Sprite>("MenuIcons/Leaderboard");
        if(menuSprite)
        {
            var icon=menu.GetComponent<Image>();icon.sprite=menuSprite;icon.type=Image.Type.Simple;icon.preserveAspect=true;
            menu.transform.Find("Label").gameObject.SetActive(false);
        }
        var root=Rect(transform,"OnlineLeaderboard",0,0,1920,1080);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;panel=root.gameObject;
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=510;root.gameObject.AddComponent<GraphicRaycaster>();
        var shade=root.gameObject.AddComponent<Image>();shade.color=new Color(.12f,.07f,.03f,.78f);
        var surface=Rect(root,"Surface",280,70,1360,940).gameObject.AddComponent<RoundedImage>();CafePanelStyle.Frame(surface.transform);
        Text(root,"Title","ONLINE LEADERBOARD  /  LEVEL 1",400,110,1120,65,40);
        Text(root,"Columns","RANK                         PLAYER                         BEST SCORE",430,210,1060,45,27);
        for(int i=0;i<rows.Length;i++) rows[i]=Text(root,"Row"+i,"",400,270+i*53,1120,48,26);
        status=Text(root,"Status","",400,815,1120,78,24);
        refresh=Button(root,"Refresh","REFRESH",620,915,300,70,Reload);Button(root,"Close","CLOSE",1000,915,300,70,Close);panel.SetActive(false);
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
        status.text="Connecting...";foreach(var row in rows) row.text="";
        try
        {
            await OnlineLeaderboard.Instance.Connect();await OnlineLeaderboard.Instance.Flush();
            var scores=await LeaderboardsService.Instance.GetScoresAsync(OnlineLeaderboard.BoardId,new GetScoresOptions {Limit=10});
            if(!this || token!=request || !panel.activeSelf) return;
            int index=0;foreach(var entry in scores.Results)
            {
                if(index==rows.Length) break;string name=string.IsNullOrEmpty(entry.PlayerName) ? "Player "+entry.PlayerId.Substring(0,Math.Min(6,entry.PlayerId.Length)) : entry.PlayerName;
                name=name.Replace("<","").Replace(">","").Replace("\n"," ").Replace("\r"," ");if(name.Length>24) name=name.Substring(0,24);
                rows[index++].text=(entry.Rank+1)+".     "+name+(entry.PlayerId==AuthenticationService.Instance.PlayerId ? " (YOU)" : "")+"     "+entry.Score.ToString("N0");
            }
            status.text=index==0 ? "No scores yet. Win Level 1 to join!" : "Top 10 players - best score per player.\n"+OnlineLeaderboard.Instance.LastStatus;
        }
        catch(Exception) {if(this && token==request) status.text="Could not load the online ranking. Check your connection and try REFRESH.";}
        finally {if(this && token==request) {busy=false;refresh.interactable=true;}}
    }
    private void StateChanged(GameState state) {if(panel && panel.activeSelf && state!=GameState.Leaderboard) Close();}
    private void OnDestroy() {request++;if(game) {game.GameEnded-=Ended;game.StateChanged-=StateChanged;}}
}
