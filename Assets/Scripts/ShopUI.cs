using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ShopBackgroundCard
{
    public string id;
    public Button button;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI description;
    public Image preview;
    public GameObject lockIcon;
    public Image actionIcon;
}

public class ShopUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI[] coinLabels;
    public TextMeshProUGUI hintCount;
    public TextMeshProUGUI shuffleCount;
    public TextMeshProUGUI shopStock;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI hintPriceText;
    public TextMeshProUGUI shufflePriceText;
    [HideInInspector] public TextMeshProUGUI gardenButtonText;
    [HideInInspector] public TextMeshProUGUI sunsetButtonText;
    public Button hintBuyButton;
    public Button shuffleBuyButton;
    [HideInInspector] public Button gardenButton;
    [HideInInspector] public Button sunsetButton;
    public Image[] backgroundTargets;
    public RectTransform mainMenuPanel;
    public ShopBackgroundCard[] backgroundCards;
    public ScrollRect productScroll;
    public Sprite buyIcon;
    public Sprite useIcon;
    public GameObject boosterPage;
    public GameObject[] backgroundPages;
    public Image boosterTab;
    public Image backgroundTab;
    public Sprite selectedTabSprite;
    public Sprite otherTabSprite;
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI backgroundPageText;
    public GameObject decorationPage;
    public Image decorationTab;
    public DecorationStudio decorations;
    private int backgroundPage;
    private bool showingBackgrounds;
    private bool showingDecorations;
    public bool IsOpen => panel && panel.activeSelf;

    private void Awake()
    {
        EconomyStore.InventoryChanged+=Refresh;
        if(backgroundCards!=null)
            foreach(var card in backgroundCards)
            {
                if(card==null || !card.button) continue;
                string id=card.id;
                card.button.onClick.AddListener(()=>SelectBackground(id));
                if(card.preview)
                {
                    card.preview.raycastTarget=true;
                    var previewButton=card.preview.GetComponent<Button>();if(!previewButton) previewButton=card.preview.gameObject.AddComponent<Button>();
                    previewButton.targetGraphic=card.preview;previewButton.onClick.AddListener(()=>decorations.preview.OpenBackground(id));
                }
            }
        if(panel) panel.SetActive(false);
        Refresh();
    }
    public void Open()
    {
        var session=GridManager.Instance ? GridManager.Instance.Session : null;
        if(!panel || !mainMenuPanel || !session || session.State!=GameState.Menu) return;
        session.OpenShop();
        mainMenuPanel.gameObject.SetActive(false);
        panel.SetActive(true);
        CafePanelStyle.Shop(panel.transform);
        panel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        var canvas=panel.GetComponent<Canvas>();
        if(canvas) { canvas.overrideSorting=true; canvas.sortingOrder=200; }
        Refresh();
        ShowBoosters();
        if(productScroll) { productScroll.StopMovement(); productScroll.verticalNormalizedPosition=1; }
        if(statusText) statusText.text="";
    }
    public void Close()
    {
        if(!IsOpen) return;
        if(decorations && decorations.preview) decorations.preview.Close();
        panel.SetActive(false);
        if(mainMenuPanel)
        {
            mainMenuPanel.anchoredPosition=Vector2.zero;
            mainMenuPanel.gameObject.SetActive(true);
        }
        if(GridManager.Instance && GridManager.Instance.Session) GridManager.Instance.Session.CloseShop();
    }
    public void BuyHint() => BuyBooster(BoosterType.Hint);
    public void BuyShuffle() => BuyBooster(BoosterType.Shuffle);
    public void ShowBoosters() { showingBackgrounds=false;showingDecorations=false; RefreshPages(); }
    public void ShowBackgrounds() { showingBackgrounds=true;showingDecorations=false; backgroundPage=0; RefreshPages(); }
    public void ShowDecorations() {showingBackgrounds=false;showingDecorations=true;backgroundPage=0;RefreshPages();}
    public void ShowAccessoryPage(int page) {showingBackgrounds=false;showingDecorations=true;backgroundPage=page;RefreshPages();}
    public void NextBackgroundPage() { backgroundPage++; RefreshPages(); }
    public void PreviousBackgroundPage() { backgroundPage--; RefreshPages(); }
    private void RefreshPages()
    {
        if(!boosterPage) return;
        boosterPage.SetActive(!showingBackgrounds && !showingDecorations);
        if(decorationPage) decorationPage.SetActive(showingDecorations);
        int pageCount=showingDecorations && decorations ? decorations.ShopPageCount : (backgroundPages==null ? 0 : backgroundPages.Length);
        backgroundPage=Mathf.Clamp(backgroundPage,0,Mathf.Max(0,pageCount-1));
        if(backgroundPages!=null)
        {
            for(int i=0;i<backgroundPages.Length;i++)
                if(backgroundPages[i]) backgroundPages[i].SetActive(showingBackgrounds && i==backgroundPage);
        }
        if(boosterTab) boosterTab.sprite=showingBackgrounds || showingDecorations ? otherTabSprite : selectedTabSprite;
        if(backgroundTab) backgroundTab.sprite=showingBackgrounds ? selectedTabSprite : otherTabSprite;
        if(decorationTab) decorationTab.sprite=showingDecorations ? selectedTabSprite : otherTabSprite;
        if(showingDecorations && decorations) decorations.ShowShopPage(backgroundPage);
        if(previousPageButton)
        {
            previousPageButton.gameObject.SetActive(showingBackgrounds || showingDecorations);
            previousPageButton.interactable=backgroundPage>0;
        }
        if(nextPageButton)
        {
            nextPageButton.gameObject.SetActive(showingBackgrounds || showingDecorations);
            nextPageButton.interactable=backgroundPage<pageCount-1;
        }
        if(backgroundPageText)
        {
            backgroundPageText.gameObject.SetActive(showingBackgrounds || showingDecorations);
            backgroundPageText.text=(backgroundPage+1)+" / "+pageCount;
        }
        var sizing=GetComponent<CacaoArtSizing>();if(sizing) sizing.Apply();
    }
    private void BuyBooster(BoosterType booster)
    {
        EconomyStore.TryBuyBooster(booster,out var message);
        if(statusText) statusText.text=message;
    }
    public void SelectGarden() { SelectBackground(EconomyStore.DefaultBackgroundId); }
    public void SelectSunset() { SelectBackground("japan"); }
    private void SelectBackground(string id)
    {
        var inventory=EconomyStore.Inventory;
        if(inventory.ownedBackgrounds.Contains(id))
        {
            if(EconomyStore.TryEquipBackground(id) && statusText) statusText.text="Background equipped.";
        }
        else
        {
            EconomyStore.TryBuyBackground(id,out var message);
            if(statusText) statusText.text=message;
        }
    }
    public void Refresh()
    {
        var inventory=EconomyStore.Inventory; var config=EconomyStore.Config;
        if(coinLabels!=null) foreach(var label in coinLabels) if(label) label.text=inventory.coins.ToString();
        if(hintCount) hintCount.text=inventory.hints.ToString();
        if(shuffleCount) shuffleCount.text=inventory.shuffles.ToString();
        if(shopStock) shopStock.text="Inventory: "+inventory.hints+" hints  •  "+inventory.shuffles+" shuffles";
        int hintPrice=Mathf.Max(1,config ? config.hintPrice : 75), shufflePrice=Mathf.Max(1,config ? config.shufflePrice : 100);
        if(hintPriceText) hintPriceText.text=boosterPage ? "BUY "+hintPrice : "BUY • "+hintPrice+" COIN";
        if(shufflePriceText) shufflePriceText.text=boosterPage ? "BUY "+shufflePrice : "BUY • "+shufflePrice+" COIN";
        if(hintBuyButton) hintBuyButton.interactable=inventory.coins>=hintPrice;
        if(shuffleBuyButton) shuffleBuyButton.interactable=inventory.coins>=shufflePrice;
        if(backgroundCards!=null && backgroundCards.Length>0)
        {
            foreach(var card in backgroundCards)
            {
                if(card==null) continue;
                var item=EconomyStore.FindBackground(card.id);
                UpdateBackgroundButton(card.id,card.button,card.buttonText,inventory,item);
                bool rewardLocked=item!=null && item.rewardOnly && !inventory.ownedBackgrounds.Contains(card.id);
                bool locked=rewardLocked || item!=null && !inventory.ownedBackgrounds.Contains(card.id) && !EconomyStore.IsBackgroundUnlocked(item);
                if(card.description) card.description.text=locked ? (boosterPage ? "Unlock to buy" : "Complete level "+item.requiredCompletedLevel+" to unlock purchase") : card.id==EconomyStore.DefaultBackgroundId ? "Included" : inventory.ownedBackgrounds.Contains(card.id) ? "Owned" : "Permanent background";
                if(card.description && item!=null && item.rewardOnly) card.description.text=inventory.ownedBackgrounds.Contains(card.id) ? "Chapter reward - owned" : RoomProgress.SourceForBackground(card.id);
                if(card.preview)
                {
                    if(item!=null && item.sprite) card.preview.sprite=item.baseSprite ? item.baseSprite : item.sprite;
                    card.preview.color=locked ? new Color(.55f,.55f,.55f,1) : Color.white;
                }
                if(card.lockIcon) card.lockIcon.SetActive(locked);
                if(card.actionIcon)
                {
                    card.actionIcon.gameObject.SetActive(!locked);
                    card.actionIcon.sprite=inventory.ownedBackgrounds.Contains(card.id) ? useIcon : buyIcon;
                }
            }
        }
        else
        {
            UpdateBackgroundButton(EconomyStore.DefaultBackgroundId,gardenButton,gardenButtonText,inventory);
            UpdateBackgroundButton("japan",sunsetButton,sunsetButtonText,inventory);
        }
        var selected=EconomyStore.FindBackground(inventory.selectedBackground);
        if(selected!=null && selected.sprite)
        {
            if(backgroundTargets!=null)
                foreach(var image in backgroundTargets) if(image) image.sprite=selected.baseSprite ? selected.baseSprite : selected.sprite;
            var shopBackground=panel ? panel.GetComponent<Image>() : null;
            if(shopBackground) shopBackground.sprite=selected.baseSprite ? selected.baseSprite : selected.sprite;
        }
    }
    private void UpdateBackgroundButton(string id,Button button,TextMeshProUGUI label,PlayerInventory inventory,ShopBackground item=null)
    {
        if(item==null) item=EconomyStore.FindBackground(id);
        bool owned=inventory.ownedBackgrounds.Contains(id), selected=inventory.selectedBackground==id;
        int price=item==null ? 0 : Mathf.Max(0,item.price);
        bool unlocked=owned || EconomyStore.IsBackgroundUnlocked(item);
        if(label) label.text=!unlocked && item!=null ? "LOCKED • LEVEL "+item.requiredCompletedLevel : selected ? "EQUIPPED" : owned ? "USE" : "BUY • "+price+" COIN";
        if(label && boosterPage) label.text=!unlocked && item!=null ? "LEVEL "+item.requiredCompletedLevel : selected ? "EQUIPPED" : owned ? "USE" : "BUY "+price.ToString("N0");
        if(button) button.interactable=item!=null && !selected && unlocked && (owned || inventory.coins>=price);
        if(item!=null && item.rewardOnly && !owned)
        {
            if(label) label.text="CHAPTER REWARD";
            if(button) button.interactable=false;
        }
    }
    private void OnDestroy()
    {
        EconomyStore.InventoryChanged-=Refresh;
    }
}
