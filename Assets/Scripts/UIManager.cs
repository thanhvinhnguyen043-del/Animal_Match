using UnityEngine;
using DG.Tweening;
using FMODUnity;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum GameScreen { Combined, MainMenu, Gameplay }

public class UIManager : MonoBehaviour
{
    [Header("Scene Navigation")]
    public GameScreen screen = GameScreen.Combined;
    public string mainMenuScene = "MainMenu";
    public string gameplayScene = "Gameplay";
    private bool changingScene;
    [Header("UI Panels")]
    public RectTransform mainMenuPanel;
    public RectTransform settingsPanel;
    public RectTransform levelSelectPanel;
    public RectTransform gameplayPanel;

    [Header("Settings")]
    public float tweenDuration = 0.5f;
    public Button settingsRestartButton;
    public TextMeshProUGUI gameplayLevelText;
    private bool gameplayActive;
    private bool settingsOpen;
    private float SettingsHiddenX => settingsPanel.rect.width + 200f;

    [Header("Audio")]
    public EventReference bgmMenu;
    public EventReference bgmGameplay;
    private FMOD.Studio.EventInstance currentBGM;

    void Start()
    {

        if (mainMenuPanel != null) mainMenuPanel.anchoredPosition = Vector2.zero;
        settingsPanel.anchoredPosition = new Vector2(SettingsHiddenX, 0);
        settingsPanel.gameObject.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.anchoredPosition = new Vector2(-1920, 0);
        

        if (gameplayPanel != null)
            gameplayPanel.anchoredPosition = screen == GameScreen.Gameplay ? Vector2.zero : new Vector2(0, 1080);


        gameplayActive = screen == GameScreen.Gameplay;
        if (!gameplayActive) PlayMusic(bgmMenu);
    }


    public void OpenSettings()
    {
        if (!settingsPanel.gameObject.activeSelf)
            settingsPanel.anchoredPosition = new Vector2(SettingsHiddenX, 0);
        settingsPanel.gameObject.SetActive(true);
        settingsOpen = true;
        if (settingsRestartButton != null) settingsRestartButton.interactable = gameplayActive;
        if (gameplayActive) Time.timeScale = 0;
        settingsPanel.SetAsLastSibling();
        settingsPanel.DOKill();
        settingsPanel.DOAnchorPosX(0, tweenDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void CloseSettings()
    {
        settingsOpen = false;
        Time.timeScale = 1;
        settingsPanel.DOKill();
        settingsPanel.DOAnchorPosX(SettingsHiddenX, tweenDuration).SetEase(Ease.OutBack).SetUpdate(true)
            .OnComplete(() => settingsPanel.gameObject.SetActive(false));
    }

    private void HideSettingsImmediately()
    {
        settingsOpen = false;
        Time.timeScale = 1;
        settingsPanel.DOKill();
        settingsPanel.anchoredPosition = new Vector2(SettingsHiddenX, 0);
        settingsPanel.gameObject.SetActive(false);
    }

    public void RestartFromSettings()
    {
        if (!gameplayActive) return;
        HideSettingsImmediately();
        var grid = FindObjectOfType<GridManager>();
        if (grid != null) grid.RestartGame();
    }

    public void ReturnHomeFromSettings()
    {
        HideSettingsImmediately();
        if (gameplayActive) BackToMenuFromGame();
    }

    public void RefreshLevelLabel(int index)
    {
        if (gameplayLevelText != null) gameplayLevelText.text = "LEVEL " + (index + 1);
    }

    public void OpenLevelSelect()
    {
        var selection = levelSelectPanel.GetComponent<LevelSelectionUI>();
        if (selection != null) selection.Refresh();
        mainMenuPanel.DOAnchorPosX(1920, tweenDuration).SetEase(Ease.OutBack);
        levelSelectPanel.DOAnchorPosX(0, tweenDuration).SetEase(Ease.OutBack);
    }

    public void CloseLevelSelect()
    {
        levelSelectPanel.DOAnchorPosX(-1920, tweenDuration).SetEase(Ease.OutBack);
        mainMenuPanel.DOAnchorPosX(0, tweenDuration).SetEase(Ease.OutBack);
    }


    public void PlayLatestLevel()
    {
        var database = Resources.Load<LevelDatabase>("LevelDatabase");
        if (database != null && database.levels != null)
            StartLevel(LevelProgress.Latest(database.levels.Count));
    }

    public void StartGameplay()
    {
        if (screen != GameScreen.Combined)
        {
            PlayLatestLevel();
            return;
        }
        HideSettingsImmediately();
        gameplayActive = true;

        FindObjectOfType<GridManager>().RestartGame();


        levelSelectPanel.DOAnchorPosX(-1920, tweenDuration).SetEase(Ease.OutBack);
        mainMenuPanel.DOAnchorPosX(-1920, tweenDuration).SetEase(Ease.OutBack);
        

        gameplayPanel.DOAnchorPosY(0, tweenDuration).SetEase(Ease.OutBack);


    }



    public void StartLevel(int index)
    {
        var database = Resources.Load<LevelDatabase>("LevelDatabase");
        if (database == null || database.levels == null || index < 0 || index >= database.levels.Count ||
            index > LevelProgress.HighestUnlocked(database.levels.Count) || database.levels[index] == null ||
            !database.levels[index].Validate(database.animalSprites.Length, out string error))
        {
            Debug.LogError("Level chua hop le. Kiem tra Tools > Level Database.");
            return;
        }
        if (screen != GameScreen.Combined)
        {
            GameSession.SelectLevel(index);
            ChangeScene(gameplayScene);
            return;
        }
        var grid = FindObjectOfType<GridManager>();
        if (grid != null && grid.SelectLevel(index)) StartGameplay();
    }

    public void BackToMenuFromGame()
    {
        if (screen != GameScreen.Combined)
        {
            ChangeScene(mainMenuScene);
            return;
        }
        HideSettingsImmediately();
        gameplayActive = false;
        var grid = FindObjectOfType<GridManager>();
        if (grid != null) grid.PauseGameplay();
        gameplayPanel.DOAnchorPosY(1080, tweenDuration).SetEase(Ease.InBack);
        mainMenuPanel.DOAnchorPosX(0, tweenDuration).SetEase(Ease.OutBack);
        PlayMusic(bgmMenu);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(bgmGameplay);
    }

    public void StopGameplayMusic()
    {
        if (!currentBGM.isValid()) return;
        currentBGM.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        currentBGM.release();
        currentBGM.clearHandle();
    }

    private void OnDestroy()
    {
        if (settingsOpen) Time.timeScale = 1;
        StopGameplayMusic();
    }

    private void ChangeScene(string name)
    {
        if (changingScene) return;
        if (!Application.CanStreamedLevelBeLoaded(name))
        {
            Debug.LogError("Scene " + name + " chua co trong Build Settings.");
            return;
        }
        changingScene = true;
        HideSettingsImmediately();
        PlayerPrefs.Save();
        DOTween.KillAll();
        StopGameplayMusic();
        SceneManager.LoadScene(name);
    }


    private void PlayMusic(EventReference newMusic)
    {

        if (currentBGM.isValid())
        {
            currentBGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentBGM.release();
            currentBGM.clearHandle();
        }


        if (!newMusic.IsNull)
        {
            currentBGM = RuntimeManager.CreateInstance(newMusic);
            currentBGM.start();
        }
    }
}
