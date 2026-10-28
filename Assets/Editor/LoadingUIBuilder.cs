using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LoadingUIBuilder
{
    private const string ArtPath="Assets/Art/Loading/";
    private static readonly Color Brown=new Color(.42f,.235f,.13f),Cream=new Color(1,.96f,.85f);
    private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+name+".png");
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color color,bool outlined=false)
    {
        var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();label.font=GameTypography.Current.font;
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao "+(outlined ? "Heading" : "Body")+".mat");
        label.text=text;label.color=color;label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-3;label.enableAutoSizing=true;
        label.enableWordWrapping=false;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private static LoadingScreenView Panel(UIManager ui,string name,Sprite background,out RectTransform content)
    {
        var root=Rect(ui.transform,name,0,0,1920,1080);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;root.pivot=new Vector2(.5f,.5f);
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=1000;root.gameObject.AddComponent<GraphicRaycaster>();
        var image=Image(root,"Background",background,0,0,1920,1080);image.preserveAspect=false;image.raycastTarget=true;
        image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
        content=Rect(root,"Content",0,0,1920,1080);content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,.5f);content.anchoredPosition=Vector2.zero;
        return new LoadingScreenView {panel=root.gameObject,heroes=new RectTransform[0]};
    }
    private static void Progress(LoadingScreenView view,Transform content,bool startup)
    {
        var color=startup ? Brown : Cream;
        var status=Text(content,"LoadingStatus",startup ? "LOADING YOUR COZY WORLD..." : "PREPARING YOUR LEVEL...",500,802,780,50,30,color);status.alignment=TextAlignmentOptions.MidlineLeft;
        view.percentage=Text(content,"Percentage","0%",1290,802,130,50,30,color);view.percentage.alignment=TextAlignmentOptions.MidlineRight;
        Image(content,"ProgressTrack",Art("Progress__Track"),500,865,920,74);
        view.fill=Image(content,"ProgressFill",Art("Progress__Fill"),520,881,0,38);view.fill.type=UnityEngine.UI.Image.Type.Sliced;view.fill.preserveAspect=false;
        Text(content,"Helper",startup ? "Getting everything ready for you..." : "Almost ready. Get your paws ready!",430,953,1060,48,27,color);
    }
    private static LoadingScreenView Startup(UIManager ui)
    {
        var view=Panel(ui,"StartupLoading",Art("Background__StartupGarden"),out var content);
        Image(content,"HeroGlow",Art("Hero__Glow"),410,330,1100,520);
        Text(content,"Eyebrow","A COZY MATCHING ADVENTURE",440,112,1040,48,28,Brown);
        Text(content,"Logo","ANIMAL MATCH",310,177,1300,155,120,new Color(1,.96f,.8f),true);
        Text(content,"Tagline","Find a friend. Make a match.",470,315,980,62,36,new Color(.56f,.35f,.2f));
        Image(content,"GoldenLink",Art("Link__Gold"),835,525.36f,250,89.28f);
        var left=Image(content,"LeftFox",Art("Hero__FoxTile"),552,393,350,350);
        var right=Image(content,"RightFox",Art("Hero__FoxTile"),1018,393,350,350);view.heroes=new[]{left.rectTransform,right.rectTransform};
        Image(content,"Paw",Art("Motif__Paw"),918,524,84,84);
        Image(content,"SparkleLeft",Art("Motif__Sparkle"),482,427,52,52);Image(content,"SparkleRight",Art("Motif__Sparkle"),1380,440,66,66);
        Image(content,"SparkleBottomRight",Art("Motif__Sparkle"),1360,692,42,42);Image(content,"SparkleBottomLeft",Art("Motif__Sparkle"),533,700,35,35);
        Progress(view,content,true);view.panel.SetActive(false);return view;
    }
    private static LoadingScreenView Play(UIManager ui,LoadingUI loading)
    {
        var view=Panel(ui,"PlayLoading",Art("Background"),out var content);
        var dim=Image(view.panel.transform,"WarmDim",null,0,0,1920,1080);dim.color=new Color(.23f,.15f,.1f,.52f);
        dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;dim.transform.SetSiblingIndex(1);
        loading.playLevel=Text(content,"Level","LEVEL 1",610,67,700,90,70,Cream,true);
        Text(content,"Subtitle","Your next little adventure is on its way.",390,156,1140,50,28,Cream);
        var panel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/DailyRewards/Panel__DailyWide.png");Image(content,"TipPanel",panel,410.07f,205,1100,619.11f);
        Image(content,"HintIcon",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/Icon__Hint.png"),814,302,52,52);
        Text(content,"TipHeader","A LITTLE TIP",870,299,250,50,30,Brown);
        Image(content,"GoldenLink",Art("Link__Gold"),669,448.04f,165,58.93f);
        Image(content,"LeftTile",Art("Tile__Empty"),552,401,155,155);Image(content,"RightTile",Art("Tile__Empty"),799,401,155,155);
        Image(content,"LeftAnimal",Art("Hero__FoxTile"),562,411,135,135);Image(content,"RightAnimal",Art("Hero__FoxTile"),809,411,135,135);
        Image(content,"Paw",Art("Motif__Paw"),713,446,67,67);
        Text(content,"Caption","FIND A PAIR",560,583,390,58,35,Brown);
        var divider=Image(content,"ColumnDivider",null,988,392,2,247);divider.color=new Color(.85f,.72f,.53f);
        Text(content,"TipTitle","Keep the path\nclear!",1015,383,360,108,39,Brown).enableWordWrapping=true;
        Text(content,"TipBody","Connect two matching\nanimals with a clear path.",1010,512,375,100,28,Brown).enableWordWrapping=true;
        Progress(view,content,false);view.panel.SetActive(false);return view;
    }
    [MenuItem("Tools/Build Loading Screens")]
    public static void Build()
    {
        var ui=Object.FindObjectOfType<UIManager>();var previous=ui.GetComponent<LoadingUI>();
        if(previous) {Object.DestroyImmediate(previous.startup.panel);Object.DestroyImmediate(previous.play.panel);Object.DestroyImmediate(previous);}
        foreach(string path in Directory.GetFiles(ArtPath,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.spritePixelsPerUnit=100;
            importer.maxTextureSize=path.Contains("Background") ? 2048 : path.Contains("Hero__Glow") ? 1024 : path.Contains("Hero__Fox") ? 512 : 1024;
            if(path.Contains("Progress__Fill")) importer.spriteBorder=new Vector4(19,18,19,18);
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        var loading=ui.gameObject.AddComponent<LoadingUI>();loading.ui=ui;loading.grid=Object.FindObjectOfType<GridManager>();ui.loading=loading;
        loading.inputGroups=new[]{ui.mainMenuPanel,ui.levelSelectPanel,ui.gameplayPanel}.Select(p=>
        {
            var group=p.GetComponent<CanvasGroup>();return group ? group : p.gameObject.AddComponent<CanvasGroup>();
        }).ToArray();
        loading.menuPlayButton=ui.mainMenuPanel.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="PlayLatestLevel"));
        loading.startup=Startup(ui);loading.play=Play(ui,loading);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
