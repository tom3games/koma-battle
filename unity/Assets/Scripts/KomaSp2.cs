using System.Collections.Generic;
using UnityEngine;

// 2026-11-28 必殺4つの追加と中央の崩落
//   4 グラビティ・アンカー（5秒だけ超重量・地面に刺さる） 5 ラスト・スパイラル（回転が3割を切った時だけ・少ないほど強い）
//   6 グレイシャル・アロー（輪が並んでから氷の矢で突進・当たると凍える） 7 チェイン・バインド（鎖でつないで振り回し、最後に放り投げる）
public partial class KomaBoot
{
    // 数字の調整はここ（勝率を見て動かす）
    const float AnchorSec = 5f, AnchorMass = 14f;
    const float SpiralBase = 21f, SpiralMin = 1.0f, SpiralMax = 2.4f;
    const float ArrowSpeed = 60f, ArrowDashSec = 0.27f, ArrowTeleSec = 0.95f, FrostSec = 2f, FrostSlow = 0.5f, ArrowDmg = 3.0f;
    const float ArrowHitVn = 26f;
    const float ChainSec = 4f, ChainLen = 6.8f;

    int cpuSpMode = -1;
    static readonly Color IceC = new Color(0.7f, 0.93f, 1f);
    static readonly Color IceW = new Color(0.9f, 0.98f, 1f);

    // 撃てるか（ゲージと、ラスト・スパイラルは回転が3割を切っていること）
    public bool SpecialReady(Koma k) => k.alive && k.gauge >= 100f && (k.sp != 5 || k.SpinRatio < 0.3f);

    // ---------- 粒（氷・火花・ほこり）。速度と色を指定して出す ----------
    int ActiveWaves() { int n = 0; for (int i = 0; i < waveT.Count; i++) if (waveT[i] >= 0) n++; return n; }
    int ActiveRings() { int n = 0; foreach (var r in arrowRings) if (r.t >= 0) n++; return n; }
    void Fleck(Vector3 at, Vector3 vel, Color c, float size, float life, bool glow)
    {
        cFleck++;
        var ep = new ParticleSystem.EmitParams { position = at, velocity = vel, startSize = size, startLifetime = life, startColor = c };
        (glow ? sparks : dust).Emit(ep, 1);
    }
    void IceBurst(Vector3 at, int n, float power)
    {
        for (int i = 0; i < n; i++)
        {
            Vector3 d = (Random.onUnitSphere + Vector3.up * 0.5f).normalized;
            Fleck(at, d * Random.Range(2f, 8f) * power, Random.value < 0.5f ? IceW : IceC, Random.Range(0.1f, 0.26f), Random.Range(0.35f, 0.8f), true);
        }
    }

    // ---------- 撃つ瞬間（TrySpecial から。ゲージは消費済み） ----------
    void SpecialNew(Koma k, Vector3 aim)
    {
        Vector3 h = aim - k.pos; h.y = 0;
        if (k.sp == 4)
        {
            // グラビティ・アンカー: 5秒だけ超重量。当てられても動かず、当てた相手の方が弾かれる
            k.anchorT = AnchorSec; k.vel = new Vector3(k.vel.x * 0.1f, Mathf.Min(k.vel.y, 0f), k.vel.z * 0.1f);
            EmitEv("special", k, 4); SpecialFx(k, 0.9f);
            Wave(k.pos, 6f); Wave(k.pos, 10f); Dust(k.pos, 50); Scorch(k.pos, 3.2f);
            Pulse(k.pos, SpColors[4], 9f, 16f);
            for (int i = 0; i < 18; i++) { float a = i * Mathf.PI * 2 / 18; Fleck(k.pos + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * 1.8f, new Vector3(Mathf.Cos(a), 0.9f, Mathf.Sin(a)) * 2.2f, new Color(0.9f, 0.3f, 0.75f), 0.18f, 0.5f, true); }
            if (k.isPlayer) { shake = Mathf.Max(shake, 0.9f); fovKick = Mathf.Max(fovKick, -4f); }
            return;
        }
        if (k.sp == 5)
        {
            // ラスト・スパイラル: 回転が少ないほど強い（残り3割 ×1.0 → 0割 ×2.4）。溜めて、渦を巻いて突っ込む
            float ratio = Mathf.Max(k.SpinRatio, 0.02f);
            k.lsPow = Mathf.Lerp(SpiralMin, SpiralMax, Mathf.Clamp01(1f - ratio / 0.3f));
            k.specialPhase = 40; k.specialT = 0.5f; k.vel = new Vector3(0, Mathf.Min(k.vel.y, 0f), 0);
            Koma best = null; float bd = 1e9f;
            foreach (var o in komas) { if (o == k || !o.alive || !o.root.activeSelf) continue; float d = (o.pos - aim).sqrMagnitude; if (d < bd) { bd = d; best = o; } }
            k.specialTarget = best != null ? new Vector3(best.idx, -999, 0) : aim;
            EmitEv("special", k, 5); SpecialFx(k, 1.4f);
            return;
        }
        if (k.sp == 6)
        {
            // グレイシャル・アロー: 狙いの向きに輪が順に開き、開き終わると氷の矢になって一直線に突進する
            Vector3 dir = h.sqrMagnitude > 0.04f ? h.normalized : Vector3.forward;
            if (k.isPlayer && !autoPlayer) { var ad = AssistDir(k, dir); dir = new Vector3(ad.x, 0, ad.z).normalized; }
            k.arrowDir = dir; k.arrowFrom = k.pos; k.arrowT = 0; k.arrowRings = 0; k.specialPhase = 30;
            k.vel = new Vector3(0, Mathf.Min(k.vel.y, 0f), 0);
            EmitEv("special", k, 6); SpecialFx(k, 1.0f);
            return;
        }
        if (k.sp == 7)
        {
            // チェイン・バインド: 一番近い相手と鎖でつながる
            var t = Nearest(k);
            if (t == null) { k.gauge = 100f; k.specials--; return; }
            StartChain(k, t);
            return;
        }
    }

    // ---------- 毎フレーム（UpdateSpecial から） ----------
    void UpdateSpecial2(Koma k, float dt)
    {
        if (k.anchorT > 0)
        {
            k.anchorT -= dt;
            if (Random.value < 0.3f) Fleck(k.pos + Random.insideUnitSphere * 1.6f + Vector3.up * 0.2f, new Vector3(0, 1.2f, 0), new Color(0.5f, 0.15f, 0.45f), 0.14f, 0.5f, true);
            if (k.anchorT <= 0) { Dust(k.pos, 16); EmitEv("anchorend", k, 0); }
        }
        if (k.frostT > 0)
        {
            k.frostT -= dt;
            if (Random.value < 0.7f) Fleck(k.Center + Random.insideUnitSphere * 0.8f, Vector3.up * Random.Range(0.8f, 2f), Random.value < 0.5f ? IceW : IceC, Random.Range(0.1f, 0.2f), 0.6f, true);
            if (k.frostT <= 0) { IceBurst(k.Center, 26, 1f); EmitEv("melt", k, 0); }
        }
        if (k.specialPhase == 30)
        {
            // 輪が順に開く（読み合い: この間に横へ逃げれば当たらない）。撃った本人は向きを変えられない
            k.arrowT += dt; k.vel = new Vector3(k.vel.x * 0.5f, k.vel.y, k.vel.z * 0.5f);
            int want = Mathf.Min(5, Mathf.FloorToInt((k.arrowT - 0.05f) / 0.17f) + 1);
            while (k.arrowRings < want) { SpawnArrowRing(k, k.arrowRings); k.arrowRings++; }
            if (k.isPlayer) shake = Mathf.Max(shake, 0.06f);
            if (Random.value < 0.8f) Fleck(k.Center + Random.insideUnitSphere * 0.7f, Vector3.up * 1.2f, IceW, 0.14f, 0.5f, true);
            if (k.arrowT >= ArrowTeleSec)
            {
                k.specialPhase = 31; k.arrowDashT = ArrowDashSec; k.dashT = ArrowDashSec + 0.05f; k.dashes++; k.grounded = k.grounded;
                k.vel = new Vector3(k.arrowDir.x * ArrowSpeed, k.vel.y, k.arrowDir.z * ArrowSpeed);
                EmitEv("arrowgo", k, 0);
                Wave(k.pos, 4f); Pulse(k.pos, SpColors[6], 10f, 16f); IceBurst(k.Center, 40, 1.3f);
                if (k.isPlayer) { fovKick = Mathf.Max(fovKick, 16f); shake = Mathf.Max(shake, 0.5f); }
            }
        }
        else if (k.specialPhase == 31)
        {
            k.arrowDashT -= dt;
            Vector3 back = -k.arrowDir;
            for (int i = 0; i < 3; i++) Fleck(k.Center + back * Random.Range(0f, 2.5f) + Random.insideUnitSphere * 0.4f, back * Random.Range(1f, 5f) + Random.insideUnitSphere * 1.5f, Random.value < 0.5f ? IceW : IceC, Random.Range(0.12f, 0.3f), Random.Range(0.3f, 0.6f), true);
            if (k.arrowDashT <= 0)
            {
                k.specialPhase = 0; k.arrowDashT = 0; k.dashT = 0; k.vel = new Vector3(k.vel.x * 0.25f, k.vel.y, k.vel.z * 0.25f);
                IceBurst(k.Center, 24, 1f);
            }
        }
        else if (k.specialPhase == 40)
        {
            // 溜め: 渦が内へ巻く
            k.specialT -= dt; k.vel = new Vector3(k.vel.x * 0.5f, k.vel.y, k.vel.z * 0.5f);
            float tt = Time.unscaledTime * 14f;
            for (int i = 0; i < 4; i++)
            {
                float a = tt + i * 1.57f, rr = 0.8f + 2.4f * Mathf.Clamp01(k.specialT / 0.5f);
                Vector3 p = k.pos + new Vector3(Mathf.Cos(a), 0.3f + 0.6f * Random.value, Mathf.Sin(a)) * rr;
                Vector3 tang = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                Fleck(p, tang * 5f - new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3f, Random.value < 0.5f ? new Color(1f, 0.2f, 0.25f) : new Color(1f, 0.85f, 0.85f), 0.2f, 0.4f, true);
            }
            if (k.isPlayer) shake = Mathf.Max(shake, 0.12f);
            if (k.specialT <= 0)
            {
                k.specialPhase = 41; k.specialT = 0.7f; k.dashT = 0.8f;
                EmitEv("spiralgo", k, k.lsPow);
                Wave(k.pos, 4f); Pulse(k.pos, SpColors[5], 10f, 16f);
                if (k.isPlayer) { fovKick = Mathf.Max(fovKick, 14f); shake = Mathf.Max(shake, 0.4f); }
            }
        }
        else if (k.specialPhase == 41)
        {
            k.specialT -= dt;
            float tt = Time.unscaledTime * 30f;
            for (int i = 0; i < 4; i++)
            {
                float a = tt + i * 1.57f;
                Fleck(k.Center + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 0.7f) * 0.5f, Mathf.Sin(a)) * 1.1f, -new Vector3(k.vel.x, 0, k.vel.z) * 0.15f, i % 2 == 0 ? new Color(1f, 0.15f, 0.2f) : new Color(1f, 0.9f, 0.9f), 0.22f, 0.4f, true);
            }
            if (k.specialT <= 0) SpiralImpact(k, k.pos, 0.6f);
        }
        if (k.thrownT > 0) k.thrownT -= dt;
    }

    // ---------- ラスト・スパイラルの当たり ----------
    void SpiralImpact(Koma k, Vector3 at, float scale)
    {
        k.specialPhase = 0; k.dashT = 0;
        float pw = k.lsPow * scale;
        Wave(at, 5f); Wave(at, 8f + 2f * pw); Wave(at, 12f);
        Spark(at + Vector3.up * 0.3f, Vector3.up, Mathf.RoundToInt(80 + 40 * pw), 1.7f);
        Dust(at, 40); Scorch(at, 5f);
        Pulse(at, SpColors[5], 8f + 4f * pw, 22f);
        k.SetAura(SpColors[5], 0.35f);
        for (int i = 0; i < 40; i++)
        {
            float a = i * 0.55f, rr = 0.3f + i * 0.09f;
            Fleck(at + new Vector3(Mathf.Cos(a) * rr, 0.25f, Mathf.Sin(a) * rr), new Vector3(Mathf.Cos(a + 1.2f), 0.5f, Mathf.Sin(a + 1.2f)) * 5f, i % 2 == 0 ? new Color(1f, 0.15f, 0.2f) : new Color(1f, 0.9f, 0.9f), 0.2f, 0.7f, true);
        }
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - at; d.y = 0;
            float dist = d.magnitude;
            if (dist > 5.5f) continue;
            Vector3 n = dist > 0.01f ? d / dist : Vector3.forward;
            float f = 1f - dist / 5.5f * 0.5f;
            float res = o.anchorT > 0 ? 0.1f : 1f;
            o.vel += n * 15f * pw * f / Mathf.Sqrt(o.st.mass) * res;
            o.vel.y = Mathf.Max(o.vel.y, 6.5f * f * res);
            o.grounded = false;
            Damage(k, o, SpiralBase * pw * f * k.st.atk / o.st.def);
            o.hitFlash = 0.2f;
            Spark(o.Center, n + Vector3.up * 0.5f, 40, 1.4f);
        }
        float near = k.isPlayer ? 1f : Mathf.Clamp(1.1f - Vector3.Distance(player.pos, at) / 18f, 0.25f, 1f);
        shake = Mathf.Max(shake, 1.2f * near * Mathf.Min(1.4f, pw)); fovKick = Mathf.Max(fovKick, 14f * near);
        var sp = cam.WorldToViewportPoint(at);
        Emit("{\"t\":\"e\",\"k\":\"boom\",\"s\":5,\"i\":" + k.idx + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
    }

    // ---------- 氷の矢が当たった ----------
    void ArrowHit(Koma a, Koma b, Vector3 nh)
    {
        float res = b.anchorT > 0 ? 0.15f : 1f;
        b.vel += nh * 14f / Mathf.Sqrt(b.st.mass) * res; b.vel.y = Mathf.Max(b.vel.y, 4.5f); b.grounded = false;
        if (b.shieldT <= 0) { b.frostT = FrostSec; EmitEv("freeze", b, 0); }
        a.specialPhase = 0; a.arrowDashT = 0; a.dashT = 0.1f; a.vel = new Vector3(a.vel.x * 0.3f, a.vel.y, a.vel.z * 0.3f);
        IceBurst(b.Center, 50, 1.5f); Wave(b.pos, 5f); Pulse(b.pos, SpColors[6], 10f, 18f);
        if (a.isPlayer || b.isPlayer) { shake = Mathf.Max(shake, 0.7f); fovKick = Mathf.Max(fovKick, 10f); }
    }

    // ---------- 氷の輪（床の印と立った輪） ----------
    class ArrowRing { public Transform hoop, mark; public Material hoopMat, markMat, diskMat; public float t = -1; }
    readonly List<ArrowRing> arrowRings = new List<ArrowRing>();
    const float RingLife = 1.6f;
    void SpawnArrowRing(Koma k, int i)
    {
        ArrowRing r = null;
        foreach (var x in arrowRings) if (x.t < 0) { r = x; break; }
        if (r == null)
        {
            r = new ArrowRing();
            var hoop = MeshKit.Obj("ArrowHoop", null, MeshKit.Ring(1.12f, 1.38f, 40), mats.AddGlow(new Color(0.45f, 0.82f, 1f, 1f)));
            var disk = MeshKit.Obj("ArrowDisk", hoop.transform, MeshKit.Disc(1.0f, 32), mats.AddGlow(new Color(0.5f, 0.85f, 1f, 1f)));
            var mark = MeshKit.Obj("ArrowMark", null, MeshKit.Ring(0.7f, 1.2f, 32), mats.AddGlow(new Color(0.7f, 0.93f, 1f, 1f)));
            foreach (var g in new[] { hoop, disk, mark }) g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.hoop = hoop.transform; r.mark = mark.transform;
            r.hoopMat = hoop.GetComponent<MeshRenderer>().material; r.diskMat = disk.GetComponent<MeshRenderer>().material; r.markMat = mark.GetComponent<MeshRenderer>().material;
            arrowRings.Add(r);
        }
        Vector3 p = k.arrowFrom + k.arrowDir * (2.8f + i * 2.8f);
        float gh = Arena.H(p.x, p.z);
        float gy = gh > -100 ? gh : k.arrowFrom.y;
        r.hoop.position = new Vector3(p.x, gy + 0.95f, p.z);
        r.hoop.rotation = Quaternion.FromToRotation(Vector3.up, k.arrowDir);
        r.mark.position = new Vector3(p.x, gy + 0.07f, p.z);
        r.t = 0; r.hoop.gameObject.SetActive(true); r.mark.gameObject.SetActive(true);
        IceBurst(r.hoop.position, 14, 0.8f);
        EmitEv("arrowring", k, i);
    }
    void UpdateArrowRings(float realDt)
    {
        foreach (var r in arrowRings)
        {
            if (r.t < 0) continue;
            r.t += realDt;
            float grow = Mathf.Clamp01(r.t / 0.2f), fade = Mathf.Clamp01((RingLife - r.t) / 0.35f);
            float s = 0.3f + 0.7f * (1f - (1f - grow) * (1f - grow));
            float pu = 0.8f + 0.2f * Mathf.Sin(r.t * 22f);
            r.hoop.localScale = Vector3.one * s;
            r.mark.localScale = Vector3.one * s;
            r.hoopMat.SetColor("_TintColor", new Color(0.45f, 0.82f, 1f, 1f) * 0.8f * grow * fade * pu);
            r.diskMat.SetColor("_TintColor", new Color(0.35f, 0.75f, 1f, 1f) * 0.2f * grow * fade);
            r.markMat.SetColor("_TintColor", new Color(0.4f, 0.8f, 1f, 1f) * 0.55f * grow * fade);
            if (r.t >= RingLife) { r.t = -1; r.hoop.gameObject.SetActive(false); r.mark.gameObject.SetActive(false); }
        }
    }

    // ---------- チェイン・バインド ----------
    class Chain { public Koma a, b; public float t, len = ChainLen, swing, prevAng; public LineRenderer lr, glow; public float[] hitCd = new float[4]; public GameObject root; }
    readonly List<Chain> chains = new List<Chain>();
    Material chainMat, chainGlowMat;
    static Texture2D linkTex;
    static Texture2D LinkTex()
    {
        if (linkTex != null) return linkTex;
        var t = new Texture2D(32, 16, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 32; x++)
            {
                float a = 0;
                float d1 = Mathf.Pow((x - 8f) / 7f, 2) + Mathf.Pow((y - 7.5f) / 6f, 2);
                float d2 = Mathf.Pow((x - 24f) / 3.5f, 2) + Mathf.Pow((y - 7.5f) / 6f, 2);
                if (Mathf.Abs(d1 - 0.78f) < 0.3f) a = 1;
                if (Mathf.Abs(d2 - 0.78f) < 0.35f) a = 1;
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply(); linkTex = t; return t;
    }

    void StartChain(Koma k, Koma t)
    {
        if (chainMat == null) { chainMat = mats.AddGlow(new Color(0.75f, 1f, 0.5f, 1f), LinkTex()); chainGlowMat = mats.AddGlow(new Color(0.6f, 1f, 0.3f, 0.5f), mats.Dot); }
        var c = new Chain { a = k, b = t, t = ChainSec };
        c.root = new GameObject("Chain");
        c.lr = MakeLine(c.root.transform, chainMat, 0.34f, true);
        c.glow = MakeLine(c.root.transform, chainGlowMat, 0.9f, false);
        c.prevAng = Mathf.Atan2(k.pos.z - t.pos.z, k.pos.x - t.pos.x);
        chains.Add(c);
        k.chainTo = t; t.chainFrom = k; k.chainT = ChainSec;
        // 初動: 相手を少しこちらへ引く
        Vector3 d = k.pos - t.pos; d.y = 0; if (d.sqrMagnitude > 0.01f) t.vel += d.normalized * 5f / Mathf.Sqrt(t.st.mass) * (t.anchorT > 0 ? 0.1f : 1f);
        EmitEv("special", k, 7); SpecialFx(k, 1.0f);
        EmitEv("chain", t, 0);
        Spark(t.Center, Vector3.up, 40, 1.2f); Wave(t.pos, 4f);
        Pulse(t.pos, SpColors[7], 7f, 14f);
    }
    LineRenderer MakeLine(Transform parent, Material m, float w, bool tile)
    {
        var g = new GameObject("line"); g.transform.SetParent(parent, false);
        var lr = g.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = 14; lr.widthMultiplier = w; lr.numCapVertices = 2;
        lr.sharedMaterial = m; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
        lr.alignment = LineAlignment.View;
        if (tile) { lr.textureMode = LineTextureMode.Tile; lr.textureScale = new Vector2(1f / (w * 2.2f), 1f); }
        return lr;
    }
    void EndChain(Chain c)
    {
        if (c.a != null) { c.a.chainTo = null; c.a.chainT = 0; }
        if (c.b != null) c.b.chainFrom = null;
        if (c.root) Object.Destroy(c.root);
        chains.Remove(c);
    }
    void ClearChains()
    {
        for (int i = chains.Count - 1; i >= 0; i--) EndChain(chains[i]);
        foreach (var k in komas) { k.chainTo = null; k.chainFrom = null; k.chainT = 0; k.thrownT = 0; }
    }

    // 物理の1刻みごと: 鎖の長さを超えたら引き合う（軽い方がよく動く）
    float EffMass(Koma k) => k.st.mass * (k.anchorT > 0 ? AnchorMass : 1f);
    void ChainConstrain(float h)
    {
        for (int i = 0; i < chains.Count; i++)
        {
            var c = chains[i]; var a = c.a; var b = c.b;
            if (!a.alive || !b.alive || a.clingT > 0 || b.clingT > 0) continue;
            Vector3 d = b.pos - a.pos; d.y = 0;
            float dist = d.magnitude;
            if (dist <= c.len || dist < 0.01f) continue;
            Vector3 n = d / dist;
            float ia = 1f / EffMass(a), ib = 1f / EffMass(b);
            float ex = Mathf.Min(dist - c.len, 16f * h);
            Vector3 pa = a.pos + n * ex * ia / (ia + ib), pb = b.pos - n * ex * ib / (ia + ib);
            if (Arena.WallH(pa.x, pa.z) - a.pos.y <= StepUp) a.pos = new Vector3(pa.x, a.pos.y, pa.z);
            if (Arena.WallH(pb.x, pb.z) - b.pos.y <= StepUp) b.pos = new Vector3(pb.x, b.pos.y, pb.z);
            float rel = Vector3.Dot(b.vel - a.vel, n);
            if (rel > 0) { float J = rel * 1.05f / (ia + ib); a.vel += n * J * ia; b.vel -= n * J * ib; }
        }
    }

    void Tug(Koma tgt, Vector3 dir)
    {
        foreach (var c in chains)
        {
            if (c.b != tgt) continue;
            c.a.vel += dir * 7f / Mathf.Sqrt(c.a.st.mass) * (c.a.anchorT > 0 ? 0.1f : 1f);
            c.t -= 0.5f; c.swing *= 0.6f;
            EmitEv("tug", tgt, 0);
            Spark((c.a.Center + tgt.Center) * 0.5f, Vector3.up, 24, 1.1f);
        }
    }

    Vector3 ChainThrowDir(Chain c)
    {
        Vector3 dir;
        if (c.a.isPlayer && !autoPlayer) { dir = aimPoint - c.a.pos; }
        else
        {
            // CPU・自動操作: 第三者がいればそちらへ、いなければ外へ
            Koma third = null; float bd = 1e9f;
            foreach (var o in komas) { if (o == c.a || o == c.b || !o.alive || !o.root.activeSelf) continue; float dd = (o.pos - c.b.pos).sqrMagnitude; if (dd < bd) { bd = dd; third = o; } }
            dir = third != null ? third.pos - c.b.pos : new Vector3(c.b.pos.x, 0, c.b.pos.z);
        }
        dir.y = 0;
        if (dir.sqrMagnitude < 0.04f) dir = c.b.pos - c.a.pos; dir.y = 0;
        if (dir.sqrMagnitude < 0.04f) dir = Vector3.forward;
        return dir.normalized;
    }

    void ThrowChain(Chain c)
    {
        var a = c.a; var b = c.b;
        Vector3 dir = ChainThrowDir(c);
        float spd = 15f + 17f * Mathf.Clamp01(c.swing / 7f);
        float res = b.anchorT > 0 ? 0.15f : 1f;
        b.vel = dir * spd * res + Vector3.up * 7f; b.grounded = false; b.thrownT = 1.0f;
        Damage(a, b, 10f * (spd / 25f) * a.st.atk / b.st.def); a.spin = Mathf.Min(a.spinMax, a.spin + a.spinMax * 0.05f);
        b.hitFlash = 0.2f;
        Spark(b.Center, dir + Vector3.up, 70, 1.6f); Wave(b.pos, 6f); Pulse(b.pos, SpColors[7], 10f, 18f);
        float near = (a.isPlayer || b.isPlayer) ? 1f : 0.4f;
        shake = Mathf.Max(shake, 0.8f * near); fovKick = Mathf.Max(fovKick, 10f * near);
        EmitEv("chainthrow", b, spd);
        EndChain(c);
    }

    void KickThird(Koma bat, Koma o, float[] cd)
    {
        Vector3 d = o.pos - bat.pos; d.y = 0;
        float dist = d.magnitude;
        if (dist > 1.9f || cd[o.idx] > 0) return;
        float sp = new Vector2(bat.vel.x, bat.vel.z).magnitude;
        if (sp < 6f) return;
        Vector3 n = dist > 0.01f ? d / dist : Vector3.forward;
        float res = o.anchorT > 0 ? 0.1f : 1f;
        o.vel += (n * (10f + sp * 0.5f)) / Mathf.Sqrt(o.st.mass) * res; o.vel.y = Mathf.Max(o.vel.y, 4f); o.grounded = false;
        Damage(bat.chainFrom != null ? bat.chainFrom : bat, o, 3.5f * bat.st.atk / o.st.def);
        cd[o.idx] = 0.45f; o.hitFlash = 0.1f;
        Spark((bat.Center + o.Center) * 0.5f, n + Vector3.up * 0.4f, 30, 1.2f);
        EmitEv("chainhit", o, 0);
    }

    float[] thrownCd = new float[4];
    void UpdateChains(float dt)
    {
        for (int i = chains.Count - 1; i >= 0; i--)
        {
            var c = chains[i];
            if (!c.a.alive || !c.b.alive || phase != "fight") { EndChain(c); continue; }
            c.t -= dt; c.a.chainT = c.t;
            for (int q = 0; q < 4; q++) if (c.hitCd[q] > 0) c.hitCd[q] -= dt;
            // 自分が相手のまわりを回った角度（回すほど遠くへ飛ぶ）
            float ang = Mathf.Atan2(c.a.pos.z - c.b.pos.z, c.a.pos.x - c.b.pos.x);
            c.swing += Mathf.Abs(Mathf.DeltaAngle(c.prevAng * Mathf.Rad2Deg, ang * Mathf.Rad2Deg)) * Mathf.Deg2Rad; c.prevAng = ang;
            foreach (var o in komas) if (o != c.a && o != c.b && o.alive && o.root.activeSelf) KickThird(c.b, o, c.hitCd);
            DrawChain(c);
            if (c.t <= 0) ThrowChain(c);
        }
        // 放り投げられた相手に当たった第三者も弾かれる
        for (int q = 0; q < 4; q++) if (thrownCd[q] > 0) thrownCd[q] -= dt;
        foreach (var k in komas)
            if (k.thrownT > 0 && k.alive)
                foreach (var o in komas) if (o != k && o.alive && o.root.activeSelf) KickThird(k, o, thrownCd);
    }

    void DrawChain(Chain c)
    {
        Vector3 p0 = c.a.Center, p1 = c.b.Center;
        float dist = new Vector2(p1.x - p0.x, p1.z - p0.z).magnitude;
        float slack = Mathf.Clamp01(1f - dist / c.len);
        bool taut = dist > c.len * 0.96f;
        int n = c.lr.positionCount;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            Vector3 p = Vector3.Lerp(p0, p1, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * slack * 1.6f;
            if (taut) p += Random.insideUnitSphere * 0.05f * Mathf.Sin(t * Mathf.PI);
            c.lr.SetPosition(i, p); c.glow.SetPosition(i, p);
        }
        float pu = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 24f);
        chainMat.SetColor("_TintColor", new Color(0.6f, 1f, 0.3f, 1f) * (taut ? 0.95f : 0.75f));
        chainGlowMat.SetColor("_TintColor", new Color(0.6f, 1f, 0.3f, 0.5f) * (taut ? 0.5f : 0.3f) * pu);
        int sparkN = taut ? 3 : 1;
        for (int s = 0; s < sparkN; s++)
            if (Random.value < 0.7f)
            {
                float t = Random.value;
                Vector3 p = Vector3.Lerp(p0, p1, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * slack * 1.6f;
                Fleck(p, Random.insideUnitSphere * 3f + Vector3.up * 1.5f, Random.value < 0.5f ? new Color(1f, 0.95f, 0.6f) : new Color(0.7f, 1f, 0.4f), Random.Range(0.08f, 0.16f), Random.Range(0.2f, 0.4f), true);
            }
    }

    // ---------- 中央の崩落 ----------
    float suddenAt = -1f; const float PitDelay = 4f, PitAtT = 70f;   // 試合が70秒を超えたらひびが入り始める（外周の崩落が先に始まっていれば、その4秒後）
    bool pitForced;
    void UpdatePitFlow(float dt)
    {
        if (phase == "fight" && !Arena.PitActive && ((sudden && fightT >= suddenAt + PitDelay) || pitForced || fightT >= PitAtT)) { Arena.StartPit(); Emit("{\"t\":\"e\",\"k\":\"pitstart\"}"); }
        if (phase != "fight" && phase != "end") return;
        int r = Arena.UpdatePit(dt);
        if (Arena.PitCracking && Random.value < 0.35f * (0.4f + Arena.PitProgress))
        {
            float a = Random.value * Mathf.PI * 2, rr = Random.Range(0.3f, Arena.DipR + 1f);
            Vector3 p = new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr); p.y = Arena.FloorH(p.x, p.z) + 0.1f;
            Fleck(p, Vector3.up * Random.Range(0.5f, 1.5f), new Color(0.6f, 0.58f, 0.55f, 0.6f), Random.Range(0.3f, 0.6f), 0.7f, false);
            if (Arena.PitProgress > 0.6f) Fleck(p, Vector3.up * 2f, new Color(1f, 0.55f, 0.2f), 0.1f, 0.4f, true);
        }
        if (r == 1)
        {
            shake = Mathf.Max(shake, 0.25f + 0.3f * Arena.PitProgress);
            Emit("{\"t\":\"e\",\"k\":\"crackstep\",\"p\":" + F(Arena.PitProgress) + "}");
        }
        else if (r == 2)
        {
            Vector3 at = new Vector3(0, 0.2f, 0);
            Dust(at, 70); Spark(at, Vector3.up, 120, 1.8f);
            Wave(at, 7f); Wave(at, 11f);
            Pulse(at + Vector3.up * 0.5f, new Color(1f, 0.4f, 0.12f), 10f, 20f);
            shake = Mathf.Max(shake, 1.1f); fovKick = Mathf.Max(fovKick, 10f);
            Emit("{\"t\":\"e\",\"k\":\"pitfall\"}");
        }
    }
    bool CrossesPit(Vector3 a, Vector3 b)
    {
        Vector2 p = new Vector2(a.x, a.z), q = new Vector2(b.x, b.z), d = q - p;
        float l2 = d.sqrMagnitude; float t = l2 > 1e-4f ? Mathf.Clamp01(Vector2.Dot(-p, d) / l2) : 0f;
        return (p + d * t).magnitude < Arena.DipR + 1.2f;
    }

    // CPU が必殺を撃つ条件（距離など）
    bool CpuWantsSpecial(Koma k, float dist)
    {
        switch (k.sp)
        {
            case 4: return dist < 6f || new Vector2(k.pos.x, k.pos.z).magnitude > Arena.R - 5f;
            case 5: return dist < 14f;
            case 6: return dist > 4.5f && dist < 14f;
            case 7: return dist < 16f;
            case 0: return dist < MeteorRange;
            default: return dist < 9f;
        }
    }
}
