using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(300)]
public sealed class ActivityUI : MonoBehaviour
{
    private ProgressUI progress;
    private DailyRewardUI daily;
    private GameManager game;
    private GameObject panel,body,dailyContent;
    private RectTransform page;
    private TextMeshProUGUI timer,notice;
    private readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
    private readonly List<Button> tabs=new List<Button>();
    private int tab,day=1;
    private bool oldInput,oldRaycasts,refreshing;
    private Coroutine clock;
    private string dateKey;
    public bool IsOpen=>panel && panel.activeSelf;
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        if (parent.Find(name) is RectTransform saved && saved) return saved;
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);GameplayHudStyle.Place(r,x,y,w,h);return r;
    }
    private Image Frame(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        string art=name=="Surface" ? "Popup_Paper_Panel" : name.StartsWith("Task_") ? "Quest_Row" : null;
        var node=Rect(parent,name,x,y,w,h).gameObject;
        var image=node.GetComponent<Image>();
        if (!image) image=art==null ? node.AddComponent<RoundedImage>() : node.AddComponent<Image>();
        if(art!=null) {CafeComponentArt.Apply(image,art);return image;}
        image.color=color;image.raycastTarget=false;
        var border=image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();border.effectColor=new Color(.7f,.49f,.29f,.6f);border.effectDistance=new Vector2(1,-1);return image;
    }
    private TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,bool center=false)
    {
        var node=Rect(parent,name,x,y,w,h).gameObject;
        if (node.TryGetComponent<TextMeshProUGUI>(out var saved)) {saved.text=value;return saved;}
        var t=node.AddComponent<TextMeshProUGUI>();t.font=GameTypography.Current.font;t.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;
        t.color=new Color(.35f,.19f,.1f);t.text=value;t.fontSize=t.fontSizeMax=size;t.fontSizeMin=size-2;t.enableAutoSizing=true;t.alignment=center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;return t;
    }
    private Sprite Icon(string name)
    {
        if(!icons.TryGetValue(name,out var sprite)) {sprite=Resources.Load<Sprite>("ActivityIcons/"+name);if(!sprite) sprite=Resources.Load<Sprite>("AchievementIcons/Achievement"+name);icons[name]=sprite;}return sprite;
    }
    private Image Art(Transform parent,string name,float x,float y,float w,float h)
    {
        var node=Rect(parent,name,x,y,w,h).gameObject;var saved=node.GetComponent<Image>();
        if (saved) return saved;
        var image=node.AddComponent<Image>();image.sprite=Icon(name);image.preserveAspect=true;image.raycastTarget=false;image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition+=new Vector2(w*.5f,-h*.5f);return image;
    }
    private Button Button(Transform parent,string caption,float x,float y,float w,UnityEngine.Events.UnityAction action,bool ready=false,bool enabled=true,string name=null)
    {
        var node=Rect(parent,name ?? caption,x,y,w,48).gameObject;
        var image=node.GetComponent<Image>() ?? node.AddComponent<Image>();
        image.sprite=Icon(ready ? "Primary" : "Secondary");
        image.type=Image.Type.Sliced;
        var b=node.GetComponent<Button>() ?? node.AddComponent<Button>();
        b.targetGraphic=image;b.onClick.RemoveAllListeners();b.onClick.AddListener(action);b.interactable=enabled;
        var colors=b.colors;colors.disabledColor=new Color(.62f,.6f,.54f);b.colors=colors;
        var sound=node.GetComponent<UIButtonClickSound>() ?? node.AddComponent<UIButtonClickSound>();
        sound.clickSound=progress.grid.sfxClick;
        var label=Text(image.transform,"Label",caption,8,3,w-16,40,21,true);label.color=Color.white;
        image.pixelsPerUnitMultiplier=image.sprite ? image.sprite.rect.height/((RectTransform)node.transform).rect.height : 1;
        return b;
    }
    private void Bar(Transform parent,float x,float y,float w,float value)
    {
        bool builtInTrack = parent.name.StartsWith("Task_") && CafeComponentArt.Sprite("Quest_Row");
        if (!builtInTrack) Frame(parent,"ProgressTrack",x,y,w,14,new Color(.86f,.79f,.65f));
        var fill = Frame(parent,"ProgressFill",builtInTrack ? 137 : x+2,builtInTrack ? 79 : y+2,4,builtInTrack ? 9 : 10,new Color(.15f,.75f,.8f));
        fill.enabled = value > 0;
        fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(4,(builtInTrack ? 559 : w-4)*Mathf.Clamp01(value)));
    }
    private void Start()
    {
        progress=GetComponent<ProgressUI>();daily=GetComponent<DailyRewardUI>();game=progress.grid.Session;
        BuildShell();
        BindShell();
        panel.SetActive(false);
        daily.menuButton.onClick=new Button.ButtonClickedEvent();daily.menuButton.onClick.AddListener(Open);
        var caption=daily.menuButton.transform.Find("Caption");if(caption) caption.GetComponent<TextMeshProUGUI>().text="ACTIVITIES";
        game.StateChanged+=OnState;EconomyStore.InventoryChanged+=InventoryChanged;
        var inventory=EconomyStore.Inventory;UpdateBadge(inventory);
    }
    private static readonly string[] TabLabels = {"DAILY", "WEEKLY", "FIRST 7 DAYS", "DAILY GIFTS", "CHECK-IN"};

    private void BuildShell()
    {
        if (transform.Find("ActivityHub")) return;
        var root=Rect(transform,"ActivityHub",0,0,1920,1080);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;panel=root.gameObject;
        var canvas=panel.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=420;panel.AddComponent<GraphicRaycaster>();
        var shade=panel.AddComponent<Image>();shade.color=new Color(.15f,.08f,.03f,.65f);
        page=Rect(root,"ActivityPage",0,0,1600,900);page.anchorMin=page.anchorMax=page.pivot=new Vector2(.5f,.5f);page.anchoredPosition=Vector2.zero;page.localScale=Vector3.one*1.2f;
        var surface=Frame(page,"Surface",54,58,1492,790,new Color(.46f,.28f,.17f));
        CafeFrameLayouts.Panel(surface.transform,"Activities");
        if(!CafeComponentArt.Sprite("Popup_Paper_Panel")) {Frame(page,"Trim",68,65,1464,765,new Color(.86f,.69f,.46f));Frame(page,"Paper",78,76,1444,744,new Color(1,.95f,.84f));}
        var header=Rect(page,"Header",410,35,780,82).gameObject.AddComponent<Image>();header.sprite=progress.panel.transform.Find("Popup/Header").GetComponent<Image>().sprite;header.type=Image.Type.Sliced;header.pixelsPerUnitMultiplier=header.sprite.rect.height/82;header.raycastTarget=false;
        var title=Text(page,"Title","CAFE ACTIVITIES",440,46,720,60,36,true);title.color=Color.white;
        Button(page,"X",1428,120,62,Close);
        timer=Text(page,"ResetTimer","",985,175,460,23,18,true);
        for(int i=0;i<TabLabels.Length;i++) {int index=i;Button(page,TabLabels[i],232+i*250,198,240,()=>Select(index));}
        notice=Text(page,"Notice","",114,763,1330,30,18);
        panel.SetActive(false);
    }

    private void BindShell()
    {
        panel=transform.Find("ActivityHub").gameObject;
        page=(RectTransform)panel.transform.Find("ActivityPage");
        timer=page.Find("ResetTimer").GetComponent<TextMeshProUGUI>();
        notice=page.Find("Notice").GetComponent<TextMeshProUGUI>();
        dailyContent=page.Find("DailyContent")?.gameObject;
        tabs.Clear();
        for (int i=0;i<TabLabels.Length;i++)
        {
            int index=i;
            var button=page.Find(TabLabels[i]).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(()=>Select(index));
            tabs.Add(button);
        }
        var close=page.Find("X").GetComponent<Button>();
        close.onClick.RemoveAllListeners();
        close.onClick.AddListener(Close);
    }

    public void PrepareSceneView()
    {
        progress=GetComponent<ProgressUI>();
        BuildShell();
        BindShell();
        var sample=new PlayerInventory();
        ActivityStore.Touch(sample,DateTime.Today);
        tab=0;
        RenderContent(sample);
        LayoutMilestones(body.transform.Find("MilestoneRewards"));
        timer.text="RESETS DAILY";
        panel.SetActive(false);
    }

    public void Open()
    {
        if(progress.ui.settingsPanel.gameObject.activeSelf || !game.OpenActivities()) return;
        oldInput=progress.menuInput.interactable;oldRaycasts=progress.menuInput.blocksRaycasts;progress.menuInput.interactable=progress.menuInput.blocksRaycasts=false;
        tab=0;day=Mathf.Clamp(ActivityStore.WelcomeDay(EconomyStore.Inventory),1,7);panel.SetActive(true);panel.transform.SetAsLastSibling();notice.text="";Refresh();clock=StartCoroutine(Tick());
    }
    public void Close()
    {
        if(!IsOpen) return;panel.SetActive(false);if(clock!=null) StopCoroutine(clock);clock=null;
        progress.menuInput.interactable=oldInput;progress.menuInput.blocksRaycasts=oldRaycasts;game.CloseActivities();
    }
    private void Select(int value)
    {
        if(value==4) {Close();daily.Open();return;}tab=value;notice.text="";Refresh();
    }
    private void OnState(GameState state) {if(state!=GameState.Activities && IsOpen) Close();}
    private void InventoryChanged() {if(!this) return;var p=EconomyStore.Inventory;UpdateBadge(p);if(IsOpen && !refreshing) Refresh();}
    private void UpdateBadge(PlayerInventory p)
    {
        bool ready=EconomyStore.DailyStatus.canClaim || !p.activities.daily.claimed.Contains("freegift");
        for(int t=0;t<3 && !ready;t++)
        {
            if(t==2 && !ActivityStore.WelcomeActive(p)) continue;
            for(int d=1;d<=(t==2 ? Mathf.Min(7,ActivityStore.WelcomeDay(p)) : 1);d++) foreach(var task in ActivityStore.Tasks(t,d)) ready|=ActivityStore.CanClaim(p,t,task);
            for(int i=0;i<3;i++) ready|=ActivityStore.Points(p,t)>=ActivityStore.Milestones(t)[i] && !ActivityStore.Period(p,t).claimed.Contains("chest"+i);
        }
        if(daily.availableBadge) daily.availableBadge.SetActive(ready);
    }
    private IEnumerator Tick()
    {
        while(IsOpen) {UpdateClock();yield return new WaitForSecondsRealtime(1);}
    }
    private void UpdateClock()
    {
        var p=EconomyStore.Inventory;string key=p.activities.lastDay;if(dateKey!=key) {dateKey=key;Refresh();return;}
        var left=ActivityStore.Remaining(p,tab==3 ? 0 : tab,DateTime.Now);
        timer.text=tab==2 && !ActivityStore.WelcomeActive(p) ? "WELCOME EVENT ENDED" : (tab==2 ? "ENDS IN  " : "RESETS IN  ")+(left.Days>0 ? left.Days+"d " : "")+left.Hours.ToString("00")+":"+left.Minutes.ToString("00")+":"+left.Seconds.ToString("00");
    }
    private void Refresh()
    {
        if(refreshing) return;refreshing=true;
        try
        {
            var p=EconomyStore.Inventory;dateKey=p.activities.lastDay;
            RenderContent(p);
            UpdateBadge(p);
        }
        finally {refreshing=false;}
        UpdateClock();
    }
    private void RenderContent(PlayerInventory inventory)
    {
        if (body && body != dailyContent)
        {
            body.SetActive(false);
            body.name="RetiredContent";
            Destroy(body);
        }
        if (dailyContent) dailyContent.SetActive(tab == 0);
        body=Rect(page,tab == 0 ? "DailyContent" : "Content",0,0,1600,900).gameObject;
        if (tab == 0) dailyContent=body;
        body.SetActive(true);
        body.transform.SetSiblingIndex(page.childCount-2);
        for (int i=0;i<tabs.Count;i++)
        {
            tabs[i].GetComponent<Image>().sprite=Icon(i==tab ? "Primary" : "Secondary");
            TabButtonStyle.Apply(tabs[i].GetComponent<Image>());
            var colors=tabs[i].colors;colors.disabledColor=Color.white;tabs[i].colors=colors;
            tabs[i].interactable=i!=tab;
        }
        if(tab==3) BuildGifts(inventory);else BuildTasks(inventory);
    }

    private void Row(ActivityTask task,int index,PlayerInventory p,int group,float y)
    {
        bool claimed=ActivityStore.Period(p,group).claimed.Contains(task.id),can=ActivityStore.CanClaim(p,group,task);
        var row=Frame(body.transform,"Task_"+task.id,114,y,1005,112,new Color(1,.98f,.94f)).transform;
        Art(row,task.icon,17,17,76,76);Text(row,"Title",task.title,112,8,590,34,25);
        int value=Mathf.Min(task.target,ActivityStore.Value(p,group,task));Text(row,"Progress",value+" / "+task.target,112,43,530,26,18);
        Bar(row,112,82,555,value/(float)task.target);Art(row,"Coins",693,6,60,55);Text(row,"Reward","+"+task.reward.coins,681,60,85,25,19,true);Text(row,"Points","+"+task.points+" PTS",674,86,105,20,15,true);
        string caption=claimed ? "COLLECTED" : can ? "CLAIM" : group==2 && (!ActivityStore.WelcomeActive(p) || task.day>ActivityStore.WelcomeDay(p)) ? "LOCKED" : "GO";
        Button(row,caption,781,34,195,()=>{if(can) {bool success=ActivityStore.Claim(group,task.id,task.day);notice.text=success ? "REWARD CLAIMED!  +"+task.reward.coins+" COINS" : "This reward is not ready.";Refresh();}else Go(task.metric);},can,!claimed && caption!="LOCKED","TaskAction");
    }
    private void BuildTasks(PlayerInventory p)
    {
        if(tab==2)
        {
            Frame(body.transform,"WelcomeBanner",114,248,1005,95,new Color(.9f,.95f,.83f));Art(body.transform,"Plant",127,250,95,91);
            Text(body.transform,"WelcomeTitle","MAKE YOURSELF AT HOME",248,251,840,40,28);Text(body.transform,"WelcomeInfo","Complete tasks from unlocked days. Progress counts from your first visit.",248,295,840,38,19);
            for(int i=1;i<=7;i++) {int selected=i;Button(body.transform,"DAY "+i,114+(i-1)*145,365,132,()=>{day=selected;Refresh();},day==i,i<=ActivityStore.WelcomeDay(p) && ActivityStore.WelcomeActive(p));}
        }
        var tasks=ActivityStore.Tasks(tab,day);for(int i=0;i<tasks.Count;i++) Row(tasks[i],i,p,tab,tab==2 ? 430+i*116 : 251+i*121);
        var side=Frame(body.transform,"MilestoneRewards",1144,250,332,508,new Color(.96f,.89f,.76f)).transform;
        side.GetComponent<Image>().enabled=false;
        if (!side.Find("MilestoneFramePNG")) RewardFrameArt.Create(side,"MilestoneFramePNG","Milestone",0,0,332,508).transform.SetAsFirstSibling();
        Text(side,"Title",tab==0 ? "TODAY'S TREATS" : tab==1 ? "WEEKLY TREATS" : "WELCOME TREATS",35,55,270,36,21,true);
        var prize=Art(side,"Chest",76,88,180,108);if(tab==2) {prize.sprite=Resources.Load<Sprite>("PlayerAvatars/Bunny");Text(side,"CharacterReward","BLOSSOM BUNNY + BONUS GIFTS",12,198,308,24,15,true);}int points=ActivityStore.Points(p,tab);var milestones=ActivityStore.Milestones(tab);
        Text(side,"Points",points+" / "+milestones[2]+" POINTS",76,447,180,39,22,true);
        Bar(side,38,225,256,points/(float)milestones[2]);
        for(int i=0;i<3;i++)
        {
            int index=i;bool collected=ActivityStore.Period(p,tab).claimed.Contains("chest"+i),ready=!collected && points>=milestones[i] && (tab!=2 || ActivityStore.WelcomeActive(p));
            string fashion=tab==1 && i==2 ? "Beret" : tab==2 && i==1 ? "Sailor" : null;
            string slotStyle=collected ? "Claimed" : fashion!=null || tab==2 && i==2 ? "Premium" : ready ? "Ready" : "Common";
            var savedSlot=side.Find("RewardSlot"+i);
            if (savedSlot) RewardFrameArt.Apply(savedSlot.GetComponent<Image>(),slotStyle);
            else RewardFrameArt.Create(side,"RewardSlot"+i,slotStyle,24+i*99,251,86,82);
            var rewardIcon=Art(side,i==0 ? "Coins" : i==1 ? "Hint" : "Shuffle",42+i*99,266,50,50);
            if(fashion!=null) {rewardIcon.sprite=Resources.Load<Sprite>("AvatarFashion/"+fashion);AvatarFashionRewards.Style(rewardIcon);}
            var details=Text(side,"RewardDetails"+i,(fashion!=null ? "+ "+(fashion=="Beret" ? "BERET" : "SAILOR CAP")+"\n" : tab==2 && i==2 ? "+ BUNNY\n" : "")+Reward(ActivityStore.MilestoneReward(tab,i)),23+i*99,338,88,48,12,true);details.alignment=TextAlignmentOptions.Top;
            string action = collected ? "GOT IT" : ready ? "CLAIM" : milestones[i]+" PTS";
            bool hasSavedAction = side.Find("MilestoneAction"+i);
            var claim=Button(side,action,24+i*99,391,86,()=>{ActivityStore.ClaimMilestone(tab,index);Refresh();},ready,ready,"MilestoneAction"+i);
            if (!hasSavedAction) SizeMilestoneAction(claim);
        }
    }

    private static void SizeMilestoneAction(Button button)
    {
        ((RectTransform)button.transform).sizeDelta = new Vector2(86,32);
        var caption=button.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        GameplayHudStyle.Place(caption.rectTransform,3,2,80,28);
        caption.fontSizeMax=17;
        caption.fontSizeMin=14;
        var icon=button.GetComponent<Image>();
        icon.pixelsPerUnitMultiplier=icon.sprite.rect.height/32;
    }

    private static void LayoutMilestones(Transform side)
    {
        PlaceExisting(side,"Points",76,447,180,39);
        var points=side.Find("Points").GetComponent<TextMeshProUGUI>();
        points.fontSizeMin=18;
        points.fontSizeMax=22;
        PlaceExisting(side,"ProgressTrack",38,225,256,14);
        var fill=(RectTransform)side.Find("ProgressFill");
        GameplayHudStyle.Place(fill,40,227,fill.rect.width,10);
        PlaceExisting(side,"Chest",76,88,180,108);
        string[] iconNames={"Coins","Hint","Shuffle"};
        for (int i=0;i<iconNames.Length;i++)
        {
            PlaceExisting(side,"RewardSlot"+i,24+i*99,251,86,82);
            RewardFrameArt.Apply(side.Find("RewardSlot"+i).GetComponent<Image>(),"Common");
            PlaceExisting(side,iconNames[i],42+i*99,266,50,50);
            PlaceExisting(side,"RewardDetails"+i,23+i*99,338,88,48);
            PlaceExisting(side,"MilestoneAction"+i,24+i*99,391,86,32);
            SizeMilestoneAction(side.Find("MilestoneAction"+i).GetComponent<Button>());
        }
    }

    private static void PlaceExisting(Transform parent,string name,float x,float y,float width,float height)
    {
        var rect=(RectTransform)parent.Find(name);
        var image=rect.GetComponent<Image>();
        GameplayHudStyle.Place(rect,x,y,width,height);
        if (image && image.preserveAspect)
        {
            rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition+=new Vector2(width*.5f,-height*.5f);
        }
    }
    private static string Reward(LevelReward r)=>r.coins+" COINS"+(r.hints>0 ? "\n+"+r.hints+" HINT" : "")+(r.shuffles>0 ? "\n+"+r.shuffles+" SHUFFLE" : "");
    private void BuildGifts(PlayerInventory p)
    {
        var gift=Frame(body.transform,"FreeGift",114,248,420,508,new Color(.89f,.95f,.88f)).transform;
        gift.GetComponent<Image>().enabled=false;
        RewardFrameArt.Create(gift,"GiftFramePNG","Milestone",0,0,420,508).transform.SetAsFirstSibling();
        Text(gift,"Title","A GIFT FROM THE CAFE",35,95,350,34,22,true);Art(gift,"Chest",110,150,200,150);Text(gift,"Amount","50 COINS",20,310,380,50,34,true);
        bool claimed=p.activities.daily.claimed.Contains("freegift");Button(gift,claimed ? "COLLECTED TODAY" : "CLAIM FREE GIFT",78,443,264,()=>{ActivityStore.ClaimGift();Refresh();},!claimed,!claimed);
        var tasks=ActivityStore.Tasks(0);
        for(int i=1;i<tasks.Count;i++)
        {
            var task=tasks[i];float y=248+(i-1)*174;
            var row=Frame(body.transform,"DailyGiftTask_"+task.id,560,y,916,150,new Color(1,.98f,.94f)).transform;
            Art(row,task.icon,16,23,95,95);Text(row,"Title",task.title,132,10,515,40,25);
            int value=Mathf.Min(task.target,ActivityStore.Value(p,0,task));Text(row,"Progress",value+" / "+task.target+"   |   +"+task.reward.coins+" COINS",132,55,515,30,19);Bar(row,132,106,450,value/(float)task.target);
            bool got=p.activities.daily.claimed.Contains(task.id),ready=ActivityStore.CanClaim(p,0,task);
            Button(row,got ? "COLLECTED" : ready ? "CLAIM" : "GO",674,53,210,()=>{if(ready) {ActivityStore.Claim(0,task.id);Refresh();}else Go(task.metric);},ready,!got);
        }
    }
    private void Go(int metric)
    {
        Close();if(metric==6) {GetComponent<DecorationStudio>().Open();return;}progress.ui.OpenLevelSelect();
    }
    private void OnDestroy()
    {
        if(game) game.StateChanged-=OnState;EconomyStore.InventoryChanged-=InventoryChanged;
        if(IsOpen && progress && progress.menuInput) {progress.menuInput.interactable=oldInput;progress.menuInput.blocksRaycasts=oldRaycasts;}
    }
}
