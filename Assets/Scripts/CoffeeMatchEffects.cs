using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(340)]
public sealed class CoffeeMatchEffects : MonoBehaviour
{
    private sealed class Drop
    {
        public Image image;public Vector2 start,burst,control,end;public float age,duration,target,size;public bool active,spray;
    }
    private struct Burst {public Vector3 world;}
    private readonly Queue<Burst> bursts=new Queue<Burst>();
    private readonly List<Drop> pool=new List<Drop>(96);
    private GameManager game;
    private GridManager grid;
    private RectTransform layer,machine;
    private CoffeeLiquidGraphic liquid;
    private TextMeshProUGUI caption;
    private float target,scheduled,shown,phase,energy,pulse;
    private int displayed=-1;
    private bool pouring;
    private CoffeePourGraphic pourGraphic;
    private Vector2 machineRest;
    public bool HasPendingCoffee=>bursts.Count>0 || ActiveFlights>0 || shown<target-.002f;
    public int ActiveFlights {get {int n=0;foreach(var p in pool) if(p.active && !p.spray)n++;return n;}}
    public float Fill=>shown;
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);GameplayHudStyle.Place(r,x,y,w,h);return r;
    }
    private Image Art(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private void Move(Transform root,string name,float x,float y,float w,float h)
    {var child=root.Find(name);if(child) GameplayHudStyle.Place((RectTransform)child,x,y,w,h);}
    private void Start()
    {
        PrepareSceneView();game=grid.Session;
        var root=GetComponent<ProgressUI>().ui.gameplayPanel;
        layer=Rect(root,"CoffeeFlightLayer",0,0,1920,1080);layer.SetAsLastSibling();var sprite=Resources.Load<Sprite>("CoffeeEffects/Drop");
        for(int i=0;i<96;i++) {var image=Art(layer,"Drop"+i,sprite,0,0,30,36);image.gameObject.SetActive(false);pool.Add(new Drop {image=image});}
        game.MatchResolved+=Match;game.LevelStarted+=Reset;game.StateChanged+=StateChanged;Reset();
    }
    public void PrepareSceneView()
    {
        var progress=GetComponent<ProgressUI>();grid=progress.grid;var root=progress.ui.gameplayPanel;
        var saved=root.Find("CoffeeCollector");
        if(saved)
        {
            machine=(RectTransform)saved;liquid=machine.Find("CoffeeTank/Liquid").GetComponent<CoffeeLiquidGraphic>();
            caption=root.Find("CoffeeProgress").GetComponent<TextMeshProUGUI>();
            PreparePour();
            return;
        }
        machine=Rect(root,"CoffeeCollector",40,525,290,363);
        Art(machine,"MachineArt",Resources.Load<Sprite>("CoffeeEffects/Machine"),0,0,290,363);
        var tank=Rect(machine,"CoffeeTank",47,102,192,117);var mask=tank.gameObject.AddComponent<RoundedImage>();mask.cornerRadius=8;mask.color=new Color(1,.94f,.79f);mask.raycastTarget=false;tank.gameObject.AddComponent<Mask>().showMaskGraphic=true;
        liquid=Rect(tank,"Liquid",0,0,192,117).gameObject.AddComponent<CoffeeLiquidGraphic>();liquid.raycastTarget=false;
        var shine=Rect(tank,"GlassReflection",7,4,7,107).gameObject.AddComponent<RoundedImage>();shine.cornerRadius=3;shine.color=new Color(1,1,.95f,.38f);shine.raycastTarget=false;
        for(int i=0;i<3;i++) {var tick=Rect(tank,"LevelMark"+i,166,23+i*30,18,3).gameObject.AddComponent<RoundedImage>();tick.color=new Color(.5f,.27f,.13f,.8f);tick.raycastTarget=false;}
        caption=Rect(root,"CoffeeProgress",40,887,290,33).gameObject.AddComponent<TextMeshProUGUI>();caption.font=GameTypography.Current.font;caption.fontSharedMaterial=progress.timeGoal.fontSharedMaterial;caption.color=new Color(.35f,.18f,.08f);caption.fontSize=23;caption.alignment=TextAlignmentOptions.Center;caption.raycastTarget=false;
        GameplayHudStyle.Place(grid.btnShuffle.GetComponent<RectTransform>(),32,936,96,96);GameplayHudStyle.Place(grid.btnHint.GetComponent<RectTransform>(),201,936,96,96);
        Move(root,"Icon__Shuffle",32,936,96,96);Move(root,"Icon__Hint",201,936,96,96);
        Move(root,"ShuffleBadge",104,923,38,38);Move(root,"ShuffleCount",104,927,38,32);Move(root,"HintBadge",273,923,38,38);Move(root,"HintCount",273,927,38,32);
        Move(root,"Text__SHUFFLE",12,1030,139,32);Move(root,"Text__HINT",178,1030,139,32);Move(root,"Text__PAIRS",48,282,100,36);
        var controller=GetComponent<CacaoUIController>();GameplayHudStyle.Place(controller.pairsText.rectTransform,151,282,170,36);GameplayHudStyle.Place(grid.comboText.rectTransform,40,485,290,37);grid.comboText.fontSizeMax=26;
        caption.text="BREWING  0%";
        PreparePour();
    }
    private void PreparePour()
    {
        var saved=machine.Find("CoffeePour");
        pourGraphic=saved ? saved.GetComponent<CoffeePourGraphic>() :
            Rect(machine,"CoffeePour",0,0,290,363).gameObject.AddComponent<CoffeePourGraphic>();
        pourGraphic.raycastTarget=false;
        pourGraphic.gameObject.SetActive(false);
        machineRest=machine.anchoredPosition;
    }

    public IEnumerator PourFinalCup()
    {
        if(!pourGraphic) PreparePour();
        pouring=true;
        displayed=100;
        caption.text="BREWING YOUR CUP...";
        float age=0;
        while(age<.65f)
        {
            if(game.State!=GameState.Playing && game.State!=GameState.Paused) yield break;
            if(game.State==GameState.Paused){yield return null;continue;}
            float t=age/.65f;
            machine.anchoredPosition=machineRest+new Vector2(Mathf.Sin(t*Mathf.PI*8)*5*(1-t),0);
            machine.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*6)*1.8f*(1-t));
            machine.localScale=Vector3.one*(1+.025f*Mathf.Sin(t*Mathf.PI));
            age+=Time.deltaTime;
            yield return null;
        }
        machine.anchoredPosition=machineRest;
        machine.localRotation=Quaternion.identity;
        machine.localScale=Vector3.one;
        pourGraphic.gameObject.SetActive(true);
        age=0;
        while(age<1.65f)
        {
            if(game.State!=GameState.Playing && game.State!=GameState.Paused) yield break;
            if(game.State==GameState.Paused){yield return null;continue;}
            float t=age/1.65f;
            pourGraphic.SetPour(Mathf.Clamp01(t/.8f),t<.8f ? 1 : 1-(t-.8f)/.2f,age*5);
            liquid.SetLevel(Mathf.Lerp(1,.12f,t),age*5,.3f);
            age+=Time.deltaTime;
            yield return null;
        }
        caption.text="FRESHLY BREWED!";
        age=0;
        while(age<.55f)
        {
            if(game.State!=GameState.Playing && game.State!=GameState.Paused) yield break;
            if(game.State==GameState.Paused){yield return null;continue;}
            pourGraphic.SetPour(1,0,8+age*5);
            age+=Time.deltaTime;
            yield return null;
        }
        pourGraphic.gameObject.SetActive(false);
    }

    private void Match(int points,float bonus,Vector2 position) {bursts.Enqueue(new Burst {world=grid.boardContainer.TransformPoint(position)});}
    private Vector2 Destination()
    {
        var r=liquid.rectTransform;return layer.InverseTransformPoint(r.TransformPoint(new Vector3(r.rect.width*.5f,r.rect.yMin+r.rect.height*Mathf.Lerp(.08f,.95f,shown),0)));
    }
    private Drop Acquire()
    {
        foreach(var p in pool) if(!p.active) return p;
        foreach(var p in pool) if(p.spray) {p.active=false;return p;}
        var oldest=pool[0];foreach(var p in pool) if(p.age>oldest.age)oldest=p;target=Mathf.Max(target,oldest.target);return oldest;
    }
    private void Launch(Vector2 start,float amount,int index,bool spray)
    {
        var p=Acquire();p.active=true;p.spray=spray;p.age=-index*.035f;p.start=start;p.end=Destination();p.target=amount;p.duration=spray ? .35f : .9f+index*.025f;p.size=spray ? Random.Range(9f,16f) : Random.Range(24f,34f);
        float angle=(index*61+20)*Mathf.Deg2Rad;p.burst=start+new Vector2(Mathf.Cos(angle)*Random.Range(24,64),Mathf.Sin(angle)*35+50);
        p.control=Vector2.Lerp(p.burst,p.end,.5f)+new Vector2(Random.Range(-70,70),Random.Range(100,200));p.image.gameObject.SetActive(true);p.image.color=Color.white;p.image.rectTransform.anchoredPosition=start;
    }
    private void Update()
    {
        if(!game || game.State!=GameState.Playing) return;
        float dt=Mathf.Min(Time.deltaTime,.05f);phase+=dt*5;energy=Mathf.MoveTowards(energy,0,dt*2.2f);pulse=Mathf.MoveTowards(pulse,0,dt*3);
        foreach(var p in pool)
        {
            if(!p.active) continue;p.age+=dt;if(p.age<0)continue;
            if(!p.spray) p.end=Destination();
            float t=Mathf.Clamp01(p.age/p.duration);Vector2 pos;
            if(p.spray) {pos=Vector2.Lerp(p.start,p.burst,t)+Vector2.down*(t*t*85);p.image.color=new Color(1,1,1,1-t);}
            else if(t<.2f) pos=Vector2.Lerp(p.start,p.burst,Mathf.Sin(t/.2f*Mathf.PI*.5f));
            else {float u=(t-.2f)/.8f;u=u*u*(3-2*u);pos=(1-u)*(1-u)*p.burst+2*(1-u)*u*p.control+u*u*p.end;}
            p.image.rectTransform.anchoredPosition=pos;float scale=p.spray ? 1-t*.5f : t>.86f ? Mathf.Lerp(1,.35f,(t-.86f)/.14f) : 1;
            p.image.rectTransform.sizeDelta=new Vector2(p.size*scale,p.size*1.2f*scale);p.image.rectTransform.localEulerAngles=new Vector3(0,0,p.spray ? t*130 : Mathf.Sin(t*6)*15);
            if(t>=1) {p.active=false;p.image.gameObject.SetActive(false);if(!p.spray) {target=Mathf.Max(target,p.target);energy=Mathf.Min(1,energy+.48f);pulse=.07f;}}
        }
        if(!pouring) {shown=Mathf.MoveTowards(shown,target,dt*.65f);liquid.SetLevel(shown,phase,energy);machine.localScale=Vector3.one*(1+pulse*.2f);}
        int percentage=Mathf.RoundToInt(shown*100);if(percentage!=displayed) {displayed=percentage;caption.text=percentage==100 ? "COFFEE READY!" : "BREWING  "+percentage+"%";}
    }
    private void LateUpdate()
    {
        if(!game || game.State!=GameState.Playing)return;
        while(bursts.Count>0)
        {
            var burst=bursts.Dequeue();Vector2 start=layer.InverseTransformPoint(burst.world);float progress=grid.ClearProgress;
            for(int i=0;i<6;i++) Launch(start,Mathf.Lerp(scheduled,progress,(i+1)/6f),i,false);
            for(int i=0;i<5;i++) Launch(start,0,i,true);scheduled=Mathf.Max(scheduled,progress);
        }
    }
    private void Reset()
    {
        pouring=false;
        if(pourGraphic) pourGraphic.gameObject.SetActive(false);
        if(machine){machine.anchoredPosition=machineRest;machine.localRotation=Quaternion.identity;}
        bursts.Clear();foreach(var p in pool){p.active=false;p.image.gameObject.SetActive(false);}target=scheduled=shown=energy=pulse=phase=0;displayed=-1;if(liquid)liquid.SetLevel(0,0,0);if(machine)machine.localScale=Vector3.one;if(caption)caption.text="BREWING  0%";
    }
    private void StateChanged(GameState state) {if(state==GameState.Menu || state==GameState.Loading || state==GameState.LevelSelect || state==GameState.Lose)Reset();}
    private void OnDestroy() {if(game){game.MatchResolved-=Match;game.LevelStarted-=Reset;game.StateChanged-=StateChanged;}}
}
