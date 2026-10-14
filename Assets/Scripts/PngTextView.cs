using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PngTextView : MonoBehaviour
{
    public TextMeshProUGUI source;
    public PngLettering lettering;
    public Image wordImage;
    public PngTextGraphic dynamicText;
    public string initialText;
    public Sprite initialSprite;
    string previous;
    void OnEnable() { previous=null;Refresh(); }
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if(!source || !lettering) return;
        source.enabled=false;
        string text=(source.text ?? "").ToUpperInvariant().Trim();
        if(previous!=text)
        {
            previous=text;var sprite=lettering.Word(text);
            if(text==initialText && wordImage && wordImage.sprite && wordImage.sprite!=initialSprite) sprite=wordImage.sprite;
            if(wordImage) { wordImage.gameObject.SetActive(sprite);if(sprite) wordImage.sprite=sprite; }
            if(dynamicText) { dynamicText.gameObject.SetActive(!sprite);dynamicText.lettering=lettering;dynamicText.source=source;dynamicText.SetVerticesDirty(); }
        }
        if(wordImage)
        {
            wordImage.color=new Color(1,1,1,source.color.a);
            var rect=wordImage.rectTransform;var bounds=source.rectTransform.rect;
            int lines=text.Split('\n').Length;
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta=new Vector2(bounds.width,Mathf.Min(bounds.height,source.fontSize*1.55f*lines));
        }
    }
    public void ResetVisual() { previous=null;Refresh(); }
    public static void Attach(TextMeshProUGUI label)
    {
        if(!label || label.GetComponent<PngTextView>()) return;
        var style=Resources.Load<PngLettering>("PngLettering");if(!style) return;
        var view=label.gameObject.AddComponent<PngTextView>();view.source=label;view.lettering=style;
        view.initialText=(label.text ?? "").ToUpperInvariant().Trim();view.initialSprite=style.Word(view.initialText);
        var imageObj=new GameObject("WordPng",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));imageObj.transform.SetParent(label.transform,false);
        view.wordImage=imageObj.GetComponent<Image>();view.wordImage.preserveAspect=true;view.wordImage.raycastTarget=false;Stretch(view.wordImage.rectTransform);
        var textObj=new GameObject("PngCharacters",typeof(RectTransform),typeof(CanvasRenderer),typeof(PngTextGraphic));textObj.transform.SetParent(label.transform,false);
        view.dynamicText=textObj.GetComponent<PngTextGraphic>();view.dynamicText.lettering=style;view.dynamicText.source=label;view.dynamicText.raycastTarget=false;Stretch(view.dynamicText.rectTransform);view.Refresh();
    }
    static void Stretch(RectTransform rect) {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
