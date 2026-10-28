using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CacaoReadabilityLayout
{
    private static RectTransform Place(Transform target,float x,float y,float width,float height)
    {
        var rect=(RectTransform)target;
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        return rect;
    }
    private static void Font(TextMeshProUGUI label,float size)
    {
        label.fontSize=label.fontSizeMin=label.fontSizeMax=size;
        label.enableAutoSizing=false;label.enableWordWrapping=false;
        label.alignment=TextAlignmentOptions.Center;label.margin=Vector4.zero;
    }
    private static void Center(Button button,TextMeshProUGUI label)
    {
        label.transform.SetParent(button.transform,false);
        var rect=label.rectTransform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);
        rect.offsetMin=new Vector2(12,4);rect.offsetMax=new Vector2(-12,-4);
        label.alignment=TextAlignmentOptions.Center;label.margin=Vector4.zero;label.raycastTarget=false;
    }
    private static Transform Closest(Transform root,string name,Vector3 point)
    {
        return root.Cast<Transform>().Where(t=>t.name==name).OrderBy(t=>(t.TransformPoint(((RectTransform)t).rect.center)-point).sqrMagnitude).First();
    }
    private static void CenterButtonLabels(Transform root)
    {
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            if(button.name=="ShuffleButton" || button.name=="HintButton" || button.name=="LevelTemplate") continue;
            if(button.TryGetComponent<Image>(out var image) && image.sprite && image.sprite.name.StartsWith("Icon__")) continue;
            if(image && image.sprite) image.preserveAspect=false;
            var own=button.GetComponentInChildren<TextMeshProUGUI>(true);
            if(own) {Center(button,own);continue;}
            var rect=(RectTransform)button.transform;
            var candidate=button.transform.parent.Cast<Transform>().Select(t=>t.GetComponent<TextMeshProUGUI>()).Where(t=>t && t.gameObject.activeSelf)
                .Where(t=>rect.rect.Contains(rect.InverseTransformPoint(t.rectTransform.TransformPoint(t.rectTransform.rect.center))))
                .OrderBy(t=>(t.rectTransform.TransformPoint(t.rectTransform.rect.center)-rect.TransformPoint(rect.rect.center)).sqrMagnitude).FirstOrDefault();
            if(candidate) Center(button,candidate);
        }
    }
    private static void Card(ShopBackgroundCard card,float x,float y)
    {
        var root=card.button.transform.parent;
        string titleName=card.id=="cafe" ? "COZY_CAFE" : card.id.ToUpperInvariant()+"_CAFE";
        string cardName="Card__"+titleName.Replace('_',' ');
        var oldCenter=card.preview.rectTransform.TransformPoint(card.preview.rectTransform.rect.center);
        var well=Closest(root,"Thumbnail__Well",oldCenter);
        var footer=Closest(root,"Text__Permanent_background",oldCenter);
        footer.gameObject.SetActive(false);
        Place(root.Find(cardName),x,y,620,190);
        Place(well,x+12,y+16,268,158);
        Place(card.preview.transform,x+16,y+20,260,150);
        var title=root.Find("Text__"+titleName).GetComponent<TextMeshProUGUI>();
        Place(title.transform,x+290,y+8,318,44);Font(title,32);
        Place(card.description.transform,x+290,y+55,318,40);Font(card.description,28);
        Place(card.button.transform,x+310,y+100,280,80);
        Font(card.buttonText,30);Center(card.button,card.buttonText);
        var colors=card.button.colors;colors.disabledColor=Color.white;card.button.colors=colors;
        if(card.lockIcon) Place(card.lockIcon.transform,x+115,y+63,62,62);
    }
    private static void Booster(ShopUI shop,bool hint,float x)
    {
        var root=shop.boosterPage.transform;
        var button=hint ? shop.hintBuyButton : shop.shuffleBuyButton;
        string title=hint ? "HINT" : "SHUFFLE";
        var oldCenter=button.transform.TransformPoint(((RectTransform)button.transform).rect.center);
        var panel=Closest(root,"Panel__ItemCard",oldCenter);
        panel.GetComponent<Image>().preserveAspect=false;
        Place(panel,x,420,430,450);
        var heading=root.Find("Text__"+title).GetComponent<TextMeshProUGUI>();
        Place(heading.transform,x+45,495,340,58);Font(heading,40);
        Place(root.Find(hint ? "Icon__Hint" : "Icon__Shuffle"),x+141,559,148,148);
        var description=root.Find(hint ? "Text__Add_1_hint" : "Text__Add_1_shuffle").GetComponent<TextMeshProUGUI>();
        Place(description.transform,x+35,740,360,42);Font(description,30);
        Place(button.transform,x+55,850,320,100);
        var label=hint ? shop.hintPriceText : shop.shufflePriceText;
        Font(label,34);Center(button,label);
    }
    [MenuItem("Tools/Apply Combo Praise")]
    public static void ApplyComboPraise()
    {
        var controller=Object.FindObjectOfType<CacaoUIController>();
        var parent=controller.ui.gameplayPanel;var existing=parent.Find("ComboPraise");
        var label=existing ? existing.GetComponent<TextMeshProUGUI>() : new GameObject("ComboPraise",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent,false);
        var rect=Place(label.transform,1470,295,440,120);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(1690,-355);
        rect.localScale=Vector3.one;rect.localRotation=Quaternion.Euler(0,0,4);
        label.font=GameTypography.Current.font;label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao Heading.mat");
        label.text="";label.fontSize=label.fontSizeMax=68;label.fontSizeMin=52;label.enableAutoSizing=true;label.enableWordWrapping=false;
        label.alignment=TextAlignmentOptions.Center;label.margin=new Vector4(10,5,10,5);label.color=new Color(1,.83f,.25f,0);label.raycastTarget=false;
        label.transform.SetAsLastSibling();label.gameObject.SetActive(false);controller.comboPraise=label;
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }
    public static void Apply()
    {
        var controller=Object.FindObjectOfType<CacaoUIController>();
        var shop=Object.FindObjectOfType<ShopUI>();
        var root=controller.transform;
        foreach(var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if(label.text=="RESTART LEVEL") label.text="RESTART";
            if(label.fontSize>=28) {label.fontSizeMin=Mathf.Min(label.fontSize,28);continue;}
            float height=label.rectTransform.rect.height;
            label.fontSize=label.fontSizeMax=28;label.fontSizeMin=26;
            label.enableAutoSizing=true;
            if(height<36 && label.rectTransform.anchorMin==label.rectTransform.anchorMax)
            {
                label.rectTransform.anchoredPosition+=new Vector2(0,(36-height)/2);
                label.rectTransform.sizeDelta=new Vector2(label.rectTransform.sizeDelta.x,36);
            }
        }
        var panel=shop.panel.transform;
        var image=panel.Find("Panel__Large").GetComponent<Image>();
        image.preserveAspect=false;Place(image.transform,70,50,1780,1000);
        Place(panel.Find("Panel__Header"),580,45,760,230);
        panel.Find("Panel__Header").GetComponent<Image>().preserveAspect=false;
        var heading=panel.Find("Text__SHOP").GetComponent<TextMeshProUGUI>();
        Place(heading.transform,700,125,520,80);Font(heading,56);
        Place(shop.boosterTab.transform,550,250,380,126);
        Place(shop.backgroundTab.transform,990,250,380,126);
        Font(panel.Find("Text__BOOSTERS").GetComponent<TextMeshProUGUI>(),34);
        Font(panel.Find("Text__BACKGROUNDS").GetComponent<TextMeshProUGUI>(),34);
        var boosterLabel=panel.Find("Text__BOOSTERS").GetComponent<TextMeshProUGUI>();
        var backgroundLabel=panel.Find("Text__BACKGROUNDS").GetComponent<TextMeshProUGUI>();
        Center(shop.boosterTab.GetComponent<Button>(),boosterLabel);Center(shop.backgroundTab.GetComponent<Button>(),backgroundLabel);
        Place(shop.shopStock.transform,400,357,1120,42);Font(shop.shopStock,30);
        Place(shop.statusText.transform,350,1000,1220,42);Font(shop.statusText,28);
        Booster(shop,true,450);Booster(shop,false,1040);
        for(int i=0;i<shop.backgroundCards.Length;i++)
        {
            int local=i<4 ? i : i-4;
            float x=local%2==0 ? 320 : 980,y=local<2 ? 410 : 620;
            if(i==6) x=650;
            Card(shop.backgroundCards[i],x,y);
        }
        var first=shop.backgroundPages[0].transform;
        var last=shop.backgroundPages[shop.backgroundPages.Length-1].transform;
        var next=first.Find("Button__NEXT").GetComponent<Button>();
        var previous=last.Find("Button__PREV").GetComponent<Button>();
        var nextLabel=first.Find("Text__NEXT").GetComponent<TextMeshProUGUI>();
        var previousLabel=last.Find("Text__PREV").GetComponent<TextMeshProUGUI>();
        next.transform.SetParent(panel,false);previous.transform.SetParent(panel,false);
        Place(previous.transform,400,852,300,100);Place(next.transform,1220,852,300,100);
        Font(nextLabel,32);Font(previousLabel,32);Center(next,nextLabel);Center(previous,previousLabel);
        shop.backgroundPageText=first.Find("Text__1_2").GetComponent<TextMeshProUGUI>();
        shop.backgroundPageText.transform.SetParent(panel,false);
        Place(shop.backgroundPageText.transform,810,880,300,44);Font(shop.backgroundPageText,32);
        last.Find("Text__2_2").gameObject.SetActive(false);
        shop.nextPageButton=next;shop.previousPageButton=previous;
        next.gameObject.SetActive(false);previous.gameObject.SetActive(false);shop.backgroundPageText.gameObject.SetActive(false);
        CenterButtonLabels(root);
        var combo=controller.grid.comboText;
        Place(combo.transform,1590,410,300,190);Font(combo,64);combo.enableWordWrapping=true;
        combo.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao Heading.mat");
        combo.color=new Color(1,.83f,.25f,1);
        ApplyComboPraise();
        Place(controller.ui.settingsRestartButton.transform,1210,710,95,95);
        foreach(var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            if(label.name=="SettingsCaption" || label.name=="Text__HOME" || label.name=="Text__RETRY" || label.name=="Caption" && label.transform.parent==controller.ui.settingsRestartButton.transform)
            {
                label.fontSharedMaterial=combo.fontSharedMaterial;label.color=new Color(1,.98f,.91f);
            }
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }
}
