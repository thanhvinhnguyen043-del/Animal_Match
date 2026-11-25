using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(350)]
public sealed class CafeVictoryUI : MonoBehaviour
{
    private ProgressUI progress;
    private CacaoUIController controller;
    private GameManager game;
    private RectTransform cup;
    private RectTransform[] sparkles;
    private Coroutine celebration;

    private RectTransform Node(Transform root,string name,float x,float y,float w,float h)
    {
        var saved=root.Find(name);
        var rect=saved ? (RectTransform)saved : new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(root,false);
        GameplayHudStyle.Place(rect,x,y,w,h);
        return rect;
    }
    private TextMeshProUGUI Label(Transform root,string name,string words,float x,float y,float w,float h,float size,Color tint)
    {
        var rect=Node(root,name,x,y,w,h);
        var text=rect.GetComponent<TextMeshProUGUI>();
        if(!text)text=rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=GameTypography.Current.font;
        text.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;
        text.text=words;text.color=tint;text.fontSize=text.fontSizeMax=size;
        text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
        return text;
    }
    private void Move(Transform node,float x,float y,float w,float h)
    {GameplayHudStyle.Place((RectTransform)node,x,y,w,h);}
    public void PrepareSceneView()
    {
        progress=GetComponent<ProgressUI>();
        controller=GetComponent<CacaoUIController>();
        var root=controller.winPanel.transform;
        var existing=root.Find("BrewedCup");
        if(existing)
        {
            cup=(RectTransform)existing;
            sparkles=new RectTransform[12];
            for(int i=0;i<12;i++)sparkles[i]=(RectTransform)root.Find("VictorySparkle"+i);
            FinishLayout(root);
            return;
        }
        var keep=new HashSet<Transform>{
            progress.grid.textTitle.transform,controller.winLevelText.transform,
            controller.winScoreText.transform,controller.winRewardText.transform,
            controller.bonusRewardText.transform,progress.resultStars.transform,
            progress.resultNotice.transform,progress.resultRecords.transform,
            progress.grid.btnNextLevel.transform
        };
        foreach(Transform child in root)
            if(!keep.Contains(child) && child.name!="Icon__Home" && child.name!="Icon__Retry")
                child.gameObject.SetActive(false);
        var shadeRect=Node(root,"VictoryShade",0,0,1920,1080);
        var shade=shadeRect.gameObject.AddComponent<Image>();
        shade.color=new Color(.045f,.018f,.008f,.94f);shade.raycastTarget=true;
        shade.transform.SetAsFirstSibling();
        var halo=Node(root,"VictoryHalo",490,115,940,720).gameObject.AddComponent<RewardHaloGraphic>();
        halo.raycastTarget=false;halo.transform.SetSiblingIndex(1);
        cup=Node(root,"BrewedCup",715,275,490,330);
        var cupImage=cup.gameObject.AddComponent<Image>();
        cupImage.sprite=Resources.Load<Sprite>("CoffeeEffects/FinishedCup");
        cupImage.preserveAspect=true;cupImage.raycastTarget=false;
        cup.pivot=new Vector2(.5f,.5f);cup.anchoredPosition+=new Vector2(245,-165);
        var cream=new Color(1,.96f,.82f);
        var cocoa=new Color(.39f,.19f,.08f);
        Move(progress.grid.textTitle.transform,510,75,900,85);
        progress.grid.textTitle.color=cream;progress.grid.textTitle.fontSizeMax=66;
        progress.grid.textTitle.alignment=TextAlignmentOptions.Center;
        progress.grid.textTitle.text="FRESHLY BREWED!";
        Move(controller.winLevelText.transform,600,165,720,45);
        controller.winLevelText.color=cream;
        Move(progress.resultStars.transform,735,215,450,100);
        Move(progress.resultNotice.transform,520,610,880,42);progress.resultNotice.color=cream;
        RewardFrameArt.Create(root,"ScoreCard","Common",530,675,410,100).transform.SetSiblingIndex(2);
        RewardFrameArt.Create(root,"RewardCard","Common",980,675,410,100).transform.SetSiblingIndex(3);
        Label(root,"ScoreLabel","SCORE",550,687,370,25,25,cocoa);
        Label(root,"RewardLabel","COINS EARNED",1000,687,370,25,25,cocoa);
        Move(controller.winScoreText.transform,550,717,370,45);
        controller.winScoreText.color=cocoa;controller.winScoreText.fontSizeMax=38;
        Move(controller.winRewardText.transform,1000,717,370,45);
        controller.winRewardText.color=cocoa;controller.winRewardText.fontSizeMax=38;
        Move(progress.resultRecords.transform,510,790,900,62);progress.resultRecords.color=cream;
        Move(controller.bonusRewardText.transform,610,858,700,32);controller.bonusRewardText.color=cream;
        Move(progress.grid.btnNextLevel.transform,710,907,500,112);
        var buttonImage=progress.grid.btnNextLevel.GetComponent<Image>();
        buttonImage.sprite=Resources.Load<Sprite>("ActivityIcons/Primary");
        buttonImage.type=Image.Type.Simple;buttonImage.preserveAspect=true;
        var home=root.Find("Icon__Home");if(home)Move(home,1285,930,72,72);
        var retry=root.Find("Icon__Retry");if(retry)Move(retry,563,930,72,72);
        sparkles=new RectTransform[12];
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI*2/12;
            var star=Node(root,"VictorySparkle"+i,945+Mathf.Cos(angle)*350,440+Mathf.Sin(angle)*230,30,30);
            var image=star.gameObject.AddComponent<Image>();
            image.sprite=Resources.Load<Sprite>("StarBar/StarGold");image.preserveAspect=true;image.raycastTarget=false;
            star.pivot=new Vector2(.5f,.5f);sparkles[i]=star;
        }
        FinishLayout(root);
    }
    private void FinishLayout(Transform root)
    {
        var tray=Node(root,"VictoryResultsTray",480,350,960,665);
        var image=tray.GetComponent<Image>();
        if(!image) image=tray.gameObject.AddComponent<Image>();
        image.sprite=Resources.Load<Sprite>("CoffeeEffects/VictoryTray");
        image.type=Image.Type.Sliced;
        image.preserveAspect=false;
        image.pixelsPerUnitMultiplier=2.2f;
        image.raycastTarget=false;
        tray.SetSiblingIndex(2);
        var summaryFrame=RewardFrameArt.Create(root,"VictorySummaryFrame","Common",525,595,870,255);
        summaryFrame.pixelsPerUnitMultiplier=8;
        summaryFrame.transform.SetSiblingIndex(3);
        var cocoa=new Color(.39f,.19f,.08f);
        var header=RewardFrameArt.Create(root,"VictoryHeader","Common",590,285,740,105);
        header.transform.SetSiblingIndex(4);
        Move(progress.grid.textTitle.transform,610,304,700,60);
        progress.grid.textTitle.color=cocoa;
        progress.grid.textTitle.fontSizeMax=44;
        Move(controller.winLevelText.transform,690,395,540,43);
        controller.winLevelText.color=cocoa;
        Move(progress.resultStars.transform,735,445,450,105);
        Move(progress.resultNotice.transform,600,556,720,36);
        progress.resultNotice.color=cocoa;
        Move(cup,775,100,370,250);
        cup.pivot=new Vector2(.5f,.5f);
        cup.anchoredPosition+=new Vector2(185,-125);
        var halo=root.Find("VictoryHalo");
        if(halo)Move(halo,600,20,720,500);
        for(int i=0;i<sparkles.Length;i++)
        {
            float angle=i*Mathf.PI*2/sparkles.Length;
            float size=28+i%3*9;
            Move(sparkles[i],960+Mathf.Cos(angle)*245,190+Mathf.Sin(angle)*100,size,size);
            sparkles[i].pivot=new Vector2(.5f,.5f);
            sparkles[i].GetComponent<Image>().enabled=false;
            var light=Node(sparkles[i],"Glint",0,0,size,size);
            var glint=light.GetComponent<RewardGlintGraphic>();
            if(!glint)glint=light.gameObject.AddComponent<RewardGlintGraphic>();
            glint.raycastTarget=false;
        }
        var score=root.Find("ScoreCard");
        if(score) Move(score,555,615,360,92);
        var rewardCard=root.Find("RewardCard");
        if(rewardCard) Move(rewardCard,1005,615,360,92);
        Move(root.Find("ScoreLabel"),575,624,320,25);
        Move(root.Find("RewardLabel"),1025,624,320,25);
        Move(controller.winScoreText.transform,575,655,320,42);
        Move(controller.winRewardText.transform,1025,655,320,42);
        Move(progress.resultRecords.transform,560,735,800,64);
        progress.resultRecords.color=cocoa;
        Move(controller.bonusRewardText.transform,610,810,700,28);
        controller.bonusRewardText.color=cocoa;
        var home=root.Find("Icon__Home");if(home)Move(home,1290,884,72,72);
        var retry=root.Find("Icon__Retry");if(retry)Move(retry,558,884,72,72);
        Move(progress.grid.btnNextLevel.transform,765,855,390,130);
        var label=controller.continueText.rectTransform;
        label.SetParent(progress.grid.btnNextLevel.transform,false);
        label.anchorMin=Vector2.zero;
        label.anchorMax=Vector2.one;
        label.offsetMin=new Vector2(18,10);
        label.offsetMax=new Vector2(-18,-10);
        controller.continueText.alignment=TextAlignmentOptions.Center;
        var reward=root.Find("RewardCard");
        if(reward) RewardFrameArt.Apply(reward.GetComponent<Image>(),"Common");
        // These nodes can have been hidden by a previous gameplay or editor preview.
        foreach(var node in new[]{progress.grid.textTitle.transform,controller.winLevelText.transform,
            controller.winScoreText.transform,controller.winRewardText.transform,
            progress.resultStars.transform,progress.resultNotice.transform,progress.resultRecords.transform,
            progress.grid.btnNextLevel.transform,controller.continueText.transform})
            node.gameObject.SetActive(true);
        progress.grid.btnNextLevel.transform.SetAsLastSibling();
        if(!Application.isPlaying) PreparePreviewContent();
    }

    private void PreparePreviewContent()
    {
        // Scene-only example values; live results still come from the completed level.
        progress.grid.textTitle.text="FRESHLY BREWED!";
        controller.winLevelText.text="LEVEL 1 COMPLETE";
        controller.winScoreText.text="1,250";
        controller.winRewardText.text="+50";
        controller.continueText.text="CONTINUE";
        controller.bonusRewardText.text="";
        progress.resultStars.UsePngStars();
        progress.resultStars.SetStars(3);
        progress.resultNotice.text="NEW MEDAL RECORD!";
        progress.resultRecords.text="BEST SCORE 1,250   |   BEST TIME 01:30\nBEST COMBO x5   |   BEST MEDALS 3 / 3";
    }

    private void Start()
    {
        PrepareSceneView();
        game=progress.grid.Session;
        game.GameEnded+=Ended;
        game.StateChanged+=StateChanged;
    }
    private void Ended(bool won)
    {
        if(!won)return;
        if(celebration!=null)StopCoroutine(celebration);
        celebration=StartCoroutine(Celebrate());
    }
    private IEnumerator Celebrate()
    {
        int earned=progress.grid.LastResult.stars;
        progress.resultStars.SetStars(0);
        float age=0;
        while(age<.35f)
        {
            cup.localScale=Vector3.one*Mathf.Lerp(.7f,1,Mathf.SmoothStep(0,1,age/.35f));
            age+=Time.unscaledDeltaTime;yield return null;
        }
        cup.localScale=Vector3.one;
        for(int i=1;i<=earned;i++)
        {
            progress.resultStars.SetStarsAnimated(i,earned);
            yield return new WaitForSecondsRealtime(.28f);
        }
        celebration=null;
    }
    private void Update()
    {
        if(!game || game.State!=GameState.Win)return;
        for(int i=0;i<sparkles.Length;i++)
        {
            float t=.5f+.5f*Mathf.Sin(Time.unscaledTime*3.2f+i*1.9f);
            sparkles[i].localScale=Vector3.one*Mathf.Lerp(.35f,1.3f,t);
            sparkles[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*.8f+i)*12);
            sparkles[i].GetComponentInChildren<RewardGlintGraphic>().color=new Color(1,.96f,.75f,.12f+.88f*t);
        }
    }
    private void StateChanged(GameState state)
    {
        if(state==GameState.Win)return;
        if(celebration!=null)StopCoroutine(celebration);
        celebration=null;if(cup)cup.localScale=Vector3.one;
    }
    private void OnDestroy()
    {
        if(game){game.GameEnded-=Ended;game.StateChanged-=StateChanged;}
    }
}
