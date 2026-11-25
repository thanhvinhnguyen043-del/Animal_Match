using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(330)]
public sealed class GameplayCompanionUI : MonoBehaviour
{
    private ProgressUI progress;
    private GameManager game;
    private Image character,hat;
    private GameObject bubble;
    private TextMeshProUGUI speech;
    private Coroutine talking;
    private Sprite trackSprite,fillSprite;
    private RectTransform motion;
    private Vector2 restPosition;
    private float idleClock,celebration=-1,celebrationLength;
    private int hops;
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);GameplayHudStyle.Place(rect,x,y,w,h);return rect;
    }
    private Image Art(Transform parent,string name,float x,float y,float w,float h)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private void Start()
    {
        PrepareSceneView();
        game=progress.grid.Session;
        RefreshCharacter();game.MatchResolved+=Match;game.StateChanged+=StateChanged;game.LevelStarted+=ResetCompanion;EconomyStore.InventoryChanged+=RefreshCharacter;
    }
    public void PrepareSceneView()
    {
        progress=GetComponent<ProgressUI>();
        var saved=progress.ui.gameplayPanel.Find("CompanionMotion");
        if(saved)
        {
            motion=(RectTransform)saved;restPosition=motion.anchoredPosition;
            character=motion.Find("PlayerCompanion").GetComponent<Image>();
            hat=motion.Find("CompanionHeadwear").GetComponent<Image>();
            bubble=progress.ui.gameplayPanel.Find("CompanionSpeech").gameObject;
            speech=bubble.transform.Find("Words").GetComponent<TextMeshProUGUI>();
            bubble.SetActive(false);
            PreparePedestal();
            return;
        }
        ArrangeProgress();
        var root=progress.ui.gameplayPanel;
        if(progress.grid.comboText)
        {
            GameplayHudStyle.Place(progress.grid.comboText.rectTransform,45,505,300,95);progress.grid.comboText.fontSizeMax=35;progress.grid.comboText.alignment=TextAlignmentOptions.Center;
        }
        motion=Rect(root,"CompanionMotion",1490,485,370,440);
        motion.pivot=new Vector2(.5f,0);motion.anchoredPosition+=new Vector2(185,-440);restPosition=motion.anchoredPosition;
        character=Art(motion,"PlayerCompanion",0,0,370,440);
        hat=Art(motion,"CompanionHeadwear",80,10,210,135);
        var panel=Art(root,"CompanionSpeech",1475,390,380,122);CafeComponentArt.Apply(panel,"Combo_Speech_Bubble");bubble=panel.gameObject;
        speech=Rect(panel.transform,"Words",43,10,320,77).gameObject.AddComponent<TextMeshProUGUI>();speech.font=GameTypography.Current.font;speech.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;
        speech.color=new Color(.35f,.18f,.08f);speech.alignment=TextAlignmentOptions.Center;speech.fontSizeMax=38;speech.fontSizeMin=25;speech.enableAutoSizing=true;speech.raycastTarget=false;
        character.sprite=Resources.Load<Sprite>("PlayerAvatars/Fox");hat.enabled=false;
        bubble.SetActive(false);
        PreparePedestal();
    }

    private void PreparePedestal()
    {
        var root=progress.ui.gameplayPanel;
        var saved=root.Find("CompanionPedestal");
        var pedestal=saved ? saved.GetComponent<Image>() : Art(root,"CompanionPedestal",1470,795,410,154);
        var background=EconomyStore.FindBackground(EconomyStore.Inventory.selectedBackground);
        pedestal.sprite=background!=null && background.companionPedestal
            ? background.companionPedestal : Resources.Load<Sprite>("CompanionArt/Pedestal");
        pedestal.type=Image.Type.Simple;
        pedestal.preserveAspect=true;
        pedestal.raycastTarget=false;
        if(pedestal.transform.GetSiblingIndex()>motion.GetSiblingIndex())
            pedestal.transform.SetSiblingIndex(motion.GetSiblingIndex());
    }

    private Sprite Crop(Sprite source,Rect normalized,bool sliced)
    {
        var r=source.rect;var crop=new Rect(r.x+r.width*normalized.x,r.y+r.height*normalized.y,r.width*normalized.width,r.height*normalized.height);
        return Sprite.Create(source.texture,crop,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect,sliced ? new Vector4(crop.height*.5f,0,crop.height*.5f,0) : Vector4.zero);
    }
    private void ArrangeProgress()
    {
        var root=(RectTransform)progress.goalTitle.transform.parent;GameplayHudStyle.Place(root,510,128,900,135);
        GameplayHudStyle.Place(progress.goalTitle.rectTransform,0,0,900,30);progress.goalTitle.fontSizeMax=23;
        progress.timeGoal.gameObject.SetActive(false);progress.challengeGoal.gameObject.SetActive(false);
        GameplayHudStyle.Place(progress.turns.rectTransform,0,108,900,25);progress.turns.fontSizeMax=20;
        var track=root.Find("TrackPNG").GetComponent<Image>();trackSprite=Crop(track.sprite,new Rect(.02f,.345f,.96f,.31f),true);track.sprite=trackSprite;track.type=Image.Type.Sliced;track.preserveAspect=false;track.pixelsPerUnitMultiplier=trackSprite.rect.height/34;
        GameplayHudStyle.Place(track.rectTransform,0,67,900,34);
        var fill=track.transform.Find("FillPNG").GetComponent<Image>();fillSprite=Crop(fill.sprite,new Rect(.016f,.305f,.968f,.39f),false);fill.sprite=fillSprite;
        fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=new Vector2(5,5);fill.rectTransform.offsetMax=new Vector2(-5,-5);
        GameplayHudStyle.Place(progress.goalStars.rectTransform,0,35,900,68);progress.goalStars.UsePngStars();
        for(int i=0;i<3;i++)
        {
            var star=(RectTransform)progress.goalStars.transform.Find("StarPNG"+i);star.anchorMin=star.anchorMax=new Vector2(0,1);star.pivot=new Vector2(.5f,.5f);star.sizeDelta=new Vector2(68,68);star.anchoredPosition=new Vector2((i+1)*285,-34);
        }
    }
    private void RefreshCharacter()
    {
        PreparePedestal();
        var profile=EconomyStore.Inventory.avatar;string id=profile.selected;string asset=char.ToUpperInvariant(id[0])+id.Substring(1);
        character.sprite=Resources.Load<Sprite>("PlayerAvatars/"+asset);var item=EconomyStore.FindDecoration(profile.hat);hat.sprite=item==null ? null : item.sprite;hat.enabled=item!=null && item.sprite;AvatarFashionRewards.Style(hat);
    }
    private void Match(int points,float bonus,Vector2 position)
    {
        string word=CacaoUIController.ComboPraiseWord(game.Combo);if(word==null) return;
        celebration=0;hops=game.Combo>=8 ? 2 : 1;celebrationLength=hops==2 ? 1.25f : .9f;
        HideSpeech();speech.text=word;bubble.SetActive(true);talking=StartCoroutine(Speak());
    }
    private IEnumerator Speak()
    {
        float elapsed=0;
        while(elapsed<1.8f)
        {
            elapsed+=Time.deltaTime;float scale=elapsed<.18f ? Mathf.Lerp(.85f,1,elapsed/.18f) : 1;bubble.transform.localScale=Vector3.one*scale;yield return null;
        }
        talking=null;bubble.SetActive(false);
    }
    private void Update()
    {
        if(!motion || !game || game.State!=GameState.Playing) return;
        float dt=Mathf.Min(Time.deltaTime,.05f);idleClock+=dt;
        float y=0,angle=0,sx=1,sy=1;
        if(celebration>=0)
        {
            celebration+=dt;float t=Mathf.Clamp01(celebration/celebrationLength);
            if(t<.14f)
            {
                float squash=Mathf.Sin(t/.14f*Mathf.PI*.5f);sx+=.055f*squash;sy-=.055f*squash;
            }
            else if(t<.8f)
            {
                float jump=(t-.14f)/.66f;float arc=Mathf.Abs(Mathf.Sin(jump*Mathf.PI*hops));
                y=arc*(hops==2 ? 34 : 26);sx-=arc*.025f;sy+=arc*.035f;
                angle=Mathf.Sin(jump*Mathf.PI*2)*5;
            }
            else
            {
                float land=(t-.8f)/.2f;float squash=Mathf.Sin(land*Mathf.PI)*(1-land);sx+=squash*.06f;sy-=squash*.06f;
            }
            if(t>=1) celebration=-1;
        }
        else
        {
            float breath=Mathf.Sin(idleClock*2.1f);sx=1-breath*.003f;sy=1+breath*.006f;
            float gesture=idleClock%7.5f;
            if(gesture>6) {float t=(gesture-6)/1.5f;angle=Mathf.Sin(t*Mathf.PI*2)*2.5f*Mathf.Sin(t*Mathf.PI);y=Mathf.Sin(t*Mathf.PI)*2;}
        }
        motion.anchoredPosition=restPosition+Vector2.up*y;motion.localScale=new Vector3(sx,sy,1);motion.localRotation=Quaternion.Euler(0,0,angle);
    }
    private void ResetCompanion()
    {
        HideSpeech();idleClock=0;celebration=-1;
        if(motion) {motion.anchoredPosition=restPosition;motion.localScale=Vector3.one;motion.localRotation=Quaternion.identity;}
    }
    private void StateChanged(GameState state)
    {
        if(state==GameState.Paused) HideSpeech();else if(state!=GameState.Playing) ResetCompanion();
    }
    private void HideSpeech() {if(talking!=null) StopCoroutine(talking);talking=null;if(bubble) bubble.SetActive(false);}
    private void OnDestroy()
    {
        HideSpeech();if(game) {game.MatchResolved-=Match;game.StateChanged-=StateChanged;game.LevelStarted-=ResetCompanion;}EconomyStore.InventoryChanged-=RefreshCharacter;
        if(Application.isPlaying) {if(trackSprite) Destroy(trackSprite);if(fillSprite) Destroy(fillSprite);}
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CompanionSpeechTail : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=rectTransform.rect;var v=UIVertex.simpleVert;v.color=color;v.position=new Vector2(r.xMin,r.yMax);mesh.AddVert(v);v.position=new Vector2(r.xMax,r.yMax);mesh.AddVert(v);v.position=new Vector2(r.xMax*.3f,r.yMin);mesh.AddVert(v);mesh.AddTriangle(0,1,2);
    }
}
