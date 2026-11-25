using UnityEngine;

// 素材（Resources の元の素材を複製して色と模様をつける）。模様はコードで描く
public class Materials
{
    Material std, stdE, vtx, add, alpha;
    public Material Floor, Base, Ramp, Arc, Pad, Sky, Line;
    public Texture2D Dot, Streak;

    public Materials()
    {
        std = Resources.Load<Material>("M_Std");
        stdE = Resources.Load<Material>("M_StdEmis");
        vtx = Resources.Load<Material>("M_Vtx");
        add = Resources.Load<Material>("M_Add");
        alpha = Resources.Load<Material>("M_Alpha");
        Dot = DotTex(64);
        Streak = StreakTex(128);
        Floor = Lit(Color.white, 0.3f, 0.0f, FloorTex(1024));
        Base = Lit(new Color(0.16f, 0.17f, 0.24f), 0.35f, 0.3f);
        Ramp = Lit(Color.white, 0.5f, 0.05f, ChevronTex());
        Ramp.mainTextureScale = new Vector2(1, 1);
        Arc = Lit(Color.white, 0.5f, 0.1f, ArcTex());
        Pad = Emis(new Color(0.1f, 0.35f, 0.4f), new Color(0.1f, 0.9f, 0.8f) * 0.9f);
        Sky = new Material(vtx);
        Line = new Material(add) { mainTexture = Dot };
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

    static Texture2D Make(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        return t;
    }

    static float Hash(int x, int y) { unchecked { int n = x * 374761393 + y * 668265263; n = (n ^ (n >> 13)) * 1274126177; return ((n ^ (n >> 16)) & 0xffff) / 65535f; } }

    // 床: 同心円の線・放射の線・真ん中の紋・縁と切れ目の警告帯
    static Texture2D FloorTex(int n)
    {
        var t = Make(n, n);
        var px = new Color32[n * n];
        Color baseIn = new Color(0.6f, 0.65f, 0.78f), baseOut = new Color(0.42f, 0.47f, 0.66f);
        Color lip = new Color(0.13f, 0.16f, 0.32f), lineC = new Color(1f, 1f, 1f);
        Color hazA = new Color(1f, 0.32f, 0.12f), hazB = new Color(1f, 0.85f, 0.2f);
        Color mark = new Color(1f, 0.42f, 0.1f);
        float px2w = 2 * Arena.Edge / n;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float wx = (x + 0.5f) / n * 2 * Arena.Edge - Arena.Edge, wz = (y + 0.5f) / n * 2 * Arena.Edge - Arena.Edge;
                float r = Mathf.Sqrt(wx * wx + wz * wz), ang = Mathf.Atan2(wz, wx);
                Color c;
                if (r <= Arena.R)
                {
                    c = Color.Lerp(baseIn, baseOut, Mathf.Pow(r / Arena.R, 1.5f));
                    // 同心円
                    float ringD = Mathf.Abs(Mathf.Repeat(r + 1.3f, 2.6f) - 1.3f);
                    float lineA = 1f - Mathf.Clamp01((ringD - 0.035f) / px2w);
                    // 放射
                    float sp = Mathf.Abs(Mathf.Repeat(ang * Mathf.Rad2Deg + 15f, 30f) - 15f) * Mathf.Deg2Rad * r;
                    float spA = (1f - Mathf.Clamp01((sp - 0.02f) / px2w)) * Mathf.Clamp01((r - 4.2f) / 0.5f) * 0.55f;
                    c = Color.Lerp(c, lineC, Mathf.Max(lineA * 0.8f, spA));
                    // 切れ目の手前を赤く
                    float g = Arena.GapFactor(ang) * Mathf.Clamp01((r - (Arena.R - 2.2f)) / 2.2f);
                    c = Color.Lerp(c, new Color(1f, 0.55f, 0.45f), g * 0.55f);
                    // 真ん中の紋（オリジナルの六つ星）
                    float k = Mathf.Cos(ang * 6) * 0.5f + 0.5f;
                    float star = 1.0f + 0.6f * Mathf.Pow(k, 3);
                    if (r < star) c = Color.Lerp(c, mark, 1f - Mathf.Clamp01((r - star + 0.05f) / px2w));
                    if (r < 0.55f) c = Color.Lerp(c, Color.white, 1f - Mathf.Clamp01((r - 0.5f) / px2w));
                    // 外周の太線
                    float outer = 1f - Mathf.Clamp01((Mathf.Abs(r - (Arena.R - 0.25f)) - 0.12f) / px2w);
                    c = Color.Lerp(c, new Color(0.2f, 0.28f, 0.6f), outer);
                }
                else
                {
                    c = lip;
                    float g = Arena.GapFactor(ang);
                    if (g > 0.01f)
                    {
                        float st = Mathf.Repeat(ang * Arena.Edge * 2.2f + r * 2.2f, 1f) < 0.5f ? 1 : 0;
                        c = Color.Lerp(c, Color.Lerp(hazA, hazB, st), g);
                    }
                    // 縁の上の白い線
                    float top = 1f - Mathf.Clamp01((Mathf.Abs(r - (Arena.R + 0.8f)) - 0.05f) / px2w);
                    c = Color.Lerp(c, Color.white, top * 0.7f * (1 - g));
                }
                float nz = (Hash(x, y) - 0.5f) * 0.035f;
                c.r += nz; c.g += nz; c.b += nz;
                px[y * n + x] = c;
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    // 坂: 内側へ向く山形の矢印
    static Texture2D ChevronTex()
    {
        int w = 256, h = 128;
        var t = Make(w, h);
        var px = new Color32[w * h];
        Color a = new Color(1f, 0.5f, 0.12f), b = new Color(1f, 0.93f, 0.8f), edge = new Color(0.25f, 0.1f, 0.05f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                float f = Mathf.Repeat(u * 5f - Mathf.Abs(v - 0.5f) * 1.6f, 1f);
                Color c = f < 0.32f ? b : a;
                if (v < 0.06f || v > 0.94f) c = edge;
                if (u < 0.03f) c = Color.white;
                px[y * w + x] = c;
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
                Color c = new Color(0.1f, 0.66f, 0.62f);
                if (v > 0.86f || v < 0.1f) c = new Color(0.95f, 1f, 0.98f);
                if (Mathf.Repeat(u * 6, 1f) < 0.06f) c = new Color(0.05f, 0.4f, 0.38f);
                px[y * w + x] = c;
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
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1 - d); a = a * a;
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
                float ring = Mathf.Clamp01((d - 0.45f) / 0.15f) * Mathf.Clamp01((1f - d) / 0.06f);
                float s = Mathf.Repeat(ang / (2 * Mathf.PI) * 4f, 1f);
                float a = ring * Mathf.Pow(s, 2.2f);
                px[y * n + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }
}
