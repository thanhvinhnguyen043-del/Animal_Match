using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ProgressUIBuilder
{
    private static readonly Color Ink=new Color(.42f,.235f,.13f);
    private static RectTransform Place(Transform target,float x,float y,float width,float height)
    {
        var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return Place(rect,x,y,w,h);}
    private static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/"+name+".png");
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,bool heading=false)
    {
        var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.font=GameTypography.Current.font;
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao "+(heading ? "Heading" : "Body")+".mat");
        label.text=value;label.color=heading ? new Color(1,.99f,.92f) : Ink;label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-2;
        label.enableAutoSizing=true;label.enableWordWrapping=true;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;}
    private static RoundedImage Round(Transform parent,string name,float x,float y,float w,float h,Color color,float radius=20)
    {var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<RoundedImage>();image.color=color;image.cornerRadius=radius;image.raycastTarget=false;return image;}
    private static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,UnityAction action,GridManager grid,bool primary=false)
    {
        var image=Image(parent,name,Art(primary ? "Button__Primary" : "Button__Secondary"),x,y,w,h);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.disabledColor=new Color(.78f,.76f,.7f,.8f);button.colors=colors;
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action);
        button.gameObject.AddComponent<UIButtonClickSound>().clickSound=grid.sfxClick;
        var text=Text(image.transform,"Label",caption,14,8,w-28,h-16,28,true);
        text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=new Vector2(14,8);text.rectTransform.offsetMax=new Vector2(-14,-8);
        return button;
    }
    private static StarRatingGraphic Stars(Transform parent,string name,float x,float y,float w,float h)
    {var stars=Rect(parent,name,x,y,w,h).gameObject.AddComponent<StarRatingGraphic>();stars.SetStars(0);return stars;}
    private static void Remove(Transform parent,string name) {var old=parent.Find(name);if(old) Object.DestroyImmediate(old.gameObject);}
    private static void ConfigureLevels()
    {
        var database=Resources.Load<LevelDatabase>("LevelDatabase");
        for(int index=0;index<database.levels.Count;index++)
        {
            var level=database.levels[index];if(!string.IsNullOrEmpty(level.id)) continue;
            level.id="animal-match-"+(index+1).ToString("000");
            int animals=0,water=0,stone=0;
            foreach(var cell in level.cells) if(cell!=null && !cell.empty) {animals++;if(cell.state==TileState.Ice) water++;if(cell.state==TileState.Barrier) stone++;}
            if(level.mode==LevelLayoutMode.Automatic) {animals=level.columns*level.rows;water=Mathf.RoundToInt(animals*level.iceChance);stone=Mathf.RoundToInt(animals*level.barrierChance);}
            StarGoal goal=water>0 && stone>0 ? index>10 && index%3==0 ? StarGoal.WaterAndStone : index%2==0 ? StarGoal.Stone : StarGoal.Water : water>0 ? StarGoal.Water : stone>0 ? StarGoal.Stone : StarGoal.Combo;
            int totalTurns=Mathf.CeilToInt((animals+water)/2f);
            level.challenge=new LevelChallenge {goal=goal,timeRemainingFraction=.3f,comboTarget=index==0 ? 3 : 5,
                turnLimit=goal==StarGoal.Combo ? index==0 ? 8 : 12 : Mathf.Max(6,Mathf.CeilToInt(totalTurns*.75f))};
        }
        EditorUtility.SetDirty(database);
    }
    [MenuItem("Tools/Build Stars And Achievements")]
    public static void Build()
    {
        var ui=Object.FindObjectOfType<UIManager>();var grid=Object.FindObjectOfType<GridManager>();var controller=ui.GetComponent<CacaoUIController>();
        var previous=ui.GetComponent<ProgressUI>();if(previous) Object.DestroyImmediate(previous);
        Remove(ui.transform,"Achievements");Remove(ui.mainMenuPanel,"AchievementMenuButton");Remove(ui.mainMenuPanel,"CareerStarSummary");
        Remove(ui.levelSelectPanel,"LevelStarSummary");Remove(ui.gameplayPanel,"ProgressGoals");
        foreach(string name in new[]{"ProgressResultStars","ProgressResultRecords","ProgressResultNotice"}) Remove(controller.winPanel.transform,name);
        ConfigureLevels();var progress=ui.gameObject.AddComponent<ProgressUI>();progress.ui=ui;progress.grid=grid;
        progress.menuInput=ui.mainMenuPanel.GetComponent<CanvasGroup>();if(!progress.menuInput) progress.menuInput=ui.mainMenuPanel.gameObject.AddComponent<CanvasGroup>();
        progress.menuButton=Button(ui.mainMenuPanel,"AchievementMenuButton","ACHIEVEMENTS",65,355,440,110,progress.Open,grid);
        var badge=Round(progress.menuButton.transform,"Available",405,-5,42,42,new Color(.35f,.66f,.27f));progress.availableBadge=badge.gameObject;
        Text(badge.transform,"Mark","!",0,0,42,42,30,true);badge.gameObject.SetActive(false);
        progress.menuStars=Text(ui.mainMenuPanel,"CareerStarSummary","STARS  0 / 0",65,475,440,45,30,true);
        progress.levelStars=Text(ui.levelSelectPanel,"LevelStarSummary","STARS  0 / 0",710,325,500,42,30);
        var selection=ui.levelSelectPanel.GetComponent<LevelSelectionUI>();var template=selection.levelButtonTemplate.transform;
        Remove(template,"BestStars");Stars(template,"BestStars",18,112,136,40);
        var status=template.Find("LevelStatus");if(status) Place(status,0,155,172,28);
        BuildGoals(progress);BuildResults(progress,controller);BuildAchievements(progress);
        progress.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);AssetDatabase.SaveAssets();
    }
    private static void BuildGoals(ProgressUI progress)
    {
        var root=Rect(progress.ui.gameplayPanel,"ProgressGoals",1580,625,300,400);
        var background=Round(root,"Backing",0,0,300,400,new Color(1,.97f,.86f,.96f),25);
        var border=background.gameObject.AddComponent<Outline>();border.effectColor=new Color(.77f,.52f,.27f);border.effectDistance=new Vector2(2,-2);
        progress.goalTitle=Text(root,"Title","STAR GOALS",15,14,270,42,29);
        progress.goalStars=Stars(root,"PotentialStars",72,58,156,42);
        progress.timeGoal=Text(root,"TimeGoal","2 STARS",20,106,260,70,25);
        progress.challengeGoal=Text(root,"ChallengeGoal","3 STARS",20,184,260,144,24);
        progress.turns=Text(root,"Turns","TURN 0 / 12",15,343,270,42,26);
    }
    private static void BuildResults(ProgressUI progress,CacaoUIController controller)
    {
        var root=controller.winPanel.transform;
        var subtitle=root.Find("Text__Board_cleared_Nice_matching_");if(subtitle) subtitle.gameObject.SetActive(false);
        progress.resultStars=Stars(root,"ProgressResultStars",825,398,270,62);
        progress.resultNotice=Text(root,"ProgressResultNotice","",620,459,680,30,24);
        progress.resultRecords=Text(root,"ProgressResultRecords","",630,596,660,64,24);
        Place(controller.bonusRewardText.transform,650,662,620,30);
        var button=controller.grid.btnNextLevel;Place(button.transform,720,700,480,110);button.GetComponent<Image>().preserveAspect=false;
        controller.continueText.transform.SetParent(button.transform,false);
        controller.continueText.rectTransform.anchorMin=Vector2.zero;controller.continueText.rectTransform.anchorMax=Vector2.one;
        controller.continueText.rectTransform.offsetMin=new Vector2(14,5);controller.continueText.rectTransform.offsetMax=new Vector2(-14,-5);
        controller.continueText.fontSize=controller.continueText.fontSizeMax=42;controller.continueText.fontSizeMin=36;controller.continueText.enableAutoSizing=true;
    }
    private static void BuildAchievements(ProgressUI progress)
    {
        var root=Rect(progress.ui.transform,"Achievements",0,0,1920,1080);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
        progress.panel=root.gameObject;var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=360;root.gameObject.AddComponent<GraphicRaycaster>();
        var shade=Image(root,"Backdrop",null,0,0,1920,1080);shade.color=new Color(.12f,.065f,.025f,.6f);shade.raycastTarget=true;
        var popup=Rect(root,"Popup",80,35,1760,990);
        Image(popup,"Frame",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/DailyRewards/Panel__DailyWide.png"),0,0,1760,990);
        Image(popup,"Header",Art("Panel__Header"),520,-4,720,240);
        Text(popup,"Title","ACHIEVEMENTS",570,72,620,70,50,true);
        progress.closeButton=Button(popup,"Close","",1630,78,90,90,progress.Close,progress.grid);progress.closeButton.GetComponent<Image>().sprite=Art("Icon__Close");
        progress.summary=Text(popup,"Summary","",400,188,960,38,27);
        progress.filters=new Button[6];string[] names={"ALL","STARS","COMBOS","PLAY","COLLECTION","READY"};
        for(int index=0;index<names.Length;index++) progress.filters[index]=Button(popup,"Filter"+names[index],names[index],120+index*254,233,240,58,null,progress.grid);
        progress.cards=new AchievementCard[6];
        for(int index=0;index<6;index++)
        {
            var cardRoot=Round(popup,"AchievementCard"+index,index%2==0 ? 120 : 910,295+index/2*168,730,155,new Color(1,.975f,.9f),22);
            var outline=cardRoot.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.73f,.52f,.34f);outline.effectDistance=new Vector2(2,-2);
            var card=new AchievementCard {root=cardRoot.gameObject};
            card.title=Text(cardRoot.transform,"Title","",18,12,500,36,29);card.title.alignment=TextAlignmentOptions.MidlineLeft;
            card.description=Text(cardRoot.transform,"Description","",18,47,490,48,24);card.description.alignment=TextAlignmentOptions.MidlineLeft;
            card.reward=Text(cardRoot.transform,"Reward","",18,103,490,28,23);card.reward.alignment=TextAlignmentOptions.MidlineLeft;
            card.progress=Text(cardRoot.transform,"Progress","",535,15,177,32,23);
            card.button=Button(cardRoot.transform,"Claim","CLAIM",531,62,180,77,null,progress.grid,true);card.action=card.button.GetComponentInChildren<TextMeshProUGUI>();card.action.fontSize=card.action.fontSizeMax=25;card.action.fontSizeMin=21;
            var track=Round(cardRoot.transform,"Track",20,139,485,10,new Color(.85f,.79f,.68f),5);
            card.fill=Round(track.transform,"Fill",0,0,485,10,new Color(.39f,.77f,.67f),5);
            card.fill.rectTransform.anchorMin=Vector2.zero;card.fill.rectTransform.anchorMax=Vector2.one;card.fill.rectTransform.offsetMin=card.fill.rectTransform.offsetMax=Vector2.zero;
            progress.cards[index]=card;
        }
        progress.previousButton=Button(popup,"Previous","PREV",130,826,190,62,progress.Previous,progress.grid);
        progress.pageLabel=Text(popup,"Page","1 / 1",330,836,175,42,30);
        progress.nextButton=Button(popup,"Next","NEXT",515,826,190,62,progress.Next,progress.grid);
        progress.milestoneButton=Button(popup,"Milestone","BONUS CHEST",1040,820,580,88,progress.ClaimMilestone,progress.grid,true);
        progress.milestoneLabel=progress.milestoneButton.GetComponentInChildren<TextMeshProUGUI>();
        Place(progress.milestoneLabel.transform,15,10,550,32);
        progress.milestoneProgress=Text(progress.milestoneButton.transform,"MilestoneProgress","",12,48,556,28,21);
        progress.notice=Text(popup,"Notice","",135,792,1485,28,23);
    }
}
