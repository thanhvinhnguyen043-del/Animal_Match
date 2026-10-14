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
    public TextMeshProUGUI gardenButtonText;
    public TextMeshProUGUI sunsetButtonText;
    public Button hintBuyButton;
    public Button shuffleBuyButton;
    public Button gardenButton;
    public Button sunsetButton;
    public Image[] backgroundTargets;
    public RectTransform mainMenuPanel;
    public ShopBackgroundCard[] backgroundCards;
    public ScrollRect productScroll;
    public Sprite buyIcon;
    public Sprite useIcon;
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
        panel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        var canvas=panel.GetComponent<Canvas>();
        if(canvas) { canvas.overrideSorting=true; canvas.sortingOrder=200; }
        Refresh();
        if(productScroll) { productScroll.StopMovement(); productScroll.verticalNormalizedPosition=1; }
        if(statusText) statusText.text="";
    }
    public void Close()
    {
        if(!IsOpen) return;
        panel.SetActive(false);
        if(mainMenuPanel)
        {
            mainMenuPanel.anchoredPosition=Vector2.zero;
            mainMenuPanel.gameObject.SetActive(true);
        }
        if(GridManager.Instance && GridManager.Instance.Session) GridManager.Instance.Session.CloseShop();
    }
    public void BuyHint()
    {
        EconomyStore.TryBuyBooster(BoosterType.Hint,out var message);
        if(statusText) statusText.text=message;
    }
    public void BuyShuffle()
    {
        EconomyStore.TryBuyBooster(BoosterType.Shuffle,out var message);
        if(statusText) statusText.text=message;
    }
    public void SelectGarden() { SelectBackground("garden"); }
    public void SelectSunset() { SelectBackground("sunset"); }
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
        if(hintPriceText) hintPriceText.text="BUY • "+hintPrice+" COIN";
        if(shufflePriceText) shufflePriceText.text="BUY • "+shufflePrice+" COIN";
        if(hintBuyButton) hintBuyButton.interactable=inventory.coins>=hintPrice;
        if(shuffleBuyButton) shuffleBuyButton.interactable=inventory.coins>=shufflePrice;
        if(backgroundCards!=null && backgroundCards.Length>0)
        {
            foreach(var card in backgroundCards)
            {
                if(card==null) continue;
                UpdateBackgroundButton(card.id,card.button,card.buttonText,inventory);
                var item=EconomyStore.FindBackground(card.id);
                bool locked=item!=null && !inventory.ownedBackgrounds.Contains(card.id) && !EconomyStore.IsBackgroundUnlocked(item);
                if(card.description) card.description.text=locked ? "Complete level "+item.requiredCompletedLevel+" to unlock purchase" : inventory.ownedBackgrounds.Contains(card.id) ? "Owned" : "Permanent background";
                if(card.preview)
                {
                    if(item!=null && item.sprite) card.preview.sprite=item.sprite;
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
            UpdateBackgroundButton("garden",gardenButton,gardenButtonText,inventory);
            UpdateBackgroundButton("sunset",sunsetButton,sunsetButtonText,inventory);
        }
        var selected=EconomyStore.FindBackground(inventory.selectedBackground);
        if(selected!=null && selected.sprite)
        {
            if(backgroundTargets!=null)
                foreach(var image in backgroundTargets) if(image) image.sprite=selected.sprite;
            var shopBackground=panel ? panel.GetComponent<Image>() : null;
            if(shopBackground) shopBackground.sprite=selected.sprite;
        }
    }
    private void UpdateBackgroundButton(string id,Button button,TextMeshProUGUI label,PlayerInventory inventory)
    {
        var item=EconomyStore.FindBackground(id);
        bool owned=inventory.ownedBackgrounds.Contains(id), selected=inventory.selectedBackground==id;
        int price=item==null ? 0 : Mathf.Max(0,item.price);
        bool unlocked=owned || EconomyStore.IsBackgroundUnlocked(item);
        if(label) label.text=!unlocked && item!=null ? "LOCKED • LEVEL "+item.requiredCompletedLevel : selected ? "EQUIPPED" : owned ? "USE" : "BUY • "+price+" COIN";
        if(button) button.interactable=item!=null && !selected && unlocked && (owned || inventory.coins>=price);
    }
    private void OnDestroy()
    {
        EconomyStore.InventoryChanged-=Refresh;
    }
}
