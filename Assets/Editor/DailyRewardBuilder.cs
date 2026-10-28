using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class DailyRewardBuilder
{
    private const string ArtPath="Assets/Art/DailyRewards/";
    private static readonly Color Brown=new Color(.42f,.235f,.13f);
    private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+name+".png");
    private static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/"+name+".png");
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string text,float x,float y,float w,float h,float size,bool outlined=false)
    {
        var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.font=GameTypography.Current.font;
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao "+(outlined ? "Heading" : "Body")+".mat");
        label.color=outlined ? new Color(1,.99f,.92f) : Brown;label.text=text;label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-3;
        label.enableAutoSizing=true;label.enableWordWrapping=false;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private static RoundedImage Round(Transform parent,string name,float x,float y,float w,float h,Color color,float radius)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<RoundedImage>();image.color=color;image.cornerRadius=radius;image.raycastTarget=false;return image;
    }
    private static Button Button(Image image,UnityAction action,GridManager grid)
    {
        image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        UnityEventTools.AddPersistentListener(button.onClick,action);image.gameObject.AddComponent<UIButtonClickSound>().clickSound=grid.sfxClick;return button;
    }
    private static GameObject Today(Transform parent,float x,float y)
    {
        var pill=Round(parent,"Today",x,y,83,32,new Color(.42f,.64f,.28f),16);
        var label=Text(pill.transform,"Label","TODAY",0,0,83,32,21);label.color=new Color(1,.99f,.92f);return pill.gameObject;
    }
    private static DailyRewardCard Card(Transform parent,int day)
    {
        bool weekly=day==7;float x=weekly ? 1110 : 140+(day-1)%3*320,y=weekly ? 235 : 235+(day-1)/3*300;
        float width=weekly ? 510 : 290,height=weekly ? 550 : 250;
        var root=Image(parent,"Day_"+day,Art(weekly ? "Card__Weekly" : "Card__Upcoming"),x,y,width,height);
        var card=new DailyRewardCard {background=root};
        var title=Text(root.transform,"Day","DAY "+day,weekly ? 100 : 24,weekly ? 23 : 12,weekly ? 310 : 160,weekly ? 58 : 40,weekly ? 42 : 30);
        if(!weekly) title.alignment=TextAlignmentOptions.MidlineLeft;
        else
        {
            Text(root.transform,"WeeklyTitle","THE WEEKLY TREAT",45,70,420,39,25).color=new Color(.52f,.33f,.62f);
            Image(root.transform,"Chest",Art("Reward__Chest"),108,107,294,294);
            var tray=Round(root.transform,"RewardTray",35,386,440,125,new Color(1,.945f,.824f),25);
            var outline=tray.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.83f,.65f,.42f);outline.effectDistance=new Vector2(2,-2);
        }
        card.rewards=Rect(root.transform,"Rewards",0,0,width,height).gameObject.AddComponent<CanvasGroup>();card.rewards.blocksRaycasts=false;
        card.icons=new Image[3];card.amounts=new TextMeshProUGUI[3];
        Sprite[] icons={Art("Reward__Coins"),Icon("Icon__Hint"),Icon("Icon__Shuffle")};
        for(int i=0;i<3;i++)
        {
            card.icons[i]=Image(card.rewards.transform,"RewardIcon"+i,icons[i],0,0,100,100);
            card.amounts[i]=Text(card.rewards.transform,"Amount"+i,"",0,0,150,40,weekly ? 28 : 29);
        }
        card.claimed=Image(root.transform,"Claimed",Art("Mark__Claimed"),weekly ? 445 : 235,weekly ? 24 : 16,38,38).gameObject;
        card.locked=Image(root.transform,"Locked",Art("Mark__Lock"),weekly ? 458 : 243,weekly ? 28 : 19,25,30).gameObject;
        card.today=Today(root.transform,weekly ? 408 : 185,weekly ? 26 : 18);
        card.status=Text(root.transform,"State","COMING UP",12,208,width-24,30,21);card.status.gameObject.SetActive(!weekly);
        return card;
    }
    [MenuItem("Tools/Build Daily Rewards")]
    public static void Build()
    {
        var ui=Object.FindObjectOfType<UIManager>();var grid=Object.FindObjectOfType<GridManager>();
        var previous=ui.GetComponent<DailyRewardUI>();
        if(previous) {if(previous.panel) Object.DestroyImmediate(previous.panel);Object.DestroyImmediate(previous);}
        var oldButton=ui.mainMenuPanel.Find("DailyRewardsButton");if(oldButton) Object.DestroyImmediate(oldButton.gameObject);
        foreach(string path in Directory.GetFiles(ArtPath,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
            importer.maxTextureSize=path.Contains("Panel__") ? 2048 : path.Contains("Mark__") ? 128 : path.Contains("Card__") ? 1024 : 512;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        var config=EconomyStore.Config;
        if(config.dailyRewards==null || config.dailyRewards.Length!=7) config.dailyRewards=DailyRewardSchedule.CreateDefaults();
        EditorUtility.SetDirty(config);
        var daily=ui.gameObject.AddComponent<DailyRewardUI>();daily.ui=ui;
        daily.menuInput=ui.mainMenuPanel.GetComponent<CanvasGroup>();if(!daily.menuInput) daily.menuInput=ui.mainMenuPanel.gameObject.AddComponent<CanvasGroup>();
        daily.upcomingCard=Art("Card__Upcoming");daily.todayCard=Art("Card__Today");daily.claimedCard=Art("Card__Claimed");
        var menu=Image(ui.mainMenuPanel,"DailyRewardsButton",null,280,45,225,245);menu.color=Color.clear;
        daily.menuButton=Button(menu,daily.Open,grid);
        var gift=Image(menu.transform,"Gift",Art("Reward__Chest"),20,0,185,185);daily.menuButton.targetGraphic=gift;
        Text(menu.transform,"Caption","DAILY GIFTS",0,176,225,46,30,true);
        var badge=Round(menu.transform,"Available",165,8,43,43,new Color(.42f,.67f,.23f),22);daily.availableBadge=badge.gameObject;
        Text(badge.transform,"Mark","!",0,0,43,43,30,true);
        var panel=Rect(ui.transform,"DailyRewards",0,0,1920,1080);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        daily.panel=panel.gameObject;var canvas=panel.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=350;panel.gameObject.AddComponent<GraphicRaycaster>();
        var backdrop=Image(panel,"ModalBackdrop",null,0,0,1920,1080);backdrop.color=new Color(.12f,.065f,.025f,.53f);backdrop.raycastTarget=true;
        var popup=Rect(panel,"Popup",80,45,1760,990);
        Image(popup,"Panel",Art("Panel__DailyWide"),0,0,1760,990);
        Image(popup,"Header",Icon("Panel__Header"),520,-4,720,240);
        Text(popup,"Title","DAILY REWARDS",570,72,620,70,50,true);
        daily.closeButton=Button(Image(popup,"Close",Icon("Icon__Close"),1623,78,92,92),daily.Close,grid);
        Text(popup,"Subtitle","A little gift, every day. A big treat on Day 7!",270,193,1220,44,26);
        daily.cards=new DailyRewardCard[7];for(int day=1;day<=7;day++) daily.cards[day-1]=Card(popup,day);
        daily.progress=Text(popup,"Progress","0 / 7 COLLECTED",145,839,350,40,27);daily.progress.alignment=TextAlignmentOptions.MidlineLeft;
        daily.progressDots=new Image[7];for(int i=0;i<7;i++) daily.progressDots[i]=Round(popup,"ProgressDot"+(i+1),147+i*38,889,24,24,new Color(.89f,.8f,.65f),12);
        daily.claimButton=Button(Image(popup,"Claim",Icon("Button__Primary"),660,817.167f,440,146.667f),daily.Claim,grid);
        daily.claimLabel=Text(daily.claimButton.transform,"Label","CLAIM REWARD",16,34,408,70,36,true);
        Image(popup,"Timer",Icon("Icon__Timer"),1230,851,38,38);
        daily.reminder=Text(popup,"Reminder","NEW GIFTS EVERY DAY",1280,842,420,48,25);
        daily.notice=Text(popup,"Notice","",360,941,1040,38,24);
        panel.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
