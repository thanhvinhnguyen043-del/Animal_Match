using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PngTextView : MonoBehaviour
{
    public TextMeshProUGUI source;
    public Material textMaterialOverride;
    public PngLettering lettering;
    public Image wordImage;
    public PngTextGraphic dynamicText;
    public string initialText;
    public Sprite initialSprite;
    public GameObject alternateArtwork;
    string previous;
    string rawText;
    string normalizedText;
    int lineCount = 1;
    bool overrideCaptured;
    Sprite customInitialSprite;
    string customInitialText;
    [SerializeField, HideInInspector] bool hasOriginalTextStyle;
    [SerializeField, HideInInspector] TMP_FontAsset originalFont;
    [SerializeField, HideInInspector] Material originalMaterial;
    [SerializeField, HideInInspector] FontStyles originalFontStyle;
    void OnEnable() { previous=null;Refresh(); }
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if(!source) return;
        if(alternateArtwork)
        {
            alternateArtwork.SetActive(!GameTypography.UseText);
            source.gameObject.SetActive(GameTypography.UseText);
        }
        if(GameTypography.UseText)
        {
            if(!hasOriginalTextStyle)
            {
                originalFont=source.font;
                originalMaterial=source.fontSharedMaterial;
                originalFontStyle=source.fontStyle;
                hasOriginalTextStyle=true;
            }
            var font=GameTypography.Current.font;
            if(source.font!=font) source.font=font;
            var material=textMaterialOverride ? textMaterialOverride : font.material;
            if(source.fontSharedMaterial!=material) source.fontSharedMaterial=material;
            if(source.fontStyle!=FontStyles.Normal) source.fontStyle=FontStyles.Normal;
            source.enabled=true;
            if(wordImage) wordImage.gameObject.SetActive(false);
            if(dynamicText) dynamicText.gameObject.SetActive(false);
            previous=null;
            return;
        }
        if(hasOriginalTextStyle)
        {
            if(source.font!=originalFont) source.font=originalFont;
            if(source.fontSharedMaterial!=originalMaterial) source.fontSharedMaterial=originalMaterial;
            if(source.fontStyle!=originalFontStyle) source.fontStyle=originalFontStyle;
        }
        if(!lettering) return;
        source.enabled=false;
        string raw=source.text ?? "";
        if(normalizedText==null || rawText!=raw)
        {
            rawText=raw;
            normalizedText=raw.ToUpperInvariant().Trim();
            lineCount=1;
            foreach(char character in normalizedText) if(character=='\n') lineCount++;
        }
        string text=normalizedText;
        if(!overrideCaptured)
        {
            overrideCaptured=true;
            if(text==initialText && wordImage && wordImage.sprite && wordImage.sprite!=initialSprite)
            { customInitialSprite=wordImage.sprite;customInitialText=initialText; }
        }
        if(previous!=text)
        {
            previous=text;var sprite=lettering.Word(text);
            if(text==initialText && text==customInitialText && customInitialSprite) sprite=customInitialSprite;
            if(wordImage) { wordImage.gameObject.SetActive(sprite);if(sprite) wordImage.sprite=sprite; }
            if(dynamicText) { dynamicText.gameObject.SetActive(!sprite);dynamicText.lettering=lettering;dynamicText.source=source;dynamicText.SetVerticesDirty(); }
        }
        if(wordImage)
        {
            wordImage.color=new Color(1,1,1,source.color.a);
            var rect=wordImage.rectTransform;var bounds=source.rectTransform.rect;
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta=new Vector2(bounds.width,Mathf.Min(bounds.height,source.fontSize*1.55f*lineCount));
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
