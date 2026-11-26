using System.Collections.Generic;
using UnityEngine;

// パッシブスキル 8種（1人1つ）。0 逆鱗 1 天翔 2 魂喰らい 3 雷鳴の刻印 4 不屈 5 紅蓮の轍 6 影分身 7 黒鉄の盾
public partial class KomaBoot
{
    static readonly Color[] PasColors = {
        new Color(1f, 0.12f, 0.1f), new Color(1f, 0.88f, 0.45f), new Color(0.4f, 1f, 0.6f), new Color(0.7f, 0.35f, 1f),
        new Color(1f, 1f, 1f), new Color(1f, 0.5f, 0.12f), new Color(0.4f, 0.6f, 1f), new Color(0.4f, 0.9f, 1f) };
    int cpuPasMode = -2;   // -2 ランダム（半分は持つ） -1 なし 0〜7 固定（検査用）

    // 画面へ: 名前（自分の物だけ表示）
    void PasEvent(Koma k, int idx)
    {
        if (k.pasCd > 0) return; k.pasCd = 3f;
        EmitEv("pas", k, idx);
    }
    void PasFx(Koma k, int type) { EmitEv("pasfx", k, type); }

    // 毎フレーム（生きている間）
    void PassiveUpdate(Koma k, float dt)
    {
        if (k.pasCd > 0) k.pasCd -= dt;
        if (k.pas < 0 || !k.alive || phase != "fight") { k.st.atk = k.baseAtk; return; }
        switch (k.pas)
        {
            case 0:
            {
                bool r = k.SpinRatio < 0.3f;
                if (r && !k.rage) { k.rage = true; k.pasCd = 0; PasEvent(k, 0); Pulse(k.pos, PasColors[0], 9f, 16f); Spark(k.Center, Vector3.up, 70, 1.6f); Wave(k.pos, 5f); if (k.isPlayer) { shake = Mathf.Max(shake, 0.5f); fovKick = Mathf.Max(fovKick, 8f); } }
                if (!r) k.rage = false;
                k.st.atk = k.baseAtk * (k.rage ? 1.25f : 1f);
                if (k.rage) { k.SetAura(PasColors[0], 0.15f); if (Random.value < 0.6f) Spark(k.Center + Random.insideUnitSphere * 0.5f, Vector3.up, 2, 0.7f); }
                break;
            }
            case 1:
            {
                float gh = Arena.H(k.pos.x, k.pos.z);
                bool air = !k.grounded && gh > -100 && k.pos.y - gh > 0.7f;
                if (air && !k.wasAir) { k.pasCd = Mathf.Min(k.pasCd, 0f); PasEvent(k, 1); Pulse(k.pos, PasColors[1], 5f, 12f); Spark(k.Center, Vector3.up, 24, 1f); }
                if (air && Random.value < 0.5f) Spark(k.Center + new Vector3(Random.Range(-1.2f, 1.2f), 0.3f, Random.Range(-1.2f, 1.2f)), Vector3.up, 1, 0.5f);
                k.wasAir = air;
                break;
            }
            case 5:
            {
                bool on = k.SpinRatio >= 0.7f && k.grounded;
                if (on)
                {
                    k.SetAura(PasColors[5], 0.1f);
                    Vector3 hv = new Vector3(k.vel.x, 0, k.vel.z);
                    if (hv.magnitude > 3f && (k.pos - k.lastFire).sqrMagnitude > 0.8f)
                    {
                        if (k.pasCd <= 0) { PasEvent(k, 5); Pulse(k.pos, PasColors[5], 5f, 12f); }
                        k.lastFire = k.pos; fires.Add(new Fire { pos = k.pos, owner = k, t = FireLife, dmg = 8f });
                        FireBurst(k.pos, 5);
                    }
                }
                break;
            }
        }
    }

    // 天翔: 着地で衝撃波の輪（近くの相手を押し出す）
    void PasLanded(Koma k, Vector3 at, float impactV)
    {
        if (k.pas != 1 || impactV < 10f || phase != "fight") return;
        Wave(at, 6.5f); Wave(at, 3.5f); Pulse(at, PasColors[1], 7f, 14f); Spark(at + Vector3.up * 0.2f, Vector3.up, 50, 1.4f);
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - at; d.y = 0; float dist = d.magnitude;
            if (dist > 4.5f) continue;
            Vector3 n = dist > 0.01f ? d / dist : Vector3.forward;
            o.vel += n * 9f * (1f - dist / 4.5f * 0.5f) / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 3.5f); o.grounded = false;
        }
        PasFx(k, 1);
        if (k.isPlayer) shake = Mathf.Max(shake, 0.4f);
    }

    // 魂喰らい: 削った分の一部を自分へ（光の粒が流れ込む）
    void PasSoul(Koma from, Koma to, float dmg)
    {
        if (from.pas != 2 || !from.alive) return;
        from.spin = Mathf.Min(from.spinMax, from.spin + dmg * 0.4f);
        if (from.pasCd <= 0) { EmitEv("pas", from, 2); from.pasCd = 3f; }
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < 9; i++)
        {
            Vector3 st = to.Center + Random.insideUnitSphere * 0.5f;
            ep.position = st; ep.velocity = (from.Center - st) / 0.34f + Vector3.up * 1.6f;
            ep.startSize = Random.Range(0.12f, 0.22f); ep.startLifetime = 0.34f;
            ep.startColor = Random.value < 0.5f ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 1f, 0.9f);
            sparks.Emit(ep, 1);
        }
        Pulse(from.pos, PasColors[2], 2.5f, 8f);
    }

    // 当たりの後: 雷鳴の刻印・黒鉄の盾のたまり
    void PasOnHit(Koma from, Koma to, bool dealt)
    {
        if (!dealt) return;
        if (from.pas == 3 && to.alive)
        {
            if (to.stampBy == from) to.stampN++; else { to.stampBy = from; to.stampN = 1; }
            Spark(to.Center, Vector3.up, 24, 1f); Pulse(to.pos, PasColors[3], 3f, 8f); PasFx(to, 2);
            if (to.stampN >= 3) { to.stampN = 0; to.stampBy = null; SpawnStrike(from, to); }
        }
        if (to.pas == 7 && to.alive && !to.guard)
        {
            to.hitsTaken++;
            if (to.hitsTaken % 3 == 0) { to.guard = true; to.pasCd = 0; PasEvent(to, 7); Pulse(to.pos, PasColors[7], 6f, 12f); Spark(to.Center, Vector3.up, 40, 1.2f); Wave(to.pos, 3.5f); }
        }
    }

    // 黒鉄の盾: 次の1発を無効にして砕ける。true なら防いだ
    bool PasGuard(Koma k)
    {
        if (!k.guard) return false;
        k.guard = false;
        Spark(k.Center, Vector3.up, 90, 1.8f); Wave(k.pos, 4.5f); Pulse(k.pos, PasColors[7], 9f, 14f); PasFx(k, 3);
        if (k.isPlayer) shake = Mathf.Max(shake, 0.35f);
        return true;
    }

    // 不屈: 1試合1回、回転0か場外の瞬間に踏みとどまる。true なら倒れない
    bool PassiveKo(Koma k, string why)
    {
        if (k.pas != 4 || k.gutsUsed || phase != "fight" || !k.alive) return false;
        k.gutsUsed = true; k.spin = k.spinMax * 0.05f; k.pasCd = 0;
        if (why == "out")
        {
            Vector2 d = new Vector2(k.pos.x, k.pos.z); d = d.sqrMagnitude > 0.01f ? d.normalized : Vector2.right;
            float rx = d.x * 10f, rz = d.y * 10f;
            k.rescue = new Vector3(rx, Arena.H(rx, rz) + 1.6f, rz); k.rescueSet = true;
        }
        else k.vel = new Vector3(k.vel.x * 0.3f, Mathf.Max(k.vel.y, 5f), k.vel.z * 0.3f);
        Vector3 at = k.rescueSet ? k.rescue : k.pos;
        Pulse(at, Color.white, 14f, 22f); Spark(at + Vector3.up * 0.5f, Vector3.up, 160, 2.2f); Wave(at, 5f); Wave(at, 8f); Wave(at, 11f);
        PasEvent(k, 4); PasFx(k, 4);
        if (k.isPlayer) { shake = Mathf.Max(shake, 0.9f); fovKick = Mathf.Max(fovKick, 12f); }
        return true;
    }

    // ---------- 雷鳴の刻印: 予告の円 → 0.9秒後に落ちる。円の外へ逃げれば外れる ----------
    class Strike { public Vector3 pos; public Koma owner; public float t; public bool done, active; public bool[] react = new bool[4]; public Transform ring, fill, bolt; public Material rm, fm, bm; public float boltT; }
    readonly List<Strike> strikes = new List<Strike>();
    public const float StrikeDelay = 0.9f, StrikeR = 2.4f;

    void SpawnStrike(Koma owner, Koma victim)
    {
        Strike s = null;
        foreach (var x in strikes) if (!x.active) { s = x; break; }
        if (s == null)
        {
            s = new Strike();
            var pc = PasColors[3];
            s.ring = MeshKit.Obj("sRing", null, MeshKit.Ring(0.9f, 1f, 56), mats.AddGlow(pc)).transform;
            s.fill = MeshKit.Obj("sFill", null, MeshKit.Disc(1f, 40), mats.AddGlow(pc)).transform;
            s.bolt = MeshKit.Obj("bolt", null, MeshKit.Frustum(0.3f, 0.75f, 18f, 12, false), mats.AddGlow(new Color(0.85f, 0.7f, 1f))).transform;
            foreach (var t in new[] { s.ring, s.fill, s.bolt }) t.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.rm = s.ring.GetComponent<MeshRenderer>().material; s.fm = s.fill.GetComponent<MeshRenderer>().material; s.bm = s.bolt.GetComponent<MeshRenderer>().material;
            strikes.Add(s);
        }
        float gh = Arena.H(victim.pos.x, victim.pos.z);
        s.pos = new Vector3(victim.pos.x, gh > -100 ? gh : victim.pos.y, victim.pos.z);
        s.owner = owner; s.t = 0; s.done = false; s.active = true; s.boltT = 0;
        for (int i = 0; i < 4; i++) s.react[i] = rng.NextDouble() < 0.8;   // CPU は8割がよける
        s.ring.position = s.pos + Vector3.up * 0.1f; s.fill.position = s.pos + Vector3.up * 0.09f;
        s.ring.gameObject.SetActive(true); s.fill.gameObject.SetActive(true); s.bolt.gameObject.SetActive(false);
        Pulse(s.pos, PasColors[3], 7f, 14f);
        var sp = cam.WorldToViewportPoint(s.pos);
        Emit("{\"t\":\"e\",\"k\":\"warn\",\"i\":" + owner.idx + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
        EmitEv("pas", owner, 3);
    }

    void UpdateStrikes(float dt)
    {
        foreach (var s in strikes)
        {
            if (!s.active) continue;
            if (!s.done)
            {
                s.t += dt;
                float k = Mathf.Clamp01(s.t / StrikeDelay);
                float pulse = 1f + 0.06f * Mathf.Sin(s.t * (14f + 30f * k));
                s.ring.localScale = Vector3.one * StrikeR * pulse; s.fill.localScale = Vector3.one * StrikeR * k;
                s.rm.SetColor("_TintColor", PasColors[3] * (0.5f + 0.4f * Mathf.Sin(s.t * (16f + 40f * k))));
                s.fm.SetColor("_TintColor", PasColors[3] * (0.12f + 0.45f * k));
                if (s.t >= StrikeDelay) ResolveStrike(s);
            }
            else
            {
                s.boltT += dt;
                float a = 1f - s.boltT / 0.3f;
                if (a <= 0) { s.active = false; s.bolt.gameObject.SetActive(false); s.ring.gameObject.SetActive(false); s.fill.gameObject.SetActive(false); continue; }
                s.bm.SetColor("_TintColor", new Color(0.9f, 0.8f, 1f) * (0.9f * a));
                s.bolt.localScale = new Vector3(1f + (1f - a), 1f, 1f + (1f - a));
            }
        }
    }

    void ResolveStrike(Strike s)
    {
        s.done = true; s.boltT = 0;
        s.ring.gameObject.SetActive(false); s.fill.gameObject.SetActive(false);
        s.bolt.position = s.pos; s.bolt.gameObject.SetActive(true);
        int hits = 0;
        foreach (var o in komas)
        {
            if (o == s.owner || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - s.pos; d.y = 0;
            if (d.magnitude > StrikeR || o.pos.y - s.pos.y > 3.2f) continue;
            hits++;
            Damage(s.owner, o, 16f * s.owner.st.atk / o.st.def);
            o.lockT = 0.9f; o.vel = new Vector3(o.vel.x * 0.1f, Mathf.Max(0f, o.vel.y), o.vel.z * 0.1f); o.hitFlash = 0.3f;
            Spark(o.Center, Vector3.up, 70, 1.8f);
        }
        Pulse(s.pos, Color.white, 14f, 22f);
        Spark(s.pos + Vector3.up * 0.2f, Vector3.up, 60, 1.6f); Dust(s.pos, 22); Wave(s.pos, StrikeR * 2.2f);
        Scorch(s.pos, hits > 0 ? 3f : 2.6f);
        float near = Mathf.Clamp(1.1f - Vector3.Distance(player.pos, s.pos) / 18f, 0.25f, 1f);
        shake = Mathf.Max(shake, 0.9f * near); fovKick = Mathf.Max(fovKick, 8f * near);
        var sp = cam.WorldToViewportPoint(s.pos);
        Emit("{\"t\":\"e\",\"k\":\"thunder\",\"i\":" + s.owner.idx + ",\"hit\":" + hits + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
    }

    // CPU（と自動操作）は予告の円から逃げようとする。戻り値 true なら今フレームの動きを上書き
    bool DodgeStrike(Koma k, out Vector2 move)
    {
        move = Vector2.zero;
        foreach (var s in strikes)
        {
            if (!s.active || s.done || s.owner == k || !s.react[k.idx] || s.t < 0.18f) continue;
            Vector3 d = k.pos - s.pos; d.y = 0;
            if (d.magnitude > StrikeR + 1.2f) continue;
            move = d.sqrMagnitude > 0.01f ? new Vector2(d.x, d.z).normalized : Vector2.right;
            return true;
        }
        return false;
    }

    // ---------- 影分身: ダッシュの後、残像が0.5秒その場に残り、触れた相手を弾く ----------
    class Clone { public Vector3 pos; public Koma owner; public float t; public bool active; public Transform tr; public Material m; public bool[] hit = new bool[4]; }
    readonly List<Clone> clones = new List<Clone>();
    void SpawnClone(Koma k)
    {
        Clone c = null;
        foreach (var x in clones) if (!x.active) { c = x; break; }
        if (c == null)
        {
            c = new Clone();
            var g = new GameObject("Clone").transform;
            var m = mats.AddGlow(PasColors[6]);
            var a = MeshKit.Obj("d", g, MeshKit.Frustum(0.8f, 0.85f, 0.18f, 28, true), m); a.transform.localPosition = new Vector3(0, 0.38f, 0);
            var b = MeshKit.Obj("c", g, MeshKit.Frustum(0.3f, 0.26f, 0.4f, 20, true), m); b.transform.localPosition = new Vector3(0, 0.5f, 0);
            var l = MeshKit.Obj("l", g, MeshKit.Frustum(0.02f, 0.25f, 0.38f, 16, true), m);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            c.tr = g; c.m = a.GetComponent<MeshRenderer>().material;
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = c.m;
            clones.Add(c);
        }
        c.owner = k; c.pos = k.pos; c.t = 0; c.active = true; for (int i = 0; i < 4; i++) c.hit[i] = false;
        c.tr.position = k.pos; c.tr.gameObject.SetActive(true);
        Spark(k.Center, Vector3.up, 20, 1f); Pulse(k.pos, PasColors[6], 3f, 8f);
        PasEvent(k, 6);
    }
    void UpdateClones(float dt)
    {
        foreach (var c in clones)
        {
            if (!c.active) continue;
            c.t += dt;
            float a = 1f - c.t / 0.5f;
            if (a <= 0) { c.active = false; c.tr.gameObject.SetActive(false); continue; }
            c.tr.rotation = Quaternion.Euler(0, Time.unscaledTime * 600f, 0);
            c.m.SetColor("_TintColor", PasColors[6] * (1.3f * a));
            foreach (var o in komas)
            {
                if (o == c.owner || !o.alive || !o.root.activeSelf || c.hit[o.idx]) continue;
                Vector3 d = o.pos - c.pos; float dy = Mathf.Abs(d.y); d.y = 0;
                if (d.magnitude > 0.85f + o.Radius || dy > 1.3f) continue;
                c.hit[o.idx] = true;
                Vector3 n = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                o.vel += n * 11f / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 2.5f); o.grounded = false;
                Damage(c.owner, o, 5f * c.owner.st.atk / o.st.def); o.hitFlash = 0.15f;
                Spark(o.Center, n, 36, 1.4f); PasFx(o, 0);
            }
        }
    }
}
