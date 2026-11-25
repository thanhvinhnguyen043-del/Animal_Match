using UnityEngine;
using UnityEngine.UI;

// A soft light glint, deliberately distinct from the five-point level stars.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RewardGlintGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect=rectTransform.rect;
        var center=rect.center;
        var radius=new Vector2(rect.width*.5f,rect.height*.5f);
        // Transparent outer glow, without a badge-like outline.
        mesh.AddVert(center,new Color(1,.82f,.35f,color.a*.55f),Vector2.zero);
        for(int i=0;i<=32;i++)
        {
            float angle=i*Mathf.PI*2/32;
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y),
                new Color(1,.73f,.2f,0),Vector2.zero);
            if(i>0)mesh.AddTriangle(0,i,i+1);
        }
        int first=mesh.currentVertCount;
        mesh.AddVert(center,new Color(1,1,.95f,color.a),Vector2.zero);
        for(int i=0;i<=64;i++)
        {
            float angle=i*Mathf.PI*2/64;
            float x=Mathf.Cos(angle),y=Mathf.Sin(angle);
            var point=center+new Vector2(x*x*x*radius.x*.68f,y*y*y*radius.y*.9f);
            mesh.AddVert(point,new Color(1,.94f,.63f,color.a),Vector2.zero);
            if(i>0)mesh.AddTriangle(first,first+i,first+i+1);
        }
    }
}
