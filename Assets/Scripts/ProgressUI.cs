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
    public Image fill;
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
    public TextMeshProUGUI menuStars,levelStars,summary,pageLabel,notice,milestoneLabel,milestoneProgress;
    public TextMeshProUGUI goalTitle,timeGoal,challengeGoal,turns,resultRecords,resultNotice;
    public StarRatingGraphic goalStars,resultStars;
    private readonly List<AchievementDefinition> visible=new List<AchievementDefinition>(64);
    private readonly string[] categories={"ALL","STARS","COMBOS","PLAY","COLLECTION","READY"};
    private GameManager game;
    private int page,filter,displayedSecond=-1;
    private bool menuBlocked,oldInteractable,oldRaycasts,goalsDirty;
    public bool IsOpen=>panel && panel.activeSelf;
    private void Start()
    {
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
        page=filter=0;panel.SetActive(true);panel.transform.SetAsLastSibling();notice.text="";
        if(menuInput) {oldInteractable=menuInput.interactable;oldRaycasts=menuInput.blocksRaycasts;menuInput.interactable=menuInput.blocksRaycasts=false;menuBlocked=true;}
        Refresh();if(EventSystem.current) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
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
    public void SetFilter(int value) {filter=Mathf.Clamp(value,0,categories.Length-1);page=0;Refresh();}
    public void Previous() {page--;Refresh();}
    public void Next() {page++;Refresh();}
    public void Refresh()
    {
        var inventory=EconomyStore.Inventory;var career=inventory.career;var database=grid.levelDatabase;
        string stars="STARS  "+CareerStore.TotalStars(career)+" / "+(database ? database.levels.Count*3 : 0);
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
        int pages=Mathf.Max(1,Mathf.CeilToInt(visible.Count/(float)cards.Length));page=Mathf.Clamp(page,0,pages-1);
        pageLabel.text=(page+1)+" / "+pages;previousButton.interactable=page>0;nextButton.interactable=page<pages-1;
        summary.text=career.claimedAchievements.Count+" / "+AchievementCatalog.All.Count+" CLAIMED     "+ready+" READY";
        for(int index=0;index<filters.Length;index++) filters[index].GetComponent<Image>().color=index==filter ? new Color(.65f,.9f,.78f) : Color.white;
        for(int index=0;index<cards.Length;index++)
        {
            var card=cards[index];int row=page*cards.Length+index;card.root.SetActive(row<visible.Count);card.id=null;if(row>=visible.Count) continue;
            var item=visible[row];card.id=item.id;int value=AchievementCatalog.Value(item.metric,inventory);bool claimed=career.claimedAchievements.Contains(item.id),complete=value>=item.target;
            card.title.text=item.title;card.description.text=item.description;card.reward.text=RewardText(item.reward);
            card.progress.text=Mathf.Min(value,item.target).ToString("N0")+" / "+item.target.ToString("N0");
            card.fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(value/(float)item.target),1);
            card.fill.color=claimed ? new Color(.58f,.69f,.55f) : new Color(.39f,.77f,.67f);
            card.action.text=claimed ? "CLAIMED" : complete ? "CLAIM" : "IN PROGRESS";card.button.interactable=!claimed && complete;
        }
        milestoneButton.interactable=chestReady;
        milestoneLabel.text=nextChest==null ? "ALL CHESTS CLAIMED" : chestReady ? "CLAIM BONUS CHEST" : "BONUS CHEST AT "+nextChest.target;
        milestoneProgress.text=nextChest==null ? "All milestones completed!" : career.claimedAchievements.Count+" / "+nextChest.target+" CLAIMED  |  "+RewardText(nextChest.reward);
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
    private void OnLevelStarted() {displayedSecond=-1;goalsDirty=false;resultNotice.text="";RefreshGoals();}
    private void OnMatch(int points,float bonus,Vector2 position) {goalsDirty=true;}
    private void OnBroken(TileState state) {goalsDirty=true;}
    private void OnTime(float remaining,float limit) {int second=Mathf.CeilToInt(remaining);if(second!=displayedSecond) {displayedSecond=second;goalsDirty=true;}}
    private void LateUpdate() {if(goalsDirty) {goalsDirty=false;RefreshGoals();}}
    public static string Clock(float seconds) {int whole=Mathf.Max(0,Mathf.CeilToInt(seconds-.0001f));return (whole/60).ToString("00")+":"+(whole%60).ToString("00");}
    private void RefreshGoals()
    {
        var challenge=game.Challenge;
        goalStars.SetStars(game.EarnedStars);goalTitle.text="STAR GOALS";
        timeGoal.text="2 STARS\nFinish with "+Clock(game.TimeLimit*challenge.timeRemainingFraction)+" left";
        timeGoal.color=game.TimeGoalAchieved ? new Color(.36f,.3f,.2f) : new Color(.7f,.3f,.22f);
        string target;
        if(game.ActiveGoal==StarGoal.Combo) target="Reach combo x"+challenge.comboTarget+"\nBest x"+game.PeakCombo;
        else if(game.ActiveGoal==StarGoal.Water) target="Break all water\n"+game.WaterBroken+" / "+game.InitialWater;
        else if(game.ActiveGoal==StarGoal.Stone) target="Break all stone\n"+game.StoneBroken+" / "+game.InitialStone;
        else target="Break all water + stone\n"+(game.WaterBroken+game.StoneBroken)+" / "+(game.InitialWater+game.InitialStone);
        challengeGoal.text="3 STARS\n"+target+"\nWithin "+challenge.turnLimit+" turns";
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
        resultRecords.text="BEST SCORE "+record.bestScore.ToString("N0")+"   |   BEST TIME "+Clock(record.bestSeconds)+"\nBEST COMBO x"+record.bestCombo+"   |   BEST STARS "+record.stars+" / 3";
        resultNotice.text=result.moreStars ? "NEW STAR RECORD!" : result.newScore || result.newTime || result.newCombo ? "NEW PERSONAL BEST!" : "BEST RECORDS SAVED";
        Refresh();
    }
    private void OnDestroy()
    {
        RestoreInput();EconomyStore.InventoryChanged-=Refresh;
        if(!game) return;
        game.StateChanged-=OnState;game.LevelStarted-=OnLevelStarted;game.GameEnded-=OnEnded;game.MatchResolved-=OnMatch;game.TileBroken-=OnBroken;game.TimeChanged-=OnTime;
    }
}
