using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class StarRatingGraphic : MaskableGraphic
{
    [SerializeField,Range(0,3)] private int stars;
    public int Stars => stars;
    [SerializeField] private bool milestones;
    private int displayTier;
    private static readonly Sprite[] medalSprites=new Sprite[3];
    private Image[] pngStars;
    private readonly Outline[] starGlows = new Outline[3];
    private readonly Coroutine[] starPulses = new Coroutine[3];
    private readonly Vector3[] restingScales = new Vector3[3];

    public void SetStarsAnimated(int value,int tier=0)
    {
        int previous = stars;
        SetStars(value);
        displayTier=tier>0 ? Mathf.Clamp(tier,1,3) : stars;
        ApplyMedals();
        if (!Application.isPlaying || !isActiveAndEnabled || pngStars == null) return;
        for (int i = previous; i < stars; i++)
        {
            restingScales[i] = pngStars[i].rectTransform.localScale;
            starPulses[i] = StartCoroutine(PulseStar(i));
        }
    }

    private IEnumerator PulseStar(int index)
    {
        const float duration = .48f;
        var image = pngStars[index];
        var rect = image.rectTransform;
        if (!starGlows[index])
        {
            starGlows[index] = image.GetComponent<Outline>();
            if (!starGlows[index]) starGlows[index] = image.gameObject.AddComponent<Outline>();
        }
        var glow = starGlows[index];
        glow.enabled = true;
        glow.useGraphicAlpha = true;
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            rect.localScale = restingScales[index] * (1 + .32f * pulse);
            image.color = Color.white;
            glow.effectColor = new Color(1, .94f, .65f, pulse * .75f);
            glow.effectDistance = Vector2.one * (2 + 3 * pulse);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rect.localScale = restingScales[index];
        image.color = Color.white;
        glow.enabled = false;
        starPulses[index] = null;
    }

    private void ResetPulses()
    {
        for (int i = 0; i < starPulses.Length; i++)
        {
            if (starPulses[i] == null) continue;
            StopCoroutine(starPulses[i]);
            starPulses[i] = null;
            if (starGlows[i]) starGlows[i].enabled = false;
            if (pngStars != null && i<pngStars.Length && pngStars[i]) pngStars[i].rectTransform.localScale = restingScales[i];
        }
    }

    protected override void OnDisable()
    {
        ResetPulses();
        ApplyMedals();
        base.OnDisable();
    }
    protected override void OnEnable() {base.OnEnable();UsePngStars();}
    public void SetMilestones(bool value) {milestones=value;UsePngStars();}
    public void UsePngStars()
    {
        string[] names={"Bronze","Silver","Gold"};
        for(int i=0;i<3;i++) if(!medalSprites[i]) medalSprites[i]=Resources.Load<Sprite>("CoffeeMedals/"+names[i]);
        if(!medalSprites[0] || !medalSprites[1] || !medalSprites[2]) return;
        // Unity can restore a non-null but empty cache after an editor domain reload.
        if(pngStars!=null && pngStars.Length==3 && pngStars[0] && pngStars[1] && pngStars[2]) {ApplyMedals();return;}
        pngStars=new Image[3];
        for(int i=0;i<3;i++)
        {
            var existing=transform.Find("StarPNG"+i);
            var image=existing ? existing.GetComponent<Image>() : new GameObject("StarPNG"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(transform,false);
            image.preserveAspect=true;image.raycastTarget=false;
            var rect=image.rectTransform;if(!existing) {rect.anchorMin=new Vector2(i/3f,0);rect.anchorMax=new Vector2((i+1)/3f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;}pngStars[i]=image;
        }
        ApplyMedals();
    }
    public void SetStars(int value)
    {
        ResetPulses();stars=Mathf.Clamp(value,0,3);displayTier=stars;raycastTarget=false;
        UsePngStars();SetVerticesDirty();
    }
    private void ApplyMedals()
    {
        if(pngStars==null || pngStars.Length!=3) return;
        float size=Mathf.Min(rectTransform.rect.height,rectTransform.rect.width/3);
        int rank=Mathf.Clamp(displayTier>0 ? displayTier : stars,1,3);
        for(int i=0;i<3;i++)
        {
            var image=pngStars[i];if(!image)continue;
            image.sprite=medalSprites[milestones ? i : rank-1];
            image.color=milestones && i>=stars ? new Color(.6f,.57f,.53f,.65f) : Color.white;
            image.gameObject.SetActive(milestones || i<stars);
            if(!milestones)
            {
                var rect=image.rectTransform;
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                rect.sizeDelta=new Vector2(size,size);
                rect.anchoredPosition=new Vector2((i-(stars-1)*.5f)*rectTransform.rect.width/3,0);
            }
        }
    }
    protected override void OnPopulateMesh(VertexHelper mesh) {mesh.Clear();}
}
