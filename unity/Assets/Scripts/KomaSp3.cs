using System.Collections.Generic;
using UnityEngine;

// 2026-11-28 必殺2つの追加と、必殺ごとのゲージ量
//   8 ソニック・トレイル（5秒、通った道に光の帯を残す。帯は4秒残り、触れた相手を削って弾く。自分には当たらない）
//   9 ファントム・ミラージュ（6秒、左右に光の分身が2体つき、同じ動きをして触れた相手に当たる）
public partial class KomaBoot
{
    // 数字の調整はここ
    const float TrailSec = 5f, TrailLife = 4f, TrailDmg = 6f, TrailKnock = 6.5f, TrailRadius = 1.15f, TrailGap = 0.7f;
    const float MirageSec = 6f, MirageSide = 2.4f, MirageDmg = 2.5f, MirageKnock = 6f;
    // 強い技ほど多く要る（ゲージは0〜100）。メテオ テンペスト ファントム・レイド イージス アンカー スパイラル アロー チェイン トレイル ミラージュ
    public static readonly float[] SpNeed = { 90f, 100f, 70f, 80f, 85f, 70f, 100f, 80f, 75f, 90f };
    public static float NeedOf(Koma k) => SpNeed[Mathf.Clamp(k.sp, 0, SpNeed.Length - 1)];

    class TrailPt { public Vector3 p; public float t; public int owner; }
    readonly List<TrailPt> trailPts = new List<TrailPt>();
    readonly float[] trailT = new float[4]; readonly Vector3[] trailLast = new Vector3[4];
    readonly LineRenderer[] trailLr = new LineRenderer[4];
    readonly float[,] trailCd = new float[4, 4];
    Material trailMat;

    class Mirage { public Koma owner; public float side, t; public Vector3 pos; public Transform tr; public Material m; public LineRenderer lr; public List<Vector3> hist = new List<Vector3>(); public float[] cd = new float[4]; }
    readonly List<Mirage> mirages = new List<Mirage>();
    readonly Vector3[] heading = new Vector3[4];
    Material mirageMat;

    void ClearSp3()
    {
        trailPts.Clear();
        for (int i = 0; i < 4; i++) { trailT[i] = 0; if (trailLr[i]) trailLr[i].positionCount = 0; heading[i] = Vector3.forward; }
        foreach (var m in mirages) if (m.tr) Object.Destroy(m.tr.gameObject);
        mirages.Clear();
    }

    void Sp3Start(Koma k)
    {
        if (k.sp == 8)
        {
            trailT[k.idx] = TrailSec; trailLast[k.idx] = new Vector3(0, -999, 0);
            EmitEv("special", k, 8); SpecialFx(k, 0.8f);
            Wave(k.pos, 5f); Pulse(k.pos, SpColors[8], 8f, 16f);
            return;
        }
        // 9: 影の分身を左右に1体ずつ（同じ持ち主の前の分身は消す）
        for (int i = mirages.Count - 1; i >= 0; i--) if (mirages[i].owner == k) { if (mirages[i].tr) Object.Destroy(mirages[i].tr.gameObject); mirages.RemoveAt(i); }
        if (mirageMat == null) mirageMat = mats.AddGlow(SpColors[9]);
        for (int s = -1; s <= 1; s += 2)
        {
            var c = new Mirage { owner = k, side = s, t = MirageSec, pos = k.pos };
            var g = new GameObject("Mirage").transform;
            c.m = new Material(mirageMat);
            var a = MeshKit.Obj("d", g, MeshKit.Frustum(0.85f, 0.9f, 0.2f, 28, true), c.m); a.transform.localPosition = new Vector3(0, 0.4f, 0);
            var b = MeshKit.Obj("c", g, MeshKit.Frustum(0.34f, 0.3f, 0.45f, 20, true), c.m); b.transform.localPosition = new Vector3(0, 0.55f, 0);
            var l = MeshKit.Obj("l", g, MeshKit.Frustum(0.02f, 0.28f, 0.4f, 16, true), c.m);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            c.tr = g; c.lr = MakeLine(g, c.m, 0.7f, false); c.lr.positionCount = 0;
            Vector3 hd = heading[k.idx].sqrMagnitude > 0.1f ? heading[k.idx] : Vector3.forward;
            c.pos = k.pos + new Vector3(hd.z, 0, -hd.x) * MirageSide * s;
            g.position = c.pos;
            mirages.Add(c);
        }
        EmitEv("special", k, 9); SpecialFx(k, 0.8f);
        Wave(k.pos, 5f); Pulse(k.pos, SpColors[9], 8f, 16f);
    }

    void UpdateSp3(float dt)
    {
        if (phase != "fight" && phase != "end") { return; }
        // ---- 光の帯 ----
        if (trailMat == null) trailMat = mats.AddGlow(SpColors[8]);
        for (int i = 0; i < 4; i++)
        {
            var k = komas[i];
            if (trailT[i] > 0)
            {
                if (!k.alive) trailT[i] = 0;
                else
                {
                    trailT[i] -= dt;
                    Vector3 hp = k.pos + Vector3.up * 0.3f;
                    if ((hp - trailLast[i]).magnitude > TrailGap || trailLast[i].y < -900) { trailLast[i] = hp; trailPts.Add(new TrailPt { p = hp, t = TrailLife, owner = i }); if (trailPts.Count > 400) trailPts.RemoveAt(0); }
                    if (Random.value < 0.5f) Fleck(hp + Random.insideUnitSphere * 0.4f, Vector3.up * 0.6f, new Color(0.7f, 1f, 1f), 0.12f, 0.4f, true);
                }
            }
        }
        for (int j = trailPts.Count - 1; j >= 0; j--) { trailPts[j].t -= dt; if (trailPts[j].t <= 0) trailPts.RemoveAt(j); }
        for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++) if (trailCd[a, b] > 0) trailCd[a, b] -= dt;
        for (int i = 0; i < 4; i++)
        {
            var pts = new List<TrailPt>(); foreach (var tp in trailPts) if (tp.owner == i) pts.Add(tp);
            if (pts.Count < 2) { if (trailLr[i]) trailLr[i].positionCount = 0; }
            else
            {
                if (!trailLr[i])
                {
                    var go = new GameObject("TrailLine"); trailLr[i] = go.AddComponent<LineRenderer>();
                    var lr = trailLr[i]; lr.useWorldSpace = true; lr.widthMultiplier = 1.4f; lr.numCapVertices = 3; lr.sharedMaterial = trailMat;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.View;
                    lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.08f), new Keyframe(1, 1f));
                }
                var l2 = trailLr[i]; l2.positionCount = pts.Count;
                for (int p = 0; p < pts.Count; p++) l2.SetPosition(p, pts[p].p);
                float newest = pts[pts.Count - 1].t / TrailLife;
                l2.sharedMaterial.SetColor("_TintColor", SpColors[8] * (0.5f + 0.12f * Mathf.Sin(Time.time * 18f)));
            }
        }
        foreach (var tp in trailPts)
        {
            var owner = komas[tp.owner];
            foreach (var o in komas)
            {
                if (o == owner || !o.alive || !o.root.activeSelf || trailCd[tp.owner, o.idx] > 0) continue;
                Vector3 d = o.pos - tp.p; float dy = Mathf.Abs(d.y - 0.3f); d.y = 0;
                if (d.magnitude > TrailRadius + o.Radius * 0.6f || dy > 1.4f) continue;
                trailCd[tp.owner, o.idx] = 0.5f;
                Vector3 n = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                o.vel += n * TrailKnock / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 2f); o.grounded = false;
                Damage(owner, o, TrailDmg * owner.st.atk / o.st.def); o.hitFlash = 0.12f;
                Spark(o.Center, n + Vector3.up * 0.4f, 30, 1.2f); Pulse(o.pos, SpColors[8], 4f, 10f);
                var sp = cam.WorldToViewportPoint(o.pos);
                Emit("{\"t\":\"e\",\"k\":\"trailhit\",\"i\":" + o.idx + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
            }
        }
        // ---- 分身 ----
        for (int i = mirages.Count - 1; i >= 0; i--)
        {
            var c = mirages[i]; var k = c.owner;
            c.t -= dt;
            if (c.t <= 0 || !k.alive || phase == "end") { if (c.tr) Object.Destroy(c.tr.gameObject); mirages.RemoveAt(i); continue; }
            Vector3 hv = new Vector3(k.vel.x, 0, k.vel.z);
            if (hv.magnitude > 1.5f) heading[k.idx] = Vector3.Lerp(heading[k.idx], hv.normalized, 1f - Mathf.Exp(-8f * dt)).normalized;
            Vector3 hd = heading[k.idx].sqrMagnitude > 0.1f ? heading[k.idx] : Vector3.forward;
            Vector3 want = k.pos + new Vector3(hd.z, 0, -hd.x) * MirageSide * c.side;
            float f = 1f - Mathf.Exp(-14f * dt);
            c.pos.x = Mathf.Lerp(c.pos.x, want.x, f); c.pos.z = Mathf.Lerp(c.pos.z, want.z, f);
            float gh = Arena.H(c.pos.x, c.pos.z), og = Arena.H(k.pos.x, k.pos.z);
            c.pos.y = gh < -100f ? k.pos.y : gh + Mathf.Max(0f, k.pos.y - (og < -100f ? k.pos.y : og));
            c.tr.position = c.pos; c.tr.rotation = Quaternion.Euler(0, Time.time * 700f * (k.left ? -1f : 1f), 0);
            float fade = Mathf.Clamp01(c.t / 0.6f);
            c.m.SetColor("_TintColor", SpColors[9] * ((0.26f + 0.08f * Mathf.Sin(Time.time * 14f + c.side)) * fade));
            c.hist.Add(c.pos + Vector3.up * 0.35f); if (c.hist.Count > 12) c.hist.RemoveAt(0);
            if (c.hist.Count >= 2) { c.lr.positionCount = c.hist.Count; for (int p = 0; p < c.hist.Count; p++) c.lr.SetPosition(p, c.hist[p]); }
            for (int o2 = 0; o2 < 4; o2++) if (c.cd[o2] > 0) c.cd[o2] -= dt;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf || c.cd[o.idx] > 0) continue;
                Vector3 d = o.pos - c.pos; float dy = Mathf.Abs(d.y); d.y = 0;
                if (d.magnitude > 0.9f + o.Radius || dy > 1.3f) continue;
                c.cd[o.idx] = 0.6f;
                Vector3 n = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                float rv = (k.vel - o.vel).magnitude;
                o.vel += n * (MirageKnock + rv * 0.2f) / Mathf.Sqrt(o.st.mass); o.vel.y = Mathf.Max(o.vel.y, 2.5f); o.grounded = false;
                Damage(k, o, (MirageDmg + Mathf.Min(8f, rv * 0.25f)) * k.st.atk / o.st.def); o.hitFlash = 0.15f;
                Spark(o.Center, n, 40, 1.4f); Pulse(o.pos, SpColors[9], 4f, 10f);
                var sp = cam.WorldToViewportPoint(o.pos);
                Emit("{\"t\":\"e\",\"k\":\"mirhit\",\"i\":" + o.idx + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
            }
        }
    }
}
