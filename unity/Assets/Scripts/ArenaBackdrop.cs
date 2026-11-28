using System.Collections.Generic;
using UnityEngine;

// 2026-11-29 ステージの背景「地下闘技場」（本人が案Aに決定。廃坑を改造した円形の観覧席）。
//   試合場のまわりをコンクリートの段々の観覧席が囲み、上ほど暗い。壁に太い配管・鉄骨の梁・黄黒の警告帯、オレンジの作業灯、点滅する赤い回転灯。
//   天井は見えないほど高く、吊り照明の光の筋。試合場の真下は底の見えない縦穴。
//   ステージごとの色: ボウル=鉄と錆のオレンジ・丘=コンクリートと青白い蛍光灯・ターンテーブル=水色の光が走る機械室
//   守る事: 試合場より暗く彩度低め（コマが埋もれない）／ほぼ動かさない（赤い回転灯の点滅だけ）／頂点色の静的な形だけ（スマホで軽い・影なし）
public static class Backdrop
{
    // 数字の調整はここ
    const int Seg = 72;                       // 周の分割
    const float RIn = 29f, Step = 1.7f, Rise = 1.1f, Y0 = -1.5f;   // 観覧席: 試合場の縁（24.1）の外 29m から 14段
    const int Steps = 14;
    const float WallH0 = 13.9f, WallH1 = 52f;

    static readonly Color[] StructTint = { new Color(1f, 0.78f, 0.56f), new Color(0.72f, 0.83f, 1f), new Color(0.56f, 0.86f, 1f) };
    static readonly Color[] LampTint = { new Color(1f, 0.55f, 0.16f), new Color(0.82f, 0.92f, 1f), new Color(0.3f, 0.92f, 1f) };

    static Material structM, lampM, haloM, redM, coneM, redLampM;
    static readonly List<GameObject> parts = new List<GameObject>();
    static int stageNow = -1;

    public static int PartCount => parts.Count;
    public static int ActiveCount() { int n = 0; foreach (var g in parts) if (g && g.activeSelf) n++; return n; }
    public static bool ColorOk(int st) => structM && stageNow == st && structM.color == StructTint[Mathf.Clamp(st, 0, 2)] && lampM.color == LampTint[Mathf.Clamp(st, 0, 2)];

    // 頂点色つきの面を溜めて 1 つのメッシュにする（影・法線なしの静的な形）
    class VB
    {
        public List<Vector3> v = new List<Vector3>(); public List<Color> c = new List<Color>(); public List<int> t = new List<int>();
        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color col, Color? colTop = null)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            Color top = colTop ?? col; c.Add(col); c.Add(top); c.Add(top); c.Add(col);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        public Mesh Mesh() { var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds(); return m; }
    }
    static float Hash(int a, int b) { unchecked { uint h = (uint)(a * 374761393 + b * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
    static Vector3 P(float ang, float r, float y) => new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r);
    static Color G(float v, float tr = 0f, float tg = 0f, float tb = 0f) => new Color(v + tr, v + tg, v + tb, 1f);

    static GameObject Add(string name, Transform parent, Mesh mesh, Material mat)
    {
        var g = MeshKit.Obj(name, parent, mesh, mat);
        var r = g.GetComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        parts.Add(g); return g;
    }

    public static void Build(Materials m)
    {
        parts.Clear();
        var root = new GameObject("Backdrop").transform;
        structM = new Material(m.Sky); lampM = new Material(m.Sky);
        haloM = m.AddGlow(new Color(1f, 0.55f, 0.16f, 1f), m.Dot); redM = m.AddGlow(new Color(1f, 0.1f, 0.06f, 1f), m.Dot);
        coneM = m.AddGlow(new Color(0.9f, 0.95f, 1f, 1f), Texture2D.whiteTexture); coneM.SetColor("_TintColor", new Color(0.8f, 0.9f, 1f, 1f) * 0.022f);
        redLampM = new Material(m.Sky);
        float da = Mathf.PI * 2 / Seg;

        // ---- 観覧席（段々）: 上ほど暗い。座席の切れ目と通路で細かい縞 ----
        var st = new VB();
        for (int i = 0; i < Steps; i++)
        {
            float f = Mathf.Lerp(1f, 0.22f, i / (float)(Steps - 1));
            float r0 = RIn + i * Step, r1 = r0 + Step, y = Y0 + i * Rise, y1 = y + Rise;
            for (int s = 0; s < Seg; s++)
            {
                float a0 = s * da, a1 = a0 + da;
                bool aisle = s % 9 == 0, gap = s % 2 == 0;
                float n = (Hash(s, i) - 0.5f) * 0.05f;
                float tread = (aisle ? 0.30f : 0.22f) * f + n * f, riser = (aisle ? 0.22f : gap ? 0.12f : 0.16f) * f + n * f * 0.6f;
                st.Quad(P(a0, r0, y), P(a0, r1, y), P(a1, r1, y), P(a1, r0, y), G(tread));
                st.Quad(P(a0, r1, y), P(a0, r1, y1), P(a1, r1, y1), P(a1, r1, y), G(riser * 0.8f), G(riser));
            }
        }
        // 観覧席の手前の低い壁（試合場との境）。上に黄黒の警告帯
        float rb = RIn - 0.8f;
        for (int s = 0; s < Seg; s++)
        {
            float a0 = s * da, a1 = a0 + da; bool yel = (s / 2) % 2 == 0;
            st.Quad(P(a0, rb, Y0 - 7f), P(a0, rb, Y0 + 0.3f), P(a1, rb, Y0 + 0.3f), P(a1, rb, Y0 - 7f), G(0.05f), G(0.09f));
            st.Quad(P(a0, rb, Y0 + 0.3f), P(a0, rb, Y0 + 1.0f), P(a1, rb, Y0 + 1.0f), P(a1, rb, Y0 + 0.3f), yel ? new Color(0.5f, 0.4f, 0.05f) : G(0.03f));
        }
        Add("Terraces", root, st.Mesh(), structM);

        // ---- 外壁: 配管・警告帯・鉄骨 ----
        var wl = new VB();
        float rw = RIn + Steps * Step - 0.2f;
        for (int s = 0; s < Seg; s++)
        {
            float a0 = s * da, a1 = a0 + da;
            for (int k = 0; k < 6; k++)
            {
                float y0 = Mathf.Lerp(WallH0, WallH1, k / 6f), y1 = Mathf.Lerp(WallH0, WallH1, (k + 1) / 6f);
                float c0 = 0.11f * Mathf.Lerp(1f, 0.2f, k / 6f) + (Hash(s, k + 40) - 0.5f) * 0.012f, c1 = 0.11f * Mathf.Lerp(1f, 0.2f, (k + 1) / 6f);
                wl.Quad(P(a0, rw, y0), P(a0, rw, y1), P(a1, rw, y1), P(a1, rw, y0), G(c0), G(c1));
            }
            // 黄黒の警告帯（壁の足元）
            bool yel = (s / 2) % 2 == 0;
            wl.Quad(P(a0, rw - 0.05f, WallH0), P(a0, rw - 0.05f, WallH0 + 1.6f), P(a1, rw - 0.05f, WallH0 + 1.6f), P(a1, rw - 0.05f, WallH0), yel ? new Color(0.42f, 0.34f, 0.04f) : G(0.025f));
        }
        // 横の太い配管（3本）と縦の配管（12本）
        foreach (var hy in new[] { 21f, 28.5f, 36f })
            for (int s = 0; s < Seg; s++)
            {
                float a0 = s * da, a1 = a0 + da, f = Mathf.Lerp(1f, 0.35f, (hy - WallH0) / (WallH1 - WallH0));
                wl.Quad(P(a0, rw - 0.5f, hy), P(a0, rw - 0.5f, hy + 1.5f), P(a1, rw - 0.5f, hy + 1.5f), P(a1, rw - 0.5f, hy), G(0.10f * f), G(0.045f * f));
                wl.Quad(P(a0, rw - 0.5f, hy + 1.5f), P(a0, rw - 0.5f, hy + 1.9f), P(a1, rw - 0.5f, hy + 1.9f), P(a1, rw - 0.5f, hy + 1.5f), G(0.2f * f));
            }
        for (int p = 0; p < 12; p++)
        {
            float a = (p + 0.5f) * Mathf.PI * 2 / 12, w = 0.016f;
            wl.Quad(P(a - w, rw - 0.6f, WallH0 + 1.6f), P(a - w, rw - 0.6f, WallH1 - 4f), P(a + w, rw - 0.6f, WallH1 - 4f), P(a + w, rw - 0.6f, WallH0 + 1.6f), G(0.09f), G(0.03f));
            wl.Quad(P(a - w * 1.2f, rw - 0.62f, WallH0 + 1.6f), P(a - w * 1.2f, rw - 0.62f, WallH1 - 4f), P(a - w * 0.5f, rw - 0.62f, WallH1 - 4f), P(a - w * 0.5f, rw - 0.62f, WallH0 + 1.6f), G(0.17f), G(0.05f));
        }
        Add("Wall", root, wl.Mesh(), structM);

        // ---- 天井の鉄骨の梁（見えないほど高い）。放射状 16 本と輪 3 本 ----
        var bm = new VB();
        for (int b = 0; b < 16; b++)
        {
            float a = b * Mathf.PI * 2 / 16; float hw = 0.012f;
            bm.Quad(P(a - hw, 6f, 52f), P(a - hw, rw, 52f), P(a + hw, rw, 52f), P(a + hw, 6f, 52f), G(0.045f), G(0.03f));
        }
        foreach (var rr in new[] { 20f, 36f, rw - 0.5f })
            for (int s = 0; s < Seg; s++) { float a0 = s * da, a1 = a0 + da; bm.Quad(P(a0, rr, 51.5f), P(a0, rr, 53f), P(a1, rr, 53f), P(a1, rr, 51.5f), G(0.04f)); }
        Add("Beams", root, bm.Mesh(), structM);

        // ---- 試合場の真下: 底の見えない縦穴 ----
        var sh = new VB(); float rs = Arena.Edge + 3.9f;
        for (int k = 0; k < 14; k++)
        {
            float y0 = -3.4f - k * 5.5f, y1 = y0 - 5.5f, c0 = 0.085f * Mathf.Pow(0.62f, k), c1 = 0.085f * Mathf.Pow(0.62f, k + 1);
            for (int s = 0; s < Seg; s++)
            {
                float a0 = s * da, a1 = a0 + da; float n = (Hash(s, k + 90) - 0.5f) * 0.02f * Mathf.Pow(0.62f, k);
                sh.Quad(P(a0, rs, y0), P(a0, rs, y1), P(a1, rs, y1), P(a1, rs, y0), G(c0 + n), G(c1 + n));
            }
            if (k % 2 == 0) for (int s = 0; s < Seg; s++) { float a0 = s * da, a1 = a0 + da; sh.Quad(P(a0, rs - 0.1f, y0 - 0.3f), P(a0, rs - 0.1f, y0 - 0.9f), P(a1, rs - 0.1f, y0 - 0.9f), P(a1, rs - 0.1f, y0 - 0.3f), G(0.12f * Mathf.Pow(0.62f, k))); }
        }
        Add("Shaft", root, sh.Mesh(), structM);

        // ---- 作業灯（オレンジ・ステージで色が変わる）と点滅する赤い回転灯 ----
        var lq = new VB(); var hq = new VB(); var rq = new VB(); var rh = new VB();
        for (int i = 0; i < 12; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2 / 12 + 0.1f; float y = i % 2 == 0 ? 20f : 31f;
            Quad2(lq, a, rw - 0.9f, y, 2.4f, 0.8f, Color.white);
            Quad2(hq, a, rw - 1.2f, y, 9f, 5f, Color.white);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2 / 6 + 0.35f;
            Quad2(rq, a, rw - 1.0f, 14.6f, 0.6f, 0.6f, Color.white);
            Quad2(rh, a, rw - 1.3f, 14.6f, 7f, 7f, Color.white);
        }
        Add("WorkLamps", root, lq.Mesh(), lampM); Add("WorkHalos", root, hq.Mesh(), haloM);
        Add("RedLamps", root, rq.Mesh(), redLampM); Add("RedHalos", root, rh.Mesh(), redM);

        // ---- 吊り照明と光の筋（ほこりが少し光る程度） ----
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2 / 5 + 0.6f, r = 11f + (i % 2) * 7f;
            var cone = Add("LightShaft", root, MeshKit.Frustum(6.5f, 0.7f, 38f, 20, false), coneM);
            cone.transform.position = P(a, r, 6f);
            var lamp = new VB(); Quad2(lamp, a, r, 44.4f, 1.6f, 0.5f, Color.white, true);
            var lg = Add("HangLamp", root, lamp.Mesh(), lampM); lg.transform.position = Vector3.zero;
        }
        SetStage(0);
    }

    // 向きは中心を向く縦の板（顔を中心へ）。flat=true なら水平の板
    static void Quad2(VB b, float ang, float r, float y, float w, float h, Color col, bool flat = false)
    {
        Vector3 c = P(ang, r, y), tan = new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang)) * w * 0.5f;
        Vector3 up = flat ? new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * h * 0.5f : Vector3.up * h * 0.5f;
        b.Quad(c - tan - up, c - tan + up, c + tan + up, c + tan - up, col);
    }

    public static void SetStage(int st)
    {
        st = Mathf.Clamp(st, 0, 2); stageNow = st;
        if (structM == null) return;
        structM.color = StructTint[st];
        lampM.color = LampTint[st] * 1.05f; lampM.color = LampTint[st];
        haloM.SetColor("_TintColor", LampTint[st] * (st == 0 ? 0.30f : 0.24f));
        foreach (var g in parts) if (g) g.SetActive(true);
    }

    // 赤い回転灯の点滅（背景で動くのはこれだけ）
    public static void Tick(float t)
    {
        if (redM == null) return;
        float k = Mathf.Repeat(t * 0.9f, 1f); float pulse = k < 0.5f ? Mathf.Sin(k * Mathf.PI * 2f) : 0f;
        redM.SetColor("_TintColor", new Color(1f, 0.1f, 0.06f, 1f) * (0.02f + 0.3f * Mathf.Max(0f, pulse)));
        redLampM.color = Color.Lerp(new Color(0.25f, 0.03f, 0.02f), new Color(1f, 0.2f, 0.12f), Mathf.Max(0f, pulse));
    }
}
