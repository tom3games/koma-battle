using System.Collections.Generic;
using UnityEngine;

// 2026-11-29 バーサーカーの必殺「狂戦化（ベルセルク・ドライブ）」と、その極大技「終焉狂神（ラグナ・ベルセルク）」。本人の依頼。
//   既存作品の名前や仕組みを真似ない: 炎は使わず、黒い瘴気と爪痕で統一する（赤黒いオーラ・赤い目が2つ・黒い稲妻）。
//   撃てる条件: 回転が5割を切っている時だけ（ラスト・スパイラルと同じく、撃てる時だけゲージが光る）
//   通常: 8秒 速さ1.6倍・攻撃2倍・受ける弾き半分・ダッシュの待ちなし／暴走（操作が効きにくい・一番近い相手へ引っぱられ・蛇行）／終わると回転が5%まで落ち3秒ふらつく（不屈は働かない）
//   極大: 10秒 速さ1.8倍・攻撃2.5倍・弾かれない／通った跡に黒紫の爪痕（触れた相手は回転が削られ一瞬ひるむ・約3秒で消える）／
//        暴走中に倒すたび +2秒・終わった後の回転が +5%（最大20%）／終わりに黒い瘴気の衝撃波→回転5%（＋倒した分）・ふらつき2秒
public partial class KomaBoot
{
    // 数字の調整はここ
    const float BzSec = 8f, BzSpeedMul = 1.6f, BzAtkMul = 2f, BzSteerWeak = 0.55f, BzPull = 0.6f, BzWobble = 0.35f, BzAfterSpin = 0.05f, BzStaggerSec = 3f;
    const float BzEvoSec = 10f, BzEvoSpeedMul = 1.8f, BzEvoAtkMul = 2.5f, BzKillBonusSec = 2f, BzKillBonusSpin = 0.05f, BzMaxAfter = 0.2f, BzEvoStagger = 2f;
    const float ClawLife = 3f, ClawSpin = 3f, ClawFlinch = 0.35f, ClawGap = 1.2f, ClawRadius = 1.3f, BzBurstR = 12f, BzBurstPush = 19f;

    public float BzSpeed(Koma k) => k.bzT > 0 ? (k.evo ? BzEvoSpeedMul : BzSpeedMul) : (k.bzStagger > 0 ? 0.5f : (trailT[k.idx] > 0 ? 1.35f : 1f));   // ソニック・トレイルの間は走る速さも上がる
    public float BzAtk(Koma k, float soft) => k.bzT > 0 ? (k.evo ? BzEvoAtkMul : BzAtkMul) : 1f;

    // 暴走: 操作が効きにくく、一番近い相手へ自動で引っぱられ、小さく蛇行する
    Vector2 BzSteer(Koma k, Vector2 mv, float dt)
    {
        if (k.bzStagger > 0f && k.bzT <= 0f) return mv * 0.5f;
        if (k.bzT <= 0f) return mv;
        var t = Nearest(k);
        Vector2 pull = Vector2.zero;
        if (t != null) { Vector3 d = t.pos - k.pos; d.y = 0; pull = new Vector2(d.x, d.z).normalized; }
        Vector2 perp = new Vector2(-pull.y, pull.x);
        float wob = Mathf.Sin(Time.time * 5.5f + k.idx) * BzWobble;
        Vector2 r = mv * BzSteerWeak + pull * BzPull + perp * wob;
        return r.sqrMagnitude > 1f ? r.normalized : r;
    }

    class Claw { public Transform tr; public Material m; public float t; public Vector3 pos; public Koma owner; public float[] cd = new float[4]; }
    readonly List<Claw> claws = new List<Claw>();
    readonly Vector3[] clawLast = new Vector3[4];
    Material clawMat;
    readonly Transform[] bzBolt = new Transform[2]; LineRenderer[] bzBoltLr = new LineRenderer[2]; Material boltMat;

    void ClearBerserk()
    {
        foreach (var c in claws) if (c.tr) Object.Destroy(c.tr.gameObject); claws.Clear();
        foreach (var k in komas) { k.bzT = 0; k.bzStagger = 0; k.bzKills = 0; k.lastHitter = null; }
    }

    void BerserkStart(Koma k)
    {
        k.bzT = BzSec; k.bzMax = BzSec; k.bzKills = 0; k.dashCd = 0;
        EmitEv("special", k, 10); SpecialFx(k, 1.2f);
        Wave(k.pos, 6f); Pulse(k.pos, SpColors[10], 10f, 20f);
        SlowFx(0.3f, 0.35f);   // 発動の瞬間だけ一瞬止める
        clawLast[k.idx] = new Vector3(0, -999, 0);
    }
    void BerserkEvoStart(Koma k)
    {
        k.bzT = BzEvoSec; k.bzMax = BzEvoSec; k.bzKills = 0; k.dashCd = 0;
        k.SetAura(new Color(0.55f, 0.1f, 0.35f), 2.5f);
    }

    // 倒した時（KO から）: 極大の暴走中なら時間を延ばし、終わりの回転を増やす
    void BzOnKill(Koma dead)
    {
        var h = dead.lastHitter;
        if (h != null && h != dead && h.alive && h.bzT > 0 && h.evo) { h.bzT += BzKillBonusSec; h.bzKills++; EmitEv("bzkill", h, h.bzKills); Wave(dead.pos, 6f); }
    }

    void UpdateBerserk(float dt)
    {
        if (clawMat == null) clawMat = mats.AddGlow(new Color(0.55f, 0.15f, 0.9f, 1f));
        foreach (var k in komas)
        {
            if (k.bzStagger > 0f) { k.bzStagger -= dt; if (k.alive && Random.value < 0.5f) Fleck(k.pos + Vector3.up * 0.1f + Random.insideUnitSphere * 0.4f, Vector3.up * 1.5f, new Color(1f, 0.7f, 0.3f), 0.1f, 0.4f, true); }
            if (k.bzT <= 0f) continue;
            if (!k.alive || phase != "fight") { k.bzT = 0; continue; }
            k.bzT -= dt; k.dashCd = 0f;
            bool evo = k.evo;
            // 見た目: 赤黒いオーラ（黒い瘴気）・赤い目が2つ・黒い稲妻
            k.SetAura(evo ? new Color(0.3f, 0.05f, 0.25f) : new Color(0.55f, 0.04f, 0.1f), 0.2f);
            // 黒い瘴気: 極大は濃く大きく量を増やす（コマの周りを黒い煙が立ちのぼる）
            int smoke = evo ? 4 : (Random.value < 0.6f ? 1 : 0);
            for (int q = 0; q < smoke; q++)
                Fleck(k.pos + Vector3.up * Random.Range(0.1f, 1.2f) + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * (evo ? 1.1f : 0.8f), Vector3.up * Random.Range(1.2f, 3.2f) + Random.insideUnitSphere * 0.7f,
                    evo ? new Color(0.02f, 0.0f, 0.04f, 1f) : new Color(0.12f, 0.0f, 0.05f, 1f), evo ? Random.Range(0.9f, 1.5f) : 0.55f, evo ? 2.0f : 1.3f, false);
            { Vector3 rt = cam.transform.right; Vector3 eye = k.Center + Vector3.up * 0.55f;
              Fleck(eye + rt * 0.2f, Vector3.zero, new Color(1f, 0.1f, 0.08f), 0.2f, 0.07f, true); Fleck(eye - rt * 0.2f, Vector3.zero, new Color(1f, 0.1f, 0.08f), 0.2f, 0.07f, true); }
            if (Random.value < 0.08f) Scorch(k.pos, 1.1f);   // 床の小さな爪痕
            if (evo) { BzBolt(k); EvoClaw(k); }
            if (k.bzT <= 0f) BerserkEnd(k, evo);
        }
        UpdateClaws(dt);
    }

    void BzBolt(Koma k)
    {
        if (boltMat == null) boltMat = mats.AddGlow(new Color(0.6f, 0.2f, 1f, 1f));
        if (Random.value > 0.25f) return;
        int bi = Random.value < 0.5f ? 0 : 1;
        if (bzBoltLr[bi] == null) { var g = new GameObject("BzBolt"); var lr = g.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.widthMultiplier = 0.16f; lr.sharedMaterial = boltMat; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.alignment = LineAlignment.View; bzBoltLr[bi] = lr; }
        var l = bzBoltLr[bi]; l.positionCount = 7;
        Vector3 top = k.pos + new Vector3(Random.Range(-1.5f, 1.5f), 5f, Random.Range(-1.5f, 1.5f)), bot = k.pos + new Vector3(Random.Range(-1.2f, 1.2f), 0.3f, Random.Range(-1.2f, 1.2f));
        for (int i = 0; i < 7; i++) l.SetPosition(i, Vector3.Lerp(top, bot, i / 6f) + (i > 0 && i < 6 ? Random.insideUnitSphere * 0.5f : Vector3.zero));
        l.enabled = true; boltOff[bi] = 0.07f;
    }
    readonly float[] boltOff = new float[2];

    // 極大: 通った跡に黒紫の爪痕
    void EvoClaw(Koma k)
    {
        if (!k.grounded) return;
        if ((k.pos - clawLast[k.idx]).magnitude < ClawGap && clawLast[k.idx].y > -900) return;
        clawLast[k.idx] = k.pos;
        var g = new GameObject("Claw").transform;
        var mt = new Material(clawMat);
        // 3本の斜めの裂け目
        var vb = new MeshKit.Builder { twoSided = true };
        for (int i = -1; i <= 1; i++)
        {
            Vector3 a = new Vector3(i * 0.38f - 0.2f, 0, -0.75f), b = new Vector3(i * 0.38f + 0.22f, 0, 0.75f), w = new Vector3(0.07f, 0, 0.0f);
            vb.Quad(a - w, a + w, b + w, b - w, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        }
        var go = MeshKit.Obj("c", g, vb.Mesh(), mt); go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        float gh = Arena.H(k.pos.x, k.pos.z);
        Vector3 vel = new Vector3(k.vel.x, 0, k.vel.z); float ang = vel.sqrMagnitude > 0.5f ? Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg : Random.value * 360f;
        g.position = new Vector3(k.pos.x, (gh > -100f ? gh : k.pos.y) + 0.09f, k.pos.z); g.rotation = Quaternion.Euler(0, ang, 0);
        claws.Add(new Claw { tr = g, m = mt, t = ClawLife, pos = k.pos, owner = k });
        if (claws.Count > 80) { var o = claws[0]; if (o.tr) Object.Destroy(o.tr.gameObject); claws.RemoveAt(0); }
    }
    void UpdateClaws(float dt)
    {
        for (int i = 0; i < 2; i++) if (boltOff[i] > 0) { boltOff[i] -= dt; if (boltOff[i] <= 0 && bzBoltLr[i]) bzBoltLr[i].enabled = false; }
        for (int i = claws.Count - 1; i >= 0; i--)
        {
            var c = claws[i]; c.t -= dt;
            if (c.t <= 0 || phase == "end") { if (c.tr) Object.Destroy(c.tr.gameObject); claws.RemoveAt(i); continue; }
            c.m.SetColor("_TintColor", new Color(0.5f, 0.12f, 0.85f) * (0.8f * Mathf.Clamp01(c.t / 1f)));
            for (int q = 0; q < 4; q++) if (c.cd[q] > 0) c.cd[q] -= dt;
            foreach (var o in komas)
            {
                if (o == c.owner || !o.alive || !o.root.activeSelf || c.cd[o.idx] > 0) continue;
                Vector3 d = o.pos - c.pos; float dy = Mathf.Abs(d.y); d.y = 0;
                if (d.magnitude > ClawRadius || dy > 1.4f) continue;
                c.cd[o.idx] = 0.8f;
                o.spin -= ClawSpin; o.lossHit += ClawSpin; o.lockT = Mathf.Max(o.lockT, ClawFlinch); o.hitFlash = 0.12f; o.lastHitter = c.owner;
                o.vel.x *= 0.5f; o.vel.z *= 0.5f;
                Spark(o.Center, Vector3.up, 24, 1f);
                if (o.spin <= 0) { o.spin = 0; KO(o, "spin"); }
            }
        }
    }

    // 終わり: 回転が一気に落ちてふらつく（不屈は働かない: KO を通さずに回転だけ書き換える）。極大は黒い瘴気の衝撃波が先
    void BerserkEnd(Koma k, bool evo)
    {
        float after = BzAfterSpin;
        if (evo)
        {
            after = Mathf.Min(BzMaxAfter, BzAfterSpin + BzKillBonusSpin * k.bzKills);
            Wave(k.pos, 8f); Wave(k.pos, 14f); Pulse(k.pos, new Color(0.35f, 0.05f, 0.5f), 12f, 24f); shake = Mathf.Max(shake, 0.8f);
            for (int i = 0; i < 40; i++) Fleck(k.Center + Random.insideUnitSphere * 0.5f, Random.onUnitSphere * 7f, new Color(0.14f, 0.0f, 0.2f), 0.8f, 1.4f, false);
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                Vector3 d = o.pos - k.pos; d.y = 0; float dist = d.magnitude; if (dist > BzBurstR) continue;
                Vector3 n = dist > 0.01f ? d / dist : Vector3.forward; float f = 1f - dist / BzBurstR * 0.6f;
                o.vel += n * BzBurstPush * f / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 4f * f); o.grounded = false; o.hitFlash = 0.15f;
            }
        }
        k.spin = Mathf.Min(k.spin, k.spinMax * after); if (k.spin < k.spinMax * 0.02f) k.spin = k.spinMax * 0.02f;
        k.bzStagger = evo ? BzEvoStagger : BzStaggerSec; k.bzT = 0; k.evoT = 0;
        EmitEv("bzend", k, 0);
    }
}
