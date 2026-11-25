using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// 全体の進行・入力・物理・CPU・演出・画面（HTML）とのやりとり
public class KomaBoot : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void KB_Emit(string s);
#else
    static void KB_Emit(string s) { }
#endif

    Materials mats;
    Camera cam;
    Transform camRig;
    readonly List<Koma> komas = new List<Koma>();
    Koma player;
    string phase = "title";
    float phaseT, fightT, emitT;
    bool sudden;
    int pTop = 0, pCore = 0, pLeg = 0, nCpu = 2;
    bool autoPlayer;
    float testScale = 1f;
    bool paused;
    Vector2 touchMove;
    bool touchDash, touchSpecial;
    Vector3 aimPoint;
    Transform reticle; LineRenderer aimLine;
    ParticleSystem sparks, dust;
    readonly List<Transform> waves = new List<Transform>();
    readonly List<float> waveT = new List<float>();
    readonly List<float> waveSize = new List<float>();
    float slowUntil, slowScale = 1f, shake;
    readonly Dictionary<long, float> pairCd = new Dictionary<long, float>();
    readonly Brain[] brains = new Brain[4];
    int endWin = -1; string endWhy = ""; float endT;
    System.Random rng = new System.Random();

    static readonly Color[] Colors = {
        new Color(0.95f, 0.25f, 0.15f),   // 自分: 朱
        new Color(0.15f, 0.45f, 0.95f),   // 藍
        new Color(0.2f, 0.75f, 0.3f),     // 若草
        new Color(0.98f, 0.75f, 0.1f),    // 山吹
    };

    class Brain
    {
        public int target = -1; public float retarget, react, padGoal = -1, think;
        public Vector2 jitter;
    }

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.shadowDistance = 45f;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        mats = new Materials();

        // 空とライト
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.36f, 0.4f, 0.56f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.3f, 0.36f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.12f, 0.18f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.98f, 0.66f, 0.55f); RenderSettings.fogStartDistance = 40; RenderSettings.fogEndDistance = 140;
        var sky = MeshKit.Obj("Sky", null, MeshKit.SkyDome(180f, new Color(0.18f, 0.25f, 0.6f), new Color(1f, 0.62f, 0.5f), new Color(0.2f, 0.12f, 0.25f)), mats.Sky);
        sky.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.93f, 0.85f); sun.intensity = 1.0f;
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f;
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0);
        var rim = new GameObject("Rim").AddComponent<Light>();
        rim.type = LightType.Directional; rim.color = new Color(0.55f, 0.65f, 1f); rim.intensity = 0.45f;
        rim.transform.rotation = Quaternion.Euler(25f, 160f, 0);

        // 観客席の代わりの柱の明かり
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2 / 16;
            var pillar = MeshKit.Obj("Pillar", null, MeshKit.Box(new Vector3(1.2f, 9f, 1.2f)), mats.Base);
            pillar.transform.position = new Vector3(Mathf.Cos(a) * 30f, -2f, Mathf.Sin(a) * 30f);
            var lamp = MeshKit.Obj("Lamp", null, MeshKit.Box(new Vector3(1.6f, 0.5f, 1.6f)), mats.Emis(Color.white, i % 2 == 0 ? new Color(1f, 0.8f, 0.5f) : new Color(0.5f, 0.9f, 1f)));
            lamp.transform.position = new Vector3(Mathf.Cos(a) * 30f, 2.7f, Mathf.Sin(a) * 30f);
        }

        Arena.BuildVisual(mats);

        camRig = new GameObject("CamRig").transform;
        cam = new GameObject("Cam").AddComponent<Camera>();
        cam.transform.SetParent(camRig, false);
        cam.fieldOfView = 50f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.15f, 0.3f);
        cam.gameObject.AddComponent<AudioListener>();

        // 狙いの印
        reticle = MeshKit.Obj("Reticle", null, MeshKit.Ring(0.42f, 0.66f, 32), mats.Alpha(new Color(1f, 0.25f, 0.12f, 1f), Texture2D.whiteTexture)).transform;
        aimLine = new GameObject("AimLine").AddComponent<LineRenderer>();
        aimLine.material = mats.Alpha(new Color(1f, 0.3f, 0.15f, 0.85f), Texture2D.whiteTexture);
        aimLine.positionCount = 2; aimLine.widthMultiplier = 0.12f;
        aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        sparks = MakeParticles("Sparks", 0.32f, true);
        dust = MakeParticles("Dust", 0.5f, false);

        for (int i = 0; i < 6; i++)
        {
            var w = MeshKit.Obj("Wave", null, MeshKit.Ring(0.85f, 1f, 48), mats.AddGlow(new Color(1f, 0.85f, 0.5f, 1f))).transform;
            w.gameObject.SetActive(false); waves.Add(w); waveT.Add(-1); waveSize.Add(1);
        }

        // 自分のコマ（見せる台の上）
        player = new Koma(0, pTop, pCore, pLeg, Colors[0], mats) { isPlayer = true };
        komas.Add(player);
        for (int i = 1; i < 4; i++)
        {
            var k = new Koma(i, i % 3, (i + 1) % 3, (i + 2) % 3, Colors[i], mats) { cpu = true };
            k.root.SetActive(false);
            komas.Add(k);
            brains[i] = new Brain();
        }
        brains[0] = new Brain();
        Showcase();
        Emit("{\"t\":\"e\",\"k\":\"ready\"}");
    }

    ParticleSystem MakeParticles(string name, float life, bool stretch)
    {
        var g = new GameObject(name);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = life; main.startSpeed = 0; main.startSize = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = stretch ? 1.2f : -0.05f;
        main.maxParticles = 800; main.playOnAwake = false; main.loop = false;
        var em = ps.emission; em.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
        col.color = gr;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, stretch ? 0.3f : 2.2f));
        var r = g.GetComponent<ParticleSystemRenderer>();
        r.material = stretch ? mats.AddGlow(Color.white, mats.Dot) : mats.Alpha(new Color(1f, 1f, 1f, 0.7f), mats.Dot);
        r.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (stretch) { r.velocityScale = 0.045f; r.lengthScale = 1.2f; }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }

    // ---------- 画面（HTML）とのやりとり ----------
    void Emit(string s) { KB_Emit(s); }

    public void Cmd(string s)
    {
        var parts = s.Split('|');
        string c = parts[0];
        string a = parts.Length > 1 ? parts[1] : "";
        var nums = a.Split(',');
        float N(int i) => i < nums.Length && float.TryParse(nums[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0;
        switch (c)
        {
            case "loadout":
                pTop = (int)N(0); pCore = (int)N(1); pLeg = (int)N(2);
                player.Rebuild(pTop, pCore, pLeg, mats);
                if (phase != "fight") Showcase();
                break;
            case "select": phase = "select"; phaseT = 0; Showcase(); break;
            case "title": phase = "title"; phaseT = 0; Showcase(); break;
            case "start":
                if (nums.Length >= 3) { pTop = (int)N(0); pCore = (int)N(1); pLeg = (int)N(2); player.Rebuild(pTop, pCore, pLeg, mats); }
                if (nums.Length >= 4) nCpu = Mathf.Clamp((int)N(3), 1, 3);
                StartMatch();
                break;
            case "auto": autoPlayer = N(0) > 0; break;
            case "timescale": testScale = Mathf.Clamp(N(0), 0.05f, 8f); break;
            case "pause": paused = N(0) > 0; break;
            case "move": touchMove = new Vector2(N(0), N(1)); if (touchMove.sqrMagnitude > 1) touchMove.Normalize(); break;
            case "dash": touchDash = true; break;
            case "special": touchSpecial = true; break;
            case "gauge": if (nums.Length > 1) komas[(int)N(1)].gauge = N(0); else player.gauge = N(0); break;
            case "spin": komas[(int)N(1)].spin = N(0) * komas[(int)N(1)].spinMax; break;
            case "tp":
                {
                    var k = komas[nums.Length > 6 ? (int)N(6) : 0];
                    k.pos = new Vector3(N(0), N(1), N(2)); k.vel = new Vector3(N(3), N(4), N(5)); k.grounded = false; k.airDashUsed = 0;
                    break;
                }
            case "aimat":
                {
                    var k = komas[(int)N(0)];
                    var sp = cam.WorldToViewportPoint(k.pos);
                    Emit("{\"t\":\"aim\",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
                    break;
                }
        }
    }

    void Showcase()
    {
        foreach (var k in komas) if (!k.isPlayer) k.root.SetActive(false);
        player.Place(new Vector3(0, Arena.H(0, 0), 0));
        player.grounded = true;
        sudden = false;
    }

    void StartMatch()
    {
        phase = "count"; phaseT = 0; fightT = 0; sudden = false; endWin = -1; endT = 0;
        pairCd.Clear();
        int n = 1 + nCpu;
        for (int i = 0; i < 4; i++)
        {
            var k = komas[i];
            if (i >= n) { k.root.SetActive(false); k.alive = false; continue; }
            if (i > 0) k.Rebuild(rng.Next(3), rng.Next(3), rng.Next(3), mats);
            float ang = -Mathf.PI / 2 + i * Mathf.PI * 2 / n;
            float r = 8.2f;
            float x = Mathf.Cos(ang) * r, z = Mathf.Sin(ang) * r;
            k.Place(new Vector3(x, Arena.H(x, z) + 6f, z));
            brains[i] = new Brain { react = 0.6f + (float)rng.NextDouble() * 0.6f };
        }
        Emit("{\"t\":\"e\",\"k\":\"count\",\"n\":3}");
    }

    // ---------- 毎フレーム ----------
    void Update()
    {
        float realDt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        float now = Time.unscaledTime;
        float scale = now < slowUntil ? slowScale : 1f;
        Time.timeScale = paused ? 0f : scale * testScale;
        float dt = Mathf.Min(Time.deltaTime, 0.05f * testScale);
        phaseT += realDt * testScale;

        if (phase == "title" || phase == "select")
        {
            player.spin = player.spinMax;
            player.Visual(Time.unscaledDeltaTime, realDt);
            float a = Time.unscaledTime * (phase == "title" ? 0.15f : 0.35f);
            float dist = phase == "title" ? 24f : 5.2f, hgt = phase == "title" ? 13f : 2.4f;
            Vector3 look = phase == "title" ? new Vector3(0, 0.5f, 0) : new Vector3(0, 0.7f, 0);
            camRig.position = new Vector3(Mathf.Sin(a) * dist, hgt, -Mathf.Cos(a) * dist);
            cam.transform.localPosition = Vector3.zero;
            if (phase == "title") look -= Vector3.Cross(Vector3.up, (look - camRig.position).normalized) * 7f;
            cam.transform.rotation = Quaternion.LookRotation(look - camRig.position);
            reticle.gameObject.SetActive(false); aimLine.enabled = false;
            EmitState(realDt, false);
            UpdateWaves(realDt);
            return;
        }

        if (phase == "count")
        {
            int before = Mathf.CeilToInt(3f - (phaseT - realDt * testScale));
            int nowN = Mathf.CeilToInt(3f - phaseT);
            if (nowN != before && nowN > 0) Emit("{\"t\":\"e\",\"k\":\"count\",\"n\":" + nowN + "}");
            foreach (var k in komas) if (k.root.activeSelf) { k.vel = Vector3.zero; k.Visual(realDt, realDt); }
            if (phaseT >= 3f)
            {
                phase = "fight"; phaseT = 0;
                Emit("{\"t\":\"e\",\"k\":\"go\"}");
                foreach (var k in komas) if (k.root.activeSelf) k.vel = new Vector3(0, -6f, 0);
            }
            FightCamera(realDt);
            EmitState(realDt, true);
            return;
        }

        // fight / end
        if (phase == "fight") fightT += dt;
        if (phase == "fight" && !sudden && fightT > 60f) { sudden = true; Emit("{\"t\":\"e\",\"k\":\"sudden\"}"); }

        // 入力
        Vector2 move = Vector2.zero; bool dash = false, special = false;
        AimUpdate();
        if (phase == "fight" && player.alive)
        {
            if (autoPlayer) Think(player, dt, out move, out dash, out special);
            else
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
                if (touchMove.sqrMagnitude > 0.01f) move = touchMove;
                if (move.sqrMagnitude > 1) move.Normalize();
                dash = Input.GetMouseButtonDown(0) || touchDash;
                special = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || touchSpecial;
                if (touchDash && Input.touchCount == 0 && !Input.GetMouseButtonDown(0))
                {
                    // 画面のボタンのダッシュ: 動かしている向き、なければ近い相手へ
                    var t = Nearest(player);
                    if (move.sqrMagnitude > 0.01f) aimPoint = player.pos + new Vector3(move.x, 0, move.y) * 6f;
                    else if (t != null) aimPoint = t.pos;
                }
            }
            touchDash = false; touchSpecial = false;
            if (dash) TryDash(player, aimPoint);
            if (special) TrySpecial(player, aimPoint);
        }

        // CPU
        var moves = new Vector2[4];
        moves[0] = move;
        for (int i = 1; i < komas.Count; i++)
        {
            var k = komas[i];
            if (!k.alive || !k.root.activeSelf || phase != "fight") continue;
            Think(k, dt, out moves[i], out bool d2, out bool s2);
            if (d2) TryDash(k, AimFor(k));
            if (s2) TrySpecial(k, AimFor(k));
        }

        // 物理（細かく刻む）
        int sub = Mathf.Max(1, Mathf.CeilToInt(dt / (1f / 120f)));
        float h = dt / sub;
        for (int s = 0; s < sub; s++)
        {
            for (int i = 0; i < komas.Count; i++)
            {
                var k = komas[i];
                if (!k.root.activeSelf) continue;
                Step(k, h, moves[i]);
            }
            Collide(h);
        }
        foreach (var k in komas)
        {
            if (!k.root.activeSelf) continue;
            if (k.dashT > 0) k.dashT -= dt;
            if (k.dashCd > 0) k.dashCd -= dt;
            if (k.padCd > 0) k.padCd -= dt;
            if (k.alive && phase == "fight")
            {
                k.spin -= (1.3f * k.st.decay + (sudden ? 8f : 0f)) * dt;
                k.gauge = Mathf.Min(100f, k.gauge + 2.2f * dt);
                if (k.spin <= 0) { k.spin = 0; KO(k, "spin"); }
            }
            k.Visual(dt, realDt);
        }
        var keys = new List<long>(pairCd.Keys);
        foreach (var key in keys) { pairCd[key] -= dt; if (pairCd[key] <= 0) pairCd.Remove(key); }

        // 決着
        if (phase == "fight")
        {
            int alive = 0; Koma last = null;
            foreach (var k in komas) if (k.root.activeSelf && k.alive) { alive++; last = k; }
            if (!player.alive || alive <= 1)
            {
                phase = "end"; endT = 0;
                endWin = player.alive ? 1 : 0;
                endWhy = player.alive ? "" : player.koWhy;
                Slow(0.25f, 0.9f);
            }
        }
        else if (phase == "end")
        {
            endT += realDt;
            if (endT > 1.6f && endT - realDt <= 1.6f)
            {
                var sb = new StringBuilder();
                sb.Append("{\"t\":\"e\",\"k\":\"end\",\"win\":").Append(endWin).Append(",\"why\":\"").Append(endWhy).Append("\",\"time\":").Append(F(fightT))
                  .Append(",\"dealt\":").Append(F(player.dealt)).Append(",\"aerial\":").Append(player.aerialHits).Append(",\"specials\":").Append(player.specials)
                  .Append(",\"dashes\":").Append(player.dashes).Append(",\"spin\":").Append(F(player.SpinRatio)).Append("}");
                Emit(sb.ToString());
            }
        }

        FightCamera(realDt);
        UpdateWaves(realDt);
        EmitState(realDt, true);
    }

    void AimUpdate()
    {
        if (cam == null || player == null) return;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        float py = player.grounded ? player.pos.y : Arena.H(player.pos.x, player.pos.z) > -100 ? Mathf.Min(player.pos.y, Arena.H(player.pos.x, player.pos.z) + 0.5f) : player.pos.y;
        var plane = new Plane(Vector3.up, new Vector3(0, py, 0));
        bool touchOnly = touchMove.sqrMagnitude > 0.01f && !Input.GetMouseButton(0);
        if (!autoPlayer && !touchOnly && plane.Raycast(ray, out float d)) aimPoint = ray.GetPoint(d);
        if (autoPlayer) aimPoint = AimFor(player);
        bool show = phase == "fight" && player.alive;
        reticle.gameObject.SetActive(show);
        aimLine.enabled = show;
        if (!show) return;
        float gh = Arena.H(aimPoint.x, aimPoint.z);
        reticle.position = new Vector3(aimPoint.x, (gh > -100 ? gh : aimPoint.y) + 0.06f, aimPoint.z);
        reticle.localScale = Vector3.one * (player.dashCd > 0 ? 0.6f : 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.08f);
        Vector3 from = player.pos + Vector3.up * 0.3f;
        Vector3 dir = aimPoint - player.pos; dir.y = 0;
        aimLine.SetPosition(0, from);
        aimLine.SetPosition(1, from + dir.normalized * Mathf.Min(dir.magnitude, 4.5f));
    }

    Koma Nearest(Koma k)
    {
        Koma best = null; float bd = 1e9f;
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            float d = (o.pos - k.pos).sqrMagnitude;
            if (d < bd) { bd = d; best = o; }
        }
        return best;
    }

    Vector3 AimFor(Koma k)
    {
        var b = brains[k.idx];
        if (b.target >= 0 && komas[b.target].alive) { var t = komas[b.target]; return t.pos + t.vel * 0.18f; }
        var n = Nearest(k);
        return n != null ? n.pos : Vector3.zero;
    }

    // CPU の頭（自分の自動操作にも使う）
    void Think(Koma k, float dt, out Vector2 move, out bool dash, out bool special)
    {
        var b = brains[k.idx];
        move = Vector2.zero; dash = false; special = false;
        b.retarget -= dt; b.react -= dt; b.think -= dt;
        if (b.target < 0 || b.retarget <= 0 || !komas[b.target].alive)
        {
            b.retarget = 2f + (float)rng.NextDouble() * 2f;
            // 近くて回転の少ない相手をねらう
            float best = 1e9f; b.target = -1;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                float sc = (o.pos - k.pos).magnitude + o.SpinRatio * 6f + (float)rng.NextDouble() * 3f;
                if (sc < best) { best = sc; b.target = o.idx; }
            }
            b.padGoal = rng.NextDouble() < 0.3 ? rng.Next(Arena.Pads.Length) : -1;
            b.jitter = new Vector2((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 2f;
        }
        if (b.target < 0) return;
        var t = komas[b.target];
        Vector3 to = t.pos - k.pos; to.y = 0;
        float dist = to.magnitude;
        Vector2 want = new Vector2(to.x, to.z).normalized + b.jitter * 0.25f;
        if (b.padGoal >= 0 && dist > 4.5f)
        {
            var p = Arena.Pads[(int)b.padGoal];
            want = (p - new Vector2(k.pos.x, k.pos.z)).normalized;
            if ((p - new Vector2(k.pos.x, k.pos.z)).magnitude < 0.6f) b.padGoal = -1;
        }
        // 端に寄りすぎたら内へ
        float r = new Vector2(k.pos.x, k.pos.z).magnitude;
        if (r > Arena.R - 2.5f) want += -new Vector2(k.pos.x, k.pos.z).normalized * (r - (Arena.R - 2.5f)) * 1.2f;
        move = want.sqrMagnitude > 1 ? want.normalized : want;
        if (k.grounded && k.dashCd <= 0 && dist < 6f && b.react <= 0)
        {
            dash = true; b.react = 0.9f + (float)rng.NextDouble() * 1.3f;
        }
        // 空中で相手の上近くなら急降下
        if (!k.grounded && k.airDashUsed < 1 && dist < 6f && k.pos.y > t.pos.y + 1f && k.vel.y < 3f) dash = true;
        if (k.gauge >= 100f && dist < 9f && b.think <= 0) { special = true; }
        if (b.think <= 0) b.think = 0.3f;
    }

    void TryDash(Koma k, Vector3 aim)
    {
        if (!k.alive || k.specialPhase > 0) return;
        Vector3 dir = aim - k.pos;
        if (k.grounded)
        {
            if (k.dashCd > 0) return;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = new Vector3(k.vel.x, 0, k.vel.z);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            float sp = 19f * k.st.speed;
            k.vel = new Vector3(dir.x * sp + k.vel.x * 0.25f, k.vel.y, dir.z * sp + k.vel.z * 0.25f);
            k.dashT = 0.32f; k.dashCd = 0.75f * k.st.dashCd;
            k.spin -= 2.5f; k.dashes++; k.lossDash += 2.5f;
            EmitEv("dash", k, 0);
            Dust(k.pos, 10);
        }
        else
        {
            if (k.airDashUsed >= 1) return;
            k.airDashUsed = 1;
            Vector3 tgt = aim; float gh = Arena.H(aim.x, aim.z); if (gh > -100) tgt.y = gh + 0.3f;
            dir = tgt - k.pos;
            if (dir.y > -1f) dir.y = -1f;
            dir.Normalize();
            k.vel = dir * 24f;
            // 狙った所の近くに相手がいれば、その相手を追いかけて落ちる
            k.diveTarget = -1; float bd = 16f;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                Vector3 dd = o.pos - aim; dd.y = 0;
                if (dd.sqrMagnitude < bd) { bd = dd.sqrMagnitude; k.diveTarget = o.idx; }
            }
            k.dashT = 0.5f; k.spin -= 3f; k.dashes++; k.lossDash += 3f;
            EmitEv("dive", k, 0);
        }
    }

    void TrySpecial(Koma k, Vector3 aim)
    {
        if (!k.alive || k.gauge < 100f || k.specialPhase > 0) return;
        k.gauge = 0; k.specials++;
        k.specialPhase = 1; k.specialT = 0.55f;
        k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.2f);
        k.vel = new Vector3(k.vel.x * 0.2f, 17f, k.vel.z * 0.2f);
        k.grounded = false;
        // 狙った所に一番近い相手を追う
        Koma best = null; float bd = 1e9f;
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            float d = (o.pos - aim).sqrMagnitude;
            if (d < bd) { bd = d; best = o; }
        }
        k.specialTarget = best != null ? new Vector3(best.idx, -999, 0) : aim;
        EmitEv("special", k, 0);
        Slow(0.35f, 0.45f);
        shake = Mathf.Max(shake, 0.25f);
    }

    void KO(Koma k, string why)
    {
        if (!k.alive) return;
        k.Knock(why);
        k.koTime = fightT;
        EmitEv("ko", k, 0, why + "\",\"r\":\"" + F(new Vector2(k.pos.x, k.pos.z).magnitude) + "\",\"vy\":\"" + F(k.vel.y) + "\",\"vh\":\"" + F(new Vector2(k.vel.x, k.vel.z).magnitude) + "\",\"ang\":\"" + F(Mathf.Atan2(k.pos.z, k.pos.x) * Mathf.Rad2Deg) + "\",\"lastHit\":\"" + F(fightT - k.lastHitT));
        Slow(0.2f, 0.5f);
        shake = Mathf.Max(shake, 0.5f);
    }

    // ---------- 物理 ----------
    const float G = 28f, StepUp = 0.35f;

    void Step(Koma k, float dt, Vector2 move)
    {
        Vector3 p = k.pos, v = k.vel;
        bool ctrl = k.alive && phase == "fight" && k.specialPhase == 0;
        if (ctrl)
        {
            float acc = 26f * k.st.speed * (k.grounded ? 1f : 0.3f * k.st.air);
            v.x += move.x * acc * dt; v.z += move.y * acc * dt;
        }
        if (k.specialPhase == 1)
        {
            v.y -= G * 0.9f * dt;
            k.specialT -= dt;
            if (k.specialT <= 0)
            {
                k.specialPhase = 2;
                Vector3 tgt = k.specialTarget.y < -900 ? komas[(int)k.specialTarget.x].pos : k.specialTarget;
                var dir = (tgt + Vector3.up * 0.3f - p).normalized;
                if (dir.y > -0.35f) { dir.y = -0.35f; dir.Normalize(); }
                v = dir * 34f;
                EmitEv("dive", k, 1);
            }
        }
        else if (k.specialPhase == 2) Home(k, ref v, k.specialTarget.y < -900 ? (int)k.specialTarget.x : -1, 34f, dt);
        else v.y -= G * dt;

        if (k.grounded)
        {
            Vector2 gr = Arena.Grad(p.x, p.z);
            if (gr.magnitude < 1.2f) { v.x -= gr.x * G * 0.5f * dt; v.z -= gr.y * G * 0.5f * dt; }
            float fr = Mathf.Exp(-0.9f * k.st.grip * dt);
            v.x *= fr; v.z *= fr;
            float maxS = 9.5f * k.st.speed, hs = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (hs > maxS && k.dashT <= 0)
            {
                float f = Mathf.Max(maxS / hs, Mathf.Exp(-3.2f * dt));
                v.x *= f; v.z *= f;
            }
        }

        if (!k.grounded && k.dashT > 0 && k.diveTarget >= 0 && k.specialPhase == 0) { v.y += G * dt; Home(k, ref v, k.diveTarget, 24f, dt); }
        if (k.grounded) k.diveTarget = -1;
        if (!k.grounded && k.dashT <= 0 && k.specialPhase == 0)
        {
            float hs2 = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (hs2 > 13f) { float f2 = Mathf.Max(13f / hs2, Mathf.Exp(-2.5f * dt)); v.x *= f2; v.z *= f2; }
        }
        Vector3 np = p + v * dt;
        float h0 = Arena.H(p.x, p.z);
        float h = Arena.H(np.x, np.z);
        float hw = Arena.WallH(np.x, np.z);
        // 壁（段差・縁・坂の横・縁の上の見えない柵）
        if (hw > -100 && hw - Mathf.Max(p.y, np.y) > StepUp && hw - p.y > StepUp)
        {
            Vector2 gr = Arena.Grad(np.x, np.z);
            Vector2 n = gr.sqrMagnitude > 1e-4f ? -gr.normalized : new Vector2(p.x - np.x, p.z - np.z).normalized;
            Vector2 vh = new Vector2(v.x, v.z);
            float vn = Vector2.Dot(vh, n);
            if (vn < 0)
            {
                vh -= 1.6f * vn * n;
                if (-vn > 6f && k.alive)
                {
                    k.spin -= Mathf.Min(2.5f, -vn * 0.1f); k.lossWall += Mathf.Min(2.5f, -vn * 0.1f);
                    Spark(new Vector3(np.x, p.y + 0.4f, np.z), new Vector3(n.x, 0.4f, n.y), 8, 0.6f);
                    EmitEv("wall", k, -vn);
                }
            }
            v.x = vh.x; v.z = vh.y;
            np = new Vector3(p.x, np.y, p.z);
            h = h0;
        }

        if (k.grounded)
        {
            if (h < -100 || h < np.y - 0.3f)
            {
                k.grounded = false;
                // 坂の先から飛び出す（大げさに上へ）
                if (k.lastGroundRamp && k.alive && v.y > 0.6f)
                {
                    v.y = 10f * k.st.jump;
                    Vector2 hv = Vector2.ClampMagnitude(new Vector2(v.x, v.z) * 0.62f, 9f);
                    v.x = hv.x; v.z = hv.y;
                    EmitEv("jump", k, 1);
                }
            }
            else
            {
                np.y = h;
                Vector2 gr = Vector2.ClampMagnitude(Arena.Grad(np.x, np.z), 1.2f);
                v.y = gr.x * v.x + gr.y * v.z;
                k.lastGroundRamp = Arena.RampAt(np.x, np.z, out _) >= 0;
                // 跳ね台
                if (Arena.PadAt(np.x, np.z) >= 0 && k.padCd <= 0 && k.alive)
                {
                    v.y = 12.5f * k.st.jump; v.x *= 0.3f; v.z *= 0.3f; k.grounded = false; k.padCd = 0.6f;
                    EmitEv("jump", k, 0);
                }
            }
        }
        else if (h > -100 && np.y <= h)
        {
            float impactV = -v.y;
            np.y = h;
            if (k.specialPhase == 2)
            {
                SpecialImpact(k, np);
                v = new Vector3(v.x * 0.2f, 6f, v.z * 0.2f);
            }
            else if (impactV > 11f && k.alive) { v.y = impactV * 0.3f; k.grounded = false; Dust(np, 14); }
            else { v.y = 0; k.grounded = true; k.airDashUsed = 0; if (impactV > 6f) Dust(np, 8); }
            if (k.specialPhase == 0 && k.dashT > 0 && impactV > 10f) k.dashT = 0.15f;
        }

        if (np.y < -5f && k.alive) KO(k, "out");
        if (np.y < -40f) { np.y = -40f; v = Vector3.zero; }
        k.pos = np; k.vel = v;
    }

    // 急降下の追尾（上から当てるのを人でもできるように）
    void Home(Koma k, ref Vector3 v, int ti, float speed, float dt)
    {
        if (ti < 0 || ti >= komas.Count) return;
        var t = komas[ti];
        if (!t.alive) return;
        Vector3 want = (t.Center + Vector3.up * 0.25f - k.Center);
        if (want.y > -0.3f) want.y = -0.3f;
        want = want.normalized * speed;
        v = Vector3.Lerp(v, want, 1f - Mathf.Exp(-14f * dt));
    }

    void SpecialImpact(Koma k, Vector3 at)
    {
        k.specialPhase = 0; k.dashT = 0;
        Wave(at, 5.5f);
        Spark(at + Vector3.up * 0.3f, Vector3.up, 60, 1.6f);
        Dust(at, 30);
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - at; d.y = 0;
            float dist = d.magnitude;
            if (dist > 5f) continue;
            Vector3 n = dist > 0.01f ? d / dist : Vector3.forward;
            float f = 1f - dist / 5f * 0.5f;
            o.vel += n * 13f * f / Mathf.Sqrt(o.st.mass) * (1.6f - o.st.grip * 0.35f);
            o.vel.y = Mathf.Max(o.vel.y, 5f * f);
            o.grounded = false;
            float dmg = 10f * f * k.st.atk / o.st.def;
            Damage(k, o, dmg);
            o.hitFlash = 0.15f;
        }
        Slow(0.15f, 0.6f);
        shake = Mathf.Max(shake, 0.9f);
        var sp = cam.WorldToViewportPoint(at);
        Emit("{\"t\":\"e\",\"k\":\"boom\",\"i\":" + k.idx + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
    }

    void Damage(Koma from, Koma to, float dmg)
    {
        to.spin -= dmg; to.lossHit += dmg; to.lastHitT = fightT;
        from.dealt += dmg;
        from.gauge = Mathf.Min(100f, from.gauge + dmg * 0.8f);
        to.gauge = Mathf.Min(100f, to.gauge + dmg * 0.6f);
        if (to.spin <= 0) { to.spin = 0; KO(to, "spin"); }
    }

    void Collide(float dt)
    {
        for (int i = 0; i < komas.Count; i++)
            for (int j = i + 1; j < komas.Count; j++)
            {
                var a = komas[i]; var b = komas[j];
                if (!a.root.activeSelf || !b.root.activeSelf || !a.alive || !b.alive) continue;
                Vector3 d = b.Center - a.Center;
                float rr = a.Radius + b.Radius;
                // 高さは楕円で（コマは平たい）
                Vector3 dd = new Vector3(d.x, d.y * 1.6f, d.z);
                float dist = dd.magnitude;
                if (dist >= rr || dist < 1e-4f) continue;
                Vector3 n = d.normalized;
                Vector3 nh = new Vector3(n.x, 0, n.z);
                if (nh.sqrMagnitude < 1e-4f) nh = Vector3.forward; else nh.Normalize();
                // 押し出し
                float pen = rr - dist;
                float ma = a.st.mass, mb = b.st.mass;
                a.pos -= nh * pen * mb / (ma + mb);
                b.pos += nh * pen * ma / (ma + mb);
                long key = i * 10 + j;
                Vector3 rel = a.vel - b.vel;
                float vn = Vector3.Dot(rel, n);
                if (vn <= 0.5f || pairCd.ContainsKey(key)) continue;
                pairCd[key] = 0.25f;

                // 空中から上に乗る突撃
                bool aAir = !a.grounded && a.Center.y > b.Center.y + 0.55f && a.vel.y < -2f;
                bool bAir = !b.grounded && b.Center.y > a.Center.y + 0.55f && b.vel.y < -2f;
                float J = (1f + 0.7f) * vn / (1f / ma + 1f / mb);
                a.vel -= n * J / ma;
                b.vel += n * J / mb;
                // 回転の弾き（おおげさ）
                float sa = Mathf.Max(0, Vector3.Dot(a.vel + n * J / ma, n)), sb = Mathf.Max(0, Vector3.Dot(b.vel - n * J / mb, -n));
                float spinPushA = 4f * a.st.atk * (0.4f + 0.6f * a.SpinRatio), spinPushB = 4f * b.st.atk * (0.4f + 0.6f * b.SpinRatio);
                Vector3 tang = Vector3.Cross(Vector3.up, nh);
                b.vel += nh * spinPushA / mb * (1.5f - b.st.grip * 0.3f) + tang * 1.5f;
                a.vel -= nh * spinPushB / ma * (1.5f - a.st.grip * 0.3f) + tang * 1.5f;
                float pop = Mathf.Min(3.2f, 1f + vn * 0.1f);
                float multA = (a.dashT > 0 ? 1.4f : 1f) * (aAir ? 2.4f * a.st.aerial : 1f) * (a.specialPhase == 2 ? 1.6f : 1f);
                float multB = (b.dashT > 0 ? 1.4f : 1f) * (bAir ? 2.4f * b.st.aerial : 1f) * (b.specialPhase == 2 ? 1.6f : 1f);
                float shareA = sa / (sa + sb + 0.01f);
                float baseD = 1f + Mathf.Min(vn, 25f) * 0.25f;
                float dmgToB = baseD * (0.5f + shareA) * a.st.atk / b.st.def * multA * 0.5f;
                float dmgToA = baseD * (1.5f - shareA) * b.st.atk / a.st.def * multB * 0.5f;
                if (aAir) { b.vel += nh * 9f; b.vel.y = Mathf.Min(b.vel.y, 1f); a.vel.y = 8f; a.vel.x *= 0.4f; a.vel.z *= 0.4f; a.airDashUsed = 0; a.aerialHits++; }
                else if (!bAir) { b.vel.y += pop; b.grounded = false; }
                if (bAir) { a.vel -= nh * 9f; a.vel.y = Mathf.Min(a.vel.y, 1f); b.vel.y = 8f; b.vel.x *= 0.4f; b.vel.z *= 0.4f; b.airDashUsed = 0; b.aerialHits++; }
                else if (!aAir) { a.vel.y += pop; a.grounded = false; }
                Damage(a, b, dmgToB);
                Damage(b, a, dmgToA);
                bool aSp = a.specialPhase == 2, bSp = b.specialPhase == 2;
                if (aSp) SpecialImpact(a, a.pos);
                if (bSp) SpecialImpact(b, b.pos);
                float power = Mathf.Max(dmgToA, dmgToB);
                Vector3 cp = (a.Center + b.Center) * 0.5f;
                Spark(cp, Vector3.up * 0.5f + tang, Mathf.Clamp((int)(power * 2.2f), 10, 70), Mathf.Clamp(power / 10f, 0.7f, 2f));
                a.hitFlash = 0.1f; b.hitFlash = 0.1f;
                bool big = aAir || bAir || power > 7.5f;
                if (big) { Slow(0.12f, aAir || bAir ? 0.45f : 0.28f); shake = Mathf.Max(shake, aAir || bAir ? 0.7f : 0.4f); if (aAir || bAir) Wave(cp, 3f); }
                else shake = Mathf.Max(shake, 0.12f);
                var sp = cam.WorldToViewportPoint(cp);
                Emit("{\"t\":\"e\",\"k\":\"hit\",\"a\":" + i + ",\"b\":" + j + ",\"p\":" + F(power) + ",\"da\":" + F(dmgToA) + ",\"db\":" + F(dmgToB) + ",\"vn\":" + F(vn) + ",\"air\":" + ((aAir || bAir) ? 1 : 0) +
                     ",\"big\":" + (big ? 1 : 0) + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
            }
    }

    // ---------- 演出 ----------
    void Slow(float scale, float dur)
    {
        float until = Time.unscaledTime + dur;
        if (Time.unscaledTime < slowUntil) { slowScale = Mathf.Min(slowScale, scale); slowUntil = Mathf.Max(slowUntil, until); }
        else { slowScale = scale; slowUntil = until; }
    }

    void Spark(Vector3 at, Vector3 bias, int n, float power)
    {
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < n; i++)
        {
            Vector3 dir = (Random.onUnitSphere + bias * 0.8f).normalized;
            ep.position = at;
            ep.velocity = dir * Random.Range(5f, 14f) * power;
            ep.startSize = Random.Range(0.06f, 0.16f) * Mathf.Sqrt(power);
            ep.startLifetime = Random.Range(0.18f, 0.42f);
            float c = Random.value;
            ep.startColor = c < 0.4f ? new Color(1f, 1f, 0.85f) : c < 0.8f ? new Color(1f, 0.75f, 0.2f) : new Color(1f, 0.4f, 0.15f);
            sparks.Emit(ep, 1);
        }
    }

    void Dust(Vector3 at, int n)
    {
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < n; i++)
        {
            Vector2 c = Random.insideUnitCircle.normalized;
            ep.position = at + new Vector3(c.x, 0.1f, c.y) * 0.5f;
            ep.velocity = new Vector3(c.x, 0.3f, c.y) * Random.Range(1.5f, 4f);
            ep.startSize = Random.Range(0.4f, 0.8f);
            ep.startLifetime = Random.Range(0.35f, 0.6f);
            ep.startColor = new Color(0.95f, 0.92f, 1f, 0.55f);
            dust.Emit(ep, 1);
        }
    }

    void Wave(Vector3 at, float size)
    {
        for (int i = 0; i < waves.Count; i++)
        {
            if (waveT[i] >= 0) continue;
            float gh = Arena.H(at.x, at.z);
            waves[i].position = new Vector3(at.x, (gh > -100 ? gh : at.y) + 0.15f, at.z);
            waves[i].localScale = Vector3.one * 0.1f;
            waves[i].gameObject.SetActive(true);
            waveT[i] = 0; waveSize[i] = size;
            return;
        }
    }

    void UpdateWaves(float realDt)
    {
        for (int i = 0; i < waves.Count; i++)
        {
            if (waveT[i] < 0) continue;
            waveT[i] += Time.deltaTime > 0 ? Time.deltaTime : realDt * 0.1f;
            float size = waveSize[i];
            float k = waveT[i] / 0.45f;
            waves[i].localScale = new Vector3(1, 1, 1) * Mathf.Lerp(0.3f, size, 1 - (1 - k) * (1 - k));
            waves[i].GetComponent<MeshRenderer>().material.SetColor("_TintColor", new Color(1f, 0.85f, 0.5f, 1f) * 0.5f * (1 - k));
            if (k >= 1) { waveT[i] = -1; waves[i].gameObject.SetActive(false); }
        }
    }

    Vector3 camVel, camLook;
    void FightCamera(float realDt)
    {
        Vector3 focus = player.alive || phase == "count" ? player.pos : Vector3.zero;
        if (phase == "end" && player.alive) focus = player.pos;
        if (focus.y < -3f) focus.y = -3f;
        Vector3 target = Vector3.Lerp(new Vector3(focus.x, Mathf.Max(focus.y, 0), focus.z), new Vector3(0, 0.6f, 0), 0.32f);
        float zoom = phase == "end" ? 0.8f : 1f;
        Vector3 want = target + new Vector3(0, 9.4f, -8.8f) * zoom;
        camRig.position = Vector3.SmoothDamp(camRig.position, want, ref camVel, 0.18f, 200f, realDt);
        camLook = Vector3.Lerp(camLook, target + Vector3.up * 0.3f, 1 - Mathf.Exp(-10f * realDt));
        Vector3 off = Vector3.zero;
        if (shake > 0)
        {
            off = Random.insideUnitSphere * shake * 0.35f;
            shake = Mathf.Max(0, shake - realDt * 2.2f);
        }
        cam.transform.localPosition = off;
        cam.transform.rotation = Quaternion.LookRotation(camLook - camRig.position);
        float fovWant = Time.unscaledTime < slowUntil ? 44f : 50f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovWant, 1 - Mathf.Exp(-8f * realDt));
    }

    void EmitEv(string kind, Koma k, float p, string extra = null)
    {
        var sp = cam.WorldToViewportPoint(k.pos + Vector3.up * 0.6f);
        Emit("{\"t\":\"e\",\"k\":\"" + kind + "\",\"i\":" + k.idx + ",\"p\":" + F(p) + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) +
             (extra != null ? ",\"why\":\"" + extra + "\"" : "") + "}");
    }

    readonly StringBuilder sbState = new StringBuilder();
    void EmitState(float realDt, bool everyFrame)
    {
        emitT -= realDt;
        if (!everyFrame && emitT > 0) return;
        emitT = 0.1f;
        var sb = sbState; sb.Length = 0;
        sb.Append("{\"t\":\"s\",\"ph\":\"").Append(phase).Append("\",\"tm\":").Append(F(fightT)).Append(",\"sd\":").Append(sudden ? 1 : 0)
          .Append(",\"pz\":").Append(paused ? 1 : 0).Append(",\"ts\":").Append(F(Time.timeScale)).Append(",\"k\":[");
        bool first = true;
        foreach (var k in komas)
        {
            if (!k.root.activeSelf) continue;
            if (!first) sb.Append(','); first = false;
            var sp = cam.WorldToViewportPoint(k.pos + Vector3.up * 1.25f);
            sb.Append("{\"i\":").Append(k.idx).Append(",\"a\":").Append(k.alive ? 1 : 0).Append(",\"sp\":").Append(F(k.SpinRatio))
              .Append(",\"g\":").Append(F(k.gauge / 100f)).Append(",\"cd\":").Append(F(Mathf.Clamp01(k.dashCd / (0.75f * k.st.dashCd))))
              .Append(",\"x\":").Append(F(sp.x)).Append(",\"y\":").Append(F(sp.y)).Append(",\"z\":").Append(F(sp.z))
              .Append(",\"air\":").Append(k.grounded ? 0 : 1).Append(",\"h\":").Append(F(k.pos.y)).Append(",\"wx\":").Append(F(k.pos.x)).Append(",\"wz\":").Append(F(k.pos.z)).Append(",\"r\":").Append(F(new Vector2(k.pos.x, k.pos.z).magnitude))
              .Append(",\"p\":[").Append(k.top).Append(',').Append(k.core).Append(',').Append(k.leg).Append("]")
              .Append(",\"why\":\"").Append(k.koWhy).Append("\",\"L\":[").Append(F(k.lossHit)).Append(',').Append(F(k.lossWall)).Append(',').Append(F(k.lossDash)).Append(',').Append(F(k.spinMax)).Append("]}");
        }
        sb.Append("]}");
        Emit(sb.ToString());
    }
}
