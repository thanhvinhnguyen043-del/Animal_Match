using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CoffeePourGraphic : MaskableGraphic
{
    private float amount, flow, phase;
    public override Texture mainTexture=>Texture2D.whiteTexture;
    public void SetPour(float level,float strength,float time)
    {amount=level;flow=strength;phase=time;SetVerticesDirty();}
    private static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {
        int n=mesh.currentVertCount;
        mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(b,color,Vector2.zero);
        mesh.AddVert(c,color,Vector2.zero);mesh.AddVert(d,color,Vector2.zero);
        mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(flow>0)
        {
            float w=2.7f*flow;
            Quad(mesh,new Vector2(144-w,-257),new Vector2(144+w,-257),
                new Vector2(144+w,-274),new Vector2(144-w,-274),new Color(.35f,.13f,.04f));
            Quad(mesh,new Vector2(143,-257),new Vector2(144,-257),
                new Vector2(144,-274),new Vector2(143,-274),new Color(.87f,.55f,.25f,flow));
        }
        if(amount<=0)return;
        int first=mesh.currentVertCount;
        mesh.AddVert(new Vector2(143,-274),new Color(.42f,.2f,.06f),Vector2.zero);
        for(int i=0;i<=32;i++)
        {
            float a=i*Mathf.PI*2/32;
            mesh.AddVert(new Vector2(143+Mathf.Cos(a)*25, -274+Mathf.Sin(a)*3.3f),
                new Color(.88f,.61f,.28f,amount),Vector2.zero);
            if(i>0)mesh.AddTriangle(first,first+i,first+i+1);
        }
        for(int strand=0;strand<3;strand++)
            for(int segment=0;segment<10;segment++)
            {
                float t=segment/10f, next=(segment+1)/10f;
                float x=137+strand*6+Mathf.Sin(t*5+phase+strand)*2.8f;
                float nx=137+strand*6+Mathf.Sin(next*5+phase+strand)*2.8f;
                var tint=new Color(1,.93f,.77f,amount*.45f*(1-t));
                Quad(mesh,new Vector2(x,-268+t*24),new Vector2(x+1.2f,-268+t*24),
                    new Vector2(nx+1.2f,-268+next*24),new Vector2(nx,-268+next*24),tint);
            }
    }
}
