using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RoomRewardCard
{
    public GameObject root;
    public Image preview;
    public TextMeshProUGUI title,detail,progress,action;
    public Button button;
    [NonSerialized] public string id;
}

[DefaultExecutionOrder(130)]
public sealed class RoomRewardsUI : MonoBehaviour
{
    public UIManager ui;
    public ShopUI shop;
    public GameObject panel,badge;
    public CanvasGroup menuInput;
    public TextMeshProUGUI summary,pageLabel,notice,badgeLabel;
    public Button previous,next;
    public Button[] filters;
    public RoomRewardCard[] cards;
    private GameManager game;
    private int filter,page;
    private bool blocked,oldInput,oldRaycasts;
    public bool IsOpen=>panel && panel.activeSelf;
    private void Start()
    {
        game=GridManager.Instance.Session;EconomyStore.InventoryChanged+=Refresh;game.StateChanged+=OnState;
        for(int i=0;i<filters.Length;i++) {int value=i;filters[i].onClick.AddListener(()=>Filter(value));}
        for(int i=0;i<cards.Length;i++) {int value=i;cards[i].button.onClick.AddListener(()=>Inspect(value));DecorationPreview.BindThumbnail(cards[i].preview,()=>PreviewBackground(value));}
        panel.SetActive(false);Refresh();
    }
    public void Open()
    {
        if(!game || ui.settingsPanel.gameObject.activeSelf || !game.OpenRoomRewards()) return;
        ApplyLayout();
        page=filter=0;panel.SetActive(true);panel.transform.SetAsLastSibling();
        if(menuInput) {oldInput=menuInput.interactable;oldRaycasts=menuInput.blocksRaycasts;menuInput.interactable=menuInput.blocksRaycasts=false;blocked=true;}
        RoomProgress.MarkSeen();Refresh();
    }
    public void Close()
    {
        if(!IsOpen) return;panel.SetActive(false);RestoreInput();game.CloseRoomRewards();
    }
    public void PrepareSceneView()
    {
        ApplyLayout();
        var config=EconomyStore.Config;if(!config)return;
        for(int i=0;i<cards.Length && i<config.roomRewards.Length;i++)
        {
            var reward=config.roomRewards[i];var card=cards[i];card.root.SetActive(true);
            card.id=reward.id;card.title.text=reward.title;card.detail.text=reward.Condition;card.preview.sprite=reward.Sprite;card.preview.enabled=reward.Sprite;
            card.progress.text="0 / "+reward.target;card.action.text="LOCKED";card.button.interactable=false;
        }
        summary.text="Play, collect and make your cafe your own.";
    }
    private void ApplyLayout()
    {
        var saved=GetComponent<SceneUILayout>();
        if(saved && saved.decoratedFramesVersion>=1) return;
        var root=panel.transform;var frame=root.Find("Frame");
        if(frame)
        {
            var old=frame.GetComponent<Image>();if(old) old.enabled=false;
            var backing=root.Find("RoomRewardSurface");
            if(!backing)
            {
                backing=new GameObject("RoomRewardSurface",typeof(RectTransform),typeof(CanvasRenderer),typeof(RoundedImage)).transform;
                backing.SetParent(root,false);backing.SetSiblingIndex(frame.GetSiblingIndex()+1);
            }
            var surface=backing.GetComponent<RoundedImage>();surface.color=new Color(1,.967f,.885f);surface.cornerRadius=32;surface.raycastTarget=false;
            Place(backing,90,155,1740,850);
        }
        Place(root.Find("Header"),540,15,840,140);Place(root.Find("Title"),660,54,600,62);
        Place(root.Find("Close"),1640,66,190,74);Place(summary.transform,180,172,1560,46);
        for(int i=0;i<filters.Length;i++) Place(filters[i].transform,300+i*335,235,315,65);
        for(int i=0;i<cards.Length;i++) Place(cards[i].root.transform,130+i%2*835,325+i/2*267,800,250);
        Place(notice.transform,170,855,1580,62);Place(previous.transform,510,920,240,70);
        Place(next.transform,1170,920,240,70);Place(pageLabel.transform,810,930,300,45);
        pageLabel.fontSharedMaterial=summary.fontSharedMaterial;pageLabel.color=new Color(.29f,.135f,.065f);
        CafeFrameLayouts.RoomRewards(this);
    }
    private static void Place(Transform target,float x,float y,float w,float h)
    {
        if(!target) return;var rect=(RectTransform)target;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
    }
    private void RestoreInput() {if(blocked && menuInput) {menuInput.interactable=oldInput;menuInput.blocksRaycasts=oldRaycasts;}blocked=false;}
    private void OnState(GameState state) {if(state!=GameState.RoomRewards && IsOpen) {panel.SetActive(false);RestoreInput();}}
    public void Filter(int value) {filter=Mathf.Clamp(value,0,3);page=0;Refresh();}
    public void Previous() {page--;Refresh();}
    public void Next() {page++;Refresh();}
    public void Refresh()
    {
        var inventory=EconomyStore.Inventory;int unseen=inventory.newRoomRewards.Count;
        if(badge) badge.SetActive(unseen>0);if(badgeLabel) badgeLabel.text=unseen.ToString();
        if(!IsOpen) return;
        var config=EconomyStore.Config;var rewards=new System.Collections.Generic.List<RoomRewardDefinition>();
        foreach(var item in config.roomRewards) if(item.goal==(filter==0 ? RoomRewardGoal.Chapters : filter==1 ? RoomRewardGoal.Stars : RoomRewardGoal.Collections)) rewards.Add(item);
        int count=filter==2 ? config.roomCollections.Length : rewards.Count;
        int pages=Mathf.Max(1,Mathf.CeilToInt(count/(float)cards.Length));page=Mathf.Clamp(page,0,pages-1);
        pageLabel.text=(page+1)+" / "+pages;previous.interactable=page>0;next.interactable=page<pages-1;
        summary.text=RoomProgress.CompletedChapters(inventory)+" LEVELS CLEARED  /  "+CareerStore.TotalStars(inventory.career)+" MEDALS  /  "+RoomProgress.CompletedCollections(inventory)+" OF "+config.roomCollections.Length+" COLLECTIONS";
        notice.text=filter==2 ? "Collect all four accessories in a set. Mix them freely with any room." : filter==3 ? "Keepsakes stay in your collection. Display items are coming soon." : "Rewards unlock automatically and stay in your inventory. Mix any skin you like.";
        for(int i=0;i<filters.Length;i++) {var image=filters[i].GetComponent<Image>();image.color=Color.white;image.sprite=Resources.Load<Sprite>("TabButtons/"+(i==filter ? "Green" : "Blue"));}
        for(int i=0;i<cards.Length;i++)
        {
            int index=page*cards.Length+i;var card=cards[i];card.root.SetActive(index<count);card.id=null;if(index>=count) continue;
            if(filter==2)
            {
                var set=config.roomCollections[index];card.id=set.id;int value=RoomProgress.CollectionCount(set,inventory);
                card.title.text=set.displayName;card.detail.text="";Sprite sprite=null;
                card.detail.rectTransform.sizeDelta=new Vector2(285,105);
                card.detail.rectTransform.anchoredPosition=new Vector2(225,-62);
                card.detail.fontSizeMax=set.itemIds.Length>4 ? 18 : 24;card.detail.fontSizeMin=15;
                card.detail.lineSpacing=set.itemIds.Length>4 ? -4 : 0;
                foreach(string id in set.itemIds)
                {
                    var item=EconomyStore.FindDecoration(id);if(item==null) continue;if(!sprite && item.sprite) sprite=item.sprite;
                    card.detail.text+=(inventory.ownedDecorations.Contains(id) ? "+ " : "- ")+item.displayName+"\n";
                }
                card.detail.text="Collect "+set.itemIds.Length+" matching accessories";card.preview.sprite=sprite;card.preview.enabled=sprite;card.progress.text=value+" / "+set.itemIds.Length+" OWNED";
                card.action.text=value==set.itemIds.Length ? "COMPLETE" : "BROWSE";card.button.interactable=value<set.itemIds.Length;
            }
            else
            {
                var reward=rewards[index];card.id=reward.id;int value=RoomProgress.Value(reward.goal,inventory);
                bool earned=inventory.unlockedRoomRewards.Contains(reward.id),owned=string.IsNullOrEmpty(reward.backgroundId) ? inventory.ownedDecorations.Contains(reward.decorationId) : inventory.ownedBackgrounds.Contains(reward.backgroundId);
                card.title.text=reward.title;card.detail.text=reward.Condition;
                card.detail.rectTransform.sizeDelta=new Vector2(285,95);
                card.detail.rectTransform.anchoredPosition=new Vector2(225,-65);
                card.detail.fontSizeMax=24;card.detail.fontSizeMin=16;card.detail.lineSpacing=0;
                card.preview.sprite=reward.Sprite;card.preview.enabled=reward.Sprite;
                card.progress.text=(earned ? "UNLOCKED" : owned ? "ALREADY OWNED" : Mathf.Min(value,reward.target)+" / "+reward.target)+(!reward.Sprite ? " - DISPLAY COMING SOON" : "");
                card.action.text=!reward.Sprite ? "SOON" : owned ? string.IsNullOrEmpty(reward.backgroundId) ? "DECORATE" : inventory.selectedBackground==reward.backgroundId ? "EQUIPPED" : "USE" : "LOCKED";
                card.button.interactable=owned && reward.Sprite && inventory.selectedBackground!=reward.backgroundId;
            }
        }
    
        UnifiedCafeLayouts.Rooms(this);
    }
    public void PreviewCard(int index) => PreviewBackground(index);
    private void PreviewBackground(int index)
    {
        if(!IsOpen || filter==2 || index<0 || index>=cards.Length) return;
        var reward=Array.Find(EconomyStore.Config.roomRewards,r=>r.id==cards[index].id);
        if(reward!=null && !string.IsNullOrEmpty(reward.backgroundId)) shop.decorations.preview.OpenBackground(reward.backgroundId);
    }
    private void Inspect(int index)
    {
        if(!IsOpen || index<0 || index>=cards.Length || string.IsNullOrEmpty(cards[index].id)) return;
        string id=cards[index].id;
        if(filter==2)
        {
            var set=Array.Find(EconomyStore.Config.roomCollections,c=>c.id==id);if(set==null) return;
            var inventory=EconomyStore.Inventory;string missing=Array.Find(set.itemIds,item=>!inventory.ownedDecorations.Contains(item));
            Close();shop.Open();shop.ShowDecorations();if(missing!=null) shop.decorations.FocusShopItem(missing);return;
        }
        var reward=Array.Find(EconomyStore.Config.roomRewards,r=>r.id==id);if(reward==null) return;
        if(!string.IsNullOrEmpty(reward.backgroundId)) {EconomyStore.TryEquipBackground(reward.backgroundId);Close();return;}
        var definition=EconomyStore.FindDecoration(reward.decorationId);if(definition==null || !definition.sprite) return;
        Close();shop.decorations.Open();shop.decorations.SelectSlot((int)definition.slot);shop.decorations.Place(definition.id);
    }
    private void OnDestroy()
    {
        RestoreInput();EconomyStore.InventoryChanged-=Refresh;if(!game) return;game.StateChanged-=OnState;
    }
}
