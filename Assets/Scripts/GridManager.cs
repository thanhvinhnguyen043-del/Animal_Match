using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;

public enum GravityDirection { None, Down, Up, Left, Right }

public class GridManager : MonoBehaviour
{   
    private static bool isDOTweenInitialized = false;

    public GameManager Session { get; private set; }
    public LevelReward LastReward { get; private set; }
    private BoardLogic boardLogic;
    private GameHudView hudView;
    private GameAudioFeedback audioFeedback;
    private Coroutine boardCheckCoroutine;
    private int boardRevision;
    private TileColliderInput tileInput;
    public static GridManager Instance;

    [Header("Gravity Settings")]
    public GravityDirection currentGravity = GravityDirection.Down;
    public float fallTime = 0.4f;

    [Header("Level Database")]
    public LevelDatabase levelDatabase;
    [Min(0)] public int levelIndex;

    [Header("Grid Settings")]
    public int columns = 10;
    public int rows = 5;
    public float tileSize = 110f;

    [Header("References")]
    public GameObject tilePrefab;
    public RectTransform boardContainer;
    public Sprite[] animalSprites;

    [Header("Score & Combo")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public int score => Session ? Session.Score : 0;
    public int combo => Session ? Session.Combo : 0;
    public float comboTimer => Session ? Session.ComboTimer : 0;
    public float comboDuration = 3f;

    public AnimalTile[,] gridArray;
    private AnimalTile firstSelectedTile;
    private AnimalTile secondSelectedTile;
    private Coroutine autoChainCoroutine;
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Button btnNextLevel;
    public GameObject levelOneWinBanner;
    private bool hasWonLevel => Session && Session.State == GameState.Win;
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textFinalScore;
    public TextMeshProUGUI rewardText;
    public GameObject winTrophy;

    [Header("Boosters")]
    public Button btnShuffle;
    public Button btnHint;
    private bool isBoosterCooldown = false;

    private AnimalTile hintedTile1;
    private AnimalTile hintedTile2;
    
    [Header("Time System")]
    public Slider timeSlider;
    public float maxTime = 60f;
    public float timeBonusPerMatch = 2f;
    public float currentTime => Session ? Session.TimeRemaining : 0;
    private bool isGameOver => !Session || Session.State != GameState.Playing;
    private int totalTilesRemaining = 0;

    [Header("Effects")]
    public GameObject floatingTextPrefab;

    [Header("Audio")]
    public EventReference sfxMatch;
    public EventReference sfxError; 
    public EventReference sfxCombo; 
    public EventReference sfxClick;
    public EventReference sfxWaterBreak;
    public EventReference sfxStoneBreak;
    public EventReference winSound;
    public EventReference loseSound;

    void Awake()
    {
        Instance = this;
        Session = GetComponent<GameManager>();
        if (!Session) Session = gameObject.AddComponent<GameManager>();
        Session.GameEnded += HandleGameEnded;
        hudView = new GameHudView(Session, this);
        audioFeedback = new GameAudioFeedback(Session, this);
        if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(false);
    }
    void Start()
    {
        var ui = FindObjectOfType<UIManager>();
        if (ui != null && ui.screen == GameScreen.Gameplay)
        {
            if (GameSession.HasSelectedLevel) levelIndex = GameSession.SelectedLevel;
            RestartGame();
            if (gridArray != null) WarmUpSystem();
            return;
        }
        GenerateGrid();
        WarmUpSystem();
        Session.ShowMenu();
        
    }

    private void WarmUpSystem()
    {

        if (!isDOTweenInitialized)
        {
            DOTween.Init();
            isDOTweenInitialized = true;
        }

        AnimalTile dummy1 = new GameObject("Dummy1").AddComponent<AnimalTile>();
        AnimalTile dummy2 = new GameObject("Dummy2").AddComponent<AnimalTile>();
        
        dummy1.gridX = -2; dummy1.gridY = -2; 
        dummy2.gridX = -2; dummy2.gridY = -2;
        
        FindPath(dummy1, dummy2);
        
        Destroy(dummy1.gameObject);
        Destroy(dummy2.gameObject);
    }

    public bool GenerateGrid()
    {
        if (levelDatabase == null) levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
        List<LevelCell> layout;
        if (levelDatabase != null)
        {
            if (levelDatabase.levels == null || levelIndex < 0 || levelIndex >= levelDatabase.levels.Count)
            { Debug.LogError("Level index khong hop le."); Session.ShowMenu(); return false; }
            var level = levelDatabase.levels[levelIndex];
            if (level == null || !level.Validate(levelDatabase.animalSprites.Length, out string error))
            { Debug.LogError("Level khong hop le. Kiem tra Tools > Level Database."); Session.ShowMenu(); return false; }
            columns = level.columns; rows = level.rows; tileSize = level.tileSize;
            maxTime = level.timeLimit; timeBonusPerMatch = level.timeBonus;
            currentGravity = level.gravity; animalSprites = levelDatabase.animalSprites;
            layout = level.mode == LevelLayoutMode.Manual ? level.cells :
                level.Generate(animalSprites.Length, level.randomEachPlay ? Random.Range(0, int.MaxValue) : level.seed);
        }
        else
        {
            var fallback = new LevelDefinition { columns = columns, rows = rows, animalKinds = animalSprites.Length };
            if (!fallback.Validate(animalSprites.Length, out string error))
            { Debug.LogError(error); Session.ShowMenu(); return false; }
            layout = fallback.Generate(animalSprites.Length, Random.Range(0, int.MaxValue));
        }
        if (tilePrefab == null || boardContainer == null)
        { Debug.LogError("Thieu tilePrefab hoac boardContainer."); Session.ShowMenu(); return false; }
        int totalTiles = 0;

        gridArray = new AnimalTile[columns, rows];
        if(!tileInput) tileInput=boardContainer.GetComponent<TileColliderInput>();
        if(!tileInput) tileInput=boardContainer.gameObject.AddComponent<TileColliderInput>();
        tileInput.Configure(this,columns*rows,new Vector2(columns+1,rows+1)*tileSize);
        float startX = -(columns / 2f * tileSize) + (tileSize / 2f);
        float startY = (rows / 2f * tileSize) - (tileSize / 2f);

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                LevelCell cell = layout[x + y * columns];
                if (cell.empty) continue;
                totalTiles++;
                GameObject newTile = Instantiate(tilePrefab, boardContainer);
                newTile.name = $"Tile_{x}_{y}";
                RectTransform rect = newTile.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(startX + (x * tileSize), startY - (y * tileSize));

                int assignedID = cell.animalID;
                AnimalTile tileScript = newTile.GetComponent<AnimalTile>();
                tileScript.SetupTile(assignedID, animalSprites[assignedID], x, y, cell.state);
                tileScript.ConfigureColliderInput(tileInput);
                gridArray[x, y] = tileScript;

            }
        }
        totalTilesRemaining = totalTiles;
        boardRevision++;
        return true;
    }

    public void SelectTile(AnimalTile clickedTile)
    {
        if (isGameOver) return;

        if (clickedTile.currentState == TileState.Hidden || clickedTile.currentState == TileState.Barrier) 
            return; 
        Session.NotifyTileSelected();
        ClearHint();

        if (firstSelectedTile == clickedTile)
        {
            firstSelectedTile.SetHighlight(false);
            firstSelectedTile = null;
            return;
        }

        clickedTile.SetHighlight(true);
        if (firstSelectedTile == null) firstSelectedTile = clickedTile;
        else
        {
            secondSelectedTile = clickedTile;
            CheckMatch(firstSelectedTile, secondSelectedTile);
        }
    }

    private void CheckMatch(AnimalTile tile1, AnimalTile tile2)
    {
        if (tile1.animalID == tile2.animalID)
        {
            if (FindPath(tile1, tile2))
            {
                ProcessMatch(tile1, tile2);
                return;
            }
        }
        Session.NotifyInvalidMatch();
        tile1.SetHighlight(false);
        tile2.SetHighlight(false);
        firstSelectedTile = null;
        secondSelectedTile = null;
    }

    private void ProcessMatch(AnimalTile t1, AnimalTile t2)
    {
        if (isGameOver || !t1 || !t2) return;
        boardRevision++;
        Session.RecordMatch(t1.rect.anchoredPosition);

        int x1 = t1.gridX, y1 = t1.gridY;
        int x2 = t2.gridX, y2 = t2.gridY;

        BreakTile(t1);
        BreakTile(t2);

        BreakNeighborBarriers(x1, y1);
        BreakNeighborBarriers(x2, y2);

        if (firstSelectedTile != null) firstSelectedTile.SetHighlight(false);
        if (secondSelectedTile != null) secondSelectedTile.SetHighlight(false);
        firstSelectedTile = null;
        secondSelectedTile = null;

        ApplyGravity();
        if (totalTilesRemaining <= 0) GameOver(true);
        else ScheduleBoardCheck();
    }

    private void BreakTile(AnimalTile t)
    {
        if (t.currentState == TileState.Ice)
        {
            t.SetState(TileState.Normal);
            Session.NotifyTileBroken(TileState.Ice);
        }
        else
        {
            gridArray[t.gridX, t.gridY] = null;
            totalTilesRemaining--;
            t.DisableColliderInput();
            t.rect.DOKill();
            Destroy(t.gameObject);
        }
    }

    private void BreakNeighborBarriers(int x, int y)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;

            if (nx >= 0 && nx < columns && ny >= 0 && ny < rows)
            {
                AnimalTile neighbor = gridArray[nx, ny];
                if (neighbor != null && neighbor.currentState == TileState.Barrier)
                {
                    neighbor.SetState(TileState.Normal);
                    Session.NotifyTileBroken(TileState.Barrier);
                    
                }
            }
        }
    }

    private bool FindPath(AnimalTile t1, AnimalTile t2)
    {
        if (!t1 || !t2 || gridArray == null) return false;
        CaptureBoard();
        return boardLogic.CanMatch(new Vector2Int(t1.gridX,t1.gridY),new Vector2Int(t2.gridX,t2.gridY));
    }

    private void ApplyGravity()
    {
        CaptureBoard();
        var moves=boardLogic.ApplyGravity(currentGravity);
        var movedTiles=new List<AnimalTile>();
        foreach(var move in moves)
        {
            var tile=gridArray[move.from.x,move.from.y];
            gridArray[move.to.x,move.to.y]=tile;
            gridArray[move.from.x,move.from.y]=null;
            tile.gridX=move.to.x; tile.gridY=move.to.y;
            tile.rect.DOKill();
            tile.rect.DOAnchorPos(TilePosition(move.to),fallTime).SetEase(Ease.OutBounce);
            movedTiles.Add(tile);
        }
        if(movedTiles.Count>0)
        {
            if(autoChainCoroutine!=null) StopCoroutine(autoChainCoroutine);
            autoChainCoroutine=StartCoroutine(CheckAutoChainCoroutine(movedTiles,fallTime+.1f));
        }
    }

    private System.Collections.IEnumerator CheckAutoChainCoroutine(List<AnimalTile> movedTiles, float delay)
    {
        yield return new WaitForSeconds(delay);
        autoChainCoroutine = null;
        if (isGameOver) yield break;

        foreach (AnimalTile tile in movedTiles)
        {
            if (tile == null || tile.gameObject == null) continue;

            if (tile.currentState == TileState.Barrier || tile.currentState == TileState.Hidden) continue;

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in directions)
            {
                int neighborX = tile.gridX + dir.x;
                int neighborY = tile.gridY + dir.y;

                if (neighborX >= 0 && neighborX < columns && neighborY >= 0 && neighborY < rows)
                {
                    AnimalTile neighbor = gridArray[neighborX, neighborY];

                    if (neighbor != null && neighbor != tile && 
                        neighbor.currentState != TileState.Hidden && 
                        neighbor.currentState != TileState.Barrier)
                    {
                        if (neighbor.animalID == tile.animalID)
                        {
                            ProcessMatch(tile, neighbor);
                            yield break; 
                        }
                    }
                }
            }
        }
    }

    public void ShuffleBoard()
    {
        if(isGameOver || isBoosterCooldown) return;
        var tiles=new List<AnimalTile>();
        var positions=new List<Vector2Int>();
        foreach(var tile in gridArray)
            if(tile) { tiles.Add(tile); positions.Add(new Vector2Int(tile.gridX,tile.gridY)); }
        if(tiles.Count<2) { CheckBoardAvailability(); return; }
        if(!EconomyStore.TryUseBooster(BoosterType.Shuffle))
        {
            Session.Notify("No shuffles left. Buy more in the shop from the menu.");
            return;
        }
        StartCoroutine(BoosterCooldownRoutine());
        ClearHint();
        if(autoChainCoroutine!=null) { StopCoroutine(autoChainCoroutine); autoChainCoroutine=null; }
        for(int attempt=0;attempt<30;attempt++)
        {
            for(int i=0;i<positions.Count;i++)
            {
                int other=Random.Range(i,positions.Count);
                var temp=positions[i]; positions[i]=positions[other]; positions[other]=temp;
            }
            System.Array.Clear(gridArray,0,gridArray.Length);
            for(int i=0;i<tiles.Count;i++)
            {
                var tile=tiles[i]; var pos=positions[i];
                tile.gridX=pos.x; tile.gridY=pos.y; gridArray[pos.x,pos.y]=tile;
            }
            CaptureBoard();
            if(boardLogic.TryFindPair(out _,out _)) break;
        }
        foreach(var tile in tiles)
        {
            tile.rect.DOKill();
            tile.rect.DOAnchorPos(TilePosition(new Vector2Int(tile.gridX,tile.gridY)),.5f).SetEase(Ease.InOutQuad);
        }
        if(firstSelectedTile) firstSelectedTile.SetHighlight(false);
        firstSelectedTile=secondSelectedTile=null;
        boardRevision++;
        ScheduleBoardCheck();
    }

    public void ShowHint()
    {
        if(isGameOver || isBoosterCooldown) return;
        CaptureBoard();
        bool available=boardLogic.TryFindPair(out var first,out var second);
        Session.SetBoardAvailability(available);
        if(!available) return;
        if(!EconomyStore.TryUseBooster(BoosterType.Hint))
        {
            Session.Notify("No hints left. Buy more in the shop from the menu.");
            return;
        }
        StartCoroutine(BoosterCooldownRoutine());
        ClearHint();
        hintedTile1=gridArray[first.x,first.y]; hintedTile2=gridArray[second.x,second.y];
        hintedTile1.rect.DOKill(); hintedTile2.rect.DOKill();
        hintedTile1.rect.localScale=hintedTile2.rect.localScale=Vector3.one;
        hintedTile1.rect.DOPunchScale(new Vector3(.3f,.3f,.3f),1,5);
        hintedTile2.rect.DOPunchScale(new Vector3(.3f,.3f,.3f),1,5);
        hintedTile1.SetHighlight(true); hintedTile2.SetHighlight(true);
    }

    private void ClearHint()
    {

        if (hintedTile1 != null && hintedTile1 != firstSelectedTile) hintedTile1.SetHighlight(false);
        if (hintedTile2 != null && hintedTile2 != firstSelectedTile) hintedTile2.SetHighlight(false);
        
        hintedTile1 = null;
        hintedTile2 = null;
    }

    private System.Collections.IEnumerator BoosterCooldownRoutine()
    {
        isBoosterCooldown = true;

        if (btnShuffle != null) btnShuffle.interactable = false;
        if (btnHint != null) btnHint.interactable = false;
        
        yield return new WaitForSeconds(1.0f); 
        
        isBoosterCooldown = false;

        if (btnShuffle != null) btnShuffle.interactable = true;
        if (btnHint != null) btnHint.interactable = true;
    }
    private void GameOver(bool isWin)
    {
        Session.Finish(isWin);
    }

    private void HandleGameEnded(bool won)
    {
        if(won && levelDatabase && levelDatabase.levels!=null)
        {
            LevelProgress.Won(levelIndex,levelDatabase.levels.Count);
            var level=levelDatabase.levels[levelIndex];
            LastReward=EconomyStore.RewardWin(level.hintReward,level.shuffleReward);
        }
        StopAllCoroutines();
        autoChainCoroutine=boardCheckCoroutine=null;
    }

    public void PlayUIButtonSound()
    {
        Session.NotifyTileSelected();
    }

    public bool SelectLevel(int index)
    {
        if (levelDatabase == null) levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
        if (levelDatabase == null || levelDatabase.levels == null || index < 0 || index >= levelDatabase.levels.Count)
        { Debug.LogError("Level index khong hop le."); return false; }
        levelIndex = index;
        GameSession.SelectLevel(index);
        return true;
    }

    public void PauseGameplay()
    {
        Session.ShowMenu();
        StopAllCoroutines();
        autoChainCoroutine=boardCheckCoroutine=null;
        isBoosterCooldown=false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(false);
    }

    public bool HasNextLevel()
    {
        return levelDatabase != null && levelDatabase.levels != null &&
            levelIndex >= 0 && levelIndex + 1 < levelDatabase.levels.Count;
    }

    public void NextLevel()
    {
        if (!isGameOver || !hasWonLevel || !HasNextLevel()) return;
        var next = levelDatabase.levels[levelIndex + 1];
        if (next == null || !next.Validate(levelDatabase.animalSprites.Length, out string error))
        {
            Debug.LogError("Man tiep theo chua hop le. Kiem tra Tools > Level Database.");
            return;
        }
        levelIndex++;
        RestartGame();
    }

    public void RestartGame()
    {
        LastReward=default;
        if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(false);

        DOTween.KillAll();
        StopAllCoroutines(); autoChainCoroutine = null;
        isBoosterCooldown = false;
        firstSelectedTile = null; secondSelectedTile = null;
        boardCheckCoroutine = null;
        boardRevision++;

        if (gridArray != null)
        {
            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < gridArray.GetLength(1); y++)
                {
                    if (gridArray[x, y] != null)
                    {
                        gridArray[x, y].DisableColliderInput();
                        Destroy(gridArray[x, y].gameObject);
                        gridArray[x, y] = null;
                    }
                }
            }
            System.Array.Clear(gridArray, 0, gridArray.Length);
        }

        ClearHint();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (btnShuffle != null) btnShuffle.interactable = true;
        if (btnHint != null) btnHint.interactable = true;

        Session.ShowMenu();
        if(GenerateGrid())
        {
            LevelProgress.Started(levelIndex);
            var ui=FindObjectOfType<UIManager>();
            if(ui && ui.screen==GameScreen.Gameplay) GameSession.SelectLevel(levelIndex);
            Session.BeginLevel(maxTime,timeBonusPerMatch,comboDuration);
            ScheduleBoardCheck();
        }
    }

    public void SetPaused(bool paused)
    {
        if(paused) Session.Pause(); else Session.Resume();
    }


    private Vector2 TilePosition(Vector2Int position)
    {
        return new Vector2(-(columns/2f*tileSize)+tileSize/2f+position.x*tileSize,
            rows/2f*tileSize-tileSize/2f-position.y*tileSize);
    }

    private void CaptureBoard()
    {
        int width=gridArray.GetLength(0), height=gridArray.GetLength(1);
        if(boardLogic==null || boardLogic.Columns!=width || boardLogic.Rows!=height)
            boardLogic=new BoardLogic(width,height);
        for(int x=0;x<width;x++)
            for(int y=0;y<height;y++)
            {
                var tile=gridArray[x,y];
                boardLogic.SetCell(x,y,tile ? new BoardCell { occupied=true,animalID=tile.animalID,state=tile.currentState } : default);
            }
    }

    public bool CheckBoardAvailability()
    {
        if(gridArray==null || Session.State!=GameState.Playing) return true;
        CaptureBoard();
        bool available=boardLogic.TryFindPair(out _,out _);
        Session.SetBoardAvailability(available);
        return available;
    }

    private void ScheduleBoardCheck()
    {
        if(boardCheckCoroutine!=null) StopCoroutine(boardCheckCoroutine);
        boardCheckCoroutine=StartCoroutine(CheckBoardAfterMovement(boardRevision));
    }

    private System.Collections.IEnumerator CheckBoardAfterMovement(int revision)
    {
        yield return new WaitForSeconds(Mathf.Max(fallTime,.5f)+.15f);
        boardCheckCoroutine=null;
        if(revision==boardRevision && autoChainCoroutine==null && !isGameOver) CheckBoardAvailability();
    }

    private void OnDestroy()
    {
        if(Session) Session.GameEnded-=HandleGameEnded;
        hudView?.Dispose(); audioFeedback?.Dispose();
        if(Instance==this) Instance=null;
    }
}
