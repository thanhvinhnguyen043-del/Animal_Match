using System.Collections.Generic;
using UnityEngine;

public struct BoardCell
{
    public bool occupied;
    public int animalID;
    public TileState state;
    public bool Selectable => occupied && state != TileState.Hidden && state != TileState.Barrier;
}

public struct BoardMove
{
    public Vector2Int from;
    public Vector2Int to;
}

public sealed class BoardLogic
{
    private BoardCell[,] cells;
    public int Columns => cells.GetLength(0);
    public int Rows => cells.GetLength(1);

    public BoardLogic(int columns, int rows) { cells = new BoardCell[columns, rows]; }
    public void SetCell(int x, int y, BoardCell cell) { cells[x, y] = cell; }
    public BoardCell GetCell(int x, int y) => cells[x, y];
    private bool Inside(Vector2Int p) => p.x >= 0 && p.x < Columns && p.y >= 0 && p.y < Rows;

    public bool CanMatch(Vector2Int a, Vector2Int b)
    {
        if (a == b || !Inside(a) || !Inside(b)) return false;
        var first = cells[a.x,a.y];
        var second = cells[b.x,b.y];
        return first.Selectable && second.Selectable && first.animalID == second.animalID && HasPath(a,b);
    }

    public bool TryFindPair(out Vector2Int first, out Vector2Int second)
    {
        for (int a = 0; a < Columns * Rows; a++)
        {
            var p = new Vector2Int(a % Columns, a / Columns);
            if (!cells[p.x,p.y].Selectable) continue;
            for (int b = a + 1; b < Columns * Rows; b++)
            {
                var q = new Vector2Int(b % Columns, b / Columns);
                if (CanMatch(p,q)) { first=p; second=q; return true; }
            }
        }
        first=second=default;
        return false;
    }

    public bool HasPath(Vector2Int first, Vector2Int second)
    {
        int x1=first.x, y1=first.y, x2=second.x, y2=second.y;
        if (first == second) return false;
        if (x1 == x2 && CheckLine(x1,y1,x2,y2)) return true;
        if (y1 == y2 && CheckLine(x1,y1,x2,y2)) return true;
        if (IsEmpty(x1,y2) && CheckLine(x1,y1,x1,y2) && CheckLine(x1,y2,x2,y2)) return true;
        if (IsEmpty(x2,y1) && CheckLine(x1,y1,x2,y1) && CheckLine(x2,y1,x2,y2)) return true;
        for(int x=x1+1; x<=Columns; x++)
        {
            if(!IsEmpty(x,y1)) break;
            if(IsEmpty(x,y2) && CheckLine(x,y1,x,y2) && CheckLine(x,y2,x2,y2)) return true;
        }
        for(int x=x1-1; x>=-1; x--)
        {
            if(!IsEmpty(x,y1)) break;
            if(IsEmpty(x,y2) && CheckLine(x,y1,x,y2) && CheckLine(x,y2,x2,y2)) return true;
        }
        for(int y=y1+1; y<=Rows; y++)
        {
            if(!IsEmpty(x1,y)) break;
            if(IsEmpty(x2,y) && CheckLine(x1,y,x2,y) && CheckLine(x2,y,x2,y2)) return true;
        }
        for(int y=y1-1; y>=-1; y--)
        {
            if(!IsEmpty(x1,y)) break;
            if(IsEmpty(x2,y) && CheckLine(x1,y,x2,y) && CheckLine(x2,y,x2,y2)) return true;
        }
        return false;
    }

    private bool CheckLine(int x1,int y1,int x2,int y2)
    {
        if(x1==x2 && y1==y2) return true;
        if(x1==x2)
        {
            int step=y1<y2 ? 1 : -1;
            for(int y=y1+step; y!=y2; y+=step) if(!IsEmpty(x1,y)) return false;
            return true;
        }
        if(y1==y2)
        {
            int step=x1<x2 ? 1 : -1;
            for(int x=x1+step; x!=x2; x+=step) if(!IsEmpty(x,y1)) return false;
            return true;
        }
        return false;
    }

    private bool IsEmpty(int x,int y)
    {
        return x<0 || x>=Columns || y<0 || y>=Rows || !cells[x,y].occupied || cells[x,y].state==TileState.Hidden;
    }

    public List<BoardMove> ApplyGravity(GravityDirection direction)
    {
        var result=new List<BoardMove>();
        if(direction==GravityDirection.None) return result;
        bool vertical=direction==GravityDirection.Down || direction==GravityDirection.Up;
        bool reverse=direction==GravityDirection.Down || direction==GravityDirection.Right;
        int length=vertical ? Rows : Columns;
        int lanes=vertical ? Columns : Rows;
        int step=reverse ? -1 : 1;
        int start=reverse ? length-1 : 0;
        for(int lane=0; lane<lanes; lane++)
        {
            int target=start;
            for(int index=start; index>=0 && index<length; index+=step)
            {
                var from=vertical ? new Vector2Int(lane,index) : new Vector2Int(index,lane);
                var cell=cells[from.x,from.y];
                if(!cell.occupied) continue;
                if(cell.state==TileState.Ice || cell.state==TileState.Barrier) { target=index+step; continue; }
                if(index!=target)
                {
                    var to=vertical ? new Vector2Int(lane,target) : new Vector2Int(target,lane);
                    cells[to.x,to.y]=cell;
                    cells[from.x,from.y]=default;
                    result.Add(new BoardMove { from=from,to=to });
                }
                target+=step;
            }
        }
        return result;
    }
}
