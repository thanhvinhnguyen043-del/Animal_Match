using System;
using UnityEngine;

[Serializable]
public class PngGlyph
{
    public string character;
    public Rect uv;
    public float width;
    public float height;
}
[Serializable]
public class PngWord
{
    public string text;
    public Sprite sprite;
}
public class PngLettering : ScriptableObject
{
    public Texture2D atlas;
    public PngGlyph[] glyphs;
    public PngWord[] words;
    public PngGlyph Glyph(char character)
    {
        if(glyphs!=null) foreach(var glyph in glyphs) if(glyph.character.Length>0 && glyph.character[0]==character) return glyph;
        return null;
    }
    public Sprite Word(string text)
    {
        if(words!=null) foreach(var word in words) if(word.text==text) return word.sprite;
        return null;
    }
}
