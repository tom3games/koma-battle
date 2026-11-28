using UnityEngine;

// 素材（Resources の元の素材を複製して色と模様をつける）。模様はコードで描く
// 美術の方向（2026-11-25 本人「デザインのオモチャ感。AI量産感がすごい」）:
//   主役＝金属を削り出したコマ。床は暗い黒鉄の削り出し（同心の細い溝・板の継ぎ目・はげた白線）、真上の強い照明で中央だけ明るい。
//   色みは「黒鉄の地・鋼の灰・各チームの色を1つ（くすませる）」。原色の多用、つるっとした単色、意味のない光の円は使わない。
public class Materials
{
    Material std, stdE, vtx, add, alpha;
    public Material Floor, Floor2, Ice, Base, Arc, Pad, PadSlot, Boost, Sky, Line, Stands, Truss, Warn;
    public Texture2D Dot, Streak, Brushed, Worn, Chevron, Funnel;

    public Materials()
    {
        std = Resources.Load<Material>("M_Std");
        stdE = Resources.Load<Material>("M_StdEmis");
        vtx = Resources.Load<Material>("M_Vtx");
        add = Resources.Load<Material>("M_Add");
        alpha = Resources.Load<Material>("M_Alpha");
        Dot = DotTex(64);
        Streak = StreakTex(128);
        Brushed = BrushedTex(256);
        Worn = WornTex(256);
        Chevron = ChevronTex();
        Funnel = FunnelTex();
        Floor = Lit(Color.white, 0.48f, 0.5f, FloorTex(1024));
        Floor2 = Lit(new Color(1f, 0.74f, 0.48f), 0.3f, 0.15f, FloorTex(1024, 1));   // 丘: 砂岩のような暖かい色
        Ice = Emis(new Color(0.42f, 0.66f, 0.85f), new Color(0.03f, 0.08f, 0.14f));                        // 氷の帯
        Base = Lit(new Color(0.09f, 0.1f, 0.12f), 0.5f, 0.6f, Brushed);
        Arc = Lit(Color.white, 0.55f, 0.7f, ArcTex());
        Pad = Lit(new Color(0.32f, 0.34f, 0.37f), 0.7f, 0.9f, Brushed);
        PadSlot = AddGlow(new Color(0.35f, 0.9f, 1f, 1f));
        Warn = AddGlow(new Color(1f, 0.15f, 0.08f, 1f));
        Boost = AddGlow(new Color(1f, 0.6f, 0.2f, 0.8f), Chevron);
        Sky = new Material(vtx);
        Line = new Material(add) { mainTexture = Dot };
        Stands = Lit(Color.white, 0.2f, 0.1f, StandsTex());
        Truss = Lit(new Color(0.12f, 0.13f, 0.15f), 0.4f, 0.8f);
    }

    public Material Lit(Color c, float gloss, float metal, Texture tex = null)
    {
        var m = new Material(std);
        m.color = c;
        m.SetFloat("_Glossiness", gloss);
        m.SetFloat("_Metallic", metal);
        if (tex != null) m.mainTexture = tex;
        return m;
    }

    public Material Emis(Color c, Color e)
    {
        var m = new Material(stdE);
        m.color = c;
        m.SetColor("_EmissionColor", e);
        m.SetFloat("_Glossiness", 0.6f);
        return m;
    }

    public Material AddGlow(Color c, Texture tex = null)
    {
        var m = new Material(add);
        m.SetColor("_TintColor", c * 0.5f);
        m.mainTexture = tex != null ? tex : Texture2D.whiteTexture;
        return m;
    }

    public Material Alpha(Color c, Texture tex)
    {
        var m = new Material(alpha);
        m.SetColor("_TintColor", c * 0.5f);
        m.mainTexture = tex;
        return m;
    }

    static Texture2D Make(int w, int h, bool repeat = false)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
    }

    static float Hash(int x, int y) { unchecked { int n = x * 374761393 + y * 668265263; n = (n ^ (n >> 13)) * 1274126177; return ((n ^ (n >> 16)) & 0xffff) / 65535f; } }
    static float VNoise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }
    static float Fbm(float x, float y) => VNoise(x, y) * 0.55f + VNoise(x * 2.1f, y * 2.1f) * 0.3f + VNoise(x * 4.3f, y * 4.3f) * 0.15f;

    // 反射の環境（金属が映り込む物）。暗い場内に上の照明と周りの光の帯
    public static Cubemap EnvCube()
    {
        int n = 32;
        var cube = new Cubemap(n, TextureFormat.RGBA32, true);
        var faces = new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ };
        foreach (var f in faces)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2 - 1, v = (y + 0.5f) / n * 2 - 1;
                    Vector3 d;
                    switch (f)
                    {
                        case CubemapFace.PositiveX: d = new Vector3(1, -v, -u); break;
                        case CubemapFace.NegativeX: d = new Vector3(-1, -v, u); break;
                        case CubemapFace.PositiveY: d = new Vector3(u, 1, v); break;
                        case CubemapFace.NegativeY: d = new Vector3(u, -1, -v); break;
                        case CubemapFace.PositiveZ: d = new Vector3(u, -v, 1); break;
                        default: d = new Vector3(-u, -v, -1); break;
                    }
                    d.Normalize();
                    float ang = Mathf.Atan2(d.z, d.x);
                    Color c = Color.Lerp(new Color(0.05f, 0.055f, 0.07f), new Color(0.13f, 0.14f, 0.17f), Mathf.Clamp01(d.y * 0.5f + 0.5f));
                    // 真上の照明の枠
                    if (d.y > 0.86f) c += new Color(0.55f, 0.53f, 0.5f) * Mathf.Clamp01((d.y - 0.86f) * 10f);
                    // 周りの照明塔の光の帯
                    float band = Mathf.Exp(-Mathf.Pow((d.y - 0.32f) / 0.05f, 2)) * (Mathf.Repeat(ang * 8f / (2 * Mathf.PI), 1f) < 0.35f ? 1f : 0.15f);
                    c += new Color(0.9f, 0.85f, 0.75f) * band;
                    // 床の照り返し
                    if (d.y < -0.2f) c += new Color(0.05f, 0.05f, 0.06f);
                    px[y * n + x] = c;
                }
            cube.SetPixels(px, f);
        }
        cube.Apply(true);
        return cube;
    }

    // 床: 黒鉄の削り出し。細い同心の溝・放射の継ぎ目・はげた白線・中央の刻印・切れ目と山の注意塗装
    // 丘は別の床の模様（切れ目の注意塗装は丘の8つに合わせ、ジャンプ台の模様は無し）
    static Texture2D FloorTex(int n, int stage)
    {
        int saved = Arena.Stage; Arena.Stage = stage;
        var t = FloorTex(n);
        Arena.Stage = saved; return t;
    }
    static Texture2D FloorTex(int n)
    {
        var t = Make(n, n);
        var px = new Color32[n * n];
        Color steel = new Color(0.26f, 0.28f, 0.31f), steelDark = new Color(0.14f, 0.15f, 0.17f);
        Color paint = new Color(0.82f, 0.82f, 0.78f), haz = new Color(0.86f, 0.62f, 0.12f), hazDark = new Color(0.08f, 0.08f, 0.09f);
        Color lip = new Color(0.18f, 0.19f, 0.22f), crimson = new Color(0.55f, 0.1f, 0.08f);
        float px2w = 2 * Arena.Edge / n;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float wx = (x + 0.5f) / n * 2 * Arena.Edge - Arena.Edge, wz = (y + 0.5f) / n * 2 * Arena.Edge - Arena.Edge;
                float r = Mathf.Sqrt(wx * wx + wz * wz), ang = Mathf.Atan2(wz, wx);
                float wear = Fbm(wx * 0.55f, wz * 0.55f);
                float grain = (Hash(x, y) - 0.5f) * 0.05f;
                Color c;
                if (r <= Arena.R)
                {
                    // 中央が明るい照明の溜まり＋外へ暗く
                    float pool = Mathf.Lerp(1.15f, 0.55f, Mathf.SmoothStep(0, 1, r / Arena.R));
                    c = Color.Lerp(steelDark, steel, wear * 0.7f + 0.3f) * pool;
                    // 削りの細い溝（0.22m ごと）
                    float groove = Mathf.Abs(Mathf.Repeat(r, 0.22f) - 0.11f) / 0.11f;
                    c *= 0.9f + 0.1f * groove;
                    // 板の継ぎ目（放射 24 本・外側だけ）
                    float sp = Mathf.Abs(Mathf.Repeat(ang * Mathf.Rad2Deg + 7.5f, 15f) - 7.5f) * Mathf.Deg2Rad * r;
                    if (r > 7f && sp < 0.025f) c *= 0.55f;
                    // 同心の太い継ぎ目
                    if (Mathf.Abs(r - 7f) < 0.03f || Mathf.Abs(r - 11.5f) < 0.03f || Mathf.Abs(r - 16f) < 0.03f) c *= 0.5f;
                    // はげた白線（外周の手前）
                    float lane = 1f - Mathf.Clamp01((Mathf.Abs(r - (Arena.R - 0.9f)) - 0.09f) / px2w);
                    c = Color.Lerp(c, paint * pool, lane * Mathf.Clamp01((wear - 0.28f) * 3f) * 0.85f);
                    // 切れ目の手前の注意塗装（斜めの縞・はげ）
                    float g = Arena.GapFactor(ang) * Mathf.Clamp01((r - (Arena.R - 3f)) / 0.2f);
                    if (g > 0.01f)
                    {
                        float st = Mathf.Repeat((wx + wz) * 0.9f, 1f) < 0.5f ? 1 : 0;
                        c = Color.Lerp(c, Color.Lerp(hazDark, haz, st) * pool, g * Mathf.Clamp01((wear - 0.22f) * 3f));
                    }
                    // 山（ジャンプ台）: 鋼の板＋注意の輪
                    for (int i = 0; Arena.Stage == 0 && i < Arena.Mounds.Length && r > Arena.MoundR - Arena.MoundBase - 0.1f && r < Arena.MoundR + Arena.MoundBase + 0.1f; i++)
                    {
                        float d = (new Vector2(wx, wz) - Arena.Mounds[i]).magnitude;
                        if (d < Arena.MoundBase)
                        {
                            float k = d / Arena.MoundBase;
                            c = Color.Lerp(steel * 1.2f, steelDark, k * 0.6f) * (0.92f + 0.08f * Mathf.Abs(Mathf.Repeat(d, 0.3f) - 0.15f) / 0.15f);
                            if (k > 0.86f && k < 0.93f) c = Color.Lerp(c, haz * 0.85f, Mathf.Clamp01((wear - 0.25f) * 3f) * 0.8f);
                            // 放射の板の継ぎ目・固定ボルトの輪・頂上の削れ
                            Vector2 dv = new Vector2(wx, wz) - Arena.Mounds[i];
                            float ma = Mathf.Atan2(dv.y, dv.x);
                            float seam = Mathf.Abs(Mathf.Repeat(ma * Mathf.Rad2Deg + 15f, 30f) - 15f) * Mathf.Deg2Rad * d;
                            if (k > 0.18f && seam < 0.03f) c *= 0.45f;
                            float boltA = Mathf.Abs(Mathf.Repeat(ma * Mathf.Rad2Deg, 15f) - 7.5f) * Mathf.Deg2Rad * d;
                            if (Mathf.Abs(k - 0.62f) < 0.025f && boltA < 0.06f) c = new Color(0.5f, 0.5f, 0.52f);
                            if (k < 0.2f) c = Color.Lerp(c, steel * 0.75f, Mathf.Clamp01(wear * 1.4f - 0.3f));
                            c *= 0.85f + Hash(x * 3, y * 5) * 0.2f;
                        }
                    }
                    // 中央の刻印（オリジナルの八角の紋を彫った線）
                    float oct = Mathf.Cos(Mathf.PI / 8) / Mathf.Cos(Mathf.Repeat(ang + Mathf.PI / 8, Mathf.PI / 4) - Mathf.PI / 8);
                    float e1 = Mathf.Abs(r - 2.2f * oct), e2 = Mathf.Abs(r - 1.6f * oct);
                    if (e1 < 0.05f || e2 < 0.03f) c *= 0.45f;
                    if (r < 1.6f * oct) c = Color.Lerp(c, crimson * pool, 0.55f * Mathf.Clamp01((wear - 0.2f) * 2.5f));
                }
                else
                {
                    c = lip * (0.8f + wear * 0.4f);
                    float g = Arena.GapFactor(ang);
                    if (g > 0.01f)
                    {
                        float st = Mathf.Repeat(ang * Arena.Edge * 1.4f + r * 1.4f, 1f) < 0.5f ? 1 : 0;
                        c = Color.Lerp(c, Color.Lerp(hazDark, haz, st), g * Mathf.Clamp01((wear - 0.15f) * 3f));
                    }
                    // ボルト
                    float bolt = Mathf.Abs(Mathf.Repeat(ang * Mathf.Rad2Deg, 6f) - 3f) * Mathf.Deg2Rad * r;
                    if (Mathf.Abs(r - (Arena.R + 1.1f)) < 0.07f && bolt < 0.07f) c = new Color(0.55f, 0.56f, 0.58f);
                }
                c.r += grain; c.g += grain; c.b += grain;
                px[y * n + x] = c;
            }
        t.SetPixels32(px); t.Apply(true, true);
        return t;
    }

    // 加速板の矢印（内向き＝ v が増える向き）
    static Texture2D ChevronTex()
    {
        int w = 64, h = 64;
        var t = Make(w, h, true);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                // 1枚に1つの山形（内向き）＋外と内の細い線
                float f = v - Mathf.Abs(u - 0.5f) * 0.9f;
                float a = (f > 0.32f && f < 0.6f && Mathf.Abs(u - 0.5f) < 0.36f) ? 1f : 0f;
                if (v < 0.07f || v > 0.93f) a = 0.55f;
                px[y * w + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    static Texture2D ArcTex()
    {
        int w = 128, h = 64;
        var t = Make(w, h);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                float wear = Fbm(u * 9f, v * 4f);
                Color c = new Color(0.3f, 0.32f, 0.35f) * (0.85f + wear * 0.3f);
                if (v > 0.86f || v < 0.1f) c = Color.Lerp(c, new Color(0.86f, 0.62f, 0.12f), Mathf.Clamp01((wear - 0.3f) * 3f));
                if (Mathf.Repeat(u * 6, 1f) < 0.03f) c *= 0.5f;
                px[y * w + x] = c;
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    // 金属の削り目（円板の上面は同心のヘアライン、側面は周方向の筋）
    static Texture2D BrushedTex(int n)
    {
        var t = Make(n, n, true);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float line = Hash(Mathf.FloorToInt(r * 900f), 7) * 0.22f + Hash(x, y) * 0.05f;
                float g = 0.8f + line;
                px[y * n + x] = new Color(g, g, g, 1);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    // 塗装のはげ（縁から金属が見える）。白地に灰のはげ。色はマテリアルの色で付ける
    static Texture2D WornTex(int n)
    {
        var t = Make(n, n, true);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float wn = Fbm(u * 14f, v * 14f);
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                float chip = wn - edge * 2.2f;
                float g = chip > 0.5f ? 0.55f : 0.92f + (Hash(x, y) - 0.5f) * 0.06f;
                px[y * n + x] = new Color(g, g, g, 1);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    static Texture2D StandsTex()
    {
        int w = 512, h = 64;
        var t = Make(w, h, true);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float tier = Mathf.Repeat(y / 8f, 1f);
                Color c = new Color(0.05f, 0.055f, 0.07f) * (tier < 0.15f ? 0.6f : 1f);
                // 観客のスマホの小さな光
                if (Hash(x, y) > 0.985f && tier > 0.3f) c = new Color(0.9f, 0.85f, 0.7f) * (0.4f + Hash(y, x) * 0.6f);
                px[y * w + x] = c;
            }
        t.SetPixels32(px); t.Apply(false);
        return t;
    }

    // 竜巻の筋
    static Texture2D FunnelTex()
    {
        int w = 128, h = 64;
        var t = Make(w, h, true);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                float s = Mathf.Repeat(u * 5f + v * 1.5f, 1f);
                float a = Mathf.Pow(s, 3f) * (0.4f + 0.6f * VNoise(u * 20f, v * 6f)) * Mathf.Clamp01(v * 4f) * Mathf.Clamp01((1 - v) * 3f);
                px[y * w + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    static Texture2D DotTex(int n)
    {
        var t = Make(n, n);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy)); a = a * a;
                px[y * n + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    // 回転の残像: 輪の上に流れる筋
    static Texture2D StreakTex(int n)
    {
        var t = Make(n, n);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                float d = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                float ring = Mathf.Clamp01((d - 0.5f) / 0.12f) * Mathf.Clamp01((1f - d) / 0.05f);
                float s = Mathf.Repeat(ang / (2 * Mathf.PI) * 3f, 1f);
                px[y * n + x] = new Color(1, 1, 1, ring * Mathf.Pow(s, 3f));
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }
}
