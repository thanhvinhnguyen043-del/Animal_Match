using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public class WaterTutorialUI : MonoBehaviour
{
    public const string SeenKey="Tutorial.Water.Seen";
    public GridManager grid;
    public GameObject panel;
    public Image waterAnimal;
    public Image normalAnimal;
    public Image uncoveredAnimal;
    private GameManager game;
    private float resumeTimeScale=1;
    public bool IsOpen => panel && panel.activeSelf;

    private void Start()
    {
        game=grid.Session;
        game.LevelStarted+=OnLevelStarted;
        game.StateChanged+=OnStateChanged;
        if(game.State==GameState.Playing) OnLevelStarted();
    }
    private void OnLevelStarted()
    {
        if(PlayerPrefs.GetInt(SeenKey,0)==1 || IsOpen || game.State!=GameState.Playing || grid.gridArray==null) return;
        foreach(var tile in grid.gridArray)
        {
            if(!tile || tile.currentState!=TileState.Ice) continue;
            var sprite=tile.animalImage.sprite;
            waterAnimal.sprite=normalAnimal.sprite=uncoveredAnimal.sprite=sprite;
            resumeTimeScale=Time.timeScale;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            game.Pause();
            Time.timeScale=0;
            return;
        }
    }
    public void Dismiss()
    {
        if(!IsOpen) return;
        PlayerPrefs.SetInt(SeenKey,1);
        PlayerPrefs.Save();
        Hide();
        if(game.State==GameState.Paused) game.Resume();
    }
    private void Hide()
    {
        panel.SetActive(false);
        Time.timeScale=resumeTimeScale;
    }
    private void OnStateChanged(GameState state)
    {
        if(IsOpen && state!=GameState.Paused) Hide();
    }
    private void OnDestroy()
    {
        if(game)
        {
            game.LevelStarted-=OnLevelStarted;
            game.StateChanged-=OnStateChanged;
        }
        if(IsOpen) Time.timeScale=resumeTimeScale;
    }
}
