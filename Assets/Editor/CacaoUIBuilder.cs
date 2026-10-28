using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CacaoUIBuilder
{
    [Serializable] private class Layouts { public Layout[] scenes; }
    [Serializable] private class Layout { public string name; public Node[] nodes; }
    [Serializable] private class Node
    {
        public string type,name,asset,text,color,fill,stroke,align;
        public float x,y,w,h,size,r,sw,opacity=1;
    }
    private static Layouts layouts;
    private static GridManager grid;
    private static UIManager ui;
    private static CacaoUIController controller;
    private static ShopUI shop;
    private static Material bodyMaterial,headingMaterial;
    private static readonly List<Image> backgrounds=new List<Image>();
    private static readonly List<TextMeshProUGUI> coins=new List<TextMeshProUGUI>();
    private static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    private static readonly Color Cacao=new Color(.427f,.235f,.129f);

    public static void Build()
    {
        layouts=JsonUtility.FromJson<Layouts>(File.ReadAllText("Assets/Editor/CacaoLayouts.json"));
        backgrounds.Clear();coins.Clear();sprites.Clear();
        foreach(string path in Directory.GetFiles("Assets/Art/CacaoUI","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.isReadable=false;
            importer.maxTextureSize=Path.GetFileName(path).StartsWith("Icon__") ? 512 : 2048;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            sprites[Path.GetFileNameWithoutExtension(path)]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        bodyMaterial=FontMaterial("Cacao Body",0);
        headingMaterial=FontMaterial("Cacao Heading",.22f);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        grid=UnityEngine.Object.FindObjectOfType<GridManager>();
        var oldUI=UnityEngine.Object.FindObjectOfType<UIManager>();
        var menuMusic=oldUI.bgmMenu;var gameMusic=oldUI.bgmGameplay;
        grid.transform.SetParent(null);
        var oldShops=UnityEngine.Object.FindObjectsOfType<ShopUI>(true);
        var oldAudio=UnityEngine.Object.FindObjectsOfType<AudioSettingsUI>(true);
        var oldControllers=UnityEngine.Object.FindObjectsOfType<CacaoUIController>(true);
        foreach(var rootCanvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true).Where(c=>c.isRootCanvas).ToArray())
            if(rootCanvas) UnityEngine.Object.DestroyImmediate(rootCanvas.gameObject);
        if(oldUI) UnityEngine.Object.DestroyImmediate(oldUI);
        foreach(var item in oldShops) if(item) UnityEngine.Object.DestroyImmediate(item);
        foreach(var item in oldAudio) if(item) UnityEngine.Object.DestroyImmediate(item);
        foreach(var item in oldControllers) if(item) UnityEngine.Object.DestroyImmediate(item);
        var canvasObject=new GameObject("CacaoCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        ui=canvasObject.AddComponent<UIManager>();ui.screen=GameScreen.Combined;ui.bgmMenu=menuMusic;ui.bgmGameplay=gameMusic;
        shop=canvasObject.AddComponent<ShopUI>();
        controller=canvasObject.AddComponent<CacaoUIController>();controller.grid=grid;controller.ui=ui;
        BuildMenu(canvas.transform);
        BuildLevels(canvas.transform);
        BuildGameplay(canvas.transform);
        BuildSettings(canvas.transform);
        BuildPause(canvas.transform);
        BuildShop(canvas.transform);
        BuildResults(canvas.transform);
        CacaoBoardLayout.Apply();
        CacaoReadabilityLayout.Apply();
        WaterTutorialBuilder.Build();
        shop.backgroundTargets=backgrounds.ToArray();shop.coinLabels=coins.ToArray();
        shop.mainMenuPanel=ui.mainMenuPanel;
        ui.settingsPanel.gameObject.SetActive(false);
        ui.levelSelectPanel.anchoredPosition=new Vector2(-1920,0);
        ui.gameplayPanel.anchoredPosition=new Vector2(0,1080);
        DecorationBuilder.Build();
        DailyRewardBuilder.Build();
        LoadingUIBuilder.Build();
        ProgressUIBuilder.Build();
        controller.pausePanel.SetActive(false);grid.gameOverPanel.SetActive(false);shop.panel.SetActive(false);
        if(!UnityEngine.Object.FindObjectOfType<EventSystem>()) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("CACAO UI BUILD COMPLETE: native panels, buttons, sliders, live text, shop tabs and background pages.");
    }
    private static Material FontMaterial(string name,float outline)
    {
        string path="Assets/Fonts/LilitaOne/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(GameTypography.Current.font.material);AssetDatabase.CreateAsset(material,path);}
        material.SetColor(ShaderUtilities.ID_OutlineColor,Cacao);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth,outline);
        material.SetColor(ShaderUtilities.ID_FaceColor,Color.white);
        if(outline>0) material.EnableKeyword(ShaderUtilities.Keyword_Outline); else material.DisableKeyword(ShaderUtilities.Keyword_Outline);
        EditorUtility.SetDirty(material);return material;
    }
    private static RectTransform Panel(string name,Transform parent)
    {
        var panel=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();panel.SetParent(parent,false);
        panel.anchorMin=panel.anchorMax=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(1920,1080);return panel;
    }
    private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    private static Color ColorOf(string value,float alpha=1)
    {
        if(!ColorUtility.TryParseHtmlString(value,out var color)) color=Color.white;
        color.a=alpha;return color;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,Color color,bool outlined=false)
    {
        var label=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=GameTypography.Current.font;label.fontSharedMaterial=outlined ? headingMaterial : bodyMaterial;
        label.fontStyle=FontStyles.Normal;label.text=value.Replace("�","•");label.color=color;
        label.fontSize=size;label.fontSizeMax=size;label.fontSizeMin=Mathf.Min(14,size);label.enableAutoSizing=true;
        label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.enableWordWrapping=true;
        return label;
    }
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private static GameObject NodeObject(Transform parent,Node node,int index,int screen)
    {
        string name=screen==2 ? CacaoBoardLayout.NameForNode(index,node.name) : node.name;
        if(node.type=="text")
        {
            var label=Text(parent,name,node.text,node.x,node.y,node.w,node.h,node.size,ColorOf(node.color),!string.IsNullOrEmpty(node.stroke));
            if(node.align=="left") label.alignment=TextAlignmentOptions.Left;
            if(node.text=="275") {label.text="0";coins.Add(label);}
            return label.gameObject;
        }
        if(node.type=="image")
        {
            Sprite sprite;
            if(node.asset=="Background" || node.name.StartsWith("Preview__"))
            {
                string id=node.name.StartsWith("Preview__") ? BackgroundId(node.name.Substring(9)) : EconomyStore.DefaultBackgroundId;
                sprite=EconomyStore.FindBackground(id).sprite;
            }
            else sprite=sprites[node.asset];
            var image=Image(parent,name,sprite,node.x,node.y,node.w,node.h);
            image.color=new Color(1,1,1,node.opacity);
            if(node.name=="Background") {image.preserveAspect=false;backgrounds.Add(image);}
            return image.gameObject;
        }
        var shape=Rect(name,parent,node.x,node.y,node.w,node.h).gameObject.AddComponent<RoundedImage>();
        shape.cornerRadius=node.r;shape.color=ColorOf(node.fill,node.opacity);shape.raycastTarget=false;
        if(node.sw>0 && !string.IsNullOrEmpty(node.stroke))
        {
            var outline=shape.gameObject.AddComponent<Outline>();outline.effectColor=ColorOf(node.stroke,node.opacity);outline.effectDistance=new Vector2(node.sw/2,-node.sw/2);
        }
        return shape.gameObject;
    }
    private static Dictionary<int,GameObject> Render(int screen,Transform parent,int start=0,int end=int.MaxValue,Func<Node,bool> filter=null)
    {
        var nodes=layouts.scenes[screen].nodes;var result=new Dictionary<int,GameObject>();
        for(int i=start;i<nodes.Length && i<end;i++) if(filter==null || filter(nodes[i])) result[i]=NodeObject(parent,nodes[i],i,screen);
        return result;
    }
    private static Button Button(GameObject obj,UnityAction action)
    {
        var button=obj.AddComponent<Button>();var graphic=obj.GetComponent<Graphic>();
        graphic.raycastTarget=true;button.targetGraphic=graphic;
        var colors=button.colors;colors.highlightedColor=new Color(1,1,.94f);colors.pressedColor=new Color(.86f,.77f,.65f);colors.disabledColor=new Color(.7f,.65f,.57f);button.colors=colors;
        UnityEventTools.AddPersistentListener(button.onClick,action);
        var sound=obj.AddComponent<UIButtonClickSound>();sound.clickSound=grid.sfxClick;return button;
    }
    private static TextMeshProUGUI Label(Dictionary<int,GameObject> nodes,int index) => nodes[index].GetComponent<TextMeshProUGUI>();
    private static string BackgroundId(string name)
    {
        if(name=="COZY CAFE") return "cafe";
        if(name=="TET CAFE") return "tet";
        return name.Split(' ')[0].ToLowerInvariant();
    }
    private static void BuildMenu(Transform canvas)
    {
        ui.mainMenuPanel=Panel("MainMenu",canvas);var nodes=Render(0,ui.mainMenuPanel);
        Button(nodes[4],ui.OpenSettings);Button(nodes[5],shop.Open);Button(nodes[10],ui.PlayLatestLevel);Button(nodes[12],ui.OpenLevelSelect);
    }
    private static void BuildLevels(Transform canvas)
    {
        ui.levelSelectPanel=Panel("LevelSelect",canvas);
        var nodes=Render(1,ui.levelSelectPanel,0,9);Button(nodes[2],ui.CloseLevelSelect);
        var viewport=Rect("LevelViewport",ui.levelSelectPanel,475,383,956,411);
        var surface=viewport.gameObject.AddComponent<Image>();surface.color=Color.clear;surface.raycastTarget=true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;
        var content=Rect("LevelContent",viewport,0,0,956,411);scroll.content=content;
        var layout=content.gameObject.AddComponent<GridLayoutGroup>();layout.cellSize=new Vector2(172,184);layout.spacing=new Vector2(24,43);
        layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=5;
        var selection=ui.levelSelectPanel.gameObject.AddComponent<LevelSelectionUI>();selection.ui=ui;selection.content=content;selection.compactTiles=true;
        var template=Rect("LevelTemplate",ui.levelSelectPanel,0,0,172,184);
        var background=template.gameObject.AddComponent<RoundedImage>();background.cornerRadius=32;background.color=new Color(1,.98f,.91f);
        var outline=template.gameObject.AddComponent<Outline>();outline.effectColor=ColorOf("#D4954D");outline.effectDistance=new Vector2(2,-2);
        selection.levelButtonTemplate=template.gameObject.AddComponent<Button>();selection.levelButtonTemplate.targetGraphic=background;
        var number=Text(template,"Number","1",0,40,172,95,76,Cacao);number.enableAutoSizing=false;
        number.rectTransform.anchorMin=number.rectTransform.anchorMax=new Vector2(.5f,1);number.rectTransform.pivot=new Vector2(.5f,.5f);
        Text(template,"LevelStatus","PLAY",0,137,172,30,20,ColorOf("#658342"));
        Image(template,"LevelLockIcon",sprites["Icon__Lock"],48,37,78,78);
        template.gameObject.SetActive(false);
        Text(ui.levelSelectPanel,"LevelPage","1 / 1",800,900,320,34,27,Cacao);
    }
    private static void BuildGameplay(Transform canvas)
    {
        ui.gameplayPanel=Panel("Gameplay",canvas);
        var nodes=Render(2,ui.gameplayPanel,0,int.MaxValue,n=>!n.name.StartsWith("TileSlot__") && n.name!="Timer__Fill" && n.name!="Timer__Highlight");
        grid.scoreText=controller.scoreNumber=Label(nodes,4);grid.comboText=Text(ui.gameplayPanel,"Combo","",60,139,310,35,24,Cacao);
        ui.gameplayLevelText=Label(nodes,6);controller.timerText=Label(nodes,15);controller.pairsText=Label(nodes,79);
        Button(nodes[10],controller.OpenPause);grid.btnShuffle=Button(nodes[68],grid.ShuffleBoard);grid.btnHint=Button(nodes[69],grid.ShowHint);
        shop.shuffleCount=Label(nodes,75);shop.hintCount=Label(nodes,77);
        var timerRoot=Rect("TimerSlider",ui.gameplayPanel,620,175,668,30);
        grid.timeSlider=timerRoot.gameObject.AddComponent<Slider>();grid.timeSlider.interactable=false;grid.timeSlider.transition=Selectable.Transition.None;
        var fillArea=Rect("FillArea",timerRoot,5,5,658,20);
        var fill=Rect("Fill",fillArea,0,0,658,20).gameObject.AddComponent<RoundedImage>();fill.cornerRadius=10;fill.color=ColorOf("#83C75C");fill.raycastTarget=false;
        fill.rectTransform.pivot=new Vector2(.5f,.5f);fill.rectTransform.anchoredPosition=Vector2.zero;fill.rectTransform.sizeDelta=Vector2.zero;
        grid.timeSlider.fillRect=fill.rectTransform;grid.timeSlider.value=1;
        grid.boardContainer=Rect("BoardContainer",ui.gameplayPanel,960,564,1210,660);
        grid.boardContainer.pivot=new Vector2(.5f,.5f);
        var input=grid.boardContainer.gameObject.AddComponent<TileColliderInput>();input.Configure(grid,400,new Vector2(1210,660));
        grid.boardContainer.SetSiblingIndex(nodes[17].transform.GetSiblingIndex()+1);
    }
    private static Slider AudioSlider(Transform parent,float y,Color color)
    {
        var rect=Rect("VolumeSlider",parent,930,y-12,390,54);
        var slider=rect.gameObject.AddComponent<Slider>();
        var area=Rect("Track",rect,6,15,372,24);var track=area.gameObject.AddComponent<RoundedImage>();track.cornerRadius=12;track.color=ColorOf("#E7C89C");track.raycastTarget=true;
        var outline=area.gameObject.AddComponent<Outline>();outline.effectColor=ColorOf("#AC7644");outline.effectDistance=new Vector2(1,-1);
        var fillArea=Rect("FillArea",area,3,3,366,18);
        var fill=Rect("Fill",fillArea,0,0,366,18).gameObject.AddComponent<RoundedImage>();fill.cornerRadius=9;fill.color=color;fill.raycastTarget=false;
        fill.rectTransform.pivot=new Vector2(.5f,.5f);fill.rectTransform.anchoredPosition=Vector2.zero;fill.rectTransform.sizeDelta=Vector2.zero;slider.fillRect=fill.rectTransform;
        var handleArea=Rect("HandleArea",rect,6,0,372,54);
        var handle=Rect("Handle",handleArea,0,5,38,44).gameObject.AddComponent<RoundedImage>();handle.cornerRadius=18;handle.color=ColorOf("#FFFDEE");
        handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;handle.rectTransform.sizeDelta=new Vector2(38,-10);
        var border=handle.gameObject.AddComponent<Outline>();border.effectColor=ColorOf("#B77942");border.effectDistance=new Vector2(1.5f,-1.5f);
        slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=1;return slider;
    }
    private static void BuildSettings(Transform canvas)
    {
        ui.settingsPanel=Panel("Settings",canvas);
        var nodes=Render(3,ui.settingsPanel,80,int.MaxValue,n=>!n.name.StartsWith("Slider__"));
        nodes[80].GetComponent<Image>().raycastTarget=true;
        Button(nodes[84],ui.CloseSettings);Button(nodes[100],ui.ReturnHomeFromSettings);
        var audio=canvas.gameObject.AddComponent<AudioSettingsUI>();
        audio.masterSlider=AudioSlider(ui.settingsPanel,382,ColorOf("#78C765"));audio.musicSlider=AudioSlider(ui.settingsPanel,507,ColorOf("#AD83DB"));audio.sfxSlider=AudioSlider(ui.settingsPanel,632,ColorOf("#65BEDC"));
        audio.masterValueText=Text(ui.settingsPanel,"MasterPercent","100%",1315,365,90,44,24,Cacao);
        audio.musicValueText=Text(ui.settingsPanel,"MusicPercent","100%",1315,490,90,44,24,Cacao);
        audio.sfxValueText=Text(ui.settingsPanel,"SfxPercent","100%",1315,615,90,44,24,Cacao);
        int[] indices={85,90,95};
        for(int i=0;i<indices.Length;i++)
        {
            var obj=nodes[indices[i]];var mute=obj.AddComponent<AudioMuteButton>();mute.channel=(AudioChannel)i;mute.background=obj.GetComponent<Image>();
            mute.onSprite=sprites[i==1 ? "Icon__Music" : "Icon__Sound"];mute.offSprite=sprites[i==1 ? "Icon__MusicOff" : "Icon__SoundOff"];
            var button=obj.GetComponent<Button>();button.targetGraphic=mute.background;mute.background.raycastTarget=true;
            obj.AddComponent<UIButtonClickSound>().clickSound=grid.sfxClick;
        }
        var restart=Image(ui.settingsPanel,"RestartLevel",sprites["Icon__Retry"],1210,742,95,95);
        ui.settingsRestartButton=Button(restart.gameObject,ui.RestartFromSettings);
        var caption=Text(restart.transform,"Caption","RESTART LEVEL",-22,95,139,28,18,Cacao);
        caption.rectTransform.anchoredPosition=new Vector2(-22,-95);
    }
    private static void BuildPause(Transform canvas)
    {
        var panel=Panel("Pause",canvas);controller.pausePanel=panel.gameObject;
        var nodes=Render(4,panel,80);nodes[80].GetComponent<Image>().raycastTarget=true;
        Button(nodes[84],controller.Resume);Button(nodes[87],controller.Resume);Button(nodes[89],controller.Home);Button(nodes[90],controller.Retry);
        var settings=Image(panel,"PauseSettings",sprites["Icon__Settings"],1182,735,82,82);Button(settings.gameObject,controller.OpenAudioSettings);
        Text(panel,"SettingsCaption","SETTINGS",1154,827,138,29,21,Cacao);
    }
    private static void BuildShop(Transform canvas)
    {
        var panel=Panel("Shop",canvas);shop.panel=panel.gameObject;
        var nodes=Render(5,panel,0,14);Button(nodes[2],shop.Close);
        shop.boosterTab=nodes[9].GetComponent<Image>();shop.backgroundTab=nodes[11].GetComponent<Image>();
        Button(nodes[9],shop.ShowBoosters);Button(nodes[11],shop.ShowBackgrounds);
        shop.selectedTabSprite=sprites["Button__Primary"];shop.otherTabSprite=sprites["Button__Secondary"];
        shop.shopStock=Label(nodes,13);
        var boosters=Panel("BoostersPage",panel);shop.boosterPage=boosters.gameObject;
        var boosterNodes=Render(5,boosters,14);
        shop.hintBuyButton=Button(boosterNodes[18],shop.BuyHint);shop.shuffleBuyButton=Button(boosterNodes[24],shop.BuyShuffle);
        shop.hintPriceText=Label(boosterNodes,19);shop.shufflePriceText=Label(boosterNodes,25);
        var pages=new List<GameObject>();var cards=new List<ShopBackgroundCard>();
        for(int page=0;page<2;page++)
        {
            var root=Panel("BackgroundsPage"+(page+1),panel);pages.Add(root.gameObject);
            var cardNodes=Render(8+page,root,14,int.MaxValue,n=>n.name!="Thumbnail__LockedTint");
            int[][] indices=page==0 ? new[]{new[]{16,17,18,19,20,-1},new[]{24,25,26,27,28,-1},new[]{32,35,36,37,38,34},new[]{42,45,46,47,48,44}} : new[]{new[]{16,19,20,21,22,18},new[]{26,29,30,31,32,28},new[]{36,39,40,41,42,38}};
            string[] ids=page==0 ? new[]{"cafe","japan","winter","tet"} : new[]{"halloween","beach","jungle"};
            for(int i=0;i<indices.Length;i++)
            {
                var map=indices[i];var button=cardNodes[map[3]].AddComponent<Button>();button.targetGraphic=cardNodes[map[3]].GetComponent<Graphic>();button.targetGraphic.raycastTarget=true;
                cardNodes[map[3]].AddComponent<UIButtonClickSound>().clickSound=grid.sfxClick;
                var card=new ShopBackgroundCard {id=ids[i],preview=cardNodes[map[0]].GetComponent<Image>(),button=button,buttonText=Label(cardNodes,map[4]),description=Label(cardNodes,map[2]),lockIcon=map[5]<0 ? null : cardNodes[map[5]]};
                if(!card.lockIcon)
                {
                    var preview=layouts.scenes[8+page].nodes[map[0]];
                    card.lockIcon=Image(root,"Lock_"+ids[i],sprites["Icon__Lock"],preview.x+79,preview.y+31,57,57).gameObject;
                }
                cards.Add(card);
            }
            if(page==0) Button(cardNodes[51],shop.NextBackgroundPage);else Button(cardNodes[45],shop.PreviousBackgroundPage);
            root.gameObject.SetActive(false);
        }
        shop.backgroundPages=pages.ToArray();shop.backgroundCards=cards.ToArray();
        shop.statusText=Text(panel,"ShopStatus","",540,941,840,30,20,Cacao);
    }
    private static void BuildResults(Transform canvas)
    {
        var root=Panel("Results",canvas);grid.gameOverPanel=root.gameObject;
        var win=Panel("Win",root);controller.winPanel=win.gameObject;
        var nodes=Render(6,win,80);nodes[80].GetComponent<Image>().raycastTarget=true;
        grid.textTitle=Label(nodes,83);controller.winLevelText=Label(nodes,85);grid.textFinalScore=controller.winScoreText=Label(nodes,90);
        grid.rewardText=controller.winRewardText=Label(nodes,92);
        grid.btnNextLevel=Button(nodes[93],controller.Continue);controller.continueText=Label(nodes,94);
        Button(nodes[84],controller.Home);Button(nodes[95],controller.Home);Button(nodes[96],controller.Retry);
        controller.bonusRewardText=Text(win,"BonusBoosters","",700,601,520,30,23,Cacao);
        var lose=Panel("Lose",root);controller.losePanel=lose.gameObject;
        var lost=Render(7,lose,80);lost[80].GetComponent<Image>().raycastTarget=true;
        controller.loseIcon=lost[85].GetComponent<Image>();
        Button(lost[84],controller.Home);Button(lost[87],controller.Retry);Button(lost[89],controller.Home);
        grid.levelOneWinBanner=null;grid.winTrophy=null;
        root.gameObject.SetActive(false);
    }
}
