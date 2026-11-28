using System.Collections.Generic;
using UnityEngine;

// 2026-11-29 極大技（ゲージ5本たまった時だけ撃てる進化版）。本人の依頼。
//   ゲージは最大500（100 が1本）。通常の必殺は技ごとの必要量（70〜100）だけ使う。5本たまっている時に必殺ボタン長押しで進化版（全部使う）。
//   進化版は元の効果を全部残して、範囲・時間・威力を上げ、技ごとの追加効果をつける。名前（ルビつき）は ui.js の EVO_NAMES。
public partial class KomaBoot
{
    public const float GaugeCap = 500f;
    // 数字の調整はここ
    const float EvoMeteorR = 10f, EvoMeteorDmg = 1.6f, EvoMeteorKnock = 1.8f;
    const float EvoTornadoSec = 5f, EvoStrikeEvery = 0.6f, EvoStrikeRange = 10f;
    const int EvoRush = 6; const float EvoGhostLife = 1f;
    const float EvoShieldSec = 4.8f, EvoReflect = 2f, EvoShieldBurstR = 11f, EvoShieldBurstPush = 17f;
    const float EvoAnchorSec = 6.5f, EvoZoneR = 8f, EvoZoneSlow = 1.3f;
    const float EvoSpiralMul = 1.5f;
    const float EvoBergLife = 6f, EvoBergR = 1.9f, EvoBergSpin = 4f, EvoSlipSec = 2.5f;
    const float EvoChainMul = 1.4f, EvoChainSpinSpeed = 22f;
    const float EvoTrailSec = 7f, EvoTrailWidth = 2.4f, EvoTrailRadius = 1.7f, EvoTrailDmg = 1.5f;
    const float EvoMirageSec = 9f;

    // 撃てるか（ゲージ満タン。スパイラルは回転が2割を切っている時だけ）
    public bool EvoReady(Koma k) => k.alive && k.gauge >= GaugeCap - 0.01f && (k.sp != 5 || k.SpinRatio < 0.2f);

    public void TryEvolved(Koma k, Vector3 aim)
    {
        if (!k.alive || k.specialPhase > 0 || k.tornadoT > 0 || k.shieldT > 0 || k.anchorT > 0 || k.chainTo != null || phase != "fight") return;
        if (k.gauge < GaugeCap - 0.01f) return;
        if (k.sp == 5 && k.SpinRatio >= 0.2f) { if (k.isPlayer && !autoPlayer) EmitEv("deny", k, 1); return; }
        int before = k.specials; k.evoT = 6f; k.gauge = NeedOf(k);   // 通常の撃ち出しを通して、消費は全部（NeedOf 引いて 0）
        TrySpecial(k, aim);
        if (k.specials == before) { k.evoT = 0; k.gauge = GaugeCap; return; }   // 撃てなかった（相手がいない等）
        EmitEv("evo", k, k.sp);
        SlowFx(0.28f, 0.5f);
        EvoStart(k);
    }

    void EvoStart(Koma k)
    {
        Color c = SpColors[Mathf.Clamp(k.sp, 0, 9)];
        k.SetAura(Color.Lerp(c, Color.white, 0.35f), 1.6f);
        Pulse(k.pos, c, 12f, 26f); Wave(k.pos, 6f); Wave(k.pos, 10f); Wave(k.pos, 15f);
        Spark(k.Center, Vector3.up, 160, 2.4f);
        shake = Mathf.Max(shake, 1.2f); fovKick = Mathf.Max(fovKick, 14f);
        k.evoCd = 0f;
        switch (k.sp)
        {
            case 0: k.evoT = 4f; break;
            case 1: k.tornadoT = EvoTornadoSec; k.evoT = EvoTornadoSec + 1f; break;
            case 2: k.rushLeft = EvoRush; k.evoT = 6f; break;
            case 3: k.shieldT = EvoShieldSec; k.evoT = EvoShieldSec + 0.5f; k.evoShield = true; break;
            case 4: k.anchorT = EvoAnchorSec; k.evoT = EvoAnchorSec + 0.5f; break;
            case 5: k.lsPow *= EvoSpiralMul; k.evoT = 4f; break;
            case 6: k.evoT = 5f; EvoBergs(k); break;
            case 7: k.evoT = ChainSec * EvoChainMul + 1f; EvoChain(k); break;
            case 8: k.evoT = EvoTrailSec + 0.5f; trailT[k.idx] = EvoTrailSec; break;
            case 9: k.evoT = EvoMirageSec + 0.5f; break;   // 分身は Sp3Start が evo を見て4体出す
        }
    }

    // ---------- 毎フレーム ----------
    class Ghost { public Transform tr; public Material m; public float t; public bool active; }
    readonly List<Ghost> ghosts = new List<Ghost>();
    class Spike { public Transform tr; public float t; public float x, z, h; }
    readonly List<Spike> spikes = new List<Spike>();
    class Berg { public Transform tr; public Vector3 pos; public float t, appear; public float[] cd = new float[4]; public Koma owner; }
    readonly List<Berg> bergs = new List<Berg>();
    Transform zoneRing; Material zoneMat; Transform burstRing; Material[] evoShieldMats; Transform[] evoShieldRings;

    void ClearEvo()
    {
        foreach (var g in ghosts) if (g.tr) Object.Destroy(g.tr.gameObject); ghosts.Clear();
        foreach (var sp in spikes) if (sp.tr) Object.Destroy(sp.tr.gameObject); spikes.Clear();
        foreach (var b in bergs) if (b.tr) Object.Destroy(b.tr.gameObject); bergs.Clear();
        if (zoneRing) zoneRing.gameObject.SetActive(false);
        foreach (var k in komas) { k.evoT = 0; k.evoShield = false; k.slipT = 0; }
        if (evoShieldRings != null) foreach (var r in evoShieldRings) if (r) r.gameObject.SetActive(false);
    }

    void UpdateEvo(float dt)
    {
        foreach (var k in komas)
        {
            if (k.slipT > 0) k.slipT -= dt;
            if (k.evoT <= 0) continue;
            if (!k.alive || phase != "fight") { k.evoT = 0; k.evoShield = false; continue; }
            k.evoT -= dt;
            switch (k.sp)
            {
                case 1:
                    if (k.tornadoT > 0)
                    {
                        // 竜巻のあいだ、自分の周りへランダムに落雷（落ちる前に床へ予告の円）
                        k.evoCd -= dt;
                        if (k.evoCd <= 0)
                        {
                            k.evoCd = EvoStrikeEvery;
                            Vector2 r = Random.insideUnitCircle * EvoStrikeRange; if (r.magnitude < 2.5f) r = r.normalized * 2.5f;
                            Vector3 at = k.pos + new Vector3(r.x, 0, r.y);
                            if (Arena.H(at.x, at.z) > -100f && !Arena.PitOpen) SpawnStrikeAt(k, at);
                        }
                    }
                    break;
                case 2:
                    if (k.specialPhase == 20 && k.dashT > 0)
                    {
                        k.evoCd -= dt;
                        if (k.evoCd <= 0) { k.evoCd = 0.06f; SpawnGhost(k); }   // 突進の残像を1秒残す（当たり判定なし）
                    }
                    break;
                case 3:
                    if (k.evoShield)
                    {
                        UpdateEvoShield(k, dt);
                        if (k.shieldT <= 0)
                        {
                            k.evoShield = false;
                            // 盾が砕けて周りへ衝撃波
                            Wave(k.pos, 8f); Wave(k.pos, 13f); Pulse(k.pos, SpColors[3], 12f, 24f); Spark(k.Center, Vector3.up, 140, 2.2f);
                            shake = Mathf.Max(shake, 0.8f);
                            foreach (var o in komas)
                            {
                                if (o == k || !o.alive || !o.root.activeSelf) continue;
                                Vector3 d = o.pos - k.pos; d.y = 0; float dist = d.magnitude; if (dist > EvoShieldBurstR) continue;
                                Vector3 n = dist > 0.01f ? d / dist : Vector3.forward; float f = 1f - dist / EvoShieldBurstR * 0.6f;
                                o.vel += n * EvoShieldBurstPush * f / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 4f * f); o.grounded = false; o.hitFlash = 0.15f;
                            }
                        }
                    }
                    break;
                case 4:
                    if (k.anchorT > 0)
                    {
                        UpdateZone(k, dt);
                    }
                    else if (zoneRing) zoneRing.gameObject.SetActive(false);
                    break;
                case 7:
                    foreach (var c in chains)
                    {
                        if (c.a != k || c.recover) continue;
                        // 自分の回転と同じ向きに、つないだ相手のまわりをぐるぐる回る（回すほど遠くへ投げられる）
                        Vector3 rel = k.pos - c.b.pos; rel.y = 0; if (rel.sqrMagnitude < 0.04f) continue;
                        Vector3 tan = new Vector3(rel.z, 0, -rel.x).normalized * (k.left ? -1f : 1f);
                        k.vel += tan * EvoChainSpinSpeed * dt;
                    }
                    break;
            }
            // 極大技のあいだは名札の色の光をまとう
            if (k.alive && Random.value < 0.3f) Fleck(k.Center + Random.insideUnitSphere * 0.9f, Vector3.up * 1.5f, Color.Lerp(SpColors[Mathf.Clamp(k.sp, 0, 9)], Color.white, 0.4f), 0.14f, 0.5f, true);
        }
        UpdateGhosts(dt); UpdateSpikes(dt); UpdateBergs(dt);
    }

    // ---------- 残像（ファントム・レイド） ----------
    void SpawnGhost(Koma k)
    {
        Ghost g = null; foreach (var x in ghosts) if (!x.active) { g = x; break; }
        if (g == null)
        {
            g = new Ghost(); var t = new GameObject("Ghost").transform; g.m = mats.AddGlow(SpColors[2]);
            var a = MeshKit.Obj("d", t, MeshKit.Frustum(0.85f, 0.9f, 0.2f, 24, true), g.m); a.transform.localPosition = new Vector3(0, 0.4f, 0);
            var b = MeshKit.Obj("c", t, MeshKit.Frustum(0.32f, 0.28f, 0.45f, 16, true), g.m); b.transform.localPosition = new Vector3(0, 0.55f, 0);
            foreach (var r in t.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.tr = t; ghosts.Add(g);
        }
        g.t = EvoGhostLife; g.active = true; g.tr.position = k.pos; g.tr.rotation = Quaternion.Euler(0, Time.time * 600f, 0); g.tr.gameObject.SetActive(true);
    }
    void UpdateGhosts(float dt)
    {
        foreach (var g in ghosts)
        {
            if (!g.active) continue;
            g.t -= dt; if (g.t <= 0) { g.active = false; g.tr.gameObject.SetActive(false); continue; }
            g.m.SetColor("_TintColor", SpColors[2] * (0.34f * g.t / EvoGhostLife));
        }
    }

    // ---------- 盾の三重の六角形（イージス・リバース） ----------
    void UpdateEvoShield(Koma k, float dt)
    {
        if (evoShieldRings == null)
        {
            evoShieldRings = new Transform[3]; evoShieldMats = new Material[3];
            for (int i = 0; i < 3; i++)
            {
                evoShieldMats[i] = mats.AddGlow(new Color(1f, 0.82f, 0.35f, 1f));
                var g = MeshKit.Obj("EvoShield" + i, null, MeshKit.Frustum(1.5f + i * 0.45f, 1.5f + i * 0.45f, 1.5f, 6, false, false), evoShieldMats[i]);
                g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                evoShieldRings[i] = g.transform;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            var t = evoShieldRings[i]; t.gameObject.SetActive(true); t.position = k.pos + Vector3.up * 0.0f;
            t.rotation = Quaternion.Euler(0, Time.time * (90f + i * 70f) * (i % 2 == 0 ? 1f : -1f), 0);
            evoShieldMats[i].SetColor("_TintColor", new Color(1f, 0.82f, 0.35f) * (0.22f + 0.1f * Mathf.Sin(Time.time * 12f + i)) * Mathf.Clamp01(k.shieldT / 0.4f));
        }
        if (k.shieldT <= 0.05f) foreach (var r in evoShieldRings) r.gameObject.SetActive(false);
    }

    // ---------- 領域（グラビティ・アンカー） ----------
    void UpdateZone(Koma k, float dt)
    {
        if (zoneRing == null)
        {
            zoneMat = mats.AddGlow(new Color(0.8f, 0.2f, 0.7f, 1f));
            var g = MeshKit.Obj("EvoZone", null, MeshKit.Ring(0.93f, 1f, 64), zoneMat); g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; zoneRing = g.transform;
        }
        zoneRing.gameObject.SetActive(true);
        float gh = Arena.H(k.pos.x, k.pos.z); zoneRing.position = new Vector3(k.pos.x, (gh > -100f ? gh : k.pos.y) + 0.12f, k.pos.z);
        zoneRing.localScale = new Vector3(EvoZoneR, 1, EvoZoneR) * (1f + 0.03f * Mathf.Sin(Time.time * 6f));
        zoneMat.SetColor("_TintColor", new Color(0.85f, 0.2f, 0.75f) * (0.3f + 0.12f * Mathf.Sin(Time.time * 8f)));
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - k.pos; d.y = 0; if (d.magnitude > EvoZoneR) continue;
            float f = Mathf.Exp(-EvoZoneSlow * dt); o.vel.x *= f; o.vel.z *= f;   // 領域の中の相手は動きが鈍る
            if (Random.value < 0.15f) Fleck(o.pos + Vector3.up * 0.1f, Vector3.up * 0.8f, new Color(0.7f, 0.2f, 0.65f), 0.12f, 0.5f, true);
        }
    }

    // ---------- 突き上がる岩（メテオ・ジャッジメント） ----------
    void EvoSpikes(Vector3 at)
    {
        var mt = mats.Lit(new Color(0.22f, 0.2f, 0.22f), 0.2f, 0.1f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 0.9f + Random.value, r = 1.5f + (i / 12f) * (EvoMeteorR - 2f) + Random.value;
            float x = at.x + Mathf.Cos(a) * r, z = at.z + Mathf.Sin(a) * r; float gh = Arena.H(x, z); if (gh < -100f) continue;
            var g = MeshKit.Obj("Spike", null, MeshKit.Frustum(0.7f + Random.value * 0.4f, 0.02f, 2.4f + Random.value * 1.6f, 7, false), mt);
            g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.value * 360f, Random.Range(-10f, 10f));
            spikes.Add(new Spike { tr = g.transform, t = 0, x = x, z = z, h = gh });
            g.transform.position = new Vector3(x, gh - 3.6f, z);
            Dust(new Vector3(x, gh, z), 4);
        }
    }
    void UpdateSpikes(float dt)
    {
        for (int i = spikes.Count - 1; i >= 0; i--)
        {
            var s = spikes[i]; s.t += dt;
            float up = s.t < 0.2f ? s.t / 0.2f : s.t < 1.6f ? 1f : Mathf.Clamp01(1f - (s.t - 1.6f) / 0.5f);
            s.tr.position = new Vector3(s.x, s.h - 3.6f + 3.6f * up, s.z);
            if (s.t > 2.1f) { Object.Destroy(s.tr.gameObject); spikes.RemoveAt(i); }
        }
    }

    // ---------- 氷山（グレイシャル・アロー） ----------
    void EvoBergs(Koma k)
    {
        Vector3 dir = k.arrowDir; if (dir.sqrMagnitude < 0.01f) return;
        float len = Mathf.Max(4f, k.arrowLen);
        var mt = mats.Lit(new Color(0.75f, 0.92f, 1f), 0.85f, 0.1f);
        int n = Mathf.Clamp((int)(len / 2.2f), 3, 9);
        for (int i = 1; i <= n; i++)
        {
            float d = i * len / n; Vector3 side = new Vector3(-dir.z, 0, dir.x) * ((i % 2 == 0 ? 1f : -1f) * 1.3f);
            Vector3 p = k.arrowFrom + dir * d + side; float gh = Arena.H(p.x, p.z); if (gh < -100f) continue;
            p.y = gh;
            var g = MeshKit.Obj("Berg", null, MeshKit.Frustum(1.5f, 0.05f, 2.8f + (i % 3) * 0.7f, 6, false), mt);
            g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.transform.position = p - Vector3.up * 3.5f; g.transform.rotation = Quaternion.Euler(0, i * 47f, 0);
            bergs.Add(new Berg { tr = g.transform, pos = p, t = 0, appear = d / ArrowSpeed, owner = k });
        }
    }
    void UpdateBergs(float dt)
    {
        for (int i = bergs.Count - 1; i >= 0; i--)
        {
            var b = bergs[i]; b.t += dt;
            float a = b.t - b.appear; float up = a < 0 ? 0f : a < 0.25f ? a / 0.25f : a < EvoBergLife ? 1f : Mathf.Clamp01(1f - (a - EvoBergLife) / 0.6f);
            b.tr.position = b.pos - Vector3.up * 3.5f * (1f - up);
            if (a > EvoBergLife + 0.7f) { Object.Destroy(b.tr.gameObject); bergs.RemoveAt(i); continue; }
            if (up < 0.9f) continue;
            for (int q = 0; q < 4; q++) if (b.cd[q] > 0) b.cd[q] -= dt;
            foreach (var o in komas)
            {
                if (!o.alive || !o.root.activeSelf || b.cd[o.idx] > 0 || o == b.owner) continue;
                Vector3 d = o.pos - b.pos; float dy = Mathf.Abs(d.y); d.y = 0;
                if (d.magnitude > EvoBergR + o.Radius * 0.5f || dy > 2.2f) continue;
                b.cd[o.idx] = 0.7f;
                Vector3 n = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                o.vel += n * 7f / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 2f); o.grounded = false;
                o.spin -= EvoBergSpin; o.lossHit += EvoBergSpin; o.frostT = Mathf.Max(o.frostT, 1.2f); o.slipT = EvoSlipSec; o.hitFlash = 0.15f;   // 回転が削られ、しばらく滑りやすく・弾かれやすくなる
                if (o.spin <= 0) { o.spin = 0; KO(o, "spin"); }
                IceBurst(o.Center, 30, 1.2f);
            }
        }
    }

    // ---------- チェイン・バインド: 2体まで ----------
    void EvoChain(Koma k)
    {
        Koma first = k.chainTo; if (first == null) return;
        Koma second = null; float bd = 1e9f;
        foreach (var o in komas) { if (o == k || o == first || !o.alive || !o.root.activeSelf) continue; float d = (o.pos - k.pos).sqrMagnitude; if (d < bd) { bd = d; second = o; } }
        foreach (var c in chains) if (c.a == k) c.t *= EvoChainMul;
        k.chainT *= EvoChainMul;
        if (second != null && bd < 18f * 18f) { StartChain(k, second); foreach (var c in chains) if (c.a == k) c.t = Mathf.Max(c.t, ChainSec * EvoChainMul); k.chainTo = first; }
    }
}
