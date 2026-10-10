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

    private Dictionary<int, List<AnimalTile>> hintCache = new Dictionary<int, List<AnimalTile>>();
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
    public int score = 0;
    public int combo = 0;
    public float comboTimer = 0f;
    public float comboDuration = 3f;

    public AnimalTile[,] gridArray;
    private AnimalTile firstSelectedTile;
    private AnimalTile secondSelectedTile;
    private Coroutine autoChainCoroutine;
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Button btnNextLevel;
    public GameObject levelOneWinBanner;
    private bool hasWonLevel;
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textFinalScore;

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
    private float currentTime;
    private bool isGameOver = false;
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
        isGameOver = true;
        
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
    void Update()
    {
        if (isGameOver) return;


        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (timeSlider != null) timeSlider.value = currentTime / maxTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                GameOver(false);
            }
        }


        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                combo = 0;
                UpdateScoreUI();
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"Điểm: {score}";
        if (comboText != null) {
            if (combo > 1) comboText.text = $"Combo: x{combo}!";
            else comboText.text = "";
        }
    }

    public void GenerateGrid()
    {
        if (levelDatabase == null) levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
        List<LevelCell> layout;
        if (levelDatabase != null)
        {
            if (levelDatabase.levels == null || levelIndex < 0 || levelIndex >= levelDatabase.levels.Count)
            { Debug.LogError("Level index khong hop le."); isGameOver = true; return; }
            var level = levelDatabase.levels[levelIndex];
            if (level == null || !level.Validate(levelDatabase.animalSprites.Length, out string error))
            { Debug.LogError("Level khong hop le. Kiem tra Tools > Level Database."); isGameOver = true; return; }
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
            { Debug.LogError(error); isGameOver = true; return; }
            layout = fallback.Generate(animalSprites.Length, Random.Range(0, int.MaxValue));
        }
        if (tilePrefab == null || boardContainer == null)
        { Debug.LogError("Thieu tilePrefab hoac boardContainer."); isGameOver = true; return; }
        int totalTiles = 0;

        gridArray = new AnimalTile[columns, rows];
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
                gridArray[x, y] = tileScript;

            }
        }
        totalTilesRemaining = totalTiles;
        currentTime = maxTime;
        if (timeSlider != null) timeSlider.value = 1;
        isGameOver = false;
    }

    public void SelectTile(AnimalTile clickedTile)
    {
        if (isGameOver) return;

        if (clickedTile.currentState == TileState.Hidden || clickedTile.currentState == TileState.Barrier) 
            return; 
        RuntimeManager.PlayOneShot(sfxClick);
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
        RuntimeManager.PlayOneShot(sfxError);
        tile1.SetHighlight(false);
        tile2.SetHighlight(false);
        firstSelectedTile = null;
        secondSelectedTile = null;
    }


    private void ProcessMatch(AnimalTile t1, AnimalTile t2)
    {
        combo++;
        comboTimer = comboDuration;
        int matchScore = 100 * combo;
        score += matchScore;
        UpdateScoreUI();
        RuntimeManager.PlayOneShot(sfxMatch);
        Vector2 spawnPos = t1.rect.anchoredPosition;


        SpawnFloatingText(spawnPos, "+" + matchScore, Color.white);

        
        if (combo > 0 && combo % 5 == 0)
        {
            currentTime += timeBonusPerMatch;
            if (currentTime > maxTime) currentTime = maxTime;
            SpawnFloatingText(spawnPos + new Vector2(0, 40), "+" + timeBonusPerMatch + " GIÂY!", Color.yellow);
            RuntimeManager.PlayOneShot(sfxCombo);

        
        }

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
    }


    private void BreakTile(AnimalTile t)
    {
        if (t.currentState == TileState.Ice)
        {
            t.SetState(TileState.Normal);
            if (!sfxWaterBreak.IsNull) RuntimeManager.PlayOneShot(sfxWaterBreak);
        }
        else
        {
            gridArray[t.gridX, t.gridY] = null;
            totalTilesRemaining--;
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
                    if (!sfxStoneBreak.IsNull) RuntimeManager.PlayOneShot(sfxStoneBreak);
                    
                }
            }
        }
    }


    private bool FindPath(AnimalTile t1, AnimalTile t2)
    {
        int x1 = t1.gridX, y1 = t1.gridY;
        int x2 = t2.gridX, y2 = t2.gridY;


        if (x1 == x2 && CheckLine(x1, y1, x2, y2)) return true;
        if (y1 == y2 && CheckLine(x1, y1, x2, y2)) return true;


        if (IsEmpty(x1, y2) && CheckLine(x1, y1, x1, y2) && CheckLine(x1, y2, x2, y2)) return true;
        if (IsEmpty(x2, y1) && CheckLine(x1, y1, x2, y1) && CheckLine(x2, y1, x2, y2)) return true;


        for (int x = x1 + 1; x <= columns; x++) {
            if (!IsEmpty(x, y1)) break; 

            if (IsEmpty(x, y2) && CheckLine(x, y1, x, y2) && CheckLine(x, y2, x2, y2)) return true;
        }
        for (int x = x1 - 1; x >= -1; x--) {
            if (!IsEmpty(x, y1)) break;
            if (IsEmpty(x, y2) && CheckLine(x, y1, x, y2) && CheckLine(x, y2, x2, y2)) return true;
        }
        

        for (int y = y1 + 1; y <= rows; y++) {
            if (!IsEmpty(x1, y)) break;
            if (IsEmpty(x2, y) && CheckLine(x1, y, x2, y) && CheckLine(x2, y, x2, y2)) return true;
        }
        for (int y = y1 - 1; y >= -1; y--) {
            if (!IsEmpty(x1, y)) break;
            if (IsEmpty(x2, y) && CheckLine(x1, y, x2, y) && CheckLine(x2, y, x2, y2)) return true;
        }

        return false;
    }

    private bool CheckLine(int x1, int y1, int x2, int y2)
    {

        if (x1 == x2 && y1 == y2) return true;

        if (x1 == x2)
        {
            int step = (y1 < y2) ? 1 : -1;
            for (int y = y1 + step; y != y2; y += step)
            {
                if (!IsEmpty(x1, y)) return false; 
            }
            return true;
        }
        else if (y1 == y2)
        {
            int step = (x1 < x2) ? 1 : -1;
            for (int x = x1 + step; x != x2; x += step)
            {
                if (!IsEmpty(x, y1)) return false;
            }
            return true;
        }
        return false;
    }

    private bool IsEmpty(int x, int y)
    {

        if (x < 0 || x >= columns || y < 0 || y >= rows) 
            return true;


        return gridArray[x, y] == null || gridArray[x, y].currentState == TileState.Hidden;
    }



    private void ApplyGravity()
    {
        if (currentGravity == GravityDirection.None) return;

        List<AnimalTile> movedTiles = new List<AnimalTile>(); 
        float startX = -(columns / 2f * tileSize) + (tileSize / 2f);
        float startY = (rows / 2f * tileSize) - (tileSize / 2f);

        if (currentGravity == GravityDirection.Down)
        {
            for (int x = 0; x < columns; x++)
            {
                int emptyY = rows - 1; 
                for (int y = rows - 1; y >= 0; y--)
                {
                    AnimalTile tile = gridArray[x, y];
                    if (tile != null)
                    {

                        if (tile.currentState == TileState.Ice || tile.currentState == TileState.Barrier) emptyY = y - 1;
                        else if (y < emptyY)
                        {
                            gridArray[x, emptyY] = tile;
                            gridArray[x, y] = null;
                            tile.gridY = emptyY;
                            tile.rect.DOKill();
                            tile.rect.DOAnchorPosY(startY - (emptyY * tileSize), fallTime).SetEase(Ease.OutBounce);
                            movedTiles.Add(tile);
                            emptyY--;
                        }
                        else emptyY--;
                    }
                }
            }
        }
        else if (currentGravity == GravityDirection.Up)
        {
            for (int x = 0; x < columns; x++)
            {
                int emptyY = 0; 
                for (int y = 0; y < rows; y++)
                {
                    AnimalTile tile = gridArray[x, y];
                    if (tile != null)
                    {
                        if (tile.currentState == TileState.Ice || tile.currentState == TileState.Barrier) emptyY = y + 1;
                        else if (y > emptyY)
                        {
                            gridArray[x, emptyY] = tile;
                            gridArray[x, y] = null;
                            tile.gridY = emptyY;
                            tile.rect.DOKill();
                            tile.rect.DOAnchorPosY(startY - (emptyY * tileSize), fallTime).SetEase(Ease.OutBounce);
                            movedTiles.Add(tile);
                            emptyY++;
                        }
                        else emptyY++;
                    }
                }
            }
        }
        else if (currentGravity == GravityDirection.Left)
        {
            for (int y = 0; y < rows; y++) 
            {
                int emptyX = 0; 
                for (int x = 0; x < columns; x++)
                {
                    AnimalTile tile = gridArray[x, y];
                    if (tile != null)
                    {
                        if (tile.currentState == TileState.Ice || tile.currentState == TileState.Barrier) emptyX = x + 1;
                        else if (x > emptyX)
                        {
                            gridArray[emptyX, y] = tile;
                            gridArray[x, y] = null;
                            tile.gridX = emptyX;
                            tile.rect.DOKill();
                            tile.rect.DOAnchorPosX(startX + (emptyX * tileSize), fallTime).SetEase(Ease.OutBounce);
                            movedTiles.Add(tile);
                            emptyX++;
                        }
                        else emptyX++;
                    }
                }
            }
        }
        else if (currentGravity == GravityDirection.Right)
        {
            for (int y = 0; y < rows; y++) 
            {
                int emptyX = columns - 1; 
                for (int x = columns - 1; x >= 0; x--)
                {
                    AnimalTile tile = gridArray[x, y];
                    if (tile != null)
                    {
                        if (tile.currentState == TileState.Ice || tile.currentState == TileState.Barrier) emptyX = x - 1;
                        else if (x < emptyX)
                        {
                            gridArray[emptyX, y] = tile;
                            gridArray[x, y] = null;
                            tile.gridX = emptyX;
                            tile.rect.DOKill();
                            tile.rect.DOAnchorPosX(startX + (emptyX * tileSize), fallTime).SetEase(Ease.OutBounce);
                            movedTiles.Add(tile);
                            emptyX--;
                        }
                        else emptyX--;
                    }
                }
            }
        }

        if (movedTiles.Count > 0)
        {
            if (autoChainCoroutine != null) StopCoroutine(autoChainCoroutine);
            autoChainCoroutine = StartCoroutine(CheckAutoChainCoroutine(movedTiles, fallTime + 0.1f));
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
        if (isGameOver || isBoosterCooldown) return;
        StartCoroutine(BoosterCooldownRoutine());
        
        ClearHint();
        Debug.Log("Kích hoạt Đảo Lưới!");


        List<AnimalTile> activeTiles = new List<AnimalTile>();
        List<Vector2Int> activePositions = new List<Vector2Int>();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (gridArray[x, y] != null)
                {
                    activeTiles.Add(gridArray[x, y]);
                    activePositions.Add(new Vector2Int(x, y));
                }
            }
        }


        for (int i = 0; i < activePositions.Count; i++)
        {
            Vector2Int temp = activePositions[i];
            int randomIndex = Random.Range(i, activePositions.Count);
            activePositions[i] = activePositions[randomIndex];
            activePositions[randomIndex] = temp;
        }


        System.Array.Clear(gridArray, 0, gridArray.Length);


        float startX = -(columns / 2f * tileSize) + (tileSize / 2f);
        float startY = (rows / 2f * tileSize) - (tileSize / 2f);


        for (int i = 0; i < activeTiles.Count; i++)
        {
            AnimalTile tile = activeTiles[i];
            Vector2Int newPos = activePositions[i];


            tile.gridX = newPos.x;
            tile.gridY = newPos.y;
            gridArray[newPos.x, newPos.y] = tile;


            float targetX = startX + (newPos.x * tileSize);
            float targetY = startY - (newPos.y * tileSize);
            tile.rect.DOKill();
            tile.rect.DOAnchorPos(new Vector2(targetX, targetY), 0.5f).SetEase(Ease.InOutQuad);
        }


        if (firstSelectedTile != null)
        {
            firstSelectedTile.SetHighlight(false);
            firstSelectedTile = null;
        }
    }
    public void ShowHint()
    {
        if (isGameOver || isBoosterCooldown) return;
        StartCoroutine(BoosterCooldownRoutine());
        
        ClearHint();

        foreach (var list in hintCache.Values) list.Clear();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                AnimalTile tile = gridArray[x, y];
                if (tile != null && tile.currentState != TileState.Hidden && tile.currentState != TileState.Barrier)
                {
                    if (!hintCache.ContainsKey(tile.animalID))
                        hintCache[tile.animalID] = new List<AnimalTile>();
                    
                    hintCache[tile.animalID].Add(tile);
                }
            }
        }


        foreach (var kvp in hintCache)
        {
            List<AnimalTile> tiles = kvp.Value;
            if (tiles.Count < 2) continue;

            for (int i = 0; i < tiles.Count - 1; i++)
            {
                for (int j = i + 1; j < tiles.Count; j++)
                {
                    if (FindPath(tiles[i], tiles[j]))
                    {

                        hintedTile1 = tiles[i];
                        hintedTile2 = tiles[j];

                        tiles[i].rect.DOKill();
                        tiles[j].rect.DOKill();
                        
                        tiles[i].rect.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 1f, 5);
                        tiles[j].rect.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 1f, 5);
                        
                        tiles[i].SetHighlight(true);
                        tiles[j].SetHighlight(true);
                        return; 
                    }
                }
            }
        }
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
        if (isGameOver) return;
        isGameOver = true;
        hasWonLevel = isWin;
        var ui = FindObjectOfType<UIManager>();
        if (ui != null) ui.StopGameplayMusic();
        var resultSound = isWin ? winSound : loseSound;
        if (!resultSound.IsNull) RuntimeManager.PlayOneShot(resultSound);
        if (isWin && levelDatabase != null && levelDatabase.levels != null)
            LevelProgress.Won(levelIndex, levelDatabase.levels.Count);
        if (btnNextLevel != null)
            btnNextLevel.gameObject.SetActive(isWin && HasNextLevel());
        bool showLevelOneBanner = isWin && levelIndex == 0 && levelOneWinBanner != null;
        if (levelOneWinBanner != null) levelOneWinBanner.SetActive(showLevelOneBanner);
        if (textTitle != null) textTitle.gameObject.SetActive(!showLevelOneBanner);
        

        if (btnShuffle != null) btnShuffle.interactable = false;
        if (btnHint != null) btnHint.interactable = false;


        if (gameOverPanel != null) gameOverPanel.SetActive(true);


        if (isWin)
        {
            textTitle.text = "LEVEL " + (levelIndex + 1) + "\nCOMPLETED!";
            textTitle.color = Color.white;
        }
        else
        {
            textTitle.text = "THỬ LẠI!";
            textTitle.color = Color.white;
        }

        if (textFinalScore != null)
        {
            textFinalScore.text = "ĐIỂM: " + score;
        }
    }
    private void SpawnFloatingText(Vector2 startPos, string text, Color color)
    {
        if (floatingTextPrefab == null) return;


        GameObject floatingObj = Instantiate(floatingTextPrefab, boardContainer);
        RectTransform rect = floatingObj.GetComponent<RectTransform>();
        rect.anchoredPosition = startPos;

        TextMeshProUGUI tmp = floatingObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;


        rect.DOAnchorPosY(startPos.y + 100f, 1f).SetEase(Ease.OutCirc);
        tmp.DOFade(0, 1f).OnComplete(() => Destroy(floatingObj));
    }


    public void PlayUIButtonSound()
    {
        RuntimeManager.PlayOneShot(sfxClick);
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
        isGameOver = true;
        hasWonLevel = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(false);
    }

    private bool HasNextLevel()
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
        hasWonLevel = false;
        if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(false);

        DOTween.KillAll();
        StopAllCoroutines(); autoChainCoroutine = null;
        isBoosterCooldown = false;
        firstSelectedTile = null; secondSelectedTile = null;
        hintCache.Clear(); 


        if (gridArray != null)
        {
            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < gridArray.GetLength(1); y++)
                {
                    if (gridArray[x, y] != null)
                    {
                        Destroy(gridArray[x, y].gameObject);
                        gridArray[x, y] = null;
                    }
                }
            }
            System.Array.Clear(gridArray, 0, gridArray.Length);
        }


        score = 0;
        combo = 0;
        comboTimer = 0;
        UpdateScoreUI();


        ClearHint();


        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (btnShuffle != null) btnShuffle.interactable = true;
        if (btnHint != null) btnHint.interactable = true;


        GenerateGrid();
        if (!isGameOver)
        {
            LevelProgress.Started(levelIndex);
            var ui = FindObjectOfType<UIManager>();
            if (ui != null)
            {
                ui.RefreshLevelLabel(levelIndex);
                if (ui.screen == GameScreen.Gameplay) GameSession.SelectLevel(levelIndex);
                ui.PlayGameplayMusic();
            }
        }
    }
    
}
