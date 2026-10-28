using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class LoadingScreenView
{
    public GameObject panel;
    public Image fill;
    public TextMeshProUGUI percentage;
    public RectTransform[] heroes;
    [NonSerialized] public int displayedPercentage=-1;
}

[DefaultExecutionOrder(50)]
public class LoadingUI : MonoBehaviour
{
    public UIManager ui;
    public GridManager grid;
    public LoadingScreenView startup,play;
    public TextMeshProUGUI playLevel;
    public CanvasGroup[] inputGroups;
    public Button menuPlayButton;
    [Range(3,5)] public float startupSeconds=4;
    [Range(1,3)] public float playSeconds=2;
    private static bool startupShown;
    private bool startupPending,blocked;
    private bool[] previousInteractable,previousRaycasts;
    private Coroutine operation;
    private GameManager game;
    private GameState returnState;
    public bool IsBusy => startupPending || operation!=null;
    public float Progress {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() {startupShown=false;}
    private void Awake()
    {
        previousInteractable=new bool[inputGroups.Length];previousRaycasts=new bool[inputGroups.Length];
        startupPending=enabled && !startupShown && ui.screen!=GameScreen.Gameplay;
        startup.panel.SetActive(startupPending);play.panel.SetActive(false);
        if(startupPending) {startupShown=true;SetProgress(startup,0);}
    }
    private void Start()
    {
        game=grid.Session;
        if(!startupPending) return;
        startupPending=false;returnState=game.State;
        if(!game.BeginLoading()) {startup.panel.SetActive(false);return;}
        BlockInput();operation=StartCoroutine(OpenApp());
    }
    private IEnumerator OpenApp()
    {
        yield return Animate(startup,Mathf.Clamp(startupSeconds,3,5));
        startup.panel.SetActive(false);RestoreInput();operation=null;game.EndLoading(returnState);
        if(EventSystem.current && menuPlayButton) EventSystem.current.SetSelectedGameObject(menuPlayButton.gameObject);
        var daily=ui.GetComponent<DailyRewardUI>();if(daily) daily.TryAutoShow();
    }
    public bool LoadLevel(int index)
    {
        if(IsBusy || !game) return false;
        returnState=game.State;if(!game.BeginLoading()) return false;
        playLevel.text="LEVEL "+(index+1);play.panel.SetActive(true);SetProgress(play,0);BlockInput();
        operation=StartCoroutine(EnterLevel(index));return true;
    }
    private IEnumerator EnterLevel(int index)
    {
        yield return Animate(play,Mathf.Clamp(playSeconds,1,3));
        try {game.EndLoading(returnState);ui.StartLevelImmediately(index);}
        finally {play.panel.SetActive(false);RestoreInput();operation=null;}
    }
    private IEnumerator Animate(LoadingScreenView view,float duration)
    {
        SetProgress(view,0);yield return null;
        float started=Time.realtimeSinceStartup;var origins=new Vector2[view.heroes.Length];
        for(int i=0;i<origins.Length;i++) origins[i]=view.heroes[i].anchoredPosition;
        while(Time.realtimeSinceStartup-started<duration)
        {
            float elapsed=Time.realtimeSinceStartup-started;SetProgress(view,Mathf.SmoothStep(0,1,elapsed/duration));
            for(int i=0;i<origins.Length;i++) view.heroes[i].anchoredPosition=origins[i]+new Vector2(0,Mathf.Sin(elapsed*2.8f+i*.4f)*7);
            yield return null;
        }
        for(int i=0;i<origins.Length;i++) view.heroes[i].anchoredPosition=origins[i];
        SetProgress(view,1);yield return null;
    }
    private void SetProgress(LoadingScreenView view,float value)
    {
        Progress=Mathf.Clamp01(value);view.fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,880*Progress);
        int percent=Mathf.FloorToInt(Progress*100);
        if(view.displayedPercentage!=percent) {view.displayedPercentage=percent;view.percentage.text=percent+"%";}
    }
    private void BlockInput()
    {
        for(int i=0;i<inputGroups.Length;i++)
        {
            var group=inputGroups[i];if(!group) continue;
            previousInteractable[i]=group.interactable;previousRaycasts[i]=group.blocksRaycasts;group.interactable=group.blocksRaycasts=false;
        }
        blocked=true;if(EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }
    private void RestoreInput()
    {
        if(!blocked) return;
        for(int i=0;i<inputGroups.Length;i++) if(inputGroups[i]) {inputGroups[i].interactable=previousInteractable[i];inputGroups[i].blocksRaycasts=previousRaycasts[i];}
        blocked=false;
    }
    private void OnDisable()
    {
        if(operation!=null) StopCoroutine(operation);operation=null;startupPending=false;
        if(startup!=null && startup.panel) startup.panel.SetActive(false);if(play!=null && play.panel) play.panel.SetActive(false);
        RestoreInput();if(game && game.State==GameState.Loading) game.EndLoading(returnState);
    }
}
