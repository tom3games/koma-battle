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
    int pTop = 0, pCore = 0, pLeg = 0, pSp = 0, nCpu = 2;
    bool autoPlayer, cpuOff, debugState;
    readonly Vector2[] movesBuf = new Vector2[4];
    readonly List<long> keyBuf = new List<long>();
    Vector2 lastAimDir; float lastAimT = -99;
    float testScale = 1f;
    bool boardAnnounced;
    class Fire { public Vector3 pos; public Koma owner; public float t; }
    readonly List<Fire> fires = new List<Fire>();
    ParticleSystem fireP;
    float BoardAt = 40f; const float FireLife = 2.5f;
    bool paused;
    Vector2 touchMove;
    bool touchMode, touchAimOn, stickDash; Vector2 touchAimDir;
    bool touchDash, touchSpecial;
    Vector3 aimPoint;
    Transform reticle; LineRenderer aimLine;
    ParticleSystem sparks, dust;
    readonly List<Transform> waves = new List<Transform>();
    readonly List<float> waveT = new List<float>();
    readonly List<float> waveSize = new List<float>();
    readonly List<Material> waveMats = new List<Material>();
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
        public Vector2 jitter; public float boardT;
    }

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.shadowDistance = 45f;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        mats = new Materials();

        // 空とライト（暗い場内に真上の強い照明。金属が映り込む環境を作る）
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.27f, 0.29f, 0.34f);
        RenderSettings.ambientEquatorColor = new Color(0.17f, 0.17f, 0.19f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = Materials.EnvCube();
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.04f, 0.045f, 0.06f); RenderSettings.fogStartDistance = 45; RenderSettings.fogEndDistance = 150;
        var sky = MeshKit.Obj("Sky", null, MeshKit.SkyDome(180f, new Color(0.02f, 0.025f, 0.035f), new Color(0.07f, 0.075f, 0.09f), new Color(0.02f, 0.02f, 0.025f)), mats.Sky);
        sky.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.9f); sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
        sun.transform.rotation = Quaternion.Euler(72f, -25f, 0);
        var rim = new GameObject("Rim").AddComponent<Light>();
        rim.type = LightType.Directional; rim.color = new Color(0.55f, 0.68f, 0.9f); rim.intensity = 0.35f; rim.renderMode = LightRenderMode.ForceVertex;
        rim.transform.rotation = Quaternion.Euler(18f, 160f, 0);

        // 観客席（暗い段々）と照明塔
        var stands = MeshKit.Obj("Stands", null, MeshKit.Frustum(Arena.Edge + 6f, Arena.Edge + 26f, 14f, 96, false), mats.Stands);
        stands.transform.position = new Vector3(0, -4f, 0);
        stands.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2 / 8;
            var tower = MeshKit.Obj("Tower", null, MeshKit.Box(new Vector3(0.7f, 22f, 0.7f)), mats.Truss);
            tower.transform.position = new Vector3(Mathf.Cos(a) * (Arena.Edge + 14f), 5f, Mathf.Sin(a) * (Arena.Edge + 14f));
            var lamp = MeshKit.Obj("Lamp", null, MeshKit.Box(new Vector3(3.2f, 1.4f, 0.3f)), mats.Emis(new Color(0.2f, 0.2f, 0.2f), new Color(1f, 0.95f, 0.85f) * 1.4f));
            lamp.transform.position = new Vector3(Mathf.Cos(a) * (Arena.Edge + 13.6f), 15.5f, Mathf.Sin(a) * (Arena.Edge + 13.6f));
            lamp.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Cos(a), -0.6f, -Mathf.Sin(a)));
        }

        Arena.BuildVisual(mats);

        camRig = new GameObject("CamRig").transform;
        cam = new GameObject("Cam").AddComponent<Camera>();
        cam.transform.SetParent(camRig, false);
        cam.fieldOfView = 50f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.03f, 0.035f, 0.045f);

        // 狙いの印
        reticle = MeshKit.Obj("Reticle", null, MeshKit.Ring(0.5f, 0.6f, 32), mats.Alpha(new Color(1f, 0.85f, 0.4f, 0.9f), Texture2D.whiteTexture)).transform;
        aimLine = new GameObject("AimLine").AddComponent<LineRenderer>();
        aimLine.material = mats.Alpha(new Color(1f, 0.3f, 0.15f, 0.85f), Texture2D.whiteTexture);
        aimLine.positionCount = 2; aimLine.widthMultiplier = 0.12f;
        aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        sparks = MakeParticles("Sparks", 0.32f, true);
        dust = MakeParticles("Dust", 0.5f, false);
        fireP = MakeParticles("Fire", 0.5f, true);
        { var fm = fireP.main; fm.gravityModifier = -0.25f; fm.maxParticles = 900; var fr = fireP.GetComponent<ParticleSystemRenderer>(); fr.material.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f)); fr.velocityScale = 0.02f; fr.lengthScale = 1f; }

        for (int i = 0; i < 6; i++)
        {
            var w = MeshKit.Obj("Wave", null, MeshKit.Ring(0.85f, 1f, 48), mats.AddGlow(new Color(1f, 0.85f, 0.5f, 1f))).transform;
            w.gameObject.SetActive(false); waves.Add(w); waveT.Add(-1); waveSize.Add(1); waveMats.Add(w.GetComponent<MeshRenderer>().material);
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
                pTop = (int)N(0); pCore = (int)N(1); pLeg = (int)N(2); if (nums.Length >= 4) pSp = (int)N(3);
                if (phase == "title" || phase == "select") { player.Rebuild(pTop, pCore, pLeg, mats); player.sp = pSp; Showcase(); }
                break;
            case "select": phase = "select"; phaseT = 0; Showcase(); break;
            case "title": phase = "title"; phaseT = 0; Showcase(); break;
            case "start":
                if (nums.Length >= 3) { pTop = (int)N(0); pCore = (int)N(1); pLeg = (int)N(2); player.Rebuild(pTop, pCore, pLeg, mats); }
                if (nums.Length >= 4) nCpu = Mathf.Clamp((int)N(3), 1, 3);
                if (nums.Length >= 5) pSp = Mathf.Clamp((int)N(4), 0, 3);
                player.sp = pSp;
                StartMatch();
                break;
            case "auto": autoPlayer = N(0) > 0; break;
            case "cpuoff": cpuOff = N(0) > 0; break;
            case "debugstate": debugState = N(0) > 0; break;
            case "timescale": testScale = Mathf.Clamp(N(0), 0.05f, 8f); break;
            case "boardat": BoardAt = N(0); break;
            case "fightt": fightT = N(0); break;
            case "jump": player.jumpReq = true; break;
            case "super": StartSuper(player); break;
            case "pause": paused = N(0) > 0; break;
            case "move": touchMove = new Vector2(N(0), N(1)); if (touchMove.sqrMagnitude > 1) touchMove.Normalize(); break;
            case "dash": touchDash = true; break;
            case "touchmode": touchMode = N(0) > 0; break;
            // 2026-11-25 本人「車のゲームみたいにマウスで視点変えたい」: 画面に固定したマウスの動きで向きと高さを変える
            case "mlock": mouseLook = N(0) > 0; break;
            case "look":
                if (mouseLook) { camYaw += N(0) * 0.0032f; camElev = Mathf.Clamp(camElev + N(1) * 0.0026f, 0.1f, 0.95f); }
                break;
            // スマホ: 画面のなぞりで視点を回す。なぞっている間と直後は、進む向きへの自動の追従を止める
            case "tlook": camYaw += N(0) * 0.0075f; camElev = Mathf.Clamp(camElev + N(1) * 0.005f, 0.1f, 0.95f); touchLookT = 1.6f; break;
            case "aimdir": touchAimOn = true; touchAimDir = new Vector2(N(0), N(1)); break;
            case "aimoff": touchAimOn = false; break;
            case "dashdir":
                {
                    touchAimOn = false;
                    Vector2 d = new Vector2(N(0), N(1));
                    Vector3 aim;
                    if (d.sqrMagnitude < 0.01f) { var t = Nearest(player); aim = t != null ? t.pos : player.pos + Vector3.forward; }
                    else { var cd = CamRel(d.normalized); aim = player.pos + new Vector3(cd.x, 0, cd.y) * 7f; stickDash = true; }   // 画面の向き → 地面の向き（矢印と同じ）
                    if (phase == "fight" && player.alive) TryDash(player, aim);
                    stickDash = false;
                    break;
                }
            case "specialdir":
                {
                    Vector2 d = new Vector2(N(0), N(1));
                    if (phase == "fight" && player.alive) { var cd = CamRel(d.normalized); TrySpecial(player, player.pos + new Vector3(cd.x, 0, cd.y) * 8f); }
                    break;
                }
            case "specialnear":
                {
                    var t = Nearest(player);
                    if (phase == "fight" && player.alive) TrySpecial(player, t != null ? t.pos : player.pos);
                    break;
                }
            case "quality":
                if (N(0) < 1)
                {
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowDistance = 28f; QualitySettings.shadowCascades = 1;
                    var sunL = GameObject.Find("Sun"); if (sunL) sunL.GetComponent<Light>().shadows = LightShadows.Hard;
                }
                break;
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
        player.grounded = true; player.SetArrow(Vector2.zero);
        sudden = false; Arena.ResetCollapse(); boardAnnounced = false; fires.Clear();
    }

    void StartMatch()
    {
        phase = "count"; phaseT = 0; fightT = 0; sudden = false; endWin = -1; endT = 0;
        touchMove = Vector2.zero; touchAimOn = false; touchDash = false; touchSpecial = false;
        camInit = true; Arena.ResetCollapse(); boardAnnounced = false; fires.Clear();
        pairCd.Clear();
        int n = 1 + nCpu;
        for (int i = 0; i < 4; i++)
        {
            var k = komas[i];
            if (i >= n) { k.root.SetActive(false); k.alive = false; continue; }
            if (i > 0) { k.Rebuild(rng.Next(Parts.Count), rng.Next(Parts.Count), rng.Next(Parts.Count), mats); k.sp = rng.Next(4); }
            float ang = -Mathf.PI / 2 + Mathf.PI / 8 + i * Mathf.PI * 2 / n;
            float r = 11f;
            float x = Mathf.Cos(ang) * r, z = Mathf.Sin(ang) * r;
            k.Place(new Vector3(x, Arena.H(x, z) + 3.5f, z));
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
            if (phase == "title") look -= Vector3.Cross(Vector3.up, (look - camRig.position).normalized) * 7f * Mathf.Clamp01(cam.aspect - 0.6f);
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
        // 周回加速板: 40秒で外周が光る板になる（直前の3秒は点滅で知らせる）
        if (phase == "fight")
        {
            bool bon = fightT >= BoardAt;
            Arena.SetBoard(bon, fightT, fightT > BoardAt - 3f && !bon ? 1f : 0f);
            if (bon && !boardAnnounced && !Arena.Collapsing) { boardAnnounced = true; Emit("{\"t\":\"e\",\"k\":\"board\"}"); }
        }
        UpdateFires(dt);
        // 最終局面: 100秒たつか、4人の試合で50秒を過ぎて残り2人になったら外から床が崩れる
        if (phase == "fight" && !sudden)
        {
            int act = 0, alv = 0; foreach (var k in komas) if (k.root.activeSelf) { act++; if (k.alive) alv++; }
            if (fightT > 100f || (act >= 4 && alv <= 2 && fightT > 50f)) { sudden = true; Arena.StartCollapse(); Emit("{\"t\":\"e\",\"k\":\"sudden\"}"); }
        }
        if (phase == "fight" || phase == "end")
        {
            if (Arena.UpdateCollapse(dt)) { shake = Mathf.Max(shake, 0.55f); fovKick = Mathf.Max(fovKick, 5f); Emit("{\"t\":\"e\",\"k\":\"collapse\"}"); }
        }

        // 入力
        Vector2 move = Vector2.zero; bool dash = false, special = false;
        AimUpdate();
        if (phase == "fight" && player.alive)
        {
            sprint = false;
            if (autoPlayer) Think(player, dt, out move, out dash, out special);
            else
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
                sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                // シフトを0.3秒以内に2回: 一時的な超加速
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                {
                    if (Time.unscaledTime - player.lastShiftT < 0.3f) { StartSuper(player); player.lastShiftT = -9; }
                    else player.lastShiftT = Time.unscaledTime;
                }
                if (Input.GetKeyDown(KeyCode.Space)) player.jumpReq = true;
                if (touchMove.sqrMagnitude > 0.01f) move = touchMove;
                if (move.sqrMagnitude > 1) move.Normalize();
                move = CamRel(move);
                dash = (!touchMode && Input.GetMouseButtonDown(0)) || touchDash;
                special = (!touchMode && Input.GetMouseButtonDown(1)) || Input.GetKeyDown(KeyCode.E) || touchSpecial;
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
        var moves = movesBuf; for (int mi = 0; mi < 4; mi++) moves[mi] = Vector2.zero;
        moves[0] = move;
        for (int i = 1; i < komas.Count; i++)
        {
            var k = komas[i];
            if (!k.alive || !k.root.activeSelf || phase != "fight" || cpuOff) continue;
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
            if (k.boostCd > 0) k.boostCd -= dt;
            if (k.superT > 0) k.superT -= dt;
            if (k.superCd > 0) k.superCd -= dt;
            if (k.boardT > 0) k.boardT -= dt;
            if (k.fireCd > 0) k.fireCd -= dt;
            if (k.burnSnd > 0) k.burnSnd -= dt;
            k.jumpReq = false;
            UpdateSpecial(k, dt);
            if (k.alive && phase == "fight")
            {
                k.spin -= (0.75f * k.st.decay + (sudden ? 1.2f : 0f)) * dt;
                k.gauge = Mathf.Min(100f, k.gauge + 2.2f * k.st.gaugeRate * dt);
                if (k.spin <= 0) { k.spin = 0; KO(k, "spin"); }
                // 奇跡: 回転数が上限近くを保つと、数秒だけ通り道に炎が残る（本人は無傷）
                if (k.alive)
                {
                    k.hiT = k.SpinRatio >= 0.97f ? k.hiT + dt : 0f;
                    if (k.fireT <= 0 && k.fireCd <= 0 && k.hiT >= 2f && fightT > 10f)
                    {
                        k.fireT = 4f; k.fireCd = 14f; k.hiT = 0; k.lastFire = new Vector3(0, -999, 0);
                        EmitEv("miracle", k, 0); Spark(k.Center, Vector3.up, 40, 1.3f);
                    }
                    if (k.fireT > 0)
                    {
                        k.fireT -= dt;
                        if (k.grounded && (k.pos - k.lastFire).sqrMagnitude > 0.8f)
                        {
                            k.lastFire = k.pos; fires.Add(new Fire { pos = k.pos, owner = k, t = FireLife });
                            FireBurst(k.pos, 6);
                        }
                    }
                }
            }
            k.Visual(dt, realDt);
        }
        keyBuf.Clear(); keyBuf.AddRange(pairCd.Keys);
        foreach (var key in keyBuf) { pairCd[key] -= dt; if (pairCd[key] <= 0) pairCd.Remove(key); }

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
        if (touchAimOn) { var ad = CamRel(touchAimDir); aimPoint = player.pos + new Vector3(ad.x, 0, ad.y) * 6.5f; }
        else if (!autoPlayer && mouseLook && ChaseOn) aimPoint = player.pos + new Vector3(Mathf.Sin(camYaw), 0, Mathf.Cos(camYaw)) * 6.5f;
        else if (!autoPlayer && !touchOnly && !touchMode && plane.Raycast(ray, out float d)) aimPoint = ray.GetPoint(d);
        if (autoPlayer) aimPoint = AimFor(player);
        // 2026-11-25 本人「スマホでやってる時コマから赤い線出てるの何」: 線をやめ、足元の短い山形の矢印（ダッシュの向き）にした。
        // スマホは右の指で狙っている間だけ。PC はマウスの位置に小さな印＋足元の矢印
        bool fight = phase == "fight" && player.alive && !autoPlayer;
        bool show = fight && (touchMode ? touchAimOn : true);
        aimLine.enabled = false;
        reticle.gameObject.SetActive(fight && !touchMode);
        Vector3 dir = aimPoint - player.pos; dir.y = 0;
        player.SetArrow(show && dir.sqrMagnitude > 0.04f ? new Vector2(dir.x, dir.z).normalized : Vector2.zero);
        if (!fight || touchMode) return;
        float gh = Arena.H(aimPoint.x, aimPoint.z);
        reticle.position = new Vector3(aimPoint.x, (gh > -100 ? gh : aimPoint.y) + 0.06f, aimPoint.z);
        reticle.localScale = Vector3.one * (player.dashCd > 0 ? 0.55f : 0.8f);
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
            b.retarget = 1.5f + (float)rng.NextDouble() * 1.5f;
            // 近くて回転の少ない相手をねらう
            float best = 1e9f; b.target = -1;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                float sc = (o.pos - k.pos).magnitude + o.SpinRatio * 6f + (float)rng.NextDouble() * 3f;
                if (sc < best) { best = sc; b.target = o.idx; }
            }
            b.boardT = Arena.BoardOn && !Arena.Collapsing && rng.NextDouble() < 0.15 ? 1.5f + (float)rng.NextDouble() * 1.5f : 0f;
            b.padGoal = rng.NextDouble() < 0.12 ? rng.Next(Arena.Pads.Length + Arena.Mounds.Length) : -1;
            b.jitter = new Vector2((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 2f;
        }
        if (b.target < 0) return;
        var t = komas[b.target];
        Vector3 to = t.pos - k.pos; to.y = 0;
        float dist = to.magnitude;
        Vector2 want = new Vector2(to.x, to.z).normalized + b.jitter * 0.15f;
        b.boardT -= dt;
        if (Arena.Collapsing) b.boardT = 0;
        float rk = new Vector2(k.pos.x, k.pos.z).magnitude;
        bool riding = b.boardT > 0 && dist > 7f;
        if (riding) want = rk < Arena.BoardRc ? new Vector2(k.pos.x, k.pos.z).normalized : Vector2.zero;
        if (!riding && b.padGoal >= 0 && dist > 6f)
        {
            var p = b.padGoal < Arena.Pads.Length ? Arena.Pads[(int)b.padGoal] : Arena.Mounds[(int)b.padGoal - Arena.Pads.Length];
            want = (p - new Vector2(k.pos.x, k.pos.z)).normalized;
            if ((p - new Vector2(k.pos.x, k.pos.z)).magnitude < 0.6f) b.padGoal = -1;
        }
        // 端に寄りすぎたら内へ
        float r = new Vector2(k.pos.x, k.pos.z).magnitude;
        float lim = Mathf.Min(Arena.R, Arena.SafeR) - 3.5f;
        if (!riding && r > lim) want += -new Vector2(k.pos.x, k.pos.z).normalized * (r - lim) * 1.2f;
        move = want.sqrMagnitude > 1 ? want.normalized : want;
        // 2026-11-25「白熱」: 遠くからでも突っ込む・間を短く
        if (k.grounded && k.dashCd <= 0 && dist < 10f && b.react <= 0)
        {
            dash = true; b.react = 0.5f + (float)rng.NextDouble() * 0.7f;
        }
        // 空中で相手の上近くなら急降下
        if (!k.grounded && k.airDashUsed < 1 && dist < 6f && k.pos.y > t.pos.y + 1f && k.vel.y < 3f) dash = true;
        if (k.gauge >= 100f && dist < 9f && b.think <= 0) { special = true; }
        if (b.think <= 0) b.think = 0.3f;
    }

    // 左クリックの連打で出し続けられないよう、ダッシュと急降下で同じ待ち時間を使う
    const float DashCdBase = 1.3f;

    void TryDash(Koma k, Vector3 aim)
    {
        if (!k.alive || k.specialPhase > 0 || k.dashCd > 0) return;
        Vector3 dir = aim - k.pos;
        if (k.grounded)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = new Vector3(k.vel.x, 0, k.vel.z);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            float sp = 19f * k.st.speed;
            float keep = stickDash ? 0f : 0.25f;   // スマホは倒した向きへまっすぐ（前の勢いを混ぜない）
            k.vel = new Vector3(dir.x * sp + k.vel.x * keep, k.vel.y, dir.z * sp + k.vel.z * keep);
            k.lockT = stickDash ? 0.2f : 0f;
            k.dashT = 0.32f; k.dashCd = DashCdBase * k.st.dashCd;
            k.spin -= 1.2f; k.dashes++; k.lossDash += 1.2f;
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
            k.diveTarget = -1; float bd = 25f;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                Vector3 dd = o.pos - aim; dd.y = 0;
                // スマホで向きを決めた時は、その向き（左右30度）にいる相手だけ追う
                if (stickDash) { Vector3 tf = o.pos - k.pos, af = aim - k.pos; tf.y = 0; af.y = 0; if (Vector3.Angle(tf, af) > 30f) continue; }
                if (dd.sqrMagnitude < bd) { bd = dd.sqrMagnitude; k.diveTarget = o.idx; }
            }
            k.dashT = 0.5f; k.dashCd = DashCdBase * k.st.dashCd; k.spin -= 1.4f; k.dashes++; k.lossDash += 1.4f;
            EmitEv("dive", k, 0);
        }
    }

    void TrySpecial(Koma k, Vector3 aim)
    {
        if (!k.alive || k.gauge < 100f || k.specialPhase > 0 || k.tornadoT > 0 || k.shieldT > 0) return;
        k.gauge = 0; k.specials++;
        if (k.sp == 1)
        {
            // テンペスト・ヴォルテックス: 竜巻で周りを引き寄せて巻き上げ、最後に吹き飛ばす
            k.tornadoT = 3.5f; k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.15f);
            EmitEv("special", k, 1); shake = Mathf.Max(shake, 0.2f);
            return;
        }
        if (k.sp == 2)
        {
            // ファントム・レイド: 近い相手へ3連続の突進
            k.specialPhase = 20; k.rushLeft = 3; k.specialT = 0f;
            EmitEv("special", k, 2);
            return;
        }
        if (k.sp == 3)
        {
            // イージス・リバース: 3秒の盾（受ける回転減りと弾かれを抑え、ぶつかった相手を弾き返す）＋回転を大きく戻す
            k.shieldT = 3.2f; k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.35f);
            EmitEv("special", k, 3);
            return;
        }
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
        shake = Mathf.Max(shake, 0.25f);
    }

    // 必殺技の続き（毎フレーム）
    void UpdateSpecial(Koma k, float dt)
    {
        if (!k.alive) { k.tornadoT = 0; k.shieldT = 0; if (k.specialPhase == 20) k.specialPhase = 0; return; }
        if (k.shieldT > 0) k.shieldT -= dt;
        if (k.tornadoT > 0)
        {
            float before = k.tornadoT;
            k.tornadoT -= dt;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                Vector3 d = k.pos - o.pos; d.y = 0;
                float dist = d.magnitude;
                if (dist > 8.5f || dist < 0.01f) continue;
                Vector3 n = d / dist, tan = Vector3.Cross(Vector3.up, n);
                float f = 1f - dist / 8.5f;
                float res = o.shieldT > 0 ? 0.25f : 1f;
                o.vel += (n * 20f * f + tan * 5f * f * Mathf.Clamp01(dist / 4f)) * dt * res;
                if (dist < 3.4f) { o.vel.y = Mathf.Max(o.vel.y, 4.5f * res); o.grounded = false; }
                Damage(k, o, 6.5f * f * k.st.atk / o.st.def * dt);
                if (k.tornadoT <= 0 && before > 0 && dist < 7.5f)
                {
                    o.vel += -n * 15f * res; o.vel.y = Mathf.Max(o.vel.y, 6f * res); o.grounded = false;
                    Damage(k, o, 9f * k.st.atk / o.st.def);
                }
            }
            if (k.tornadoT <= 0) { Wave(k.pos, 6f); shake = Mathf.Max(shake, 0.3f); var sp = cam.WorldToViewportPoint(k.pos); Emit("{\"t\":\"e\",\"k\":\"boom\",\"i\":" + k.idx + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}"); }
            if (Random.value < 0.5f) Dust(k.pos + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f)), 1);
        }
        if (k.specialPhase == 20)
        {
            k.specialT -= dt;
            if (k.specialT <= 0)
            {
                var t = Nearest(k);
                if (k.rushLeft <= 0 || t == null) { k.specialPhase = 0; return; }
                Vector3 dir = t.pos + t.vel * 0.12f - k.pos; dir.y = 0; dir.Normalize();
                k.vel = new Vector3(dir.x * 27f, k.grounded ? 1.5f : k.vel.y, dir.z * 27f);
                k.dashT = 0.36f; k.rushLeft--; k.specialT = 0.42f; k.dashes++;
                EmitEv("dash", k, 2);
            }
        }
    }

    void StartSuper(Koma k)
    {
        if (!k.alive || phase != "fight" || k.superCd > 0 || k.superT > 0) return;
        k.superT = 1f; k.superCd = 6f; k.dashes++;
        fovKick = Mathf.Max(fovKick, 7f);
        Spark(k.Center, Vector3.up * 0.3f, 24, 1.1f);
        EmitEv("super", k, 0);
    }

    void FireBurst(Vector3 at, int n)
    {
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < n; i++)
        {
            Vector2 c = Random.insideUnitCircle * 0.75f;
            ep.position = at + new Vector3(c.x, 0.15f, c.y);
            ep.velocity = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(2.5f, 4.5f), Random.Range(-0.6f, 0.6f));
            ep.startSize = Random.Range(0.8f, 1.3f);
            ep.startLifetime = Random.Range(0.35f, 0.6f);
            float q = Random.value;
            ep.startColor = q < 0.35f ? new Color(1f, 0.85f, 0.35f) : q < 0.75f ? new Color(1f, 0.5f, 0.12f) : new Color(0.95f, 0.25f, 0.08f);
            fireP.Emit(ep, 1);
        }
    }

    void UpdateFires(float dt)
    {
        for (int i = fires.Count - 1; i >= 0; i--)
        {
            var f = fires[i];
            f.t -= dt;
            if (f.t <= 0 || phase == "end") { fires.RemoveAt(i); continue; }
            if (Random.value < 0.5f * Mathf.Min(1f, f.t / 0.8f)) FireBurst(f.pos, 1);
            if (phase != "fight") continue;
            foreach (var o in komas)
            {
                if (o == f.owner || !o.alive || !o.root.activeSelf) continue;
                float dx = o.pos.x - f.pos.x, dz = o.pos.z - f.pos.z;
                if (dx * dx + dz * dz > 1.3f * 1.3f || o.pos.y - f.pos.y > 1.2f) continue;
                Damage(f.owner, o, 12f * dt);
                if (o.burnSnd <= 0) { o.burnSnd = 0.45f; o.hitFlash = 0.08f; EmitEv("burn", o, 0); }
            }
        }
    }

    void KO(Koma k, string why)
    {
        if (!k.alive || phase == "end") return;
        k.Knock(why);
        k.koTime = fightT;
        EmitEv("ko", k, 0, why + "\",\"r\":\"" + F(new Vector2(k.pos.x, k.pos.z).magnitude) + "\",\"vy\":\"" + F(k.vel.y) + "\",\"vh\":\"" + F(new Vector2(k.vel.x, k.vel.z).magnitude) + "\",\"ang\":\"" + F(Mathf.Atan2(k.pos.z, k.pos.x) * Mathf.Rad2Deg) + "\",\"lastHit\":\"" + F(fightT - k.lastHitT));
        shake = Mathf.Max(shake, 0.4f);
    }

    // ---------- 物理 ----------
    const float G = 28f, StepUp = 0.35f;

    void Step(Koma k, float dt, Vector2 move)
    {
        Vector3 p = k.pos, v = k.vel;
        bool ctrl = k.alive && phase == "fight" && k.specialPhase == 0;
        k.lockT -= dt;
        // スペース: 小さく跳ぶ。高さは足の跳躍と重さで変わる（重いほど低い）
        if (k.jumpReq && ctrl && k.grounded)
        {
            v.y = 7.2f * k.st.jump * Mathf.Pow(k.st.mass, -0.4f);
            k.grounded = false; k.jumpReq = false; k.lastVy = 0;
            Dust(p, 5); EmitEv("jump", k, 2);
        }
        if (ctrl && k.lockT <= 0)
        {
            // 2026-11-25 本人「シフトボタン押しながらの移動で少し早くなるように」: Shift で加速と上限を少し上げる
            float acc = 26f * k.st.speed * (k.grounded ? 1f : 0.3f * k.st.air) * (k.superT > 0 ? 4.5f : (k.isPlayer && sprint ? 2.2f : 1f));   // 2026-11-26 本人「シフトの加速度をあげて」1.3→2.2
            Vector2 mv = move;
            if (k.superT > 0 && mv.sqrMagnitude < 0.04f) { var hv0 = new Vector2(v.x, v.z); if (hv0.magnitude > 1f) mv = hv0.normalized; }
            v.x += mv.x * acc * dt; v.z += mv.y * acc * dt;
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
            // 周回加速板: 回る向き（反時計回り）に強く押され、帯に沿って1周流れる。回転数が少しだけ戻る
            if (k.alive && phase == "fight" && Arena.OnBoard(p.x, p.z))
            {
                float rr = Mathf.Max(0.1f, Mathf.Sqrt(p.x * p.x + p.z * p.z));
                Vector2 rad = new Vector2(p.x, p.z) / rr, tan = new Vector2(-rad.y, rad.x);
                float vr = v.x * rad.x + v.z * rad.y, vt = v.x * tan.x + v.z * tan.y;
                if (vt < 21f) vt = Mathf.Min(21f, vt + 48f * dt);
                vr += ((Arena.BoardRc - rr) * 3f - vr) * (1f - Mathf.Exp(-8f * dt));
                v.x = rad.x * vr + tan.x * vt; v.z = rad.y * vr + tan.y * vt;
                if (k.boardT <= 0) EmitEv("boost", k, vt);
                k.boardT = 0.8f;
                k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.01f * dt);
            }
            float maxS = 9.5f * k.st.speed * (k.superT > 0 ? 2.3f : (k.isPlayer && sprint ? 1.35f : 1f)), hs = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (hs > maxS && k.dashT <= 0 && k.boostCd < 0.35f && k.boardT <= 0)
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
        float hsNow = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        // 切れ目の低い縁は、弾かれて速い時だけ越えられる（自分のダッシュや加速では落ちない）
        float stepAllow = (hsNow > 8f && k.dashT <= 0 && k.boostCd < 0.35f && Arena.GapFactor(Mathf.Atan2(np.z, np.x)) > 0.5f && new Vector2(np.x, np.z).magnitude > Arena.R) ? 0.75f : StepUp;
        if (hw > -100 && hw - Mathf.Max(p.y, np.y) > stepAllow && hw - p.y > stepAllow)
        {
            // 壁の向き: 1刻みの中点で幅を広く取った勾配から。逆向きなら、来た方向へ返す
            float mx = (p.x + np.x) * 0.5f, mz = (p.z + np.z) * 0.5f, e = 0.3f;
            Vector2 gr = new Vector2(Arena.WallH(mx + e, mz) - Arena.WallH(mx - e, mz), Arena.WallH(mx, mz + e) - Arena.WallH(mx, mz - e));
            Vector2 back = new Vector2(p.x - np.x, p.z - np.z);
            Vector2 n = gr.sqrMagnitude > 1e-4f ? -gr.normalized : back.normalized;
            if (back.sqrMagnitude > 1e-8f && Vector2.Dot(n, back) < 0) n = back.normalized;
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

        if (k.grounded && k.specialPhase == 2) { SpecialImpact(k, new Vector3(np.x, h > -100 ? h : np.y, np.z)); v = new Vector3(v.x * 0.2f, 6f, v.z * 0.2f); k.grounded = false; }
        else if (k.grounded && k.specialPhase == 1) k.grounded = false;
        if (k.grounded)
        {
            if (h < -100 || h < np.y - 0.3f)
            {
                k.grounded = false;
            }
            else
            {
                np.y = h;
                Vector2 gr = Vector2.ClampMagnitude(Arena.Grad(np.x, np.z), 1.2f);
                float newVy = gr.x * v.x + gr.y * v.z;
                float hsp = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                // ジャンプ台（円い山）: 登ってきて頂上を越えた瞬間に大きく跳ぶ。どの向きから来ても同じ
                bool onMound = Arena.MoundAt(np.x, np.z) >= 0;
                if (k.alive && onMound && k.lastVy > 1.1f && newVy < 0.3f && hsp > 6.5f)
                {
                    v.y = Mathf.Min(13.5f, (6.5f + hsp * 0.38f) * k.st.jump);
                    Vector2 hv = Vector2.ClampMagnitude(new Vector2(v.x, v.z) * 0.85f, 13f);
                    v.x = hv.x; v.z = hv.y;
                    k.grounded = false; k.lastVy = 0;
                    EmitEv("jump", k, 1);
                }
                else
                {
                    v.y = newVy; k.lastVy = onMound ? Mathf.Max(k.lastVy, newVy) : 0f;
                    // 加速板: 踏んだ向きへ一気に加速（止める演出はしない）
                    int bi = Arena.BoostAt(np.x, np.z);
                    Vector2 toM = bi >= 0 ? (Arena.Mounds[bi] - new Vector2(np.x, np.z)).normalized : Vector2.zero;
                    if (k.alive && k.boostCd <= 0 && bi >= 0 && (hsp < 1f || Vector2.Dot(new Vector2(v.x, v.z) / Mathf.Max(hsp, 0.01f), toM) > 0.25f))
                    {
                        Vector2 dir = hsp > 1f ? new Vector2(v.x, v.z) / hsp : (Arena.Mounds[Arena.BoostAt(np.x, np.z)] - new Vector2(np.x, np.z)).normalized;
                        float ns = Mathf.Max(hsp + 9f, 20f) * Mathf.Lerp(1f, k.st.speed, 0.5f);
                        v.x = dir.x * ns; v.z = dir.y * ns;
                        k.boostCd = 0.9f;
                        EmitEv("boost", k, ns);
                    }
                }
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

        if (k.alive && (np.y < -5f || (new Vector2(np.x, np.z).magnitude > Arena.Edge + 0.4f && np.y < Arena.Bowl(Arena.R)))) KO(k, "out");
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

    const float HitScale = 0.36f;   // 当たりで減る量。試合の長さはここで決まる
    void Damage(Koma from, Koma to, float dmg)
    {
        if (phase == "end") return;
        dmg *= HitScale; if (to.shieldT > 0) dmg *= 0.2f;
        to.spin -= dmg; to.lossHit += dmg; to.lastHitT = fightT;
        if (from.st.steal > 0 && from.alive) from.spin = Mathf.Min(from.spinMax, from.spin + dmg * from.st.steal);
        from.dealt += dmg;
        from.gauge = Mathf.Min(100f, from.gauge + dmg * 1.5f * from.st.gaugeRate);
        to.gauge = Mathf.Min(100f, to.gauge + dmg * 1.1f * to.st.gaugeRate);
        if (to.spin <= 0) { to.spin = 0; KO(to, "spin"); }
    }

    // 当たりの角度の倍率。dir は攻める側から受ける側への向き
    // どれだけ真っすぐ入ったか（かすり 0.6 〜 芯 1.25）× 受ける側の向き（正面 1 / 横 1.2 / 背後 1.45）
    // kind: 0 ふつう 1 かすり 2 芯 3 横 4 背後
    float AngleFactor(Vector3 av, Vector3 bv, Vector3 dir, out int kind)
    {
        kind = 0;
        av.y = 0; bv.y = 0; float sp = av.magnitude;
        if (sp < 3f) return 1f;
        float str = Vector3.Dot(av / sp, dir);
        if (str <= 0.1f) return 1f;   // 攻めていない（当てられた側）
        float ang = Mathf.Acos(Mathf.Clamp01(str)) * Mathf.Rad2Deg;   // 0 = 芯
        float f = ang <= 12f ? 1.25f : ang <= 50f ? Mathf.Lerp(1.25f, 0.75f, (ang - 12f) / 38f) : 0.6f;
        if (ang > 50f) { kind = 1; return f; }
        if (bv.magnitude > 2.5f)
        {
            float face = Vector3.Dot(bv.normalized, -dir);   // 1 = 向かい合い / -1 = 背中
            if (face < -0.5f) { kind = 4; return f * 1.45f; }
            if (face < 0.35f) { kind = 3; return f * 1.2f; }
        }
        if (ang <= 12f) kind = 2;
        return f;
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
                // 押し出し: 押した先が壁（段差・縁・柵）の中なら、その側は動かさない
                Vector3 pa = a.pos - nh * pen * mb / (ma + mb), pb = b.pos + nh * pen * ma / (ma + mb);
                if (Arena.WallH(pa.x, pa.z) - a.pos.y <= StepUp) a.pos = new Vector3(pa.x, a.pos.y, pa.z);
                if (Arena.WallH(pb.x, pb.z) - b.pos.y <= StepUp) b.pos = new Vector3(pb.x, b.pos.y, pb.z);
                long key = i * 10 + j;
                Vector3 rel = a.vel - b.vel;
                float vn = Vector3.Dot(rel, n);
                if (vn <= 0.5f || pairCd.ContainsKey(key)) continue;
                pairCd[key] = 0.25f;

                // 空中から上に乗る突撃
                bool aAir = !a.grounded && a.Center.y > b.Center.y + 0.55f && a.vel.y < -2f;
                bool bAir = !b.grounded && b.Center.y > a.Center.y + 0.55f && b.vel.y < -2f;
                Vector3 aV0 = a.vel, bV0 = b.vel;
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
                float multA = (a.dashT > 0 ? 1.4f : 1f) * (aAir ? 2.4f * a.st.aerial : 1f) * (a.specialPhase == 2 ? 1.6f : 1f) * (a.specialPhase == 20 ? 1.35f : 1f);
                float multB = (b.dashT > 0 ? 1.4f : 1f) * (bAir ? 2.4f * b.st.aerial : 1f) * (b.specialPhase == 2 ? 1.6f : 1f) * (b.specialPhase == 20 ? 1.35f : 1f);
                float shareA = sa / (sa + sb + 0.01f);
                float baseD = 1f + Mathf.Min(vn, 25f) * 0.25f;
                float dmgToB = baseD * (0.5f + shareA) * a.st.atk / b.st.def * multA * 0.5f;
                float dmgToA = baseD * (1.5f - shareA) * b.st.atk / a.st.def * multB * 0.5f;
                // 2026-11-26 本人「攻撃の入った角度とかも計算できるように」: 芯・かすり × 正面・横・背後。空中からの突撃と必殺の落下は角度を見ない
                int angA = 0, angB = 0;
                float facA = aAir || a.specialPhase == 2 ? 1f : AngleFactor(aV0, bV0, nh, out angA);
                float facB = bAir || b.specialPhase == 2 ? 1f : AngleFactor(bV0, aV0, -nh, out angB);
                dmgToB *= facA; dmgToA *= facB;
                // 芯・背後は大きく弾く。かすりは弾きが弱い
                b.vel += nh * (facA - 1f) * 2.5f / mb; a.vel -= nh * (facB - 1f) * 2.5f / ma;
                // 板で出した速さのまま当てると大きく弾く
                { float hA = Mathf.Sqrt(aV0.x * aV0.x + aV0.z * aV0.z), hB = Mathf.Sqrt(bV0.x * bV0.x + bV0.z * bV0.z);
                  if (a.boardT > 0 && hA > 13f && Vector3.Dot(aV0, nh) > 0.3f * hA) b.vel += nh * Mathf.Min(hA, 26f) * 0.55f / mb;
                  if (b.boardT > 0 && hB > 13f && Vector3.Dot(bV0, -nh) > 0.3f * hB) a.vel -= nh * Mathf.Min(hB, 26f) * 0.55f / ma; }
                if (aAir) { b.vel += nh * 9f; b.vel.y = Mathf.Min(b.vel.y, 1f); a.vel.y = 8f; a.vel.x *= 0.4f; a.vel.z *= 0.4f; a.airDashUsed = 1; a.aerialHits++; }
                else if (!bAir) { b.vel.y += pop; b.grounded = false; }
                if (bAir) { a.vel -= nh * 9f; a.vel.y = Mathf.Min(a.vel.y, 1f); b.vel.y = 8f; b.vel.x *= 0.4f; b.vel.z *= 0.4f; b.airDashUsed = 1; b.aerialHits++; }
                else if (!aAir) { a.vel.y += pop; a.grounded = false; }
                // 盾: 自分はほとんど弾かれず、相手を弾き返す
                if (a.shieldT > 0) { a.vel = Vector3.Lerp(aV0, a.vel, 0.2f); b.vel += nh * 9f; }
                if (b.shieldT > 0) { b.vel = Vector3.Lerp(bV0, b.vel, 0.2f); a.vel -= nh * 9f; }
                Damage(a, b, dmgToB);
                Damage(b, a, dmgToA);
                bool aSp = a.specialPhase == 2, bSp = b.specialPhase == 2;
                if (aSp) SpecialImpact(a, a.pos);
                if (bSp) SpecialImpact(b, b.pos);
                float power = Mathf.Max(dmgToA, dmgToB);
                Vector3 cp = (a.Center + b.Center) * 0.5f;
                // 2026-11-25「当たりの手応え」: 速くぶつかるほど火花が多く大きく、画面も強く揺れ、カメラが一瞬寄る
                Spark(cp, Vector3.up * 0.5f + tang, Mathf.Clamp((int)(power * 3.5f + vn * 2f), 16, 130), Mathf.Clamp(power / 7f + vn * 0.03f, 0.9f, 2.6f));
                bool meHit = a.isPlayer || b.isPlayer;
                fovKick = Mathf.Max(fovKick, Mathf.Min(vn * 0.3f, meHit ? 7f : 3.5f));
                a.hitFlash = 0.1f; b.hitFlash = 0.1f;
                bool big = aAir || bAir || power > 7.5f;
                if (big) { Slow(0.12f, aAir || bAir ? 0.45f : 0.28f); shake = Mathf.Max(shake, aAir || bAir ? 0.7f : 0.4f); if (aAir || bAir) Wave(cp, 3f); }
                else shake = Mathf.Max(shake, 0.12f + Mathf.Min(vn, 25f) * (meHit ? 0.014f : 0.006f));
                var sp = cam.WorldToViewportPoint(cp);
                Emit("{\"t\":\"e\",\"k\":\"hit\",\"a\":" + i + ",\"b\":" + j + ",\"p\":" + F(power) + ",\"da\":" + F(dmgToA) + ",\"db\":" + F(dmgToB) + ",\"vn\":" + F(vn) + ",\"ka\":" + angA + ",\"kb\":" + angB + ",\"fa\":" + F(facA) + ",\"fb\":" + F(facB) + ",\"air\":" + ((aAir || bAir) ? 1 : 0) +
                     ",\"big\":" + (big ? 1 : 0) + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
            }
    }

    // ---------- 演出 ----------
    // 2026-11-25 本人「いちいち技のたびに画面止まるのやめて」: スロー・ヒットストップは使わない（呼び出しは残して何もしない）
    void Slow(float scale, float dur)
    {
        return;
#pragma warning disable CS0162
        float until = Time.unscaledTime + dur;
        if (Time.unscaledTime < slowUntil) { slowScale = Mathf.Min(slowScale, scale); slowUntil = Mathf.Max(slowUntil, until); }
        else { slowScale = scale; slowUntil = until; }
#pragma warning restore CS0162
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
            waveMats[i].SetColor("_TintColor", new Color(1f, 0.85f, 0.5f, 1f) * 0.5f * (1 - k));
            if (k >= 1) { waveT[i] = -1; waves[i].gameObject.SetActive(false); }
        }
    }

    Vector3 camVel, camLook;
    float camYaw, camElev = 0.43f, fovKick, camFov = 60f; bool camInit, mouseLook, sprint; float touchLookT;
    // 操作の向きをカメラ基準にする（上＝カメラの向いている前）
    Vector2 CamRel(Vector2 v)
    {
        if (!ChaseOn) return v;
        float s = Mathf.Sin(camYaw), c = Mathf.Cos(camYaw);
        return new Vector2(v.x * c + v.y * s, -v.x * s + v.y * c);
    }
    bool ChaseOn => player != null && player.alive && (phase == "count" || phase == "fight" || phase == "end");

    // 2026-11-25 本人「臨場感が欲しいから、車のゲームみたいな感じで視点を変えて」: 自分のコマの後ろ・低めから追う。
    // 向きは進む向きにゆっくり付いていく（ぶつかって跳ね返った時は振り回さない）
    void ChaseCamera(float realDt)
    {
        Vector3 p = player.pos;
        Vector2 hv = new Vector2(player.vel.x, player.vel.z);
        bool snap = camInit;
        if (camInit)
        {
            camInit = false;
            Vector2 toC = -new Vector2(p.x, p.z);
            camYaw = toC.sqrMagnitude > 0.01f ? Mathf.Atan2(toC.x, toC.y) : 0f;
            camVel = Vector3.zero;
        }
        else if (touchLookT > 0) touchLookT -= realDt;
        else if (touchAimOn) { }   // 狙っている間はカメラを勝手に回さない（指の向きと地面の向きがずれる）
        else if (phase == "fight" && !mouseLook && hv.magnitude > 2.5f)
        {
            float want = Mathf.Atan2(hv.x, hv.y);
            float diff = Mathf.DeltaAngle(camYaw * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(diff) < 1.75f)
            {
                float rate = 2.2f * Mathf.Clamp01((hv.magnitude - 2.5f) / 6f) * (1f - Mathf.Abs(diff) / 1.75f * 0.6f);
                camYaw += diff * (1 - Mathf.Exp(-rate * realDt));
            }
        }
        Vector3 fwd = new Vector3(Mathf.Sin(camYaw), 0, Mathf.Cos(camYaw));
        float portrait = cam.aspect < 1.2f ? Mathf.Lerp(1.45f, 1f, Mathf.InverseLerp(0.45f, 1.2f, cam.aspect)) : 1f;
        float baseY = Mathf.Max(p.y, -3f);
        const float camD = 7.9f;
        Vector3 want3 = new Vector3(p.x, baseY, p.z) + (-fwd * Mathf.Cos(camElev) + Vector3.up * Mathf.Sin(camElev)) * camD * portrait;
        float fh = Arena.H(want3.x, want3.z);
        if (fh > -100 && want3.y < fh + 1.4f) want3.y = fh + 1.4f;
        camRig.position = snap ? want3 : Vector3.SmoothDamp(camRig.position, want3, ref camVel, 0.12f, 200f, realDt);
        Vector3 lookWant = new Vector3(p.x, baseY + 0.6f, p.z) + fwd * 4f;
        camLook = snap ? lookWant : Vector3.Lerp(camLook, lookWant, 1 - Mathf.Exp(-12f * realDt));
        Vector3 off = Vector3.zero;
        if (shake > 0)
        {
            off = Random.insideUnitSphere * shake * 0.2f;
            shake = Mathf.Max(0, shake - realDt * 2.2f);
        }
        cam.transform.localPosition = off;
        cam.transform.rotation = Quaternion.LookRotation(camLook - camRig.position);
        float fovWant = (cam.aspect < 1.2f ? 66f : 60f) + Mathf.Clamp01((hv.magnitude - 10f) / 15f) * 8f;
        camFov = Mathf.Lerp(camFov, fovWant, 1 - Mathf.Exp(-6f * realDt));
        cam.fieldOfView = camFov - fovKick;
        fovKick = Mathf.Max(0, fovKick - realDt * 22f);
    }

    void FightCamera(float realDt)
    {
        if (ChaseOn) { ChaseCamera(realDt); return; }
        Vector3 focus = player.alive || phase == "count" ? player.pos : Vector3.zero;
        if (phase == "end" && player.alive) focus = player.pos;
        if (focus.y < -3f) focus.y = -3f;
        Vector3 target = Vector3.Lerp(new Vector3(focus.x, Mathf.Max(focus.y, 0), focus.z), new Vector3(0, 0.6f, 0), 0.18f);
        float zoom = phase == "end" ? 0.8f : 1f;
        if (cam.aspect < 1.2f) zoom *= Mathf.Lerp(1.55f, 1f, Mathf.InverseLerp(0.45f, 1.2f, cam.aspect));
        Vector3 want = target + new Vector3(0, 10.6f, -9.9f) * zoom;
        camRig.position = Vector3.SmoothDamp(camRig.position, want, ref camVel, 0.18f, 200f, realDt);
        camLook = Vector3.Lerp(camLook, target + Vector3.up * 0.3f, 1 - Mathf.Exp(-10f * realDt));
        Vector3 off = Vector3.zero;
        if (shake > 0)
        {
            off = Random.insideUnitSphere * shake * 0.2f;
            shake = Mathf.Max(0, shake - realDt * 2.2f);
        }
        cam.transform.localPosition = off;
        cam.transform.rotation = Quaternion.LookRotation(camLook - camRig.position);
        float fovWant = 50f;
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
              .Append(",\"g\":").Append(F(k.gauge / 100f)).Append(",\"cd\":").Append(F(Mathf.Clamp01(k.dashCd / (DashCdBase * k.st.dashCd))))
              .Append(",\"x\":").Append(F(sp.x)).Append(",\"y\":").Append(F(sp.y)).Append(",\"z\":").Append(F(sp.z))
              .Append(",\"vx\":").Append(F(k.vel.x)).Append(",\"vz\":").Append(F(k.vel.z)).Append(",\"yaw\":").Append(F(camYaw)).Append(",\"air\":").Append(k.grounded ? 0 : 1).Append(",\"h\":").Append(F(k.pos.y)).Append(",\"gh\":").Append(F(Arena.H(k.pos.x, k.pos.z))).Append(",\"wx\":").Append(F(k.pos.x)).Append(",\"wz\":").Append(F(k.pos.z)).Append(",\"r\":").Append(F(new Vector2(k.pos.x, k.pos.z).magnitude))
              .Append(",\"p\":[").Append(k.top).Append(',').Append(k.core).Append(',').Append(k.leg).Append("]")
              .Append(",\"why\":\"").Append(k.koWhy).Append("\"");
            if (debugState) sb.Append(",\"L\":[").Append(F(k.lossHit)).Append(',').Append(F(k.lossWall)).Append(',').Append(F(k.lossDash)).Append(',').Append(F(k.spinMax)).Append("]");
            sb.Append('}');
        }
        sb.Append("]}");
        Emit(sb.ToString());
    }
}
