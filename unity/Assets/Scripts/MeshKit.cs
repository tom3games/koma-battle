using System.Collections.Generic;
using UnityEngine;

// 形をコードで作る小道具
public static class MeshKit
{
    public static GameObject Obj(string name, Transform parent, Mesh mesh, Material mat)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return g;
    }

    public class Builder
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> t = new List<int>();
        public bool twoSided = true;

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); uv.Add(ua); uv.Add(ub); uv.Add(uc);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            if (twoSided)
            {
                i = v.Count;
                v.Add(a); v.Add(c); v.Add(b); uv.Add(ua); uv.Add(uc); uv.Add(ub);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            Tri(a, b, c, ua, ub, uc); Tri(a, c, d, ua, uc, ud);
        }
        public Mesh Mesh()
        {
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    // 下の半径 r0・上の半径 r1・高さ h の筒（円錐台）。底は y=0
    public static Mesh Frustum(float r0, float r1, float h, int seg, bool caps, bool smooth = true)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int j = 0; j <= seg; j++)
        {
            float a = j * Mathf.PI * 2 / seg;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            v.Add(new Vector3(c * r0, 0, s * r0)); uv.Add(new Vector2(j / (float)seg, 0));
            v.Add(new Vector3(c * r1, h, s * r1)); uv.Add(new Vector2(j / (float)seg, 1));
        }
        for (int j = 0; j < seg; j++)
        {
            int a = j * 2, b = a + 1, c = a + 2, d = a + 3;
            t.Add(a); t.Add(b); t.Add(c); t.Add(c); t.Add(b); t.Add(d);
        }
        if (caps)
        {
            int ct = v.Count; v.Add(new Vector3(0, h, 0)); uv.Add(new Vector2(0.5f, 0.5f));
            int s0 = v.Count;
            for (int j = 0; j <= seg; j++)
            {
                float a = j * Mathf.PI * 2 / seg;
                v.Add(new Vector3(Mathf.Cos(a) * r1, h, Mathf.Sin(a) * r1)); uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int j = 0; j < seg; j++) { t.Add(ct); t.Add(s0 + j + 1); t.Add(s0 + j); }
            int cb = v.Count; v.Add(new Vector3(0, 0, 0)); uv.Add(new Vector2(0.5f, 0.5f));
            int b0 = v.Count;
            for (int j = 0; j <= seg; j++)
            {
                float a = j * Mathf.PI * 2 / seg;
                v.Add(new Vector3(Mathf.Cos(a) * r0, 0, Mathf.Sin(a) * r0)); uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int j = 0; j < seg; j++) { t.Add(cb); t.Add(b0 + j); t.Add(b0 + j + 1); }
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // 平らな輪（上向き）。UV の u は外向き
    public static Mesh Ring(float rIn, float rOut, int seg)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int j = 0; j <= seg; j++)
        {
            float a = j * Mathf.PI * 2 / seg; float c = Mathf.Cos(a), s = Mathf.Sin(a);
            v.Add(new Vector3(c * rIn, 0, s * rIn)); uv.Add(new Vector2(0, j / (float)seg));
            v.Add(new Vector3(c * rOut, 0, s * rOut)); uv.Add(new Vector2(1, j / (float)seg));
        }
        for (int j = 0; j < seg; j++)
        {
            int a = j * 2, b = a + 1, c = a + 2, d = a + 3;
            t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // 平らな円板（上向き）。UV は円の中を 0..1
    public static Mesh Disc(float r, int seg)
    {
        var v = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { new Vector2(0.5f, 0.5f) }; var t = new List<int>();
        for (int j = 0; j <= seg; j++)
        {
            float a = j * Mathf.PI * 2 / seg;
            v.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
        }
        for (int j = 0; j < seg; j++) { t.Add(0); t.Add(j + 2); t.Add(j + 1); }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // 箱（中心が原点）
    public static Mesh Box(Vector3 size)
    {
        var mb = new Builder { twoSided = false };
        Vector3 h = size * 0.5f;
        Vector3[] p = {
            new Vector3(-h.x,-h.y,-h.z), new Vector3(h.x,-h.y,-h.z), new Vector3(h.x,h.y,-h.z), new Vector3(-h.x,h.y,-h.z),
            new Vector3(-h.x,-h.y,h.z), new Vector3(h.x,-h.y,h.z), new Vector3(h.x,h.y,h.z), new Vector3(-h.x,h.y,h.z) };
        Vector2 a = new Vector2(0, 0), b = new Vector2(1, 0), c = new Vector2(1, 1), d = new Vector2(0, 1);
        mb.Quad(p[0], p[3], p[2], p[1], a, d, c, b);
        mb.Quad(p[5], p[6], p[7], p[4], a, d, c, b);
        mb.Quad(p[4], p[7], p[3], p[0], a, d, c, b);
        mb.Quad(p[1], p[2], p[6], p[5], a, d, c, b);
        mb.Quad(p[3], p[7], p[6], p[2], a, d, c, b);
        mb.Quad(p[4], p[0], p[1], p[5], a, d, c, b);
        return mb.Mesh();
    }

    // 内側を向いた球（空）。色は頂点色
    public static Mesh SkyDome(float r, Color top, Color mid, Color bottom)
    {
        int seg = 48, rings = 24;
        var v = new List<Vector3>(); var col = new List<Color>(); var t = new List<int>();
        for (int i = 0; i <= rings; i++)
        {
            float th = Mathf.PI * i / rings; float y = Mathf.Cos(th);
            Color c = y > 0 ? Color.Lerp(mid, top, Mathf.Pow(y, 0.6f)) : Color.Lerp(mid, bottom, Mathf.Pow(-y, 0.5f));
            for (int j = 0; j <= seg; j++)
            {
                float a = 2 * Mathf.PI * j / seg;
                v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(a), y, Mathf.Sin(th) * Mathf.Sin(a)) * r);
                col.Add(c);
            }
        }
        int W = seg + 1;
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < seg; j++)
            {
                int a = i * W + j, b = a + 1, c = a + W, d = c + 1;
                t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
            }
        var m = new Mesh();
        m.SetVertices(v); m.SetColors(col); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
}
