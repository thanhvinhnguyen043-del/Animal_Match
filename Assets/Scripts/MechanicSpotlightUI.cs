using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(225)]
public sealed class MechanicSpotlightUI : MonoBehaviour
{
    private GridManager grid;
    private GameManager game;
    private WaterTutorialUI water;
    private LoadingUI loading;
    private RectTransform overlay, card, focus, actionRect, timerFocus;
    private Image[] shade, rim;
    private Image mascot, actionArt;
    private TextMeshProUGUI heading, instruction, actionLabel;
    private Button action;
    private Coroutine sequence;
    private bool pressed;
    private readonly Vector3[] corners = new Vector3[4];
    public bool IsRunning => sequence != null;
    public static bool ReplayForDemo => Application.isEditor;

    private void Start()
    {
        var progress = GetComponent<ProgressUI>();
        grid = progress.grid;
        game = grid.Session;
        water = GetComponent<WaterTutorialUI>();
        loading = GetComponent<LoadingUI>();
        Build(progress);
        game.LevelStarted += Queue;
        game.StateChanged += StateChanged;
        if (game.State == GameState.Playing || water && water.IsOpen) Queue();
    }

    private static RectTransform Node(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image Picture(Transform parent, string name, Color color)
    {
        var image = Node(parent, name).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, float size)
    {
        var text = Node(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GameTypography.Current.font;
        text.fontSize = text.fontSizeMax = size;
        text.fontSizeMin = size * .8f;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.27f, .12f, .07f);
        text.raycastTarget = false;
        return text;
    }

    private void Build(ProgressUI progress)
    {
        overlay = Node(progress.ui.gameplayPanel, "MechanicSpotlight");
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        var canvas = overlay.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 600;
        overlay.gameObject.AddComponent<GraphicRaycaster>();
        var blocker = overlay.gameObject.AddComponent<Image>();
        blocker.color = Color.clear;
        blocker.canvasRenderer.cullTransparentMesh = false;

        shade = new Image[4];
        rim = new Image[4];
        focus = Node(overlay, "GoldFocus");
        for (int i = 0; i < 4; i++)
        {
            shade[i] = Picture(overlay, "Dim" + i, new Color(.09f, .035f, .025f, .81f));
            rim[i] = Picture(focus, "GoldEdge" + i, new Color(1f, .82f, .37f));
            var glow = rim[i].gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(1f, .46f, .12f, .85f);
            glow.effectDistance = new Vector2(3, 3);
        }
        focus.SetAsLastSibling();

        var panel = Picture(overlay, "GuideCard", new Color(1f, .94f, .8f));
        card = panel.rectTransform;
        GameplayHudStyle.Place(card, 0, 0, 870, 182);
        CafeComponentArt.Apply(panel, "Popup_Paper_Panel");
        mascot = Picture(card, "GuideMascot", Color.white);
        mascot.sprite = Resources.Load<Sprite>("PlayerAvatars/Fox");
        mascot.preserveAspect = true;
        GameplayHudStyle.Place(mascot.rectTransform, 23, 18, 136, 146);
        heading = Label(card, "Heading", 35);
        GameplayHudStyle.Place(heading.rectTransform, 165, 18, 670, 48);
        instruction = Label(card, "Instruction", 28);
        GameplayHudStyle.Place(instruction.rectTransform, 167, 69, 666, 92);

        actionArt = Picture(overlay, "GuideAction", Color.clear);
        actionArt.raycastTarget = true;
        actionArt.canvasRenderer.cullTransparentMesh = false;
        actionRect = actionArt.rectTransform;
        action = actionArt.gameObject.AddComponent<Button>();
        action.transition = Selectable.Transition.None;
        action.targetGraphic = actionArt;
        action.onClick.AddListener(() => pressed = true);
        actionLabel = Label(actionRect, "Continue", 30);
        actionLabel.color = Color.white;
        actionLabel.rectTransform.anchorMin = Vector2.zero;
        actionLabel.rectTransform.anchorMax = Vector2.one;
        actionLabel.rectTransform.offsetMin = actionLabel.rectTransform.offsetMax = Vector2.zero;
        overlay.gameObject.SetActive(false);
    }

    private void Queue()
    {
        StopGuide();
        sequence = StartCoroutine(TeachLevel(water && water.IsOpen));
    }

    private IEnumerator TeachLevel(bool waterPopupAlreadyOpened)
    {
        yield return null;
        while ((loading && loading.IsBusy) || (water && water.IsOpen)) yield return null;
        if (game.State != GameState.Playing) { StopGuide(); yield break; }

        game.TutorialTimerPaused = true;
        int level = grid.levelIndex + 1;
        var boosterRewards = GetComponent<FirstBoosterRewardUI>();
        if (boosterRewards)
        {
            if (level >= 3) yield return boosterRewards.Receive(BoosterType.Hint);
            if (level >= 6) yield return boosterRewards.Receive(BoosterType.Shuffle);
        }
        if (level == 1)
        {
            yield return PairLesson("Match", "MAKE A MATCH", "Tap two matching animals. The path can turn at most twice.", TileState.Normal, true);
            yield return HudLesson();
        }
        if (level == 2)
            yield return PairLesson("Combo", "KEEP THE COMBO GOING", "Match pairs quickly to build a combo. Every fifth match adds time.", TileState.Normal, true);
        if (level == 3) yield return BoosterLesson(BoosterType.Hint);
        if (level == 4 && game.InitialWater > 0)
            yield return PairLesson("Water", "BREAK THE WATER", "Match a water-covered animal with a normal animal of the same kind.", TileState.Ice, !waterPopupAlreadyOpened);
        if (level == 6) yield return BoosterLesson(BoosterType.Shuffle);
        if (level == 8 && game.InitialStone > 0)
            yield return PairLesson("Stone", "BREAK THE STONE", "Clear an animal next to the stone to free the animal inside.", TileState.Barrier, true);
        if (level >= 11 && level <= 14 && grid.currentGravity != GravityDirection.None)
            yield return PairLesson("Gravity" + grid.currentGravity, "GRAVITY: " + grid.currentGravity.ToString().ToUpperInvariant(),
                "Make a match and watch the free animals slide " + grid.currentGravity.ToString().ToLowerInvariant() + ".", TileState.Normal, true);
        if (level == 15)
            yield return PairLesson("Auto", "AUTO MATCHES", "After a match, gravity can bring matching animals together for an automatic match.", TileState.Normal, true);
        if (level == 16)
            yield return PairLesson("Cascade", "CHAIN REACTIONS", "An automatic match can move more animals and set off another match.", TileState.Normal, true);
        FinishGuide();
    }

    private static string Key(string lesson) => "Tutorial.Spotlight.v1." + lesson;
    private static bool Seen(string lesson) => !ReplayForDemo && PlayerPrefs.GetInt(Key(lesson), 0) == 1;
    private static void SaveSeen(string lesson)
    {
        if (ReplayForDemo) return;
        PlayerPrefs.SetInt(Key(lesson), 1);
        PlayerPrefs.Save();
    }

    private IEnumerator PairLesson(string lesson, string title, string text, TileState kind, bool intro)
    {
        if (Seen(lesson)) yield break;
        while (grid.TutorialBoardBusy) yield return null;
        if (intro) yield return Present(title, text, null);

        bool practiced = false;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            bool targetAvailable = grid.FindTutorialPair(kind, out var first, out var second);
            if (!targetAvailable)
            {
                if (kind == TileState.Normal) break;
                bool opensWater = kind == TileState.Ice &&
                    grid.FindTutorialPair(TileState.Normal, out first, out second, opensWater: true);
                if (!opensWater && !grid.FindTutorialPair(TileState.Normal, out first, out second)) break;
            }

            string step = targetAvailable ? text : "Clear this pair first to open a path to the covered animal.";
            yield return Present(title, step + "\nTap the glowing animal.", first.rect);
            grid.SelectTile(first);
            yield return Present(title, "Now tap its matching partner.", second.rect);
            grid.SelectTile(second);
            yield return Observe();

            if (targetAvailable) { practiced = true; break; }
        }

        if (!practiced)
            yield return Present(title, text + "\nKeep this in mind as you clear the board.", null);
        SaveSeen(lesson);
    }

    private IEnumerator BoosterLesson(BoosterType booster)
    {
        string lesson = booster.ToString();
        if (Seen(lesson)) yield break;
        var button = booster == BoosterType.Hint ? grid.btnHint : grid.btnShuffle;
        if (!button || !button.gameObject.activeInHierarchy) yield break;
        string text = booster == BoosterType.Hint ? "Hint highlights a pair you can match." : "Shuffle rearranges the animals when you get stuck.";
        yield return Present(lesson.ToUpperInvariant(), text, null);
        while (grid.TutorialBoardBusy) yield return null;
        yield return Present("TRY " + lesson.ToUpperInvariant(), text + "\nTap the glowing button for a free practice use.", (RectTransform)button.transform);
        grid.PracticeBooster(booster);
        yield return Observe();
        SaveSeen(lesson);
    }

    private IEnumerator Observe()
    {
        card.gameObject.SetActive(false);
        focus.gameObject.SetActive(false);
        actionRect.gameObject.SetActive(false);
        foreach (var band in shade) band.enabled = false;
        yield return new WaitForSecondsRealtime(.45f);
        while (grid.TutorialBoardBusy) yield return null;
    }

    private IEnumerator HudLesson()
    {
        if (Seen("GameplayHud")) yield break;
        var progress = GetComponent<ProgressUI>();
        var root = progress.ui.gameplayPanel;
        var coffee = GetComponent<CoffeeMatchEffects>();
        while (coffee && coffee.HasPendingCoffee) yield return null;

        var machine = root.Find("CoffeeCollector") as RectTransform;
        if (machine)
            yield return Present("FILL YOUR COFFEE MACHINE", "Each match sends coffee here. Clear every animal to fill the machine and win!", machine, true);

        var controller = GetComponent<CacaoUIController>();
        timerFocus = Node(root, "TutorialTimerArea");
        FocusArea(timerFocus, grid.timeSlider.transform, controller.timerText.transform, root.Find("Icon__Timer"));
        yield return Present("WATCH THE TIME", "Clear the board before time runs out. Every fifth combo match adds bonus time.", timerFocus, true);
        Destroy(timerFocus.gameObject);
        timerFocus = null;

        yield return Present("EARN THREE STARS", "1 star: clear the level. 2: finish with at least 30% time left. 3: also complete the special goal within its turn limit.", progress.goalTitle.transform.parent as RectTransform, true);
        var help = root.Find("StarGoalHelp") as RectTransform;
        if (help)
            yield return Present("CHECK YOUR GOALS", "Tap ? whenever you need to check this level's star goals. Each successful match counts as one turn.", help, true);
        SaveSeen("GameplayHud");
    }

    private void FocusArea(RectTransform area, params Transform[] targets)
    {
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        var parent = (RectTransform)area.parent;
        foreach (var target in targets)
        {
            if (!(target is RectTransform rect)) continue;
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 local = parent.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }
        GameplayHudStyle.Place(area, min.x - parent.rect.xMin, parent.rect.yMax - max.y, max.x - min.x, max.y - min.y);
    }

    private IEnumerator Present(string title, string text, RectTransform target, bool explain = false)
    {
        pressed = false;
        overlay.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        card.gameObject.SetActive(true);
        actionRect.gameObject.SetActive(true);
        foreach (var band in shade) band.enabled = true;
        heading.text = title;
        instruction.text = text;
        actionArt.sprite = target && !explain ? null : Resources.Load<Sprite>("ActivityIcons/Primary");
        actionArt.color = target && !explain ? Color.clear : Color.white;
        actionLabel.text = explain ? "CONTINUE" : target ? "" : "GOT IT";
        focus.gameObject.SetActive(target);
        while (!pressed)
        {
            Arrange(target);
            if (explain)
            {
                float focusY = -focus.anchoredPosition.y;
                float cardY = focusY >= 285 ? focusY - 280 : focusY + focus.rect.height + 16;
                cardY = Mathf.Clamp(cardY, 12, overlay.rect.height - 270);
                GameplayHudStyle.Place(card, (overlay.rect.width - card.rect.width) / 2, cardY, card.rect.width, 182);
                GameplayHudStyle.Place(actionRect, (overlay.rect.width - 230) / 2, cardY + 194, 230, 62);
            }
            yield return null;
        }
    }

    private void Arrange(RectTransform target)
    {
        Rect bounds = overlay.rect;
        float width = bounds.width, height = bounds.height;
        float cardWidth = Mathf.Min(870, width - 30);
        if (!target)
        {
            Place(shade[0], 0, 0, width, height);
            for (int i = 1; i < shade.Length; i++) Place(shade[i], 0, 0, 0, 0);
            GameplayHudStyle.Place(card, (width - cardWidth) / 2, height * .32f, cardWidth, 182);
            GameplayHudStyle.Place(actionRect, (width - 230) / 2, height * .32f + 200, 230, 62);
            return;
        }

        target.GetWorldCorners(corners);
        Vector3 min = overlay.InverseTransformPoint(corners[0]);
        Vector3 max = overlay.InverseTransformPoint(corners[2]);
        float x = Mathf.Clamp(min.x - bounds.xMin - 5, 0, width);
        float y = Mathf.Clamp(bounds.yMax - max.y - 5, 0, height);
        float w = Mathf.Clamp(max.x - min.x + 10, 0, width - x);
        float h = Mathf.Clamp(max.y - min.y + 10, 0, height - y);
        Place(shade[0], 0, 0, width, y);
        Place(shade[1], 0, y + h, width, height - y - h);
        Place(shade[2], 0, y, x, h);
        Place(shade[3], x + w, y, width - x - w, h);

        GameplayHudStyle.Place(focus, x, y, w, h);
        float edge = 3.5f + Mathf.Sin(Time.unscaledTime * 5f);
        Place(rim[0], 0, 0, w, edge);
        Place(rim[1], 0, h - edge, w, edge);
        Place(rim[2], 0, 0, edge, h);
        Place(rim[3], w - edge, 0, edge, h);
        GameplayHudStyle.Place(actionRect, x, y, w, h);
        float cardY = y >= 205 ? Mathf.Max(12, y - 197) : Mathf.Min(height - 194, y + h + 16);
        GameplayHudStyle.Place(card, (width - cardWidth) / 2, cardY, cardWidth, 182);
    }

    private static void Place(Image image, float x, float y, float w, float h)
        => GameplayHudStyle.Place(image.rectTransform, x, y, w, h);

    private void StateChanged(GameState state)
    {
        if (state != GameState.Playing && !(state == GameState.Paused && water && water.IsOpen))
            StopGuide();
    }

    private void FinishGuide()
    {
        sequence = null;
        if (timerFocus) Destroy(timerFocus.gameObject);
        timerFocus = null;
        if (overlay) overlay.gameObject.SetActive(false);
        if (game) game.TutorialTimerPaused = false;
    }

    private void StopGuide()
    {
        if (sequence != null) StopCoroutine(sequence);
        FinishGuide();
    }

    private void OnDestroy()
    {
        StopGuide();
        if (!game) return;
        game.LevelStarted -= Queue;
        game.StateChanged -= StateChanged;
    }
}
