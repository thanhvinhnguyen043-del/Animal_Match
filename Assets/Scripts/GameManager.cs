using System;
using UnityEngine;

public enum GameState { Menu, LevelSelect, Playing, Paused, Win, Lose, Shop }

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
    private float comboDuration;
    private float timeBonus;

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

    public void BeginLevel(float seconds,float bonus,float comboSeconds)
    {
        Score=Combo=0;
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

    private void Update() { Advance(Time.deltaTime); }
    public void Advance(float seconds)
    {
        if(state!=GameState.Playing || seconds<=0) return;
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
        Combo++;
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

    public void NotifyTileBroken(TileState previousState) { TileBroken?.Invoke(previousState); }
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
