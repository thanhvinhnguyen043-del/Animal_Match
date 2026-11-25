using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class RewardHaloGraphic : MaskableGraphic
{
    private const int Segments=64;
    private static readonly float[] Radii={0,.22f,.5f,.78f,1};
    private static readonly Color[] Tints={
        new Color(1,.98f,.8f,.98f),new Color(1,.92f,.57f,.86f),
        new Color(1,.77f,.3f,.48f),new Color(1,.65f,.18f,.15f),
        new Color(1,.58f,.12f,0)
    };

    private void Update()
    {
        if(Application.isPlaying) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect=rectTransform.rect;
        var center=rect.center;
        var radius=new Vector2(rect.width*.5f,rect.height*.5f);
        float pulse=Application.isPlaying ? .93f+.07f*Mathf.Sin(Time.unscaledTime*2) : 1;
        for(int ring=0;ring<Radii.Length;ring++)
        {
            var tint=Tints[ring];tint.a*=pulse;
            for(int side=0;side<=Segments;side++)
            {
                float angle=side*Mathf.PI*2/Segments;
                mesh.AddVert(center+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y)*Radii[ring],
                    tint,Vector2.zero);
                if(ring==0 || side==0) continue;
                int current=ring*(Segments+1)+side;
                int previous=current-(Segments+1);
                mesh.AddTriangle(previous-1,previous,current);
                mesh.AddTriangle(previous-1,current,current-1);
            }
        }
        float rotation=Application.isPlaying ? Time.unscaledTime*.08f : 0;
        for(int ray=0;ray<14;ray++)
        {
            float angle=ray*Mathf.PI*2/14+rotation;
            int first=mesh.currentVertCount;
            mesh.AddVert(center,new Color(1,1,.8f,.25f*pulse),Vector2.zero);
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle-.065f)*radius.x,Mathf.Sin(angle-.065f)*radius.y),
                new Color(1,.89f,.45f,0),Vector2.zero);
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle+.065f)*radius.x,Mathf.Sin(angle+.065f)*radius.y),
                new Color(1,.89f,.45f,0),Vector2.zero);
            mesh.AddTriangle(first,first+1,first+2);
        }
    }
}
