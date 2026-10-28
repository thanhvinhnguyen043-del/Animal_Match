using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchPathGraphic : MaskableGraphic
{
    private readonly Vector2[] points=new Vector2[4];
    private int pointCount;
    private float lineWidth;
    public bool IsVisible => pointCount>1 && canvasRenderer.GetAlpha()>0;

    public void ShowPath(IList<Vector2Int> path,Vector2 origin,float cellSize,int columns,int rows)
    {
        pointCount=Mathf.Min(path.Count,points.Length);
        for(int index=0;index<pointCount;index++)
            points[index]=origin+new Vector2(Mathf.Clamp(path[index].x,-.75f,columns-.25f),
                -Mathf.Clamp(path[index].y,-.75f,rows-.25f))*cellSize;
        lineWidth=Mathf.Clamp(cellSize*.1f,8,15);
        raycastTarget=false;maskable=false;
        rectTransform.SetAsLastSibling();
        canvasRenderer.SetAlpha(1);
        SetVerticesDirty();
    }

    public void ClearPath()
    {
        if(pointCount==0) return;
        pointCount=0;SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(pointCount<2) return;
        Stroke(mesh,lineWidth+5,new Color(.15f,.25f,.3f,.95f));
        Stroke(mesh,lineWidth,new Color(.12f,.88f,1));
        Stroke(mesh,lineWidth*.36f,new Color(.91f,1,1));
    }

    private void Stroke(VertexHelper mesh,float width,Color tint)
    {
        float radius=width*.5f;
        for(int index=1;index<pointCount;index++)
        {
            Vector2 a=points[index-1],b=points[index],direction=(b-a).normalized;
            var normal=new Vector2(-direction.y,direction.x)*radius;
            int start=mesh.currentVertCount;
            Vertex(mesh,a-normal,tint);Vertex(mesh,a+normal,tint);
            Vertex(mesh,b+normal,tint);Vertex(mesh,b-normal,tint);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
        for(int index=0;index<pointCount;index++)
        {
            int start=mesh.currentVertCount;
            Vertex(mesh,points[index],tint);
            for(int side=0;side<=12;side++)
            {
                float angle=side*Mathf.PI/6;
                Vertex(mesh,points[index]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint);
                if(side>0) mesh.AddTriangle(start,start+side,start+side+1);
            }
        }
    }

    private static void Vertex(VertexHelper mesh,Vector2 point,Color tint)
    {
        var vertex=UIVertex.simpleVert;vertex.position=point;vertex.color=tint;mesh.AddVert(vertex);
    }
}
