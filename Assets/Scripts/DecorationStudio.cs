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
    public DecorationPreview preview;
    public DecorationCard[] shopCards,inventoryCards;
    public GameObject backgroundPanel;
    public DecorationCard[] backgroundCards=new DecorationCard[0];
    public Button backgroundPrevious,backgroundNext;
    public TextMeshProUGUI backgroundPageLabel;
    private readonly List<DecorationItem> owned=new List<DecorationItem>();
    private readonly List<ShopBackground> ownedRooms=new List<ShopBackground>();
    private readonly Dictionary<string,RoomLayout> drafts=new Dictionary<string,RoomLayout>();
    private readonly List<DecorationItem> shopItems=new List<DecorationItem>();
    private readonly Dictionary<string,Image> placed=new Dictionary<string,Image>();
    private RoomLayout draft;
    private string selected,savedBackground,browsed;
    private DecorationSlot selectedSlot=DecorationSlot.Plants;
    private int inventoryPage,shopPage,backgroundPage;
    private bool returnToShop,dirty;
    public bool IsOpen => panel && panel.activeSelf;
    public RoomLayout Draft => draft;
    public DecorationSlot ActiveSlot => selectedSlot;
    public int ShopPageCount {get {BuildShopList();return Mathf.Max(1,Mathf.CeilToInt(shopItems.Count/6f));}}
    private void Awake()
    {
        CafePanelStyle.Studio(panel.transform);
        panel.transform.Find("StudioEquip").GetComponent<Button>().onClick.AddListener(EquipBrowsed);
        var sample=room.transform.Find("StudioSceneSample");if(sample)sample.gameObject.SetActive(false);
        for(int i=0;i<shopCards.Length;i++) {int slot=i;shopCards[i].button.onClick.AddListener(()=>Preview(shopPage*6+slot));}
        for(int i=0;i<inventoryCards.Length;i++) {int slot=i;inventoryCards[i].button.onClick.AddListener(()=>BrowseOwned(inventoryPage*6+slot));}
        for(int i=0;i<slotButtons.Length;i++) {int slot=i+1;slotButtons[i].onClick.AddListener(()=>SelectSlot(slot));}
        for(int i=0;i<backgroundCards.Length;i++) {int slot=i;backgroundCards[i].button.onClick.AddListener(()=>ChooseBackground(backgroundPage*backgroundCards.Length+slot));DecorationPreview.BindThumbnail(backgroundCards[i].preview,()=>PreviewBackground(slot));}
        EconomyStore.InventoryChanged+=RefreshInventory;
    }
    public void ShowShopPage(int page) {shopPage=page;RefreshShop();}
    public void FocusShopItem(string id) {BuildShopList();int index=shopItems.FindIndex(i=>i.id==id);if(index>=0) shop.ShowAccessoryPage(index/6);}
    private void BuildShopList()
    {
        shopItems.Clear();foreach(var item in EconomyStore.Config.decorations) if(item!=null && item.sprite) shopItems.Add(item);
    }
    private void Preview(int index)
    {
        if(index>=0 && index<shopItems.Count && preview) preview.Open(shopItems[index].id);
    }
    private void RefreshShop() => RefreshShop(EconomyStore.Inventory, shopPage);

    public void PrepareShopSceneView() => RefreshShop(new PlayerInventory(), 0);

    private void RefreshShop(PlayerInventory inventory, int page)
    {
        BuildShopList();var items=shopItems;
        for(int i=0;i<shopCards.Length;i++)
        {
            int index=page*6+i;var card=shopCards[i];card.preview.transform.parent.gameObject.SetActive(index<items.Count);
            if(index>=items.Count) continue;
            var item=items[index];bool has=inventory.ownedDecorations.Contains(item.id);
            card.preview.sprite=item.sprite;card.title.text=item.displayName;
            card.detail.text=DecorationSlots.Label(item.slot)+" / "+(has ? "OWNED" : item.rewardOnly ? "PLAY REWARD" : item.price.ToString("N0")+" COINS");
            card.action.text="PREVIEW";
            card.button.interactable=true;
        }
    }
    public void Open()
    {
        if(preview) preview.Close();
        var game=GridManager.Instance.Session;returnToShop=game.State==GameState.Shop;
        if(!game.OpenDecorations()) return;
        drafts.Clear();savedBackground=EconomyStore.Inventory.selectedBackground;
        draft=EconomyStore.GetRoomLayout(savedBackground);drafts.Add(savedBackground,draft);dirty=false;selected=null;browsed=null;inventoryPage=0;selectedSlot=DecorationSlot.Plants;
        CloseBackgrounds();
        selected=draft.items.Find(p=>EconomyStore.FindDecoration(p.itemId).slot==selectedSlot)?.itemId;
        shop.panel.SetActive(false);shop.mainMenuPanel.gameObject.SetActive(false);
        panel.SetActive(true);panel.transform.SetAsLastSibling();
        slotHighlight.gameObject.SetActive(true);ShowRoom();
        status.text="Choose an area, then equip one item for that area.";RefreshInventory();Render();
    }
    public void Close()
    {
        if(!IsOpen) return;
        CloseBackgrounds();panel.SetActive(false);drafts.Clear();draft=null;dirty=false;selected=null;
        GridManager.Instance.Session.CloseDecorations(returnToShop);
        if(returnToShop) shop.panel.SetActive(true);else shop.mainMenuPanel.gameObject.SetActive(true);
        shop.Refresh();
    }
    public void Save()
    {
        if(!IsOpen || draft==null || !EconomyStore.SaveDecorationRooms(draft.backgroundId,drafts.Values)) return;
        dirty=false;savedBackground=draft.backgroundId;status.text="Saved! Your background and decorations are updated everywhere.";RefreshSelection();
    }
    private void ShowRoom()
    {
        var item=EconomyStore.FindBackground(draft.backgroundId);room.sprite=item.baseSprite ? item.baseSprite : item.sprite;roomName.text=item.displayName;
    }
    public void OpenBackgrounds()
    {
        if(!IsOpen || !backgroundPanel) return;
        backgroundPage=0;backgroundPanel.SetActive(true);backgroundPanel.transform.SetAsLastSibling();RefreshBackgrounds();
    }
    public void CloseBackgrounds() {if(backgroundPanel) backgroundPanel.SetActive(false);}
    public void PreviousBackground() {backgroundPage--;RefreshBackgrounds();}
    public void NextBackground() {backgroundPage++;RefreshBackgrounds();}
    private void RefreshBackgrounds()
    {
        if(!IsOpen || !backgroundPanel || !backgroundPanel.activeSelf || backgroundCards.Length==0) return;
        var inventory=EconomyStore.Inventory;ownedRooms.Clear();
        foreach(var item in EconomyStore.Config.backgrounds) if(item!=null && item.sprite && inventory.ownedBackgrounds.Contains(item.id)) ownedRooms.Add(item);
        int pages=Mathf.Max(1,Mathf.CeilToInt(ownedRooms.Count/(float)backgroundCards.Length));backgroundPage=Mathf.Clamp(backgroundPage,0,pages-1);
        for(int i=0;i<backgroundCards.Length;i++)
        {
            var card=backgroundCards[i];int index=backgroundPage*backgroundCards.Length+i;card.preview.transform.parent.gameObject.SetActive(index<ownedRooms.Count);
            if(index>=ownedRooms.Count) continue;var item=ownedRooms[index];bool chosen=item.id==draft.backgroundId;
            card.preview.sprite=item.baseSprite ? item.baseSprite : item.sprite;card.title.text=item.displayName;
            card.detail.text=chosen ? "CURRENT PREVIEW" : item.id==inventory.selectedBackground ? "EQUIPPED" : "OWNED";
            card.action.text=chosen ? "SELECTED" : "CHOOSE";card.button.interactable=!chosen;
        }
        backgroundPageLabel.text=ownedRooms.Count+" OWNED  /  "+(backgroundPage+1)+" OF "+pages;
        backgroundPrevious.interactable=backgroundPage>0;backgroundNext.interactable=backgroundPage<pages-1;
    }
    private void ChooseBackground(int index) {if(index>=0 && index<ownedRooms.Count) SelectBackground(ownedRooms[index].id);}
    private void PreviewBackground(int slot)
    {
        int index=backgroundPage*backgroundCards.Length+slot;
        if(index>=0 && index<ownedRooms.Count && preview) preview.OpenBackground(ownedRooms[index].id);
    }
    public void SelectBackground(string id)
    {
        var item=EconomyStore.FindBackground(id);
        if(!IsOpen || draft==null || item==null || !item.sprite || !EconomyStore.Inventory.ownedBackgrounds.Contains(id)) return;
        if(!drafts.TryGetValue(id,out var nextDraft)) {nextDraft=EconomyStore.GetRoomLayout(id);drafts.Add(id,nextDraft);}
        draft=nextDraft;selected=draft.items.Find(p=>EconomyStore.FindDecoration(p.itemId).slot==selectedSlot)?.itemId;browsed=null;inventoryPage=0;
        CloseBackgrounds();ShowRoom();Render();RefreshInventory();status.text="Previewing "+item.displayName+". Save to apply your background and decorations.";
    }
    private void RefreshInventory()
    {
        RefreshShop();if(!IsOpen) return;RefreshBackgrounds();
        var inventory=EconomyStore.Inventory;coins.text=inventory.coins+" COIN";owned.Clear();
        foreach(var item in EconomyStore.Config.decorations)
            if(item.slot==selectedSlot && inventory.ownedDecorations.Contains(item.id)) owned.Add(item);
        int pages=Mathf.Max(1,Mathf.CeilToInt(owned.Count/6f));inventoryPage=Mathf.Clamp(inventoryPage,0,pages-1);
        for(int i=0;i<inventoryCards.Length;i++)
        {
            int index=inventoryPage*6+i;var card=inventoryCards[i];card.preview.transform.parent.gameObject.SetActive(index<owned.Count);
            if(index>=owned.Count) continue;
            var item=owned[index];bool isPlaced=draft.items.Exists(p=>p.itemId==item.id);
            card.preview.sprite=item.sprite;card.preview.enabled=item.sprite;card.title.text=item.displayName;card.detail.text=item.sprite ? item.skin+(isPlaced ? "\nEQUIPPED" : "") : "DISPLAY COMING SOON";
            card.action.text=!item.sprite ? "COLLECTED" : isPlaced ? "REMOVE" : "EQUIP";card.button.interactable=item.sprite;
        }
        pageLabel.text=DecorationSlots.Label(selectedSlot)+" / "+owned.Count+" OWNED / "+(inventoryPage+1)+" OF "+pages;
        if(owned.Count==0) status.text="No owned items for this area. Visit ACCESSORIES in Shop.";
        previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);
        previous.interactable=inventoryPage>0;next.interactable=inventoryPage<pages-1;
        var list=panel.transform.Find("StudioOwnedList/Items") as RectTransform;if(list)list.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(187.2f,Mathf.Min(6,owned.Count-inventoryPage*6)*84));
        if(!owned.Exists(i=>i.id==browsed))browsed=owned.Exists(i=>i.id==selected)?selected:owned.Count>0?owned[0].id:null;
        RefreshSelection();
    }
    public void Previous() {inventoryPage--;RefreshInventory();}
    public void Next() {inventoryPage++;RefreshInventory();}
    public void BrowseOwned(int index)
    {if(index<0 || index>=owned.Count)return;browsed=owned[index].id;RefreshSelection();}
    public void EquipBrowsed()
    {if(string.IsNullOrEmpty(browsed))return;if(draft.items.Exists(p=>p.itemId==browsed)){selected=browsed;Remove();}else Place(browsed);}
    public void Place(string id)
    {
        var item=EconomyStore.FindDecoration(id);
        if(!IsOpen || !EconomyStore.Inventory.ownedDecorations.Contains(id) || item==null || !item.sprite) return;
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
        selectedSlot=(DecorationSlot)slot;inventoryPage=0;browsed=null;
        selected=draft.items.Find(p=>EconomyStore.FindDecoration(p.itemId).slot==selectedSlot)?.itemId;
        status.text="Choose one item for "+DecorationSlots.Label(selectedSlot)+". It stays in the highlighted area.";
        RefreshInventory();
    }
    private DecorationPlacement Selected => draft?.items.Find(p=>p.itemId==selected);
    public void Remove() {if(Selected==null) return;draft.items.Remove(Selected);selected=null;status.text=DecorationSlots.Label(selectedSlot)+" cleared. Save to apply.";Changed();RefreshInventory();}
    public void Clear() {if(draft==null) return;draft.items.Clear();selected=null;status.text="Room cleared. Save to apply.";Changed();RefreshInventory();}
    private void Changed() {dirty=true;Render();RefreshSelection();}
    private void RefreshSelection()
    {
        bool has=Selected!=null;var item=has ? EconomyStore.FindDecoration(selected) : null;
        selection.text=DecorationSlots.Label(selectedSlot)+"  /  "+(has ? item.displayName : "EMPTY");
        remove.interactable=has;
        save.interactable=dirty || draft!=null && draft.backgroundId!=savedBackground;clear.interactable=draft!=null && draft.items.Count>0;
        for(int i=0;i<slotButtons.Length;i++) slotButtons[i].GetComponent<Image>().sprite=Resources.Load<Sprite>("TabButtons/"+(i+1==(int)selectedSlot?"Green":"Blue"));
        TabButtonStyle.ApplyAll(transform);
        if(slotHighlight && draft!=null)
        {
            var anchor=DecorationSlots.Anchor(draft.backgroundId,selectedSlot);var rect=slotHighlight.rectTransform;
            rect.anchorMin=rect.anchorMax=anchor.position;rect.pivot=anchor.pivot;rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta=new Vector2(anchor.maxSize.x*room.rectTransform.rect.width/1920,anchor.maxSize.y*room.rectTransform.rect.height/1080);
            rect.SetAsLastSibling();
        }
        foreach(var pair in placed) pair.Value.color=Color.white;
        var sizing=GetComponent<CacaoArtSizing>();if(sizing) sizing.Apply();
        var shown=EconomyStore.FindDecoration(browsed);UnifiedCafeLayouts.StudioItem(this,shown,shown!=null && draft!=null && draft.items.Exists(p=>p.itemId==shown.id));
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
    public void PrepareStudioSceneView()
    {
        UnifiedCafeLayouts.Studio(this);var bg=EconomyStore.FindBackground("cafe");room.sprite=bg.baseSprite?bg.baseSprite:bg.sprite;roomName.text=bg.displayName;
        var sample=EconomyStore.FindDecoration("moonlight_moonflowerplant");
        for(int i=0;i<inventoryCards.Length;i++){var c=inventoryCards[i];c.preview.transform.parent.gameObject.SetActive(i==0);if(i==0){c.preview.sprite=sample.sprite;c.title.text=sample.displayName;c.detail.text=sample.skin+" collection";}}
        var list=(RectTransform)panel.transform.Find("StudioOwnedList/Items");list.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,187.2f);
        pageLabel.text="PLANTS / 1 OWNED";selection.text="PLANTS AREA";status.text="Choose an area, preview an item, then save your room.";
        UnifiedCafeLayouts.StudioItem(this,sample,false);previous.gameObject.SetActive(false);next.gameObject.SetActive(false);
        var old=room.transform.Find("StudioSceneSample");var image=old?old.GetComponent<Image>():new GameObject("StudioSceneSample",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(room.transform,false);image.sprite=sample.sprite;image.preserveAspect=true;image.raycastTarget=false;image.gameObject.SetActive(true);
        RoomDecorationView.ApplyRect(image.rectTransform,room.rectTransform,sample,"cafe");
        slotHighlight.gameObject.SetActive(false);panel.SetActive(false);
    }
    private void OnDestroy() {EconomyStore.InventoryChanged-=RefreshInventory;}
}
