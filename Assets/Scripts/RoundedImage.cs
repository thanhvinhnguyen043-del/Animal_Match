using UnityEngine;
using UnityEngine.UI;

public class RoundedImage : Image
{
    [Min(0)] public float cornerRadius=24;
    public override Texture mainTexture => Texture2D.whiteTexture;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        var rect=GetPixelAdjustedRect();
        float radius=Mathf.Clamp(cornerRadius,0,Mathf.Min(rect.width,rect.height)/2);
        mesh.Clear();
        mesh.AddVert(rect.center,color,new Vector2(.5f,.5f));
        var centers=new[]{new Vector2(rect.xMax-radius,rect.yMax-radius),new Vector2(rect.xMin+radius,rect.yMax-radius),
            new Vector2(rect.xMin+radius,rect.yMin+radius),new Vector2(rect.xMax-radius,rect.yMin+radius)};
        int outer=0;
        for(int corner=0;corner<4;corner++)
            for(int segment=0;segment<=8;segment++)
            {
                float angle=(corner*90+segment*90f/8)*Mathf.Deg2Rad;
                var point=centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                mesh.AddVert(point,color,Vector2.zero);
                outer++;
            }
        for(int index=0;index<outer;index++) mesh.AddTriangle(0,index+1,(index+1)%outer+1);
    }
}
