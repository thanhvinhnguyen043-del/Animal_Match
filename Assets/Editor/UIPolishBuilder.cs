using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIPolishBuilder
{
    private static void Place(Transform target,float x,float y,float w,float h)
    {
        if(!target) return;var rect=target as RectTransform;if(!rect) return;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
    }
    private static void Surface(Transform root,string name,float x,float y,float w,float h)
    {
        if(!root) return;var old=root.Find(name);if(!old) return;
        var oldImage=old.GetComponent<Image>();if(oldImage && !(oldImage is RoundedImage)) Object.DestroyImmediate(oldImage);
        var surface=old.GetComponent<RoundedImage>();if(!surface) surface=old.gameObject.AddComponent<RoundedImage>();
        surface.enabled=true;surface.cornerRadius=32;surface.color=new Color(1,.967f,.885f);surface.raycastTarget=false;
        var edge=old.GetComponent<Outline>();if(!edge) edge=old.gameObject.AddComponent<Outline>();
        edge.effectColor=new Color(.35f,.18f,.09f);edge.effectDistance=new Vector2(3,-3);
        Place(old,x,y,w,h);
    }
    [MenuItem("Tools/Polish UI Layout And Contrast")]
    public static void Apply()
    {
        var ui=Object.FindObjectOfType<UIManager>();if(!ui) return;
        var heading=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao Heading.mat");
        heading.SetColor("_OutlineColor",new Color(.24f,.105f,.045f,1));heading.SetFloat("_OutlineWidth",.24f);
        heading.SetFloat("_OutlineSoftness",0);EditorUtility.SetDirty(heading);
        foreach(var text in Object.FindObjectsOfType<TextMeshProUGUI>(true))
        {
            if(text.font!=GameTypography.Current.font) continue;
            var color=text.color;
            if(color.r>color.g && color.g>color.b && color.r<.8f)
                text.color=new Color(.29f,.135f,.065f,color.a);
            text.extraPadding=true;text.UpdateMeshPadding();EditorUtility.SetDirty(text);
        }
        var rewards=Object.FindObjectOfType<RoomRewardsUI>();
        if(rewards)
        {
            var root=rewards.panel.transform;Surface(root,"Frame",90,155,1740,850);
            Place(root.Find("Header"),540,15,840,140);Place(root.Find("Title"),660,54,600,62);
            Place(root.Find("Close"),1640,66,190,74);Place(rewards.summary.transform,180,172,1560,46);
            for(int i=0;i<rewards.filters.Length;i++) Place(rewards.filters[i].transform,300+i*335,235,315,65);
            for(int i=0;i<rewards.cards.Length;i++) Place(rewards.cards[i].root.transform,130+i%2*835,325+i/2*267,800,250);
            Place(rewards.notice.transform,170,855,1580,62);
            Place(rewards.previous.transform,510,920,240,70);Place(rewards.next.transform,1170,920,240,70);
            Place(rewards.pageLabel.transform,810,930,300,45);
            rewards.pageLabel.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne/Cacao Body.mat");
            rewards.pageLabel.color=new Color(.29f,.135f,.065f);
        }
        var progress=Object.FindObjectOfType<ProgressUI>();
        if(progress) Surface(progress.panel.transform.Find("Popup"),"Frame",65,165,1630,770);
        var studio=Object.FindObjectOfType<DecorationStudio>();
        if(studio)
        {
            Surface(studio.backgroundPanel.transform,"Frame",150,175,1620,820);
            Place(studio.panel.transform.Find("ChangeBackground"),840,125,380,58);
        }
        var preview=Object.FindObjectOfType<DecorationPreview>();
        if(preview) Surface(preview.panel.transform,"Frame",65,190,1790,830);
        var shop=Object.FindObjectOfType<ShopUI>();
        if(shop)
        {
            foreach(var image in shop.panel.GetComponentsInChildren<Image>(true))
                if(image.sprite && image.sprite.name=="Panel__Large" && image.rectTransform.rect.width>1200)
                    Surface(image.transform.parent,image.name,130,185,1660,830);
        }
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
}
