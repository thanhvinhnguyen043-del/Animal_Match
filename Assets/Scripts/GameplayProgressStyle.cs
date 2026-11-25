using UnityEngine;
using UnityEngine.UI;

public static class GameplayProgressStyle
{
    private static Sprite trackSprite, fillSprite;

    private static Sprite Crop(string path, Rect normalized, bool sliced)
    {
        var source = Resources.Load<Sprite>(path);
        if (!source) return null;
        var rect = source.rect;
        var crop = new Rect(rect.x + rect.width * normalized.x, rect.y + rect.height * normalized.y,
            rect.width * normalized.width, rect.height * normalized.height);
        var sprite = Sprite.Create(source.texture, crop, new Vector2(.5f,.5f), source.pixelsPerUnit,
            0, SpriteMeshType.FullRect, sliced ? new Vector4(crop.height * .65f,0,crop.height * .65f,0) : Vector4.zero);
        sprite.name = sliced ? "CafeProgressTrack" : "CafeProgressFill";
        return sprite;
    }

    public static void Apply(ProgressUI progress)
    {
        progress.goalStars.SetMilestones(true);
        progress.goalTitle.text="MEDAL PROGRESS";
        var goal = progress.goalTitle.transform.parent;
        var track = goal.Find("TrackPNG").GetComponent<Image>();
        GameplayHudStyle.Place(track.rectTransform, 0, 50, 900, 54);
        track.type = Image.Type.Sliced;
        track.preserveAspect = false;
        track.pixelsPerUnitMultiplier = track.sprite.rect.height / 54 *
            track.canvas.referencePixelsPerUnit / track.sprite.pixelsPerUnit;
        var starFill = track.transform.Find("FillPNG").GetComponent<Image>();
        starFill.rectTransform.anchorMin = Vector2.zero;
        starFill.rectTransform.anchorMax = Vector2.one;
        starFill.rectTransform.offsetMin = new Vector2(8,8);
        starFill.rectTransform.offsetMax = new Vector2(-8,-8);
        GameplayHudStyle.Place(progress.goalStars.rectTransform, 0, 30, 900, 80);
        for (int i=0;i<3;i++)
        {
            var star = progress.goalStars.transform.Find("StarPNG"+i);
            if (!star) continue;
            var rect = (RectTransform)star;
            rect.anchorMin = rect.anchorMax = new Vector2(0,1);
            rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = new Vector2(76,76);
            rect.anchoredPosition = new Vector2((i+1)*285,-40);
        }
        GameplayHudStyle.Place(progress.turns.rectTransform,0,109,900,25);

        var slider = progress.grid.timeSlider;
        GameplayHudStyle.Place((RectTransform)slider.transform,50,440,270,50);
        foreach (string name in new[]{"WoodRim","GoldenRim","Recess"})
        {
            var child=slider.transform.Find(name);
            if(child) child.gameObject.SetActive(false);
        }
        if (!trackSprite) trackSprite=Crop("StarBar/StarBarTrack",new Rect(.02f,.345f,.96f,.31f),true);
        if (!fillSprite) fillSprite=Crop("StarBar/StarBarFill",new Rect(.016f,.305f,.968f,.39f),true);
        var saved=slider.transform.Find("CafeTimerFrame");
        var frame=saved ? saved.GetComponent<Image>() : new GameObject("CafeTimerFrame",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
        frame.transform.SetParent(slider.transform,false);
        frame.transform.SetAsFirstSibling();
        frame.sprite=trackSprite;
        frame.type=Image.Type.Sliced;
        frame.raycastTarget=false;
        frame.pixelsPerUnitMultiplier=trackSprite.rect.height/50*100/trackSprite.pixelsPerUnit;
        GameplayHudStyle.Place(frame.rectTransform,0,0,270,50);
        var area=(RectTransform)slider.fillRect.parent;
        GameplayHudStyle.Place(area,8,8,254,34);
        var oldFill=slider.fillRect.GetComponent<Image>();
        var savedFill=area.Find("CafeTimerFill");
        var fill=savedFill ? savedFill.GetComponent<Image>() :
            new GameObject("CafeTimerFill",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(area,false);
        if(oldFill!=fill) oldFill.enabled=false;
        fill.raycastTarget=false;
        fill.rectTransform.anchorMin=Vector2.zero;
        fill.rectTransform.anchorMax=Vector2.one;
        fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
        slider.fillRect=fill.rectTransform;
        fill.sprite=fillSprite;
        fill.type=Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier=fillSprite.rect.height/34*100/fillSprite.pixelsPerUnit;
        fill.color=new Color(1,.92f,.79f);
        var gloss=fill.transform.Find("SoftHighlight");
        if(gloss) gloss.gameObject.SetActive(false);
        slider.SetValueWithoutNotify(slider.value);
    }
}
