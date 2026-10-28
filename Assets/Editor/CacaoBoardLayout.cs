using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CacaoBoardLayout
{
    public static string NameForNode(int index,string fallback)
    {
        switch(index)
        {
            case 2: return "ScorePlate";
            case 4: return "ScoreValue";
            case 5: return "LevelPlate";
            case 6: return "LevelLabel";
            case 7: return "CoinPlate";
            case 9: return "CoinValue";
            case 15: return "TimerValue";
            case 68: return "ShuffleButton";
            case 69: return "HintButton";
            case 74: return "ShuffleBadge";
            case 75: return "ShuffleCount";
            case 76: return "HintBadge";
            case 77: return "HintCount";
            case 79: return "PairsValue";
            default: return fallback;
        }
    }
    private static void Place(Transform parent,string name,float x,float y,float width,float height)
    {
        var rect=(RectTransform)parent.Find(name);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);
        rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);
        rect.sizeDelta=new Vector2(width,height);
    }
    public static void Apply()
    {
        const string path="Assets/Art/CacaoUI/Frame__BoardAdaptive.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spriteBorder=new Vector4(160,160,160,160);
        importer.spritePixelsPerUnit=100;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.isReadable=false;
        importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        var controller=Object.FindObjectOfType<CacaoUIController>();
        var panel=controller.ui.gameplayPanel;
        controller.boardFrame=panel.Find("Frame__Board").GetComponent<Image>();
        controller.boardFrame.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        controller.boardFrame.type=Image.Type.Sliced;
        controller.boardFrame.preserveAspect=false;
        controller.boardFrame.fillCenter=false;
        controller.boardFrame.pixelsPerUnitMultiplier=2.5f;
        controller.boardFrame.raycastTarget=false;
        controller.boardBacking=(RectTransform)panel.Find("Board__Backing");
        controller.boardBacking.GetComponent<RoundedImage>().cornerRadius=24;
        Place(panel,"ScorePlate",40,180,310,92);
        Place(panel,"Text__SCORE",69,196,140,29);
        Place(panel,"ScoreValue",216,190,100,58);
        Place(panel,"Combo",40,285,310,35);
        Place(panel,"LevelPlate",762,0,396,132);
        Place(panel,"LevelLabel",806,39,308,55);
        Place(panel,"CoinPlate",1590,36,270,90);
        Place(panel,"Icon__Coin",1595,49,68,68);
        Place(panel,"CoinValue",1675,58,140,48);
        Place(panel,"Icon__Pause",1672,180,106,106);
        Place(panel,"Icon__Timer",55,356,70,70);
        Place(panel,"TimerValue",150,368,130,40);
        Place(panel,"Timer__Track",55,450,270,30);
        Place(panel,"TimerSlider",55,450,270,30);
        Place(panel.Find("TimerSlider"),"FillArea",5,5,260,20);
        Place(panel,"ShuffleButton",45,650,270,100);
        Place(panel,"Icon__Shuffle",34,632,120,120);
        Place(panel,"Text__SHUFFLE",163,678,140,32);
        Place(panel,"ShuffleBadge",121,630,43,43);
        Place(panel,"ShuffleCount",121,636,43,32);
        Place(panel,"HintButton",45,815,270,100);
        Place(panel,"Icon__Hint",34,797,120,120);
        Place(panel,"Text__HINT",163,843,100,32);
        Place(panel,"HintBadge",121,795,43,43);
        Place(panel,"HintCount",121,801,43,32);
        var database=controller.grid.levelDatabase ? controller.grid.levelDatabase : Resources.Load<LevelDatabase>("LevelDatabase");
        var level=database.levels[0];
        float width=level.columns*level.tileSize,height=level.rows*level.tileSize;
        Place(panel,"Frame__Board",960-width/2-48,580-height/2-48,width+96,height+96);
        Place(panel,"Board__Backing",960-width/2-20,580-height/2-20,width+40,height+40);
        controller.grid.boardContainer.localScale=Vector3.one;
        controller.grid.boardContainer.anchoredPosition=new Vector2(960,-580);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }
}
