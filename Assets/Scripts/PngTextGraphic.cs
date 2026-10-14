using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PngTextGraphic : MaskableGraphic
{
    public PngLettering lettering;
    public TextMeshProUGUI source;
    public string literal;
    public float capHeight=100;
    public override Texture mainTexture => lettering && lettering.atlas ? lettering.atlas : Texture2D.whiteTexture;
    string previous;
    float previousAlpha=-1;
    Vector2 previousSize;
    protected override void OnEnable() { base.OnEnable(); raycastTarget=false; }
    void LateUpdate()
    {
        string text=source ? source.text : literal;
        float alpha=source ? source.color.a : color.a;
        var size=rectTransform.rect.size;
        if(previous!=text || previousAlpha!=alpha || previousSize!=size)
        {
            previous=text;previousAlpha=alpha;previousSize=size;SetVerticesDirty();
        }
    }
    public float Width(string text)
    {
        float width=0;
        foreach(char c in text.ToUpperInvariant())
        {
            if(c==' ') { width+=.35f;continue; }
            var glyph=lettering.Glyph(c);if(glyph!=null) width+=glyph.width+.045f;
        }
        return Mathf.Max(0,width-.045f);
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();if(!lettering || !lettering.atlas) return;
        string text=(source ? source.text : literal) ?? "";text=text.ToUpperInvariant();
        if(text.Length==0) return;
        var bounds=rectTransform.rect;
        float height=source ? Mathf.Max(10,source.fontSize) : capHeight;
        var lines=new List<string>(text.Split('\n'));
        bool wrap=source && source.enableWordWrapping;
        if(wrap)
        {
            var wrapped=new List<string>();
            foreach(var line in lines)
            {
                string current="";
                foreach(var word in line.Split(' '))
                {
                    string candidate=current.Length==0 ? word : current+" "+word;
                    if(current.Length>0 && Width(candidate)*height>bounds.width) { wrapped.Add(current);current=word; }
                    else current=candidate;
                }
                wrapped.Add(current);
            }
            lines=wrapped;
        }
        float widest=0;foreach(var line in lines)widest=Mathf.Max(widest,Width(line));
        height=Mathf.Min(height,bounds.width/Mathf.Max(.1f,widest),bounds.height/Mathf.Max(1,lines.Count*1.18f));
        float totalHeight=height*(1+(lines.Count-1)*1.18f);
        float top=bounds.center.y+totalHeight/2;
        var tint=new Color(1,1,1,source ? source.color.a : color.a);
        int alignment=source ? ((int)source.alignment & 255) : 2;
        for(int row=0;row<lines.Count;row++)
        {
            float width=Width(lines[row])*height;
            float x=alignment==1 ? bounds.xMin : alignment==4 ? bounds.xMax-width : bounds.center.x-width/2;
            float baseline=top-height-row*height*1.18f;
            foreach(char character in lines[row])
            {
                if(character==' ') {x+=height*.35f;continue;}
                var glyph=lettering.Glyph(character);if(glyph==null) continue;
                float w=glyph.width*height,h=glyph.height*height;int index=mesh.currentVertCount;
                mesh.AddVert(new Vector3(x,baseline,0),tint,new Vector2(glyph.uv.xMin,glyph.uv.yMin));
                mesh.AddVert(new Vector3(x,baseline+h,0),tint,new Vector2(glyph.uv.xMin,glyph.uv.yMax));
                mesh.AddVert(new Vector3(x+w,baseline+h,0),tint,new Vector2(glyph.uv.xMax,glyph.uv.yMax));
                mesh.AddVert(new Vector3(x+w,baseline,0),tint,new Vector2(glyph.uv.xMax,glyph.uv.yMin));
                mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);x+=w+height*.045f;
            }
        }
    }
}
