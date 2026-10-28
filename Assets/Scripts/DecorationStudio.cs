using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DecorationCard
{
    public Image preview;
    public TextMeshProUGUI title;
    public TextMeshProUGUI detail;
    public Button button;
    public TextMeshProUGUI action;
}

public class DecorationStudio : MonoBehaviour
{
    public ShopUI shop;
    public GameObject panel;
    public Image room;
    public TextMeshProUGUI roomName,coins,status,selection,pageLabel;
    public Button previous,next,save,remove,clear;
    public Button[] slotButtons=new Button[0];
    public Image slotHighlight;
    public DecorationCard[] shopCards,inventoryCards;
    private readonly List<DecorationItem> owned=new List<DecorationItem>();
    private readonly Dictionary<string,Image> placed=new Dictionary<string,Image>();
    private RoomLayout draft;
    private string selected;
    private DecorationSlot selectedSlot=DecorationSlot.Plants;
    private int inventoryPage,shopPage;
    private bool returnToShop,dirty;
    public bool IsOpen => panel && panel.activeSelf;
    public RoomLayout Draft => draft;
    public DecorationSlot ActiveSlot => selectedSlot;
    public int ShopPageCount => Mathf.Max(1,Mathf.CeilToInt(EconomyStore.Config.decorations.Length/6f));
    private void Awake()
    {
        for(int i=0;i<shopCards.Length;i++) {int slot=i;shopCards[i].button.onClick.AddListener(()=>Buy(shopPage*6+slot));}
        for(int i=0;i<inventoryCards.Length;i++) {int slot=i;inventoryCards[i].button.onClick.AddListener(()=>PlaceOwned(inventoryPage*6+slot));}
        for(int i=0;i<slotButtons.Length;i++) {int slot=i+1;slotButtons[i].onClick.AddListener(()=>SelectSlot(slot));}
        EconomyStore.InventoryChanged+=RefreshInventory;
    }
    public void ShowShopPage(int page) {shopPage=page;RefreshShop();}
    private void Buy(int index)
    {
        var items=EconomyStore.Config.decorations;if(index<0 || index>=items.Length) return;
        EconomyStore.TryBuyDecoration(items[index].id,out var message);shop.statusText.text=message;
    }
    private void RefreshShop()
    {
        var inventory=EconomyStore.Inventory;var items=EconomyStore.Config.decorations;
        for(int i=0;i<shopCards.Length;i++)
        {
            int index=shopPage*6+i;var card=shopCards[i];card.preview.transform.parent.gameObject.SetActive(index<items.Length);
            if(index>=items.Length) continue;
            var item=items[index];bool has=inventory.ownedDecorations.Contains(item.id);
            card.preview.sprite=item.sprite;card.title.text=item.displayName;card.detail.text=item.skin+" / "+DecorationSlots.Label(item.slot);
            card.action.text=has ? "OWNED" : "BUY "+item.price;
            card.button.interactable=!has && inventory.coins>=item.price;
        }
    }
    public void Open()
    {
        var game=GridManager.Instance.Session;returnToShop=game.State==GameState.Shop;
        if(!game.OpenDecorations()) return;
        draft=EconomyStore.GetRoomLayout(EconomyStore.Inventory.selectedBackground);dirty=false;selected=null;inventoryPage=0;selectedSlot=DecorationSlot.Plants;
        selected=draft.items.Find(p=>EconomyStore.FindDecoration(p.itemId).slot==selectedSlot)?.itemId;
        shop.panel.SetActive(false);shop.mainMenuPanel.gameObject.SetActive(false);
        panel.SetActive(true);panel.transform.SetAsLastSibling();
        var item=EconomyStore.FindBackground(draft.backgroundId);room.sprite=item.baseSprite ? item.baseSprite : item.sprite;roomName.text=item.displayName;
        status.text="Choose an area, then equip one item for that area.";RefreshInventory();Render();
    }
    public void Close()
    {
        if(!IsOpen) return;
        panel.SetActive(false);draft=null;dirty=false;selected=null;
        GridManager.Instance.Session.CloseDecorations(returnToShop);
        if(returnToShop) shop.panel.SetActive(true);else shop.mainMenuPanel.gameObject.SetActive(true);
        shop.Refresh();
    }
    public void Save()
    {
        if(!IsOpen || !EconomyStore.SaveRoomLayout(draft)) return;
        dirty=false;status.text="Saved! Your room is updated everywhere.";RefreshSelection();
    }
    private void RefreshInventory()
    {
        RefreshShop();if(!IsOpen) return;
        var inventory=EconomyStore.Inventory;coins.text=inventory.coins+" COIN";owned.Clear();
        foreach(var item in EconomyStore.Config.decorations)
            if(item.slot==selectedSlot && inventory.ownedDecorations.Contains(item.id)) owned.Add(item);
        int pages=Mathf.Max(1,Mathf.CeilToInt(owned.Count/6f));inventoryPage=Mathf.Clamp(inventoryPage,0,pages-1);
        for(int i=0;i<inventoryCards.Length;i++)
        {
            int index=inventoryPage*6+i;var card=inventoryCards[i];card.preview.transform.parent.gameObject.SetActive(index<owned.Count);
            if(index>=owned.Count) continue;
            var item=owned[index];bool isPlaced=draft.items.Exists(p=>p.itemId==item.id);
            card.preview.sprite=item.sprite;card.title.text=item.displayName;card.detail.text=item.skin;
            card.action.text=isPlaced ? "EQUIPPED" : "EQUIP";card.button.interactable=!isPlaced;
        }
        pageLabel.text=DecorationSlots.Label(selectedSlot)+"  /  "+owned.Count+" OWNED";
        if(owned.Count==0) status.text="No owned items for this area. Visit ACCESSORIES in Shop.";
        previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);
        previous.interactable=inventoryPage>0;next.interactable=inventoryPage<pages-1;RefreshSelection();
    }
    public void Previous() {inventoryPage--;RefreshInventory();}
    public void Next() {inventoryPage++;RefreshInventory();}
    private void PlaceOwned(int index) {if(index>=0 && index<owned.Count) Place(owned[index].id);}
    public void Place(string id)
    {
        var item=EconomyStore.FindDecoration(id);
        if(!IsOpen || !EconomyStore.Inventory.ownedDecorations.Contains(id) || item==null) return;
        if(item.slot!=selectedSlot) {status.text="This item belongs to "+DecorationSlots.Label(item.slot)+".";return;}
        if(draft.items.Exists(p=>p.itemId==id)) return;
        draft.items.RemoveAll(p=>EconomyStore.FindDecoration(p.itemId).slot==selectedSlot);
        draft.items.Add(EconomyStore.SlotPlacement(id,draft.backgroundId));dirty=true;selected=id;
        status.text=item.displayName+" equipped in "+DecorationSlots.Label(selectedSlot)+". Save to apply.";
        Render();RefreshInventory();
    }
    public void Select(string id)
    {
        var item=EconomyStore.FindDecoration(id);if(item!=null) SelectSlot((int)item.slot);
    }
    public void SelectSlot(int slot)
    {
        if(!IsOpen || slot<1 || slot>DecorationSlots.Count) return;
        selectedSlot=(DecorationSlot)slot;inventoryPage=0;
        selected=draft.items.Find(p=>EconomyStore.FindDecoration(p.itemId).slot==selectedSlot)?.itemId;
        status.text="Choose one item for "+DecorationSlots.Label(selectedSlot)+". It stays in the highlighted area.";
        RefreshInventory();
    }
    private DecorationPlacement Selected => draft?.items.Find(p=>p.itemId==selected);
    public void Remove() {if(Selected==null) return;draft.items.Remove(Selected);selected=null;Changed();RefreshInventory();}
    public void Clear() {if(draft==null) return;draft.items.Clear();selected=null;Changed();RefreshInventory();}
    private void Changed() {dirty=true;Render();RefreshSelection();}
    private void RefreshSelection()
    {
        bool has=Selected!=null;var item=has ? EconomyStore.FindDecoration(selected) : null;
        selection.text=DecorationSlots.Label(selectedSlot)+"  /  "+(has ? item.displayName : "EMPTY");
        remove.interactable=has;
        save.interactable=dirty;clear.interactable=draft!=null && draft.items.Count>0;
        for(int i=0;i<slotButtons.Length;i++) slotButtons[i].GetComponent<Image>().sprite=i+1==(int)selectedSlot ? shop.selectedTabSprite : shop.otherTabSprite;
        if(slotHighlight && draft!=null)
        {
            var anchor=DecorationSlots.Anchor(draft.backgroundId,selectedSlot);var rect=slotHighlight.rectTransform;
            rect.anchorMin=rect.anchorMax=anchor.position;rect.pivot=anchor.pivot;rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta=new Vector2(anchor.maxSize.x*room.rectTransform.rect.width/1920,anchor.maxSize.y*room.rectTransform.rect.height/1080);
            rect.SetAsLastSibling();
        }
        foreach(var pair in placed) pair.Value.color=pair.Key==selected ? new Color(1,.88f,.7f) : Color.white;
    }
    private void Render()
    {
        foreach(var image in placed.Values) image.gameObject.SetActive(false);
        foreach(var placement in draft.items)
        {
            var item=EconomyStore.FindDecoration(placement.itemId);if(item==null) continue;
            if(!placed.TryGetValue(item.id,out var image))
            {
                var obj=new GameObject(item.displayName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
                obj.transform.SetParent(room.transform,false);image=obj.GetComponent<Image>();image.preserveAspect=true;
                var button=obj.GetComponent<Button>();button.targetGraphic=image;string id=item.id;button.onClick.AddListener(()=>Select(id));placed.Add(item.id,image);
            }
            image.gameObject.SetActive(true);image.transform.SetAsLastSibling();image.sprite=item.sprite;
            RoomDecorationView.ApplyRect(image.rectTransform,room.rectTransform,item,draft.backgroundId);
        }
        RefreshSelection();
    }
    private void OnDestroy() {EconomyStore.InventoryChanged-=RefreshInventory;}
}
