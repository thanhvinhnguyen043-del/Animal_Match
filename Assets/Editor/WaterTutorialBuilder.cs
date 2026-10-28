using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class WaterTutorialBuilder
{
    private static readonly Color Brown=new Color(.42f,.235f,.13f);
    private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    private static Image Image(Transform parent,string name,string asset,float x,float y,float width,float height)
    {
        var image=Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>();
        if(!string.IsNullOrEmpty(asset)) image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/"+asset+".png");
        image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string text,float x,float y,float width,float height,float size,bool outlined=false)
    {
        var label=Rect(parent,name,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=GameTypography.Current.font;
        label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao "+(outlined ? "Heading" : "Body")+".mat");
        label.color=outlined ? new Color(1,.99f,.92f) : Brown;
        label.text=text;label.fontSize=label.fontSizeMax=size;label.fontSizeMin=size-2;label.enableAutoSizing=true;
        label.alignment=TextAlignmentOptions.Center;label.enableWordWrapping=false;label.raycastTarget=false;return label;
    }
    private static void Slot(Transform parent,string name,float x,float y,float width)
    {
        var image=Rect(parent,name,x,y,width,width).gameObject.AddComponent<RoundedImage>();
        image.cornerRadius=width*.2f;image.color=new Color(1,.965f,.9f);image.raycastTarget=false;
        var outline=image.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.62f,.4f,.24f);outline.effectDistance=new Vector2(3,-3);
    }
    private static Button Button(Image image,GridManager grid,UnityAction action)
    {
        image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        UnityEventTools.AddPersistentListener(button.onClick,action);
        image.gameObject.AddComponent<UIButtonClickSound>().clickSound=grid.sfxClick;return button;
    }
    public static void Build()
    {
        var controller=Object.FindObjectOfType<CacaoUIController>();
        var previous=controller.GetComponent<WaterTutorialUI>();
        if(previous)
        {
            if(previous.panel) Object.DestroyImmediate(previous.panel);
            Object.DestroyImmediate(previous);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/CacaoUI/Effect__Water.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.maxTextureSize=512;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        var tutorial=controller.gameObject.AddComponent<WaterTutorialUI>();tutorial.grid=controller.grid;
        var panel=new GameObject("WaterTutorial",typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(controller.transform,false);panel.anchorMin=panel.anchorMax=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(1920,1080);
        tutorial.panel=panel.gameObject;
        var backdrop=Image(panel,"ModalBackdrop",null,0,0,1920,1080);backdrop.color=new Color(.12f,.065f,.025f,.65f);backdrop.raycastTarget=true;
        var content=new GameObject("Popup",typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panel,false);content.anchorMin=content.anchorMax=new Vector2(.5f,.5f);content.sizeDelta=new Vector2(1280,900);
        Image(content,"Panel", "Panel__Large",0,35,1280,853.333f);
        Image(content,"Header","Panel__Header",300,0,680,226.667f);
        Text(content,"Title","WATER TILES",370,70,540,80,47,true);
        Text(content,"Category","NEW MECHANIC",400,192,480,40,26);
        Button(Image(content,"Close","Icon__Close",1140,62,94,94),controller.grid,tutorial.Dismiss);
        Slot(content,"WaterSlot",300,235,190);
        tutorial.waterAnimal=Image(content,"WaterAnimal",null,313,248,164,164);
        var water=Image(content,"WaterLayer","Effect__Water",280,215,230,230);water.color=new Color(1,1,1,.6f);
        Text(content,"Plus","+",515,265,60,65,44);
        Slot(content,"NormalSlot",505,324,86);
        tutorial.normalAnimal=Image(content,"NormalAnimal",null,512,331,72,72);
        var arrowObject=Rect(content,"MatchArrow",600,302,80,60).gameObject;
        arrowObject.AddComponent<CanvasRenderer>();
        var arrow=arrowObject.AddComponent<TutorialArrowGraphic>();
        arrow.color=new Color(1,.68f,.25f);arrow.raycastTarget=false;
        Slot(content,"UncoveredSlot",790,235,190);
        tutorial.uncoveredAnimal=Image(content,"UncoveredAnimal",null,803,248,164,164);
        var database=Resources.Load<LevelDatabase>("LevelDatabase");
        tutorial.waterAnimal.sprite=tutorial.normalAnimal.sprite=tutorial.uncoveredAnimal.sprite=database.animalSprites[0];
        Text(content,"BeforeLabel","Water + normal",275,445,300,48,28);
        Text(content,"AfterLabel","Match again",745,445,280,48,28);
        Text(content,"InstructionMatch","Match with a normal animal of the same kind.",165,515,950,45,30);
        Text(content,"InstructionWater","The normal animal clears. The water layer breaks.",165,560,950,43,29);
        Text(content,"InstructionAgain","Match the uncovered animal again to clear it.",165,603,950,43,29);
        Text(content,"SameKindNote","Use the same animal kind for both matches.",190,649,900,38,26);
        Image(content,"TimerIcon","Icon__Timer",493,691,32,32);
        Text(content,"TimerPaused","Timer paused",525,684,250,44,26);
        var gotIt=Button(Image(content,"GotItButton","Button__Primary",410,735,460,153.333f),controller.grid,tutorial.Dismiss);
        var label=Text(gotIt.transform,"Label","GOT IT!",0,0,460,153.333f,43,true);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
        label.rectTransform.offsetMin=new Vector2(12,4);label.rectTransform.offsetMax=new Vector2(-12,-4);
        panel.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }
}
