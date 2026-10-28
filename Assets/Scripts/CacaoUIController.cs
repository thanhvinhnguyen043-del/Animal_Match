using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DefaultExecutionOrder(100)]
public class CacaoUIController : MonoBehaviour
{
    public GridManager grid;
    public UIManager ui;
    public GameObject pausePanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI pairsText;
    public TextMeshProUGUI scoreNumber;
    public TextMeshProUGUI winLevelText;
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI winRewardText;
    public TextMeshProUGUI bonusRewardText;
    public TextMeshProUGUI continueText;
    public Image loseIcon;
    public Image boardFrame;
    public RectTransform boardBacking;
    public TextMeshProUGUI comboPraise;
    private GameManager game;
    private int totalTiles;
    private int displayedSeconds=-1;
    private bool pairsDirty;
    private Sequence praiseTween;
    private Vector2 praisePosition;

    private void Start()
    {
        game=grid.Session;
        game.LevelStarted+=OnLevelStarted;
        game.TimeChanged+=OnTimeChanged;
        game.ScoreChanged+=OnScoreChanged;
        game.MatchResolved+=OnMatch;
        game.GameEnded+=OnGameEnded;
        game.StateChanged+=OnStateChanged;
        if(comboPraise) praisePosition=comboPraise.rectTransform.anchoredPosition;
        HideComboPraise();
        if(game.State==GameState.Playing) OnLevelStarted();
    }
    public void OpenPause()
    {
        if(game.State!=GameState.Playing) return;
        game.Pause();
        Time.timeScale=0;
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
    }
    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale=1;
        game.Resume();
    }
    public void OpenAudioSettings()
    {
        pausePanel.SetActive(false);
        ui.OpenSettings();
    }
    public void Home()
    {
        pausePanel.SetActive(false);
        Time.timeScale=1;
        ui.BackToMenuFromGame();
    }
    public void Retry()
    {
        pausePanel.SetActive(false);
        Time.timeScale=1;
        ui.RestartFromSettings();
    }
    public void Continue()
    {
        if(grid.HasNextLevel()) grid.NextLevel(); else Home();
    }
    private int CountTiles()
    {
        int count=0;
        if(grid.gridArray!=null) foreach(var tile in grid.gridArray) if(tile) count++;
        return count;
    }
    private void OnLevelStarted()
    {
        HideComboPraise();
        pausePanel.SetActive(false);
        totalTiles=CountTiles();
        grid.boardContainer.localScale=Vector3.one;
        float width=grid.columns*grid.tileSize,height=grid.rows*grid.tileSize;
        grid.boardContainer.anchoredPosition=new Vector2(960,-580);
        grid.boardContainer.sizeDelta=new Vector2(width,height);
        if(boardFrame) FitBoardRect(boardFrame.rectTransform,width+96,height+96);
        if(boardBacking) FitBoardRect(boardBacking,width+40,height+40);
        displayedSeconds=-1;
        OnTimeChanged(game.TimeRemaining,game.TimeLimit);
        OnScoreChanged(0,0);
        pairsDirty=true;
    }
    private static void FitBoardRect(RectTransform rect,float width,float height)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);
        rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(960,-580);
        rect.sizeDelta=new Vector2(width,height);
    }
    private void OnTimeChanged(float remaining,float limit)
    {
        int seconds=Mathf.CeilToInt(remaining);
        if(seconds==displayedSeconds) return;
        displayedSeconds=seconds;
        timerText.text=(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");
    }
    private void OnScoreChanged(int score,int combo)
    {
        scoreNumber.text=score.ToString("N0");
        if(combo<=1) HideComboPraise();
        if(!grid.comboText) return;
        grid.comboText.text=combo>1 ? "COMBO\nx"+combo : "";
        var rect=grid.comboText.rectTransform;
        rect.DOKill();rect.localScale=Vector3.one;
        if(combo>1) rect.DOPunchScale(Vector3.one*.1f,.22f,1,.4f);
    }
    private void OnMatch(int score,float time,Vector2 position)
    {
        pairsDirty=true;
        string word=ComboPraiseWord(game.Combo);
        if(comboPraise && word!=null) ShowComboPraise(word,game.Combo);
    }
    public static string ComboPraiseWord(int combo)
    {
        if(combo==3) return "GREAT!";
        if(combo==5) return "EXCELLENT!";
        if(combo==8) return "AMAZING!";
        if(combo==12 || combo>12 && combo%5==0) return "PERFECT!";
        return null;
    }
    private void ShowComboPraise(string word,int combo)
    {
        HideComboPraise();comboPraise.text=word;comboPraise.gameObject.SetActive(true);
        comboPraise.color=combo>=12 ? new Color(1,.91f,.43f) : combo>=8 ? new Color(1,.64f,.91f) : combo>=5 ? new Color(.47f,.96f,.84f) : new Color(1,.83f,.25f);
        var rect=comboPraise.rectTransform;rect.anchoredPosition=praisePosition-new Vector2(0,18);rect.localScale=Vector3.one*.7f;
        praiseTween=DOTween.Sequence()
            .Append(rect.DOScale(1.08f,.18f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPos(praisePosition,.25f).SetEase(Ease.OutCubic))
            .Append(rect.DOScale(1,.12f).SetEase(Ease.InOutSine))
            .AppendInterval(.45f)
            .Append(comboPraise.DOFade(0,.3f))
            .Join(rect.DOAnchorPosY(praisePosition.y+28,.3f).SetEase(Ease.OutSine))
            .OnComplete(()=>{praiseTween=null;comboPraise.text="";comboPraise.gameObject.SetActive(false);});
    }
    private void HideComboPraise()
    {
        praiseTween?.Kill();praiseTween=null;
        if(!comboPraise) return;
        comboPraise.text="";comboPraise.alpha=0;comboPraise.rectTransform.localScale=Vector3.one;
        comboPraise.rectTransform.anchoredPosition=praisePosition;comboPraise.gameObject.SetActive(false);
    }
    private void OnStateChanged(GameState state) {if(state!=GameState.Playing) HideComboPraise();}
    private void LateUpdate()
    {
        if(!pairsDirty) return;
        pairsDirty=false;
        pairsText.text=((totalTiles-CountTiles())/2)+" / "+(totalTiles/2);
    }
    private void OnGameEnded(bool won)
    {
        pausePanel.SetActive(false);
        winPanel.SetActive(won);
        losePanel.SetActive(!won);
        grid.textTitle.text="WELL DONE!";
        winLevelText.text="LEVEL "+(grid.levelIndex+1)+" COMPLETE";
        winScoreText.text=game.Score.ToString("N0");
        winRewardText.text="+"+grid.LastReward.coins;
        var bonus=new List<string>(2);
        if(grid.LastReward.hints>0) bonus.Add("+"+grid.LastReward.hints+" HINT");
        if(grid.LastReward.shuffles>0) bonus.Add("+"+grid.LastReward.shuffles+" SHUFFLE");
        bonusRewardText.text=string.Join("   ",bonus);
        grid.btnNextLevel.gameObject.SetActive(won);
        continueText.text=grid.HasNextLevel() ? "CONTINUE" : "RETURN HOME";
        if(loseIcon) loseIcon.gameObject.SetActive(!won);
        grid.gameOverPanel.transform.SetAsLastSibling();
        pairsDirty=true;
    }
    private void OnDestroy()
    {
        HideComboPraise();
        if(!game) return;
        if(grid && grid.comboText) grid.comboText.rectTransform.DOKill();
        game.LevelStarted-=OnLevelStarted;
        game.TimeChanged-=OnTimeChanged;
        game.ScoreChanged-=OnScoreChanged;
        game.MatchResolved-=OnMatch;
        game.GameEnded-=OnGameEnded;
        game.StateChanged-=OnStateChanged;
        if(pausePanel && pausePanel.activeSelf) Time.timeScale=1;
    }
}
