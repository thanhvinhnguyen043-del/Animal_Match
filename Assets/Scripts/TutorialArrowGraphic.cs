using UnityEngine;
using UnityEngine.UI;

public class TutorialArrowGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect=rectTransform.rect;
        float w=rect.width,h=rect.height;
        var outline=new Color(.42f,.235f,.13f);
        Quad(mesh,rect,new Vector2(0,h*.32f),new Vector2(w*.59f,h*.68f),outline);
        Triangle(mesh,rect,new Vector2(w*.57f,0),new Vector2(w,h*.5f),new Vector2(w*.57f,h),outline);
        Quad(mesh,rect,new Vector2(4,h*.32f+4),new Vector2(w*.59f+3,h*.68f-4),color);
        Triangle(mesh,rect,new Vector2(w*.57f+4,8),new Vector2(w-6,h*.5f),new Vector2(w*.57f+4,h-8),color);
    }
    private static void Vertex(VertexHelper mesh,Rect rect,Vector2 point,Color tint)
    {
        var vertex=UIVertex.simpleVert;vertex.position=rect.min+point;vertex.color=tint;mesh.AddVert(vertex);
    }
    private static void Quad(VertexHelper mesh,Rect rect,Vector2 min,Vector2 max,Color tint)
    {
        int start=mesh.currentVertCount;
        Vertex(mesh,rect,min,tint);Vertex(mesh,rect,new Vector2(min.x,max.y),tint);
        Vertex(mesh,rect,max,tint);Vertex(mesh,rect,new Vector2(max.x,min.y),tint);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
    private static void Triangle(VertexHelper mesh,Rect rect,Vector2 a,Vector2 b,Vector2 c,Color tint)
    {
        int start=mesh.currentVertCount;
        Vertex(mesh,rect,a,tint);Vertex(mesh,rect,b,tint);Vertex(mesh,rect,c,tint);mesh.AddTriangle(start,start+1,start+2);
    }
}
