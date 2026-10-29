using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DecorationPreview : MonoBehaviour
{
    public ShopUI shop;
    public GameObject panel;
    public Image room,highlight;
    public TextMeshProUGUI title,description,action,status;
    public Button actionButton;
    private readonly List<Image> placed=new List<Image>(DecorationSlots.Count);
    private string itemId,backgroundId;
    private bool backgroundMode;
    private bool studioBackground;
    public static void BindThumbnail(Image image,UnityEngine.Events.UnityAction action)
    {
        if(!image) return;image.raycastTarget=true;
        var button=image.GetComponent<Button>();if(!button) button=image.gameObject.AddComponent<Button>();
        button.targetGraphic=image;button.onClick.AddListener(action);
    }
    public bool IsOpen=>panel && panel.activeSelf;
    public string ItemId=>itemId;
    private void Awake() {EconomyStore.InventoryChanged+=Refresh;}
    public void Open(string id)
    {
        var item=EconomyStore.FindDecoration(id);
        if(!shop.IsOpen || item==null || !item.sprite) return;
        backgroundMode=false;itemId=id;backgroundId=EconomyStore.Inventory.selectedBackground;
        panel.SetActive(true);panel.transform.SetAsLastSibling();status.text="Preview only. Your saved room has not changed.";Refresh();
    }
    public void Close() {if(panel) panel.SetActive(false);itemId=null;}
    public void OpenBackground(string id)
    {
        var background=EconomyStore.FindBackground(id);var rewards=shop.GetComponent<RoomRewardsUI>();
        if(background==null || !(shop.IsOpen || shop.decorations.IsOpen || rewards && rewards.IsOpen)) return;
        studioBackground=shop.decorations.IsOpen;
        backgroundMode=true;itemId=null;backgroundId=id;panel.SetActive(true);panel.transform.SetAsLastSibling();
        status.text="Preview only. Your equipped background has not changed.";Refresh();
    }
    public void Act()
    {
        if(!IsOpen) return;
        if(backgroundMode)
        {
            if(studioBackground) {shop.decorations.SelectBackground(backgroundId);Close();return;}
            if(EconomyStore.Inventory.ownedBackgrounds.Contains(backgroundId)) EconomyStore.TryEquipBackground(backgroundId);
            else {EconomyStore.TryBuyBackground(backgroundId,out var message);status.text=message;}
            Refresh();return;
        }
        var item=EconomyStore.FindDecoration(itemId);if(item==null || !item.sprite) return;
        var inventory=EconomyStore.Inventory;
        if(!inventory.ownedDecorations.Contains(itemId))
        {
            EconomyStore.TryBuyDecoration(itemId,out var message);status.text=message;Refresh();return;
        }
        var layout=EconomyStore.GetRoomLayout(backgroundId);
        layout.items.RemoveAll(p=>EconomyStore.FindDecoration(p.itemId).slot==item.slot);
        layout.items.Add(EconomyStore.SlotPlacement(itemId,backgroundId));
        if(EconomyStore.SaveRoomLayout(layout)) status.text="Equipped! Your room is updated everywhere.";
        Refresh();
    }
    public void Refresh()
    {
        if(!IsOpen) return;
        var item=EconomyStore.FindDecoration(itemId);var background=EconomyStore.FindBackground(backgroundId);
        if((!backgroundMode && item==null) || background==null) {Close();return;}
        var inventory=EconomyStore.Inventory;var saved=EconomyStore.GetRoomLayout(inventory,backgroundId);
        if(backgroundMode)
        {
            bool has=inventory.ownedBackgrounds.Contains(backgroundId),usingRoom=inventory.selectedBackground==backgroundId;
            bool unlocked=EconomyStore.IsBackgroundUnlocked(background);
            title.text=background.displayName;
            description.text=has ? "Your saved decorations are shown. Choose EQUIP to use this room." : background.rewardOnly ? RoomProgress.SourceForBackground(backgroundId) : "Preview before buying. "+background.price.ToString("N0")+" COINS";
            action.text=usingRoom ? "EQUIPPED" : has ? "EQUIP" : background.rewardOnly ? "PLAY TO UNLOCK" : !unlocked ? "LEVEL "+background.requiredCompletedLevel : "BUY "+background.price.ToString("N0");
            actionButton.interactable=!usingRoom && (has || !background.rewardOnly && unlocked && inventory.coins>=background.price);
            if(studioBackground)
            {
                description.text="Choose this room in Decorate. Press SAVE there to apply your changes.";
                action.text=shop.decorations.Draft.backgroundId==backgroundId ? "SELECTED" : "SELECT";
                actionButton.interactable=has && shop.decorations.Draft.backgroundId!=backgroundId;
                if(shop.decorations.Draft.backgroundId==backgroundId) saved=shop.decorations.Draft;
            }
        }
        else
        {
        bool owned=inventory.ownedDecorations.Contains(itemId),equipped=saved.items.Exists(p=>p.itemId==itemId);
        saved.items.RemoveAll(p=>EconomyStore.FindDecoration(p.itemId).slot==item.slot);
        saved.items.Add(EconomyStore.SlotPlacement(itemId,backgroundId));
        title.text=item.displayName;
        description.text=background.displayName+"  /  "+DecorationSlots.Label(item.slot)+"\n"+(owned ? "Owned - choose EQUIP to apply" : item.rewardOnly ? RoomProgress.SourceForDecoration(itemId) : "Try it here before buying. "+item.price.ToString("N0")+" COINS");
        action.text=equipped ? "EQUIPPED" : owned ? "EQUIP" : item.rewardOnly ? "PLAY TO UNLOCK" : "BUY "+item.price.ToString("N0");
        actionButton.interactable=!equipped && (owned || !item.rewardOnly && inventory.coins>=Mathf.Max(0,item.price));
        }
        room.sprite=background.baseSprite ? background.baseSprite : background.sprite;
        for(int i=0;i<saved.items.Count;i++)
        {
            if(i==placed.Count)
            {
                var obj=new GameObject("PreviewDecoration",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));obj.transform.SetParent(room.transform,false);
                var image=obj.GetComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;placed.Add(image);
            }
            var definition=EconomyStore.FindDecoration(saved.items[i].itemId);var view=placed[i];view.gameObject.SetActive(definition!=null && definition.sprite);
            if(definition==null) continue;view.sprite=definition.sprite;view.color=Color.white;
            RoomDecorationView.ApplyRect(view.rectTransform,room.rectTransform,definition,backgroundId);
        }
        for(int i=saved.items.Count;i<placed.Count;i++) placed[i].gameObject.SetActive(false);
        if(highlight)
        {
            highlight.gameObject.SetActive(!backgroundMode);if(backgroundMode) return;
            var anchor=DecorationSlots.Anchor(backgroundId,item.slot);var rect=highlight.rectTransform;
            rect.anchorMin=rect.anchorMax=anchor.position;rect.pivot=anchor.pivot;rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta=new Vector2(anchor.maxSize.x*room.rectTransform.rect.width/1920,anchor.maxSize.y*room.rectTransform.rect.height/1080);
            rect.SetAsLastSibling();
        }
    }
    private void OnDestroy() {EconomyStore.InventoryChanged-=Refresh;}
}
