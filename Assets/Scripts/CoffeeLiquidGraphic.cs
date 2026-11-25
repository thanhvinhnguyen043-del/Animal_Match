using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CoffeeLiquidGraphic : MaskableGraphic
{
    public override Texture mainTexture=>Texture2D.whiteTexture;
    private float level,phase,energy;
    public void SetLevel(float amount,float time,float impulse) {level=amount;phase=time;energy=impulse;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();if(level<=0)return;var r=rectTransform.rect;float height=Mathf.Lerp(3,r.height-4,level);const int segments=40;
        for(int i=0;i<=segments;i++)
        {
            float x=i/(float)segments;float wave=(Mathf.Sin(x*9+phase)*.65f+Mathf.Sin(x*16-phase*1.4f)*.35f)*(1+energy*7)*Mathf.Sin(x*Mathf.PI);
            float y=Mathf.Clamp(r.yMin+height+wave,r.yMin+2,r.yMax-2);var v=UIVertex.simpleVert;
            v.position=new Vector2(r.xMin+x*r.width,r.yMin);v.color=new Color(.24f,.09f,.025f);mesh.AddVert(v);
            v.position=new Vector2(r.xMin+x*r.width,y-3);v.color=new Color(.48f,.22f,.065f);mesh.AddVert(v);
            v.position=new Vector2(r.xMin+x*r.width,y);v.color=new Color(.93f,.65f,.29f);mesh.AddVert(v);
            if(i>0){int a=(i-1)*3,b=i*3;mesh.AddTriangle(a,b,b+1);mesh.AddTriangle(a,b+1,a+1);mesh.AddTriangle(a+1,b+1,b+2);mesh.AddTriangle(a+1,b+2,a+2);}
        }
    }
}
