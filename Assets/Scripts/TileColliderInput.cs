using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TileColliderInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private readonly Dictionary<Collider2D,AnimalTile> tiles=new Dictionary<Collider2D,AnimalTile>(64);
    private readonly Dictionary<int,AnimalTile> pressedTiles=new Dictionary<int,AnimalTile>(16);
    private Collider2D[] hits=new Collider2D[16];
    private ContactFilter2D filter;
    private RectTransform boardRect;
    private GridManager grid;
    public int TileLayer { get; private set; }

    public void Configure(GridManager owner,int capacity,Vector2 boardSize)
    {
        grid=owner;
        boardRect=(RectTransform)transform;
        boardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,boardSize.x);
        boardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,boardSize.y);
        int layer=LayerMask.NameToLayer("AnimalTileHits");
        TileLayer=layer>=0 ? layer : gameObject.layer;
        filter=new ContactFilter2D();
        filter.SetLayerMask(1<<TileLayer);
        filter.useTriggers=true;
        if(hits.Length<capacity) hits=new Collider2D[capacity];
        var surface=GetComponent<Image>();
        if(!surface)
        {
            surface=gameObject.AddComponent<Image>();
            surface.color=Color.clear;
        }
        surface.raycastTarget=true;
        surface.canvasRenderer.cullTransparentMesh=false;
    }

    public void Register(AnimalTile tile)
    {
        if(tile && tile.hitCollider) tiles[tile.hitCollider]=tile;
    }
    public void Unregister(AnimalTile tile)
    {
        if(tile && tile.hitCollider) tiles.Remove(tile.hitCollider);
    }

    public AnimalTile Pick(Vector2 screenPoint,Camera eventCamera)
    {
        if(!grid || !grid.Session || grid.Session.State!=GameState.Playing || !gameObject.activeInHierarchy ||
            !RectTransformUtility.ScreenPointToWorldPointInRectangle(boardRect,screenPoint,eventCamera,out var point)) return null;
        Physics2D.SyncTransforms();
        int count=Physics2D.OverlapPoint((Vector2)point,filter,hits);
        AnimalTile selected=null;
        int highest=-1;
        for(int index=0;index<count;index++)
        {
            var collider=hits[index];
            hits[index]=null;
            if(!tiles.TryGetValue(collider,out var tile) || !tile || !tile.CanReceiveColliderClick) continue;
            int sibling=tile.transform.GetSiblingIndex();
            if(sibling>highest) { selected=tile; highest=sibling; }
        }
        return selected;
    }

    public void OnPointerClick(PointerEventData pointer)
    {
        if(pointer.button!=PointerEventData.InputButton.Left) return;
        if(!pressedTiles.TryGetValue(pointer.pointerId,out var pressed)) return;
        pressedTiles.Remove(pointer.pointerId);
        var tile=Pick(pointer.position,pointer.pressEventCamera);
        if(tile && tile==pressed) grid.SelectTile(tile);
    }
    public void OnPointerDown(PointerEventData pointer)
    {
        if(pointer.button==PointerEventData.InputButton.Left)
            pressedTiles[pointer.pointerId]=Pick(pointer.position,pointer.pressEventCamera);
    }
    public void OnPointerUp(PointerEventData pointer)
    {
        if(pointer.pointerCurrentRaycast.gameObject!=gameObject) pressedTiles.Remove(pointer.pointerId);
    }
    private void OnDisable() { pressedTiles.Clear(); }
}
