using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(310)]
public sealed class PlayerAvatarUI : MonoBehaviour
{
    private ProgressUI progress;
    private GameManager game;
    private GameObject panel;
    private RectTransform page;
    private Image preview,previewHat,previewLock;
    private TextMeshProUGUI title,condition,pageLabel,notice;
    private Button save,charactersTab,hatsTab,previous,next;
    private readonly List<Image> portraits=new List<Image>(),portraitHats=new List<Image>();
    private readonly Dictionary<string,Sprite> bodies=new Dictionary<string,Sprite>(),faces=new Dictionary<string,Sprite>();
    private readonly List<Transform> cards=new List<Transform>();
    private readonly List<DecorationItem> hats=new List<DecorationItem>();
    private string draftCharacter,draftHat;
    private int tab,hatPage;
    private bool oldInput,oldRaycasts,blocked;
    public bool IsOpen=>panel && panel.activeSelf;
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var existing=parent.Find(name) as RectTransform;
        if(existing) {existing.gameObject.SetActive(true);return existing;}
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);GameplayHudStyle.Place(r,x,y,w,h);return r;
    }
    private Image Art(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        bool exists=parent.Find(name);
        var node=Rect(parent,name,x,y,w,h).gameObject;
        var image=node.GetComponent<Image>();if(!image) image=node.AddComponent<Image>();
        image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
        if(!exists) {image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition+=new Vector2(w*.5f,-h*.5f);}
        AvatarFashionRewards.Style(image);return image;
    }
    private Transform Frame(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        bool card=name.StartsWith("Character_") || name.StartsWith("Headwear_");
        var node=Rect(parent,name,x,y,w,h).gameObject;
        var image=node.GetComponent<Image>();if(!image) image=card ? node.AddComponent<Image>() : node.AddComponent<RoundedImage>();
        if(card) {CafeComponentArt.Apply(image,"Wardrobe_Card");return image.transform;}
        image.color=color;image.raycastTarget=false;
        var border=image.GetComponent<Outline>();if(!border) border=image.gameObject.AddComponent<Outline>();border.effectColor=new Color(.68f,.44f,.25f,.65f);border.effectDistance=new Vector2(1,-1);return image.transform;
    }
    private TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size)
    {
        var textNode=Rect(parent,name,x,y,w,h).gameObject;var label=textNode.GetComponent<TextMeshProUGUI>();if(label) {label.text=value;return label;}label=textNode.AddComponent<TextMeshProUGUI>();label.font=GameTypography.Current.font;label.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;
        label.color=new Color(.35f,.18f,.09f);label.text=value;label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-3;label.enableAutoSizing=true;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private Button Button(Transform parent,string caption,float x,float y,float w,UnityEngine.Events.UnityAction action,bool green=false)
    {
        string nodeName=parent.name.StartsWith("Character_") || parent.name.StartsWith("Headwear_") ? "Action" : caption;
        var image=Art(parent,nodeName,Resources.Load<Sprite>("ActivityIcons/"+(green ? "Primary" : "Secondary")),x,y,w,62);image.preserveAspect=false;image.type=Image.Type.Sliced;image.raycastTarget=true;
        image.pixelsPerUnitMultiplier=image.sprite.rect.height/62;
        var button=image.GetComponent<Button>();if(!button)button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);
        var sound=image.GetComponent<UIButtonClickSound>();if(!sound)sound=image.gameObject.AddComponent<UIButtonClickSound>();sound.clickSound=progress.grid.sfxClick;
        Text(image.transform,"Label",caption,10,7,w-20,46,25).color=Color.white;return button;
    }
    private void Compact(Button button)
    {
        var r=(RectTransform)button.transform;float oldHeight=r.sizeDelta.y;r.sizeDelta=new Vector2(r.sizeDelta.x,40);r.anchoredPosition+=Vector2.up*(oldHeight-40)*.5f;
        var label=(RectTransform)button.transform.Find("Label");GameplayHudStyle.Place(label,10,3,r.sizeDelta.x-20,34);
    }
    private Sprite Body(string id)
    {
        if(!bodies.TryGetValue(id,out var sprite))
        {
            string asset=char.ToUpperInvariant(id[0])+id.Substring(1);
            bodies[id]=sprite=Resources.Load<Sprite>("PlayerAvatars/"+asset);
        }
        return sprite;
    }
    private Sprite Face(string id)
    {
        if(faces.TryGetValue(id,out var face)) return face;
        var body=Body(id);var source=body.rect;float bottom=.38f;
        var crop=new UnityEngine.Rect(source.x+source.width*.08f,source.y+source.height*bottom,source.width*.84f,source.height*(1-bottom));
        face=Sprite.Create(body.texture,crop,new Vector2(.5f,.5f),body.pixelsPerUnit,0,SpriteMeshType.FullRect);face.name=id+"_Portrait";faces[id]=face;return face;
    }
    private void AvatarButton(Transform parent,float x,float y,float size)
    {
        var frame=Rect(parent,"PlayerAvatarButton",x,y,size,size).gameObject.AddComponent<RoundedImage>();frame.cornerRadius=size*.16f;frame.color=new Color(.25f,.12f,.06f);frame.raycastTarget=true;
        var rim=PortraitSurface(frame.transform,"GoldRim",size*.025f,size*.025f,size*.95f,new Color(1,.72f,.23f),size*.14f);
        PortraitSurface(frame.transform,"CreamEdge",size*.065f,size*.065f,size*.87f,new Color(1,.92f,.69f),size*.11f);
        var window=PortraitSurface(frame.transform,"PortraitWindow",size*.095f,size*.095f,size*.81f,PortraitBackground,size*.085f);
        window.gameObject.AddComponent<Mask>().showMaskGraphic=true;
        var button=frame.gameObject.AddComponent<Button>();button.targetGraphic=rim;button.onClick.AddListener(Open);
        frame.gameObject.AddComponent<UIButtonClickSound>().clickSound=progress.grid.sfxClick;
        float inner=size*.81f;
        var face=Art(window.transform,"SelectedFace",null,-inner*.12f,-inner*.08f,inner*1.24f,inner*1.24f);portraits.Add(face);
        var hat=Art(window.transform,"SelectedHat",null,inner*.16f,-inner*.08f,inner*.68f,inner*.48f);portraitHats.Add(hat);
    }
    private RoundedImage PortraitSurface(Transform parent,string name,float x,float y,float size,Color color,float radius)
    {
        var image=Rect(parent,name,x,y,size,size).gameObject.AddComponent<RoundedImage>();image.cornerRadius=radius;image.color=color;image.raycastTarget=false;return image;
    }
    private void Start()
    {
        PrepareSceneView();Build();game=progress.grid.Session;
        var button=progress.ui.mainMenuPanel.Find("PlayerAvatarButton").GetComponent<Button>();
        button.onClick.RemoveListener(Open);button.onClick.AddListener(Open);
        game.StateChanged+=OnState;EconomyStore.InventoryChanged+=InventoryChanged;RefreshPortraits();
    }
    public void PrepareSceneView()
    {
        progress=GetComponent<ProgressUI>();
        portraits.Clear();portraitHats.Clear();
        var saved=progress.ui.mainMenuPanel.Find("PlayerAvatarButton");
        if(saved)
        {
            portraits.Add(saved.Find("PortraitWindow/SelectedFace").GetComponent<Image>());
            portraitHats.Add(saved.Find("PortraitWindow/SelectedHat").GetComponent<Image>());
        }
        else
        {
            AvatarButton(progress.ui.mainMenuPanel,65,45,154);
            Text(progress.ui.mainMenuPanel,"PlayerProfileCaption","MY AVATAR",42,205,200,34,24);
        }
        if(!Application.isPlaying)
        {
            portraits[0].sprite=Face("fox");portraitHats[0].enabled=false;
        }
    }
    private void RefreshPortraits()
    {
        var profile=EconomyStore.Inventory.avatar;var item=EconomyStore.FindDecoration(profile.hat);
        for(int i=0;i<portraits.Count;i++)
        {
            portraits[i].sprite=Face(profile.selected);portraitHats[i].sprite=item==null ? null : item.sprite;portraitHats[i].enabled=item!=null && item.sprite;AvatarFashionRewards.Style(portraitHats[i]);
        }
    }

    private static readonly Color PortraitBackground = new Color(1f, .91f, .76f);

    public void PrepareMenuBadge()
    {
        PrepareSceneView();
        var menu=progress.ui.mainMenuPanel;
        var root=(RectTransform)menu.Find("PlayerAvatarButton");
        GameplayHudStyle.Place(root, 58, 43, 166, 166);
        var window=root.Find("PortraitWindow").GetComponent<RoundedImage>();
        GameplayHudStyle.Place(window.rectTransform, 19, 27, 122, 122);
        window.cornerRadius=61;
        window.color=PortraitBackground;
        root.GetComponent<Image>().color=Color.clear;
        root.Find("GoldRim").gameObject.SetActive(false);
        root.Find("CreamEdge").gameObject.SetActive(false);
        var badge=Art(root,"BiscuitRim",Resources.Load<Sprite>("CafeFrames/AvatarBadge"),0,0,166,166);
        badge.transform.SetAsLastSibling();
        root.GetComponent<Button>().targetGraphic=badge;
        PlacePortraitArt(portraits[0], 8, 19, 106, 95);
        PlacePortraitArt(portraitHats[0], 22, 5, 78, 60);
        GameplayHudStyle.Place((RectTransform)menu.Find("PlayerProfileCaption"), 41, 211, 200, 30);
    }

    private static void PlacePortraitArt(Image image,float x,float y,float width,float height)
    {
        var rect=image.rectTransform;
        GameplayHudStyle.Place(rect,x,y,width,height);
        rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition+=new Vector2(width*.5f,-height*.5f);
    }
    private void Build()
    {
        var root=Rect(transform,"PlayerProfile",0,0,1920,1080);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;panel=root.gameObject;
        var canvas=panel.GetComponent<Canvas>();if(!canvas)canvas=panel.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=520;if(!panel.GetComponent<GraphicRaycaster>())panel.AddComponent<GraphicRaycaster>();var backdrop=panel.GetComponent<Image>();if(!backdrop)backdrop=panel.AddComponent<Image>();backdrop.color=new Color(.15f,.08f,.03f,.7f);
        page=Rect(root,"Page",0,0,1920,1080);page.anchorMin=page.anchorMax=page.pivot=new Vector2(.5f,.5f);page.anchoredPosition=Vector2.zero;
        var surface=Frame(page,"Surface",85,115,1750,860,new Color(1,.95f,.84f));CafeFrameLayouts.Panel(surface,"Avatar");
        var header=Art(page,"Header",progress.panel.transform.Find("Popup/Header").GetComponent<Image>().sprite,550,75,820,100);header.preserveAspect=false;header.type=Image.Type.Sliced;header.pixelsPerUnitMultiplier=header.sprite.rect.height/100;
        Text(page,"Title","MY CHARACTER",580,91,760,65,42).color=Color.white;
        Button(page,"CLOSE",1600,150,180,Close);
        Frame(page,"PreviewArea",125,227,565,633,new Color(.94f,.87f,.73f));
        title=Text(page,"CharacterName","",150,243,515,50,34);
        preview=Art(page,"CharacterPreview",null,185,303,445,445);
        previewHat=Art(page,"HeadwearPreview",null,282,283,250,170);
        previewLock=Art(page,"CharacterLock",Resources.Load<Sprite>("PlayerAvatars/Lock"),335,455,145,145);
        condition=Text(page,"UnlockCondition","",145,767,525,70,24);
        charactersTab=Button(page,"CHARACTERS",750,245,455,()=>Switch(0));hatsTab=Button(page,"HEADWEAR",1240,245,455,()=>Switch(1));
        Button(page,"REMOVE HAT",150,880,250,()=>{draftHat="";Refresh();});
        save=Button(page,"SAVE & USE",430,880,310,Save,true);
        notice=Text(page,"Notice","",790,865,890,45,22);
        previous=Button(page,"PREV",850,913,200,()=>{hatPage--;Refresh();});
        next=Button(page,"NEXT",1395,913,200,()=>{hatPage++;Refresh();});pageLabel=Text(page,"PageLabel","",1100,916,250,50,24);
        hats.Clear();var config=EconomyStore.Config;if(config) foreach(var item in config.decorations) if(item!=null && item.slot==DecorationSlot.Hats && item.sprite) hats.Add(item);
        foreach(var item in AvatarFashionRewards.Items) if(item.sprite) hats.Add(item);
        panel.SetActive(false);
    }
    public void Open()
    {
        if(progress.ui.settingsPanel.gameObject.activeSelf || IsOpen || !game.OpenPlayerProfile()) return;
        if(!panel) Build();
        var p=EconomyStore.Inventory.avatar;draftCharacter=p.selected;draftHat=p.hat;tab=hatPage=0;
        oldInput=progress.menuInput.interactable;oldRaycasts=progress.menuInput.blocksRaycasts;blocked=true;progress.menuInput.interactable=progress.menuInput.blocksRaycasts=false;
        panel.SetActive(true);panel.transform.SetAsLastSibling();Refresh();
    }
    public void Close()
    {
        if(!IsOpen) return;panel.SetActive(false);RestoreInput();game.ClosePlayerProfile();
    }
    private void RestoreInput()
    {
        if(!blocked) return;blocked=false;progress.menuInput.interactable=oldInput;progress.menuInput.blocksRaycasts=oldRaycasts;
    }
    private void Switch(int value) {tab=value;hatPage=0;Refresh();}
    private void Save()
    {
        if(!IsOpen) return;
        if(PlayerAvatarStore.Save(draftCharacter,draftHat)) Close();else notice.text="Unlock this character or headwear first.";
    }
    private void InventoryChanged() {RefreshPortraits();if(IsOpen) Refresh();}
    private void OnState(GameState state) {if(state!=GameState.PlayerProfile && IsOpen) {panel.SetActive(false);RestoreInput();}}
    private void Refresh() => RefreshView(EconomyStore.Inventory);
    public void PrepareProfileSceneView()
    {
        progress=GetComponent<ProgressUI>();Build();draftCharacter="fox";draftHat="";tab=hatPage=0;
        var sample=new PlayerInventory {avatar=new PlayerAvatarProfile()};
        tab=1;RefreshView(sample);tab=0;RefreshView(sample);
        CafeFrameLayouts.Avatar(page);
        panel.SetActive(false);
    }
    private void RefreshView(PlayerInventory p)
    {
        int index=PlayerAvatarStore.Index(draftCharacter);bool unlocked=p.avatar.unlocked.Contains(draftCharacter);
        title.text=PlayerAvatarStore.Names[index];preview.sprite=Body(draftCharacter);
        preview.color=unlocked ? Color.white : Color.black;previewLock.enabled=!unlocked;
        var hat=EconomyStore.FindDecoration(draftHat);previewHat.sprite=hat==null ? null : hat.sprite;previewHat.enabled=unlocked && hat!=null && hat.sprite;AvatarFashionRewards.Style(previewHat);
        condition.text=unlocked ? "UNLOCKED" : PlayerAvatarStore.Condition(draftCharacter);
        save.interactable=unlocked && PlayerAvatarStore.CanWear(p,draftHat);notice.text="Preview changes, then SAVE & USE.";
        charactersTab.GetComponent<Image>().sprite=Resources.Load<Sprite>("ActivityIcons/"+(tab==0 ? "Primary" : "Secondary"));
        hatsTab.GetComponent<Image>().sprite=Resources.Load<Sprite>("ActivityIcons/"+(tab==1 ? "Primary" : "Secondary"));
        TabButtonStyle.ApplyAll(transform);
        foreach(Transform card in page) if(card.name.StartsWith("Character_") || card.name.StartsWith("Headwear_"))card.gameObject.SetActive(false);cards.Clear();
        previous.gameObject.SetActive(tab==1);next.gameObject.SetActive(tab==1);pageLabel.gameObject.SetActive(tab==1);
        if(tab==0)
        {
            for(int i=0;i<PlayerAvatarStore.Ids.Length;i++)
            {
                string id=PlayerAvatarStore.Ids[i];bool owned=p.avatar.unlocked.Contains(id);
                int row=i/3,rowCount=Mathf.Min(3,PlayerAvatarStore.Ids.Length-row*3);
                float x=760+(i%3)*315;
                var card=Frame(page,"Character_"+id,x,340+row*240,285,225,id==draftCharacter ? new Color(1,.9f,.64f) : new Color(1,.98f,.93f));cards.Add(card);
                Text(card,"Name",PlayerAvatarStore.Names[i],10,6,265,32,22);
                var body=Art(card,"FullBody",Body(id),55,42,175,100);body.color=owned ? Color.white : Color.black;
                var locked=Art(card,"Lock",Resources.Load<Sprite>("PlayerAvatars/Lock"),115,70,56,56);locked.enabled=!owned;
                Text(card,"Unlock",owned ? "UNLOCKED" : i==1 ? "DAY 7 CHECK-IN" : i==2 ? "WELCOME EVENT: 210 PTS" : i==3 ? "30 TOTAL MEDALS" : "COMPLETE LEVELS 1-20",14,145,257,24,16);
                Compact(Button(card,owned ? id==draftCharacter ? "SELECTED" : "PREVIEW" : "LOCKED",20,176,245,()=>{draftCharacter=id;Refresh();},owned && id==draftCharacter));
            }
            UnifiedCafeLayouts.Avatar(page);
            return;
        }
        int count=hats.Count+1,pages=Mathf.Max(1,Mathf.CeilToInt(count/6f));hatPage=Mathf.Clamp(hatPage,0,pages-1);pageLabel.text=(hatPage+1)+" / "+pages;previous.interactable=hatPage>0;next.interactable=hatPage+1<pages;
        for(int i=0;i<6 && hatPage*6+i<count;i++)
        {
            int entry=hatPage*6+i;var item=entry==0 ? null : hats[entry-1];string id=item==null ? "" : item.id;bool owned=PlayerAvatarStore.CanWear(p,id);
            var card=Frame(page,"Headwear_"+entry,760+(i%3)*315,340+(i/3)*240,285,225,id==draftHat ? new Color(1,.9f,.64f) : new Color(1,.98f,.93f));cards.Add(card);
            if(item!=null) Art(card,"Hat",item.sprite,65,12,156,110);else Text(card,"None","NO HAT",15,30,280,70,30);
            Text(card,"Name",item==null ? "Original look" : item.displayName,12,140,261,28,20);
            bool rewardOnly=item!=null && item.rewardOnly;
            Compact(Button(card,!owned ? rewardOnly ? "REWARD" : "BUY IN SHOP" : id==draftHat ? "EQUIPPED" : "TRY ON",20,176,245,()=>{if(owned) {draftHat=id;Refresh();}else notice.text=rewardOnly ? RoomProgress.SourceForDecoration(id) : "Buy this headwear in Shop > Accessories first.";},owned && id==draftHat));
        }
    
        UnifiedCafeLayouts.Avatar(page);
    }
    private void OnDestroy()
    {
        EconomyStore.InventoryChanged-=InventoryChanged;if(game) game.StateChanged-=OnState;
        if(blocked && progress && progress.menuInput) RestoreInput();
        if(Application.isPlaying) foreach(var sprite in faces.Values) if(sprite) Destroy(sprite);
    }
}
