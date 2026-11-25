using UnityEngine;
using UnityEngine.UI;

public static class GameplayHudStyle
{
    public static void Place(RectTransform rect,float x,float y,float w,float h)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
    private static RoundedImage Box(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var old=parent.Find(name);var image=old ? old.GetComponent<RoundedImage>() : null;
        if(!image) {image=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(RoundedImage)).GetComponent<RoundedImage>();image.transform.SetParent(parent,false);}
        Place(image.rectTransform,x,y,w,h);image.color=color;image.cornerRadius=h/2;image.raycastTarget=false;return image;
    }
    public static Image Apply(ProgressUI progress)
    {
        var root=progress.ui.gameplayPanel;var slider=progress.grid.timeSlider;
        Place(slider.GetComponent<RectTransform>(),50,440,270,42);
        var oldTrack=root.Find("Timer__Track");if(oldTrack) oldTrack.gameObject.SetActive(false);
        Box(slider.transform,"WoodRim",0,0,270,42,new Color(.91f,.65f,.28f)).transform.SetAsFirstSibling();
        Box(slider.transform,"GoldenRim",3,3,264,36,new Color(.97f,.74f,.35f)).transform.SetSiblingIndex(1);
        Box(slider.transform,"Recess",7,7,256,28,new Color(1,.91f,.7f)).transform.SetSiblingIndex(2);
        var area=(RectTransform)slider.fillRect.parent;Place(area,9,9,252,24);area.SetAsLastSibling();
        var gloss=Box(slider.fillRect,"SoftHighlight",0,0,1,1,new Color(1,1,.8f,.24f));
        gloss.rectTransform.anchorMin=new Vector2(.03f,.6f);gloss.rectTransform.anchorMax=new Vector2(.97f,.87f);gloss.rectTransform.offsetMin=gloss.rectTransform.offsetMax=Vector2.zero;
        foreach(var surface in root.GetComponentsInChildren<RoundedImage>(true))
            if(surface.transform.parent==root && surface.rectTransform.rect.width>200 && surface.rectTransform.rect.width<400 && surface.rectTransform.rect.height>60 && surface.rectTransform.rect.height<130)
            {surface.gameObject.SetActive(false);}
        var goalRoot=progress.goalTitle.transform.parent;
        var backing=goalRoot.Find("Backing");if(backing) backing.gameObject.SetActive(false);
        progress.goalTitle.text="MEDAL PROGRESS";Place(progress.goalTitle.rectTransform,15,12,270,40);
        var track=Box(goalRoot,"StarTrack",30,83,240,18,new Color(1,.9f,.67f));track.transform.SetSiblingIndex(1);
        var fill=Box(track.transform,"Earned",3,3,234,12,new Color(1,.73f,.15f));
        fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=new Vector2(3,3);fill.rectTransform.offsetMax=new Vector2(-3,-3);
        Place(progress.goalStars.rectTransform,15,52,270,78);progress.goalStars.transform.SetAsLastSibling();
        Place(progress.timeGoal.rectTransform,20,135,260,65);Place(progress.challengeGoal.rectTransform,20,207,260,105);
        Place(progress.turns.rectTransform,15,320,270,36);
        progress.timeGoal.fontSizeMax=24;progress.challengeGoal.fontSizeMax=23;
        var trackPng=Resources.Load<Sprite>("StarBar/StarBarTrack");
        var fillPng=Resources.Load<Sprite>("StarBar/StarBarFill");
        if(trackPng && fillPng)
        {
            track.enabled=false;fill.enabled=false;
            var savedTrack=goalRoot.Find("TrackPNG");
            var trackImage=savedTrack ? savedTrack.GetComponent<Image>() : new GameObject("TrackPNG",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();trackImage.transform.SetParent(goalRoot,false);
            trackImage.sprite=trackPng;trackImage.raycastTarget=false;Place(trackImage.rectTransform,20,67,260,72);trackImage.transform.SetSiblingIndex(track.transform.GetSiblingIndex());
            var savedFill=trackImage.transform.Find("FillPNG");
            var fillImage=savedFill ? savedFill.GetComponent<Image>() : new GameObject("FillPNG",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();fillImage.transform.SetParent(trackImage.transform,false);
            fillImage.sprite=fillPng;fillImage.type=Image.Type.Filled;fillImage.fillMethod=Image.FillMethod.Horizontal;fillImage.fillOrigin=0;fillImage.raycastTarget=false;
            fillImage.rectTransform.anchorMin=new Vector2(0,.25f);fillImage.rectTransform.anchorMax=new Vector2(1,.75f);fillImage.rectTransform.offsetMin=new Vector2(7,0);fillImage.rectTransform.offsetMax=new Vector2(-7,0);
            progress.goalStars.UsePngStars();return fillImage;
        }
        return fill;
    }
}
