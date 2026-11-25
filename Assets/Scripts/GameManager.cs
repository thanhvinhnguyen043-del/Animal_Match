using System;
using UnityEngine;

public enum GameState { Menu, LevelSelect, Playing, Paused, Win, Lose, Shop, Decorating, DailyRewards, Loading, Achievements, RoomRewards, Leaderboard, Activities, PlayerProfile }

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameState state=GameState.Menu;
    public GameState State => state;
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public float ComboTimer { get; private set; }
    public float TimeRemaining { get; private set; }
    public float TimeLimit { get; private set; }
    public bool HasAvailablePair { get; private set; }=true;
    public bool IsResolvingMatch { get; internal set; }
    public int Moves {get;private set;}
    public int PeakCombo {get;private set;}
    public int WaterBroken {get;private set;}
    public int StoneBroken {get;private set;}
    public int InitialWater {get;private set;}
    public int InitialStone {get;private set;}
    public float ElapsedSeconds {get;private set;}
    public LevelChallenge Challenge {get;private set;}=new LevelChallenge();
    public StarGoal ActiveGoal {get;private set;}
    public bool GoalAchieved {get;private set;}
    public bool TimeGoalAchieved => TimeRemaining+.001f>=TimeLimit*Challenge.timeRemainingFraction;
    public int EarnedStars => 1+(TimeGoalAchieved ? 1 : 0)+(TimeGoalAchieved && GoalAchieved ? 1 : 0);
    private float comboDuration;
    private float timeBonus;
    private GameState profileReturnState;

    public event Action<GameState> StateChanged;
    public event Action<int,int> ScoreChanged;
    public event Action<float,float> TimeChanged;
    public event Action<int,float,Vector2> MatchResolved;
    public event Action<TileState> TileBroken;
    public event Action InvalidMatch;
    public event Action TileSelected;
    public event Action<bool> BoardAvailabilityChanged;
    public event Action LevelStarted;
    public event Action<bool> GameEnded;
    public event Action<string> NoticeRequested;

    public void BeginLevel(float seconds,float bonus,float comboSeconds,LevelChallenge challenge=null,int water=0,int stone=0)
    {
        Challenge=challenge ?? new LevelChallenge();
        InitialWater=Mathf.Max(0,water);InitialStone=Mathf.Max(0,stone);
        ActiveGoal=Challenge.goal;
        if(ActiveGoal==StarGoal.Automatic) ActiveGoal=water>0 && stone>0 ? StarGoal.WaterAndStone : water>0 ? StarGoal.Water : stone>0 ? StarGoal.Stone : StarGoal.Combo;
        if(ActiveGoal==StarGoal.Water && water==0 || ActiveGoal==StarGoal.Stone && stone==0 || ActiveGoal==StarGoal.WaterAndStone && water+stone==0) ActiveGoal=StarGoal.Combo;
        Moves=PeakCombo=WaterBroken=StoneBroken=0;ElapsedSeconds=0;GoalAchieved=false;
        Score=Combo=0;
        IsResolvingMatch=false;
        ComboTimer=0;
        TimeLimit=TimeRemaining=Mathf.Max(.01f,seconds);
        timeBonus=Mathf.Max(0,bonus);
        comboDuration=Mathf.Max(0,comboSeconds);
        HasAvailablePair=true;
        SetState(GameState.Playing);
        ScoreChanged?.Invoke(Score,Combo);
        TimeChanged?.Invoke(TimeRemaining,TimeLimit);
        BoardAvailabilityChanged?.Invoke(true);
        LevelStarted?.Invoke();
    }

    public bool TutorialTimerPaused { get; set; }

    private void Update() { Advance(Time.deltaTime); }
    public void Advance(float seconds)
    {
        if(state!=GameState.Playing || TutorialTimerPaused || IsResolvingMatch || seconds<=0) return;
        ElapsedSeconds+=Mathf.Min(seconds,TimeRemaining);
        TimeRemaining=Mathf.Max(0,TimeRemaining-seconds);
        TimeChanged?.Invoke(TimeRemaining,TimeLimit);
        if(TimeRemaining<=0) { Finish(false); return; }
        if(ComboTimer>0)
        {
            ComboTimer=Mathf.Max(0,ComboTimer-seconds);
            if(ComboTimer==0) { Combo=0; ScoreChanged?.Invoke(Score,Combo); }
        }
    }

    public void RecordMatch(Vector2 position)
    {
        if(state!=GameState.Playing) return;
        Moves++;
        Combo++;
        PeakCombo=Mathf.Max(PeakCombo,Combo);CheckStarGoal();
        ComboTimer=comboDuration;
        int added=100*Combo;
        Score+=added;
        float bonus=0;
        if(Combo%5==0)
        {
            bonus=timeBonus;
            TimeRemaining=Mathf.Min(TimeLimit,TimeRemaining+bonus);
            TimeChanged?.Invoke(TimeRemaining,TimeLimit);
        }
        ScoreChanged?.Invoke(Score,Combo);
        MatchResolved?.Invoke(added,bonus,position);
    }

    public void NotifyTileBroken(TileState previousState)
    {
        if(state!=GameState.Playing) return;
        if(previousState==TileState.Ice) WaterBroken++;
        if(previousState==TileState.Barrier) StoneBroken++;
        CheckStarGoal();TileBroken?.Invoke(previousState);
    }
    private void CheckStarGoal()
    {
        if(GoalAchieved || Moves>Mathf.Max(1,Challenge.turnLimit)) return;
        switch(ActiveGoal)
        {
            case StarGoal.Water:GoalAchieved=WaterBroken>=InitialWater;break;
            case StarGoal.Stone:GoalAchieved=StoneBroken>=InitialStone;break;
            case StarGoal.WaterAndStone:GoalAchieved=WaterBroken>=InitialWater && StoneBroken>=InitialStone;break;
            default:GoalAchieved=PeakCombo>=Mathf.Max(2,Challenge.comboTarget);break;
        }
    }
    public void NotifyInvalidMatch() { if(state==GameState.Playing) InvalidMatch?.Invoke(); }
    public void NotifyTileSelected() { if(state==GameState.Playing) TileSelected?.Invoke(); }
    public void SetBoardAvailability(bool available)
    {
        HasAvailablePair=available;
        BoardAvailabilityChanged?.Invoke(available);
    }
    public void Pause() { if(state==GameState.Playing) SetState(GameState.Paused); }
    public void Resume() { if(state==GameState.Paused) SetState(GameState.Playing); }
    public void ShowMenu() { SetState(GameState.Menu); }
    public void ShowLevelSelect() { if(state==GameState.Menu) SetState(GameState.LevelSelect); }
    public void OpenShop()
    {
        if(state!=GameState.Menu) return;
        SetState(GameState.Shop);
    }
    public void CloseShop() { if(state==GameState.Shop) SetState(GameState.Menu); }
    public bool OpenDecorations()
    {
        if(state!=GameState.Menu && state!=GameState.Shop) return false;
        SetState(GameState.Decorating);return true;
    }
    public void CloseDecorations(bool returnToShop) {if(state==GameState.Decorating) SetState(returnToShop ? GameState.Shop : GameState.Menu);}
    public bool OpenDailyRewards()
    {
        if(state!=GameState.Menu) return false;
        SetState(GameState.DailyRewards);return true;
    }
    public bool OpenActivities() {if(state!=GameState.Menu) return false;SetState(GameState.Activities);return true;}
    public void CloseActivities() {if(state==GameState.Activities) SetState(GameState.Menu);}
    public bool OpenPlayerProfile()
    {
        if(state!=GameState.Menu && state!=GameState.Playing || IsResolvingMatch) return false;
        profileReturnState=state;SetState(GameState.PlayerProfile);return true;
    }
    public void ClosePlayerProfile() {if(state==GameState.PlayerProfile) SetState(profileReturnState);}
    public void CloseDailyRewards() {if(state==GameState.DailyRewards) SetState(GameState.Menu);}
    public bool OpenAchievements() {if(state!=GameState.Menu) return false;SetState(GameState.Achievements);return true;}
    public void CloseAchievements() {if(state==GameState.Achievements) SetState(GameState.Menu);}
    public bool OpenRoomRewards() {if(state!=GameState.Menu) return false;SetState(GameState.RoomRewards);return true;}
    public bool OpenLeaderboard() {if(state!=GameState.Menu) return false;SetState(GameState.Leaderboard);return true;}
    public void CloseLeaderboard() {if(state==GameState.Leaderboard) SetState(GameState.Menu);}
    public void CloseRoomRewards() {if(state==GameState.RoomRewards) SetState(GameState.Menu);}
    public bool BeginLoading()
    {
        if(state!=GameState.Menu && state!=GameState.LevelSelect) return false;
        SetState(GameState.Loading);return true;
    }
    public void EndLoading(GameState previous) {if(state==GameState.Loading) SetState(previous==GameState.LevelSelect ? GameState.LevelSelect : GameState.Menu);}
    public void Notify(string message) { if(state==GameState.Playing) NoticeRequested?.Invoke(message); }
    public void Finish(bool won)
    {
        if(state!=GameState.Playing) return;
        SetState(won ? GameState.Win : GameState.Lose);
        GameEnded?.Invoke(won);
    }
    private void SetState(GameState next)
    {
        if(state==next) return;
        state=next;
        StateChanged?.Invoke(state);
    }
}
