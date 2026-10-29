using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(250)]
public sealed class StarGoalTutorialUI : MonoBehaviour
{
    private ProgressUI progress;
    private WaterTutorialUI mechanic;
    private GameManager game;
    private GameObject panel;
    private TextMeshProUGUI description;
    private Coroutine pending;
    private float previousScale=1;
    public bool IsOpen=>panel && panel.activeSelf;
    private void Start()
    {
        progress=GetComponent<ProgressUI>();mechanic=GetComponent<WaterTutorialUI>();game=progress.grid.Session;
        Build();game.LevelStarted+=Queue;game.StateChanged+=StateChanged;
        if(game.State==GameState.Playing || mechanic && mechanic.IsOpen) Queue();
    }
    private void Build()
    {
        panel=Instantiate(mechanic.panel,mechanic.panel.transform.parent);panel.name="StarGoalTutorial";panel.SetActive(false);
        var popup=panel.transform.Find("Popup");
        foreach(Transform child in popup) if(child.name!="Panel" && child.name!="Header") child.gameObject.SetActive(false);
        Label(popup,"Title","THREE STAR GOALS",370,70,540,80,42,true);
        Label(popup,"Level","LEVEL "+(progress.grid.levelIndex+1),400,210,480,48,30,false);
        var stars=new GameObject("Stars",typeof(RectTransform),typeof(CanvasRenderer),typeof(StarRatingGraphic)).GetComponent<StarRatingGraphic>();stars.transform.SetParent(popup,false);
        GameplayHudStyle.Place(stars.rectTransform,440,265,400,95);stars.SetStars(3);
        description=Label(popup,"Goals","",210,370,860,290,30,false);
        Label(popup,"Paused","Timer paused. Open ? to check these goals again.",210,665,860,42,24,false);
        var original=popup.Find("GotItButton").GetComponent<Image>();
        var button=new GameObject("Continue",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));button.transform.SetParent(popup,false);
        GameplayHudStyle.Place(button.GetComponent<RectTransform>(),410,735,460,145);button.GetComponent<Image>().sprite=original.sprite;button.GetComponent<Button>().onClick.AddListener(Dismiss);
        Label(button.transform,"Label","LET'S PLAY!",20,35,420,70,38,true);
        var help=new GameObject("StarGoalHelp",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));help.transform.SetParent(progress.ui.gameplayPanel,false);
        GameplayHudStyle.Place(help.GetComponent<RectTransform>(),1740,300,90,90);
        help.GetComponent<Image>().sprite=original.sprite;help.GetComponent<Button>().onClick.AddListener(Open);
        Label(help.transform,"Label","?",0,5,90,75,52,true);
    }
    private TextMeshProUGUI Label(Transform parent,string name,string value,float x,float y,float w,float h,float size,bool heading)
    {
        var text=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();text.transform.SetParent(parent,false);
        GameplayHudStyle.Place(text.rectTransform,x,y,w,h);text.font=GameTypography.Current.font;
        text.fontSharedMaterial=heading ? mechanic.panel.transform.Find("Popup/Title").GetComponent<TextMeshProUGUI>().fontSharedMaterial : progress.timeGoal.fontSharedMaterial;
        text.color=heading ? new Color(1,.99f,.92f) : new Color(.32f,.16f,.08f);text.text=value;text.fontSize=text.fontSizeMax=size;text.fontSizeMin=size-3;
        text.enableAutoSizing=true;text.enableWordWrapping=true;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    private void Queue()
    {
        if(pending!=null) StopCoroutine(pending);if(IsOpen) Hide();pending=StartCoroutine(AfterMechanic());
    }
    private IEnumerator AfterMechanic()
    {
        yield return null;
        var loading=GetComponent<LoadingUI>();
        while((mechanic && mechanic.IsOpen) || (loading && loading.IsBusy)) yield return null;
        pending=null;if(game.State==GameState.Playing) Open();
    }
    public void Open()
    {
        if(IsOpen || game.State!=GameState.Playing || mechanic && mechanic.IsOpen || game.IsResolvingMatch) return;
        var challenge=game.Challenge;string goal;
        switch(game.ActiveGoal)
        {
            case StarGoal.Water:goal="Break all water covers";break;
            case StarGoal.Stone:goal="Break all stone covers";break;
            case StarGoal.WaterAndStone:goal="Break all water and stone covers";break;
            default:goal="Reach combo x"+challenge.comboTarget;break;
        }
        panel.transform.Find("Popup/Level").GetComponent<TextMeshProUGUI>().text="LEVEL "+(progress.grid.levelIndex+1);
        description.text="1 STAR  -  Clear the board\n\n2 STARS  -  Finish with at least "+ProgressUI.Clock(game.TimeLimit*challenge.timeRemainingFraction)+" left\n\n3 STARS  -  Meet the time goal AND\n"+goal+" within "+challenge.turnLimit+" turns";
        previousScale=Time.timeScale;game.Pause();Time.timeScale=0;panel.SetActive(true);panel.transform.SetAsLastSibling();
    }
    public void Dismiss() {if(!IsOpen) return;Hide();if(game.State==GameState.Paused) game.Resume();}
    private void Hide() {panel.SetActive(false);Time.timeScale=previousScale;}
    private void StateChanged(GameState state) {if(IsOpen && state!=GameState.Paused) Hide();if(state==GameState.Menu || state==GameState.LevelSelect) {if(pending!=null) StopCoroutine(pending);pending=null;}}
    private void OnDestroy() {if(game) {game.LevelStarted-=Queue;game.StateChanged-=StateChanged;}if(IsOpen) Time.timeScale=previousScale;}
}
