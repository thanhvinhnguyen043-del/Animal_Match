using System;
using FMODUnity;
using UnityEngine;

public sealed class GameAudioFeedback : IDisposable
{
    private readonly GameManager game;
    private readonly GridManager grid;
    public GameAudioFeedback(GameManager game,GridManager grid)
    {
        this.game=game; this.grid=grid;
        game.LevelStarted+=OnLevelStarted;
        game.MatchResolved+=OnMatch;
        game.TileBroken+=OnTileBroken;
        game.InvalidMatch+=OnInvalidMatch;
        game.TileSelected+=OnTileSelected;
        game.GameEnded+=OnGameEnded;
    }
    private void Play(EventReference sound) { if(!sound.IsNull) RuntimeManager.PlayOneShot(sound); }
    private void OnLevelStarted()
    {
        var ui=UnityEngine.Object.FindObjectOfType<UIManager>();
        if(ui) ui.PlayGameplayMusic();
    }
    private void OnMatch(int points,float bonus,Vector2 position)
    {
        Play(grid.sfxMatch);
        if(game.Combo%5==0) Play(grid.sfxCombo);
    }
    private void OnTileBroken(TileState state)
    {
        if(state==TileState.Ice) Play(grid.sfxWaterBreak);
        else if(state==TileState.Barrier) Play(grid.sfxStoneBreak);
    }
    private void OnInvalidMatch() { Play(grid.sfxError); }
    private void OnTileSelected() { Play(grid.sfxClick); }
    private void OnGameEnded(bool won)
    {
        var ui=UnityEngine.Object.FindObjectOfType<UIManager>();
        if(ui) ui.StopGameplayMusic();
        Play(won ? grid.winSound : grid.loseSound);
    }
    public void Dispose()
    {
        game.LevelStarted-=OnLevelStarted;
        game.MatchResolved-=OnMatch;
        game.TileBroken-=OnTileBroken;
        game.InvalidMatch-=OnInvalidMatch;
        game.TileSelected-=OnTileSelected;
        game.GameEnded-=OnGameEnded;
    }
}
