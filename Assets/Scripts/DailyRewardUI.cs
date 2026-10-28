using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class DailyRewardCard
{
    public Image background;
    public CanvasGroup rewards;
    public Image[] icons;
    public TextMeshProUGUI[] amounts;
    public GameObject claimed,locked,today;
    public TextMeshProUGUI status;
}

[DefaultExecutionOrder(150)]
public class DailyRewardUI : MonoBehaviour
{
    public UIManager ui;
    public GameObject panel;
    public Button menuButton,claimButton,closeButton;
    public CanvasGroup menuInput;
    public GameObject availableBadge;
    public TextMeshProUGUI claimLabel,progress,reminder,notice;
    public Image[] progressDots;
    public DailyRewardCard[] cards;
    public Sprite upcomingCard,todayCard,claimedCard;
    public bool autoShow=true;
    private GameManager game;
    private Coroutine clock;
    private bool menuBlocked,previousInteractable,previousRaycasts;
    private static bool shownThisSession;
    public bool IsOpen => panel && panel.activeSelf;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() {shownThisSession=false;}
    private void Start()
    {
        game=GridManager.Instance.Session;game.StateChanged+=OnStateChanged;EconomyStore.InventoryChanged+=Refresh;
        panel.SetActive(false);Refresh();
        TryAutoShow();
    }
    public void TryAutoShow() {if(game && autoShow && !shownThisSession && EconomyStore.DailyStatus.canClaim && game.State==GameState.Menu && ui.screen!=GameScreen.Gameplay) Open();}
    public void Open()
    {
        if(!game || ui.screen==GameScreen.Gameplay || ui.settingsPanel.gameObject.activeSelf || !game.OpenDailyRewards()) return;
        shownThisSession=true;panel.SetActive(true);panel.transform.SetAsLastSibling();notice.text="";Refresh();
        if(menuInput) {previousInteractable=menuInput.interactable;previousRaycasts=menuInput.blocksRaycasts;menuInput.interactable=menuInput.blocksRaycasts=false;menuBlocked=true;}
        if(EventSystem.current) EventSystem.current.SetSelectedGameObject(claimButton.interactable ? claimButton.gameObject : closeButton.gameObject);
        if(clock!=null) StopCoroutine(clock);clock=StartCoroutine(RefreshClock());
    }
    public void Close()
    {
        if(!IsOpen) return;
        panel.SetActive(false);StopClock();RestoreMenuInput();game.CloseDailyRewards();
        if(EventSystem.current) EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
    }
    public void Claim()
    {
        if(!IsOpen || !game || game.State!=GameState.DailyRewards) return;
        EconomyStore.TryClaimDailyReward(out _,out var message);Refresh();notice.text=message;
        if(EventSystem.current && !claimButton.interactable) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
    }
    private void OnStateChanged(GameState state)
    {
        if(state==GameState.DailyRewards || !IsOpen) return;
        panel.SetActive(false);StopClock();RestoreMenuInput();
        if(EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }
    public void Refresh()
    {
        var state=EconomyStore.DailyStatus;
        if(availableBadge) availableBadge.SetActive(state.canClaim);
        if(!IsOpen) return;
        for(int i=0;i<cards.Length;i++) RefreshCard(cards[i],i,state);
        progress.text=state.collected+" / 7 COLLECTED";
        for(int i=0;i<progressDots.Length;i++) progressDots[i].color=i<state.collected ? new Color(.49f,.71f,.31f) : new Color(.89f,.8f,.65f);
        claimButton.interactable=state.canClaim;claimLabel.text=state.canClaim ? "CLAIM REWARD" : "COLLECTED TODAY";RefreshReminder();
    }
    private void RefreshCard(DailyRewardCard card,int index,DailyRewardStatus state)
    {
        bool claimed=index<state.collected,ready=state.canClaim && index+1==state.day,weekly=index==6;
        if(!weekly) card.background.sprite=claimed ? claimedCard : ready ? todayCard : upcomingCard;
        card.claimed.SetActive(claimed);card.locked.SetActive(!claimed && !ready);card.today.SetActive(ready);
        card.status.text=claimed ? "CLAIMED" : ready ? "READY TO CLAIM" : "COMING UP";
        card.status.color=claimed ? new Color(.34f,.47f,.25f) : new Color(.58f,.38f,.16f);
        var reward=DailyRewardSchedule.Reward(index+1);int[] values={reward.coins,reward.hints,reward.shuffles};
        int count=0;foreach(int value in values) if(value>0) count++;
        int column=0;
        for(int kind=0;kind<3;kind++)
        {
            var icon=card.icons[kind];var amount=card.amounts[kind];bool visible=values[kind]>0;icon.gameObject.SetActive(visible);amount.gameObject.SetActive(visible);
            icon.color=new Color(1,1,1,claimed ? .58f : 1);
            if(!visible) continue;
            float size=weekly ? 76 : count==1 ? 130 : count==2 ? 95 : 76;
            float spacing=weekly ? 139 : count==2 ? 118 : 87;
            float center=(weekly ? 255 : 145)+(column-(count-1)*.5f)*spacing;
            Position(icon.rectTransform,center-size/2,weekly ? 397 : count==1 ? 60 : 68,size,size);
            Position(amount.rectTransform,center-(weekly ? 65 : count==1 ? 138 : 58),weekly ? 474 : count==1 ? 182 : 171,weekly ? 130 : count==1 ? 276 : 116,40);
            amount.text=(kind==0 ? "+" : "x")+values[kind]+(!weekly && count==1 ? kind==0 ? " COINS" : kind==1 ? " HINT" : " SHUFFLE" : "");
            column++;
        }
    }
    private static void Position(RectTransform rect,float x,float y,float width,float height)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
    }
    private void RefreshReminder()
    {
        var state=EconomyStore.DailyStatus;
        if(state.canClaim) {reminder.text="NEW GIFTS EVERY DAY";return;}
        var remaining=state.nextClaimAt-DateTime.Now;if(remaining<TimeSpan.Zero) remaining=TimeSpan.Zero;
        reminder.text="NEXT GIFT IN "+(remaining.Days>0 ? remaining.Days+"d " : "")+remaining.ToString(@"hh\:mm\:ss");
    }
    private IEnumerator RefreshClock()
    {
        DateTime today=DateTime.Today;var interval=new WaitForSecondsRealtime(1);
        while(IsOpen)
        {
            yield return interval;
            if(DateTime.Today!=today) {today=DateTime.Today;notice.text="";Refresh();} else RefreshReminder();
        }
    }
    private void StopClock() {if(clock==null) return;StopCoroutine(clock);clock=null;}
    private void RestoreMenuInput()
    {
        if(!menuBlocked) return;
        if(menuInput) {menuInput.interactable=previousInteractable;menuInput.blocksRaycasts=previousRaycasts;}
        menuBlocked=false;
    }
    private void OnApplicationFocus(bool focused) {if(focused && game) Refresh();}
    private void OnDestroy()
    {
        StopClock();RestoreMenuInput();EconomyStore.InventoryChanged-=Refresh;if(game) game.StateChanged-=OnStateChanged;
    }
}
