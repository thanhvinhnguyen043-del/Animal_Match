using UnityEngine;
using UnityEngine.UI;

public static class CafeFrameLayouts
{
    private static void Place(Transform target,float x,float y,float width,float height)
    {
        if(target is RectTransform rect)
        {
            GameplayHudStyle.Place(rect,x,y,width,height);
            var image=target.GetComponent<Image>();
            if(image && image.preserveAspect) {rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition+=new Vector2(width*.5f,-height*.5f);}
        }
    }
    public static Image Panel(Transform root,string style)
    {
        var sprite=Resources.Load<Sprite>("CafeFrames/"+style);
        if(!sprite || !root) return null;
        var old=root.GetComponent<Image>();if(old)old.enabled=false;
        foreach(var outline in root.GetComponents<Outline>())outline.enabled=false;
        foreach(string name in new[]{"CafePanelPNG","WarmTrim","CreamInset"})
        {
            var child=root.Find(name);if(child)child.gameObject.SetActive(false);
        }
        var saved=root.Find("ThemedFrame");
        var image=saved ? saved.GetComponent<Image>() : new GameObject("ThemedFrame",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(root,false);image.transform.SetAsFirstSibling();
        var rect=image.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        image.sprite=sprite;image.type=Image.Type.Sliced;image.color=Color.white;image.raycastTarget=false;
        image.preserveAspect=false;image.pixelsPerUnitMultiplier=sprite.rect.height/Mathf.Max(1,((RectTransform)root).rect.height);
        return image;
    }
    private static void Card(Transform root,float width,float height)
    {
        var old=root.GetComponent<Image>();if(old)old.enabled=false;
        foreach(var outline in root.GetComponents<Outline>())outline.enabled=false;
        var image=RewardFrameArt.Create(root,"CardFrame","Common",0,0,width,height);
        image.transform.SetAsFirstSibling();
    }

    public static void Shop(ShopUI shop)
    {
        var root = shop.panel.transform;
        CafePanelStyle.Shop(root);
        var frame = root.Find("ShopWorkspace");
        Place(frame, 115, 112, 1690, 888);
        Panel(frame, "Shop");

        Place(root.Find("Panel__Header"), 560, 75, 800, 150);
        Place(root.Find("Text__SHOP"), 700, 112, 520, 72);
        Place(root.Find("Icon__Back"), 55, 39, 92, 92);
        var studio = root.Find("OpenStudio");
        var tabs = new[] {shop.boosterTab.transform, shop.backgroundTab.transform, shop.decorationTab.transform, studio};
        for (int i = 0; i < tabs.Length; i++) Place(tabs[i], 280 + i * 347, 302, 330, 60);
        Place(shop.decorationTab.transform.Find("Label"), 8, 2, 314, 56);
        if (studio) Place(studio.Find("Label"), 8, 2, 314, 56);
        Place(shop.shopStock.transform, 400, 363, 1120, 32);
        shop.selectedTabSprite = Resources.Load<Sprite>("ActivityIcons/Primary");
        shop.otherTabSprite = Resources.Load<Sprite>("ActivityIcons/Secondary");
        for (int i = 0; i < tabs.Length; i++)
        {
            if (!tabs[i]) continue;
            var image = tabs[i].GetComponent<Image>();
            image.sprite = i == 0 || i == 3 ? shop.selectedTabSprite : shop.otherTabSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = image.sprite.rect.height / 60f;
        }
        Place(shop.previousPageButton.transform, 460, 820, 255, 64);
        Place(shop.nextPageButton.transform, 1205, 820, 255, 64);
        Place(shop.backgroundPageText.transform, 810, 830, 300, 44);
        Place(shop.statusText.transform, 350, 900, 1220, 35);

        var boosters = shop.boosterPage.transform;
        foreach (Transform child in boosters)
        {
            if (child.name != "Panel__ItemCard") continue;
            var rect = (RectTransform)child;
            float x = rect.anchoredPosition.x < 960 ? 440 : 1020;
            Place(child, x, 405, 460, 396);
            Card(child, 460, 396);
        }
        Place(boosters.Find("Text__HINT"), 480, 461, 380, 43);
        Place(boosters.Find("Text__SHUFFLE"), 1060, 461, 380, 43);
        Place(boosters.Find("Icon__Hint"), 592, 507, 156, 156);
        Place(boosters.Find("Icon__Shuffle"), 1172, 507, 156, 156);
        Place(boosters.Find("Text__Add_1_hint"), 480, 663, 380, 34);
        Place(boosters.Find("Text__Add_1_shuffle"), 1060, 663, 380, 34);
        Place(shop.hintBuyButton.transform, 515, 703, 310, 62);
        Place(shop.shuffleBuyButton.transform, 1095, 703, 310, 62);

        foreach (var item in shop.backgroundCards)
        {
            var card = (RectTransform)item.button.transform.parent;
            float y = -card.anchoredPosition.y < 500 ? 395 : 599;
            Place(card, card.anchoredPosition.x, y, 650, 190);
            Card(card, 650, 190);
            Place(item.preview.transform, 32, 32, 207, 116);
            Place(item.description.transform, 254, 65, 366, 40);
            Place(card.Find("Name"), 254, 23, 366, 36);
            Place(item.button.transform, 257, 110, 360, 58);
        }

        foreach (var item in shop.decorations.shopCards)
        {
            var card = (RectTransform)item.button.transform.parent;
            float y = -card.anchoredPosition.y < 500 ? 395 : 599;
            Place(card, card.anchoredPosition.x, y, 420, 190);
            Card(card, 420, 190);
            Place(item.preview.transform, 22, 25, 117, 128);
            Place(item.title.transform, 150, 26, 248, 37);
            Place(item.detail.transform, 150, 66, 248, 37);
            Place(item.button.transform, 150, 110, 248, 58);
        }
    
        UnifiedCafeLayouts.Shop(shop);
    }
    public static void Achievements(ProgressUI progress)
    {
        if (progress.achievementScroll)
        {
            RestoreAchievementFrame(progress);
            UnifiedCafeLayouts.Achievements(progress);
            return;
        }
        var popup=progress.panel.transform.Find("Popup");
        var frame=popup.Find("Frame");Place(frame,0,100,1760,850);Panel(frame,"Achievements");
        Place(popup.Find("Header"),490,15,780,130);Place(popup.Find("Title"),550,48,660,60);
        Place(progress.closeButton.transform,1605,130,78,78);
        Place(progress.summary.transform,200,225,1360,34);progress.summary.fontSizeMax=25;
        for(int i=0;i<progress.filters.Length;i++)Place(progress.filters[i].transform,150+i*245,270,230,50);
        var viewport = new GameObject("Achievement List", typeof(RectTransform), typeof(Image),
            typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        viewport.SetParent(popup, false);
        Place(viewport, 150, 334, 1460, 472);
        viewport.GetComponent<Image>().color = Color.clear;
        var content = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        Place(content, 0, 0, 1460, 472);
        var scroll = progress.achievementScroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55;

        var rows = new AchievementCard[AchievementCatalog.All.Count];
        for (int i = 0; i < rows.Length; i++)
        {
            var card = rows[i] = i < progress.cards.Length ? progress.cards[i] : CloneAchievement(rows[0], content);
            card.root.name = "AchievementCard" + i;
            card.root.transform.SetParent(content, false);
            Place(card.root.transform, 8, 6 + i * 154, 1444, 142);
            Card(card.root.transform, 1444, 142);
            Place(card.icon.transform, 30, 13, 116, 116);
            Place(card.title.transform, 170, 16, 860, 37);
            Place(card.description.transform, 170, 55, 860, 34);
            Place(card.reward.transform, 170, 90, 480, 30);
            Place(card.fill.transform.parent, 170, 129, 860, 7);
            Place(card.progress.transform, 1090, 18, 298, 32);
            Place(card.button.transform, 1090, 56, 298, 66);
            Place(card.action.transform, 10, 4, 278, 58);
            card.title.fontSize = card.title.fontSizeMax = 34;
            card.description.fontSize = card.description.fontSizeMax = 28;
            card.reward.fontSize = card.reward.fontSizeMax = 24;
            card.progress.fontSize = card.progress.fontSizeMax = 26;
            card.action.fontSize = card.action.fontSizeMax = 26;
        }
        progress.cards = rows;
        progress.previousButton.gameObject.SetActive(false);
        progress.nextButton.gameObject.SetActive(false);
        progress.pageLabel.gameObject.SetActive(false);
        Place(progress.notice.transform, 155, 852, 700, 53);
        Place(progress.milestoneButton.transform, 890, 836, 680, 78);
        Place(progress.milestoneLabel.transform, 15, 7, 650, 31);
        Place(progress.milestoneProgress.transform, 15, 40, 650, 29);
    
        UnifiedCafeLayouts.Achievements(progress);
    }

    private static void RestoreAchievementFrame(ProgressUI progress)
    {
        var popup = progress.panel.transform.Find("Popup");
        var scroll = progress.achievementScroll;
        var frame = popup.Find("Frame");
        if (!frame) frame = scroll.content.Find("Frame");
        if (!frame) return;
        bool migrating = frame.parent != popup;
        frame.SetParent(popup, false);
        frame.SetAsFirstSibling();
        Place(frame, 0, 100, 1760, 850);
        Panel(frame, "Achievements");
        if (!migrating) return;
        Place(scroll.viewport, 150, 345, 1460, 455);
        Place(scroll.content, 0, 0, 1460, AchievementCatalog.All.Count * 154);
        for (int i = 0; i < progress.cards.Length; i++)
            Place(progress.cards[i].root.transform, 8, 6 + i * 154, 1444, 142);
        scroll.content.anchoredPosition = Vector2.zero;
        Place(progress.summary.transform, 200, 215, 1360, 34);
        for (int i = 0; i < progress.filters.Length; i++)
            Place(progress.filters[i].transform, 150 + i * 245, 265, 230, 58);
        Place(progress.notice.transform, 150, 822, 1460, 30);
        Place(progress.milestoneButton.transform, 540, 860, 680, 78);
        Place(progress.milestoneLabel.transform, 15, 7, 650, 31);
        Place(progress.milestoneProgress.transform, 15, 40, 650, 29);
    }

    public static void SizeAchievementList(ProgressUI progress, int rowCount)
    {
        var scroll = progress.achievementScroll;
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(scroll.viewport.rect.height, rowCount * UnifiedCafeLayouts.AchievementRowHeight * 1.2f));
        var position = scroll.content.anchoredPosition;
        position.y = Mathf.Clamp(position.y, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height));
        scroll.content.anchoredPosition = position;
    }

    private static AchievementCard CloneAchievement(AchievementCard source, Transform parent)
    {
        var root = Object.Instantiate(source.root, parent, false);
        Transform Find(Transform original) => original == source.root.transform
            ? root.transform
            : Find(original.parent).GetChild(original.GetSiblingIndex());
        return new AchievementCard
        {
            root = root,
            title = Find(source.title.transform).GetComponent<TMPro.TextMeshProUGUI>(),
            description = Find(source.description.transform).GetComponent<TMPro.TextMeshProUGUI>(),
            reward = Find(source.reward.transform).GetComponent<TMPro.TextMeshProUGUI>(),
            progress = Find(source.progress.transform).GetComponent<TMPro.TextMeshProUGUI>(),
            action = Find(source.action.transform).GetComponent<TMPro.TextMeshProUGUI>(),
            icon = Find(source.icon.transform).GetComponent<Image>(),
            fill = Find(source.fill.transform).GetComponent<Image>(),
            button = Find(source.button.transform).GetComponent<Button>()
        };
    }
    public static void RoomRewards(RoomRewardsUI rewards)
    {
        var root=rewards.panel.transform;var old=root.Find("Frame");if(old)old.gameObject.SetActive(false);
        var surface=root.Find("RoomRewardSurface");
        if(!surface) {surface=new GameObject("RoomRewardSurface",typeof(RectTransform)).transform;surface.SetParent(root,false);}
        Place(surface,65,110,1790,900);Panel(surface,"RoomRewards");surface.SetSiblingIndex(1);
        Place(root.Find("Header"),540,40,840,130);Place(root.Find("Title"),600,72,720,60);
        Place(root.Find("Close"),1660,145,150,60);Place(rewards.summary.transform,220,235,1480,35);
        for(int i=0;i<rewards.filters.Length;i++)Place(rewards.filters[i].transform,235+i*370,285,340,50);
        for(int i=0;i<rewards.cards.Length;i++)
        {
            var card=rewards.cards[i];Place(card.root.transform,205+i%2*775,345+i/2*240,740,220);Card(card.root.transform,740,220);
            Place(card.preview.transform,38,42,165,120);Place(card.title.transform,225,32,285,38);
            Place(card.detail.transform,225,65,285,95);Place(card.button.transform,535,75,170,65);
            Place(card.progress.transform,225,162,475,26);card.title.fontSizeMax=27;card.detail.fontSizeMax=23;card.progress.fontSizeMax=22;
        }
        Place(rewards.notice.transform,210,825,1500,35);Place(rewards.previous.transform,520,875,240,60);
        Place(rewards.next.transform,1160,875,240,60);Place(rewards.pageLabel.transform,810,885,300,38);
    
        UnifiedCafeLayouts.Rooms(rewards);
    }
    public static void Avatar(Transform page)
    {
        Place(page.Find("Surface"),60,95,1800,945);Panel(page.Find("Surface"),"Avatar");
        Place(page.Find("Header"),550,50,820,110);Place(page.Find("Title"),580,72,760,60);
        Place(page.Find("CLOSE"),1620,175,145,55);
        Place(page.Find("PreviewArea"),150,275,510,575);Panel(page.Find("PreviewArea"),"Achievements");
        Place(page.Find("CharacterName"),210,345,390,40);Place(page.Find("CharacterPreview"),240,400,330,330);
        Place(page.Find("HeadwearPreview"),310,380,190,130);Place(page.Find("CharacterLock"),335,470,145,145);
        Place(page.Find("UnlockCondition"),175,755,460,30);Place(page.Find("REMOVE HAT"),150,875,230,62);
        Place(page.Find("SAVE & USE"),400,875,285,62);Place(page.Find("CHARACTERS"),760,245,440,62);
        Place(page.Find("HEADWEAR"),1230,245,440,62);Place(page.Find("Notice"),780,835,890,44);
        Place(page.Find("PREV"),850,895,200,44);Place(page.Find("NEXT"),1395,895,200,44);Place(page.Find("PageLabel"),1100,895,250,44);
        foreach(string name in new[]{"REMOVE HAT","SAVE & USE","CHARACTERS","HEADWEAR","CLOSE","PREV","NEXT"})
        {
            var button=page.Find(name);if(!button)continue;
            var label=button.Find("Label");if(label)Place(label,8,4,((RectTransform)button).rect.width-16,((RectTransform)button).rect.height-8);
        }
    
        UnifiedCafeLayouts.Avatar(page);
    }
}
