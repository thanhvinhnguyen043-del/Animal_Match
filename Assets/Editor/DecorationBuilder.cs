using System.Linq;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class DecorationBuilder
{
    [System.Serializable] private class Catalog {public CatalogItem[] items;}
    [System.Serializable] private class CatalogItem
    {
        public string id,displayName,skin,path;
        public int price;
        public Vector2 size;
    }
    public static void RefreshCatalog()
    {
        string path="Assets/Editor/DecorationCatalog.json";
        if(!File.Exists(path)) return;
        var config=EconomyStore.Config;var result=config.decorations.ToList();
        var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
        foreach(var entry in catalog.items)
        {
            var item=result.FirstOrDefault(i=>i.id==entry.id);
            if(item==null)
            {
                item=new DecorationItem {id=entry.id,displayName=entry.displayName,skin=entry.skin,price=entry.price,size=entry.size};
                result.Add(item);
            }
            item.sprite=Import(entry.path,1024);
        }
        config.decorations=result.ToArray();
        foreach(var background in config.backgrounds)
        {
            if(!background.baseSprite) continue;
            string expanded="Assets/Art/BackgroundBasesExpanded/"+background.baseSprite.name+".png";
            if(File.Exists(expanded)) background.baseSprite=Import(expanded,2048);
        }
        ConfigureSlots();EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
    }
    private static void ConfigureSlots()
    {
        var config=EconomyStore.Config;
        string[] groups={"catplant,christmastree,apricotblossom,palmplant,bananaplant,fern", "sakuralantern,pinegarland,redlantern,ghostbatgarland,hangingvine", "catpicture,surfboard", "bonsai,coffeemachine,luckycat,tablelamp", "teaset,tettray,candybowl,coconutdrink,fruitbowl", "pumpkinlantern,giftboxes,chungcakes", "catchair", "witchhat,strawhat"};
        foreach(var item in config.decorations)
        {
            string suffix=item.id.Substring(item.id.IndexOf('_')+1);
            for(int group=0;group<groups.Length;group++)
                if(groups[group].Split(',').Contains(suffix)) {item.slot=(DecorationSlot)(group+1);break;}
        }
        var anchors=(config.decorationSlots ?? new DecorationSlotAnchor[0]).ToList();
        foreach(var background in config.backgrounds)
            for(int slot=1;slot<=DecorationSlots.Count;slot++)
                if(!anchors.Any(a=>a.backgroundId==background.id && a.slot==(DecorationSlot)slot))
                    anchors.Add(DecorationSlots.Default(background.id,(DecorationSlot)slot));
        config.decorationSlots=anchors.ToArray();
    }
    [MenuItem("Tools/Apply Decoration Slots")]
    public static void ApplySlotLayout()
    {
        ConfigureSlots();EditorUtility.SetDirty(EconomyStore.Config);
        var studio=Object.FindObjectOfType<DecorationStudio>();if(!studio) return;
        var root=studio.panel.transform;
        foreach(string name in new[]{"Smaller","Larger","Flip","Back","Front","SlotButtons","FixedAreaHelp"})
        {
            var old=root.Find(name);if(old) Object.DestroyImmediate(old.gameObject);
        }
        var previous=studio.room.transform.Find("SlotHighlight");if(previous) Object.DestroyImmediate(previous.gameObject);
        Place(root.Find("Header"),380,6,1160,120);Place(root.Find("Title"),520,30,880,65);
        Place(studio.roomName.transform,50,127,1200,42);Place(studio.room.transform,90,255,1120,630);
        Place(studio.selection.transform,50,895,1200,40);
        Place(root.Find("InventoryTitle"),1320,132,540,48);Place(studio.pageLabel.transform,1320,181,540,35);
        var slots=Rect(root,"SlotButtons",50,175,1200,65);studio.slotButtons=new Button[DecorationSlots.Count];
        for(int i=0;i<DecorationSlots.Count;i++)
        {
            var button=Button(slots,DecorationSlots.Label((DecorationSlot)(i+1)),DecorationSlots.Label((DecorationSlot)(i+1)),i*150,0,140,65);
            var text=button.GetComponentInChildren<TextMeshProUGUI>();text.fontSize=text.fontSizeMax=23;text.fontSizeMin=20;
            studio.slotButtons[i]=button;
        }
        studio.slotHighlight=Image(studio.room.transform,"SlotHighlight",null,0,0,1,1);
        studio.slotHighlight.color=new Color(1,.78f,.15f,.04f);studio.slotHighlight.raycastTarget=false;
        for(int edge=0;edge<4;edge++)
        {
            var border=Image(studio.slotHighlight.transform,"AreaEdge"+edge,null,0,0,1,1);border.color=new Color(1,.65f,.08f,.9f);
            var rect=border.rectTransform;bool horizontal=edge<2;float side=edge%2;
            rect.anchorMin=horizontal ? new Vector2(0,side) : new Vector2(side,0);
            rect.anchorMax=horizontal ? new Vector2(1,side) : new Vector2(side,1);
            rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=horizontal ? new Vector2(0,3) : new Vector2(3,0);
        }
        Text(root,"FixedAreaHelp","Choose an area above. Equip one item per area.",50,947,760,78,27);
        Place(studio.previous.transform,1320,846,150,75);Place(studio.next.transform,1710,846,150,75);
        studio.previous.gameObject.SetActive(false);studio.next.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(studio.gameObject.scene);AssetDatabase.SaveAssets();
    }
    private static Color ink=new Color(.38f,.2f,.11f);
    private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    private static void Place(Transform target,float x,float y,float w,float h)
    {
        var r=(RectTransform)target;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
    }
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string text,float x,float y,float w,float h,int size=30)
    {
        var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.text=text;label.font=GameTypography.Current.font;
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao Body.mat");label.color=ink;
        label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-3;label.enableAutoSizing=true;label.enableWordWrapping=false;
        label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/"+name+".png");
    private static Button Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityAction action=null,bool primary=false)
    {
        var image=Image(parent,name,Art(primary ? "Button__Primary" : "Button__Secondary"),x,y,w,h);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var label=Text(image.transform,"Label",text,8,3,w-16,h-6,28);label.color=new Color(1,.98f,.9f);
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao Heading.mat");
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action);
        image.gameObject.AddComponent<UIButtonClickSound>().clickSound=Object.FindObjectOfType<GridManager>().sfxClick;
        return button;
    }
    private static DecorationCard Card(Transform parent,float x,float y,float width,bool inventory)
    {
        var root=Rect(parent,inventory ? "OwnedItem" : "AccessoryCard",x,y,width,190);
        var panel=root.gameObject.AddComponent<RoundedImage>();panel.cornerRadius=22;panel.color=new Color(1,.94f,.81f);panel.raycastTarget=false;
        var outline=root.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.8f,.55f,.27f);outline.effectDistance=new Vector2(2,-2);
        float imageW=inventory ? 85 : 130,textX=inventory ? 95 : 150;
        var card=new DecorationCard();card.preview=Image(root,"Preview",null,10,25,imageW,130);card.preview.preserveAspect=true;
        card.title=Text(root,"Name","",textX,12,width-textX-10,48,inventory ? 24 : 28);
        card.detail=Text(root,"Skin","",textX,62,width-textX-10,34,inventory ? 20 : 22);
        card.button=Button(root,"Action","",textX,112,width-textX-12,66);
        card.action=card.button.GetComponentInChildren<TextMeshProUGUI>();return card;
    }
    private static Sprite Import(string path,int maximum)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=maximum;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Tools/Build Decoration UI")]
    public static void Build()
    {
        var shop=Object.FindObjectOfType<ShopUI>();var ui=Object.FindObjectOfType<UIManager>();
        var old=shop.GetComponent<DecorationStudio>();
        if(old) {if(old.panel) Object.DestroyImmediate(old.panel);Object.DestroyImmediate(old);}
        var config=EconomyStore.Config;
        string[] ids={"cafe","japan","winter","tet","halloween","beach","jungle"};
        string[] folders={"Cozy","Japan","Winter","Tet","Halloween","Beach","Jungle"};
        string[] bases={"CozyCafe","JapanCafe","WinterCafe","TetCafe","HalloweenCafe","BeachCafe","JungleCafe"};
        string[] props={"CatPlant","CatPicture","Bonsai","SakuraLantern","ChristmasTree","PineGarland","ApricotBlossom","RedLantern","PumpkinLantern","GhostBatGarland","PalmPlant","Surfboard","BananaPlant","HangingVine"};
        string[] names={"Cat Planter","Cat Portrait","Bonsai","Sakura Lantern","Christmas Tree","Pine Garland","Apricot Blossom","Red Lantern","Pumpkin Lantern","Ghost Garland","Tropical Palm","Surfboard","Jungle Plant","Hanging Vines"};
        string[] skins={"Cozy","Japan","Winter","Tet","Halloween","Beach","Jungle"};
        var items=new DecorationItem[14];
        for(int i=0;i<7;i++)
        {
            var background=EconomyStore.FindBackground(ids[i]);background.baseSprite=Import("Assets/Art/BackgroundBases/"+bases[i]+".png",2048);
            for(int p=0;p<2;p++)
            {
                int n=i*2+p;
                items[n]=new DecorationItem {id=ids[i]+"_"+props[n].ToLowerInvariant(),displayName=names[n],skin=skins[i],price=p==0 ? 400 : 250,
                    sprite=Import("Assets/Art/Decorations/"+folders[i]+"/"+props[n]+".png",1024),size=new Vector2(260,320)};
                if(n==1) items[n].size=new Vector2(170,210);
                if(n==2) items[n].size=new Vector2(220,180);
                if(n==4 || n==6) items[n].size=new Vector2(380,530);
                if(n==5 || n==9 || n==13) items[n].size=new Vector2(620,260);
                if(n==8) items[n].size=new Vector2(210,190);
                if(n==11) items[n].size=new Vector2(200,490);
            }
        }
        var catalogItems=(config.decorations ?? new DecorationItem[0]).ToList();
        foreach(var item in items) if(!catalogItems.Any(existing=>existing.id==item.id)) catalogItems.Add(item);
        config.decorations=catalogItems.ToArray();EditorUtility.SetDirty(config);RefreshCatalog();
        var studio=shop.gameObject.AddComponent<DecorationStudio>();studio.shop=shop;shop.decorations=studio;
        var prior=shop.panel.transform.Find("Decorations");if(prior) Object.DestroyImmediate(prior.gameObject);
        var page=Rect(shop.panel.transform,"Decorations",0,0,1920,1080);shop.decorationPage=page.gameObject;
        studio.shopCards=new DecorationCard[6];
        for(int i=0;i<6;i++) studio.shopCards[i]=Card(page,300+i%3*440,420+i/3*210,420,false);
        page.gameObject.SetActive(false);
        foreach(string name in new[]{"DecorationTab","OpenStudio"}) {var prev=shop.panel.transform.Find(name);if(prev) Object.DestroyImmediate(prev.gameObject);}
        Place(shop.boosterTab.transform,390,250,340,126);Place(shop.backgroundTab.transform,750,250,340,126);
        var boostLabel=shop.panel.transform.Find("Text__BOOSTERS");var bgLabel=shop.panel.transform.Find("Text__BACKGROUNDS");
        if(boostLabel && boostLabel.parent!=shop.boosterTab.transform) Place(boostLabel,420,282,280,58);
        if(bgLabel && bgLabel.parent!=shop.backgroundTab.transform) Place(bgLabel,780,282,280,58);
        var tab=Button(shop.panel.transform,"DecorationTab","ACCESSORIES",1110,250,340,126,shop.ShowDecorations);shop.decorationTab=tab.GetComponent<Image>();
        Button(shop.panel.transform,"OpenStudio","DECORATE",1470,270,300,90,studio.Open,true);
        var menu=ui.mainMenuPanel.Find("OpenStudio");if(menu) Object.DestroyImmediate(menu.gameObject);
        Button(ui.mainMenuPanel,"OpenStudio","INVENTORY / DECORATE",1320,910,480,100,studio.Open,true);
        var root=Rect(shop.transform,"DecorationStudio",0,0,1920,1080);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
        var backdrop=root.gameObject.AddComponent<Image>();backdrop.color=new Color(1,.94f,.81f);
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=300;root.gameObject.AddComponent<GraphicRaycaster>();studio.panel=root.gameObject;
        Image(root,"Header",Art("Panel__Header"),380,6,1160,150);
        Text(root,"Title","DECORATE YOUR CAFE",520,44,880,65,46);
        studio.coins=Text(root,"Coins","",1510,52,350,60,34);
        studio.roomName=Text(root,"RoomName","",55,137,1180,45,32);
        studio.room=Image(root,"Room",null,50,190,1200,675);studio.room.gameObject.AddComponent<RectMask2D>();studio.room.raycastTarget=true;
        var border=root.gameObject.AddComponent<CanvasGroup>();border.blocksRaycasts=true;
        Text(root,"InventoryTitle","YOUR INVENTORY",1320,150,540,55,34);
        studio.inventoryCards=new DecorationCard[6];
        for(int i=0;i<6;i++) studio.inventoryCards[i]=Card(root,1320+i%2*280,220+i/2*205,260,true);
        studio.previous=Button(root,"Previous","PREV",1320,846,150,75,studio.Previous);
        studio.next=Button(root,"Next","NEXT",1710,846,150,75,studio.Next);
        studio.pageLabel=Text(root,"Page","",1470,850,240,65,25);
        studio.selection=Text(root,"Selection","",50,880,1200,45,29);
        studio.remove=Button(root,"Remove","REMOVE",850,945,180,85,studio.Remove);
        studio.clear=Button(root,"Clear","CLEAR ROOM",1050,945,220,85,studio.Clear);
        studio.save=Button(root,"Save","SAVE",1320,945,250,85,studio.Save,true);
        Button(root,"Close","CLOSE / CANCEL",1590,945,270,85,studio.Close);
        studio.status=Text(root,"Status","",50,1030,1810,42,25);
        ApplySlotLayout();
        root.gameObject.SetActive(false);
        var targets=shop.backgroundTargets.Where(i=>i).ToList();var shopImage=shop.panel.GetComponent<Image>();if(shopImage) targets.Add(shopImage);
        foreach(var target in targets.Distinct())
        {
            var existing=target.transform.Find("RoomDecorations");if(existing) Object.DestroyImmediate(existing.gameObject);
            var layer=Rect(target.transform,"RoomDecorations",0,0,1920,1080);layer.anchorMin=Vector2.zero;layer.anchorMax=Vector2.one;layer.offsetMin=layer.offsetMax=Vector2.zero;
            layer.SetAsFirstSibling();layer.gameObject.AddComponent<RectMask2D>();layer.gameObject.AddComponent<RoomDecorationView>();
            target.sprite=EconomyStore.FindBackground("cafe").baseSprite;
        }
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
