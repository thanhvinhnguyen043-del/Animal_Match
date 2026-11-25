using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Coordinates follow the approved 1600 x 900 previews; the game canvas is 1920 x 1080.
public static class UnifiedCafeLayouts
{
    public static bool Owns(Transform node)
    {
        for(var p=node;p;p=p.parent)
            if(p.Find("UnifiedShopTray") || p.Find("UnifiedAchievementTray") || p.Find("UnifiedRoomTray") || p.Find("UnifiedWardrobeTray") || p.Find("UnifiedStudioTray"))return true;
        return false;
    }
    public const float AchievementRowHeight=101f;
    private static readonly Color Cocoa=new Color(.41f,.24f,.14f);
    private static readonly Color Muted=new Color(.61f,.46f,.29f);
    public static void Place(Transform node,float x,float y,float w,float h)
    {if(node) GameplayHudStyle.Place((RectTransform)node,x*1.2f,y*1.2f,w*1.2f,h*1.2f);}
    private static RectTransform Node(Transform parent,string name,float x,float y,float w,float h)
    {
        var old=parent.Find(name);
        var rect=old ? (RectTransform)old : new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        if(!old)rect.SetParent(parent,false);
        Place(rect,x,y,w,h);return rect;
    }
    private static void Hide(Transform parent,params string[] names)
    {foreach(string name in names){var node=parent.Find(name);if(node)node.gameObject.SetActive(false);}}
    private static void Plain(Transform root)
    {
        foreach(var image in root.GetComponents<Image>())image.enabled=false;
        foreach(var outline in root.GetComponents<Outline>())outline.enabled=false;
        Hide(root,"CardFrame","WarmTrim","CreamInset","ThemedFrame","CafePanelPNG");
    }
    private static Image Art(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var node=Node(parent,name,x,y,w,h);var image=node.GetComponent<Image>();
        if(!image)image=node.gameObject.AddComponent<Image>();
        image.enabled=true;image.sprite=sprite;image.color=Color.white;
        image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private static TextMeshProUGUI Label(Transform parent,string name,string value,float x,float y,float w,float h,float size=20)
    {
        var node=Node(parent,name,x,y,w,h);var label=node.GetComponent<TextMeshProUGUI>();
        if(!label)label=node.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=GameTypography.Current.font;label.text=value;label.color=Cocoa;label.raycastTarget=false;
        label.fontSize=label.fontSizeMax=size*1.2f;label.fontSizeMin=size*1.05f;label.enableAutoSizing=true;
        label.alignment=TextAlignmentOptions.MidlineLeft;return label;
    }
    private static void Text(Transform node,float x,float y,float w,float h,float size,bool center=false)
    {
        if(!node)return;Place(node,x,y,w,h);var label=node.GetComponent<TextMeshProUGUI>();if(!label)return;
        label.fontSize=label.fontSizeMax=size*1.2f;label.fontSizeMin=size*.95f;label.enableAutoSizing=true;
        label.alignment=center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
    }
    private static Image Tray(Transform parent,string name,float x,float y,float w,float h)
    {
        var image=RewardFrameArt.Create(parent,name,"Common",x*1.2f,y*1.2f,w*1.2f,h*1.2f);
        image.pixelsPerUnitMultiplier=5.5f;image.transform.SetSiblingIndex(2);return image;
    }
    private static void Line(Transform root,string name,float x,float y,float w,float h)
    {
        var image=Art(root,name,null,x,y,w,h);image.color=new Color(.84f,.71f,.52f);image.preserveAspect=false;
    }
    private static void Button(Transform node,float x,float y,float w,string label=null,bool? green=null)
    {
        if(!node)return;Place(node,x,y,w,w/4);var image=node.GetComponent<Image>();
        if(image){image.enabled=true;image.type=Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;
            bool selected=green ?? (image.sprite && (image.sprite.name.Contains("Primary") || image.sprite.name.Contains("Green")));
            image.sprite=Resources.Load<Sprite>("TabButtons/"+(selected ? "Green" : "Blue"));}
        var text=node.GetComponentInChildren<TextMeshProUGUI>(true);
        if(text){text.gameObject.SetActive(true);if(label!=null)text.text=label;var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8,3);r.offsetMax=new Vector2(-8,-3);text.alignment=TextAlignmentOptions.Center;text.fontSizeMax=24;text.fontSizeMin=17;}
    }
    private static void Back(Button button)
    {
        if(!button)return;Place(button.transform,28,22,78,78);var image=button.GetComponent<Image>();
        image.sprite=Resources.Load<Sprite>("MenuIcons/Back");image.type=Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;
        button.targetGraphic=image;foreach(var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))text.gameObject.SetActive(false);
    }
    private static void Frame(Transform node,string style)
    {
        if(!node)return;Place(node,25,96,1550,775);var image=CafeFrameLayouts.Panel(node,style);
        image.type=Image.Type.Simple;image.preserveAspect=true;
    }
    private static void Header(Transform root,string header,string title,float width)
    {
        var image=root.Find(header);Place(image,800-width/2,130-width/6,width,width/3);
        if(image){var art=image.GetComponent<Image>();art.type=Image.Type.Simple;art.preserveAspect=true;}
        Text(root.Find(title),800-width*.35f,103,width*.7f,48,35,true);
    }
    public static void Shop(ShopUI shop)
    {
        var root=shop.panel.transform;Frame(root.Find("ShopWorkspace"),"Shop");
        Header(root,"Panel__Header","Text__SHOP",600);Back(root.Find("Icon__Back").GetComponent<Button>());
        Tray(root,"UnifiedShopTray",139,325,1322,410);Line(root,"ShopProductDivider",800,357,2,347);
        root.Find("ShopProductDivider").gameObject.SetActive(shop.boosterPage.activeSelf);
        var tabs=new[]{shop.boosterTab.transform,shop.backgroundTab.transform,shop.decorationTab.transform,root.Find("OpenStudio")};
        for(int i=0;i<tabs.Length;i++)Button(tabs[i],177+i*317,267,295,null,i==3 ? (bool?)true : null);
        Text(shop.shopStock.transform,400,230,800,32,22,true);
        foreach(var label in shop.coinLabels)if(label && label.transform.IsChildOf(root))label.gameObject.SetActive(false);
        Hide(root,"Icon__Coin","Panel__Currency","Plate__Status");
        foreach(var img in root.GetComponentsInChildren<Image>(true)) if(img.sprite && img.sprite.name=="Panel__Currency")img.gameObject.SetActive(false);
        if(shop.productScroll){Place(shop.productScroll.viewport,0,0,1600,900);Place(shop.productScroll.content,0,0,1600,900);shop.productScroll.vertical=false;}
        Place(shop.boosterPage.transform,0,0,1600,900);
        foreach(Transform child in shop.boosterPage.transform)if(child.name=="Panel__ItemCard")child.gameObject.SetActive(false);
        var boosts=shop.boosterPage.transform;
        Text(boosts.Find("Text__HINT"),300,346,350,42,31,true);Text(boosts.Find("Text__SHUFFLE"),950,346,350,42,31,true);
        Place(boosts.Find("Icon__Hint"),385,399,180,180);Place(boosts.Find("Icon__Shuffle"),1035,399,180,180);
        Text(boosts.Find("Text__Add_1_hint"),275,582,400,35,24,true);Text(boosts.Find("Text__Add_1_shuffle"),925,582,400,35,24,true);
        Button(shop.hintBuyButton.transform,328,642,294,null,true);Button(shop.shuffleBuyButton.transform,978,642,294,null,true);
        Text(shop.hintCount.transform,275,618,400,24,18,true);Text(shop.shuffleCount.transform,925,618,400,24,18,true);
        Button(shop.previousPageButton.transform,470,789,194);Button(shop.nextPageButton.transform,937,789,194);
        Text(shop.backgroundPageText.transform,720,796,160,40,24,true);Text(shop.statusText.transform,300,746,1000,32,19,true);
        // All shop pages keep their existing purchases and thumbnail previews.
        for(int i=0;i<shop.backgroundCards.Length;i++)
        {
            var card=shop.backgroundCards[i];var box=card.button.transform.parent;Place(box,175+i%2*640,354+(i%4)/2*160,610,148);
            Plain(box);ProductFrame(box,610,148);Place(card.preview.transform,12,17,156,106);Text(box.Find("Name"),190,12,390,32,24);
            Text(card.description.transform,190,48,390,32,18);Button(card.button.transform,190,87,240);
        }
        for(int i=0;i<shop.decorations.shopCards.Length;i++)
        {
            var card=shop.decorations.shopCards[i];var box=card.button.transform.parent;Place(box,175+i%3*422,347+i/3*186,397,173);Plain(box);ProductFrame(box,397,173);
            Place(card.preview.transform,15,24,95,118);Text(card.title.transform,122,15,257,30,20);Text(card.detail.transform,122,48,257,38,17);Button(card.button.transform,122,99,240);
        }
    }
    private static void ProductFrame(Transform card,float width,float height)
    {
        var frame=RewardFrameArt.Create(card,"UnifiedProductFrame","Common",0,0,width*1.2f,height*1.2f);
        frame.pixelsPerUnitMultiplier=7;frame.transform.SetAsFirstSibling();
    }

    public static void Studio(DecorationStudio studio)
    {
        if(!studio || !studio.room)return;var root=studio.panel.transform;
        Hide(root,"RoomWorkspace","InventoryWorkspace","ActionShelf","FixedAreaHelp");studio.coins.gameObject.SetActive(false);
        var bg=root.GetComponent<Image>();if(bg){bg.color=new Color(.97f,.91f,.79f);bg.raycastTarget=true;}
        Place(root.Find("Header"),545,-8,510,170);var header=root.Find("Header").GetComponent<Image>();header.type=Image.Type.Simple;header.preserveAspect=true;
        Text(root.Find("Title"),580,64,440,48,28,true);root.Find("Title").GetComponent<TextMeshProUGUI>().color=Color.white;Back(root.Find("Close").GetComponent<Button>());
        Label(root,"StudioRoomCaption","CURRENT ROOM",61,114,450,24,15).color=Muted;
        Text(studio.roomName.transform,61,140,650,34,28);
        Button(root.Find("ChangeBackground"),1236,40,320);
        Tray(root,"UnifiedStudioTray",44,254,994,580);Tray(root,"StudioInventoryFrame",1071,156,485,678);
        Tray(root,"StudioSlotFrame",44,186,994,54);Tray(root,"StudioActionFrame",25,845,1550,53);
        Place(studio.room.transform,70,279,942,529.875f);studio.room.preserveAspect=true;
        var slots=root.Find("SlotButtons");Place(slots,44,186,994,54);
        for(int i=0;i<studio.slotButtons.Length;i++){Button(studio.slotButtons[i].transform,15+i*121,12,117);var caption=studio.slotButtons[i].GetComponentInChildren<TextMeshProUGUI>();caption.fontSizeMax=18.6f;caption.fontSizeMin=16;caption.enableAutoSizing=true;}
        Text(root.Find("InventoryTitle"),1100,174,426,40,27,true);Text(studio.pageLabel.transform,1100,214,426,31,17,true);
        var viewport=Node(root,"StudioOwnedList",1097,264,433,156);var scroll=viewport.GetComponent<ScrollRect>();
        if(!scroll){var image=viewport.gameObject.AddComponent<Image>();image.color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();scroll=viewport.gameObject.AddComponent<ScrollRect>();}
        var content=Node(viewport,"Items",0,0,433,studio.inventoryCards.Length*70);scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=38;
        for(int i=0;i<studio.inventoryCards.Length;i++)
        {
            var c=studio.inventoryCards[i];var card=c.preview.transform.parent;card.SetParent(content,false);Place(card,0,i*70,425,65);Plain(card);
            Place(c.preview.transform,8,3,60,59);Text(c.title.transform,83,5,326,27,19);Text(c.detail.transform,83,32,326,28,15);
            Place(c.button.transform,0,0,425,65);c.button.GetComponent<Image>().color=Color.clear;c.action.gameObject.SetActive(false);
            Line(card,"StudioItemDivider",80,64,328,1);
        }
        Label(root,"StudioPreviewHeading","PREVIEW",1100,432,426,30,18).alignment=TextAlignmentOptions.Center;
        Hide(root,"StudioPreviewGlow");var disk=Node(root,"StudioPreviewDisk",1193,456,240,240);var glow=disk.GetComponent<RoundedImage>();if(!glow)glow=disk.gameObject.AddComponent<RoundedImage>();glow.cornerRadius=144;glow.color=new Color(1,.91f,.71f,.4f);glow.raycastTarget=false;
        var portrait=Art(root,"StudioItemPreview",null,1224,469,177,211);portrait.preserveAspect=true;portrait.transform.SetAsLastSibling();
        Text(studio.selection.transform,1098,683,430,32,22,true);
        Label(root,"StudioPreviewHint","One item per area. Mix any skin.",1098,718,430,26,16).alignment=TextAlignmentOptions.Center;
        var equip=Node(root,"StudioEquip",1169,751,286,71.5f);if(!equip.GetComponent<Image>())equip.gameObject.AddComponent<Image>();if(!equip.GetComponent<Button>())equip.gameObject.AddComponent<Button>();
        Label(equip,"Label","EQUIP TO AREA",0,0,286,71.5f,21);Button(equip,1169,751,286,null,true);equip.GetComponent<Button>().targetGraphic=equip.GetComponent<Image>();
        Button(studio.remove.transform,822,842,210,"REMOVE AREA");Button(studio.clear.transform,1050,842,210,"CLEAR ROOM");Button(studio.save.transform,1298,836,240,"SAVE ROOM",true);
        Text(studio.status.transform,60,855,730,34,17);
        Button(studio.previous.transform,1100,247,90);Button(studio.next.transform,1438,247,90);
        // The dedicated list scrolls; page controls remain available when more than six items are owned.
    }
    public static void StudioItem(DecorationStudio studio,DecorationItem item,bool equipped)
    {
        var root=studio.panel.transform;var image=root.Find("StudioItemPreview").GetComponent<Image>();image.sprite=item?.sprite;image.enabled=item!=null && item.sprite;
        root.Find("StudioPreviewDisk").gameObject.SetActive(item!=null);
        var label=root.Find("StudioEquip").GetComponentInChildren<TextMeshProUGUI>();label.text=equipped?"REMOVE FROM AREA":"EQUIP TO AREA";label.color=Color.white;
        root.Find("StudioEquip").GetComponent<Button>().interactable=item!=null && item.sprite;
        root.Find("StudioPreviewHint").GetComponent<TextMeshProUGUI>().text=item==null?"Choose an owned item above.":"One item per area. Mix any skin.";
    }

    public static void Achievements(ProgressUI p)
    {
        if(!p.achievementScroll)return;var root=p.panel.transform.Find("Popup");Place(root,0,0,1600,900);
        Frame(root.Find("Frame"),"Achievements");Header(root,"Header","Title",740);Back(p.closeButton);
        Tray(root,"UnifiedAchievementTray",139,325,1322,410);
        Text(p.summary.transform,300,229,1000,34,22,true);
        string[] captions={"ALL","MEDALS","COMBOS","PLAY","COLLECTION","READY"};
        for(int i=0;i<p.filters.Length;i++)Button(p.filters[i].transform,159+i*214,267,205,captions[i]);
        Place(p.achievementScroll.viewport,173,344,1257,303);p.achievementScroll.horizontal=false;p.achievementScroll.vertical=true;
        var content=p.achievementScroll.content;var position=content.anchoredPosition;Place(content,0,0,1257,p.cards.Length*101);content.anchoredPosition=new Vector2(0,position.y);
        for(int i=0;i<p.cards.Length;i++)
        {
            var c=p.cards[i];Place(c.root.transform,0,i*101,1240,101);Plain(c.root.transform);
            Place(c.icon.transform,6,6,83,88);Text(c.title.transform,117,3,510,32,24);Text(c.description.transform,117,33,510,32,19);
            Text(c.progress.transform,557,4,120,31,21,true);Text(c.reward.transform,745,20,230,52,18);
            var rewardFrame=c.root.transform.Find(c.reward.name+"RewardFramePNG");if(rewardFrame)rewardFrame.gameObject.SetActive(false);
            Place(c.fill.transform.parent,117,77,559,12);Button(c.button.transform,985,26,231);
            Line(c.root.transform,"UnifiedRowDivider",0,100,1240,1);
        }
        Art(root,"BonusChestArt",Resources.Load<Sprite>("ActivityIcons/Chest"),179,659,78,66);
        Text(p.milestoneLabel.transform,0,0,525,26,21);Text(p.milestoneProgress.transform,0,27,525,24,17);
        Place(p.milestoneButton.transform,278,671,1108,53);var old=p.milestoneButton.GetComponent<Image>();if(old)old.color=Color.clear;
        var action=Art(p.milestoneButton.transform,"UnifiedChestAction",Resources.Load<Sprite>("TabButtons/Blue"),880,0,231,58);
        var label=Label(p.milestoneButton.transform,"UnifiedChestActionLabel","BONUS CHEST",880,0,231,58,20);label.alignment=TextAlignmentOptions.Center;
        action.raycastTarget=false;
        Text(p.notice.transform,330,751,940,35,19,true);
        p.previousButton.gameObject.SetActive(false);p.nextButton.gameObject.SetActive(false);p.pageLabel.gameObject.SetActive(false);
    }
    public static void Rooms(RoomRewardsUI rewards)
    {
        var root=rewards.panel.transform;Frame(root.Find("RoomRewardSurface"),"RoomRewards");Header(root,"Header","Title",850);
        Back(root.Find("Close").GetComponent<Button>());Tray(root,"UnifiedRoomTray",139,325,1322,410);
        Text(rewards.summary.transform,200,229,1200,34,22,true);
        for(int i=0;i<rewards.filters.Length;i++)Button(rewards.filters[i].transform,177+i*317,267,295,i==1?"MEDALS":null);
        Line(root,"RoomFeatureDivider",640,357,2,347);
        for(int i=0;i<rewards.cards.Length;i++)
        {
            var c=rewards.cards[i];Place(c.root.transform,675,351+i*89,755,89);Plain(c.root.transform);
            Place(c.preview.transform,7,12,108,62);Text(c.title.transform,137,7,370,29,24);Text(c.detail.transform,137,35,390,26,17);
            Text(c.progress.transform,137,62,390,22,15);Button(c.button.transform,568,20,165);
            if(i>0)Line(c.root.transform,"UnifiedRowDivider",7,0,729,1);
        }
        Text(rewards.notice.transform,175,739,1250,33,18,true);Button(rewards.previous.transform,470,789,194);Button(rewards.next.transform,937,789,194);Text(rewards.pageLabel.transform,720,794,160,40,24,true);
        RoomFeature(rewards);
    }
    public static void RoomFeature(RoomRewardsUI rewards)
    {
        var root=rewards.panel.transform;int index=-1;
        for(int i=0;i<rewards.cards.Length;i++)if(rewards.cards[i].root.activeSelf){if(index<0)index=i;if(!rewards.cards[i].button.interactable){index=i;break;}}
        if(index<0)return;var c=rewards.cards[index];
        Label(root,"RoomFeatureHeading","REWARD PREVIEW",181,349,411,34,20).color=new Color(.45f,.57f,.35f);
        var photo=Art(root,"RoomFeaturePhoto",c.preview.sprite,181,388,411,200);photo.enabled=c.preview.sprite;
        Label(root,"RoomFeatureTitle",c.title.text,181,594,411,42,29);
        Label(root,"RoomFeatureDetail",c.detail.text,181,638,411,52,20);
        Label(root,"RoomFeatureProgress",c.progress.text,181,692,411,27,18).color=Muted;
        int selected=index;var button=photo.GetComponent<Button>();if(!button)button=photo.gameObject.AddComponent<Button>();photo.raycastTarget=true;button.targetGraphic=photo;button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>rewards.PreviewCard(selected));
    }
    public static void Avatar(Transform page)
    {
        Frame(page.Find("Surface"),"Avatar");Header(page,"Header","Title",850);Back(page.Find("CLOSE").GetComponent<Button>());
        Hide(page,"PreviewArea");Tray(page,"UnifiedWardrobeTray",139,325,1322,410);Line(page,"WardrobeDivider",623,357,2,347);
        Label(page,"WardrobeIntro","CHOOSE YOUR COMPANION. MAKE THEIR LOOK YOUR OWN.",300,229,1000,34,22).alignment=TextAlignmentOptions.Center;
        Text(page.Find("CharacterName"),205,343,355,44,30,true);Text(page.Find("UnlockCondition"),188,383,390,27,18,true);
        Place(page.Find("CharacterPreview"),239,403,290,277);Place(page.Find("HeadwearPreview"),309,395,157,106);Place(page.Find("CharacterLock"),326,478,110,110);
        var theme=EconomyStore.FindBackground(EconomyStore.Inventory.selectedBackground);
        var pedestal=Art(page,"WardrobePedestal",theme!=null && theme.companionPedestal ? theme.companionPedestal : Resources.Load<Sprite>("CompanionArt/Pedestal"),207,602,351,131);
        pedestal.transform.SetAsLastSibling();page.Find("CharacterPreview").SetAsLastSibling();page.Find("HeadwearPreview").SetAsLastSibling();page.Find("CharacterLock").SetAsLastSibling();
        Button(page.Find("CHARACTERS"),686,267,310);Button(page.Find("HEADWEAR"),1079,267,310);
        foreach(Transform card in page)
        {
            bool character=card.name.StartsWith("Character_");bool hat=card.name.StartsWith("Headwear_");if(!character&&!hat)continue;
            Plain(card);int index=0;
            if(character)index=PlayerAvatarStore.Index(card.name.Substring(10));else int.TryParse(card.name.Substring(9),out index);
            if(character)
            {
                Place(card,656+index*155,384,145,247);Text(card.Find("Name"),0,11,135,30,18,true);Place(card.Find("FullBody"),4,53,128,134);Place(card.Find("Lock"),47,139,41,47);
                Text(card.Find("Unlock"),0,199,135,49,14,true);
                var action=card.Find("Action");if(action){var label=action.GetComponentInChildren<TextMeshProUGUI>();bool selected=label && label.text=="SELECTED";
                    if(selected)Button(action,-1,198,138,null,true);
                    else{Place(action,0,0,145,247);action.GetComponent<Image>().color=Color.clear;if(label)label.gameObject.SetActive(false);}}
            }
            else
            {
                int slot=index%6;Place(card,656+slot%3*254,352+slot/3*148,240,145);
                Place(card.Find("Hat"),72,0,96,76);Text(card.Find("None"),0,15,240,60,25,true);Text(card.Find("Name"),0,75,240,25,18,true);Button(card.Find("Action"),18,103,204);
            }
        }
        bool hatsVisible=page.Find("Headwear_0") && page.Find("Headwear_0").gameObject.activeSelf;
        Text(page.Find("Notice"),hatsVisible?330:664,hatsVisible?740:655,hatsVisible?1000:747,hatsVisible?32:58,19);Button(page.Find("REMOVE HAT"),747,782,255);Button(page.Find("SAVE & USE"),1094,779,293);
        Button(page.Find("PREV"),698,675,155);Button(page.Find("NEXT"),1210,675,155);Text(page.Find("PageLabel"),920,681,200,32,19,true);
    }
}
