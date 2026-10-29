using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RoomProgressBuilder
{
    private static readonly Color Ink=new Color(.42f,.235f,.13f);
    private static RectTransform Place(Transform target,float x,float y,float w,float h)
    {
        var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return Place(rect,x,y,w,h);}
    private static void Remove(Transform parent,string name) {var old=parent.Find(name);if(old) Object.DestroyImmediate(old.gameObject);}
    private static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/"+name+".png");
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;}
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,bool heading=false)
    {
        var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.font=GameTypography.Current.font;
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao "+(heading ? "Heading" : "Body")+".mat");
        label.text=value;label.color=heading ? new Color(1,.99f,.92f) : Ink;label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-3;
        label.enableAutoSizing=true;label.enableWordWrapping=true;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,UnityAction action=null,bool primary=false)
    {
        var image=Image(parent,name,Art(primary ? "Button__Primary" : "Button__Secondary"),x,y,w,h);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action);
        var label=Text(image.transform,"Label",caption,12,6,w-24,h-12,28,true);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
        label.rectTransform.offsetMin=new Vector2(12,6);label.rectTransform.offsetMax=new Vector2(-12,-6);
        image.gameObject.AddComponent<UIButtonClickSound>().clickSound=Object.FindObjectOfType<GridManager>().sfxClick;return button;
    }
    private static void Modal(RectTransform root,int order)
    {
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
        var shade=root.gameObject.AddComponent<Image>();shade.color=new Color(.15f,.075f,.045f,.88f);shade.raycastTarget=true;
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=order;root.gameObject.AddComponent<GraphicRaycaster>();
    }
    private static void Round(RectTransform root)
    {
        var panel=root.gameObject.AddComponent<RoundedImage>();panel.cornerRadius=24;panel.color=new Color(1,.96f,.86f);panel.raycastTarget=false;
        var edge=root.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.77f,.52f,.27f);edge.effectDistance=new Vector2(2,-2);
    }
    private static Sprite Import(string path,int maximum)
    {
        if(!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=maximum;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void Configure()
    {
        var config=EconomyStore.Config;var rooms=config.backgrounds.ToList();
        foreach(var entry in new[]{new[]{"moonlight","Moonlight Cafe","MoonlightCafe"},new[]{"bakery","Sweet Bakery Cafe","SweetBakeryCafe"}})
        {
            var room=rooms.Find(r=>r.id==entry[0]);if(room==null) {room=new ShopBackground {id=entry[0],displayName=entry[1],price=2400};rooms.Add(room);}
            var sprite=Import("Assets/Art/BackgroundBasesExpanded/"+entry[2]+".png",2048);if(sprite) room.sprite=room.baseSprite=sprite;
        }
        config.backgrounds=rooms.ToArray();var items=config.decorations.ToList();
        AddItem(items,"moonlight_moonflowerplant","Moonflower Planter","Moonlight",DecorationSlot.Plants,"MoonlightCafe/MoonflowerPlant",360,470,0,true);
        AddItem(items,"moonlight_starlantern","Moon Lantern","Moonlight",DecorationSlot.Hanging,"MoonlightCafe/StarLantern",210,400,0,true);
        AddItem(items,"moonlight_mooncatchair","Moon Cat Chair","Moonlight",DecorationSlot.Chair,"MoonlightCafe/MoonCatChair",230,260,0,true);
        AddItem(items,"moonlight_telescopetable","Telescope Table","Moonlight",DecorationSlot.SideTable,"MoonlightCafe/TelescopeTable",240,260,0,true);
        AddItem(items,"bakery_strawberryplanter","Strawberry Planter","Bakery",DecorationSlot.Plants,"SweetBakeryCafe/StrawberryPlanter",350,450,450,false);
        AddItem(items,"bakery_macarongarland","Macaron Garland","Bakery",DecorationSlot.Hanging,"SweetBakeryCafe/MacaronGarland",620,260,350,false);
        AddItem(items,"bakery_pastrydisplay","Cat Bun Display","Bakery",DecorationSlot.Counter,"SweetBakeryCafe/PastryDisplay",300,205,450,false);
        AddItem(items,"bakery_pawcookierug","Paw Cookie Rug","Bakery",DecorationSlot.Floor,"SweetBakeryCafe/PawCookieRug",300,170,350,false);
        AddItem(items,"keepsake_firstset","Collector Cat","Keepsakes",DecorationSlot.Wall,"Keepsakes/CollectorCat",160,200,0,true);
        AddItem(items,"keepsake_threesets","Cafe Curator","Keepsakes",DecorationSlot.Wall,"Keepsakes/CafeCurator",180,210,0,true);
        AddItem(items,"keepsake_sevensets","Master Collector","Keepsakes",DecorationSlot.Wall,"Keepsakes/MasterCollector",180,220,0,true);
        ConfigureExpandedArt(rooms,items);
        config.backgrounds=rooms.ToArray();
        config.decorations=items.ToArray();
        var rewards=(config.roomRewards ?? new RoomRewardDefinition[0]).ToList();
        AddReward(rewards,"chapter-5","Japan Cafe",RoomRewardGoal.Chapters,5,"japan",null);
        AddReward(rewards,"chapter-10","Moonlight Cafe",RoomRewardGoal.Chapters,10,"moonlight",null);
        AddReward(rewards,"chapter-15","Winter Cafe",RoomRewardGoal.Chapters,15,"winter",null);
        AddReward(rewards,"chapter-20","Jungle Cafe",RoomRewardGoal.Chapters,20,"jungle",null);
        AddReward(rewards,"room-stars-10","Moonflower Planter",RoomRewardGoal.Stars,10,null,"moonlight_moonflowerplant");
        AddReward(rewards,"room-stars-20","Moon Lantern",RoomRewardGoal.Stars,20,null,"moonlight_starlantern");
        AddReward(rewards,"room-stars-35","Moon Cat Chair",RoomRewardGoal.Stars,35,null,"moonlight_mooncatchair");
        AddReward(rewards,"room-stars-50","Telescope Table",RoomRewardGoal.Stars,50,null,"moonlight_telescopetable");
        AddReward(rewards,"collection-1","Collector Cat",RoomRewardGoal.Collections,1,null,"keepsake_firstset");
        AddReward(rewards,"collection-3","Cafe Curator",RoomRewardGoal.Collections,3,null,"keepsake_threesets");
        AddReward(rewards,"collection-7","Master Collector",RoomRewardGoal.Collections,7,null,"keepsake_sevensets");
        config.roomRewards=rewards.ToArray();
        foreach(var reward in rewards) if(!string.IsNullOrEmpty(reward.backgroundId)) {var room=rooms.Find(r=>r.id==reward.backgroundId);if(room!=null) room.rewardOnly=true;}
        foreach(var room in rooms)
        {
            if(room.rewardOnly) room.requiredCompletedLevel=0;
            if(room.id=="halloween") room.requiredCompletedLevel=10;
            if(room.id=="beach") room.requiredCompletedLevel=15;
            if(room.id=="tet") room.requiredCompletedLevel=5;
        }
        var collections=(config.roomCollections ?? new RoomCollection[0]).ToList();
        foreach(string skin in new[]{"Cozy","Japan","Winter","Tet","Halloween","Beach","Jungle","Moonlight","Bakery","Aquarium","Greenhouse","Magic Library","Cloud"})
        {
            string id=skin.ToLowerInvariant();if(collections.Any(c=>c.id==id)) continue;
            collections.Add(new RoomCollection {id=id,displayName=skin+" Collection",itemIds=items.Where(i=>i.skin==skin).Select(i=>i.id).ToArray()});
        }
        config.roomCollections=collections.ToArray();
        var anchors=(config.decorationSlots ?? new DecorationSlotAnchor[0]).ToList();
        foreach(var room in rooms) for(int i=1;i<=DecorationSlots.Count;i++)
            if(!anchors.Any(a=>a.backgroundId==room.id && a.slot==(DecorationSlot)i)) anchors.Add(DecorationSlots.Default(room.id,(DecorationSlot)i));
        config.decorationSlots=anchors.ToArray();EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
    }
    private static void ConfigureExpandedArt(List<ShopBackground> rooms,List<DecorationItem> items)
    {
        string[][] themes={new[]{"aquarium","Aquarium Cafe","AquariumCafe"},new[]{"greenhouse","Greenhouse Cafe","GreenhouseCafe"},new[]{"magiclibrary","Magic Library Cafe","MagicLibraryCafe"},new[]{"cloud","Cloud Cafe","CloudCafe"}};
        foreach(var entry in themes)
        {
            var room=rooms.Find(r=>r.id==entry[0]);
            if(room==null) {room=new ShopBackground {id=entry[0],displayName=entry[1],price=2400};rooms.Add(room);}
            var sprite=Import("Assets/Art/BackgroundBasesExpanded/"+entry[2]+".png",2048);
            if(sprite) room.sprite=room.baseSprite=sprite;
        }
        AddItem(items,"aquarium_seaweedplanter","Seaweed Planter","Aquarium",DecorationSlot.Plants,"AquariumCafe/SeaweedPlanter",350,440,450,false);
        AddItem(items,"aquarium_jellyfishlamp","Jellyfish Lamp","Aquarium",DecorationSlot.Hanging,"AquariumCafe/JellyfishLamp",300,250,350,false);
        AddItem(items,"aquarium_seahorseplaque","Seahorse Plaque","Aquarium",DecorationSlot.Wall,"AquariumCafe/SeahorsePlaque",180,210,300,false);
        AddItem(items,"aquarium_shellsidetable","Shell Side Table","Aquarium",DecorationSlot.SideTable,"AquariumCafe/ShellSideTable",240,250,400,false);
        AddItem(items,"aquarium_pearlteapot","Pearl Teapot","Aquarium",DecorationSlot.Counter,"AquariumCafe/PearlTeapot",220,200,350,false);
        AddItem(items,"aquarium_shellrug","Shell Rug","Aquarium",DecorationSlot.Floor,"AquariumCafe/ShellRug",300,180,300,false);
        AddItem(items,"aquarium_shellchair","Shell Chair","Aquarium",DecorationSlot.Chair,"AquariumCafe/ShellChair",230,260,450,false);
        AddItem(items,"aquarium_sailorhat","Sailor Hat","Aquarium",DecorationSlot.Hats,"AquariumCafe/SailorHat",180,145,250,false);
        AddItem(items,"greenhouse_bloomplanter","Bloom Planter","Greenhouse",DecorationSlot.Plants,"GreenhouseCafe/BloomPlanter",350,440,450,false);
        AddItem(items,"greenhouse_leafmobile","Leaf Mobile","Greenhouse",DecorationSlot.Hanging,"GreenhouseCafe/LeafMobile",300,250,350,false);
        AddItem(items,"greenhouse_butterflyplaque","Butterfly Plaque","Greenhouse",DecorationSlot.Wall,"GreenhouseCafe/ButterflyPlaque",180,210,300,false);
        AddItem(items,"greenhouse_pottingtable","Potting Table","Greenhouse",DecorationSlot.SideTable,"GreenhouseCafe/PottingTable",240,250,400,false);
        AddItem(items,"greenhouse_flowerwateringcan","Flower Watering Can","Greenhouse",DecorationSlot.Counter,"GreenhouseCafe/FlowerWateringCan",220,200,350,false);
        AddItem(items,"greenhouse_leafrug","Leaf Rug","Greenhouse",DecorationSlot.Floor,"GreenhouseCafe/LeafRug",300,180,300,false);
        AddItem(items,"greenhouse_gardenchair","Garden Chair","Greenhouse",DecorationSlot.Chair,"GreenhouseCafe/GardenChair",230,260,450,false);
        AddItem(items,"greenhouse_gardenerhat","Gardener Hat","Greenhouse",DecorationSlot.Hats,"GreenhouseCafe/GardenerHat",180,145,250,false);
        AddItem(items,"magiclibrary_enchantedplanter","Enchanted Planter","Magic Library",DecorationSlot.Plants,"MagicLibraryCafe/EnchantedPlanter",350,440,450,false);
        AddItem(items,"magiclibrary_booklantern","Book Lantern","Magic Library",DecorationSlot.Hanging,"MagicLibraryCafe/BookLantern",300,250,350,false);
        AddItem(items,"magiclibrary_owlplaque","Owl Plaque","Magic Library",DecorationSlot.Wall,"MagicLibraryCafe/OwlPlaque",180,210,300,false);
        AddItem(items,"magiclibrary_spellbooktable","Spellbook Table","Magic Library",DecorationSlot.SideTable,"MagicLibraryCafe/SpellbookTable",240,250,400,false);
        AddItem(items,"magiclibrary_quillstand","Quill Stand","Magic Library",DecorationSlot.Counter,"MagicLibraryCafe/QuillStand",220,200,350,false);
        AddItem(items,"magiclibrary_spellcirclerug","Spell Circle Rug","Magic Library",DecorationSlot.Floor,"MagicLibraryCafe/SpellCircleRug",300,180,300,false);
        AddItem(items,"magiclibrary_readingchair","Reading Chair","Magic Library",DecorationSlot.Chair,"MagicLibraryCafe/ReadingChair",230,260,450,false);
        AddItem(items,"magiclibrary_wizardhat","Wizard Hat","Magic Library",DecorationSlot.Hats,"MagicLibraryCafe/WizardHat",180,145,250,false);
        AddItem(items,"cloud_cloudplanter","Cloud Planter","Cloud",DecorationSlot.Plants,"CloudCafe/CloudPlanter",350,440,450,false);
        AddItem(items,"cloud_cloudmobile","Cloud Mobile","Cloud",DecorationSlot.Hanging,"CloudCafe/CloudMobile",300,250,350,false);
        AddItem(items,"cloud_rainbowplaque","Rainbow Plaque","Cloud",DecorationSlot.Wall,"CloudCafe/RainbowPlaque",180,210,300,false);
        AddItem(items,"cloud_cloudsidetable","Cloud Side Table","Cloud",DecorationSlot.SideTable,"CloudCafe/CloudSideTable",240,250,400,false);
        AddItem(items,"cloud_cloudteaset","Cloud Tea Set","Cloud",DecorationSlot.Counter,"CloudCafe/CloudTeaSet",220,200,350,false);
        AddItem(items,"cloud_cloudrug","Cloud Rug","Cloud",DecorationSlot.Floor,"CloudCafe/CloudRug",300,180,300,false);
        AddItem(items,"cloud_cloudchair","Cloud Chair","Cloud",DecorationSlot.Chair,"CloudCafe/CloudChair",230,260,450,false);
        AddItem(items,"cloud_aviatorcap","Aviator Cap","Cloud",DecorationSlot.Hats,"CloudCafe/AviatorCap",180,145,250,false);
    }
    [MenuItem("Tools/Import Expanded Room Art")]
    public static void ImportExpandedRoomArt()
    {
        Configure();
        var shop=Object.FindObjectOfType<ShopUI>();if(shop) BuildShopRooms(shop);
        var rewards=Object.FindObjectOfType<RoomRewardsUI>();
        if(rewards) foreach(var card in rewards.cards) {card.detail.fontSizeMin=16;card.detail.fontSizeMax=24;card.detail.enableAutoSizing=true;}
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
    private static void AddItem(List<DecorationItem> items,string id,string name,string skin,DecorationSlot slot,string path,float w,float h,int price,bool reward)
    {
        var item=items.Find(i=>i.id==id);
        if(item==null) {item=new DecorationItem {id=id,displayName=name,skin=skin,slot=slot,size=new Vector2(w,h),price=price,rewardOnly=reward};items.Add(item);}
        if(path!=null) {var sprite=Import("Assets/Art/Decorations/"+path+".png",1024);if(sprite) item.sprite=sprite;}
    }
    private static void AddReward(List<RoomRewardDefinition> rewards,string id,string title,RoomRewardGoal goal,int target,string background,string item)
    {if(!rewards.Any(r=>r.id==id)) rewards.Add(new RoomRewardDefinition {id=id,title=title,goal=goal,target=target,backgroundId=background,decorationId=item});}
    [MenuItem("Tools/Build Room Progress And Preview")]
    public static void Build()
    {
        Configure();var shop=Object.FindObjectOfType<ShopUI>();var ui=Object.FindObjectOfType<UIManager>();if(!shop || !ui || !shop.decorations) return;
        BuildShopRooms(shop);BuildPreview(shop,ui);BuildRewards(shop,ui);
        var studio=shop.decorations;studio.remove.GetComponentInChildren<TextMeshProUGUI>().text="REMOVE AREA";
        foreach(var card in studio.inventoryCards)
        {
            Place(card.title.transform,10,8,240,52);card.title.enableWordWrapping=true;card.title.fontSize=card.title.fontSizeMax=24;card.title.fontSizeMin=20;
            Place(card.preview.transform,12,68,88,100);
            Place(card.detail.transform,105,65,145,48);card.detail.enableWordWrapping=true;card.detail.fontSize=card.detail.fontSizeMax=20;card.detail.fontSizeMin=18;
            Place(card.button.transform,103,120,147,60);
            card.action.rectTransform.anchorMin=Vector2.zero;card.action.rectTransform.anchorMax=Vector2.one;card.action.rectTransform.offsetMin=new Vector2(10,5);card.action.rectTransform.offsetMax=new Vector2(-10,-5);
            card.action.enableWordWrapping=true;card.action.fontSize=card.action.fontSizeMax=25;card.action.fontSizeMin=21;
        }
        studio.remove.GetComponentInChildren<TextMeshProUGUI>().fontSizeMin=21;
        studio.pageLabel.fontSize=studio.pageLabel.fontSizeMax=23;studio.pageLabel.fontSizeMin=20;
        BuildRoomSelector();
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Build Decorate Background Selector")]
    public static void BuildRoomSelector()
    {
        var studio=Object.FindObjectOfType<DecorationStudio>();if(!studio || !studio.panel) return;
        Remove(studio.panel.transform,"ChangeBackground");Remove(studio.panel.transform,"ChooseRoom");
        Place(studio.roomName.transform,90,123,700,45);
        Button(studio.panel.transform,"ChangeBackground","CHANGE BACKGROUND",840,120,380,58,studio.OpenBackgrounds);
        var root=Rect(studio.panel.transform,"ChooseRoom",0,0,1920,1080);Modal(root,360);studio.backgroundPanel=root.gameObject;
        Image(root,"Frame",Art("Panel__Large"),90,45,1740,990);Image(root,"Header",Art("Panel__Header"),520,40,880,145);
        Text(root,"Title","YOUR BACKGROUNDS",660,84,600,58,40,true);
        Button(root,"Close","BACK",1580,85,210,75,studio.CloseBackgrounds);
        Text(root,"Help","Choose an owned background. Each room keeps its own decorations.\nPress SAVE in Decorate to apply your changes.",270,195,1380,74,27);
        studio.backgroundCards=new DecorationCard[4];
        for(int i=0;i<4;i++)
        {
            var backing=Rect(root,"BackgroundCard"+i,230+i%2*760,295+i/2*270,700,250);Round(backing);
            var card=new DecorationCard();card.preview=Image(backing,"Preview",null,20,40,270,152);card.preview.preserveAspect=true;
            card.title=Text(backing,"Name","",300,24,380,52,29);
            card.detail=Text(backing,"State","",300,85,380,42,24);
            card.button=Button(backing,"Action","",320,150,340,75);card.action=card.button.GetComponentInChildren<TextMeshProUGUI>();studio.backgroundCards[i]=card;
        }
        studio.backgroundPrevious=Button(root,"Previous","PREV",480,875,260,80,studio.PreviousBackground);
        studio.backgroundNext=Button(root,"Next","NEXT",1180,875,260,80,studio.NextBackground);
        studio.backgroundPageLabel=Text(root,"Page","",755,891,410,48,27);
        root.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(studio.gameObject.scene);
    }
    private static void BuildShopRooms(ShopUI shop)
    {
        foreach(var page in shop.backgroundPages) if(page) Object.DestroyImmediate(page);
        var pages=new List<GameObject>();var cards=new List<ShopBackgroundCard>();var rooms=EconomyStore.Config.backgrounds;
        for(int i=0;i<rooms.Length;i++)
        {
            if(i%4==0) {var page=Rect(shop.panel.transform,"RoomCatalogPage"+(i/4+1),0,0,1920,1080);pages.Add(page.gameObject);}
            var item=rooms[i];int local=i%4;var root=Rect(pages[pages.Count-1].transform,"Room_"+item.id,290+local%2*680,410+local/2*210,650,195);Round(root);
            var preview=Image(root,"Thumbnail",item.baseSprite ? item.baseSprite : item.sprite,12,15,225,126);preview.preserveAspect=true;
            Text(root,"Name",item.displayName,248,8,390,42,29);
            var detail=Text(root,"Source","",248,52,390,62,23);
            var button=Button(root,"Action","",255,119,370,65);
            var lockImage=Image(root,"Lock",Art("Icon__Lock"),94,50,60,60);lockImage.gameObject.SetActive(false);
            cards.Add(new ShopBackgroundCard {id=item.id,preview=preview,description=detail,button=button,buttonText=button.GetComponentInChildren<TextMeshProUGUI>(),lockIcon=lockImage.gameObject});
        }
        foreach(var page in pages) page.SetActive(false);shop.backgroundPages=pages.ToArray();shop.backgroundCards=cards.ToArray();
    }
    private static Image Highlight(Transform room)
    {
        var image=Image(room,"Area",null,0,0,1,1);image.color=new Color(1,.78f,.15f,.035f);
        for(int i=0;i<4;i++)
        {
            var border=Image(image.transform,"Edge"+i,null,0,0,1,1);border.color=new Color(1,.64f,.09f,.85f);bool horizontal=i<2;float side=i%2;var rect=border.rectTransform;
            rect.anchorMin=horizontal ? new Vector2(0,side) : new Vector2(side,0);rect.anchorMax=horizontal ? new Vector2(1,side) : new Vector2(side,1);
            rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=horizontal ? new Vector2(0,3) : new Vector2(3,0);
        }
        return image;
    }
    private static void BuildPreview(ShopUI shop,UIManager ui)
    {
        var old=shop.GetComponent<DecorationPreview>();if(old) {if(old.panel) Object.DestroyImmediate(old.panel);Object.DestroyImmediate(old);}
        Remove(ui.transform,"AccessoryPreview");var preview=shop.gameObject.AddComponent<DecorationPreview>();preview.shop=shop;shop.decorations.preview=preview;
        var root=Rect(ui.transform,"AccessoryPreview",0,0,1920,1080);Modal(root,450);preview.panel=root.gameObject;
        Image(root,"Frame",Art("Panel__Large"),45,35,1830,1010);
        Image(root,"Header",Art("Panel__Header"),530,30,860,145);Text(root,"Heading","TRY IT IN YOUR ROOM",670,73,580,56,38,true);
        preview.room=Image(root,"Room",null,130,240,1280,720);preview.room.gameObject.AddComponent<RectMask2D>();
        preview.highlight=Highlight(preview.room.transform);
        preview.title=Text(root,"ItemName","",1440,260,300,100,34);
        preview.description=Text(root,"Source","",1430,380,320,240,26);
        preview.actionButton=Button(root,"Action","",1440,660,300,110,preview.Act,true);preview.action=preview.actionButton.GetComponentInChildren<TextMeshProUGUI>();
        Button(root,"Close","BACK",1440,800,300,90,preview.Close);
        preview.status=Text(root,"Status","",180,968,1560,45,26,true);root.gameObject.SetActive(false);
    }
    private static void BuildRewards(ShopUI shop,UIManager ui)
    {
        var old=ui.GetComponent<RoomRewardsUI>();if(old) Object.DestroyImmediate(old);
        Remove(ui.transform,"RoomRewards");Remove(ui.mainMenuPanel,"RoomRewardMenuButton");Remove(ui.GetComponent<CacaoUIController>().winPanel.transform,"RoomRewardResultNotice");
        var rewards=ui.gameObject.AddComponent<RoomRewardsUI>();rewards.ui=ui;rewards.shop=shop;
        rewards.menuInput=ui.mainMenuPanel.GetComponent<CanvasGroup>();if(!rewards.menuInput) rewards.menuInput=ui.mainMenuPanel.gameObject.AddComponent<CanvasGroup>();
        var menu=Button(ui.mainMenuPanel,"RoomRewardMenuButton","ROOM REWARDS",65,535,440,100,rewards.Open);
        var badge=Rect(menu.transform,"New",392,-5,55,45);Round(badge);rewards.badge=badge.gameObject;rewards.badgeLabel=Text(badge,"Count","",0,0,55,45,24);badge.gameObject.SetActive(false);
        var root=Rect(ui.transform,"RoomRewards",0,0,1920,1080);Modal(root,420);rewards.panel=root.gameObject;
        Image(root,"Frame",Art("Panel__Large"),40,15,1840,1050);Image(root,"Header",Art("Panel__Header"),540,13,840,140);
        Text(root,"Title","PLAY, COLLECT, DECORATE",660,52,600,62,38,true);Button(root,"Close","CLOSE",1620,65,220,80,rewards.Close);
        rewards.summary=Text(root,"Summary","",180,173,1560,46,29);
        string[] tabs={"CHAPTERS","STARS","COLLECTIONS","KEEPSAKES"};rewards.filters=new Button[tabs.Length];
        for(int i=0;i<tabs.Length;i++) rewards.filters[i]=Button(root,"Filter"+tabs[i],tabs[i],310+i*330,226,310,64);
        rewards.cards=new RoomRewardCard[4];
        for(int i=0;i<4;i++)
        {
            var card=new RoomRewardCard();var backing=Rect(root,"RewardCard"+i,150+i%2*830,320+i/2*270,800,250);Round(backing);card.root=backing.gameObject;
            card.preview=Image(backing,"Preview",null,16,25,190,170);card.preview.preserveAspect=true;
            card.title=Text(backing,"Title","",215,12,565,44,29);card.detail=Text(backing,"Condition","",220,64,370,120,24);
            card.progress=Text(backing,"Progress","",215,195,565,40,23);
            card.button=Button(backing,"Action","",600,80,180,95);card.action=card.button.GetComponentInChildren<TextMeshProUGUI>();
            card.action.fontSize=card.action.fontSizeMax=24;card.action.fontSizeMin=21;rewards.cards[i]=card;
        }
        rewards.notice=Text(root,"Notice","",190,875,1540,64,27);
        rewards.previous=Button(root,"Previous","PREV",500,949,250,70,rewards.Previous);
        rewards.next=Button(root,"Next","NEXT",1170,949,250,70,rewards.Next);
        rewards.pageLabel=Text(root,"Page","",810,959,300,45,30,true);
        root.gameObject.SetActive(false);
    }
}
