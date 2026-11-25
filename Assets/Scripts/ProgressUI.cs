using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class AchievementCard
{
    public GameObject root;
    public TextMeshProUGUI title,description,reward,progress,action;
    public Image fill,icon;
    public Button button;
    [NonSerialized] public string id;
}

[DefaultExecutionOrder(120)]
public sealed class ProgressUI : MonoBehaviour
{
    public GridManager grid;
    public UIManager ui;
    public GameObject panel,availableBadge;
    public CanvasGroup menuInput;
    public Button menuButton,closeButton,previousButton,nextButton,milestoneButton;
    public Button[] filters;
    public AchievementCard[] cards;
    public ScrollRect achievementScroll;
    public TextMeshProUGUI menuStars,levelStars,summary,pageLabel,notice,milestoneLabel,milestoneProgress;
    public TextMeshProUGUI goalTitle,timeGoal,challengeGoal,turns,resultRecords,resultNotice;
    public StarRatingGraphic goalStars,resultStars;
    private readonly List<AchievementDefinition> visible=new List<AchievementDefinition>(64);
    // Catalog keys stay stable; the visible tab caption is MEDALS.
    private readonly string[] categories={"ALL","STARS","COMBOS","PLAY","COLLECTION","READY"};
    private GameManager game;
    private int filter,displayedSecond=-1;
    private bool menuBlocked,oldInteractable,oldRaycasts,goalsDirty;
    private Image starProgressFill;
    private float starProgress,targetStarProgress;
    private readonly Dictionary<AchievementMetric,Sprite> achievementIcons=new Dictionary<AchievementMetric,Sprite>();
    public bool IsOpen=>panel && panel.activeSelf;
    private void Start()
    {
        PrepareAchievementIcons();
        CafeFrameLayouts.Achievements(this);
        var sceneLayout=GetComponent<SceneUILayout>();
        if(sceneLayout && sceneLayout.starFill) starProgressFill=sceneLayout.starFill;
        else starProgressFill=PrepareSceneView();
        GameplayProgressStyle.Apply(this);
        if(!GetComponent<FirstBoosterRewardUI>()) gameObject.AddComponent<FirstBoosterRewardUI>();
        if(!GetComponent<MechanicSpotlightUI>()) gameObject.AddComponent<MechanicSpotlightUI>();
        if(!GetComponent<StarGoalTutorialUI>()) gameObject.AddComponent<StarGoalTutorialUI>();
        if(!GetComponent<LeaderboardUI>()) gameObject.AddComponent<LeaderboardUI>();
        if(!GetComponent<ActivityUI>()) gameObject.AddComponent<ActivityUI>();
        if(!GetComponent<PlayerAvatarUI>()) gameObject.AddComponent<PlayerAvatarUI>();
        if(!GetComponent<GameplayCompanionUI>()) gameObject.AddComponent<GameplayCompanionUI>();
        if(!GetComponent<CafeVictoryUI>()) gameObject.AddComponent<CafeVictoryUI>();
        if(!GetComponent<CoffeeMatchEffects>()) gameObject.AddComponent<CoffeeMatchEffects>();
        if(!GetComponent<CacaoArtSizing>()) gameObject.AddComponent<CacaoArtSizing>();
        game=grid.Session;game.StateChanged+=OnState;game.LevelStarted+=OnLevelStarted;game.GameEnded+=OnEnded;
        game.MatchResolved+=OnMatch;game.TileBroken+=OnBroken;game.TimeChanged+=OnTime;
        EconomyStore.InventoryChanged+=Refresh;
        for(int index=0;index<cards.Length;index++) {int card=index;cards[index].button.onClick.AddListener(()=>Claim(card));}
        for(int index=0;index<filters.Length;index++) {int category=index;filters[index].onClick.AddListener(()=>SetFilter(category));}
        panel.SetActive(false);Refresh();if(game.State==GameState.Playing) OnLevelStarted();
    }
    public void Open()
    {
        if(!game || ui.settingsPanel.gameObject.activeSelf || !game.OpenAchievements()) return;
        filter=0;panel.SetActive(true);panel.transform.SetAsLastSibling();notice.text="";
        if(menuInput) {oldInteractable=menuInput.interactable;oldRaycasts=menuInput.blocksRaycasts;menuInput.interactable=menuInput.blocksRaycasts=false;menuBlocked=true;}
        Refresh();ResetScroll();if(EventSystem.current) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
    }
    public void Close()
    {
        if(!IsOpen) return;panel.SetActive(false);RestoreInput();game.CloseAchievements();
        if(EventSystem.current) EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
    }
    private void RestoreInput()
    {if(menuBlocked && menuInput) {menuInput.interactable=oldInteractable;menuInput.blocksRaycasts=oldRaycasts;}menuBlocked=false;}
    private void OnState(GameState state)
    {
        if(state!=GameState.Achievements && IsOpen) {panel.SetActive(false);RestoreInput();}
        if(state==GameState.Menu) Refresh();
    }
    public void SetFilter(int value) {filter=Mathf.Clamp(value,0,categories.Length-1);Refresh();ResetScroll();}
    private void ResetScroll() {achievementScroll.StopMovement();achievementScroll.verticalNormalizedPosition=1;}
    public void Refresh()
    {
        var inventory=EconomyStore.Inventory;var career=inventory.career;var database=grid.levelDatabase;
        string stars="MEDALS  "+CareerStore.TotalStars(career)+" / "+(database ? database.levels.Count*3 : 0);
        menuStars.text=stars;levelStars.text=stars;
        int ready=0;
        foreach(var item in AchievementCatalog.All)
            if(!career.claimedAchievements.Contains(item.id) && AchievementCatalog.Value(item.metric,inventory)>=item.target) ready++;
        AchievementMilestone nextChest=null;
        foreach(var chest in AchievementCatalog.Milestones) if(!career.claimedMilestones.Contains(chest.target)) {nextChest=chest;break;}
        bool chestReady=nextChest!=null && career.claimedAchievements.Count>=nextChest.target;
        availableBadge.SetActive(ready>0 || chestReady);
        if(!IsOpen) return;
        visible.Clear();
        foreach(var item in AchievementCatalog.All)
        {
            bool claimed=career.claimedAchievements.Contains(item.id);
            if(filter==0 || filter==5 && !claimed && AchievementCatalog.Value(item.metric,inventory)>=item.target || filter>0 && filter<5 && item.category==categories[filter]) visible.Add(item);
        }
        CafeFrameLayouts.SizeAchievementList(this,visible.Count);
        summary.text=career.claimedAchievements.Count+" / "+AchievementCatalog.All.Count+" CLAIMED     "+ready+" READY";
        for(int index=0;index<filters.Length;index++) {var image=filters[index].GetComponent<Image>();image.color=Color.white;image.sprite=Resources.Load<Sprite>("TabButtons/"+(index==filter ? "Green" : "Blue"));}
        for(int index=0;index<cards.Length;index++)
        {
            var card=cards[index];int row=index;card.root.SetActive(row<visible.Count);card.id=null;if(row>=visible.Count) continue;
            var item=visible[row];card.id=item.id;int value=AchievementCatalog.Value(item.metric,inventory);bool claimed=career.claimedAchievements.Contains(item.id),complete=value>=item.target;
            card.icon.sprite=achievementIcons[item.metric];card.icon.enabled=card.icon.sprite;
            card.title.text=item.title;card.description.text=item.description;card.reward.text=RewardText(item.reward);
            // Rewards share the unified achievement list surface.
            card.progress.text=Mathf.Min(value,item.target).ToString("N0")+" / "+item.target.ToString("N0");
            card.fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(value/(float)item.target),1);
            card.fill.color=claimed ? new Color(.58f,.69f,.55f) : new Color(.39f,.77f,.67f);
            card.action.text=claimed ? "CLAIMED" : complete ? "CLAIM" : "IN PROGRESS";card.button.interactable=!claimed && complete;
        }
        milestoneButton.interactable=chestReady;
        milestoneLabel.text=nextChest==null ? "ALL CHESTS CLAIMED" : chestReady ? "CLAIM BONUS CHEST" : "BONUS CHEST AT "+nextChest.target;
        milestoneProgress.text=nextChest==null ? "All milestones completed!" : career.claimedAchievements.Count+" / "+nextChest.target+" CLAIMED  |  "+RewardText(nextChest.reward)+(nextChest.target==10 ? " + DREAMY NIGHTCAP" : "");
    }
    public Image PrepareSceneView()
    {
        PrepareMenuIcons();
        return GameplayHudStyle.Apply(this);
    }
    public void PrepareAchievementSceneView()
    {
        PrepareAchievementIcons();
        CafeFrameLayouts.Achievements(this);
        for(int i=0;i<cards.Length && i<AchievementCatalog.All.Count;i++)
        {
            var item=AchievementCatalog.All[i];var card=cards[i];
            card.title.text=item.title;card.description.text=item.description;card.reward.text=RewardText(item.reward);
            card.icon.sprite=achievementIcons[item.metric];card.progress.text="0 / "+item.target;
            card.action.text="IN PROGRESS";card.button.interactable=false;
        }
        CafeFrameLayouts.SizeAchievementList(this,AchievementCatalog.All.Count);
        summary.text="Complete goals, collect rewards and celebrate your progress.";
    }
    private void PrepareMenuIcons()
    {
        string[] buttons={"AchievementMenuButton","RoomRewardMenuButton","OpenStudio"};
        string[] sprites={"Achievements","RoomRewards","Inventory"};
        for(int i=0;i<buttons.Length;i++)
        {
            var root=ui.mainMenuPanel.Find(buttons[i]);if(!root) continue;
            var sprite=Resources.Load<Sprite>("MenuIcons/"+sprites[i]);if(!sprite) continue;
            var icon=root.GetComponent<Image>();icon.sprite=sprite;icon.type=Image.Type.Simple;
            icon.preserveAspect=true;icon.color=Color.white;icon.raycastTarget=true;
            root.GetComponent<Button>().targetGraphic=icon;
            PlaceCardElement((RectTransform)root,i==2 ? 1695 : 65,i==0 ? 335 : 535,160,160);
            var child=root.Find("MenuIcon");if(child) child.gameObject.SetActive(false);
            foreach(Transform element in root)
            {
                if(element.GetComponent<TextMeshProUGUI>()) element.gameObject.SetActive(false);
                if(element.name=="Available" || element.name=="New")
                    PlaceCardElement((RectTransform)element,120,0,42,42);
            }
        }
        PlaceCardElement(menuStars.rectTransform,35,495,220,40);
        var shop=ui.mainMenuPanel.Find("Icon__Shop");
        if(shop) PlaceCardElement((RectTransform)shop,1695,335,160,160);
        var shopCaption=ui.mainMenuPanel.Find("Text__SHOP");
        if(shopCaption) PlaceCardElement((RectTransform)shopCaption,1665,495,220,40);
        var daily=ui.mainMenuPanel.Find("DailyRewardsButton");
        if(daily)
        {
            PlaceCardElement((RectTransform)daily,1695,745,160,205);
            var gift=daily.Find("Gift");if(gift) PlaceCardElement((RectTransform)gift,0,0,160,160);
            var caption=daily.Find("Caption");if(caption) PlaceCardElement((RectTransform)caption,-30,164,220,40);
            var badge=daily.Find("Available");if(badge) PlaceCardElement((RectTransform)badge,120,0,42,42);
        }
    }
    private void PrepareAchievementIcons()
    {
        var loaded=new Dictionary<string,Sprite>();
        foreach(AchievementMetric metric in Enum.GetValues(typeof(AchievementMetric)))
        {
            string name;
            switch(metric)
            {
                case AchievementMetric.Stars: case AchievementMetric.PerfectLevels: name="Stars";break;
                case AchievementMetric.Levels: name="Levels";break;
                case AchievementMetric.Wins: name="Wins";break;
                case AchievementMetric.Matches: name="Matches";break;
                case AchievementMetric.Water: name="Water";break;
                case AchievementMetric.Stone: name="Stone";break;
                case AchievementMetric.Decorations: name="Decorations";break;
                case AchievementMetric.Backgrounds: name="Backgrounds";break;
                case AchievementMetric.DailyClaims: name="Daily";break;
                default: name="Combo";break;
            }
            if(!loaded.TryGetValue(name,out var sprite)) {sprite=Resources.Load<Sprite>("AchievementIcons/Achievement"+name);loaded.Add(name,sprite);}
            achievementIcons[metric]=sprite;
        }
        foreach(var card in cards)
        {
            if(!card.icon)
            {
                var image=new GameObject("AchievementIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                image.transform.SetParent(card.root.transform,false);card.icon=image.GetComponent<Image>();
            }
            card.icon.preserveAspect=true;card.icon.raycastTarget=false;
            var saved=GetComponent<SceneUILayout>();if(saved && saved.decoratedFramesVersion>=1)continue;
            PlaceCardElement(card.icon.rectTransform,12,16,112,112);
            PlaceCardElement(card.title.rectTransform,134,10,385,38);
            PlaceCardElement(card.description.rectTransform,134,48,385,48);
            PlaceCardElement(card.reward.rectTransform,134,103,385,28);
            var track=card.fill.rectTransform.parent as RectTransform;
            PlaceCardElement(track,134,139,371,10);
        }
    }
    private static void PlaceCardElement(RectTransform rect,float x,float y,float width,float height)
    {
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
    }
    private static string RewardText(LevelReward reward)
    {
        string value="+"+reward.coins+" COINS";
        if(reward.hints>0) value+="  +"+reward.hints+" HINT";
        if(reward.shuffles>0) value+="  +"+reward.shuffles+" SHUFFLE";
        return value;
    }
    private void Claim(int index)
    {
        if(!IsOpen || game.State!=GameState.Achievements || index<0 || index>=cards.Length) return;
        string id=cards[index].id;
        if(CareerStore.Claim(id,out var reward)) {notice.text="REWARD CLAIMED!  "+RewardText(reward);Refresh();}
    }
    public void ClaimMilestone()
    {
        if(!IsOpen || game.State!=GameState.Achievements) return;
        if(CareerStore.ClaimMilestone(out var reward)) {notice.text="BONUS CHEST!  "+RewardText(reward);Refresh();}
    }
    private void OnLevelStarted() {displayedSecond=-1;goalsDirty=false;starProgress=targetStarProgress=0;goalStars.SetStars(0);resultNotice.text="";RefreshGoals();}
    private void OnMatch(int points,float bonus,Vector2 position) {goalsDirty=true;}
    private void OnBroken(TileState state) {goalsDirty=true;}
    private void OnTime(float remaining,float limit) {int second=Mathf.CeilToInt(remaining);if(second!=displayedSecond) {displayedSecond=second;goalsDirty=true;var fill=grid.timeSlider.fillRect.GetComponent<Image>();fill.color=remaining/limit<.2f ? new Color(.9f,.31f,.22f) : remaining/limit<.4f ? new Color(.96f,.67f,.23f) : new Color(1,.92f,.79f);}}
    private void LateUpdate()
    {
        if(goalsDirty) {goalsDirty=false;RefreshGoals();}
        if(!starProgressFill) return;
        starProgress=Mathf.MoveTowards(starProgress,targetStarProgress,Time.unscaledDeltaTime*.65f);
        if(starProgressFill.type==Image.Type.Filled) starProgressFill.fillAmount=starProgress;
        else starProgressFill.rectTransform.anchorMax=new Vector2(starProgress,1);
        starProgressFill.enabled=starProgress>.005f;
        int lit=Mathf.Clamp(Mathf.FloorToInt(starProgress*3+.001f),0,3);if(goalStars.Stars!=lit) goalStars.SetStarsAnimated(lit);
    }
    public static string Clock(float seconds) {int whole=Mathf.Max(0,Mathf.CeilToInt(seconds-.0001f));return (whole/60).ToString("00")+":"+(whole%60).ToString("00");}
    private void RefreshGoals()
    {
        var challenge=game.Challenge;
        goalTitle.text="MEDAL PROGRESS";
        targetStarProgress=game.State==GameState.Win ? game.EarnedStars/3f : Mathf.Min(.99f,grid.ClearProgress)*game.EarnedStars/3f;
        timeGoal.text="SILVER\nFinish with "+Clock(game.TimeLimit*challenge.timeRemainingFraction)+" left";
        timeGoal.color=game.TimeGoalAchieved ? new Color(.36f,.3f,.2f) : new Color(.7f,.3f,.22f);
        string target;
        if(game.ActiveGoal==StarGoal.Combo) target="Reach combo x"+challenge.comboTarget+"\nBest x"+game.PeakCombo;
        else if(game.ActiveGoal==StarGoal.Water) target="Break all water\n"+game.WaterBroken+" / "+game.InitialWater;
        else if(game.ActiveGoal==StarGoal.Stone) target="Break all stone\n"+game.StoneBroken+" / "+game.InitialStone;
        else target="Break all water + stone\n"+(game.WaterBroken+game.StoneBroken)+" / "+(game.InitialWater+game.InitialStone);
        challengeGoal.text="GOLD\n"+target+"\nWithin "+challenge.turnLimit+" turns";
        bool failed=!game.GoalAchieved && game.Moves>=challenge.turnLimit;
        challengeGoal.color=game.GoalAchieved ? new Color(.28f,.52f,.24f) : failed ? new Color(.7f,.3f,.22f) : new Color(.36f,.3f,.2f);
        turns.text=game.GoalAchieved ? "GOAL COMPLETE!" : failed ? "TRY AGAIN FOR 3!" : "TURN "+game.Moves+" / "+challenge.turnLimit;
    }
    private void OnEnded(bool won)
    {
        if(!won) return;
        RefreshGoals();
        var result=grid.LastResult;resultStars.SetStars(result.stars);
        var record=CareerStore.Record(EconomyStore.Inventory.career,CareerStore.LevelId(grid.levelIndex,grid.levelDatabase));
        resultRecords.text="BEST SCORE "+record.bestScore.ToString("N0")+"   |   BEST TIME "+Clock(record.bestSeconds)+"\nBEST COMBO x"+record.bestCombo+"   |   BEST MEDALS "+record.stars+" / 3";
        resultNotice.text=result.moreStars ? "NEW MEDAL RECORD!" : result.newScore || result.newTime || result.newCombo ? "NEW PERSONAL BEST!" : "BEST RECORDS SAVED";
        Refresh();
    }
    private void OnDestroy()
    {
        RestoreInput();EconomyStore.InventoryChanged-=Refresh;
        if(!game) return;
        game.StateChanged-=OnState;game.LevelStarted-=OnLevelStarted;game.GameEnded-=OnEnded;game.MatchResolved-=OnMatch;game.TileBroken-=OnBroken;game.TimeChanged-=OnTime;
    }
}
