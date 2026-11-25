using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(215)]
public sealed class FirstBoosterRewardUI : MonoBehaviour
{
    public GameObject panel;
    public bool replayInEditor = true;
    private bool hintRevealed, shuffleRevealed;
    private ProgressUI progress;
    private GameManager game;
    private Image hero, flying, burst;
    private Image[] locks;
    private RectTransform[] sparkles;
    private TextMeshProUGUI amount;
    private Button claim;
    private bool requested, busy;
    private Vector3[] corners = new Vector3[4];

    private RectTransform Node(Transform parent,string name,float x,float y,float w,float h)
    {
        var old=parent.Find(name);
        var rect=old ? (RectTransform)old : new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);
        GameplayHudStyle.Place(rect,x,y,w,h);
        return rect;
    }

    private Image Art(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var rect=Node(parent,name,x,y,w,h);
        var image=rect.GetComponent<Image>();
        if(!image) image=rect.gameObject.AddComponent<Image>();
        image.sprite=sprite;
        image.preserveAspect=true;
        image.raycastTarget=false;
        return image;
    }

    private TextMeshProUGUI Text(Transform parent,string name,string words,float x,float y,float w,float h,float size)
    {
        var rect=Node(parent,name,x,y,w,h);
        var label=rect.GetComponent<TextMeshProUGUI>();
        if(!label) label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text=words;
        label.font=GameTypography.Current.font;
        label.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;
        label.fontSize=label.fontSizeMax=size;
        label.fontSizeMin=size*.8f;
        label.enableAutoSizing=true;
        label.alignment=TextAlignmentOptions.Center;
        label.color=new Color(1,.96f,.82f);
        label.raycastTarget=false;
        return label;
    }

    public void PrepareSceneView()
    {
        progress=GetComponent<ProgressUI>();
        var root=progress.ui.gameplayPanel;
        BindVisibleBooster(BoosterType.Hint);
        BindVisibleBooster(BoosterType.Shuffle);
        var existing=root.Find("FirstBoosterReward");
        if(existing && existing.Find("Sparkle9"))
        {
            panel=existing.gameObject;
            hero=existing.Find("Booster").GetComponent<Image>();
            flying=existing.Find("FlyingBooster").GetComponent<Image>();
            burst=existing.Find("UnlockFlash").GetComponent<Image>();
            amount=existing.Find("Amount").GetComponent<TextMeshProUGUI>();
            claim=existing.Find("Claim").GetComponent<Button>();
            claim.onClick.RemoveAllListeners();
            claim.onClick.AddListener(()=>requested=true);
            sparkles=new RectTransform[10];
            for(int i=0;i<10;i++) sparkles[i]=(RectTransform)existing.Find("Sparkle"+i);
            locks=new[]{progress.grid.btnHint.transform.Find("UnlockLock").GetComponent<Image>(),
                progress.grid.btnShuffle.transform.Find("UnlockLock").GetComponent<Image>()};
            PositionHalo();
            StyleLocks();
            panel.SetActive(false);
            return;
        }
        var rect=Node(root,"FirstBoosterReward",0,0,1920,1080);
        panel=rect.gameObject;
        var canvas=panel.GetComponent<Canvas>();
        if(!canvas) canvas=panel.AddComponent<Canvas>();
        canvas.overrideSorting=true;
        canvas.sortingOrder=620;
        if(!panel.GetComponent<GraphicRaycaster>()) panel.AddComponent<GraphicRaycaster>();
        var shade=panel.GetComponent<Image>();
        if(!shade) shade=panel.AddComponent<Image>();
        shade.color=new Color(.045f,.018f,.008f,.94f);
        shade.raycastTarget=true;
        var halo=Node(rect,"RewardHalo",480,150,960,760);
        var haloGraphic=halo.GetComponent<RewardHaloGraphic>();
        if(!haloGraphic) haloGraphic=halo.gameObject.AddComponent<RewardHaloGraphic>();
        haloGraphic.raycastTarget=false;
        RewardFrameArt.Create(rect,"TitleFrame","Common",700,125,520,100);
        Text(rect,"Title","YOUR FIRST BOOSTER!",730,145,460,60,39).color=new Color(.43f,.23f,.12f);
        Text(rect,"Celebration","A little café magic",560,235,800,95,66);
        hero=Art(rect,"Booster",Resources.Load<Sprite>("ActivityIcons/Hint"),782,322,356,356);
        RewardFrameArt.Create(rect,"AmountFrame","Common",836,666,248,72);
        amount=Text(rect,"Amount","+3 HINTS",850,675,220,52,36);
        amount.color=new Color(.43f,.23f,.12f);
        Text(rect,"Welcome","A welcome gift for your café.",600,750,720,45,25);
        var buttonImage=Art(rect,"Claim",Resources.Load<Sprite>("ActivityIcons/Primary"),751,817,418,139);
        buttonImage.raycastTarget=true;
        claim=buttonImage.GetComponent<Button>();
        if(!claim) claim=buttonImage.gameObject.AddComponent<Button>();
        claim.targetGraphic=buttonImage;
        var sound=claim.GetComponent<UIButtonClickSound>();
        if(!sound) sound=claim.gameObject.AddComponent<UIButtonClickSound>();
        sound.clickSound=progress.grid.sfxClick;
        Text(buttonImage.transform,"Label","CLAIM",0,0,418,139,45);
        claim.onClick.RemoveAllListeners();
        claim.onClick.AddListener(()=>requested=true);
        flying=Art(rect,"FlyingBooster",hero.sprite,782,322,356,356);
        flying.gameObject.SetActive(false);
        burst=Art(rect,"UnlockFlash",Resources.Load<Sprite>("StarBar/StarGold"),0,0,150,150);
        burst.gameObject.SetActive(false);
        sparkles=new RectTransform[10];
        for(int i=0;i<sparkles.Length;i++)
        {
            float angle=i*Mathf.PI*2/sparkles.Length;
            sparkles[i]=Art(rect,"Sparkle"+i,Resources.Load<Sprite>("StarBar/StarGold"),
                940+Mathf.Cos(angle)*355,480+Mathf.Sin(angle)*245,24+i%3*10,24+i%3*10).rectTransform;
            sparkles[i].pivot=new Vector2(.5f,.5f);
        }
        locks=new Image[2];
        for(int i=0;i<2;i++)
        {
            var button=i==0 ? progress.grid.btnHint : progress.grid.btnShuffle;
            var image=Art(button.transform,"UnlockLock",Resources.Load<Sprite>("PlayerAvatars/Lock"),20,20,56,56);
            image.rectTransform.anchorMin=image.rectTransform.anchorMax=image.rectTransform.pivot=new Vector2(.5f,.5f);
            image.rectTransform.anchoredPosition=Vector2.zero;
            image.rectTransform.sizeDelta=new Vector2(62,62);
            image.transform.SetAsLastSibling();
            locks[i]=image;
        }
        PositionHalo();
        StyleLocks();
        panel.SetActive(false);
    }

    private void Start()
    {
        PrepareSceneView();
        game=progress.grid.Session;
        game.LevelStarted+=BeginLevel;
        game.StateChanged+=StateChanged;
        EconomyStore.InventoryChanged+=RefreshLocks;
        RefreshLocks();
    }

    private void PositionHalo()
    {
        var halo=(RectTransform)panel.transform.Find("RewardHalo");
        if(!halo.GetComponent<CanvasRenderer>()) halo.gameObject.AddComponent<CanvasRenderer>();
        halo.GetComponent<RewardHaloGraphic>().SetVerticesDirty();
        GameplayHudStyle.Place(halo,500,140,920,720);
        halo.pivot=new Vector2(.5f,.5f);
        halo.anchoredPosition=new Vector2(960,-500);
        halo.SetAsFirstSibling();
    }

    private void BindVisibleBooster(BoosterType type)
    {
        var icon=progress.ui.gameplayPanel.Find(type==BoosterType.Hint ? "Icon__Hint" : "Icon__Shuffle");
        if(!icon) return;
        var previous=Target(type);
        var button=icon.GetComponent<Button>();
        if(!button) button=icon.gameObject.AddComponent<Button>();
        button.targetGraphic=icon.GetComponent<Image>();
        button.onClick.RemoveAllListeners();
        if(type==BoosterType.Hint) button.onClick.AddListener(progress.grid.ShowHint);
        else button.onClick.AddListener(progress.grid.ShuffleBoard);
        var sound=icon.GetComponent<UIButtonClickSound>();
        if(!sound) sound=icon.gameObject.AddComponent<UIButtonClickSound>();
        sound.clickSound=progress.grid.sfxClick;
        if(previous && previous.transform!=icon)
        {
            var oldLock=previous.transform.Find("UnlockLock");
            if(oldLock && !icon.Find("UnlockLock")) oldLock.SetParent(icon,false);
        }
        if(type==BoosterType.Hint) progress.grid.btnHint=button;
        else progress.grid.btnShuffle=button;
    }

    private void StyleLocks()
    {
        foreach(var image in locks)
        {
            var canvas=image.GetComponent<Canvas>();
            if(!canvas) canvas=image.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting=true;
            canvas.sortingOrder=590;
            image.rectTransform.sizeDelta=new Vector2(78,78);
            image.color=Color.white;
        }
        var group=panel.GetComponent<CanvasGroup>();
        if(!group) group=panel.AddComponent<CanvasGroup>();
        panel.GetComponent<Image>().color=new Color(.045f,.018f,.008f,.94f);
        group.alpha=1;
        group.ignoreParentGroups=true;
    }

    private Button Target(BoosterType type)=>type==BoosterType.Hint ? progress.grid.btnHint : progress.grid.btnShuffle;

    private void BeginLevel()
    {
        hintRevealed=shuffleRevealed=false;
        RefreshLocks();
    }

    public bool CanUse(BoosterType type)
    {
        if(!progress) progress=GetComponent<ProgressUI>();
        int level=progress.grid.levelIndex+1;
        int introduction=type==BoosterType.Hint ? 3 : 6;
        if(level<introduction || !EconomyStore.IsBoosterUnlocked(type)) return false;
        bool revealed=type==BoosterType.Hint ? hintRevealed : shuffleRevealed;
        return !(Application.isEditor && replayInEditor && level==introduction && !revealed);
    }

    private void RefreshLocks()
    {
        for(int i=0;i<2;i++)
        {
            var type=i==0 ? BoosterType.Hint : BoosterType.Shuffle;
            bool locked=!CanUse(type);
            locks[i].gameObject.SetActive(locked);
            if(locked) Target(type).interactable=false;
            locks[i].rectTransform.sizeDelta=new Vector2(78,78);
            var graphic=Target(type).GetComponent<Image>();
            graphic.color=locked ? new Color(.48f,.42f,.35f,1) : Color.white;
            var icon=progress.ui.gameplayPanel.Find(type==BoosterType.Hint ? "Icon__Hint" : "Icon__Shuffle");
            if(icon) icon.GetComponent<Image>().color=locked ? new Color(.42f,.37f,.31f) : Color.white;
            string prefix=type==BoosterType.Hint ? "Hint" : "Shuffle";
            var count=progress.ui.gameplayPanel.Find(prefix+"Count");
            var badge=progress.ui.gameplayPanel.Find(prefix+"Badge");
            if(count) count.gameObject.SetActive(!locked);
            if(badge) badge.gameObject.SetActive(!locked);
        }
    }

    public IEnumerator Receive(BoosterType type)
    {
        bool repeatDemo=Application.isEditor && replayInEditor &&
            progress.grid.levelIndex+1==(type==BoosterType.Hint ? 3 : 6);
        if(EconomyStore.IsBoosterUnlocked(type) && !repeatDemo) yield break;
        busy=true;
        requested=false;
        foreach(Transform child in panel.transform)
            child.gameObject.SetActive(child!=flying.transform && child!=burst.transform);
        panel.GetComponent<Image>().color=new Color(.045f,.018f,.008f,.94f);
        claim.interactable=true;
        hero.sprite=Resources.Load<Sprite>("ActivityIcons/"+type);
        flying.sprite=hero.sprite;
        amount.text=type==BoosterType.Hint ? "+3 HINTS" : "+3 SHUFFLES";
        panel.SetActive(true);
        hero.gameObject.SetActive(true);
        // TeachLevel owns the timer pause throughout both the gift and the lesson.
        while(!requested)
        {
            for(int i=0;i<sparkles.Length;i++)
            {
                float shine=.5f+.5f*Mathf.Sin(Time.unscaledTime*4+i*1.7f);
                sparkles[i].localScale=Vector3.one*(.55f+.65f*shine);
                sparkles[i].localRotation=Quaternion.Euler(0,0,Time.unscaledTime*12+i*23);
                sparkles[i].GetComponent<Image>().color=new Color(1,.94f,.65f,.3f+.7f*shine);
            }
            yield return null;
        }
        claim.interactable=false;
        foreach(Transform child in panel.transform)
            child.gameObject.SetActive(child==flying.transform || child==burst.transform);
        panel.GetComponent<Image>().color=new Color(.13f,.065f,.035f,.42f);
        flying.gameObject.SetActive(true);
        var target=(RectTransform)Target(type).transform;
        target.GetWorldCorners(corners);
        var end=(Vector2)((RectTransform)panel.transform).InverseTransformPoint((corners[0]+corners[2])*.5f)
            -((RectTransform)panel.transform).rect.center;
        var flyer=flying.rectTransform;
        flyer.anchorMin=flyer.anchorMax=flyer.pivot=new Vector2(.5f,.5f);
        Vector2 start=new Vector2(0,40);
        Vector2 control=(start+end)*.5f+Vector2.up*240;
        float age=0;
        while(age<.85f)
        {
            float t=Mathf.SmoothStep(0,1,age/.85f);
            flyer.anchoredPosition=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
            flyer.sizeDelta=Vector2.one*Mathf.Lerp(356,76,t);
            age+=Time.unscaledDeltaTime;
            yield return null;
        }
        flying.gameObject.SetActive(false);
        var padlock=locks[type==BoosterType.Hint ? 0 : 1];
        age=0;
        while(age<.4f)
        {
            float t=age/.4f;
            padlock.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*4)*16*(1-t));
            age+=Time.unscaledDeltaTime;
            yield return null;
        }
        padlock.rectTransform.localRotation=Quaternion.identity;
        burst.gameObject.SetActive(true);
        burst.rectTransform.anchorMin=burst.rectTransform.anchorMax=burst.rectTransform.pivot=new Vector2(.5f,.5f);
        burst.rectTransform.anchoredPosition=end;
        age=0;
        while(age<.38f)
        {
            float t=age/.38f;
            burst.rectTransform.localScale=Vector3.one*Mathf.Lerp(.5f,1.65f,t);
            burst.color=new Color(1,1,.75f,Mathf.Sin(t*Mathf.PI));
            padlock.rectTransform.localScale=Vector3.one*(1+.2f*Mathf.Sin(t*Mathf.PI));
            age+=Time.unscaledDeltaTime;
            yield return null;
        }
        EconomyStore.ClaimFirstBooster(type);
        if(type==BoosterType.Hint) hintRevealed=true;else shuffleRevealed=true;
        Target(type).interactable=true;
        Cleanup();
    }

    private void StateChanged(GameState state)
    {
        if(state!=GameState.Playing && busy) Cleanup();
    }

    private void Cleanup()
    {
        busy=false;
        if(panel) panel.SetActive(false);
        if(locks!=null)
            foreach(var image in locks)
            {
                image.rectTransform.localRotation=Quaternion.identity;
                image.rectTransform.localScale=Vector3.one;
            }
        if(progress) RefreshLocks();
    }

    private void OnDestroy()
    {
        if(game) {game.LevelStarted-=BeginLevel;game.StateChanged-=StateChanged;}
        EconomyStore.InventoryChanged-=RefreshLocks;
    }
}
