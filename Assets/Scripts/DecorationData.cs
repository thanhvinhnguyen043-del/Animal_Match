using System;
using System.Collections.Generic;
using UnityEngine;

public enum DecorationSlot { None, Plants, Hanging, Wall, SideTable, Counter, Floor, Chair, Hats }

[Serializable]
public class DecorationSlotAnchor
{
    public string backgroundId;
    public DecorationSlot slot;
    public Vector2 position;
    public Vector2 pivot;
    public Vector2 maxSize;
}

public static class DecorationSlots
{
    public const int Count=8;
    public static string Label(DecorationSlot slot)
    {
        switch(slot)
        {
            case DecorationSlot.Plants:return "PLANTS";
            case DecorationSlot.Hanging:return "HANGING";
            case DecorationSlot.Wall:return "WALL ART";
            case DecorationSlot.SideTable:return "SIDE TABLE";
            case DecorationSlot.Counter:return "COUNTER";
            case DecorationSlot.Floor:return "FLOOR";
            case DecorationSlot.Chair:return "CHAIR";
            case DecorationSlot.Hats:return "HATS";
            default:return "NONE";
        }
    }
    public static DecorationSlotAnchor Anchor(string backgroundId,DecorationSlot slot)
    {
        var anchors=EconomyStore.Config.decorationSlots;
        if(anchors!=null) foreach(var anchor in anchors)
            if(anchor.backgroundId==backgroundId && anchor.slot==slot) return anchor;
        return Default(backgroundId,slot);
    }
    public static DecorationSlotAnchor Default(string backgroundId,DecorationSlot slot)
    {
        Vector2 position=new Vector2(.26f,.33f),pivot=new Vector2(.5f,0),size=new Vector2(360,470);
        switch(slot)
        {
            case DecorationSlot.Hanging:position=new Vector2(.78f,.97f);pivot=new Vector2(.5f,1);size=new Vector2(600,250);break;
            case DecorationSlot.Wall:position=new Vector2(.82f,.7f);pivot=new Vector2(.5f,.5f);size=new Vector2(190,240);break;
            case DecorationSlot.SideTable:position=new Vector2(.15f,.44f);size=new Vector2(240,250);break;
            case DecorationSlot.Counter:position=new Vector2(.935f,.4f);size=new Vector2(220,205);break;
            case DecorationSlot.Floor:position=new Vector2(.36f,.08f);size=new Vector2(250,210);break;
            case DecorationSlot.Chair:position=new Vector2(.31f,.055f);size=new Vector2(220,250);break;
            case DecorationSlot.Hats:position=new Vector2(.18f,.26f);size=new Vector2(180,145);break;
        }
        if(backgroundId=="japan")
        {
            if(slot==DecorationSlot.Plants) position=new Vector2(.28f,.25f);
            if(slot==DecorationSlot.SideTable) position=new Vector2(.15f,.425f);
            if(slot==DecorationSlot.Counter) position=new Vector2(.935f,.38f);
            if(slot==DecorationSlot.Hats) position=new Vector2(.13f,.23f);
        }
        if(backgroundId=="winter" && slot==DecorationSlot.SideTable) position=new Vector2(.155f,.425f);
        if(slot==DecorationSlot.Wall && backgroundId!="cafe")
        {
            position=new Vector2(.94f,.6f);size=new Vector2(150,170);
            if(backgroundId=="japan") position=new Vector2(.19f,.87f);
            if(backgroundId=="tet" || backgroundId=="jungle") position=new Vector2(.27f,.87f);
        }
        if(backgroundId=="halloween" && slot==DecorationSlot.Plants) position=new Vector2(.33f,.33f);
        return new DecorationSlotAnchor {backgroundId=backgroundId,slot=slot,position=position,pivot=pivot,maxSize=size};
    }
}

[Serializable]
public class DecorationItem
{
    public string id;
    public string displayName;
    public string skin;
    public int price=300;
    public Sprite sprite;
    public Vector2 size=new Vector2(220,280);
    public DecorationSlot slot;
}

[Serializable]
public class DecorationPlacement
{
    public string itemId;
    public float x=.5f;
    public float y=.5f;
    public float scale=1;
    public bool flipped;
    public DecorationPlacement Copy() => new DecorationPlacement {itemId=itemId,x=x,y=y,scale=scale,flipped=flipped};
}

[Serializable]
public class RoomLayout
{
    public string backgroundId;
    public List<DecorationPlacement> items=new List<DecorationPlacement>();
}
