using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CafeComponentArt
{
    private static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
    public static Sprite Sprite(string name)
    {
        if(cache.TryGetValue(name,out var sprite) && sprite) return sprite;
        var source=Resources.Load<Sprite>("CafeComponents/"+name);if(!source) return null;
        Rect trim;Vector4 border;
        switch(name)
        {
            case "Quest_Row":trim=new Rect(10f/2172,122f/724,2153f/2172,475f/724);border=new Vector4(480f/2172,170f/724,180f/2172,100f/724);break;
            case "Wardrobe_Card":trim=new Rect(28f/1402,31f/1122,1340f/1402,1046f/1122);border=Vector4.zero;break;
            case "Combo_Speech_Bubble":trim=new Rect(20f/2172,23f/724,2124f/2172,678f/724);border=Vector4.zero;break;
            default:trim=new Rect(22f/1672,39f/941,1628f/1672,863f/941);border=new Vector4(240f/1672,240f/941,240f/1672,240f/941);break;
        }
        var r=source.rect;var crop=new Rect(r.x+r.width*trim.x,r.y+r.height*trim.y,r.width*trim.width,r.height*trim.height);
        border=new Vector4(border.x*r.width,border.y*r.height,border.z*r.width,border.w*r.height);
        sprite=UnityEngine.Sprite.Create(source.texture,crop,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect,border);sprite.name=name;cache[name]=sprite;return sprite;
    }
    public static void Apply(Image image,string name)
    {
        var sprite=Sprite(name);if(!sprite) return;
        image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;
        bool sliced=name=="Popup_Paper_Panel" || name=="Quest_Row";
        image.type=sliced ? Image.Type.Sliced : Image.Type.Simple;image.preserveAspect=!sliced;
        image.pixelsPerUnitMultiplier=name=="Quest_Row" ? sprite.rect.height/image.rectTransform.rect.height : 3;
    }
    public static void Panel(Transform root)
    {
        if(!Sprite("Popup_Paper_Panel")) return;
        foreach(string name in new[]{"WarmTrim","CreamInset"}) {var old=root.Find(name);if(old) old.gameObject.SetActive(false);}
        var oldImage=root.GetComponent<Image>();if(oldImage) oldImage.enabled=false;
        var child=root.Find("CafePanelPNG");
        if(!child) child=new GameObject("CafePanelPNG",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).transform;
        child.SetParent(root,false);child.SetAsFirstSibling();var r=(RectTransform)child;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        Apply(child.GetComponent<Image>(),"Popup_Paper_Panel");
    }
}
