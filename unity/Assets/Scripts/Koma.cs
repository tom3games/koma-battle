using System.Collections.Generic;
using UnityEngine;

public struct Stats
{
    public float atk, def, mass, stamina, speed, grip, jump, air, radius, dashCd, decay, aerial, gaugeRate, steal;
}

// パーツ（上・芯・足 各5種）。名前と説明は画面側（HTML）が持つ。ここは数値と形だけ
public static class Parts
{
    public const int Count = 5;
    public static Stats Compute(int top, int core, int leg)
    {
        var s = new Stats { atk = 1, def = 1, mass = 1, stamina = 1, speed = 1, grip = 1, jump = 1, air = 1, radius = 0.8f, dashCd = 1, decay = 1, aerial = 1, gaugeRate = 1, steal = 0 };
        switch (top)
        {
            case 0: s.atk = 1.35f; s.def = 0.85f; s.mass = 1.0f; s.radius = 0.82f; break;                         // ブラッドファング
            case 1: s.atk = 0.85f; s.def = 1.4f; s.mass = 1.3f; s.radius = 0.86f; break;                          // アイギスウォール
            case 2: s.atk = 1.05f; s.def = 0.95f; s.mass = 0.82f; s.air = 1.7f; s.radius = 0.8f; s.aerial = 1.15f; break; // ゼファーウイング
            case 3: s.atk = 1.15f; s.def = 1.1f; s.mass = 1.05f; s.radius = 0.83f; break;                         // ヴォーテックスエッジ
            case 4: s.atk = 1.5f; s.def = 0.75f; s.mass = 1.15f; s.radius = 0.88f; s.speed = 0.95f; break;        // ルナティックサイズ
        }
        switch (core)
        {
            case 0: s.stamina = 1.35f; s.speed *= 0.92f; break;                    // エターナルコア
            case 1: s.stamina = 0.85f; s.speed *= 1.22f; s.dashCd = 0.75f; break;  // ソニックコア
            case 2: s.stamina = 1.05f; s.speed *= 0.95f; s.mass += 0.35f; s.def *= 1.1f; break; // グラビティコア
            case 3: s.stamina = 0.95f; s.gaugeRate = 1.55f; break;                 // フェニックスコア
            case 4: s.stamina = 1.0f; s.steal = 0.35f; s.mass += 0.15f; break;     // アビスコア
        }
        switch (leg)
        {
            case 0: s.grip = 1.7f; s.jump = 0.85f; s.speed *= 0.95f; break;          // タイタンベース
            case 1: s.grip = 0.6f; s.speed *= 1.15f; s.decay = 0.7f; break;          // スティンガーニードル
            case 2: s.jump = 1.45f; s.aerial *= 1.3f; break;                         // ヘヴンズスプリング
            case 3: s.grip = 0.8f; s.speed *= 1.3f; s.decay = 1.15f; break;          // ファントムホイール
            case 4: s.grip = 1.3f; s.jump = 1.15f; s.aerial *= 1.15f; break;         // クレイモアスパイク
        }
        return s;
    }
}

// コマ1つ。物理は自前（高さ関数の上をすべる・跳ぶ）
public class Koma
{
    public int idx;
    public bool isPlayer, cpu;
    public int top, core, leg, sp;   // sp = 必殺技の種類
    public Stats st;
    public Vector3 pos, vel;
    public bool grounded, alive = true, down;
    public float spin, spinMax, gauge;
    public float dashCd, dashT, airDashUsed, padCd, boostCd, hitFlash;
    public float specialT; public int specialPhase; public Vector3 specialTarget; public int rushLeft;
    public float shieldT, tornadoT;
    public string koWhy = "";
    public float koTime;
    public bool lastGroundRamp; public float lastVy;
    public Color color;
    public float dealt; public int aerialHits, specials, dashes;
    public float lossWall, lossHit, lossDash, lastHitT = -99;
    public int diveTarget = -1;

    public GameObject root; Transform tilt, spinner, ghost, shadow, marker, arrow, shield, funnel;
    TrailRenderer trail;
    float spinAngle, wobblePhase, downT;
    Vector3 downAxis;
    Materials mats;
    Material ghostMat, shadowMat;

    public float SpinRatio => Mathf.Clamp01(spin / spinMax);
    public float Radius => st.radius;
    public Vector3 Center => pos + Vector3.up * 0.42f;

    public Koma(int idx, int top, int core, int leg, Color color, Materials m)
    {
        this.idx = idx; this.top = top; this.core = core; this.leg = leg; this.color = color; mats = m;
        st = Parts.Compute(top, core, leg);
        spinMax = 160f * st.stamina; spin = spinMax;
        BuildVisual(m);
    }

    public void Rebuild(int t, int c, int l, Materials m)
    {
        top = Mathf.Clamp(t, 0, Parts.Count - 1); core = Mathf.Clamp(c, 0, Parts.Count - 1); leg = Mathf.Clamp(l, 0, Parts.Count - 1);
        st = Parts.Compute(top, core, leg);
        spinMax = 160f * st.stamina; spin = spinMax;
        var p = root.transform.position;
        // 作り直す前に、このコマだけの形と素材を捨てる（WebGL のメモリが増え続けないように）
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh) Object.Destroy(mf.sharedMesh);
        foreach (var rd in root.GetComponentsInChildren<Renderer>(true)) foreach (var mt in rd.sharedMaterials) if (mt) Object.Destroy(mt);
        Object.Destroy(root);
        BuildVisual(m);
        root.transform.position = p;
    }

    // 金属の部品（削り目）・塗装の部品（はげ）・黒い樹脂
    Material Metal(float shade, float gloss = 0.78f) => mats.Lit(new Color(shade, shade * 1.01f, shade * 1.04f), gloss, 0.95f, mats.Brushed);
    Material Paint(Color c) => mats.Lit(Color.Lerp(c, Color.white, 0.12f) * 1.1f, 0.5f, 0.25f, mats.Worn);
    Material Resin() => mats.Lit(new Color(0.07f, 0.075f, 0.085f), 0.35f, 0.1f);

    void BuildVisual(Materials m)
    {
        root = new GameObject("Koma" + idx);
        tilt = new GameObject("tilt").transform; tilt.SetParent(root.transform, false);
        spinner = new GameObject("spin").transform; spinner.SetParent(tilt, false);

        var body = Paint(color);
        var bodyDark = Paint(Color.Lerp(color, Color.black, 0.45f));
        var steel = Metal(0.85f, 0.88f);
        var steelDark = Metal(0.22f, 0.45f);
        var brass = mats.Lit(new Color(0.72f, 0.56f, 0.3f), 0.8f, 0.95f, mats.Brushed);
        var resin = Resin();

        // 足
        switch (leg)
        {
            case 0:
                Put(MeshKit.Frustum(0.36f, 0.34f, 0.06f, 32, true), steelDark, 0f);
                Put(MeshKit.Frustum(0.34f, 0.24f, 0.14f, 32, true), resin, 0.06f);
                Put(MeshKit.Frustum(0.25f, 0.29f, 0.12f, 32, true), steel, 0.2f);
                break;
            case 1:
                Put(MeshKit.Frustum(0.015f, 0.12f, 0.2f, 24, true), steel, 0f);
                Put(MeshKit.Frustum(0.12f, 0.28f, 0.14f, 24, true), steelDark, 0.2f);
                break;
            case 2:
                Put(MeshKit.Frustum(0.1f, 0.16f, 0.07f, 24, true), steel, 0f);
                for (int i = 0; i < 4; i++) Put(MeshKit.Frustum(0.24f, 0.24f, 0.04f, 28, true), i % 2 == 0 ? brass : resin, 0.07f + i * 0.065f);
                Put(MeshKit.Frustum(0.28f, 0.3f, 0.06f, 28, true), steelDark, 0.33f);
                break;
            case 3:
                Put(MeshKit.Frustum(0.33f, 0.36f, 0.08f, 36, true), resin, 0f);
                for (int i = 0; i < 6; i++)
                {
                    var w = Put(MeshKit.Frustum(0.07f, 0.07f, 0.06f, 12, true), steel, 0.01f);
                    float a = i * 60f * Mathf.Deg2Rad;
                    w.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.3f, 0.01f, Mathf.Sin(a) * 0.3f);
                    w.transform.localRotation = Quaternion.Euler(0, -i * 60f, 90f);
                }
                Put(MeshKit.Frustum(0.3f, 0.28f, 0.22f, 32, true), steelDark, 0.08f);
                break;
            case 4:
                for (int i = 0; i < 3; i++)
                {
                    var sp2 = Put(MeshKit.Frustum(0.015f, 0.08f, 0.2f, 12, true), steel, 0f);
                    float a = i * 120f * Mathf.Deg2Rad;
                    sp2.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.1f, 0f, Mathf.Sin(a) * 0.1f);
                }
                Put(MeshKit.Frustum(0.18f, 0.3f, 0.16f, 28, true), steelDark, 0.18f);
                break;
        }
        float ry = 0.34f;
        // 上（攻めの輪）: 下に鋼の受け皿、上に塗装の輪、刃は鋼
        switch (top)
        {
            case 0:
                Put(MeshKit.Frustum(0.56f, 0.6f, 0.05f, 40, true), steelDark, ry);
                Put(MeshKit.Frustum(0.6f, 0.55f, 0.1f, 40, true), body, ry + 0.05f);
                for (int i = 0; i < 4; i++) Blade(new Vector3(0.36f, 0.08f, 0.13f), steel, ry + 0.06f, i * 90f, 0.6f, -32f, 0, 1.25f);
                break;
            case 1:
                Put(MeshKit.Frustum(0.86f, 0.82f, 0.06f, 48, true), steel, ry);
                Put(MeshKit.Frustum(0.82f, 0.66f, 0.15f, 48, true), body, ry + 0.06f);
                for (int i = 0; i < 8; i++) Blade(new Vector3(0.1f, 0.05f, 0.2f), steelDark, ry + 0.2f, i * 45f, 0.55f, 0, 0, 1f);
                break;
            case 2:
                Put(MeshKit.Frustum(0.48f, 0.5f, 0.08f, 36, true), body, ry);
                for (int i = 0; i < 3; i++) Blade(new Vector3(0.52f, 0.03f, 0.24f), steel, ry + 0.07f, i * 120f, 0.6f, -24f, 16f, 1f);
                break;
            case 3:
                Put(MeshKit.Frustum(0.6f, 0.6f, 0.05f, 40, true), steelDark, ry);
                Put(MeshKit.Frustum(0.58f, 0.5f, 0.1f, 40, true), body, ry + 0.05f);
                for (int i = 0; i < 6; i++) Blade(new Vector3(0.28f, 0.06f, 0.08f), steel, ry + 0.06f, i * 60f, 0.62f, -48f, 0, 1f);
                break;
            case 4:
                Put(MeshKit.Frustum(0.5f, 0.52f, 0.1f, 40, true), body, ry);
                for (int i = 0; i < 2; i++)
                {
                    Blade(new Vector3(0.7f, 0.05f, 0.12f), steel, ry + 0.06f, i * 180f, 0.62f, -14f, 0, 1f);
                    Blade(new Vector3(0.36f, 0.05f, 0.08f), steel, ry + 0.06f, i * 180f + 38f, 0.82f, -60f, 0, 1f);
                }
                break;
        }
        // 芯: 色の付いた筒＋鋼の帽子＋ボルト
        Color coreC = core switch { 0 => new Color(0.36f, 0.5f, 0.38f), 1 => new Color(0.3f, 0.42f, 0.6f), 2 => new Color(0.3f, 0.3f, 0.33f), 3 => new Color(0.66f, 0.38f, 0.16f), _ => new Color(0.18f, 0.36f, 0.38f) };
        Put(MeshKit.Frustum(0.3f, 0.27f, 0.28f, 32, true), Paint(coreC), ry + 0.12f);
        Put(MeshKit.Frustum(0.27f, 0.2f, 0.06f, 32, true), steel, ry + 0.4f);
        Put(MeshKit.Frustum(0.13f, 0.1f, 0.06f, 24, true), brass, ry + 0.46f);
        for (int i = 0; i < 4; i++)
        {
            var b = Put(MeshKit.Frustum(0.03f, 0.03f, 0.03f, 8, true), steelDark, ry + 0.46f);
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            b.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.19f, ry + 0.445f, Mathf.Sin(a) * 0.19f);
        }
        if (core == 2) Put(MeshKit.Frustum(0.06f, 0.06f, 0.1f, 6, true), steelDark, ry + 0.52f);
        // 片側の目印（回っているのが見える）
        var mark = Put(MeshKit.Box(new Vector3(0.16f, 0.025f, 0.05f)), bodyDark, ry + 0.405f);
        mark.transform.localPosition = new Vector3(0.18f, ry + 0.415f, 0);

        CombineSpinner();

        // 回転の残像（半透明の筋の輪）
        var gm = m.Alpha(Color.Lerp(color, Color.white, 0.3f), m.Streak);
        ghost = MeshKit.Obj("ghost", tilt, MeshKit.Disc(Radius * 1.22f, 32), gm).transform;
        ghost.localPosition = Vector3.up * (ry + 0.12f);
        ghost.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ghostMat = ghost.GetComponent<MeshRenderer>().material;

        var sm = m.Alpha(new Color(0, 0, 0, 0.6f), m.Dot);
        shadow = MeshKit.Obj("shadow", root.transform, MeshKit.Disc(Radius * 1.1f, 24), sm).transform;
        shadow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowMat = shadow.GetComponent<MeshRenderer>().material;
        var mk = m.Alpha(Color.Lerp(color, Color.white, 0.2f) * new Color(1, 1, 1, 0.55f), Texture2D.whiteTexture);
        marker = MeshKit.Obj("marker", root.transform, MeshKit.Ring(Radius * 1.18f, Radius * 1.26f, 40), mk).transform;
        marker.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // ダッシュの向きの矢印（足元の山形。狙っている間だけ）
        arrow = MeshKit.Obj("arrow", root.transform, ArrowMesh(), m.Alpha(new Color(1f, 0.85f, 0.4f, 0.95f), Texture2D.whiteTexture)).transform;
        arrow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arrow.gameObject.SetActive(false);
        // 盾（イージス・リバース）と竜巻（テンペスト・ヴォルテックス）
        shield = MeshKit.Obj("shield", root.transform, Dome(1.3f), m.Alpha(new Color(1f, 0.8f, 0.4f, 0.32f), Texture2D.whiteTexture)).transform;
        var sring = MeshKit.Obj("shieldRing", shield, MeshKit.Ring(1.22f, 1.36f, 40), m.AddGlow(new Color(1f, 0.8f, 0.4f, 1f)));
        sring.transform.localPosition = new Vector3(0, 0.05f, 0);
        shield.gameObject.SetActive(false);
        shield.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        funnel = MeshKit.Obj("funnel", root.transform, MeshKit.Frustum(1.0f, 4.2f, 5.5f, 32, false), m.Alpha(new Color(0.85f, 0.9f, 0.95f, 0.75f), m.Funnel)).transform;
        funnel.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        funnel.gameObject.SetActive(false);

        trail = root.AddComponent<TrailRenderer>();
        trail.material = m.Alpha(Color.white, Texture2D.whiteTexture);
        trail.time = 0.22f;
        trail.widthMultiplier = Radius * 1.3f;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0), new GradientColorKey(color, 1) },
            new[] { new GradientAlphaKey(0.6f, 0), new GradientAlphaKey(0, 1) });
        trail.colorGradient = grad;
        trail.minVertexDistance = 0.08f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.emitting = false;
    }

    // 一緒に回る部品を素材ごとに1つの形へまとめる（描く回数を減らす。見た目は同じ）
    void CombineSpinner()
    {
        var groups = new Dictionary<Material, List<CombineInstance>>();
        var olds = new List<GameObject>();
        var toLocal = spinner.worldToLocalMatrix;
        foreach (var mf in spinner.GetComponentsInChildren<MeshFilter>())
        {
            var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
            if (!groups.TryGetValue(mat, out var list)) groups[mat] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = toLocal * mf.transform.localToWorldMatrix });
            olds.Add(mf.gameObject);
        }
        foreach (var kv in groups)
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(kv.Value.ToArray(), true, true);
            var g = MeshKit.Obj("part", spinner, mesh, kv.Key);
            g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity; g.transform.localScale = Vector3.one;
        }
        foreach (var o in olds) { var mf = o.GetComponent<MeshFilter>(); if (mf.sharedMesh) Object.Destroy(mf.sharedMesh); o.SetActive(false); Object.Destroy(o); }
    }

    void Blade(Vector3 size, Material mat, float y, float yaw, float dist, float sweep, float pitch, float stretch)
    {
        var f = Put(MeshKit.Box(size), mat, y);
        var rot = Quaternion.Euler(0, yaw, 0);
        f.transform.localRotation = rot;
        f.transform.localPosition = rot * new Vector3(dist, 0, 0) + Vector3.up * y;
        f.transform.localRotation *= Quaternion.Euler(pitch, sweep, 0);
        f.transform.localScale = new Vector3(stretch, 1, 1);
    }

    static Mesh ArrowMesh()
    {
        var mb = new MeshKit.Builder { twoSided = true };
        Vector2 z = Vector2.zero;
        for (int i = 0; i < 2; i++)
        {
            float o = 1.25f + i * 0.45f;
            mb.Quad(new Vector3(-0.42f, 0, o - 0.28f), new Vector3(0, 0, o), new Vector3(0, 0, o + 0.18f), new Vector3(-0.42f, 0, o - 0.1f), z, z, z, z);
            mb.Quad(new Vector3(0, 0, o), new Vector3(0.42f, 0, o - 0.28f), new Vector3(0.42f, 0, o - 0.1f), new Vector3(0, 0, o + 0.18f), z, z, z, z);
        }
        return mb.Mesh();
    }

    static Mesh Dome(float r)
    {
        var mb = new MeshKit.Builder { twoSided = false };
        int seg = 24, rings = 8;
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < seg; j++)
            {
                float t0 = i / (float)rings * Mathf.PI / 2, t1 = (i + 1) / (float)rings * Mathf.PI / 2;
                float a0 = j * Mathf.PI * 2 / seg, a1 = (j + 1) * Mathf.PI * 2 / seg;
                System.Func<float, float, Vector3> P = (th, a) => new Vector3(Mathf.Cos(th) * Mathf.Cos(a) * r, Mathf.Sin(th) * r * 0.8f, Mathf.Cos(th) * Mathf.Sin(a) * r);
                mb.Quad(P(t0, a0), P(t1, a0), P(t1, a1), P(t0, a1), new Vector2(j / (float)seg, i / (float)rings), new Vector2(j / (float)seg, (i + 1f) / rings), new Vector2((j + 1f) / seg, (i + 1f) / rings), new Vector2((j + 1f) / seg, i / (float)rings));
            }
        return mb.Mesh();
    }

    GameObject Put(Mesh mesh, Material mat, float y)
    {
        var g = MeshKit.Obj("p", spinner, mesh, mat);
        g.transform.localPosition = new Vector3(0, y, 0);
        return g;
    }

    public void Place(Vector3 p)
    {
        pos = p; vel = Vector3.zero; grounded = false; alive = true; down = false; downT = 0;
        spin = spinMax; gauge = 0; dashCd = 0; dashT = 0; airDashUsed = 0; specialPhase = 0; koWhy = ""; shieldT = 0; tornadoT = 0; rushLeft = 0;
        dealt = 0; aerialHits = 0; specials = 0; dashes = 0; lossWall = 0; lossHit = 0; lossDash = 0; diveTarget = -1; boostCd = 0;
        padCd = 0; hitFlash = 0; lastVy = 0; lastHitT = -99; specialT = 0;
        tilt.localRotation = Quaternion.identity;
        root.SetActive(true);
        root.transform.position = pos;
        trail.Clear();
    }

    public void Knock(string why)
    {
        if (!alive) return;
        alive = false; koWhy = why; shieldT = 0; tornadoT = 0;
        if (why == "spin") { down = true; downAxis = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized; if (downAxis == Vector3.zero) downAxis = Vector3.right; }
    }

    // 狙いの矢印: dir が 0 なら隠す
    public void SetArrow(Vector2 dir)
    {
        bool on = dir.sqrMagnitude > 0.01f && alive;
        arrow.gameObject.SetActive(on);
        if (!on) return;
        float gh = Arena.H(pos.x, pos.z);
        arrow.position = new Vector3(pos.x, (gh > -100 ? Mathf.Min(gh, pos.y) : pos.y) + 0.06f, pos.z);
        arrow.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
    }

    // 見た目の更新（体力は数字でなく、回転の速さ・残像の濃さ・首振りで見せる）
    public void Visual(float dt, float realDt)
    {
        root.transform.position = pos;
        float sr = SpinRatio;
        float w = alive ? Mathf.Lerp(7f, 46f, Mathf.Sqrt(sr)) : (down ? Mathf.Max(0, 8f - downT * 10f) : 30f);
        if (specialPhase > 0 || tornadoT > 0) w = 70f;
        spinAngle += w * dt * Mathf.Rad2Deg;
        spinner.localRotation = Quaternion.Euler(0, -spinAngle, 0);
        wobblePhase += dt * Mathf.Lerp(2f, 8f, 1 - sr);
        float wob = alive ? Mathf.Lerp(0f, 22f, Mathf.Pow(1 - sr, 1.6f)) : 0;
        if (down)
        {
            downT += dt;
            float k = Mathf.Clamp01(downT / 0.7f);
            float bounce = Mathf.Abs(Mathf.Sin(downT * 14f)) * (1 - k) * 12f;
            tilt.localRotation = Quaternion.AngleAxis(Mathf.Lerp(0, 74f, k * k) + bounce, downAxis);
        }
        else
        {
            Vector3 lean = new Vector3(vel.z, 0, -vel.x) * 1.1f;
            if (lean.magnitude > 12f) lean = lean.normalized * 12f;
            tilt.localRotation = Quaternion.Euler(Mathf.Sin(wobblePhase) * wob + lean.x, 0, Mathf.Cos(wobblePhase) * wob + lean.z);
        }
        float ga = alive ? Mathf.Lerp(0.0f, 0.55f, sr * sr) * (specialPhase > 0 ? 1.4f : 1f) : 0;
        if (hitFlash > 0) { ga = 0.9f; hitFlash -= realDt; }
        var gc = Color.Lerp(color, Color.white, 0.3f); gc.a = Mathf.Clamp01(ga);
        ghostMat.SetColor("_TintColor", gc * 0.5f);
        ghost.localRotation = Quaternion.Euler(0, -spinAngle * 0.37f, 0);

        float gh = Arena.H(pos.x, pos.z);
        if (gh > -100)
        {
            shadow.gameObject.SetActive(true);
            shadow.position = new Vector3(pos.x, gh + 0.03f, pos.z);
            float hgt = Mathf.Max(0, pos.y - gh);
            shadow.localScale = Vector3.one * Mathf.Clamp(1f - hgt * 0.07f, 0.5f, 1f);
            shadowMat.SetColor("_TintColor", new Color(0, 0, 0, Mathf.Clamp(0.6f - hgt * 0.05f, 0.25f, 0.6f)) * 0.5f);
        }
        else shadow.gameObject.SetActive(false);
        marker.gameObject.SetActive(alive);
        marker.position = new Vector3(pos.x, (gh > -100 ? gh : pos.y) + 0.05f, pos.z);
        shield.gameObject.SetActive(shieldT > 0 && alive);
        if (shieldT > 0) { shield.position = pos; shield.rotation = Quaternion.Euler(0, spinAngle * 0.2f, 0); }
        funnel.gameObject.SetActive(tornadoT > 0 && alive);
        if (tornadoT > 0)
        {
            funnel.position = new Vector3(pos.x, (gh > -100 ? gh : pos.y) - 0.2f, pos.z);
            funnel.rotation = Quaternion.Euler(0, -spinAngle * 0.6f, 0);
            float grow = Mathf.Clamp01((3.5f - tornadoT) / 0.4f) * Mathf.Clamp01(tornadoT / 0.3f);
            funnel.localScale = new Vector3(grow, Mathf.Max(0.05f, grow), grow);
        }
        trail.emitting = alive && (dashT > 0 || specialPhase > 0 || boostCd > 0.5f || (!grounded && vel.magnitude > 6f));
    }
}
