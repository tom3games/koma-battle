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
            case 1: s.stamina = 0.92f; s.speed *= 1.22f; s.dashCd = 0.75f; break;  // ソニックコア
            case 2: s.stamina = 1.05f; s.speed *= 0.95f; s.mass += 0.35f; s.def *= 1.1f; break; // グラビティコア
            case 3: s.stamina = 0.95f; s.gaugeRate = 1.55f; break;                 // フェニックスコア
            case 4: s.stamina = 1.0f; s.steal = 0.35f; s.mass += 0.15f; break;     // アビスコア
        }
        switch (leg)
        {
            case 0: s.grip = 1.7f; s.jump = 0.85f; s.speed *= 0.95f; break;          // タイタンベース
            case 1: s.grip = 0.6f; s.speed *= 1.15f; s.decay = 0.7f; break;          // スティンガーニードル
            case 2: s.jump = 1.45f; s.aerial *= 1.3f; break;                         // ヘヴンズスプリング
            case 3: s.grip = 0.8f; s.speed *= 1.3f; s.decay = 1.05f; break;          // ファントムホイール
            case 4: s.grip = 1.3f; s.jump = 1.15f; s.aerial *= 1.15f; break;         // クレイモアスパイク
        }
        return s;
    }
}

// コマ1つ。物理は自前（高さ関数の上をすべる・跳ぶ）
public class Koma
{
    public const float SpinBase = 270f;   // 2026-11-28 本人「全体の回転数の基礎をあげて」: 160→270（回転切れで終わるのを遅く）
    public int idx;
    public bool isPlayer, cpu;
    public int top, core, leg, sp;   // sp = 必殺技の種類
    public Stats st;
    public Vector3 pos, vel;
    public bool grounded, alive = true, down;
    public float spin, spinMax, gauge;
    public float dashCd, dashT, lockT, airDashUsed, padCd, boostCd, hitFlash;
    public float specialT; public int specialPhase; public Vector3 specialTarget; public int rushLeft;
    public float shieldT, tornadoT;
    // 2026-11-28 必殺4つ追加: アンカー（重くなる）・凍え・氷の矢の溜め/突進・鎖
    public Vector2 inputMv;   // いまの移動の入力（世界の向き。CPU は Think の move）
    public bool braceHold; public float braceT, braceFxT; public bool Bracing => alive && (braceHold || braceT > 0f);
    public bool recoverUsed; public float wallFxT, dashBrakeT, arrowLen, dashGuardT, dashStop; public Vector3 dashFrom, dashDirG; public float anchorT, frostT, arrowT, arrowDashT, chainSwing; public Vector3 arrowDir; public int arrowRings;
    public Koma chainTo, chainFrom; public float chainT; public Vector3 arrowFrom; public float lsPow, thrownT;
    public float superT, superCd, boardT, fireT, fireCd, hiT, burnSnd, lastShiftT = -9; public bool jumpReq; public Vector3 lastFire;
    public string koWhy = "";
    public float koTime;
    public bool lastGroundRamp; public float lastVy;
    public Color color;
    public float dealt; public int aerialHits, specials, dashes;
    public float lossWall, lossHit, lossDash, lastHitT = -99;
    public int diveTarget = -1;
    public bool left;   // 左回転（逆回り）
    // パッシブ（-1 なし / 0 逆鱗 1 天翔 2 魂喰らい 3 雷鳴の刻印 4 不屈 5 紅蓮の轍 6 影分身 7 黒鉄の盾）
    public int pas = -1; public float baseAtk = 1f, pasCd;
    // 2026-11-28 パーツ破損: 刃（外周・横の体当たりで削れる）・円盤（中・上からで削れる）・軸（下・かち上げで削れる）。壊れ方は KomaParts.cs
    public float pb0, pb1, pb2, pbm0, pbm1, pbm2, atk0, mass0; public bool bladeBroken, diskBroken; public readonly float[] pbLast = new float[3];
    public void RecalcParts()
    {
        atk0 = st.atk; mass0 = st.mass;
        pbm0 = 100f * Mathf.Pow(Mathf.Clamp(st.atk, 0.8f, 1.6f), 1.3f);   // 刃: 攻撃型が固い
        pbm1 = 100f * Mathf.Clamp(st.mass, 0.8f, 1.9f);                      // 円盤: 重量型が固い
        pbm2 = 100f * Mathf.Clamp(st.speed, 0.85f, 1.7f);                    // 軸: 速度型が固い
        pb0 = pbm0; pb1 = pbm1; pb2 = pbm2; bladeBroken = diskBroken = false;
    }
    // 場外ギリギリの踏ん張り（2026-11-27 本人「二は欲しい」）: 縁に引っかかって外へ傾いている残り時間・待ち・場所・外の向き
    // WASD 2回押しの傾け（2026-11-27 本人「二回連続クリックしたら意図的にその方向に角度をつけれるように」）: 残り時間・待ち・向き
    public float leanT, leanCd; public Vector3 leanDir;
    public float clingT, clingCd, clingWin, cpuSaveAt; public bool clingHit; public Vector3 clingAt, clingOut;
    public bool rage, gutsUsed, guard, rescueSet, wasAir; public int stampN, hitsTaken; public Koma stampBy; public Vector3 rescue;
    Transform frostShell, iceTail, iceHead, anchorRing, anchorGlowRing, anchorCrack, anchorCrackGlow; Material anchorCrackGlowMat; static Mesh anchorCrackGlowMesh; Material frostMat, tailMat, headMat, anchorMat, anchorGlowMat, anchorCrackMat;
    static Mesh anchorCrackMesh; static int anchorCrackSegs;
    Transform wings, hex; Transform[] stampObj = new Transform[3]; Material wingMat, hexMat, stampMat;
    float landAmp, landT = 99f, landAng, landSq;
    float auraT; Color auraC = Color.white; Transform aura, auraRing; Material auraMat, auraRingMat;
    public bool WingsOn => wings != null && wings.gameObject.activeSelf;
    public float TiltDeg => tilt ? Quaternion.Angle(Quaternion.identity, tilt.localRotation) : 0f;

    public GameObject root; Transform tilt, spinner, ghost, shadow, marker, arrow, shield, funnel;
    TrailRenderer trail;
    float spinAngle, wobblePhase, downT, downYaw;
    Vector3 downAxis;
    Materials mats;
    Material ghostMat, shadowMat;

    public float SpinRatio => Mathf.Clamp01(spin / spinMax);
    public float Radius => st.radius;
    public Vector3 Center => pos + Vector3.up * 0.42f;

    public Koma(int idx, int top, int core, int leg, Color color, Materials m, bool left = false)
    {
        this.left = left;
        this.idx = idx; this.top = top; this.core = core; this.leg = leg; this.color = color; mats = m;
        st = Parts.Compute(top, core, leg); baseAtk = st.atk; RecalcParts();
        spinMax = SpinBase * st.stamina; spin = spinMax;
        BuildVisual(m);
    }

    public void Rebuild(int t, int c, int l, Materials m, bool leftSpin = false)
    {
        left = leftSpin;
        top = Mathf.Clamp(t, 0, Parts.Count - 1); core = Mathf.Clamp(c, 0, Parts.Count - 1); leg = Mathf.Clamp(l, 0, Parts.Count - 1);
        st = Parts.Compute(top, core, leg); baseAtk = st.atk; RecalcParts();
        spinMax = SpinBase * st.stamina; spin = spinMax;
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

        // 氷: 凍えの殻・氷の矢の尾と先端
        frostShell = MeshKit.Obj("frost", root.transform, Dome(1.05f), m.Alpha(new Color(0.6f, 0.9f, 1f, 0.38f), Texture2D.whiteTexture)).transform;
        frostMat = frostShell.GetComponent<MeshRenderer>().material;
        frostShell.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; frostShell.gameObject.SetActive(false);
        iceTail = MeshKit.Obj("iceTail", root.transform, MeshKit.Frustum(0.95f, 0.06f, 8f, 20, false), m.AddGlow(new Color(0.6f, 0.9f, 1f, 1f), m.Funnel)).transform;
        tailMat = iceTail.GetComponent<MeshRenderer>().material; iceTail.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; iceTail.gameObject.SetActive(false);
        iceHead = MeshKit.Obj("iceHead", root.transform, MeshKit.Frustum(0.8f, 0.02f, 2.6f, 16, false), m.AddGlow(new Color(0.85f, 0.97f, 1f, 1f), m.Funnel)).transform;
        headMat = iceHead.GetComponent<MeshRenderer>().material; iceHead.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; iceHead.gameObject.SetActive(false);
        // グラビティ・アンカー: 足元の暗い輪・ひび・縁の光
        anchorRing = MeshKit.Obj("anchorRing", root.transform, MeshKit.Disc(2.4f, 32), m.Alpha(new Color(0.02f, 0.01f, 0.03f, 1f), m.Dot)).transform;
        anchorMat = anchorRing.GetComponent<MeshRenderer>().material; anchorRing.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; anchorRing.gameObject.SetActive(false);
        anchorGlowRing = MeshKit.Obj("anchorGlow", root.transform, MeshKit.Ring(2.0f, 2.4f, 40), m.AddGlow(new Color(0.9f, 0.28f, 0.75f, 1f))).transform;
        anchorGlowMat = anchorGlowRing.GetComponent<MeshRenderer>().material; anchorGlowRing.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; anchorGlowRing.gameObject.SetActive(false);
        if (anchorCrackMesh == null) anchorCrackMesh = Arena.CrackMesh(2.5f, 0.11f, 21, false, out anchorCrackSegs, 0.02f);
        if (anchorCrackGlowMesh == null) anchorCrackGlowMesh = Arena.CrackMesh(2.5f, 0.05f, 21, false, out _, 0.04f);
        anchorCrackGlow = MeshKit.Obj("anchorCrackGlow", root.transform, anchorCrackGlowMesh, m.AddGlow(new Color(0.9f, 0.28f, 0.75f, 1f))).transform;
        anchorCrackGlowMat = anchorCrackGlow.GetComponent<MeshRenderer>().material; anchorCrackGlow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; anchorCrackGlow.gameObject.SetActive(false);
        anchorCrack = MeshKit.Obj("anchorCrack", root.transform, anchorCrackMesh, m.Alpha(new Color(0.01f, 0.005f, 0.015f, 1f), Texture2D.whiteTexture)).transform;
        anchorCrackMat = anchorCrack.GetComponent<MeshRenderer>().material; anchorCrack.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; anchorCrack.gameObject.SetActive(false);

        // 必殺技のオーラ（光の柱＋足元の輪）。色は技ごと
        auraMat = m.AddGlow(Color.white, m.Funnel);
        aura = MeshKit.Obj("aura", root.transform, MeshKit.Frustum(1.15f, 0.55f, 3.6f, 28, false), auraMat).transform;
        aura.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        auraMat = aura.GetComponent<MeshRenderer>().material;
        aura.gameObject.SetActive(false);
        auraRing = MeshKit.Obj("auraRing", root.transform, MeshKit.Ring(0.9f, 1.25f, 40), m.AddGlow(Color.white)).transform;
        auraRing.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        auraRingMat = auraRing.GetComponent<MeshRenderer>().material;
        auraRing.gameObject.SetActive(false);

        // パッシブの見た目: 光の羽・六角の盾・紋章
        var wb = new MeshKit.Builder { twoSided = true }; Vector2 z0 = Vector2.zero;
        for (int sd = -1; sd <= 1; sd += 2)
            for (int i = 0; i < 4; i++)
            {
                float ang = (18f + i * 17f) * Mathf.Deg2Rad, len = 2.7f - i * 0.4f;
                Vector3 b0 = new Vector3(sd * 0.3f, 0.7f, 0), tip = b0 + new Vector3(sd * Mathf.Cos(ang), Mathf.Sin(ang) * 0.9f, -0.15f * i) * len;
                Vector3 nz = new Vector3(0, 0, 1);
                wb.Quad(b0 + nz * 0.3f, b0 - nz * 0.3f, tip - nz * 0.1f, tip + nz * 0.1f, z0, z0, z0, z0);
            }
        wingMat = m.AddGlow(Color.white);
        wings = MeshKit.Obj("wings", root.transform, wb.Mesh(), wingMat).transform;
        wings.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        wingMat = wings.GetComponent<MeshRenderer>().material; wings.gameObject.SetActive(false);
        hex = MeshKit.Obj("hex", root.transform, MeshKit.Frustum(1.15f, 1.15f, 1.1f, 6, false, false), m.AddGlow(Color.white)).transform;
        hex.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hexMat = hex.GetComponent<MeshRenderer>().material; hex.gameObject.SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            stampObj[i] = MeshKit.Obj("stamp", root.transform, MeshKit.Ring(0.22f, 0.36f, 6), m.AddGlow(Color.white)).transform;
            stampObj[i].GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (i == 0) stampMat = stampObj[i].GetComponent<MeshRenderer>().material; else stampObj[i].GetComponent<MeshRenderer>().sharedMaterial = stampMat;
            stampObj[i].gameObject.SetActive(false);
        }

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
        // 回る向きに合わせて刃の向きを鏡写しにする（右回りは鏡写し、左回りはもとの形）
        float mir = left ? 1f : -1f; yaw *= mir; sweep *= mir; pitch *= mir;
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
        pos = p; vel = Vector3.zero; grounded = false; alive = true; down = false; downT = 0; downYaw = 0;
        st.mass = mass0; baseAtk = atk0; pb0 = pbm0; pb1 = pbm1; pb2 = pbm2; bladeBroken = diskBroken = false;
        spin = spinMax; gauge = 0; dashCd = 0; dashT = 0; airDashUsed = 0; specialPhase = 0; koWhy = ""; shieldT = 0; tornadoT = 0; rushLeft = 0;
        dealt = 0; aerialHits = 0; specials = 0; dashes = 0; lossWall = 0; lossHit = 0; lossDash = 0; diveTarget = -1; boostCd = 0;
        padCd = 0; hitFlash = 0; lastVy = 0; lastHitT = -99; specialT = 0;
        superT = 0; superCd = 0; boardT = 0; fireT = 0; fireCd = 0; hiT = 0; burnSnd = 0; lastShiftT = -9; jumpReq = false; lastFire = new Vector3(0, -999, 0);
        tilt.localRotation = Quaternion.identity; tilt.localScale = Vector3.one; landT = 99f; landAmp = 0; auraT = 0;
        leanT = 0; leanCd = 0; dashBrakeT = 0; recoverUsed = false; braceHold = false; braceT = 0; braceFxT = 0; dashGuardT = 0;
        anchorT = 0; frostT = 0; arrowT = 0; arrowDashT = 0; arrowRings = 0; chainTo = null; chainFrom = null; chainT = 0; chainSwing = 0; thrownT = 0; lsPow = 0;
        clingT = 0; clingCd = 0; clingHit = false; cpuSaveAt = -1;
        rage = false; gutsUsed = false; guard = false; rescueSet = false; wasAir = false; stampN = 0; stampBy = null; hitsTaken = 0; pasCd = 0; st.atk = baseAtk;
        root.SetActive(true);
        root.transform.position = pos;
        trail.Clear();
    }

    public void Knock(string why)
    {
        if (!alive) return;
        alive = false; koWhy = why; shieldT = 0; tornadoT = 0; anchorT = 0; frostT = 0; arrowT = 0; arrowDashT = 0;
        if (why == "spin" || why == "shaft") { down = true; downAxis = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized; if (downAxis == Vector3.zero) downAxis = Vector3.right; }
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

    // 着地: 先端で当たって傾き、首を振りながら（歳差）だんだん収まる。強いほど大きく傾き、少し潰れる
    public void Land(float impactV, Vector3 hvel)
    {
        float s = Mathf.Clamp01((impactV - 6f) / 24f);
        landAmp = Mathf.Lerp(4f, 20f, s);
        landT = 0f; landSq = Mathf.Lerp(0.04f, 0.11f, s);
        // 進んでいる向きへまず傾く（先端が止まって上が前へ倒れる）
        landAng = hvel.sqrMagnitude > 1f ? Mathf.Atan2(hvel.x, hvel.z) : Random.value * 6.28f;
    }

    // 必殺技のオーラ（power 0 で消す）
    public void SetAura(Color c, float seconds) { auraC = c; auraT = Mathf.Max(auraT, seconds); }

    // 見た目の更新（体力は数字でなく、回転の速さ・残像の濃さ・首振りで見せる）
    public void Visual(float dt, float realDt)
    {
        root.transform.position = pos;
        float sr = SpinRatio;
        float w = alive ? Mathf.Lerp(7f, 46f, Mathf.Sqrt(sr)) : (down ? Mathf.Max(0f, 9f * Mathf.Exp(-downT * 1.7f) - 0.4f) : 30f);
        if (specialPhase > 0 || tornadoT > 0) w = 70f;
        spinAngle += w * dt * Mathf.Rad2Deg;
        spinner.localRotation = Quaternion.Euler(0, left ? -spinAngle : spinAngle, 0);
        wobblePhase += dt * Mathf.Lerp(2f, 11f, 1 - sr);   // 回転が落ちるほど首振り（歳差）が大きく・速くなる
        float wob = alive ? Mathf.Lerp(0f, 30f, Mathf.Pow(1 - sr, 1.9f)) : 0;
        if (alive && pbm2 > 0f && pb2 < pbm2 * 0.3f) wob = Mathf.Max(wob, Mathf.Lerp(18f, 6f, pb2 / (pbm2 * 0.3f)) + Mathf.Sin(Time.time * 40f) * 3f);   // 軸が壊れかけ: ふらつく
        if (down)
        {
            // 倒れたあとも床の上で縁をこすりながら回る（慣性）。傾きは大きくなり、回る速さはだんだん落ちて1.8秒ほどで止まる
            downT += dt;
            float k = Mathf.Clamp01(downT / 0.45f);
            float roll = Mathf.Exp(-downT * 1.7f);
            float bounce = Mathf.Abs(Mathf.Sin(downT * 16f)) * (1 - k) * 12f;
            float ang = Mathf.Lerp(0, 66f, k * k) + 18f * (1f - roll) * k + Mathf.Sin(downT * 52f) * 3.2f * roll + bounce;
            downYaw += dt * 15f * roll * Mathf.Rad2Deg * (left ? -1f : 1f);
            tilt.localRotation = Quaternion.AngleAxis(downYaw, Vector3.up) * Quaternion.AngleAxis(ang, downAxis);
        }
        else if (leanT > 0 && clingT <= 0)
        {
            // 傾け: 入りは素早く、最後の0.25秒で戻す。回りながらも傾きの向きは保つ
            float k = Mathf.Clamp01(leanT / 0.25f) * Mathf.Clamp01((1.0f - leanT) / 0.08f + 0.0001f);
            Vector3 ax = Vector3.Cross(Vector3.up, leanDir); if (ax.sqrMagnitude < 1e-4f) ax = Vector3.right;
            tilt.localRotation = Quaternion.AngleAxis(30f * Mathf.Clamp01(k), ax.normalized);
        }
        else if (clingT > 0)
        {
            // 縁で外へ大きく傾き、ガタガタ震える（残り時間が減るほど深く倒れる）
            float k = 1f - clingT / Mathf.Max(0.01f, clingWin);
            Vector3 ax = Vector3.Cross(Vector3.up, clingOut); if (ax.sqrMagnitude < 1e-4f) ax = Vector3.right;
            float ang = Mathf.Lerp(24f, 46f, k) + Mathf.Sin(Time.time * 55f) * Mathf.Lerp(4f, 9f, k);
            tilt.localRotation = Quaternion.AngleAxis(ang, ax.normalized);
        }
        else
        {
            Vector3 lean = new Vector3(vel.z, 0, -vel.x) * 1.1f;
            if (lean.magnitude > 12f) lean = lean.normalized * 12f;
            tilt.localRotation = Quaternion.Euler(Mathf.Sin(wobblePhase) * wob + lean.x, 0, Mathf.Cos(wobblePhase) * wob + lean.z);
        }
        // 着地の揺れ（歳差）と一瞬の潰れ
        if (landT < 3f)
        {
            landT += dt;
            float amp = landAmp * Mathf.Exp(-landT * 3.3f);
            float a = landAng + landT * 15f;   // 最初は進行方向へ倒れ、首が回りながら収まる
            tilt.localRotation = tilt.localRotation * Quaternion.Euler(Mathf.Cos(a) * amp, 0, -Mathf.Sin(a) * amp);
            float sq = landSq * Mathf.Exp(-landT * 16f);
            tilt.localScale = new Vector3(1 + sq * 0.5f, 1 - sq, 1 + sq * 0.5f);
            if (amp < 0.25f) { landT = 99f; tilt.localScale = Vector3.one; }
        }
        float ga = alive ? Mathf.Lerp(0.0f, 0.55f, sr * sr) * (specialPhase > 0 ? 1.4f : 1f) : 0;
        if (hitFlash > 0) { ga = 0.9f; hitFlash -= realDt; }
        if (frostT > 0 && alive) ga = Mathf.Max(ga, 0.75f);
        var gc = frostT > 0 && alive ? new Color(0.6f, 0.88f, 1f) : Color.Lerp(color, Color.white, 0.3f); gc.a = Mathf.Clamp01(ga);
        ghostMat.SetColor("_TintColor", gc * 0.5f);
        ghost.localRotation = Quaternion.Euler(0, (left ? -1f : 1f) * spinAngle * 0.37f, 0);

        // 着地の潰れが終わっている間は等倍に戻す（毎フレーム掛け続けて大きくなるのを防ぐ。2026-11-28 本人「ステージ全体を覆うエフェクト」の原因）
        if (landT >= 3f) tilt.localScale = Vector3.one;
        // 重くなる: 少し沈んで平たく潰れる
        if (anchorT > 0 && alive)
        {
            float sink = Mathf.Clamp01((5f - anchorT) / 0.25f);
            tilt.localPosition = new Vector3(0, -0.13f * sink, 0);
            tilt.localScale = Vector3.Scale(tilt.localScale, new Vector3(1f + 0.12f * sink, 1f - 0.12f * sink, 1f + 0.12f * sink));
        }
        else tilt.localPosition = Vector3.zero;
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
        // 光の羽（空中・天翔）／六角の盾（黒鉄）／紋章（雷鳴を受けている間）
        bool wOn = alive && pas == 1 && !grounded && gh > -100 && pos.y - gh > 0.7f;
        wings.gameObject.SetActive(wOn);
        if (wOn)
        {
            Vector3 hv = new Vector3(vel.x, 0, vel.z); if (hv.sqrMagnitude < 0.5f) hv = new Vector3(0, 0, 1);
            wings.position = pos; wings.rotation = Quaternion.LookRotation(hv.normalized);
            float fl = 1f + 0.18f * Mathf.Sin(Time.unscaledTime * 16f);
            wings.localScale = new Vector3(1f, fl, 1f);
            wingMat.SetColor("_TintColor", new Color(1f, 0.75f, 0.25f) * 0.5f);
        }
        hex.gameObject.SetActive(alive && guard);
        if (alive && guard) { hex.position = pos + Vector3.up * 0.15f; hex.rotation = Quaternion.Euler(0, Time.unscaledTime * 90f, 0); hexMat.SetColor("_TintColor", new Color(0.4f, 0.9f, 1f) * (0.22f + 0.08f * Mathf.Sin(Time.unscaledTime * 9f))); }
        for (int si = 0; si < 3; si++)
        {
            bool on = alive && si < stampN; stampObj[si].gameObject.SetActive(on);
            if (!on) continue;
            float aa = Time.unscaledTime * 3f + si * 2.094f;
            stampObj[si].position = pos + new Vector3(Mathf.Cos(aa) * 0.95f, 1.1f + 0.1f * si, Mathf.Sin(aa) * 0.95f);
            stampObj[si].rotation = Quaternion.Euler(90f, Time.unscaledTime * 200f, 0);
        }
        if (stampMat != null) stampMat.SetColor("_TintColor", new Color(0.7f, 0.35f, 1f) * 0.9f);
        // 凍え・氷の矢・アンカーの見た目
        bool frozen = alive && frostT > 0;
        frostShell.gameObject.SetActive(frozen);
        if (frozen)
        {
            frostShell.position = pos + Vector3.up * 0.02f; frostShell.rotation = Quaternion.Euler(0, 30f, 0);
            float fa = Mathf.Clamp01(frostT / 0.5f);
            frostShell.localScale = Vector3.one * (1.0f + 0.04f * Mathf.Sin(Time.unscaledTime * 20f));
            frostMat.SetColor("_TintColor", new Color(0.62f, 0.92f, 1f, 0.5f) * 0.5f * fa);
        }
        bool arrowOn = alive && arrowDashT > 0;
        iceTail.gameObject.SetActive(arrowOn); iceHead.gameObject.SetActive(arrowOn);
        if (arrowOn)
        {
            Vector3 hd = new Vector3(arrowDir.x, 0, arrowDir.z).normalized;
            Vector3 c = pos + Vector3.up * 0.45f;
            iceTail.position = c; iceTail.rotation = Quaternion.FromToRotation(Vector3.up, -hd + Vector3.up * 0.04f);
            iceHead.position = c - hd * 0.1f; iceHead.rotation = Quaternion.FromToRotation(Vector3.up, hd);
            float fl = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 50f);
            tailMat.SetColor("_TintColor", new Color(0.62f, 0.92f, 1f, 1f) * 0.75f * fl);
            headMat.SetColor("_TintColor", new Color(0.9f, 0.98f, 1f, 1f) * 0.8f);
        }
        bool anOn = alive && anchorT > 0 && gh > -100;
        anchorRing.gameObject.SetActive(anOn); anchorGlowRing.gameObject.SetActive(anOn); anchorCrack.gameObject.SetActive(anOn); anchorCrackGlow.gameObject.SetActive(anOn);
        if (anOn)
        {
            float inT = Mathf.Clamp01((5f - anchorT) / 0.18f), outT = Mathf.Clamp01(anchorT / 0.5f);
            float ap = inT * outT; float pu = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
            Vector2 gg = Arena.Grad(pos.x, pos.z); Quaternion sl = Quaternion.FromToRotation(Vector3.up, new Vector3(-gg.x, 1f, -gg.y).normalized);
            Vector3 fp = new Vector3(pos.x, gh + 0.08f, pos.z);
            anchorRing.SetPositionAndRotation(fp, sl); anchorRing.localScale = Vector3.one * (0.4f + 0.6f * inT);
            anchorGlowRing.SetPositionAndRotation(fp + Vector3.up * 0.02f, sl); anchorGlowRing.localScale = Vector3.one * (0.4f + 0.6f * inT);
            anchorCrack.SetPositionAndRotation(fp + Vector3.up * 0.01f, sl); anchorCrack.localScale = Vector3.one * (0.5f + 0.5f * inT);
            anchorCrackGlow.SetPositionAndRotation(fp + Vector3.up * 0.03f, sl); anchorCrackGlow.localScale = Vector3.one * (0.5f + 0.5f * inT);
            anchorCrackGlowMat.renderQueue = 3100; anchorGlowMat.renderQueue = 3100;
            anchorCrackGlowMat.SetColor("_TintColor", new Color(0.9f, 0.28f, 0.75f, 1f) * 0.9f * ap * pu);
            anchorMat.SetColor("_TintColor", new Color(0.02f, 0.01f, 0.03f, 0.88f) * 0.5f * ap);
            anchorGlowMat.SetColor("_TintColor", new Color(0.9f, 0.28f, 0.75f, 1f) * 0.6f * ap * pu);
            anchorCrackMat.SetColor("_TintColor", new Color(0.01f, 0.005f, 0.015f, 1f) * 0.5f * ap);
        }
        bool auraOn = alive && (auraT > 0 || specialPhase > 0 || tornadoT > 0);
        if (auraT > 0) auraT -= realDt;
        aura.gameObject.SetActive(auraOn); auraRing.gameObject.SetActive(auraOn);
        if (auraOn)
        {
            float pu = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 24f);
            float grow = Mathf.Clamp01(auraT > 0 ? 1f : 0.6f) * pu;
            aura.position = new Vector3(pos.x, pos.y - 0.1f, pos.z);
            aura.rotation = Quaternion.Euler(0, Time.unscaledTime * 420f, 0);
            aura.localScale = new Vector3(1f + 0.25f * pu, 1f, 1f + 0.25f * pu);
            auraMat.SetColor("_TintColor", auraC * (0.55f * grow));
            auraRing.position = new Vector3(pos.x, (gh > -100 ? gh : pos.y) + 0.12f, pos.z);
            auraRing.localScale = Vector3.one * (1.1f + 0.35f * Mathf.Sin(Time.unscaledTime * 18f));
            auraRingMat.SetColor("_TintColor", auraC * (0.7f * pu));
        }
        trail.emitting = alive && (dashT > 0 || specialPhase > 0 || boostCd > 0.5f || superT > 0 || boardT > 0.3f || fireT > 0 || (!grounded && vel.magnitude > 6f));
    }
}
