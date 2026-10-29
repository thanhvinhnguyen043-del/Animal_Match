using UnityEngine;
using UnityEngine.UI;

public static class LoadingVolumeStyle
{
    public static void Apply(AudioSettingsUI audio)
    {
        var loading=audio.GetComponent<LoadingUI>();if(!loading) return;
        var track=loading.startup.panel.transform.Find("Content/ProgressTrack").GetComponent<Image>();
        foreach(var slider in new[]{audio.masterSlider,audio.musicSlider,audio.sfxSlider})
        {
            if(!slider || slider.transform.Find("LoadingVolumeTrack")) continue;
            foreach(var image in slider.GetComponentsInChildren<Image>(true))
                if(!image.transform.IsChildOf(slider.handleRect)) image.enabled=false;
            var backing=Image(slider.transform,"LoadingVolumeTrack",track.sprite,0,9,390,34);backing.raycastTarget=true;backing.transform.SetAsFirstSibling();
            var area=new GameObject("LoadingVolumeArea",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(slider.transform,false);
            GameplayHudStyle.Place(area,9,16,372,18);
            var fill=Image(area,"Fill",loading.startup.fill.sprite,0,0,372,18);fill.type=UnityEngine.UI.Image.Type.Sliced;
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            slider.fillRect=fill.rectTransform;slider.handleRect.parent.SetAsLastSibling();slider.SetValueWithoutNotify(slider.value);
        }
    }
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var image=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(parent,false);
        GameplayHudStyle.Place(image.rectTransform,x,y,w,h);image.sprite=sprite;image.raycastTarget=false;return image;
    }
}
