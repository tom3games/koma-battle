using System.Collections.Generic;
using UnityEngine;

// スタジアムの形（高さ関数）と見た目の組み立て。物理は H() だけを見る
public static class Arena
{
    public const float R = 13f;          // すり鉢の床の半径
    public const float LipW = 1.3f;      // 縁の幅
    public const float Edge = R + LipW;  // ここより外は奈落
    public const float BowlH = 2.6f;
    public const float LipH = 2.0f, GapLipH = 0.24f, GapHalf = 0.2f;
    public static readonly float[] GapDeg = { 30, 150, 270 };
    public static readonly float[] RampDeg = { 90, 210, 330 };
    public const float RampIn = 5.8f, RampOut = 10.6f, RampHalfW = 1.4f, RampH = 2.2f;
    public static readonly float[] ArcDeg = { 30, 150, 270 };
    public const float ArcHalf = 0.5f, ArcR0 = 2.3f, ArcR1 = 3.7f, ArcH = 0.75f;
    public static readonly Vector2[] Pads = {
        new Vector2(Mathf.Cos(0f), Mathf.Sin(0f)) * 6.6f,
        new Vector2(Mathf.Cos(Mathf.PI), Mathf.Sin(Mathf.PI)) * 6.6f,
    };
    public const float PadR = 1.05f;

    public static float Bowl(float r) { float t = r / R; return BowlH * t * t; }

    static float AngDist(float a, float deg)
    {
        float d = Mathf.DeltaAngle(a * Mathf.Rad2Deg, deg) * Mathf.Deg2Rad;
        return Mathf.Abs(d);
    }

    public static float GapFactor(float ang)
    {
        float g = 0;
        foreach (var d in GapDeg) g = Mathf.Max(g, 1f - Mathf.SmoothStep(0, 1, (AngDist(ang, d) - GapHalf) / 0.06f));
        return g;
    }

    public static float LipHeight(float ang) => Mathf.Lerp(LipH, GapLipH, GapFactor(ang));

    // すり鉢＋縁だけ（見た目の床の網に使う）
    public static float BaseH(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        if (r > Edge) return -1000f;
        if (r <= R) return Bowl(r);
        float ang = Mathf.Atan2(z, x);
        float t = Mathf.Clamp01((r - R) / 0.32f);
        return Bowl(R) + LipHeight(ang) * t * t * (3 - 2 * t);
    }

    public static float RampTop(float u, float r) => Bowl(r) + RampH * (RampOut - u) / (RampOut - RampIn);

    // どのジャンプ台の上か（-1 ならなし）
    public static int RampAt(float x, float z, out float u)
    {
        u = 0;
        for (int i = 0; i < RampDeg.Length; i++)
        {
            float a = RampDeg[i] * Mathf.Deg2Rad;
            float dx = Mathf.Cos(a), dz = Mathf.Sin(a);
            float uu = x * dx + z * dz, v = -x * dz + z * dx;
            if (uu >= RampIn && uu <= RampOut && Mathf.Abs(v) <= RampHalfW) { u = uu; return i; }
        }
        return -1;
    }

    public static bool OnArc(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        if (r < ArcR0 || r > ArcR1) return false;
        float ang = Mathf.Atan2(z, x);
        foreach (var d in ArcDeg) if (AngDist(ang, d) <= ArcHalf) return true;
        return false;
    }

    public static int PadAt(float x, float z)
    {
        for (int i = 0; i < Pads.Length; i++) if ((new Vector2(x, z) - Pads[i]).sqrMagnitude < PadR * PadR) return i;
        return -1;
    }

    public static float H(float x, float z)
    {
        float h = BaseH(x, z);
        if (h < -100) return h;
        float r = Mathf.Sqrt(x * x + z * z);
        if (RampAt(x, z, out float u) >= 0) h = Mathf.Max(h, RampTop(u, r));
        if (OnArc(x, z)) h = Mathf.Max(h, Bowl(r) + ArcH);
        return h;
    }

    // 壁の判定用: 縁の上には見えない柵（切れ目以外からは場外に出ない。切れ目が危ない所だと一目で分かるように）
    public static float WallH(float x, float z)
    {
        float h = H(x, z);
        float r = Mathf.Sqrt(x * x + z * z);
        if (r > R + 0.25f && r <= Edge + 0.6f && GapFactor(Mathf.Atan2(z, x)) < 0.5f) return Mathf.Max(h, Bowl(R) + LipH + 6f);
        return h;
    }

    public static Vector2 Grad(float x, float z)
    {
        const float e = 0.06f;
        return new Vector2((H(x + e, z) - H(x - e, z)) / (2 * e), (H(x, z + e) - H(x, z - e)) / (2 * e));
    }

    // ---------- 見た目 ----------
    public static GameObject BuildVisual(Materials m)
    {
        var root = new GameObject("Arena");
        // 床（すり鉢＋縁＋外側の壁）
        var floor = MeshKit.Obj("Floor", root.transform, FloorMesh(), m.Floor);
        floor.GetComponent<MeshRenderer>().receiveShadows = true;
        // 台座（外側の大きな台）
        var basePlate = MeshKit.Obj("Base", root.transform, MeshKit.Frustum(Edge + 3.5f, Edge + 2.0f, 3.2f, 96, true), m.Base);
        basePlate.transform.localPosition = new Vector3(0, -6.2f, 0);
        // ジャンプ台（坂）
        for (int i = 0; i < RampDeg.Length; i++) MeshKit.Obj("Ramp" + i, root.transform, RampMesh(RampDeg[i]), m.Ramp);
        // 段差（内側の弧の台）
        foreach (var d in ArcDeg) MeshKit.Obj("Arc", root.transform, ArcMesh(d), m.Arc);
        // 跳ね台（丸いパッド）
        foreach (var p in Pads)
        {
            var pad = MeshKit.Obj("Pad", root.transform, MeshKit.Frustum(PadR, PadR * 0.92f, 0.12f, 40, true), m.Pad);
            pad.transform.localPosition = new Vector3(p.x, BaseH(p.x, p.y) - 0.04f, p.y);
            var ring = MeshKit.Obj("PadGlow", root.transform, MeshKit.Ring(PadR * 0.55f, PadR * 0.85f, 40), m.AddGlow(new Color(0.2f, 1f, 0.85f, 0.9f)));
            ring.transform.localPosition = new Vector3(p.x, BaseH(p.x, p.y) + 0.1f, p.y);
            var beam = MeshKit.Obj("PadBeam", root.transform, MeshKit.Frustum(PadR * 0.8f, PadR * 0.5f, 3.2f, 32, false), m.AddGlow(new Color(0.15f, 0.9f, 0.8f, 0.18f)));
            beam.transform.localPosition = new Vector3(p.x, BaseH(p.x, p.y) + 0.1f, p.y);
        }
        return root;
    }

    static Mesh FloorMesh()
    {
        int seg = 192;
        var rings = new List<float>();
        for (float r = 0; r < R - 0.01f; r += 0.4f) rings.Add(r);
        rings.Add(R); rings.Add(R + 0.08f); rings.Add(R + 0.16f); rings.Add(R + 0.24f); rings.Add(R + 0.32f); rings.Add(R + 0.8f); rings.Add(Edge);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i < rings.Count; i++)
            for (int j = 0; j <= seg; j++)
            {
                float a = j * Mathf.PI * 2 / seg, r = rings[i];
                float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                v.Add(new Vector3(x, BaseH(x, z), z));
                uv.Add(new Vector2(x / (2 * Edge) + 0.5f, z / (2 * Edge) + 0.5f));
            }
        int W = seg + 1;
        for (int i = 0; i < rings.Count - 1; i++)
            for (int j = 0; j < seg; j++)
            {
                int a = i * W + j, b = a + 1, c = a + W, d = c + 1;
                t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
            }
        // 外側の壁（縁の外周から下へ）
        int top0 = v.Count;
        for (int j = 0; j <= seg; j++)
        {
            float a = j * Mathf.PI * 2 / seg;
            float x = Mathf.Cos(a) * Edge, z = Mathf.Sin(a) * Edge;
            Vector2 wuv = new Vector2((R + 0.55f) / (2 * Edge) + 0.5f, 0.5f);
            v.Add(new Vector3(x, BaseH(x * 0.9999f, z * 0.9999f), z)); uv.Add(wuv);
            v.Add(new Vector3(x * 1.03f, -3.2f, z * 1.03f)); uv.Add(wuv);
        }
        for (int j = 0; j < seg; j++)
        {
            int a = top0 + j * 2, b = a + 1, c = a + 2, d = a + 3;
            t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
        }
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh RampMesh(float deg)
    {
        float a = deg * Mathf.Deg2Rad;
        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), s = new Vector2(-d.y, d.x);
        var mb = new MeshKit.Builder();
        int nu = 14, nv = 4;
        System.Func<float, float, Vector3> top = (u, w) =>
        {
            Vector2 p = d * u + s * w; float r = p.magnitude;
            return new Vector3(p.x, RampTop(u, r) + 0.01f, p.y);
        };
        System.Func<float, float, Vector3> bot = (u, w) =>
        {
            Vector2 p = d * u + s * w; float r = p.magnitude;
            return new Vector3(p.x, Bowl(r) - 0.3f, p.y);
        };
        // 上面（UV の u は坂の向き＝矢印の模様）
        for (int i = 0; i < nu; i++)
            for (int k = 0; k < nv; k++)
            {
                float u0 = Mathf.Lerp(RampIn, RampOut, i / (float)nu), u1 = Mathf.Lerp(RampIn, RampOut, (i + 1) / (float)nu);
                float w0 = Mathf.Lerp(-RampHalfW, RampHalfW, k / (float)nv), w1 = Mathf.Lerp(-RampHalfW, RampHalfW, (k + 1) / (float)nv);
                mb.Quad(top(u0, w0), top(u0, w1), top(u1, w1), top(u1, w0),
                    new Vector2((u0 - RampIn) / (RampOut - RampIn), (w0 + RampHalfW) / (2 * RampHalfW)),
                    new Vector2((u0 - RampIn) / (RampOut - RampIn), (w1 + RampHalfW) / (2 * RampHalfW)),
                    new Vector2((u1 - RampIn) / (RampOut - RampIn), (w1 + RampHalfW) / (2 * RampHalfW)),
                    new Vector2((u1 - RampIn) / (RampOut - RampIn), (w0 + RampHalfW) / (2 * RampHalfW)));
            }
        var side = new Vector2(0.02f, 0.02f);
        for (int i = 0; i < nu; i++)
        {
            float u0 = Mathf.Lerp(RampIn, RampOut, i / (float)nu), u1 = Mathf.Lerp(RampIn, RampOut, (i + 1) / (float)nu);
            mb.Quad(bot(u0, -RampHalfW), top(u0, -RampHalfW), top(u1, -RampHalfW), bot(u1, -RampHalfW), side, side, side, side);
            mb.Quad(bot(u1, RampHalfW), top(u1, RampHalfW), top(u0, RampHalfW), bot(u0, RampHalfW), side, side, side, side);
        }
        // 内側の崖
        mb.Quad(bot(RampIn, RampHalfW), top(RampIn, RampHalfW), top(RampIn, -RampHalfW), bot(RampIn, -RampHalfW), side, side, side, side);
        return mb.Mesh();
    }

    static Mesh ArcMesh(float deg)
    {
        float c = deg * Mathf.Deg2Rad;
        var mb = new MeshKit.Builder();
        int n = 18;
        System.Func<float, float, float, Vector3> P = (ang, r, y) => new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r);
        var side = new Vector2(0.02f, 0.5f);
        for (int i = 0; i < n; i++)
        {
            float a0 = c - ArcHalf + 2 * ArcHalf * i / n, a1 = c - ArcHalf + 2 * ArcHalf * (i + 1) / n;
            int nr = 3;
            for (int k = 0; k < nr; k++)
            {
                float r0 = Mathf.Lerp(ArcR0, ArcR1, k / (float)nr), r1 = Mathf.Lerp(ArcR0, ArcR1, (k + 1) / (float)nr);
                Vector2 uvA = new Vector2(i / (float)n, k / (float)nr), uvB = new Vector2((i + 1f) / n, (k + 1f) / nr);
                mb.Quad(P(a0, r0, Bowl(r0) + ArcH), P(a0, r1, Bowl(r1) + ArcH), P(a1, r1, Bowl(r1) + ArcH), P(a1, r0, Bowl(r0) + ArcH),
                    uvA, new Vector2(uvA.x, uvB.y), uvB, new Vector2(uvB.x, uvA.y));
            }
            mb.Quad(P(a0, ArcR1, Bowl(ArcR1) - 0.3f), P(a0, ArcR1, Bowl(ArcR1) + ArcH), P(a1, ArcR1, Bowl(ArcR1) + ArcH), P(a1, ArcR1, Bowl(ArcR1) - 0.3f), side, side, side, side);
            mb.Quad(P(a1, ArcR0, Bowl(ArcR0) - 0.3f), P(a1, ArcR0, Bowl(ArcR0) + ArcH), P(a0, ArcR0, Bowl(ArcR0) + ArcH), P(a0, ArcR0, Bowl(ArcR0) - 0.3f), side, side, side, side);
        }
        float e0 = c - ArcHalf, e1 = c + ArcHalf;
        mb.Quad(P(e0, ArcR0, Bowl(ArcR0) - 0.3f), P(e0, ArcR0, Bowl(ArcR0) + ArcH), P(e0, ArcR1, Bowl(ArcR1) + ArcH), P(e0, ArcR1, Bowl(ArcR1) - 0.3f), side, side, side, side);
        mb.Quad(P(e1, ArcR1, Bowl(ArcR1) - 0.3f), P(e1, ArcR1, Bowl(ArcR1) + ArcH), P(e1, ArcR0, Bowl(ArcR0) + ArcH), P(e1, ArcR0, Bowl(ArcR0) - 0.3f), side, side, side, side);
        return mb.Mesh();
    }
}
