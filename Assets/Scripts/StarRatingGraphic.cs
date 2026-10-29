using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class StarRatingGraphic : MaskableGraphic
{
    [SerializeField,Range(0,3)] private int stars;
    public int Stars => stars;
    private static Sprite starSprite;
    private Image[] pngStars;
    protected override void OnEnable() {base.OnEnable();if(Application.isPlaying) UsePngStars();}
    public void UsePngStars()
    {
        if(!starSprite) starSprite=Resources.Load<Sprite>("StarBar/StarGold");
        if(!starSprite || pngStars!=null) return;
        pngStars=new Image[3];
        for(int i=0;i<3;i++)
        {
            var existing=transform.Find("StarPNG"+i);
            var image=existing ? existing.GetComponent<Image>() : new GameObject("StarPNG"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(transform,false);
            image.sprite=starSprite;image.preserveAspect=true;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=new Vector2(i/3f,0);rect.anchorMax=new Vector2((i+1)/3f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;pngStars[i]=image;
        }
        SetStars(stars);
    }
    public void SetStars(int value) {stars=Mathf.Clamp(value,0,3);raycastTarget=false;if(pngStars!=null) for(int i=0;i<3;i++) pngStars[i].color=i<stars ? Color.white : new Color(.48f,.43f,.37f);SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();if(pngStars!=null) return;var rect=rectTransform.rect;
        float radius=Mathf.Min(rect.height*.46f,rect.width/7.2f);
        for(int index=0;index<3;index++)
        {
            var center=new Vector2(rect.center.x+(index-1)*radius*2.5f,rect.center.y);
            Star(mesh,center,radius,new Color(.46f,.27f,.12f));
            Star(mesh,center,radius*.8f,index<stars ? new Color(1,.73f,.12f) : new Color(.83f,.78f,.68f));
            if(index<stars) Star(mesh,center+new Vector2(-radius*.08f,radius*.12f),radius*.43f,new Color(1,.95f,.55f));
        }
    }
    private static void Star(VertexHelper mesh,Vector2 center,float radius,Color tint)
    {
        int start=mesh.currentVertCount;Vertex(mesh,center,tint);
        for(int side=0;side<=10;side++)
        {
            float angle=Mathf.PI*.5f-side*Mathf.PI/5;
            Vertex(mesh,center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*(side%2==0 ? 1 : .46f),tint);
            if(side>0) mesh.AddTriangle(start,start+side,start+side+1);
        }
    }
    private static void Vertex(VertexHelper mesh,Vector2 position,Color color)
    {var vertex=UIVertex.simpleVert;vertex.position=position;vertex.color=color;mesh.AddVert(vertex);}
}
