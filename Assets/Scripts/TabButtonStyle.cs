using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TabButtonStyle
{
    private static Sprite blue, green;

    public static void Apply(Image image)
    {
        if (!image || UnifiedCafeLayouts.Owns(image.transform)) return;
        if (!blue) blue = Resources.Load<Sprite>("TabButtons/Blue");
        if (!green) green = Resources.Load<Sprite>("TabButtons/Green");
        var source = image.sprite;
        bool selected = source && (source.name.Contains("Primary") || source.name.Contains("Green"));
        image.sprite = selected ? green : blue;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.pixelsPerUnitMultiplier = 1;
        var rect = image.rectTransform;
        float width = Mathf.Min(rect.rect.width, rect.rect.width > 350 ? 320 : 288);
        float height = Mathf.Max(rect.rect.height, width / 4);
        // Preserve the centre so existing groups retain their spacing.
        rect.anchoredPosition += new Vector2((rect.rect.width-width)*(0.5f-rect.pivot.x),
            (height-rect.rect.height)*(0.5f-rect.pivot.y));
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        var label = image.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!label) return;
        var textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14, 5);
        textRect.offsetMax = new Vector2(-14, -5);
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void Place(Transform target, float x, float y, float w, float h)
    {
        if (target is RectTransform rect) GameplayHudStyle.Place(rect, x, y, w, h);
    }

    private static void ArrangeGroups(Transform root)
    {
        var progress = root.GetComponent<ProgressUI>();
        if (progress && progress.ui)
        {
            var menu = progress.ui.mainMenuPanel;
            var inventory = menu.Find("OpenStudio");
            if (inventory)
            {
                var image = inventory.GetComponent<Image>();
                image.sprite = Resources.Load<Sprite>("MenuIcons/Inventory");
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                Place(inventory, 1695, 535, 160, 160);
                var label = inventory.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label) label.gameObject.SetActive(false);
            }
        }
        var rewards = root.GetComponent<RoomRewardsUI>();
        if (rewards && !UnifiedCafeLayouts.Owns(rewards.summary.transform))
        {
            Place(rewards.summary.transform, 220, 205, 1480, 30);
            for (int i = 0; i < rewards.filters.Length; i++)
                Place(rewards.filters[i].transform, 261 + i * 370, 250, 288, 72);
        }
        var shop = root.GetComponent<ShopUI>();
        if (shop && shop.panel && !UnifiedCafeLayouts.Owns(shop.shopStock.transform))
        {
            var tabs = new Transform[] {shop.boosterTab.transform, shop.backgroundTab.transform,
                shop.decorationTab.transform, shop.panel.transform.Find("OpenStudio")};
            for (int i = 0; i < tabs.Length; i++) Place(tabs[i], 301 + i * 347, 277, 288, 72);
            Place(shop.shopStock.transform, 400, 365, 1120, 30);
        }
        var page = root.Find("ActivityHub/ActivityPage");
        if (page)
        {
            string[] names = {"DAILY", "WEEKLY", "FIRST 7 DAYS", "DAILY GIFTS", "CHECK-IN"};
            for (int i = 0; i < names.Length; i++) Place(page.Find(names[i]), 232 + i * 250, 177, 240, 60);
            Place(page.Find("ResetTimer"), 985, 143, 460, 23);
        }
    }

    public static void ApplyAll(Transform root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.name;
            bool filter = name.StartsWith("Filter");
            bool tab = name == "CHARACTERS" || name == "HEADWEAR" ||
                name == "DAILY" || name == "WEEKLY" || name == "FIRST 7 DAYS" ||
                name == "DAILY GIFTS" || name == "CHECK-IN" ||
                name == "Button__BOOSTERS" || name == "Button__BACKGROUNDS" ||
                name == "DecorationTab" || name == "OpenStudio";
            if (name == "OpenStudio" && root.GetComponent<ProgressUI>() is ProgressUI progress &&
                image.transform.parent == progress.ui.mainMenuPanel) continue;
            if ((filter || tab) && image.GetComponent<Button>()) Apply(image);
        }
        ArrangeGroups(root);
        foreach (var studio in root.GetComponentsInChildren<DecorationStudio>(true))
            foreach (var button in studio.slotButtons) if (button) Apply(button.GetComponent<Image>());
    }
}

