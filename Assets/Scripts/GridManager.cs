using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;

public enum GravityDirection { None, Down, Up, Left, Right }

public class GridManager : MonoBehaviour
{   
    private static bool isDOTweenInitialized = false;
    private static readonly Vector2Int[] NeighborDirections =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public GameManager Session { get; private set; }
    public LevelReward LastReward { get; private set; }
    public LevelRunResult LastResult {get;private set;}
    private ProgressTracker progressTracker;
    private int initialWater,initialStone;
    private BoardLogic boardLogic;
    private GameHudView hudView;
    private GameAudioFeedback audioFeedback;
    private Coroutine boardCheckCoroutine;
    private int boardRevision;
    private TileColliderInput tileInput;
    private MatchPathGraphic matchPath;
    private Coroutine matchPathCoroutine;
    private readonly List<Vector2Int> matchPoints=new List<Vector2Int>(4);
    private float boardMovingUntil;
    private bool BoardBusy => Session.IsResolvingMatch || Time.time<boardMovingUntil;
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
    private int initialTileCount;
    public float ClearProgress => initialTileCount>0 ? Mathf.Clamp01(1-totalTilesRemaining/(float)initialTileCount) : 0;

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
        Session.StateChanged += HandleMatchPathState;
        progressTracker=new ProgressTracker(this);
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
        if (gridArray != null) CaptureBoard();
    }

    private List<LevelCell> designedLayout;

    public bool GenerateGrid()
    {
        if (levelDatabase == null) levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
        List<LevelCell> layout;
        designedLayout = null;
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
            if (level.mode == LevelLayoutMode.Manual && level.useDesignedBoardRules) designedLayout = layout;
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
        initialWater=initialStone=0;

        gridArray = new AnimalTile[columns, rows];
        if(!tileInput) tileInput=boardContainer.GetComponent<TileColliderInput>();
        if(!tileInput) tileInput=boardContainer.gameObject.AddComponent<TileColliderInput>();
        tileInput.Configure(this,columns*rows,new Vector2(columns+1,rows+1)*tileSize);
        if(!matchPath)
        {
            var effect=new GameObject("MatchPath",typeof(RectTransform),typeof(CanvasRenderer),typeof(MatchPathGraphic));
            effect.transform.SetParent(boardContainer,false);
            matchPath=effect.GetComponent<MatchPathGraphic>();
            matchPath.raycastTarget=false;matchPath.maskable=false;
            matchPath.rectTransform.anchorMin=matchPath.rectTransform.anchorMax=boardContainer.pivot;
            matchPath.rectTransform.pivot=new Vector2(.5f,.5f);
        }
        matchPath.rectTransform.sizeDelta=new Vector2(columns+2,rows+2)*tileSize;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                LevelCell cell = layout[x + y * columns];
                if (cell.empty) continue;
                if(cell.state==TileState.Ice) initialWater++;
                if(cell.state==TileState.Barrier) initialStone++;
                totalTiles++;
                GameObject newTile = Instantiate(tilePrefab, boardContainer);
                newTile.name = $"Tile_{x}_{y}";
                RectTransform rect = newTile.GetComponent<RectTransform>();
                rect.anchoredPosition = TilePosition(new Vector2Int(x, y));

                int assignedID = cell.animalID;
                AnimalTile tileScript = newTile.GetComponent<AnimalTile>();
                tileScript.SetupTile(assignedID, animalSprites[assignedID], x, y, cell.state);
                tileScript.ConfigureColliderInput(tileInput);
                gridArray[x, y] = tileScript;

            }
        }
        totalTilesRemaining = totalTiles;
        initialTileCount=totalTiles;
        boardRevision++;
        return true;
    }

    public bool TutorialBoardBusy => BoardBusy || isBoosterCooldown;

    public void PracticeBooster(BoosterType booster)
    {
        practicingBooster = true;
        try
        {
            if (booster == BoosterType.Hint) ShowHint();
            else ShuffleBoard();
        }
        finally { practicingBooster = false; }
    }

    private bool practicingBooster;

    public bool FindTutorialPair(TileState target, out AnimalTile first, out AnimalTile second, bool opensWater = false)
    {
        CaptureBoard();
        first = second = null;
        foreach (var a in gridArray)
        {
            if (!a || !a.IsSelectable) continue;
            if (target == TileState.Ice && a.currentState != TileState.Ice) continue;
            if (target == TileState.Barrier)
            {
                if (a.currentState != TileState.Normal) continue;
                bool besideBarrier = false;
                foreach (var direction in NeighborDirections)
                {
                    var tile = TileAt(a.gridX + direction.x, a.gridY + direction.y);
                    if (tile && tile.currentState == TileState.Barrier) { besideBarrier = true; break; }
                }
                if (!besideBarrier) continue;
            }

            foreach (var b in gridArray)
            {
                if (!b || b.currentState != TileState.Normal || a.animalID != b.animalID) continue;
                if (!boardLogic.CanMatch(new Vector2Int(a.gridX, a.gridY), new Vector2Int(b.gridX, b.gridY))) continue;
                if (opensWater && !OpensWaterPath(a, b)) continue;
                first = a;
                second = b;
                return true;
            }
        }
        return false;
    }

    private bool OpensWaterPath(AnimalTile first, AnimalTile second)
    {
        var a = new Vector2Int(first.gridX, first.gridY);
        var b = new Vector2Int(second.gridX, second.gridY);
        var firstCell = boardLogic.GetCell(a.x, a.y);
        var secondCell = boardLogic.GetCell(b.x, b.y);
        boardLogic.SetCell(a.x, a.y, default);
        boardLogic.SetCell(b.x, b.y, default);
        try
        {
            foreach (var waterTile in gridArray)
            {
                if (!waterTile || waterTile.currentState != TileState.Ice) continue;
                var from = new Vector2Int(waterTile.gridX, waterTile.gridY);
                foreach (var normal in gridArray)
                {
                    if (!normal || normal == first || normal == second ||
                        normal.currentState != TileState.Normal || normal.animalID != waterTile.animalID) continue;
                    if (boardLogic.CanMatch(from, new Vector2Int(normal.gridX, normal.gridY))) return true;
                }
            }
            return false;
        }
        finally
        {
            boardLogic.SetCell(a.x, a.y, firstCell);
            boardLogic.SetCell(b.x, b.y, secondCell);
        }
    }

    public void SelectTile(AnimalTile clickedTile)
    {
        if (isGameOver || BoardBusy || !clickedTile) return;

        if (!clickedTile.IsSelectable) return;
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

    private void ClearSelection()
    {
        if (firstSelectedTile) firstSelectedTile.SetHighlight(false);
        if (secondSelectedTile) secondSelectedTile.SetHighlight(false);
        firstSelectedTile = secondSelectedTile = null;
    }

    private void CheckMatch(AnimalTile tile1, AnimalTile tile2)
    {
        if (ProcessMatch(tile1,tile2)) return;
        Session.NotifyInvalidMatch();
        ClearSelection();
    }

    private bool ProcessMatch(AnimalTile t1,AnimalTile t2)
    {
        if(isGameOver || BoardBusy || !FindPath(t1,t2)) return false;
        if(matchPathCoroutine!=null) StopCoroutine(matchPathCoroutine);
        if(autoChainCoroutine!=null) {StopCoroutine(autoChainCoroutine);autoChainCoroutine=null;}
        Session.IsResolvingMatch=true;
        matchPath.ShowPath(matchPoints,TilePosition(Vector2Int.zero),tileSize,columns,rows);
        matchPathCoroutine=StartCoroutine(ShowMatchPath(t1,t2));
        return true;
    }

    private System.Collections.IEnumerator ShowMatchPath(AnimalTile first,AnimalTile second)
    {
        float elapsed=0;
        while(elapsed<.14f)
        {
            yield return null;
            if(Session.State==GameState.Playing)
            {
                elapsed+=Time.deltaTime;
                matchPath.SetReveal(elapsed/.1f);
            }
        }
        Session.IsResolvingMatch=false;
        CompleteMatch(first,second);
        if(isGameOver) yield break;
        elapsed=0;
        while(elapsed<.14f)
        {
            yield return null;
            if(Session.State!=GameState.Playing) continue;
            elapsed+=Time.deltaTime;
            matchPath.canvasRenderer.SetAlpha(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.14f)));
        }
        matchPath.ClearPath();matchPathCoroutine=null;
    }

    private void ClearMatchPath()
    {
        if(matchPathCoroutine!=null) StopCoroutine(matchPathCoroutine);
        matchPathCoroutine=null;
        if(Session) Session.IsResolvingMatch=false;
        if(matchPath) matchPath.ClearPath();
        ClearSelection();
    }

    private void HandleMatchPathState(GameState state)
    {
        if(state==GameState.Paused) {if(matchPath) matchPath.canvasRenderer.SetAlpha(0);return;}
        if(state==GameState.Playing) {if(matchPath) matchPath.canvasRenderer.SetAlpha(1);return;}
        ClearMatchPath();
    }

    private void CompleteMatch(AnimalTile t1, AnimalTile t2)
    {
        if (isGameOver || !t1 || !t2) return;
        boardRevision++;
        Session.RecordMatch(t1.rect.anchoredPosition);

        int x1 = t1.gridX, y1 = t1.gridY;
        int x2 = t2.gridX, y2 = t2.gridY;

        bool firstRemoved = t1.currentState != TileState.Ice;
        bool secondRemoved = t2.currentState != TileState.Ice;
        BreakTile(t1);
        BreakTile(t2);

        if (designedLayout == null || firstRemoved) BreakNeighborBarriers(x1, y1);
        if (designedLayout == null || secondRemoved) BreakNeighborBarriers(x2, y2);

        ClearSelection();

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
            DestroyTile(t);
        }
    }

    private void DestroyTile(AnimalTile tile)
    {
        tile.DisableColliderInput();
        tile.rect.DOKill();
        Destroy(tile.gameObject);
    }

    private AnimalTile TileAt(int x, int y) =>
        x >= 0 && x < columns && y >= 0 && y < rows ? gridArray[x, y] : null;

    private void BreakNeighborBarriers(int x, int y)
    {
        foreach (var direction in NeighborDirections)
        {
            var neighbor = TileAt(x + direction.x, y + direction.y);
            if (!neighbor || neighbor.currentState != TileState.Barrier) continue;
            neighbor.SetState(TileState.Normal);
            Session.NotifyTileBroken(TileState.Barrier);
        }
    }

    private bool FindPath(AnimalTile t1, AnimalTile t2)
    {
        if (!t1 || !t2 || gridArray == null) return false;
        CaptureBoard();
        return boardLogic.TryGetMatchPath(new Vector2Int(t1.gridX,t1.gridY),new Vector2Int(t2.gridX,t2.gridY),matchPoints);
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
            boardMovingUntil=Time.time+fallTime;
            if(autoChainCoroutine!=null) StopCoroutine(autoChainCoroutine);
            autoChainCoroutine=StartCoroutine(CheckAutoChainCoroutine(movedTiles,fallTime+.1f));
        }
    }

    private System.Collections.IEnumerator CheckAutoChainCoroutine(List<AnimalTile> movedTiles, float delay)
    {
        yield return new WaitForSeconds(delay);
        autoChainCoroutine = null;
        if (isGameOver) yield break;
        foreach (var tile in movedTiles)
        {
            if (!tile || !tile.IsSelectable) continue;
            foreach (var direction in NeighborDirections)
            {
                var neighbor = TileAt(tile.gridX + direction.x, tile.gridY + direction.y);
                if (!neighbor || neighbor == tile || !neighbor.IsSelectable || neighbor.animalID != tile.animalID) continue;
                if (designedLayout != null && (tile.currentState == TileState.Ice || neighbor.currentState == TileState.Ice)) continue;
                if(ProcessMatch(tile,neighbor)) yield break;
            }
        }
    }

    public void ShuffleBoard()
    {
        if(isGameOver || isBoosterCooldown || BoardBusy) return;
        ClearMatchPath();
        var tiles=new List<AnimalTile>();
        var positions=new List<Vector2Int>();
        foreach(var tile in gridArray)
            if(tile && (designedLayout == null || tile.currentState != TileState.Barrier)) { tiles.Add(tile); positions.Add(new Vector2Int(tile.gridX,tile.gridY)); }
        if(tiles.Count<2) { CheckBoardAvailability(); return; }
        if (!TryStartBooster(BoosterType.Shuffle)) return;
        ClearHint();
        if(autoChainCoroutine!=null) { StopCoroutine(autoChainCoroutine); autoChainCoroutine=null; }
        for(int attempt=0;attempt<30;attempt++)
        {
            for(int i=0;i<positions.Count;i++)
            {
                int other=Random.Range(i,positions.Count);
                var temp=positions[i]; positions[i]=positions[other]; positions[other]=temp;
            }
            foreach (var pos in positions) gridArray[pos.x,pos.y] = null;
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
        boardMovingUntil=Time.time+.5f;
        ClearSelection();
        boardRevision++;
        ScheduleBoardCheck();
    }

    public void ShowHint()
    {
        if(isGameOver || isBoosterCooldown || BoardBusy) return;
        CaptureBoard();
        bool available=boardLogic.TryFindPair(out var first,out var second);
        Session.SetBoardAvailability(available);
        if(!available) return;
        if (!TryStartBooster(BoosterType.Hint)) return;
        ClearHint();
        hintedTile1=gridArray[first.x,first.y]; hintedTile2=gridArray[second.x,second.y];
        HighlightHint(hintedTile1);
        HighlightHint(hintedTile2);
    }

    private static void HighlightHint(AnimalTile tile)
    {
        tile.rect.DOKill();
        tile.rect.localScale = Vector3.one;
        tile.rect.DOPunchScale(new Vector3(.3f,.3f,.3f),1,5);
        tile.SetHighlight(true);
    }

    private bool TryStartBooster(BoosterType booster)
    {
        if (!BoosterAvailable(booster)) return false;
        if (!practicingBooster && !EconomyStore.TryUseBooster(booster))
        {
            string stock = booster == BoosterType.Hint ? "hints" : "shuffles";
            Session.Notify("No " + stock + " left. Buy more in the shop from the menu.");
            return false;
        }
        StartCoroutine(BoosterCooldownRoutine());
        return true;
    }

    private bool BoosterAvailable(BoosterType type)
    {
        var unlocks=FindObjectOfType<FirstBoosterRewardUI>();
        return unlocks ? unlocks.CanUse(type) : EconomyStore.IsBoosterUnlocked(type);
    }

    private void SetBoostersInteractable(bool value)
    {
        if (btnShuffle) btnShuffle.interactable = value && BoosterAvailable(BoosterType.Shuffle);
        if (btnHint) btnHint.interactable = value && BoosterAvailable(BoosterType.Hint);
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

        SetBoostersInteractable(false);
        
        yield return new WaitForSeconds(1.0f); 
        
        isBoosterCooldown = false;

        SetBoostersInteractable(true);
    }
    private bool finishingCoffee;
    private void GameOver(bool isWin)
    {
        var coffee=FindObjectOfType<CoffeeMatchEffects>();
        if(finishingCoffee) return;
        if(isWin && coffee) {finishingCoffee=true; SetBoostersInteractable(false); StartCoroutine(FinishCoffee(coffee));return;}
        Session.Finish(isWin);
    }
    private System.Collections.IEnumerator FinishCoffee(CoffeeMatchEffects coffee)
    {
        Session.IsResolvingMatch=true;
        while(coffee && coffee.HasPendingCoffee && (Session.State==GameState.Playing || Session.State==GameState.Paused)) yield return null;
        while(Session.State==GameState.Paused) yield return null;
        if(coffee && Session.State==GameState.Playing) yield return coffee.PourFinalCup();
        while(Session.State==GameState.Paused) yield return null;
        if(Session.State==GameState.Playing) Session.Finish(true);
        finishingCoffee=false;
    }

    private void HandleGameEnded(bool won)
    {
        LastResult=progressTracker.Finish(won);
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
        finishingCoffee=false;
        progressTracker.Flush();LastResult=default;
        ClearMatchPath();boardMovingUntil=0;
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
            foreach (var tile in gridArray)
                if (tile) DestroyTile(tile);
            System.Array.Clear(gridArray, 0, gridArray.Length);
        }

        ClearHint();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        SetBoostersInteractable(true);

        Session.ShowMenu();
        if(GenerateGrid())
        {
            LevelProgress.Started(levelIndex);
            var ui=FindObjectOfType<UIManager>();
            if(ui && ui.screen==GameScreen.Gameplay) GameSession.SelectLevel(levelIndex);
            var challenge=levelDatabase && levelIndex<levelDatabase.levels.Count ? levelDatabase.levels[levelIndex].challenge : null;
            Session.BeginLevel(maxTime,timeBonusPerMatch,comboDuration,challenge,initialWater,initialStone);
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
        boardLogic.UseDesignedBoardRules = designedLayout != null;
        for(int x=0;x<width;x++)
            for(int y=0;y<height;y++)
            {
                var tile=gridArray[x,y];
                boardLogic.SetCell(x,y,tile ? new BoardCell { occupied=true,animalID=tile.animalID,state=tile.currentState } : new BoardCell { blocksGravity = designedLayout != null && designedLayout[x + y * width].empty });
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
        progressTracker?.Dispose();
        if(Session) {Session.GameEnded-=HandleGameEnded;Session.StateChanged-=HandleMatchPathState;}
        hudView?.Dispose(); audioFeedback?.Dispose();
        if(Instance==this) Instance=null;
    }

    private void OnDisable() {ClearMatchPath();}
    private void OnApplicationPause(bool paused) {if(paused) progressTracker?.Flush();}
    private void OnApplicationQuit() {progressTracker?.Flush();}
}
