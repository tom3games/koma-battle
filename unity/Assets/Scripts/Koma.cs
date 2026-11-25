using UnityEngine;

public struct Stats
{
    public float atk, def, mass, stamina, speed, grip, jump, air, radius, dashCd, decay, aerial;
}

// パーツ（上・芯・足）。名前と説明は画面側（HTML）が持つ。ここは数値と形だけ
public static class Parts
{
    public static Stats Compute(int top, int core, int leg)
    {
        var s = new Stats { atk = 1, def = 1, mass = 1, stamina = 1, speed = 1, grip = 1, jump = 1, air = 1, radius = 0.8f, dashCd = 1, decay = 1, aerial = 1 };
        switch (top)
        {
            case 0: s.atk = 1.35f; s.def = 0.85f; s.mass = 1.0f; s.radius = 0.82f; break;       // キバ: 攻め
            case 1: s.atk = 0.85f; s.def = 1.4f; s.mass = 1.3f; s.radius = 0.86f; break;        // タテ: 守り・重い
            case 2: s.atk = 1.05f; s.def = 0.95f; s.mass = 0.82f; s.air = 1.7f; s.radius = 0.8f; s.aerial = 1.15f; break; // ハネ: 軽い・空中で曲がる
        }
        switch (core)
        {
            case 0: s.stamina = 1.35f; s.speed *= 0.92f; break;                    // ネバリ
            case 1: s.stamina = 0.85f; s.speed *= 1.22f; s.dashCd = 0.75f; break;  // ハヤテ
            case 2: s.stamina = 1.05f; s.speed *= 0.95f; s.mass += 0.35f; s.def *= 1.1f; break; // オモシ
        }
        switch (leg)
        {
            case 0: s.grip = 1.7f; s.jump = 0.85f; s.speed *= 0.95f; break;          // ベタ: 踏ん張る
            case 1: s.grip = 0.6f; s.speed *= 1.15f; s.decay = 0.7f; break;          // ツンツン: よく滑る・長持ち
            case 2: s.jump = 1.45f; s.aerial *= 1.3f; break;                         // バネ: 高く跳ぶ
        }
        return s;
    }
}

// コマ1つ。物理は自前（高さ関数の上をすべる・跳ぶ）
public class Koma
{
    public int idx;
    public bool isPlayer, cpu;
    public int top, core, leg;
    public Stats st;
    public Vector3 pos, vel;
    public bool grounded, alive = true, down;
    public float spin, spinMax, gauge;
    public float dashCd, dashT, airDashUsed, padCd, hitFlash;
    public float specialT; public int specialPhase; public Vector3 specialTarget;
    public string koWhy = "";
    public float koTime;
    public bool lastGroundRamp;
    public Color color;
    // 成績
    public float dealt; public int aerialHits, specials, dashes;
    public float lossWall, lossHit, lossDash, lastHitT = -99;
    public int diveTarget = -1;

    // 見た目
    public GameObject root; Transform tilt, spinner, ghost, shadow, marker;
    TrailRenderer trail;
    float spinAngle, wobblePhase, downT;
    Vector3 downAxis;

    public float SpinRatio => Mathf.Clamp01(spin / spinMax);
    public float Radius => st.radius;
    public Vector3 Center => pos + Vector3.up * 0.42f;

    public Koma(int idx, int top, int core, int leg, Color color, Materials m)
    {
        this.idx = idx; this.top = top; this.core = core; this.leg = leg; this.color = color;
        st = Parts.Compute(top, core, leg);
        spinMax = 160f * st.stamina; spin = spinMax;
        BuildVisual(m);
    }

    public void Rebuild(int t, int c, int l, Materials m)
    {
        top = t; core = c; leg = l;
        st = Parts.Compute(top, core, leg);
        spinMax = 160f * st.stamina; spin = spinMax;
        var p = root.transform.position;
        Object.Destroy(root);
        BuildVisual(m);
        root.transform.position = p;
    }

    void BuildVisual(Materials m)
    {
        root = new GameObject("Koma" + idx);
        tilt = new GameObject("tilt").transform; tilt.SetParent(root.transform, false);
        spinner = new GameObject("spin").transform; spinner.SetParent(tilt, false);

        Color dark = Color.Lerp(color, Color.black, 0.55f);
        Color light = Color.Lerp(color, Color.white, 0.35f);
        var body = m.Lit(color, 0.75f, 0.35f);
        var metal = m.Lit(new Color(0.78f, 0.8f, 0.86f), 0.85f, 0.9f);
        var darkM = m.Lit(dark, 0.6f, 0.4f);
        var gold = m.Lit(new Color(1f, 0.78f, 0.25f), 0.85f, 0.8f);

        // 足
        switch (leg)
        {
            case 0:
                Put(MeshKit.Frustum(0.34f, 0.26f, 0.2f, 24, true), darkM, 0f);
                Put(MeshKit.Frustum(0.26f, 0.3f, 0.12f, 24, true), metal, 0.2f);
                break;
            case 1:
                Put(MeshKit.Frustum(0.02f, 0.28f, 0.34f, 24, true), metal, 0f);
                break;
            case 2:
                Put(MeshKit.Frustum(0.1f, 0.18f, 0.08f, 20, true), metal, 0f);
                for (int i = 0; i < 3; i++) Put(MeshKit.Frustum(0.26f, 0.26f, 0.05f, 24, true), i % 2 == 0 ? gold : darkM, 0.09f + i * 0.08f);
                break;
        }
        // 上（攻めの輪）
        float ry = 0.34f;
        switch (top)
        {
            case 0:
                Put(MeshKit.Frustum(0.58f, 0.62f, 0.14f, 32, true), body, ry);
                for (int i = 0; i < 4; i++)
                {
                    var f = Put(MeshKit.Box(new Vector3(0.36f, 0.12f, 0.16f)), metal, ry + 0.07f);
                    float a = i * 90f;
                    f.transform.localRotation = Quaternion.Euler(0, a, 0);
                    f.transform.localPosition = f.transform.localRotation * new Vector3(0.62f, 0, 0) + Vector3.up * (ry + 0.07f);
                    f.transform.localRotation *= Quaternion.Euler(0, -28f, 0);
                    f.transform.localScale = new Vector3(1.2f, 1, 1);
                }
                break;
            case 1:
                Put(MeshKit.Frustum(0.84f, 0.7f, 0.2f, 40, true), body, ry);
                Put(MeshKit.Frustum(0.86f, 0.86f, 0.06f, 40, true), metal, ry + 0.02f);
                for (int i = 0; i < 6; i++)
                {
                    var f = Put(MeshKit.Box(new Vector3(0.12f, 0.06f, 0.22f)), darkM, ry + 0.2f);
                    f.transform.localRotation = Quaternion.Euler(0, i * 60f, 0);
                    f.transform.localPosition = f.transform.localRotation * new Vector3(0.58f, 0, 0) + Vector3.up * (ry + 0.21f);
                }
                break;
            case 2:
                Put(MeshKit.Frustum(0.5f, 0.52f, 0.1f, 32, true), body, ry);
                for (int i = 0; i < 3; i++)
                {
                    var f = Put(MeshKit.Box(new Vector3(0.5f, 0.04f, 0.26f)), light == Color.white ? metal : m.Lit(light, 0.7f, 0.2f), ry + 0.08f);
                    float a = i * 120f;
                    f.transform.localRotation = Quaternion.Euler(0, a, 0);
                    f.transform.localPosition = f.transform.localRotation * new Vector3(0.58f, 0, 0) + Vector3.up * (ry + 0.08f);
                    f.transform.localRotation *= Quaternion.Euler(18f, -20f, 0);
                }
                break;
        }
        // 芯
        Color coreC = core == 0 ? new Color(0.35f, 0.85f, 0.35f) : core == 1 ? new Color(0.3f, 0.6f, 1f) : new Color(0.35f, 0.33f, 0.38f);
        Put(MeshKit.Frustum(0.3f, 0.26f, 0.3f, 24, true), m.Lit(coreC, 0.7f, 0.3f), ry + 0.1f);
        Put(MeshKit.Frustum(0.2f, 0.12f, 0.12f, 24, true), gold, ry + 0.4f);
        if (core == 2) Put(MeshKit.Frustum(0.07f, 0.07f, 0.12f, 6, true), metal, ry + 0.52f);
        // 目印（片側だけの色の点。回っているのが見える）
        var dot = Put(MeshKit.Box(new Vector3(0.14f, 0.04f, 0.14f)), m.Lit(Color.white, 0.4f, 0), ry + 0.4f);
        dot.transform.localPosition = new Vector3(0.16f, ry + 0.41f, 0);

        // 回転の残像（加算の筋の輪）
        var gm = m.Alpha(Color.Lerp(color, Color.white, 0.3f), m.Streak);
        ghost = MeshKit.Obj("ghost", tilt, MeshKit.Disc(Radius * 1.25f, 32), gm).transform;
        ghost.localPosition = Vector3.up * (ry + 0.12f);
        ghost.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 足元の丸い影（場外でも見える）と自分の印
        var sm = m.Alpha(new Color(0, 0, 0, 0.55f), m.Dot);
        shadow = MeshKit.Obj("shadow", root.transform, MeshKit.Disc(Radius * 1.1f, 24), sm).transform;
        shadow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mk = m.AddGlow(color, null);
        marker = MeshKit.Obj("marker", root.transform, MeshKit.Ring(Radius * 1.15f, Radius * 1.3f, 32), mk).transform;
        marker.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        trail = root.AddComponent<TrailRenderer>();
        trail.material = m.Alpha(Color.white, Texture2D.whiteTexture);
        trail.time = 0.22f;
        trail.widthMultiplier = Radius * 1.4f;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.5f), 0), new GradientColorKey(color, 1) },
            new[] { new GradientAlphaKey(0.75f, 0), new GradientAlphaKey(0, 1) });
        trail.colorGradient = grad;
        trail.minVertexDistance = 0.08f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.emitting = false;
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
        spin = spinMax; gauge = 0; dashCd = 0; dashT = 0; airDashUsed = 0; specialPhase = 0; koWhy = "";
        dealt = 0; aerialHits = 0; specials = 0; dashes = 0; lossWall = 0; lossHit = 0; lossDash = 0;
        tilt.localRotation = Quaternion.identity;
        root.SetActive(true);
        root.transform.position = pos;
        trail.Clear();
    }

    public void Knock(string why)
    {
        if (!alive) return;
        alive = false; koWhy = why;
        if (why == "spin") { down = true; downAxis = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized; if (downAxis == Vector3.zero) downAxis = Vector3.right; }
    }

    // 見た目の更新
    public void Visual(float dt, float realDt)
    {
        root.transform.position = pos;
        float sr = SpinRatio;
        float w = alive ? Mathf.Lerp(8f, 46f, Mathf.Sqrt(sr)) : (down ? Mathf.Max(0, 8f - downT * 10f) : 30f);
        if (specialPhase == 1) w = 70f;
        spinAngle += w * dt * Mathf.Rad2Deg;
        spinner.localRotation = Quaternion.Euler(0, -spinAngle, 0);
        // 回転が減るとふらつく（首振り）
        wobblePhase += dt * Mathf.Lerp(2f, 9f, 1 - sr);
        float wob = alive ? Mathf.Lerp(0f, 16f, Mathf.Pow(1 - sr, 2)) : 0;
        if (down)
        {
            downT += dt;
            float k = Mathf.Clamp01(downT / 0.7f);
            float bounce = Mathf.Abs(Mathf.Sin(downT * 14f)) * (1 - k) * 12f;
            tilt.localRotation = Quaternion.AngleAxis(Mathf.Lerp(0, 74f, k * k) + bounce, downAxis);
        }
        else
        {
            Vector3 lean = new Vector3(vel.z, 0, -vel.x) * 1.3f;
            if (lean.magnitude > 14f) lean = lean.normalized * 14f;
            tilt.localRotation = Quaternion.Euler(Mathf.Sin(wobblePhase) * wob + lean.x, 0, Mathf.Cos(wobblePhase) * wob + lean.z);
        }
        var gmr = ghost.GetComponent<MeshRenderer>();
        float ga = alive ? Mathf.Lerp(0.05f, 0.75f, sr) * (specialPhase > 0 ? 1.4f : 1f) : 0;
        if (hitFlash > 0) { ga = 1.2f; hitFlash -= realDt; }
        var gc = Color.Lerp(color, Color.white, 0.3f); gc.a = Mathf.Clamp01(ga * 0.85f); gmr.material.SetColor("_TintColor", gc * 0.5f);
        ghost.localRotation = Quaternion.Euler(0, -spinAngle * 0.37f, 0);

        float gh = Arena.H(pos.x, pos.z);
        if (gh > -100)
        {
            shadow.gameObject.SetActive(true);
            shadow.position = new Vector3(pos.x, gh + 0.03f, pos.z);
            float hgt = Mathf.Max(0, pos.y - gh);
            shadow.localScale = Vector3.one * Mathf.Clamp(1f - hgt * 0.07f, 0.5f, 1f);
            var sc = shadow.GetComponent<MeshRenderer>().material;
            sc.SetColor("_TintColor", new Color(0, 0, 0, Mathf.Clamp(0.6f - hgt * 0.05f, 0.25f, 0.6f)) * 0.5f);
        }
        else shadow.gameObject.SetActive(false);
        marker.gameObject.SetActive(alive);
        marker.position = new Vector3(pos.x, (gh > -100 ? gh : pos.y) + 0.05f, pos.z);
        trail.emitting = alive && (dashT > 0 || specialPhase > 0 || (!grounded && vel.magnitude > 6f));
    }
}
