using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchPathGraphic : MaskableGraphic
{
    private readonly Vector2[] points = new Vector2[4];
    private readonly Vector2[] curve = new Vector2[20];
    private readonly Vector2[] visibleCurve = new Vector2[20];
    private int pointCount, curveCount, visibleCount;
    private float lineWidth, totalLength, reveal;
    public bool IsVisible => pointCount > 1 && canvasRenderer.GetAlpha() > 0;

    public void ShowPath(IList<Vector2Int> path, Vector2 origin, float cellSize, int columns, int rows)
    {
        pointCount = Mathf.Min(path.Count, points.Length);
        for (int i = 0; i < pointCount; i++)
            points[i] = origin + new Vector2(Mathf.Clamp(path[i].x, -.75f, columns - .25f),
                -Mathf.Clamp(path[i].y, -.75f, rows - .25f)) * cellSize;
        lineWidth = Mathf.Clamp(cellSize * .085f, 7, 12);
        BuildCurve(cellSize * .12f);
        reveal = 0;
        raycastTarget = false;
        maskable = false;
        rectTransform.SetAsLastSibling();
        canvasRenderer.SetAlpha(1);
        SetVerticesDirty();
    }

    private void BuildCurve(float radius)
    {
        curveCount = 0;
        totalLength = 0;
        if (pointCount < 2) return;
        curve[curveCount++] = points[0];
        for (int i = 1; i < pointCount - 1; i++)
        {
            Vector2 corner = points[i];
            float trim = Mathf.Min(radius, Vector2.Distance(points[i - 1], corner) * .25f,
                Vector2.Distance(corner, points[i + 1]) * .25f);
            Vector2 entry = corner + (points[i - 1] - corner).normalized * trim;
            Vector2 exit = corner + (points[i + 1] - corner).normalized * trim;
            curve[curveCount++] = entry;
            for (int step = 1; step <= 6; step++)
            {
                float t = step / 6f;
                curve[curveCount++] = (1 - t) * (1 - t) * entry +
                    2 * (1 - t) * t * corner + t * t * exit;
            }
        }
        curve[curveCount++] = points[pointCount - 1];
        for (int i = 1; i < curveCount; i++)
            totalLength += Vector2.Distance(curve[i - 1], curve[i]);
    }

    public void SetReveal(float value)
    {
        reveal = Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
        SetVerticesDirty();
    }

    public void ClearPath()
    {
        if (pointCount == 0) return;
        pointCount = 0;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (pointCount < 2 || reveal <= 0) return;
        visibleCount = 1;
        visibleCurve[0] = curve[0];
        float remaining = totalLength * reveal;
        for (int i = 1; i < curveCount; i++)
        {
            float length = Vector2.Distance(curve[i - 1], curve[i]);
            if (length <= .001f) continue;
            visibleCurve[visibleCount++] = Vector2.Lerp(curve[i - 1], curve[i], Mathf.Clamp01(remaining / length));
            remaining -= length;
            if (remaining <= 0) break;
        }
        // A soft caramel edge surrounds espresso and a thin milk-foam highlight.
        Stroke(mesh, lineWidth + 8, new Color(.86f, .55f, .25f, .16f));
        Stroke(mesh, lineWidth + 3, new Color(.39f, .19f, .08f, .92f));
        Stroke(mesh, lineWidth, new Color(.90f, .63f, .31f));
        Stroke(mesh, lineWidth * .34f, new Color(1f, .94f, .77f));
    }

    private void Stroke(VertexHelper mesh, float width, Color tint)
    {
        float radius = width * .5f;
        for (int i = 1; i < visibleCount; i++)
        {
            Vector2 a = visibleCurve[i - 1], b = visibleCurve[i];
            Vector2 direction = (b - a).normalized;
            var normal = new Vector2(-direction.y, direction.x) * radius;
            int start = mesh.currentVertCount;
            Vertex(mesh, a - normal, tint);
            Vertex(mesh, a + normal, tint);
            Vertex(mesh, b + normal, tint);
            Vertex(mesh, b - normal, tint);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
        for (int i = 0; i < visibleCount; i++)
        {
            int start = mesh.currentVertCount;
            Vertex(mesh, visibleCurve[i], tint);
            for (int side = 0; side <= 12; side++)
            {
                float angle = side * Mathf.PI / 6;
                Vertex(mesh, visibleCurve[i] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint);
                if (side > 0) mesh.AddTriangle(start, start + side, start + side + 1);
            }
        }
    }

    private static void Vertex(VertexHelper mesh, Vector2 point, Color tint)
    {
        var vertex = UIVertex.simpleVert;
        vertex.position = point;
        vertex.color = tint;
        mesh.AddVert(vertex);
    }
}
