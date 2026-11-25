using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class RewardFrameArt
{
    private static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    public static Sprite Load(string name)
    {
        if(sprites.TryGetValue(name,out var cached) && cached) return cached;
        var source=Resources.Load<Sprite>("RewardFrames/Reward_"+name);if(!source) return null;
        var r=new Rect(0,0,source.texture.width,source.texture.height);
        var border=new Vector4(r.width*.34f,r.height*.3f,r.width*.34f,r.height*.34f);
        var sprite=Sprite.Create(source.texture,r,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect,border);sprite.name="Reward_"+name;sprites[name]=sprite;return sprite;
    }
    public static void Apply(Image image,string name)
    {
        var sprite=Load(name);if(!sprite || !image) return;
        image.sprite=sprite;image.type=Image.Type.Sliced;image.preserveAspect=false;image.color=Color.white;image.raycastTarget=false;
        image.pixelsPerUnitMultiplier=Mathf.Max(sprite.rect.width/Mathf.Max(1,image.rectTransform.rect.width),sprite.rect.height/Mathf.Max(1,image.rectTransform.rect.height));
    }
    public static Image Create(Transform parent,string name,string style,float x,float y,float width,float height)
    {
        var old=parent.Find(name);var image=old ? old.GetComponent<Image>() : null;
        if(!image) {image=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(parent,false);}
        GameplayHudStyle.Place(image.rectTransform,x,y,width,height);Apply(image,style);return image;
    }
    public static void Label(TextMeshProUGUI label,string style)
    {
        var r=label.rectTransform;var parent=r.parent;string name=label.name+"RewardFramePNG";
        var image=Create(parent,name,style,0,0,1,1);var rect=image.rectTransform;
        rect.anchorMin=r.anchorMin;rect.anchorMax=r.anchorMax;rect.pivot=r.pivot;rect.anchoredPosition=r.anchoredPosition+new Vector2(-8,7);rect.sizeDelta=r.sizeDelta+new Vector2(16,14);
        image.transform.SetAsFirstSibling();Apply(image,style);
    }
}
