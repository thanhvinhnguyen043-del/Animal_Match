using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomDecorationView : MonoBehaviour
{
    private readonly List<Image> images=new List<Image>();
    private void OnEnable() {EconomyStore.InventoryChanged+=Refresh;Refresh();}
    private void OnDisable() {EconomyStore.InventoryChanged-=Refresh;}
    private void Refresh()
    {
        var layout=EconomyStore.GetRoomLayout(EconomyStore.Inventory.selectedBackground);
        for(int i=0;i<layout.items.Count;i++)
        {
            if(i==images.Count)
            {
                var obj=new GameObject("Decoration",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                obj.transform.SetParent(transform,false);var image=obj.GetComponent<Image>();
                image.preserveAspect=true;image.raycastTarget=false;images.Add(image);
            }
            var placement=layout.items[i];var item=EconomyStore.FindDecoration(placement.itemId);
            var view=images[i];view.gameObject.SetActive(item!=null && item.sprite);
            if(item==null) continue;
            view.sprite=item.sprite;ApplyRect(view.rectTransform,(RectTransform)transform,item,layout.backgroundId);
        }
        for(int i=layout.items.Count;i<images.Count;i++) images[i].gameObject.SetActive(false);
    }
    public static void ApplyRect(RectTransform rect,RectTransform room,DecorationItem item,string backgroundId)
    {
        float width=room.rect.width,height=room.rect.height;
        var anchor=DecorationSlots.Anchor(backgroundId,item.slot);
        float fit=Mathf.Min(1,anchor.maxSize.x/Mathf.Max(1,item.size.x),anchor.maxSize.y/Mathf.Max(1,item.size.y));
        rect.anchorMin=rect.anchorMax=anchor.position;rect.pivot=anchor.pivot;
        rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(item.size.x*width/1920,item.size.y*height/1080)*fit;rect.localScale=Vector3.one;
    }
}
