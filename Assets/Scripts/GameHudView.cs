using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameHudView : IDisposable
{
    private readonly GameManager game;
    private readonly GridManager grid;
    private GameObject warning;
    private readonly List<GameObject> floatingTexts=new List<GameObject>();
    public GameHudView(GameManager game,GridManager grid)
    {
        this.game=game; this.grid=grid;
        game.ScoreChanged+=OnScoreChanged;
        game.TimeChanged+=OnTimeChanged;
        game.MatchResolved+=OnMatch;
        game.GameEnded+=OnGameEnded;
        game.NoticeRequested+=OnNotice;
        game.LevelStarted+=OnLevelStarted;
        game.StateChanged+=OnStateChanged;
        game.BoardAvailabilityChanged+=OnBoardAvailabilityChanged;
    }
    private void OnScoreChanged(int score,int combo)
    {
        if(grid.scoreText) grid.scoreText.text=$"Score: {score}";
        if(grid.comboText) grid.comboText.text=combo>1 ? $"Combo: x{combo}!" : "";
    }
    private void OnTimeChanged(float remaining,float limit)
    {
        if(grid.timeSlider) grid.timeSlider.value=remaining/limit;
    }
    private void OnMatch(int points,float bonus,Vector2 position)
    {
        SpawnFloatingText(position,"+"+points,Color.white);
        if(bonus>0) SpawnFloatingText(position+new Vector2(0,40),"+"+bonus+" SEC!",Color.yellow);
    }
    private void OnNotice(string message)
    {
        SpawnFloatingText(new Vector2(0,-grid.rows*grid.tileSize/2f),message,Color.white,3f);
    }
    private void SpawnFloatingText(Vector2 position,string text,Color color,float duration=1f)
    {
        if(!grid.floatingTextPrefab || !grid.boardContainer) return;
        var obj=UnityEngine.Object.Instantiate(grid.floatingTextPrefab,grid.boardContainer);
        floatingTexts.Add(obj);
        var rect=obj.GetComponent<RectTransform>();
        var label=obj.GetComponent<TextMeshProUGUI>();
        rect.anchoredPosition=position;
        label.text=text; label.color=color;
        if(duration>1f)
        {
            rect.sizeDelta=new Vector2(1000,70);
            label.fontSize=28;
            label.enableAutoSizing=true;
            label.fontSizeMin=20; label.fontSizeMax=28;
            label.alignment=TextAlignmentOptions.Center;
            label.raycastTarget=false;
        }
        rect.DOAnchorPosY(position.y+100,duration).SetEase(Ease.OutCirc);
        label.DOFade(0,duration).OnComplete(()=>
        {
            floatingTexts.Remove(obj);
            UnityEngine.Object.Destroy(obj);
        });
    }
    private void OnLevelStarted()
    {
        ClearFloatingTexts();
        if(grid.gameOverPanel) grid.gameOverPanel.SetActive(false);
        if(grid.btnNextLevel) grid.btnNextLevel.gameObject.SetActive(false);
        if(grid.rewardText) grid.rewardText.gameObject.SetActive(false);
        if(grid.winTrophy) grid.winTrophy.SetActive(false);
        if(grid.levelOneWinBanner) grid.levelOneWinBanner.SetActive(false);
        if(grid.btnShuffle) grid.btnShuffle.interactable=true;
        if(grid.btnHint) grid.btnHint.interactable=true;
        var ui=UnityEngine.Object.FindObjectOfType<UIManager>();
        if(ui) ui.RefreshLevelLabel(grid.levelIndex);
        OnBoardAvailabilityChanged(true);
    }
    private void OnGameEnded(bool won)
    {
        if(grid.winTrophy) grid.winTrophy.SetActive(won);
        if(grid.btnNextLevel) grid.btnNextLevel.gameObject.SetActive(won && grid.HasNextLevel());
        bool banner=won && grid.levelIndex==0 && grid.levelOneWinBanner;
        if(grid.levelOneWinBanner) grid.levelOneWinBanner.SetActive(banner);
        if(grid.textTitle)
        {
            grid.textTitle.gameObject.SetActive(!banner);
            grid.textTitle.text=won ? "LEVEL "+(grid.levelIndex+1)+"\nCOMPLETED!" : "TRY AGAIN!";
            grid.textTitle.color=Color.white;
        }
        if(grid.textFinalScore) grid.textFinalScore.text="SCORE: "+game.Score;
        if(grid.rewardText)
        {
            grid.rewardText.gameObject.SetActive(won);
            grid.rewardText.text=won ? grid.LastReward.DisplayText() : "";
        }
        if(grid.gameOverPanel) grid.gameOverPanel.SetActive(true);
        if(grid.btnShuffle) grid.btnShuffle.interactable=false;
        if(grid.btnHint) grid.btnHint.interactable=false;
    }
    private void OnStateChanged(GameState state)
    {
        if(state==GameState.Menu || state==GameState.LevelSelect)
        {
            ClearFloatingTexts();
            if(grid.gameOverPanel) grid.gameOverPanel.SetActive(false);
            if(grid.btnNextLevel) grid.btnNextLevel.gameObject.SetActive(false);
        }
        OnBoardAvailabilityChanged(game.HasAvailablePair);
    }
    private void OnBoardAvailabilityChanged(bool available)
    {
        bool show=!available && game.State==GameState.Playing;
        if(show && !warning) CreateWarning();
        if(warning) warning.SetActive(show);
    }
    private void CreateWarning()
    {
        warning=new GameObject("NoAvailablePairNotice",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        warning.transform.SetParent(grid.boardContainer,false);
        var rect=warning.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(0,-grid.rows*grid.tileSize/2f-34);
        rect.sizeDelta=new Vector2(Mathf.Clamp(grid.columns*grid.tileSize,700,1100),44);
        var background=warning.GetComponent<Image>();
        background.color=new Color(.035f,.15f,.27f,.94f); background.raycastTarget=false;
        var label=new GameObject("Message",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(warning.transform,false);
        label.rectTransform.anchorMin=Vector2.zero; label.rectTransform.anchorMax=Vector2.one;
        label.rectTransform.offsetMin=new Vector2(12,4); label.rectTransform.offsetMax=new Vector2(-12,-4);
        if(grid.scoreText) label.font=grid.scoreText.font;
        label.text="No available matches. Shuffle or restart.";
        label.fontSize=24; label.enableAutoSizing=true; label.fontSizeMin=18; label.fontSizeMax=24;
        PngTextView.Attach(label);
        label.alignment=TextAlignmentOptions.Center; label.color=Color.white; label.raycastTarget=false;
    }
    public void Dispose()
    {
        game.ScoreChanged-=OnScoreChanged;
        game.TimeChanged-=OnTimeChanged;
        game.MatchResolved-=OnMatch;
        game.GameEnded-=OnGameEnded;
        game.NoticeRequested-=OnNotice;
        game.LevelStarted-=OnLevelStarted;
        game.StateChanged-=OnStateChanged;
        game.BoardAvailabilityChanged-=OnBoardAvailabilityChanged;
        ClearFloatingTexts();
        if(warning) UnityEngine.Object.Destroy(warning);
    }
    private void ClearFloatingTexts()
    {
        foreach(var obj in floatingTexts)
            if(obj)
            {
                var rect=obj.GetComponent<RectTransform>();
                var text=obj.GetComponent<TextMeshProUGUI>();
                rect.DOKill(); text.DOKill();
                UnityEngine.Object.Destroy(obj);
            }
        floatingTexts.Clear();
    }
}
