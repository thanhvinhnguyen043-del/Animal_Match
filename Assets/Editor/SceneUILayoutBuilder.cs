using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneUILayoutBuilder
{
    private const int CurrentFrameLayoutVersion = 34;

    static SceneUILayoutBuilder()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += PrepareOpenScene;
        EditorApplication.delayCall += PrepareOpenScene;
    }

    private static T Component<T>(GameObject owner) where T : Component
    {
        var component=owner.GetComponent<T>();
        return component ? component : owner.AddComponent<T>();
    }

    private static void PrepareOpenScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        var progress=Object.FindObjectOfType<ProgressUI>(true);
        if(!progress) return;
        var layout=progress.GetComponent<SceneUILayout>();if(layout && layout.decoratedFramesVersion>=CurrentFrameLayoutVersion) return;
        Apply();
    }

    [MenuItem("Tools/Animal Match/Prepare Saved Scene UI")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var progress=Object.FindObjectOfType<ProgressUI>(true);
        if(!progress || !progress.ui || !progress.grid) return;
        var existing=progress.GetComponent<SceneUILayout>();
        if(existing && existing.starFill) {UpgradeFrames(progress,existing);return;}
        var layout=Component<SceneUILayout>(progress.gameObject);
        layout.starFill=progress.PrepareSceneView();
        Component<GameplayCompanionUI>(progress.gameObject).PrepareSceneView();
        Component<CoffeeMatchEffects>(progress.gameObject).PrepareSceneView();
        Component<PlayerAvatarUI>(progress.gameObject).PrepareSceneView();
        Component<LeaderboardUI>(progress.gameObject).PrepareSceneView();
        Component<StarGoalTutorialUI>(progress.gameObject).PrepareSceneView();
        var controller=progress.GetComponent<CacaoUIController>();
        var database=progress.grid.levelDatabase ? progress.grid.levelDatabase : Resources.Load<LevelDatabase>("LevelDatabase");
        if(controller && database && database.levels.Count>0)
        {
            var level=database.levels[0];
            controller.PrepareBoardView(level.columns*level.tileSize,level.rows*level.tileSize);
            controller.timerText.text=ProgressUI.Clock(level.timeLimit);
            controller.pairsText.text="0 / "+(level.cells.FindAll(cell=>!cell.empty).Count/2);
            progress.turns.text="TURN 0 / "+level.challenge.turnLimit;
        }
        layout.starFill.fillAmount=0;layout.starFill.enabled=false;
        progress.menuStars.text=progress.levelStars.text="MEDALS  0 / "+(database ? database.levels.Count*3 : 0);
        var select=progress.ui.levelSelectPanel.GetComponent<LevelSelectionUI>();
        if(select) select.PrepareSceneView();
        var daily=progress.GetComponent<DailyRewardUI>();
        if(daily)
        {
            var caption=daily.menuButton.transform.Find("Caption");
            if(caption) caption.GetComponent<TextMeshProUGUI>().text="ACTIVITIES";
        }
        var shop=Object.FindObjectOfType<ShopUI>(true);
        if(shop && shop.panel) CafePanelStyle.Shop(shop.panel.transform);
        var rewards=Object.FindObjectOfType<RoomRewardsUI>(true);
        if(rewards && rewards.panel) CafePanelStyle.Rewards(rewards.panel.transform);
        foreach(var star in progress.GetComponentsInChildren<StarRatingGraphic>(true)) star.UsePngStars();
        Component<CacaoArtSizing>(progress.gameObject).Apply();
        UpgradeFrames(progress,layout);
        TabButtonStyle.ApplyAll(progress.transform);
        PersistSprites(progress);
        EditorSceneManager.MarkSceneDirty(progress.gameObject.scene);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Animal Match/Preview/Achievements")]
    private static void PreviewAchievements() => Preview("Achievements");
    [MenuItem("Tools/Animal Match/Preview/Victory")]
    private static void PreviewVictory()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var progress=Object.FindObjectOfType<ProgressUI>(true);if(!progress)return;
        Apply();
        Component<CafeVictoryUI>(progress.gameObject).PrepareSceneView();
        var controller=progress.GetComponent<CacaoUIController>();
        progress.grid.gameOverPanel.SetActive(true);
        controller.losePanel.SetActive(false);
        controller.winPanel.SetActive(true);
        progress.grid.gameOverPanel.transform.SetAsLastSibling();
        Selection.activeGameObject=controller.winPanel;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.FrameSelected();
        EditorSceneManager.MarkSceneDirty(progress.gameObject.scene);
    }
    [MenuItem("Tools/Animal Match/Preview/Play Collect Decorate")]
    private static void PreviewRoomRewards() => Preview("RoomRewards");
    [MenuItem("Tools/Animal Match/Preview/Character Wardrobe")]
    private static void PreviewAvatar() => Preview("PlayerProfile");
    [MenuItem("Tools/Animal Match/Preview/Character Headwear")]
    private static void PreviewHeadwear() => Preview("PlayerProfile", true);
    [MenuItem("Tools/Animal Match/Preview/Shop Boosters")]
    private static void PreviewShopBoosters() => Preview("Shop", shopPage: 0);
    [MenuItem("Tools/Animal Match/Preview/Shop Backgrounds")]
    private static void PreviewShopBackgrounds() => Preview("Shop", shopPage: 1);
    [MenuItem("Tools/Animal Match/Preview/Shop Accessories")]
    private static void PreviewShopAccessories() => Preview("Shop", shopPage: 2);
    [MenuItem("Tools/Animal Match/Preview/Leaderboard")]
    private static void PreviewLeaderboard() => Preview("OnlineLeaderboard");
    [MenuItem("Tools/Animal Match/Preview/Cafe Activities")]
    private static void PreviewActivities() => Preview("ActivityHub");

    private static void Preview(string name, bool headwear = false, int shopPage = -1)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var progress=Object.FindObjectOfType<ProgressUI>(true);if(!progress)return;
        Apply();
        var rewards=progress.GetComponent<RoomRewardsUI>();
        var avatar=progress.transform.Find("PlayerProfile");
        var rewardsPanel = rewards ? rewards.panel : null;
        var avatarPanel = avatar ? avatar.gameObject : null;
        var shop = Object.FindObjectOfType<ShopUI>(true);
        var shopPanel = shop ? shop.panel : null;
        var leaderboard = progress.transform.Find("OnlineLeaderboard");
        var leaderboardPanel = leaderboard ? leaderboard.gameObject : null;
        var activities = progress.transform.Find("ActivityHub");
        var activityPanel = activities ? activities.gameObject : null;
        var target = name switch
        {
            "Achievements" => progress.panel,
            "RoomRewards" => rewardsPanel,
            "Shop" => shopPanel,
            "OnlineLeaderboard" => leaderboardPanel,
            "ActivityHub" => activityPanel,
            _ => avatarPanel
        };
        if (!target) return;
        foreach(var panel in new[]{progress.panel,rewardsPanel,avatarPanel,shopPanel,leaderboardPanel,activityPanel})
        {
            if(!panel)continue;Undo.RecordObject(panel,"Preview cafe panel");panel.SetActive(false);
        }
        if (target == avatarPanel && avatar.Find("Page") is Transform page && page)
        {
            foreach (Transform child in page)
            {
                bool characterCard = child.name.StartsWith("Character_");
                bool headwearControl = child.name.StartsWith("Headwear_") || child.name == "PREV" || child.name == "NEXT" || child.name == "PageLabel";
                if (!characterCard && !headwearControl) continue;
                Undo.RecordObject(child.gameObject, "Preview wardrobe tab");
                child.gameObject.SetActive(characterCard ? !headwear : headwear);
            }
        }
        if (target == shopPanel) SetShopPreview(shop, shopPage);
        target.SetActive(true);Selection.activeGameObject=target;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.FrameSelected();
        EditorSceneManager.MarkSceneDirty(target.scene);
    }

    private static void SetShopPreview(ShopUI shop, int page)
    {
        SetPreviewActive(shop.boosterPage, page == 0);
        for (int i = 0; i < shop.backgroundPages.Length; i++)
            SetPreviewActive(shop.backgroundPages[i], page == 1 && i == 0);
        SetPreviewActive(shop.decorationPage, page == 2);
        SetPreviewActive(shop.previousPageButton.gameObject, page != 0);
        SetPreviewActive(shop.nextPageButton.gameObject, page != 0);
        SetPreviewActive(shop.backgroundPageText.gameObject, page != 0);
        var tabs = new[] {shop.boosterTab, shop.backgroundTab, shop.decorationTab};
        for (int i = 0; i < tabs.Length; i++)
        {
            Undo.RecordObject(tabs[i], "Preview shop tab");
            tabs[i].sprite = shop.GetTabSprite(tabs[i], i == page);
            TabButtonStyle.Apply(tabs[i]);
        }
    }

    private static void SetPreviewActive(GameObject target, bool active)
    {
        if (!target || target.activeSelf == active) return;
        Undo.RecordObject(target, "Preview shop page");
        target.SetActive(active);
    }

    private static void UpgradeFrames(ProgressUI progress,SceneUILayout layout)
    {
        if(layout.decoratedFramesVersion>=CurrentFrameLayoutVersion) return;
        if (!ImportFrames("Activities", "AvatarBadge")) return;
        if (!ImportFrames("Shop", "Leaderboard")) return;
        if (layout.decoratedFramesVersion == 0 && !ImportFrames("Achievements", "RoomRewards", "Avatar")) return;

        if (layout.decoratedFramesVersion < 2)
        {
            var rewards=progress.GetComponent<RoomRewardsUI>();
            if (rewards)
            {
                if (layout.decoratedFramesVersion == 0) rewards.PrepareSceneView();
                else CafeFrameLayouts.RoomRewards(rewards);
            }
            Component<PlayerAvatarUI>(progress.gameObject).PrepareProfileSceneView();
            var studio=Object.FindObjectOfType<DecorationStudio>(true);
            if(studio && layout.decoratedFramesVersion == 0) CafePanelStyle.Studio(studio.panel.transform);
        }
        progress.PrepareAchievementSceneView();
        var shop = Object.FindObjectOfType<ShopUI>(true);
        if (shop && shop.panel && layout.decoratedFramesVersion < 3)
        {
            CafeFrameLayouts.Shop(shop);
            if (shop.decorations) shop.decorations.PrepareShopSceneView();
        }
        if (layout.decoratedFramesVersion < 5) Component<LeaderboardUI>(progress.gameObject).PreparePanelSceneView();
        if (layout.decoratedFramesVersion < 4)
        {
            if (shop && shop.panel && shop.panel.transform.Find("ShopWorkspace/ThemedFrame") is RectTransform shopFrame && shopFrame)
            {
                GameplayHudStyle.Place((RectTransform)shopFrame.parent, 115, 145, 1690, 845);
                var image = shopFrame.GetComponent<Image>();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
            Component<PlayerAvatarUI>(progress.gameObject).PrepareMenuBadge();
        }
        if (layout.decoratedFramesVersion < 5 && shop && shop.panel && shop.panel.transform.Find("Panel__Header") is RectTransform shopHeader && shopHeader)
        {
            var image = shopHeader.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CacaoUI/Panel__Header.png");
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            GameplayHudStyle.Place(shopHeader, 726, 70, 468, 156);
        }
        if (layout.decoratedFramesVersion < 5) Component<ActivityUI>(progress.gameObject).PrepareSceneView();
        TabButtonStyle.ApplyAll(progress.transform);
        PersistSprites(progress);
        Component<CafeVictoryUI>(progress.gameObject).PrepareSceneView();
        Component<CoffeeMatchEffects>(progress.gameObject).PrepareSceneView();
        Component<GameplayCompanionUI>(progress.gameObject).PrepareSceneView();
        Component<FirstBoosterRewardUI>(progress.gameObject).PrepareSceneView();
        GameplayProgressStyle.Apply(progress);
        foreach(var rating in progress.GetComponentsInChildren<StarRatingGraphic>(true)) rating.UsePngStars();
        progress.menuStars.text=progress.menuStars.text.Replace("STARS","MEDALS");
        progress.levelStars.text=progress.levelStars.text.Replace("STARS","MEDALS");
        Component<RoomRewardsUI>(progress.gameObject).PrepareSceneView();
        CafeFrameLayouts.RoomRewards(progress.GetComponent<RoomRewardsUI>());
        Component<PlayerAvatarUI>(progress.gameObject).PrepareProfileSceneView();
        if(shop) CafeFrameLayouts.Shop(shop);
        if(shop && shop.decorations) shop.decorations.PrepareStudioSceneView();
        PersistSprites(progress);
        layout.decoratedFramesVersion=CurrentFrameLayoutVersion;
        EditorSceneManager.MarkSceneDirty(progress.gameObject.scene);AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(progress.gameObject.scene);SceneView.RepaintAll();
    }

    private static bool ImportFrames(params string[] names)
    {
        foreach(string name in names)
        {
            string path="Assets/Resources/CafeFrames/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer) {AssetDatabase.ImportAsset(path);importer=AssetImporter.GetAtPath(path) as TextureImporter;}
            if(!importer) return false;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteBorder.x > 0) continue;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;
            importer.spriteBorder = name == "AvatarBadge" ? Vector4.zero : new Vector4(texture.width*.18f,texture.height*.25f,texture.width*.18f,texture.height*.25f);
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=name == "AvatarBadge" ? 512 : 2048;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        return true;
    }

    private static void PersistSprites(ProgressUI progress)
    {
        const string folder="Assets/Art/SceneUI";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var saved=new Dictionary<Sprite,Sprite>();
        foreach(var image in progress.GetComponentsInChildren<Image>(true))
        {
            var source=image.sprite;
            if(!source || EditorUtility.IsPersistent(source)) continue;
            if(!saved.TryGetValue(source,out var sprite))
            {
                string texturePath=AssetDatabase.GetAssetPath(source.texture);
                if(string.IsNullOrEmpty(texturePath)) throw new System.InvalidOperationException("UI texture must be saved: "+image.name);
                string key=AssetDatabase.AssetPathToGUID(texturePath)+source.rect.ToString("F4")+source.border.ToString("F4");
                string path=folder+"/"+Hash128.Compute(key)+".asset";
                sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(!sprite)
                {
                    sprite=Object.Instantiate(source);sprite.name=source.name;
                    AssetDatabase.CreateAsset(sprite,path);
                }
                saved[source]=sprite;
            }
            image.sprite=sprite;EditorUtility.SetDirty(image);
        }
    }
}
