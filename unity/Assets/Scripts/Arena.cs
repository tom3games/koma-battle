using System.Collections.Generic;
using UnityEngine;

// スタジアムの形（高さ関数）と見た目の組み立て。物理は H() だけを見る
// 2026-11-25 本人「ステージ狭すぎるわ。3倍くらいにして」「ジャンプ台はどの角度からでも乗れるように」「ジャンプ台に加速板も追加」
//   → 面積を約3倍（半径 13 → 22.5）。ジャンプ台は円い山（どの向きからでも登れて、頂上を越える時に跳ぶ）。
//     山の裾を一周する加速板（どの向きから来ても踏む）
public static class Arena
{
    public const float R = 22.5f;        // すり鉢の床の半径
    public const float LipW = 1.6f;      // 縁の幅
    public const float Edge = R + LipW;  // ここより外は奈落
    public const float BowlH = 3.6f;
    public const float LipH = 2.0f, GapLipH = 0.5f, GapHalf = 0.1f;
    public static readonly float[] GapDeg = { 0, 90, 180, 270 };
    // ジャンプ台（円い山）と、その裾の加速板（輪）
    public static readonly float[] MoundDeg = { 45, 135, 225, 315 };
    public const float MoundR = 13f, MoundBase = 3.6f, MoundH = 2.4f;
    public const float BoostR0 = MoundBase + 0.25f, BoostR1 = MoundBase + 1.55f;
    public static readonly Vector2[] Mounds = MakeRing(MoundDeg, MoundR);
    // 中央の段差（弧の台）
    public static readonly float[] ArcDeg = { 30, 150, 270 };
    public const float ArcHalf = 0.45f, ArcR0 = 4.2f, ArcR1 = 6.0f, ArcH = 0.75f;
    // 跳ね台
    public static readonly Vector2[] Pads = MakeRing(new float[] { 90, 210, 330 }, 8.5f);
    public const float PadR = 1.1f;

    static Vector2[] MakeRing(float[] deg, float r)
    {
        var a = new Vector2[deg.Length];
        for (int i = 0; i < deg.Length; i++) a[i] = new Vector2(Mathf.Cos(deg[i] * Mathf.Deg2Rad), Mathf.Sin(deg[i] * Mathf.Deg2Rad)) * r;
        return a;
    }

    public static float Bowl(float r) { float t = r / R; return BowlH * t * t; }

    static float AngDist(float a, float deg) => Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, deg) * Mathf.Deg2Rad);

    public static float GapFactor(float ang)
    {
        float g = 0;
        foreach (var d in GapDeg) g = Mathf.Max(g, 1f - Mathf.SmoothStep(0, 1, (AngDist(ang, d) - GapHalf) / 0.04f));
        return g;
    }

    public static float LipHeight(float ang) => Mathf.Lerp(LipH, GapLipH, GapFactor(ang));

    public static float BaseH(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        if (r > Edge) return -1000f;
        if (r <= R) return Bowl(r);
        float ang = Mathf.Atan2(z, x);
        float t = Mathf.Clamp01((r - R) / 0.36f);
        return Bowl(R) + LipHeight(ang) * t * t * (3 - 2 * t);
    }

    // 山の上乗せの高さ。裾はなめらかに、頂上は少し丸める
    public static float MoundAdd(float x, float z, out int idx)
    {
        idx = -1; float best = 0;
        for (int i = 0; i < Mounds.Length; i++)
        {
            float d = (new Vector2(x, z) - Mounds[i]).magnitude;
            if (d >= MoundBase) continue;
            float k = 1f - d / MoundBase;
            float s = k < 0.15f ? k * k / 0.3f : k - 0.075f;            // 裾
            float top = 1f - 0.075f;
            if (k > 0.9f) s = Mathf.Lerp(0.9f - 0.075f, top, (k - 0.9f) / 0.1f) - 0.04f * Mathf.Pow((k - 0.9f) / 0.1f, 2);   // 頂上を丸める
            float h = MoundH * s / top;
            if (h > best) { best = h; idx = i; }
        }
        return best;
    }

    public static int MoundAt(float x, float z) { MoundAdd(x, z, out int i); return i; }

    public static int BoostAt(float x, float z)
    {
        for (int i = 0; i < Mounds.Length; i++)
        {
            float d = (new Vector2(x, z) - Mounds[i]).magnitude;
            if (d >= BoostR0 && d <= BoostR1) return i;
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

    public static float FloorH(float x, float z)
    {
        float h = BaseH(x, z);
        if (h < -100) return h;
        return h + MoundAdd(x, z, out _);
    }

    public static float H(float x, float z)
    {
        float h = FloorH(x, z);
        if (h < -100) return h;
        if (OnArc(x, z)) h = Mathf.Max(h, Bowl(Mathf.Sqrt(x * x + z * z)) + ArcH);
        return h;
    }

    // 壁の判定用: 縁の上には見えない柵（切れ目以外からは場外に出ない）
    public static float WallH(float x, float z)
    {
        float h = H(x, z);
        float r = Mathf.Sqrt(x * x + z * z);
        if (r > R + 0.25f && r <= Edge + 0.6f && GapFactor(Mathf.Atan2(z, x)) < 0.5f) return Mathf.Max(h, Bowl(R) + LipH + 14f);
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
        var floor = MeshKit.Obj("Floor", root.transform, FloorMesh(), m.Floor);
        floor.GetComponent<MeshRenderer>().receiveShadows = true;
        var basePlate = MeshKit.Obj("Base", root.transform, MeshKit.Frustum(Edge + 4f, Edge + 2.2f, 3.4f, 128, true), m.Base);
        basePlate.transform.localPosition = new Vector3(0, -6.4f, 0);
        foreach (var d in ArcDeg) MeshKit.Obj("Arc", root.transform, ArcMesh(d), m.Arc);
        foreach (var p in Pads)
        {
            float y = BaseH(p.x, p.y);
            var pad = MeshKit.Obj("Pad", root.transform, MeshKit.Frustum(PadR, PadR * 0.94f, 0.1f, 40, true), m.Pad);
            pad.transform.localPosition = new Vector3(p.x, y - 0.03f, p.y);
            foreach (var rr in new[] { 0.66f, 0.33f })
            {
                var slot = MeshKit.Obj("PadSlot", root.transform, MeshKit.Ring(PadR * rr, PadR * (rr + 0.07f), 40), m.PadSlot);
                slot.transform.localPosition = new Vector3(p.x, y + 0.08f, p.y);
            }
        }
        // 加速板: 山の裾の輪に、山へ向かう矢印の光
        foreach (var c in Mounds)
        {
            var ring = MeshKit.Obj("Boost", root.transform, BoostMesh(c), m.Boost);
            ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return root;
    }

    // 加速板の輪（床に沿わせる）。UV の u は周、v は内向き
    static Mesh BoostMesh(Vector2 c)
    {
        int seg = 72, nr = 3;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= nr; i++)
        {
            float rr = Mathf.Lerp(BoostR1, BoostR0, i / (float)nr);
            for (int j = 0; j <= seg; j++)
            {
                float a = j * Mathf.PI * 2 / seg;
                float x = c.x + Mathf.Cos(a) * rr, z = c.y + Mathf.Sin(a) * rr;
                v.Add(new Vector3(x, FloorH(x, z) + 0.035f, z));
                uv.Add(new Vector2(j / (float)seg * 22f, i / (float)nr));
            }
        }
        int W = seg + 1;
        for (int i = 0; i < nr; i++)
            for (int j = 0; j < seg; j++)
            {
                int a = i * W + j, b = a + 1, cc = a + W, d = cc + 1;
                t.Add(a); t.Add(cc); t.Add(b); t.Add(b); t.Add(cc); t.Add(d);
            }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh FloorMesh()
    {
        int seg = 288;
        var rings = new List<float>();
        for (float r = 0; r < R - 0.01f; r += 0.4f) rings.Add(r);
        rings.Add(R); rings.Add(R + 0.09f); rings.Add(R + 0.18f); rings.Add(R + 0.27f); rings.Add(R + 0.36f); rings.Add(R + 1f); rings.Add(Edge);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i < rings.Count; i++)
            for (int j = 0; j <= seg; j++)
            {
                float a = j * Mathf.PI * 2 / seg, r = rings[i];
                float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                v.Add(new Vector3(x, FloorH(x * 0.99999f, z * 0.99999f), z));
                uv.Add(new Vector2(x / (2 * Edge) + 0.5f, z / (2 * Edge) + 0.5f));
            }
        int W = seg + 1;
        for (int i = 0; i < rings.Count - 1; i++)
            for (int j = 0; j < seg; j++)
            {
                int a = i * W + j, b = a + 1, c = a + W, d = c + 1;
                t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
            }
        int top0 = v.Count;
        Vector2 wuv = new Vector2((R + 0.7f) / (2 * Edge) + 0.5f, 0.5f);
        for (int j = 0; j <= seg; j++)
        {
            float a = j * Mathf.PI * 2 / seg;
            float x = Mathf.Cos(a) * Edge, z = Mathf.Sin(a) * Edge;
            v.Add(new Vector3(x, BaseH(x * 0.9999f, z * 0.9999f), z)); uv.Add(wuv);
            v.Add(new Vector3(x * 1.03f, -3.4f, z * 1.03f)); uv.Add(wuv);
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

    static Mesh ArcMesh(float deg)
    {
        float c = deg * Mathf.Deg2Rad;
        var mb = new MeshKit.Builder();
        int n = 22;
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
