using UnityEngine;
using UnityEngine.UI;

public static class CafePanelStyle
{
    private static readonly Color Wood=new Color(.48f,.29f,.16f),Trim=new Color(.88f,.69f,.43f),Paper=new Color(1,.95f,.82f);
    private static RoundedImage Layer(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var child=parent.Find(name);
        if(!child) {child=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(RoundedImage)).transform;child.SetParent(parent,false);}
        var rect=(RectTransform)child;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
        var image=child.GetComponent<RoundedImage>();image.color=color;image.cornerRadius=24;image.raycastTarget=false;return image;
    }
    public static void Frame(Transform root)
    {
        var image=root.GetComponent<RoundedImage>();if(image) image.color=Wood;
        Layer(root,"WarmTrim",5,5,((RectTransform)root).rect.width-10,((RectTransform)root).rect.height-10,Trim).transform.SetAsFirstSibling();
        Layer(root,"CreamInset",13,13,((RectTransform)root).rect.width-26,((RectTransform)root).rect.height-26,Paper).transform.SetSiblingIndex(1);
    }
    public static void Rewards(Transform root)
    {
        var surface=root.Find("RoomRewardSurface");if(!surface) surface=root.Find("Frame");if(surface) Frame(surface);
    }
    public static void Studio(Transform root)
    {
        var background=root.GetComponent<Image>();if(background) background.color=new Color(.91f,.82f,.65f);
        var room=Layer(root,"RoomWorkspace",65,245,1170,655,Wood);room.transform.SetAsFirstSibling();Frame(room.transform);
        var inventory=Layer(root,"InventoryWorkspace",1295,125,590,805,Wood);inventory.transform.SetSiblingIndex(1);Frame(inventory.transform);
        var footer=Layer(root,"ActionShelf",30,935,1860,100,new Color(.82f,.65f,.43f));footer.transform.SetSiblingIndex(2);
    }
    public static void Shop(Transform root)
    {
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            if(!image.sprite || image.sprite.name!="Panel__Large" || image.rectTransform.rect.width<1200) continue;
            image.enabled=false;
        }
        var surface=Layer(root,"ShopWorkspace",130,190,1660,825,Wood);surface.transform.SetSiblingIndex(1);Frame(surface.transform);
        var old=root.GetComponentsInChildren<RoundedImage>(true);
        foreach(var image in old) if(image.name!="ShopWorkspace" && image.transform.parent==root && image.rectTransform.rect.width>1500) image.gameObject.SetActive(false);
    }
}
