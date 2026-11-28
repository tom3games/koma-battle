using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// 全体の進行・入力・物理・CPU・演出・画面（HTML）とのやりとり
public partial class KomaBoot : MonoBehaviour
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
    int pTop = 0, pCore = 0, pLeg = 0, pSp = 0, nCpu = 2; bool pLeft; int pPas = -1;
    bool autoPlayer, cpuOff, debugState;
    readonly Vector2[] movesBuf = new Vector2[4];
    readonly List<long> keyBuf = new List<long>();
    Vector2 lastAimDir; float lastAimT = -99;
    float testScale = 1f;
    bool boardAnnounced;
    class Fire { public Vector3 pos; public Koma owner; public float t, dmg = 12f; }
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
        public int target = -1; public float retarget, react, padGoal = -1, think, braceCd;
        public Vector2 jitter; public float boardT, lastPlayerT;
    }

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    void Start()
    {
        Application.targetFrameRate = 60;
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;   // 名前の入力欄に打てるように（試合の開始で canvas にフォーカスを渡す）
#endif
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
        sun.transform.rotation = Quaternion.Euler(72f, -25f, 0); sunL = sun;
        var rim = new GameObject("Rim").AddComponent<Light>();
        rim.type = LightType.Directional; rim.color = new Color(0.55f, 0.68f, 0.9f); rim.intensity = 0.35f; rim.renderMode = LightRenderMode.ForceVertex;
        rim.transform.rotation = Quaternion.Euler(18f, 160f, 0);

        // 背景は地下闘技場（ArenaBackdrop.cs）
        Backdrop.Build(mats);

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
                pTop = (int)N(0); pCore = (int)N(1); pLeg = (int)N(2); if (nums.Length >= 4) pSp = (int)N(3); if (nums.Length >= 5) pLeft = N(4) > 0; if (nums.Length >= 6) pPas = (int)N(5);
                if (phase == "title" || phase == "select") { player.Rebuild(pTop, pCore, pLeg, mats, pLeft); player.sp = pSp; Showcase(); }
                break;
            case "select": phase = "select"; phaseT = 0; Showcase(); break;
            case "title": phase = "title"; phaseT = 0; Showcase(); break;
            case "start":
                if (nums.Length >= 6) pLeft = N(5) > 0; if (nums.Length >= 7) pPas = (int)N(6);
                if (nums.Length >= 3) { pTop = (int)N(0); pCore = (int)N(1); pLeg = (int)N(2); player.Rebuild(pTop, pCore, pLeg, mats, pLeft); if (pLeft) { player.spinMax *= LeftSpinMul; player.spin = player.spinMax; } }   // 2026-11-28 本人「左回転強すぎ。自分の回転数のデフォが少し下がるように」: 左回転を選んだ自分だけ回転の総量を下げる（割合の判定はそのまま）
                if (nums.Length >= 4) nCpu = Mathf.Clamp((int)N(3), 1, 3);
                if (nums.Length >= 5) pSp = Mathf.Clamp((int)N(4), 0, 10);
                player.sp = pSp; player.pas = Mathf.Clamp(pPas, -1, 7);
                StartMatch();
                break;
            case "auto": autoPlayer = N(0) > 0; break;
            case "spfire": { var kk = komas[(int)N(0)]; TrySpecial(kk, new Vector3(N(1), kk.pos.y, N(2))); break; }   // 検査用: 指定のコマが必殺を撃つ（狙い=世界の点）
            case "dashat": { var kk = komas[(int)N(0)]; TryDash(kk, new Vector3(N(1), kk.pos.y, N(2))); break; }   // 検査用: 指定のコマがダッシュ
            case "frost": komas[(int)N(1)].frostT = N(0); break;
            case "aimc": AimCycle(); break;   // 検査用: C キーと同じ
            case "stage": stageForce = (int)N(0); break;   // 検査用: 次の試合のステージを固定（-1 でランダム）
            case "spec": SpectateSet((int)N(0)); break;   // 観戦: 誰を見るか
            case "specnext": SpectateStep((int)N(0) >= 0 ? 1 : -1); break;
            case "specexit": if (phase == "fight" && spectating) { phase = "end"; endT = EndHold - 0.001f; endWin = 0; endWhy = player.koWhy; endSubject = specTarget; spectating = false; } break;
            case "brace": touchBrace = N(0) > 0; break;
            case "partwear": PartWear(komas[(int)N(0)], (int)N(1), N(2)); break;   // 検査用: パーツを直接削る
            case "arenacheck": Emit("{\"t\":\"e\",\"k\":\"arena\"," + Arena.Verify() + "}"); break;   // 検査用: ステージの部品が全部あるか
            case "practice": practice = N(0) > 0; dummyType = (int)N(1); break;   // 練習場（開始の前に送る）
            case "pracreset": if (practice) { player.Place(new Vector3(-6f, Arena.H(-6f, 0) + 2.5f, 0)); dummy.Place(new Vector3(6f, Arena.H(6f, 0) + 2.5f, 0)); pAliveLast = true; deathT = -1f; pracWinT = -1f; } break;
            case "dummytype": dummyType = (int)N(0); break;
            case "recenter": RecenterHalf = N(0) > 0 ? N(0) : 1e9f; break;   // 検査用: 寄せる速さ（半減秒）。0以下で切る
            case "setparts": { var kk = komas[(int)N(0)]; kk.Rebuild((int)N(1), (int)N(2), (int)N(3), mats, kk.left); break; }   // 検査用: 指定のコマのパーツを変える
            case "leftmul": LeftSpinMul = N(0); break;   // 検査用
            case "knockk": KnockRest = N(0); KnockRestDecisive = N(1); KnockSpin = N(2); KnockSpinDecisive = N(3); break;   // 検査用
            case "runknock": RunHitKnock = N(0); break;
            case "cpusp": cpuSpMode = (int)N(0); break;   // 検査用: CPU の必殺を固定（-1 でランダム）
            case "setsp": komas[(int)N(1)].sp = (int)N(0); break;
            case "pitstart": pitForced = true; break;
            case "cpupas": cpuPasMode = (int)N(0); break;
            case "assist": assistOn = N(0) > 0; break;
            case "strike": SpawnStrike(komas[(int)N(1)], komas[(int)N(0)]); break;   // 検査用: 雷（0=標的 1=撃つ側）
            case "guard": komas[(int)N(1)].guard = N(0) > 0; break;
            case "setpas": komas[(int)N(1)].pas = (int)N(0); break;
            case "cpuoff": cpuOff = N(0) > 0; break;
            case "inmv": testMoveOn = nums.Length >= 2; testMove = new Vector2(N(0), N(1)); break;   // 検査用: 自分の入力を世界の向きで固定（引数なしで解除）
            case "debugstate": debugState = N(0) > 0; break;
            case "timescale": testScale = Mathf.Clamp(N(0), 0.05f, 8f); break;
            case "boardat": BoardAt = N(0); break;
            case "fightt": fightT = N(0); break;
            case "jump": player.jumpReq = true; break;
            case "lean": StartLean(player, CamRel(new Vector2(N(0), N(1)))); break;   // 検査用: 傾け（画面の向き）
            case "leanw": StartLean(player, new Vector2(N(0), N(1))); break;   // 検査用: 傾け（世界の向き）
            case "super": StartSuper(player); break;
            case "pause": paused = N(0) > 0; break;
            case "move": touchMove = new Vector2(N(0), N(1)); if (touchMove.sqrMagnitude > 1) touchMove.Normalize(); break;
            case "dash": touchDash = true; break;
            case "touchmode": touchMode = N(0) > 0; break;
            // 2026-11-25 本人「車のゲームみたいにマウスで視点変えたい」: 画面に固定したマウスの動きで向きと高さを変える
            case "mlock": mouseLook = N(0) > 0; break;
            case "look":
                if (N(0) != 0 || N(1) != 0) camIdleT = 0f;
                if (mouseLook) { camYaw += N(0) * 0.0032f; camElev = Mathf.Clamp(camElev + N(1) * 0.0026f, 0.1f, 0.95f); }
                break;
            // スマホ: 画面のなぞりで視点を回す。なぞっている間と直後は、進む向きへの自動の追従を止める
            case "tlook": camIdleT = 0f; camYaw += N(0) * 0.0075f; camElev = Mathf.Clamp(camElev + N(1) * 0.005f, 0.1f, 0.95f); touchLookT = 1.6f; break;
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
            case "specialx": { var t = Nearest(player); if (phase == "fight" && player.alive) TryEvolved(player, t != null ? t.pos : player.pos + Vector3.forward); break; }   // 極大技（スマホ・検査用）
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
        Arena.ResetCollapse(); Arena.SetStage(0); ApplyStageLook(0);
        sudden = false; boardAnnounced = false; fires.Clear(); ClearChains();
    }

    void StartMatch()
    {
        phase = "count"; phaseT = 0; fightT = 0; sudden = false; endWin = -1; endT = 0;
        touchMove = Vector2.zero; touchAimOn = false; touchDash = false; touchSpecial = false;
        camInit = true; Arena.ResetCollapse(); boardAnnounced = false; fires.Clear(); ClearChains(); pitForced = false; suddenAt = -1f;
        if (practice) nCpu = 1;
        { int st = practice ? 0 : PickStage(); Arena.SetStage(st); ApplyStageLook(st); BoardAt = st != 0 ? 1e9f : 40f; Emit("{\"t\":\"e\",\"k\":\"stage\",\"p\":" + st + "}"); }
        spectating = false; deathT = -1f; pAliveLast = true; specTarget = null; endSubject = null; camClose = 0f; placeAtDeath = 0; totalK = 1 + nCpu;
        pairCd.Clear();
        int n = 1 + nCpu;
        for (int i = 0; i < 4; i++)
        {
            var k = komas[i];
            if (i >= n) { k.root.SetActive(false); k.alive = false; continue; }
            if (i > 0 && practice) { /* 練習: ダミーは PracticeSetup で固定の装備 */ }
            else if (i > 0) { k.Rebuild(rng.Next(Parts.Count), rng.Next(Parts.Count), rng.Next(Parts.Count), mats, rng.NextDouble() < 0.25); k.sp = cpuSpMode >= 0 ? cpuSpMode : rng.Next(11); k.pas = cpuPasMode == -2 ? (rng.NextDouble() < 0.5 ? rng.Next(8) : -1) : cpuPasMode; }
            float ang = -Mathf.PI / 2 + Mathf.PI / 8 + i * Mathf.PI * 2 / n;
            float r = 11f;
            float x = Mathf.Cos(ang) * r, z = Mathf.Sin(ang) * r;
            k.Place(new Vector3(x, Arena.H(x, z) + 3.5f, z));
            brains[i] = new Brain { react = 0.6f + (float)rng.NextDouble() * 0.6f };
        }
        countStarted = false;
        if (practice) PracticeSetup();
    }
    // 2026-11-28 本人「必殺が簡単にたまりすぎ」: 時間 2.2→1.0/秒、当てた時 ×1.5→×0.9、当てられた時 ×1.1→×0.6
    public float GaugeTime = 1.6f, GaugeDealt = 1.8f, GaugeTaken = 1.1f;
    // 受けるダメージの倍率 [ステージ, 型]（型 0 なし・1 速度・2 攻撃・3 重量）。typemix.mjs（3体とも CPU）で測って合わせる
    static readonly float[,] TypeTake = { { 1f, 0.7f, 1.1f, 0.85f }, { 1f, 1f, 1f, 0.75f }, { 1f, 1f, 1f, 1f } };
    bool rmbHold; float rmbT, evoCharge;
    public float HeavyDecayK = 2.4f;   // 2026-11-28 本人「重量型が強すぎる」: 質量1.4を超えた分だけ回転が早く減る（重量型 1.65 で×2.0）
    public float TurnSudden = 70f, TurnDecayMul = 0.8f, TurnWear = 0.8f, TurnGain = 0.6f, TurnSpeedK = 0.5f;
    public float TurnSameDecay = 0.88f, TurnReverseDecay = 1.15f;
    const float StageIntro = 1.5f; bool countStarted;

    // ---------- 毎フレーム ----------
    void Update()
    {
        float realDt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        float now = Time.unscaledTime;
        float scale = now < slowUntil ? slowScale : 1f;
        Time.timeScale = paused ? 0f : scale * testScale;
        float dt = Mathf.Min(Time.deltaTime, 0.05f * testScale);
        phaseT += realDt * testScale;
        Backdrop.Tick(Time.unscaledTime);

        if (phase == "title" || phase == "select")
        {
            player.spin = player.spinMax;
            player.Visual(Time.unscaledDeltaTime, realDt);
            float a = Time.unscaledTime * (phase == "title" ? 0.15f : 0.35f);
            float dist = phase == "title" ? 15f : 5.2f, hgt = phase == "title" ? 7.5f : 2.4f;
            Vector3 look = phase == "title" ? new Vector3(0, 0.5f + (cam.aspect < 1f ? 2.6f : 0f), 0) : new Vector3(0, 0.7f, 0);
            camRig.position = new Vector3(Mathf.Sin(a) * dist, hgt, -Mathf.Cos(a) * dist);
            cam.transform.localPosition = Vector3.zero;
            if (phase == "title") look -= Vector3.Cross(Vector3.up, (look - camRig.position).normalized) * 4.2f * Mathf.Clamp01(cam.aspect - 0.6f);
            cam.transform.rotation = Quaternion.LookRotation(look - camRig.position);
            reticle.gameObject.SetActive(false); aimLine.enabled = false;
            EmitState(realDt, false);
            UpdateWaves(realDt);
            return;
        }

        if (phase == "count")
        {
            // 2026-11-28 本人「気になったところは直して」: 最初の StageIntro 秒はステージ名だけ見せ、そのあとにカウントダウンを始める（重ならない）
            float ptc = Mathf.Max(0f, phaseT - StageIntro), ptb = Mathf.Max(0f, phaseT - realDt * testScale - StageIntro);
            if (!countStarted && phaseT >= StageIntro) { countStarted = true; Emit("{\"t\":\"e\",\"k\":\"count\",\"n\":3}"); }
            int before = Mathf.CeilToInt(3f - ptb);
            int nowN = Mathf.CeilToInt(3f - ptc);
            if (countStarted && nowN != before && nowN > 0 && nowN < 3) Emit("{\"t\":\"e\",\"k\":\"count\",\"n\":" + nowN + "}");
            foreach (var k in komas) if (k.root.activeSelf) { k.vel = Vector3.zero; k.Visual(realDt, realDt); }
            if (phaseT >= 3f + StageIntro)
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
            bool bon = fightT >= BoardAt && !practice;
            Arena.SetBoard(bon, fightT, fightT > BoardAt - 3f && !bon ? 1f : 0f);
            if (bon && !boardAnnounced && !Arena.Collapsing) { boardAnnounced = true; Emit("{\"t\":\"e\",\"k\":\"board\"}"); }
        }
        UpdateFires(dt);
        if (phase != "end") Arena.TurnUpdate(realDt * testScale);
        // 最終局面: 100秒たつか、4人の試合で50秒を過ぎて残り2人になったら外から床が崩れる
        if (phase == "fight" && !sudden && !practice)
        {
            int act = 0, alv = 0; foreach (var k in komas) if (k.root.activeSelf) { act++; if (k.alive) alv++; }
            if (fightT > (Arena.Stage == 1 ? 70f : Arena.Stage == 2 ? TurnSudden : 100f) || (act >= 4 && alv <= 2 && fightT > (Arena.Stage == 1 ? 35f : Arena.Stage == 2 ? TurnSudden * 0.5f : 50f))) { sudden = true; suddenAt = fightT; Arena.StartCollapse(); Emit("{\"t\":\"e\",\"k\":\"sudden\"}"); }
        }
        if (phase == "fight")   // 2026-11-28 本人「勝った後、ステージの崩壊とかコマの移動は止めて」: 決着後は崩落・穴・床の回転を止める
        {
            UpdatePitFlow(dt);
            if (Arena.UpdateCollapse(dt)) { shake = Mathf.Max(shake, 0.55f); fovKick = Mathf.Max(fovKick, 5f); Emit("{\"t\":\"e\",\"k\":\"collapse\"}"); }
        }

        // 入力
        Vector2 move = Vector2.zero; bool dash = false, special = false, evoFire = false;
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
                if (Input.GetKeyDown(KeyCode.C)) AimCycle();   // 2026-11-28 本人「C を押したら相手に照準が合う」
                // WASD（矢印）を0.28秒以内に2回: その向きへ傾ける
                for (int di = 0; di < 4; di++)
                {
                    if (!(Input.GetKeyDown(LeanKeys[di * 2]) || Input.GetKeyDown(LeanKeys[di * 2 + 1]))) continue;
                    if (Time.unscaledTime - lastKeyT[di] < 0.28f) { StartLean(player, CamRel(LeanVec[di])); lastKeyT[di] = -9; }
                    else lastKeyT[di] = Time.unscaledTime;
                }
                if (touchMove.sqrMagnitude > 0.01f) move = touchMove;
                if (move.sqrMagnitude > 1) move.Normalize();
                move = CamRel(move);
                dash = (!touchMode && Input.GetMouseButtonDown(0)) || touchDash;
                // 2026-11-29 極大技: ゲージ5本の時は右クリック長押し（0.5秒）で進化版、短く押すと今の必殺（1回分）。5本でなければ今まで通りすぐ撃つ
                if (!touchMode && Input.GetMouseButtonDown(1)) { if (EvoReady(player)) { rmbHold = true; rmbT = 0f; } else special = true; }
                if (rmbHold) { if (Input.GetMouseButton(1)) { rmbT += realDt; if (rmbT >= 0.5f) { rmbHold = false; evoFire = true; } } else { rmbHold = false; special = true; } }
                if (!EvoReady(player)) rmbHold = false;
                evoCharge = rmbHold ? Mathf.Clamp01(rmbT / 0.5f) : 0f;
                special |= touchSpecial;   // 2026-11-26 本人「必殺は右クリックだけでいい」
                if (touchDash && Input.touchCount == 0 && !Input.GetMouseButtonDown(0))
                {
                    // 画面のボタンのダッシュ: 動かしている向き、なければ近い相手へ
                    var t = Nearest(player);
                    if (move.sqrMagnitude > 0.01f) aimPoint = player.pos + new Vector3(move.x, 0, move.y) * 6f;
                    else if (t != null) aimPoint = t.pos;
                }
            }
            if (testMoveOn) move = testMove;
            // 2026-11-28 本人「Fで踏ん張り」: PC は F を押している間。スマホは踏ん張りボタン（左のスティックを離している間だけ有効）
            player.braceHold = !autoPlayer && ((!touchMode && Input.GetKey(KeyCode.F)) || (touchBrace && touchMove.sqrMagnitude < 0.01f));
            touchDash = false; touchSpecial = false;
            if (dash) TryDash(player, aimPoint);
            if (special) TrySpecial(player, aimPoint);
            if (evoFire) TryEvolved(player, aimPoint);
        }

        // CPU
        var moves = movesBuf; for (int mi = 0; mi < 4; mi++) moves[mi] = Vector2.zero;
        moves[0] = move;
        for (int i = 1; i < komas.Count; i++)
        {
            var k = komas[i];
            if (!k.alive || !k.root.activeSelf || phase != "fight" || cpuOff) continue;
            bool d2, s2 = false;
            if (practice) PracticeThink(k, dt, out moves[i], out d2); else Think(k, dt, out moves[i], out d2, out s2);
            if (d2 && practice) TryDash(k, player.pos);
            else if (d2 && !(Arena.PitActive && CrossesPit(k.pos, AimFor(k)))) TryDash(k, k.chainFrom != null ? k.pos + (k.pos - k.chainFrom.pos) : AimFor(k));
            if (s2) { if (EvoReady(k) && rng.NextDouble() < 0.7) TryEvolved(k, AimFor(k)); else TrySpecial(k, AimFor(k)); }
        }

        for (int mi = 0; mi < komas.Count; mi++) komas[mi].inputMv = phase == "fight" ? moves[mi] : Vector2.zero;
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
            ChainConstrain(h);
        }
        UpdateChains(dt);
        foreach (var k in komas)
        {
            if (!k.root.activeSelf) continue;
            if (k.dashT > 0) k.dashT -= dt;
            if (k.dashBrakeT > 0) k.dashBrakeT -= dt;
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
                k.spin -= (0.75f * (k.bzT > 0 ? 0f : 1f) * (Arena.Stage == 1 ? 0.7f : Arena.Stage == 0 ? 1.45f : 1f) * ShaftDecayMul(k) * (1f + HeavyDecayK * (Arena.Stage == 2 ? 0.4f : 1f) * Mathf.Max(0f, k.st.mass - 1.4f)) * (Arena.Stage == 2 ? Mathf.Min(k.st.decay, 0.9f) : k.st.decay) * (Arena.Stage == 2 ? TurnDecayMul * ((k.left == (Arena.TurnDirAt(new Vector2(k.pos.x, k.pos.z).magnitude) < 0f)) ? TurnSameDecay : TurnReverseDecay) : 1f) + (sudden ? 1.2f : 0f)) * dt;   // ターンテーブル: 床と同じ向き（右回り）は減りが少し遅く、逆向き（左回り）は少し速い
                if (Arena.Stage == 2 && k.grounded) k.spin -= TurnWear * Mathf.Max(0f, k.st.grip - 0.7f) * Mathf.Clamp01(new Vector2(k.pos.x, k.pos.z).magnitude / Arena.R) * dt;   // 回る床の摩擦: グリップの高い（重い）足ほど、外側で回転が削れる
                if (Arena.Stage == 2 && k.alive) k.spin = Mathf.Min(k.spinMax, k.spin + TurnGain * Mathf.Max(0f, k.st.speed - 0.9f) * dt);   // 回る床の勢いをもらう: 速く動くコマほど回転が戻る
                if (k.braceT > 0) k.braceT -= dt;
                if (k.Bracing && k.grounded)
                {
                    k.spin -= BraceWear * BraceWearF(k) * dt; k.lossHit += BraceWear * BraceWearF(k) * dt;
                    k.braceFxT -= dt;
                    if (k.braceFxT <= 0) { k.braceFxT = 0.09f; Spark(k.pos + Vector3.up * 0.05f, new Vector3(Random.Range(-1f, 1f), 0.3f, Random.Range(-1f, 1f)), 4, 0.55f); }
                    if (Random.value < 0.02f) EmitEv("brace", k, 0);
                }
                k.gauge = Mathf.Min(GaugeCap, k.gauge + GaugeTime * k.st.gaugeRate * dt);
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
            PassiveUpdate(k, dt);
            k.Visual(dt, realDt);
        }
        UpdateStrikes(dt); UpdateClones(dt); UpdateSp3(dt); UpdateEvo(dt); UpdateBerserk(dt); UpdateArrowRings(realDt); UpdateAimMark(realDt);
        keyBuf.Clear(); keyBuf.AddRange(pairCd.Keys);
        foreach (var key in keyBuf) { pairCd[key] -= dt; if (pairCd[key] <= 0) pairCd.Remove(key); }

        // 決着（練習場は決着なし）
        if (practice && phase == "fight") PracticeUpdate(dt, realDt);
        if (phase == "fight" && !practice)
        {
            int alive = 0; Koma last = null;
            foreach (var k in komas) if (k.root.activeSelf && k.alive) { alive++; last = k; }
            // 2026-11-28 本人「ダウンしてから負けが出るまでが早い。余韻も無い。観戦できるように」:
            // 自分がダウンしたらスロー＋カメラが寄って見送る。他のコマがまだ2体以上いれば観戦に入り、決着してから余韻（2.3秒）を置いて結果へ
            if (pAliveLast && !player.alive)
            {
                pAliveLast = false; deathT = 0; placeAtDeath = alive + 1;
                SlowFx(0.45f, 1.0f);
            }
            if (deathT >= 0) deathT += realDt;
            if (alive <= 1)
            {
                phase = "end"; endT = 0;
                endWin = player.alive ? 1 : 0;
                endWhy = player.alive ? "" : player.koWhy;
                endSubject = last != null ? last : player; spectating = false;
                if (player.alive) SlowFx(0.45f, 1.0f);
            }
            else if (!player.alive && deathT > 2.2f && !spectating) { spectating = true; PickSpec(player.pos); }
            if (spectating && (specTarget == null || !specTarget.alive)) PickSpec(specTarget != null ? specTarget.pos : player.pos);
        }
        else if (phase == "end")
        {
            endT += realDt;
            if (endT > EndHold && endT - realDt <= EndHold)
            {
                var sb = new StringBuilder();
                sb.Append("{\"t\":\"e\",\"k\":\"end\",\"win\":").Append(endWin).Append(",\"why\":\"").Append(endWhy).Append("\",\"time\":").Append(F(fightT))
                  .Append(",\"dealt\":").Append(F(player.dealt)).Append(",\"aerial\":").Append(player.aerialHits).Append(",\"specials\":").Append(player.specials)
                  .Append(",\"dashes\":").Append(player.dashes).Append(",\"spin\":").Append(F(player.SpinRatio))
                  .Append(",\"rank\":").Append(endWin == 1 ? 1 : Mathf.Max(2, placeAtDeath > 0 ? placeAtDeath : 2)).Append(",\"of\":").Append(totalK).Append("}");
                Emit(sb.ToString());
            }
        }

        FightCamera(realDt);
        UpdateWaves(realDt);
        UpdateFxLight(realDt); UpdateScorches(realDt);
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
        if (aimHoldT > 0 && aimTarget != null && aimTarget.alive) aimPoint = aimTarget.pos;
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
        if (DodgeStrike(k, out move)) return;
        b.retarget -= dt; b.react -= dt; b.think -= dt;
        if (b.target < 0 || b.retarget <= 0 || !komas[b.target].alive)
        {
            b.retarget = 1.5f + (float)rng.NextDouble() * 1.5f;
            // 近くて回転の少ない相手をねらう
            float best = 1e9f; b.target = -1; bool nearPick = rng.NextDouble() < 0.2;
            foreach (var o in komas)
            {
                if (o == k || !o.alive || !o.root.activeSelf) continue;
                float sc = (o.pos - k.pos).magnitude + o.SpinRatio * 6f + (float)rng.NextDouble() * 3f;
                // 2026-11-28 本人「コンピューター同士でばっかり戦ってる」: プレイヤーが生きている間は狙いの重みを2倍に（距離を半分に見る）、しばらく当たっていなければさらに寄る。たまに（2割）は近い相手を普通に選ぶ
                if (o.isPlayer && !nearPick) { sc *= 0.5f; if (fightT - b.lastPlayerT > 8f) sc -= 6f; }
                if (sc < best) { best = sc; b.target = o.idx; }
            }
            b.boardT = Arena.BoardOn && !Arena.Collapsing && rng.NextDouble() < 0.15 ? 1.5f + (float)rng.NextDouble() * 1.5f : 0f;
            b.padGoal = Arena.Stage == 0 && rng.NextDouble() < 0.12 ? rng.Next(Arena.Pads.Length + Arena.Mounds.Length) : -1;
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
        float lim = Mathf.Min(Arena.R, Arena.SafeR) - (Arena.Stage == 2 ? 8.5f : Arena.Stage == 1 ? 6f : 3.5f);
        if (!riding && r > lim) want += -new Vector2(k.pos.x, k.pos.z).normalized * (r - lim) * 1.2f;
        // 丘: 頂上（中央）を取りに行く。外周に寄ったら上へ戻る
        if (Arena.Stage == 1 && !riding && dist > 3f) { float rr2 = new Vector2(k.pos.x, k.pos.z).magnitude; if (rr2 > 5f) want += -new Vector2(k.pos.x, k.pos.z).normalized * Mathf.Clamp01((rr2 - 5f) / 10f) * 0.9f; }
        // 鎖でつないだ時は、相手のまわりを回って振り回す
        if (k.chainTo != null)
        {
            Vector2 tw = new Vector2(to.x, to.z).normalized; Vector2 tang = new Vector2(-tw.y, tw.x);
            want = tang + tw * Mathf.Clamp((dist - ChainLen * 0.85f) * 0.5f, -1f, 1f);
        }
        // 中央の穴（ひびが入ったら）を避ける
        if (Arena.PitActive && !riding)
        {
            float pr = Arena.DipR + (Arena.PitOpen ? 2.6f : 1.6f);
            if (r < pr) want += (r > 0.1f ? new Vector2(k.pos.x, k.pos.z) / r : Vector2.right) * (pr - r) * 1.8f;
        }
        move = want.sqrMagnitude > 1 ? want.normalized : want;
        // 2026-11-25「白熱」: 遠くからでも突っ込む・間を短く
        if (k.grounded && k.dashCd <= 0 && dist < 10f && b.react <= 0)
        {
            dash = true; b.react = 0.5f + (float)rng.NextDouble() * 0.7f;
        }
        // 空中で相手の上近くなら急降下
        if (!k.grounded && k.airDashUsed < 1 && dist < 6f && k.pos.y > t.pos.y + 1f && k.vel.y < 3f) dash = true;
        if (SpecialReady(k) && b.think <= 0 && CpuWantsSpecial(k, dist)) { special = true; }
        // 縁の近くで当てられそうなら踏ん張る
        b.braceCd -= dt;
        if (k.grounded && b.braceCd <= 0 && k.braceT <= 0 && dist < 5f && new Vector2(k.pos.x, k.pos.z).magnitude > Arena.R - 6f && Vector3.Dot(new Vector3(t.vel.x, 0, t.vel.z), (k.pos - t.pos).normalized) > 5f) { k.braceT = 0.7f; b.braceCd = 2.5f; }
        if (k.sp == 7 && SpecialReady(k) && IsFalling(k) && NearestWithin(k, RecoverRange) != null) special = true;   // 落ちそうな時は鎖で戻る
        if (b.think <= 0) b.think = 0.3f;
    }

    // ダッシュの縁ガード: 向きの先で床が終わる（奈落・穴）までの距離 − 余白。何も無ければ大きい値
    const float DashEdgeMargin = 0.8f;
    float DashReach(Vector3 from, Vector3 dir)
    {
        for (float t = 0.5f; t <= 14f; t += 0.25f)
        {
            float qx = from.x + dir.x * t, qz = from.z + dir.z * t;
            if (Arena.H(qx, qz) < -100 || (Arena.PitActive && new Vector2(qx, qz).magnitude < Arena.DipR + 0.3f)) return Mathf.Max(0f, t - DashEdgeMargin);
        }
        return 99f;
    }
    // 左クリックの連打で出し続けられないよう、ダッシュと急降下で同じ待ち時間を使う
    const float DashCdBase = 1.3f;
    // 2026-11-28 本人「ダッシュの距離短くして」: 出だしの速さ（19）は変えず、続く時間を 0.32→DashSec に、速さの上限へ戻す減速を 3.2→DashBrake に
    const float DashSec = 0.2f, DashBrake = 6f, DashCoastBrake = 3.0f, DashCoastSec = 0.6f;

    // 狙いの補助（かなり甘め）: 狙った向きから 18度以内・9m以内に相手がいる時だけ、その相手の向きへ 50% 寄せる（最大およそ9度）。
    // 範囲に誰もいなければ何もしない。カメラは動かさない
    const float AssistAngle = 18f, AssistRange = 9f, AssistRate = 0.5f;
    bool assistOn = true; public float assistMaxBend;
    Vector3 AssistDir(Koma k, Vector3 dir)
    {
        if (!assistOn || !k.isPlayer || autoPlayer) return dir;
        Vector3 h = new Vector3(dir.x, 0, dir.z); if (h.sqrMagnitude < 1e-4f) return dir; h.Normalize();
        float best = AssistAngle + 1f; Vector3 bt = Vector3.zero;
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - k.pos; d.y = 0; float dist = d.magnitude;
            if (dist > AssistRange || dist < 0.3f) continue;
            float ang = Vector3.Angle(h, d / dist);
            if (ang < best) { best = ang; bt = d / dist; }
        }
        if (best > AssistAngle) return dir;
        Vector3 nd = Vector3.Slerp(h, bt, AssistRate).normalized;
        assistMaxBend = Mathf.Max(assistMaxBend, best * AssistRate);
        return new Vector3(nd.x, dir.y, nd.z);
    }

    void TryDash(Koma k, Vector3 aim)
    {
        if (k.clingT > 0) { if (k.alive && k.isPlayer && !autoPlayer) ClingSave(k); return; }   // CPU は StartCling で決めた時刻に戻る
        if (!k.alive || k.specialPhase > 0 || k.dashCd > 0 || k.Bracing) return;
        if (!k.grounded && k.isPlayer)
        {
            // 空中の急降下: 狙う点を、水平の向きだけ補助で寄せる
            Vector3 hh = aim - k.pos; float hl = new Vector2(hh.x, hh.z).magnitude;
            if (hl > 0.5f) { var nd = AssistDir(k, new Vector3(hh.x, 0, hh.z)); aim = new Vector3(k.pos.x + nd.x * hl, aim.y, k.pos.z + nd.z * hl); }
        }
        Vector3 dir = aim - k.pos;
        if (k.grounded)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = new Vector3(k.vel.x, 0, k.vel.z);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            dir = AssistDir(k, dir);
            float sp = 19f * k.st.speed * (k.frostT > 0 ? FrostSlow : 1f);
            if (k.chainFrom != null) Tug(k, dir);
            float keep = stickDash ? 0f : 0.25f;   // スマホは倒した向きへまっすぐ（前の勢いを混ぜない）
            k.vel = new Vector3(dir.x * sp + k.vel.x * keep, k.vel.y, dir.z * sp + k.vel.z * keep);
            k.lockT = stickDash ? 0.2f : 0f;
            // 2026-11-28 本人「ダッシュで場外になるのもやめて」: 狙いの向きで縁（床の終わり・穴）までを測り、その手前で止まる（自分の勢いだけ守る）
            k.dashFrom = k.pos; k.dashDirG = dir; k.dashStop = DashReach(k.pos, dir); k.dashGuardT = DashSec + 1.4f;
            k.dashT = DashSec; k.dashBrakeT = DashSec + DashCoastSec; k.dashCd = DashCdBase * k.st.dashCd;
            k.spin -= 1.2f; k.dashes++; k.lossDash += 1.2f;
            EmitEv("dash", k, 0);
            Dust(k.pos, 10);
            if (k.pas == 6) SpawnClone(k);
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

    public const float MeteorRange = 8.5f, MeteorFwd = 2.5f;
    // 落ちる先が穴・場外なら、自分の側へ寄せて床の上にする
    Vector3 SafeLanding(Vector3 from, Vector3 want)
    {
        for (float t = 1f; t >= 0f; t -= 0.1f)
        {
            Vector3 q = Vector3.Lerp(from, want, t);
            float hh = Arena.H(q.x, q.z);
            if (hh > -100 && !(Arena.PitActive && new Vector2(q.x, q.z).magnitude < Arena.DipR + 0.8f && t > 0f)) return new Vector3(q.x, hh, q.z);
        }
        return new Vector3(from.x, Mathf.Max(0, Arena.H(from.x, from.z)), from.z);
    }
    int NearMe(Vector3 at, float r) { if (player == null || !player.alive) return 0; Vector3 d = player.pos - at; d.y = 0; return d.magnitude < r ? 1 : 0; }   // 効果線を出すか（自分が巻き込まれる近さ）
    bool touchBrace;
    // 踏ん張り: ノックバックを減らす代わりに、軸がすり減って回転が削れる（重い型は効きやすく減りが少ない）
    const float BraceWear = 11f, BraceHitWear = 3f;
    float BraceCut(Koma k) => Mathf.Clamp(0.35f * Mathf.Sqrt(k.st.mass / 1.1f), 0.25f, 0.38f);
    float BraceWearF(Koma k) => Mathf.Clamp(1.1f / k.st.mass, 0.7f, 1.3f);
    float lastDenyT = -9;
    void TrySpecial(Koma k, Vector3 aim)
    {
        if (!k.alive || k.specialPhase > 0 || k.tornadoT > 0 || k.shieldT > 0 || k.anchorT > 0 || k.chainTo != null) return;
        if (k.gauge < NeedOf(k) || (k.sp == 5 && k.SpinRatio >= 0.3f) || (k.sp == 10 && k.SpinRatio >= 0.5f))
        {
            // ラスト・スパイラルは回転が3割を切るまで撃てない（押したら「撃てない」と分かる）
            if (k.isPlayer && (k.sp == 5 || k.sp == 10) && !autoPlayer && Time.unscaledTime - lastDenyT > 0.4f) { lastDenyT = Time.unscaledTime; EmitEv("deny", k, k.gauge >= NeedOf(k) ? 1 : 0); }
            return;
        }
        k.gauge = Mathf.Max(0f, k.gauge - NeedOf(k)); k.specials++;
        if (!evoCasting) EvoClearForNormal(k);
        if (k.sp >= 4) { SpecialNew(k, aim); return; }
        if (k.sp == 1)
        {
            // テンペスト・ヴォルテックス: 竜巻で周りを引き寄せて巻き上げ、最後に吹き飛ばす
            k.tornadoT = 3.5f; k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.15f);
            EmitEv("special", k, 1); SpecialFx(k, 3.6f);
            return;
        }
        if (k.sp == 2)
        {
            // ファントム・レイド: 近い相手へ3連続の突進
            k.specialPhase = 20; k.rushLeft = 3; k.specialT = 0f;
            EmitEv("special", k, 2); SpecialFx(k, 2.0f);
            return;
        }
        if (k.sp == 3)
        {
            // イージス・リバース: 3秒の盾（受ける回転減りと弾かれを抑え、ぶつかった相手を弾き返す）＋回転を大きく戻す
            k.shieldT = 3.2f; k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.35f);
            EmitEv("special", k, 3); SpecialFx(k, 3.2f);
            Wave(k.pos, 9f); Pulse(k.pos, SpColors[3], 8f, 18f);
            return;
        }
        k.specialPhase = 1; k.specialT = 0.55f;
        k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.2f);
        k.vel = new Vector3(k.vel.x * 0.2f, 17f, k.vel.z * 0.2f);
        k.grounded = false;
        // 2026-11-28 本人「効果距離長すぎ。近くに敵いなかったらそのままほんの少し斜め前に直下」: 自分から MeteorRange 以内の相手だけを追う（その中で狙いに一番近い相手）。
        // いなければ追わず、狙いの向きの2.5m 前の床へほぼ真下に落ちる（穴や場外なら手前の床へずらす）
        Koma best = null; float bd = 1e9f;
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 dk = o.pos - k.pos; dk.y = 0;
            if (dk.magnitude > MeteorRange) continue;
            float d = (o.pos - aim).sqrMagnitude;
            if (d < bd) { bd = d; best = o; }
        }
        if (best != null) k.specialTarget = new Vector3(best.idx, -999, 0);
        else
        {
            Vector3 fw = aim - k.pos; fw.y = 0;
            if (fw.sqrMagnitude < 0.04f) fw = new Vector3(k.vel.x, 0, k.vel.z);
            if (fw.sqrMagnitude < 0.04f) fw = Vector3.forward;
            k.specialTarget = SafeLanding(k.pos, k.pos + fw.normalized * MeteorFwd);
        }
        EmitEv("special", k, 0);
        SpecialFx(k, 1.3f);
    }

    // 必殺技の続き（毎フレーム）
    void UpdateSpecial(Koma k, float dt)
    {
        if (!k.alive) { k.tornadoT = 0; k.shieldT = 0; k.anchorT = 0; k.frostT = 0; k.arrowDashT = 0; if (k.specialPhase == 20 || k.specialPhase >= 30) k.specialPhase = 0; return; }
        if (k.shieldT > 0) k.shieldT -= dt;
        UpdateSpecial2(k, dt);
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
                float res = o.anchorT > 0 ? 0.08f : o.shieldT > 0 ? 0.25f : 1f;
                o.vel += (n * 20f * f + tan * 5f * f * Mathf.Clamp01(dist / 4f)) * dt * res;
                if (dist < 3.4f) { o.vel.y = Mathf.Max(o.vel.y, 4.5f * res); o.grounded = false; }
                Damage(k, o, 6.5f * f * k.st.atk / o.st.def * dt);
                if (k.tornadoT <= 0 && before > 0 && dist < 7.5f)
                {
                    o.vel += -n * 15f * res; o.vel.y = Mathf.Max(o.vel.y, 6f * res); o.grounded = false;
                    Damage(k, o, 9f * k.st.atk / o.st.def);
                }
            }
            if (k.tornadoT <= 0)
            {
                Wave(k.pos, 6f); Wave(k.pos, 9.5f); Wave(k.pos, 13f);
                shake = Mathf.Max(shake, 0.9f); fovKick = Mathf.Max(fovKick, 12f);
                Spark(k.Center, Vector3.up, 110, 1.9f); Dust(k.pos, 36); Scorch(k.pos, 4.5f);
                Pulse(k.pos, SpColors[1], 10f, 20f);
                var sp = cam.WorldToViewportPoint(k.pos); Emit("{\"t\":\"e\",\"k\":\"boom\",\"i\":" + k.idx + ",\"hm\":" + NearMe(k.pos, 9f) + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
            }
            if (Random.value < 0.5f) Dust(k.pos + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f)), 1);
            if (Random.value < 0.6f) Spark(k.pos + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(0.3f, 3.5f), Random.Range(-2.5f, 2.5f)), Vector3.up, 4, 0.6f);
            if (k.isPlayer) shake = Mathf.Max(shake, 0.12f);
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
                k.SetAura(SpColors[2], 0.5f); Spark(k.Center, -new Vector3(dir.x, 0, dir.z) + Vector3.up * 0.3f, 36, 1.3f); Wave(k.pos, 3f);
                Pulse(k.pos, SpColors[2], 4f, 12f);
                if (k.isPlayer) { fovKick = Mathf.Max(fovKick, 8f); shake = Mathf.Max(shake, 0.3f); }
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
        cFire += n;
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
                Damage(f.owner, o, f.dmg * dt);
                if (o.burnSnd <= 0) { o.burnSnd = 0.45f; o.hitFlash = 0.08f; EmitEv("burn", o, 0); }
            }
        }
    }

    void KO(Koma k, string why)
    {
        if (!k.alive || phase == "end") return;
        if (PassiveKo(k, why)) return;
        BzOnKill(k);
        k.Knock(why);
        k.koTime = fightT;
        EmitEv("ko", k, 0, why + "\",\"r\":\"" + F(new Vector2(k.pos.x, k.pos.z).magnitude) + "\",\"vy\":\"" + F(k.vel.y) + "\",\"vh\":\"" + F(new Vector2(k.vel.x, k.vel.z).magnitude) + "\",\"ang\":\"" + F(Mathf.Atan2(k.pos.z, k.pos.x) * Mathf.Rad2Deg) + "\",\"lastHit\":\"" + F(fightT - k.lastHitT));
        shake = Mathf.Max(shake, 0.4f);
    }

    // ---------- 傾け（2026-11-27） ----------
    // WASD 2回押しで、その向きへ1秒だけ大きく傾ける。傾けた側で当てると「かち上げ」（相手が上へ浮いて大きく飛ぶ・削りも少し増える）。
    // 傾けた向きへ少しだけ滑り出る。待ち1.2秒。スマホは未対応
    static readonly KeyCode[] LeanKeys = { KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow, KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow };
    static readonly Vector2[] LeanVec = { Vector2.up, Vector2.down, Vector2.right, Vector2.left };
    readonly float[] lastKeyT = { -9, -9, -9, -9 };
    const float LeanTime = 1.0f;
    public void StartLean(Koma k, Vector2 dir)
    {
        if (!k.alive || k.leanCd > 0 || k.clingT > 0 || k.specialPhase > 0 || phase != "fight") return;
        Vector3 d = new Vector3(dir.x, 0, dir.y); if (d.sqrMagnitude < 1e-4f) return; d.Normalize();
        k.leanDir = d; k.leanT = LeanTime; k.leanCd = LeanTime + 0.2f;
        if (k.grounded) k.vel += d * 6f;
        EmitEv("lean", k, 0);
    }
    bool Upper(Koma a, Vector3 nhToB) => a.leanT > 0 && Vector3.Dot(a.leanDir, nhToB) > 0.45f;

    // ---------- 場外ギリギリの踏ん張り（2026-11-27） ----------
    // 縁から外へ出かけた瞬間、1.1秒だけ縁に引っかかって外へ傾く。ダッシュ（左クリック・はじく・スペース）で内へ跳ね戻る。
    // 間に合わない・その間に当てられたら落ちる。CPU は 55% が 0.35〜0.8秒で戻る。1回使うと 6秒は起きない
    const float ClingWin = 1.1f;
    void StartCling(Koma k, Vector3 at, Vector3 outD)
    {
        k.clingWin = ClingWin; k.clingT = ClingWin; k.clingCd = 6f; k.clingHit = false;
        k.clingAt = at + outD * 0.25f + Vector3.up * 0.05f; k.clingOut = outD;
        k.pos = k.clingAt; k.vel = Vector3.zero; k.grounded = false; k.dashT = 0; k.lockT = 0;
        k.cpuSaveAt = (!k.isPlayer || autoPlayer) && rng.NextDouble() < 0.55 ? ClingWin - (0.35f + (float)rng.NextDouble() * 0.45f) : -1f;
        Spark(k.clingAt + Vector3.up * 0.1f, outD + Vector3.up * 0.6f, 24, 0.9f);
        EmitEv("cling", k, 0);
        if (k.isPlayer) shake = Mathf.Max(shake, 0.25f);
    }
    void ClingStep(Koma k, float dt)
    {
        k.clingT -= dt;
        k.spin -= k.spinMax * 0.02f * dt;
        k.pos = k.clingAt + new Vector3(Mathf.Sin(fightT * 61f), 0, Mathf.Cos(fightT * 47f)) * 0.04f; k.vel = Vector3.zero;
        if (k.jumpReq && k.isPlayer) { k.jumpReq = false; ClingSave(k); return; }
        if (k.cpuSaveAt > 0 && k.clingT <= k.cpuSaveAt) { ClingSave(k); return; }
        if (!k.alive || phase != "fight" || k.clingHit || k.clingT <= 0 || k.spin <= 0)
        {
            // 落ちる: 外へ押し出して、あとは普通に落下（場外の判定は Step の最後）
            k.clingT = 0;
            k.pos = k.clingAt + k.clingOut * 0.9f;
            k.vel = k.clingOut * (k.clingHit ? 11f : 5f) + Vector3.down * 2f;
            EmitEv("clingfall", k, k.clingHit ? 1 : 0);
            if (k.spin <= 0) { k.spin = 0; KO(k, "spin"); }
        }
    }
    void ClingSave(Koma k)
    {
        k.clingT = 0; k.cpuSaveAt = -1;
        Vector3 inD = -k.clingOut;
        k.pos = k.clingAt + Vector3.up * 0.3f;
        k.vel = inD * 15f * Mathf.Lerp(1f, k.st.speed, 0.4f) + Vector3.up * 9.5f;
        k.grounded = false; k.dashT = 0.3f; k.dashCd = Mathf.Max(k.dashCd, 0.6f); k.airDashUsed = 0;
        k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.04f);
        Spark(k.clingAt + Vector3.up * 0.3f, inD + Vector3.up, 70, 1.6f); Wave(k.clingAt, 4f); Pulse(k.clingAt, new Color(1f, 0.8f, 0.4f), 10f, 12f);
        EmitEv("save", k, 0);
        if (k.isPlayer) shake = Mathf.Max(shake, 0.45f);
    }

    // ---------- 物理 ----------
    const float G = 28f, StepUp = 0.35f, WallMinBounce = 2.0f;
    // 壁の中から一番近い外（壁の高さが低い向き）
    Vector2 WallOut(Vector2 q)
    {
        Vector2 best = Vector2.zero; float bh = 1e9f;
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8; Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float hh = Arena.WallH(q.x + d.x * 0.6f, q.y + d.y * 0.6f);
            if (hh < -100) hh = 1e3f;   // 奈落側は選ばない（床の上の外を探す）
            if (hh < bh) { bh = hh; best = d; }
        }
        return best;
    }

    void Step(Koma k, float dt, Vector2 move)
    {
        if (k.clingCd > 0) k.clingCd -= dt;
        if (k.wallFxT > 0) k.wallFxT -= dt;
        if (k.leanT > 0) k.leanT -= dt; if (k.leanCd > 0) k.leanCd -= dt;
        if (k.clingT > 0) { ClingStep(k, dt); return; }
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
            float acc = 26f * k.st.speed * BzSpeed(k) * (k.grounded ? 1f : 0.3f * k.st.air) * (k.superT > 0 ? 4.5f : (k.isPlayer && sprint ? 2.2f : 1f)) * (k.frostT > 0 ? FrostSlow : 1f) * (k.anchorT > 0 ? 0.4f : 1f) * (k.chainTo != null ? 1.6f : 1f) * (Arena.IceAt(p.x, p.z) ? 0.4f : 1f) * (k.Bracing ? 0.5f : 1f);   // 2026-11-26 本人「シフトの加速度をあげて」1.3→2.2
            Vector2 mv = move;
            mv = BzSteer(k, mv, dt);
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
            if (phase == "end" && k.alive) { float dm = Mathf.Exp(-5f * dt); v.x *= dm; v.z *= dm; }
            else if (!k.alive && k.down) { float dm = Mathf.Exp(-2.5f * dt); v.x *= dm; v.z *= dm; }   // 倒れたコマは摩擦が大きく、当てると少し転がって止まる   // 決着後、残ったコマはその場で減速して止まる
            else if (gr.magnitude < 1.2f) { v.x -= gr.x * G * 0.5f * dt; v.z -= gr.y * G * 0.5f * dt; }
            float fr = Mathf.Exp(-0.9f * k.st.grip * (Arena.Stage >= 1 && p.x * p.x + p.z * p.z > (Arena.R - 3.5f) * (Arena.R - 3.5f) ? (Arena.Stage == 2 ? 1.6f : 1.5f) : 1f) * (k.anchorT > 0 ? 6f : (k.dashBrakeT > 0 && k.dashT <= 0 ? DashCoastBrake : 1f)) * (Arena.IceAt(p.x, p.z) ? 0.12f : 1f) * dt);
            if (Arena.Stage == 2 && k.alive && phase == "fight")
            {
                // ターンテーブル: 床が時計回りに回り、足の摩擦（グリップ）の分だけ床の速さに引きずられる。外ほど速い（中心は弱い）。グリップの低い（速い）コマは流されにくい
                Vector2 fl = new Vector2(p.z, -p.x) * Arena.TurnW * Arena.TurnDirAt(Mathf.Sqrt(p.x * p.x + p.z * p.z)) * Mathf.Clamp(k.st.grip, 0.5f, 1.3f) * (p.x * p.x + p.z * p.z > (Arena.R - 3.5f) * (Arena.R - 3.5f) ? 0.45f : 1f);   // 縁の赤い帯の手前は流れが弱い（粘れる）
                v.x = fl.x + (v.x - fl.x) * fr; v.z = fl.y + (v.z - fl.y) * fr;
            }
            else { v.x *= fr; v.z *= fr; }
            // 周回加速板: 矢印の向き（時計回り）に強く押され、帯に沿って1周流れる。回転数が少しだけ戻る
            // 2026-11-26 本人「降りられない・加速が矢印と反対」: 向きを矢印に合わせ、中央へ引き戻すのは外へはみ出した時だけ（内側へはハンドルで降りられる）
            if (k.alive && phase == "fight" && Arena.OnBoard(p.x, p.z))
            {
                float rr = Mathf.Max(0.1f, Mathf.Sqrt(p.x * p.x + p.z * p.z));
                Vector2 rad = new Vector2(p.x, p.z) / rr, tan = new Vector2(rad.y, -rad.x);
                float vr = v.x * rad.x + v.z * rad.y, vt = v.x * tan.x + v.z * tan.y;
                if (vt < 21f) vt = Mathf.Min(21f, vt + 48f * dt);
                if (rr > Arena.BoardRc) vr += ((Arena.BoardRc - rr) * 3f - vr) * (1f - Mathf.Exp(-8f * dt));
                v.x = rad.x * vr + tan.x * vt; v.z = rad.y * vr + tan.y * vt;
                if (k.boardT <= 0) EmitEv("boost", k, vt);
                k.boardT = 0.8f;
                k.spin = Mathf.Min(k.spinMax, k.spin + k.spinMax * 0.01f * dt);
            }
            float maxS = 9.5f * k.st.speed * BzSpeed(k) * (k.superT > 0 ? 2.3f : (k.isPlayer && sprint ? 1.35f : 1f)) * (k.frostT > 0 ? FrostSlow : 1f) * (k.anchorT > 0 ? 0.4f : 1f) * (k.chainTo != null ? 1.7f : 1f) * (k.Bracing ? 0.5f : 1f), hs = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (hs > maxS && k.dashT <= 0 && k.boostCd < 0.35f && k.boardT <= 0)
            {
                float f = Mathf.Max(maxS / hs, Mathf.Exp(-DashBrake * dt));
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
        if (k.specialPhase == 31) { v.x = k.arrowDir.x * ArrowSpeed; v.z = k.arrowDir.z * ArrowSpeed; }
        else if (k.specialPhase == 40) { v.x = 0; v.z = 0; }
        else if (k.specialPhase == 41)
        {
            Vector3 tg = k.specialTarget.y < -900 ? komas[(int)k.specialTarget.x].pos : k.specialTarget;
            if (k.specialTarget.y < -900 && !komas[(int)k.specialTarget.x].alive) tg = p + new Vector3(v.x, 0, v.z);
            Vector3 hd = tg - p; hd.y = 0; if (hd.sqrMagnitude > 0.01f) { hd.Normalize(); v.x = hd.x * 36f; v.z = hd.z * 36f; }
        }
        // ダッシュの縁ガード: 縁の手前の線へ近づいたら強く減速し、線を越える向きの速さは消す（踏ん張りに入れる縁ぎりぎりまでは入れる）
        if (k.dashGuardT > 0 && k.dashStop < 90f && k.grounded)
        {
            k.dashGuardT -= dt;
            float proj = Vector3.Dot(new Vector3(p.x - k.dashFrom.x, 0, p.z - k.dashFrom.z), k.dashDirG);
            float left = k.dashStop - proj;
            if (left < 2.5f)
            {
                float vOut = Vector3.Dot(new Vector3(v.x, 0, v.z), k.dashDirG);
                if (vOut > 0f)
                {
                    float keep = left <= 0f ? 0f : Mathf.Clamp01(left / 2.5f);
                    float want = Mathf.Min(vOut, keep * 6f);   // 残りが少ないほど外向きの速さを絞る
                    float cut = vOut - want; v.x -= k.dashDirG.x * cut; v.z -= k.dashDirG.z * cut;
                }
            }
        }
        Vector3 np = p + v * dt;
        float h0 = Arena.H(p.x, p.z);
        float h = Arena.H(np.x, np.z);
        // 場外ギリギリの踏ん張り: 足場の上から奈落へ出る瞬間、低い所なら縁に引っかかる
        if (h0 > -100 && h < -100 && k.alive && phase == "fight" && k.specialPhase == 0 && k.clingCd <= 0 && p.y < h0 + 2.2f)
        {
            Vector3 outD = new Vector3(np.x - p.x, 0, np.z - p.z);
            if (outD.sqrMagnitude < 1e-6f) outD = new Vector3(p.x, 0, p.z);
            StartCling(k, new Vector3(p.x, h0, p.z), outD.normalized);
            return;
        }
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
            // 2026-11-28 本人「壁に埋まることできるけど、すこし弾かれる感じが欲しい」: すでに壁の中にいたら必ず外へ押し戻す（めり込みを残さない）。
            // ゆっくり当たっても壁の向きに最低 WallMinBounce の跳ね返り（押し付け続けても張り付かず小さく弾かれ続ける）
            // 氷の矢・スパイラルの突進は壁に当たったらそこで終わる（毎刻み壁に当たり続けて回転が減らないように）
            if (k.specialPhase == 31) { k.specialPhase = 0; k.arrowDashT = 0; k.dashT = 0; v *= 0.3f; IceBurst(new Vector3(np.x, p.y + 0.4f, np.z), 20, 1f); }
            else if (k.specialPhase == 41) { SpiralImpact(k, p, 0.6f); v *= 0.3f; }
            bool embedded = Arena.WallH(p.x, p.z) - p.y > stepAllow + 0.02f;
            if (embedded) n = WallOut(new Vector2(p.x, p.z));
            Vector2 vh = new Vector2(v.x, v.z);
            float vn = Vector2.Dot(vh, n);
            if (vn < 0 || embedded)
            {
                if (vn < 0) vh -= 1.6f * vn * n;
                float vn2 = Vector2.Dot(vh, n);
                if (vn2 < WallMinBounce) vh += n * (WallMinBounce - vn2);
                if (-vn > 6f && k.alive)
                {
                    k.spin -= Mathf.Min(2.5f, -vn * 0.1f); k.lossWall += Mathf.Min(2.5f, -vn * 0.1f);
                    Spark(new Vector3(np.x, p.y + 0.4f, np.z), new Vector3(n.x, 0.4f, n.y), 8, 0.6f);
                    EmitEv("wall", k, -vn);
                }
                else if (-vn > 0.8f && k.alive && k.wallFxT <= 0)
                {
                    // 遅い当たり: 小さなほこりと軽い音（連続しても鳴らしすぎない）
                    k.wallFxT = 0.25f;
                    Dust(new Vector3(np.x, p.y, np.z), 2);
                    Spark(new Vector3(np.x, p.y + 0.35f, np.z), new Vector3(n.x, 0.4f, n.y), 3, 0.4f);
                    if (k.isPlayer) EmitEv("wallsoft", k, -vn);
                }
            }
            v.x = vh.x; v.z = vh.y;
            if (embedded) np = new Vector3(p.x + n.x * 0.15f, np.y, p.z + n.y * 0.15f);
            else np = new Vector3(p.x, np.y, p.z);
            h = embedded ? Arena.H(np.x, np.z) : h0;
        }

        // 穴の上で必殺が着地しようとしたら、縁で爆発して外へ弾き出される（自分は落ちない）
        if ((k.specialPhase == 2 || k.specialPhase == 41) && h < -100 && Arena.PitOpen && np.y < 0.6f && new Vector2(np.x, np.z).magnitude < Arena.PitR + 0.3f)
        {
            Vector2 od = new Vector2(np.x, np.z); od = od.sqrMagnitude > 0.01f ? od.normalized : Vector2.right;
            float rr = Arena.PitR + 0.7f;
            np = new Vector3(od.x * rr, Arena.H(od.x * rr, od.y * rr) + 0.3f, od.y * rr);
            if (k.specialPhase == 2) SpecialImpact(k, np); else SpiralImpact(k, np, 1f);
            v = new Vector3(od.x * 8f, 8f, od.y * 8f); k.grounded = false; h = Arena.H(np.x, np.z);
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
                if (k.specialPhase == 31) newVy = Mathf.Clamp(newVy, -8f, 8f);   // 氷の矢は速いので、坂の傾きに乗って空へ飛び出さない
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
            else if (impactV > 6.5f && k.alive)
            {
                // 着地: 床の向きに跳ね返る（反発 約0.28）。先端がこすれて水平の速さが少し落ち、回転が少し減る。傾きは Koma.Land が受け持つ
                Vector2 lg = Vector2.ClampMagnitude(Arena.Grad(np.x, np.z), 1.2f);
                Vector3 ln = Vector3.Lerp(Vector3.up, new Vector3(-lg.x, 1f, -lg.y).normalized, 0.4f).normalized;   // 先端が床をつかむので、斜面でも横へは半分以下しか流れない
                float e = impactV > 14f ? 0.28f : 0.2f;
                float vn = Vector3.Dot(v, ln);
                if (vn < 0) v -= (1f + e) * vn * ln;
                float fr = Mathf.Exp(-impactV * 0.008f * k.st.grip);
                v.x *= fr; v.z *= fr;
                k.grounded = false;
                Landed(k, np, impactV, v);
            }
            else { v.y = 0; k.grounded = true; k.airDashUsed = 0; if (impactV > 6f) Dust(np, 8); }
            if (k.specialPhase == 0 && k.dashT > 0 && impactV > 10f) k.dashT = 0.15f;
        }

        if (k.alive && (np.y < -5f || (new Vector2(np.x, np.z).magnitude > Arena.Edge + 0.4f && np.y < Arena.Bowl(Arena.R)))) KO(k, Arena.PitOpen && new Vector2(np.x, np.z).magnitude < Arena.PitR + 0.6f ? "pit" : "out");
        if (np.y < -40f) { np.y = -40f; v = Vector3.zero; }
        k.pos = np; k.vel = v;
        if (k.rescueSet) { k.pos = k.rescue; k.vel = Vector3.zero; k.grounded = false; k.rescueSet = false; }
    }

    // 着地の演出（強さに比例: 土ぼこり・先端の火花・輪・音・自分の画面の揺れ）と回転の減り
    void Landed(Koma k, Vector3 at, float impactV, Vector3 v)
    {
        float s = Mathf.Clamp01((impactV - 6f) / 24f);
        k.Land(impactV, new Vector3(v.x, 0, v.z));
        Dust(at, Mathf.RoundToInt(6 + 26 * s));
        Spark(at + Vector3.up * 0.1f, Vector3.up, Mathf.RoundToInt(6 + 46 * s), 0.6f + 0.8f * s);
        if (impactV > 12f) Wave(at, 1.8f + 4f * s);
        if (fightT > 2f && impactV > 8f && phase == "fight")
        {
            float loss = Mathf.Min(4f, 0.08f * impactV);
            k.spin -= loss; k.lossHit += loss;
            if (k.spin <= 0) { k.spin = 0; KO(k, "spin"); }
        }
        if (k.isPlayer && impactV > 10f) shake = Mathf.Max(shake, 0.08f + 0.4f * s);
        EmitEv("land", k, impactV);
        PasLanded(k, at, impactV);
    }

    // 光（必殺の発動・当たりの瞬間に周りを一瞬照らす）
    Light fxLight; float fxLightT, fxLightI;
    void Pulse(Vector3 at, Color c, float intensity, float range = 14f)
    {
        cPulse++;
        if (fxLight == null)
        {
            fxLight = new GameObject("FxLight").AddComponent<Light>();
            fxLight.type = LightType.Point; fxLight.shadows = LightShadows.None; fxLight.renderMode = LightRenderMode.ForceVertex;
        }
        fxLight.transform.position = at + Vector3.up * 1.6f; fxLight.color = c; fxLight.range = range;
        fxLightI = intensity; fxLightT = 0f; fxLight.intensity = intensity; fxLight.enabled = true;
    }
    void UpdateFxLight(float realDt)
    {
        if (fxLight == null || !fxLight.enabled) return;
        fxLightT += realDt;
        float k = 1f - fxLightT / 0.42f;
        if (k <= 0) { fxLight.enabled = false; return; }
        fxLight.intensity = fxLightI * k * k;
    }

    // 焦げ跡（衝撃で床に残る暗い円。しばらくして消える）
    readonly List<Transform> scorches = new List<Transform>(); readonly List<float> scorchT = new List<float>(); readonly List<Material> scorchMats = new List<Material>();
    void Scorch(Vector3 at, float r)
    {
        int idx = -1;
        for (int i = 0; i < scorchT.Count; i++) if (scorchT[i] < 0) { idx = i; break; }
        if (idx < 0 && scorches.Count < 4)
        {
            var t = MeshKit.Obj("Scorch", null, MeshKit.Disc(1f, 40), mats.Alpha(new Color(0.02f, 0.02f, 0.03f, 1f), mats.Dot)).transform;
            t.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            scorches.Add(t); scorchT.Add(-1); scorchMats.Add(t.GetComponent<MeshRenderer>().material); idx = scorches.Count - 1;
        }
        if (idx < 0) return;
        float gh = Arena.H(at.x, at.z);
        scorches[idx].position = new Vector3(at.x, (gh > -100 ? gh : at.y) + 0.04f, at.z);
        scorches[idx].localScale = Vector3.one * r; scorches[idx].gameObject.SetActive(true); scorchT[idx] = 0;
    }
    void UpdateScorches(float realDt)
    {
        for (int i = 0; i < scorchT.Count; i++)
        {
            if (scorchT[i] < 0) continue;
            scorchT[i] += realDt;
            float a = scorchT[i] < 0.15f ? scorchT[i] / 0.15f : Mathf.Clamp01(1f - (scorchT[i] - 0.15f) / 3.2f);
            scorchMats[i].SetColor("_TintColor", new Color(0.02f, 0.02f, 0.03f, 0.8f * a) * 0.5f);
            if (scorchT[i] > 3.4f) { scorchT[i] = -1; scorches[i].gameObject.SetActive(false); }
        }
    }

    static readonly Color[] SpColors = {
        new Color(1f, 0.55f, 0.2f), new Color(0.55f, 0.9f, 1f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.82f, 0.4f),
        new Color(0.9f, 0.28f, 0.75f), new Color(1f, 0.15f, 0.25f), new Color(0.7f, 0.93f, 1f), new Color(0.6f, 1f, 0.3f),
        new Color(0.45f, 0.95f, 1f), new Color(0.72f, 0.5f, 1f), new Color(0.85f, 0.08f, 0.14f) };

    // 必殺技を出した瞬間の演出（溜めのオーラ・光・輪・火花・ズーム）
    void SpecialFx(Koma k, float charge)
    {
        var c = SpColors[Mathf.Clamp(k.sp, 0, 10)];
        k.SetAura(c, charge);
        Pulse(k.pos, c, 6f);
        Spark(k.Center, Vector3.up, 70, 1.5f);
        Wave(k.pos, 3.5f); Wave(k.pos, 7f);
        bool me = k.isPlayer;
        fovKick = Mathf.Max(fovKick, me ? 12f : 4f);
        shake = Mathf.Max(shake, me ? 0.45f : 0.15f);
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
        Wave(at, 5.5f); Wave(at, 9f); Wave(at, 13f);
        Spark(at + Vector3.up * 0.3f, Vector3.up, 130, 2.0f);
        Dust(at, 54); Scorch(at, 5.5f);
        Pulse(at, SpColors[0], 12f, 22f);
        k.SetAura(SpColors[0], 0.35f);
        foreach (var o in komas)
        {
            if (o == k || !o.alive || !o.root.activeSelf) continue;
            Vector3 d = o.pos - at; d.y = 0;
            float dist = d.magnitude;
            float RR = k.evo ? EvoMeteorR : 6f;
            if (dist > RR) continue;
            Vector3 n = dist > 0.01f ? d / dist : Vector3.forward;
            float f = 1f - dist / RR * 0.5f;
            float ares = o.anchorT > 0 ? 0.1f : 1f;
            o.vel += n * 16.5f * f / Mathf.Sqrt(o.st.mass) * (1.6f - o.st.grip * 0.35f) * ares * (k.evo ? EvoMeteorKnock : 1f);
            o.vel.y = Mathf.Max(o.vel.y, 6.5f * f * ares);
            o.grounded = false;
            float dmg = 10f * f * k.st.atk / o.st.def * (k.evo ? EvoMeteorDmg : 1f);
            Damage(k, o, dmg); PartWear(o, 1, DiskMeteor * f);
            o.hitFlash = 0.2f;
            Spark(o.Center, n + Vector3.up * 0.5f, 40, 1.4f);
        }
        if (k.evo) EvoSpikes(at);
        if (k.sp == 0) k.evoT = 0;   // 極大のメテオは着地で終わり（効果を後に引き継がない）
        Slow(0.15f, 0.6f);
        float near = k.isPlayer ? 1f : Mathf.Clamp(1.1f - Vector3.Distance(player.pos, at) / 18f, 0.25f, 1f);
        shake = Mathf.Max(shake, 1.5f * near); fovKick = Mathf.Max(fovKick, 16f * near);
        var sp = cam.WorldToViewportPoint(at);
        Emit("{\"t\":\"e\",\"k\":\"boom\",\"i\":" + k.idx + ",\"hm\":" + NearMe(at, 6.5f) + ",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
    }

    const float HitScale = 0.36f;   // 当たりで減る量。試合の長さはここで決まる
    void Damage(Koma from, Koma to, float dmg)
    {
        if (phase == "end") return;
        dmg *= HitScale * Mathf.Clamp(1.4f - 0.4f * to.st.speed, 0.7f, 1.15f) * (Arena.Stage == 2 ? 1f : Mathf.Pow(Mathf.Max(0.6f, from.st.atk), Arena.Stage == 1 ? -0.9f : -0.5f));   // 攻撃型の与ダメを少し抑える（ターンテーブルは別式）   // 2026-11-29 速度型が受けるダメージを少し減らす（速いコマは当たりをいなす）。重い・遅いコマはわずかに増える
        if (Arena.Stage == 2) dmg *= Mathf.Clamp(1.9f - TurnSpeedK * to.st.speed, 0.5f, 1.4f) * Mathf.Pow(Mathf.Max(0.5f, from.st.atk), -1.1f) * 0.85f;   // ターンテーブル: 速く動き回れるコマは回る床の上で衝撃をいなす（受けるダメージが減る）。遅いコマは受けが増える
        if (to.shieldT > 0) dmg *= 0.2f; if (to.anchorT > 0) dmg *= 0.35f; if (to.specialPhase == 30) dmg *= 0.4f; if (to.chainTo != null) dmg *= 0.5f;
        dmg *= TypeTake[Mathf.Clamp(Arena.Stage, 0, 2), to.tclass];   // 型ごとの受けるダメージの補正（CPU 同士で測って合わせる）
        dmg *= BzAtk(from, 1f);
        if (trailT[to.idx] > 0) dmg *= 0.8f;   // ソニック・トレイル中は受けるダメージが2割減る
        to.lastHitter = from;
        to.spin -= dmg; to.lossHit += dmg; to.lastHitT = fightT;
        if (to.evoShield && to.shieldT > 0 && from != to && from.alive) { float rf = dmg * EvoReflect; from.spin -= rf; from.lossHit += rf; Spark(from.Center, Vector3.up, 16, 1f); if (from.spin <= 0) { from.spin = 0; KO(from, "spin"); } }   // イージス進化版: 受けた分の一部を相手へ跳ね返す
        if (from.st.steal > 0 && from.alive) from.spin = Mathf.Min(from.spinMax, from.spin + dmg * from.st.steal);
        from.dealt += dmg; PasSoul(from, to, dmg);
        from.gauge = Mathf.Min(GaugeCap, from.gauge + dmg * GaugeDealt * from.st.gaugeRate);
        to.gauge = Mathf.Min(GaugeCap, to.gauge + dmg * GaugeTaken * to.st.gaugeRate);
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

    public float LeftSpinMul = 0.9f;
    public float KnockRest = 0.45f, KnockRestDecisive = 0.62f, KnockSpin = 3.1f, KnockSpinDecisive = 3.7f;
    public float RunHitKnock = 1.05f; bool testMoveOn; Vector2 testMove;
    void Collide(float dt)
    {
        for (int i = 0; i < komas.Count; i++)
            for (int j = i + 1; j < komas.Count; j++)
            {
                var a = komas[i]; var b = komas[j];
                if (!a.root.activeSelf || !b.root.activeSelf) continue;
                // 2026-11-28 本人「回転が止まったコマも当たり判定は残してて」: 倒れたコマ（回転0・軸折れ）は床に残る障害物。当たると押し動かされ跳ね返るが、ダメージ・勝敗・必殺の対象にはならない。場外・穴に落ちたコマは今まで通り判定なし
                bool dwA = !a.alive && a.down, dwB = !b.alive && b.down;
                if ((!a.alive && !dwA) || (!b.alive && !dwB)) continue;
                if (dwA || dwB) { if (!(dwA && dwB)) DownBump(a, b, dwA, dwB); continue; }
                Vector3 d = b.Center - a.Center;
                float rr = a.Radius + b.Radius;
                // 高さは楕円で（コマは平たい）
                Vector3 dd = new Vector3(d.x, d.y * 1.6f, d.z);
                float dist = dd.magnitude;
                if (dist >= rr || dist < 1e-4f) continue;
                if (a.clingT > 0 || b.clingT > 0)
                {
                    // 縁で踏ん張っている相手に当てると、とどめ（相手は落ち、当てた方は少し跳ね返る）
                    var c = a.clingT > 0 ? a : b; var o = c == a ? b : a;
                    if (o.clingT <= 0) { c.clingHit = true; c.lastHitT = fightT; o.vel = new Vector3(-o.vel.x * 0.3f, 3f, -o.vel.z * 0.3f); }
                    continue;
                }
                Vector3 n = d.normalized;
                Vector3 nh = new Vector3(n.x, 0, n.z);
                if (nh.sqrMagnitude < 1e-4f) nh = Vector3.forward; else nh.Normalize();
                // 押し出し
                float pen = rr - dist;
                float ma = a.st.mass * (a.anchorT > 0 ? AnchorMass : 1f), mb = b.st.mass * (b.anchorT > 0 ? AnchorMass : 1f);
                // 押し出し: 押した先が壁（段差・縁・柵）の中なら、その側は動かさない
                Vector3 pa = a.pos - nh * pen * mb / (ma + mb), pb = b.pos + nh * pen * ma / (ma + mb);
                bool okA = Arena.WallH(pa.x, pa.z) - a.pos.y <= StepUp, okB = Arena.WallH(pb.x, pb.z) - b.pos.y <= StepUp;
                // 片方が壁で動けないなら、もう片方が全部よける（壁へ押し込まない）
                if (!okA && okB) { pb = b.pos + nh * pen; okB = Arena.WallH(pb.x, pb.z) - b.pos.y <= StepUp; }
                else if (okA && !okB) { pa = a.pos - nh * pen; okA = Arena.WallH(pa.x, pa.z) - a.pos.y <= StepUp; }
                if (okA) a.pos = new Vector3(pa.x, a.pos.y, pa.z);
                if (okB) b.pos = new Vector3(pb.x, b.pos.y, pb.z);
                long key = i * 10 + j;
                Vector3 rel = a.vel - b.vel;
                float vn = Vector3.Dot(rel, n);
                if (a.specialPhase == 31 || b.specialPhase == 31) vn = Mathf.Min(vn, ArrowHitVn);   // 氷の矢は速すぎるので、当たりの強さは上限をつける
                if (vn <= 0.5f || pairCd.ContainsKey(key)) continue;
                pairCd[key] = 0.25f;

                // 空中から上に乗る突撃
                bool aAir = !a.grounded && a.Center.y > b.Center.y + 0.55f && a.vel.y < -2f;
                bool bAir = !b.grounded && b.Center.y > a.Center.y + 0.55f && b.vel.y < -2f;
                Vector3 aV0 = a.vel, bV0 = b.vel;
                // 2026-11-28 本人「ちょっと飛ばされすぎる」: ふつうの当たり（歩き・ダッシュ）は跳ね返り 0.7→0.45・回転の弾き 4→3.1。空中から・かち上げ・必殺は決め技なので下げ幅は小さめ（0.62・3.7）
                bool decisive = aAir || bAir || Upper(a, nh) || Upper(b, -nh) || a.specialPhase >= 2 || b.specialPhase >= 2;
                float rest = decisive ? KnockRestDecisive : KnockRest, spinK = decisive ? KnockSpinDecisive : KnockSpin;
                float J = (1f + rest) * vn / (1f / ma + 1f / mb);
                a.vel -= n * J / ma;
                b.vel += n * J / mb;
                // 回転の弾き（おおげさ）
                float sa = Mathf.Max(0, Vector3.Dot(a.vel + n * J / ma, n)), sb = Mathf.Max(0, Vector3.Dot(b.vel - n * J / mb, -n));
                float spinPushA = spinK * a.st.atk * BzAtk(a, 0.6f) * (0.4f + 0.6f * a.SpinRatio), spinPushB = spinK * b.st.atk * BzAtk(b, 0.6f) * (0.4f + 0.6f * b.SpinRatio);
                Vector3 tang = Vector3.Cross(Vector3.up, nh);
                // 逆回り（左回転）どうしは接点が同じ向きに動いてかみ合わず、横へ大きく流れる
                bool opp = a.left != b.left; float tk = opp ? 4.2f : 1.5f;
                b.vel += nh * spinPushA / mb * (1.5f - b.st.grip * 0.3f) + tang * tk;
                a.vel -= nh * spinPushB / ma * (1.5f - a.st.grip * 0.3f) + tang * tk;
                float pop = Mathf.Min(3.2f, 1f + vn * 0.1f);
                float multA = (a.dashT > 0 ? 1.4f : 1f) * (aAir ? 2.4f * a.st.aerial * (a.pas == 1 ? 1.3f : 1f) : 1f) * (a.specialPhase == 2 ? 1.6f : 1f) * (a.specialPhase == 20 ? 1.35f : 1f) * (a.specialPhase == 31 ? ArrowDmg : 1f) * (a.specialPhase == 41 ? 1.4f * a.lsPow : 1f);
                float multB = (b.dashT > 0 ? 1.4f : 1f) * (bAir ? 2.4f * b.st.aerial * (b.pas == 1 ? 1.3f : 1f) : 1f) * (b.specialPhase == 2 ? 1.6f : 1f) * (b.specialPhase == 20 ? 1.35f : 1f) * (b.specialPhase == 31 ? ArrowDmg : 1f) * (b.specialPhase == 41 ? 1.4f * b.lsPow : 1f);
                // 丘: 坂を駆け下りながら当てると強い（軽い・速い型が坂を使って重い相手に当たる）
                if (Arena.Stage == 1) { float da = -a.vel.y, db = -b.vel.y; if (da > 1.5f) multA *= 1f + Mathf.Min(0.6f, (da - 1.5f) * 0.15f); if (db > 1.5f) multB *= 1f + Mathf.Min(0.6f, (db - 1.5f) * 0.15f); }
                // 丘: 高い所から当てると強い（頂上の取り合い）
                if (Arena.Stage == 1) { float dh = a.Center.y - b.Center.y; if (dh > 1f) multA *= 1f + Mathf.Min(0.35f, (dh - 1f) * 0.15f); else if (dh < -1f) multB *= 1f + Mathf.Min(0.35f, (-dh - 1f) * 0.15f); }
                float shareA = sa / (sa + sb + 0.01f);
                float baseD = 1f + Mathf.Min(vn, 25f) * 0.25f;
                float dmgToB = baseD * (0.5f + shareA) * a.st.atk / b.st.def * multA * 0.5f;
                float dmgToA = baseD * (1.5f - shareA) * b.st.atk / a.st.def * multB * 0.5f;
                if (opp) { if (a.left) dmgToB *= 1.12f; else dmgToA *= 1.12f; }   // 左回転は右回りの相手を1割強く削る
                // 2026-11-26 本人「攻撃の入った角度とかも計算できるように」: 芯・かすり × 正面・横・背後。空中からの突撃と必殺の落下は角度を見ない
                int angA = 0, angB = 0;
                float facA = aAir || a.specialPhase == 2 || a.specialPhase >= 31 ? 1f : AngleFactor(aV0, bV0, nh, out angA);
                float facB = bAir || b.specialPhase == 2 || b.specialPhase >= 31 ? 1f : AngleFactor(bV0, aV0, -nh, out angB);
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
                // 傾けた側で当てた: かち上げ（相手を上へ浮かせて外へ大きく・自分はあまり弾かれない）
                if (Upper(a, nh) && !aAir) { b.vel += nh * 9f / mb; b.vel.y = Mathf.Max(b.vel.y, 13.5f); b.grounded = false; a.vel = Vector3.Lerp(aV0, a.vel, 0.5f); dmgToB *= 1.2f; EmitEv("upper", b, a.idx); }
                if (Upper(b, -nh) && !bAir) { a.vel -= nh * 9f / ma; a.vel.y = Mathf.Max(a.vel.y, 13.5f); a.grounded = false; b.vel = Vector3.Lerp(bV0, b.vel, 0.5f); dmgToA *= 1.2f; EmitEv("upper", a, b.idx); }
                // 盾: 自分はほとんど弾かれず、相手を弾き返す
                if (a.shieldT > 0) { a.vel = Vector3.Lerp(aV0, a.vel, 0.2f); b.vel += nh * 9f; }
                if (b.shieldT > 0) { b.vel = Vector3.Lerp(bV0, b.vel, 0.2f); a.vel -= nh * 9f; }
                // 氷の矢の溜め中は踏ん張る（輪の列がずれないよう、ほとんど弾かれない）
                if (a.specialPhase == 30) a.vel = Vector3.Lerp(aV0, a.vel, 0.3f);
                if (b.specialPhase == 30) b.vel = Vector3.Lerp(bV0, b.vel, 0.3f);
                // グラビティ・アンカー: 動かず、当てた方が弾かれる（少し反動も受ける）
                if (a.anchorT > 0) { a.vel = Vector3.Lerp(aV0, a.vel, 0.05f); b.vel += nh * 7f; b.vel.y = Mathf.Max(b.vel.y, 3f); dmgToA *= 0.35f; dmgToB *= 1.3f; }
                if (b.anchorT > 0) { b.vel = Vector3.Lerp(bV0, b.vel, 0.05f); a.vel -= nh * 7f; a.vel.y = Mathf.Max(a.vel.y, 3f); dmgToB *= 0.35f; dmgToA *= 1.3f; }
                // 踏ん張り: 受けるノックバックを減らす（軸がすり減って回転も削れる）
                if (a.Bracing && a.anchorT <= 0) { a.vel = aV0 + (a.vel - aV0) * (1f - BraceCut(a)); a.spin -= BraceHitWear; a.lossHit += BraceHitWear; Spark(a.pos + Vector3.up * 0.05f, Vector3.up, 12, 0.9f); EmitEv("bracehit", a, 0); }
                if (b.Bracing && b.anchorT <= 0) { b.vel = bV0 + (b.vel - bV0) * (1f - BraceCut(b)); b.spin -= BraceHitWear; b.lossHit += BraceHitWear; Spark(b.pos + Vector3.up * 0.05f, Vector3.up, 12, 0.9f); EmitEv("bracehit", b, 0); }
                // 逆鱗: 弾かれにくい／黒鉄の盾: 次の1発を無効に
                if (a.bzT > 0) a.vel = Vector3.Lerp(aV0, a.vel, a.evo ? 0.08f : 0.5f);
                if (b.bzT > 0) b.vel = Vector3.Lerp(bV0, b.vel, b.evo ? 0.08f : 0.5f);
                if (a.rage) a.vel = Vector3.Lerp(aV0, a.vel, 0.6f);
                if (b.rage) b.vel = Vector3.Lerp(bV0, b.vel, 0.6f);
                if (dmgToA > 0 && PasGuard(a)) { dmgToA = 0; a.vel = Vector3.Lerp(aV0, a.vel, 0.3f); }
                if (dmgToB > 0 && PasGuard(b)) { dmgToB = 0; b.vel = Vector3.Lerp(bV0, b.vel, 0.3f); }
                // 2026-11-28 本人「移動の入力方向とは逆からの攻撃されたらほんの少しノックバック強く」: 逃げる向きの後ろから当てられたら ×1.05（背後の角度と重なる時は大きい方だけ）
                if (!aAir && angA != 4 && b.specialPhase == 0 && b.inputMv.sqrMagnitude > 0.04f && Vector2.Dot(new Vector2(nh.x, nh.z), b.inputMv.normalized) >= 0.5f) { b.vel = bV0 + (b.vel - bV0) * RunHitKnock; EmitEv("runhit", b, 0); }
                if (!bAir && angB != 4 && a.specialPhase == 0 && a.inputMv.sqrMagnitude > 0.04f && Vector2.Dot(new Vector2(-nh.x, -nh.z), a.inputMv.normalized) >= 0.5f) { a.vel = aV0 + (a.vel - aV0) * RunHitKnock; EmitEv("runhit", a, 0); }
                float spA0 = a.spin, spB0 = b.spin;
                Damage(a, b, dmgToB);
                Damage(b, a, dmgToA);
                PasOnHit(a, b, dmgToB > 0); PasOnHit(b, a, dmgToA > 0);
                PartsHit(a, b, nh, aAir, bAir, vn);
                if (a.isPlayer && brains[b.idx] != null) brains[b.idx].lastPlayerT = fightT; else if (b.isPlayer && brains[a.idx] != null) brains[a.idx].lastPlayerT = fightT;
                if (practice) PracticeHit(a, b, nh, aAir, bAir, spA0 - a.spin, spB0 - b.spin);
                bool aSp = a.specialPhase == 2, bSp = b.specialPhase == 2;
                if (aSp) SpecialImpact(a, a.pos);
                if (bSp) SpecialImpact(b, b.pos);
                if (a.specialPhase == 31) ArrowHit(a, b, nh);
                if (b.specialPhase == 31) ArrowHit(b, a, -nh);
                if (a.specialPhase == 41) SpiralImpact(a, a.pos, 1f);
                if (b.specialPhase == 41) SpiralImpact(b, b.pos, 1f);
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

    // 倒れたコマとの当たり: 押し出して、重さに応じて跳ね返す（ダメージなし）
    void DownBump(Koma a, Koma b, bool dwA, bool dwB)
    {
        Vector3 d = b.Center - a.Center; Vector3 dd = new Vector3(d.x, d.y * 1.6f, d.z);
        float rr = a.Radius + b.Radius, dist = dd.magnitude;
        if (dist >= rr || dist < 1e-4f) return;
        Vector3 n = d.normalized; Vector3 nh = new Vector3(n.x, 0, n.z); if (nh.sqrMagnitude < 1e-4f) nh = Vector3.forward; else nh.Normalize();
        float pen = rr - dist, ma = a.st.mass, mb = b.st.mass;
        Vector3 pa = a.pos - nh * pen * mb / (ma + mb), pb = b.pos + nh * pen * ma / (ma + mb);
        if (Arena.WallH(pa.x, pa.z) - a.pos.y <= StepUp) a.pos = new Vector3(pa.x, a.pos.y, pa.z);
        if (Arena.WallH(pb.x, pb.z) - b.pos.y <= StepUp) b.pos = new Vector3(pb.x, b.pos.y, pb.z);
        float vn = Vector3.Dot(a.vel - b.vel, nh);
        if (vn <= 0.3f) return;
        float J = 1.3f * vn / (1f / ma + 1f / mb);
        a.vel -= nh * J / ma; b.vel += nh * J / mb;
        Vector3 cp = (a.Center + b.Center) * 0.5f; Spark(cp, Vector3.up * 0.3f, Mathf.Clamp((int)(vn * 2f), 6, 30), 0.8f);
        var sp = cam.WorldToViewportPoint(cp);
        Emit("{\"t\":\"e\",\"k\":\"wall\",\"x\":" + F(sp.x) + ",\"y\":" + F(sp.y) + "}");
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

    public static int cSpark, cDust, cFleck, cWave, cFire, cPulse;   // 検査用: 生成した数の累計
    void Spark(Vector3 at, Vector3 bias, int n, float power)
    {
        cSpark += n;
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
        cDust += n;
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
        cWave++;
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

    Koma aimTarget; float camAimT, aimHoldT, aimMarkT, lastAimC = -9; int aimCycleN;
    Transform aimMark; Material aimMarkMat;
    // C: 一番近い生きている相手へ。押すたびに次に近い相手（3秒以内）。1回合わせるだけで、その後はマウスが優先
    void AimCycle()
    {
        if (phase != "fight" || player == null || !player.alive) return;
        var list = new List<Koma>();
        foreach (var o in komas) if (o != player && o.alive && o.root.activeSelf) list.Add(o);
        if (list.Count == 0) return;
        list.Sort((a, b) => (a.pos - player.pos).sqrMagnitude.CompareTo((b.pos - player.pos).sqrMagnitude));
        aimCycleN = Time.unscaledTime - lastAimC < 3f ? (aimCycleN + 1) % list.Count : 0;
        lastAimC = Time.unscaledTime;
        aimTarget = list[aimCycleN];
        camAimT = 0.25f; aimHoldT = 0.3f; aimMarkT = 0.6f; camIdleT = -0.9f;
        EmitEv("aimc", aimTarget, aimCycleN);
    }
    void UpdateAimMark(float realDt)
    {
        if (aimMark == null)
        {
            aimMark = MeshKit.Obj("AimMark", null, MeshKit.Frustum(0.02f, 0.42f, 0.5f, 3, false), mats.AddGlow(new Color(1f, 0.9f, 0.3f, 1f))).transform;
            aimMark.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aimMarkMat = aimMark.GetComponent<MeshRenderer>().material; aimMark.gameObject.SetActive(false);
        }
        if (aimHoldT > 0) aimHoldT -= realDt;
        bool on = aimMarkT > 0 && aimTarget != null && aimTarget.alive && phase == "fight";
        if (aimMarkT > 0) aimMarkT -= realDt;
        aimMark.gameObject.SetActive(on);
        if (!on) return;
        aimMark.position = aimTarget.pos + Vector3.up * (2.1f + 0.12f * Mathf.Sin(Time.unscaledTime * 20f));
        aimMark.rotation = Quaternion.Euler(0, Time.unscaledTime * 240f, 0);
        aimMarkMat.SetColor("_TintColor", new Color(1f, 0.9f, 0.3f, 1f) * 0.9f * Mathf.Clamp01(aimMarkT / 0.2f));
    }
    // ---------- ステージ（ランダムに2つから。同じ物が3回続かない） ----------
    Light sunL; int stageForce = -1, lastStage = -1, stageRun;
    int PickStage()
    {
        int n = Arena.StageNames.Length, s;
        if (stageForce >= 0) s = Mathf.Clamp(stageForce, 0, n - 1);
        else { s = rng.Next(n); if (s == lastStage && stageRun >= 2) s = (s + 1) % n; }
        stageRun = s == lastStage ? stageRun + 1 : 1; lastStage = s;
        return s;
    }
    void ApplyStageLook(int st)
    {
        Backdrop.SetStage(st);
        bool hill = st == 1, turn = st == 2;
        if (sunL != null) { sunL.color = hill ? new Color(1f, 0.78f, 0.5f) : turn ? new Color(0.85f, 0.95f, 1f) : new Color(1f, 0.96f, 0.9f); sunL.intensity = hill ? 1.5f : 1.35f; sunL.transform.rotation = Quaternion.Euler(hill ? 38f : 72f, hill ? 55f : -25f, 0); }
        RenderSettings.ambientSkyColor = hill ? new Color(0.36f, 0.3f, 0.25f) : turn ? new Color(0.24f, 0.32f, 0.42f) : new Color(0.27f, 0.29f, 0.34f);
        RenderSettings.ambientEquatorColor = hill ? new Color(0.24f, 0.18f, 0.13f) : turn ? new Color(0.13f, 0.18f, 0.26f) : new Color(0.17f, 0.17f, 0.19f);
        RenderSettings.fogColor = hill ? new Color(0.2f, 0.12f, 0.08f) : turn ? new Color(0.03f, 0.07f, 0.12f) : new Color(0.04f, 0.045f, 0.06f);
        if (cam != null) cam.backgroundColor = hill ? new Color(0.14f, 0.08f, 0.06f) : turn ? new Color(0.02f, 0.05f, 0.09f) : new Color(0.03f, 0.035f, 0.045f);
    }
    float camIdleT; public float RecenterHalf = 1.0f; const float RecenterIdle = 0.6f, RecenterR = 0.68f;
    Vector3 camVel, camLook;
    float camYaw, camElev = 0.43f, fovKick, camFov = 60f; bool camInit, mouseLook, sprint; float touchLookT;
    // 操作の向きをカメラ基準にする（上＝カメラの向いている前）
    Vector2 CamRel(Vector2 v)
    {
        if (!ChaseOn) return v;
        float s = Mathf.Sin(camYaw), c = Mathf.Cos(camYaw);
        return new Vector2(v.x * c + v.y * s, -v.x * s + v.y * c);
    }
    bool ChaseOn => player != null && (phase == "count" || phase == "fight" || phase == "end");
    // ---------- 観戦（誰を見るか だけが入力。2026-11-28） ----------
    // 将来オンラインで他人の試合を観る時も、SpectateSet(idx) と CamSubject() だけで同じ画面が使える
    Koma specTarget, endSubject; bool spectating, pAliveLast = true; float deathT = -1f, camClose; int placeAtDeath, totalK;
    const float EndHold = 2.3f;
    public Koma CamSubject()
    {
        if (phase == "end" && endSubject != null) return endSubject;
        if (player.alive) return player;
        if (spectating && specTarget != null && specTarget.alive) return specTarget;
        return player;
    }
    void SpectateSet(int idx) { if (idx >= 0 && idx < komas.Count && komas[idx] != player && komas[idx].alive && komas[idx].root.activeSelf) specTarget = komas[idx]; }
    void PickSpec(Vector3 near)
    {
        Koma best = null; float bd = 1e9f;
        foreach (var o in komas) { if (o == player || !o.alive || !o.root.activeSelf) continue; float d = (o.pos - near).sqrMagnitude; if (d < bd) { bd = d; best = o; } }
        specTarget = best;
    }
    void SpectateStep(int dir)
    {
        var list = new List<Koma>(); foreach (var o in komas) if (o != player && o.alive && o.root.activeSelf) list.Add(o);
        if (list.Count == 0) return;
        int i = specTarget != null ? list.IndexOf(specTarget) : -1;
        specTarget = list[((i + (dir >= 0 ? 1 : -1)) % list.Count + list.Count) % list.Count];
    }
    // 本人のダウン・勝ちの余韻用のスロー（決着の演出だけ。ふだんの当たりでは使わない）
    void SlowFx(float scale, float dur) { slowScale = scale; slowUntil = Time.unscaledTime + dur; }

    // 2026-11-25 本人「臨場感が欲しいから、車のゲームみたいな感じで視点を変えて」: 自分のコマの後ろ・低めから追う。
    // 向きは進む向きにゆっくり付いていく（ぶつかって跳ね返った時は振り回さない）
    void ChaseCamera(float realDt)
    {
        var subj = CamSubject();
        Vector3 p = subj.pos;
        Vector2 hv = new Vector2(subj.vel.x, subj.vel.z);
        bool closeWant = (!player.alive && deathT >= 0 && deathT < 1.6f && !spectating) || phase == "end";
        camClose = Mathf.MoveTowards(camClose, closeWant ? 1f : 0f, realDt * 2.2f);
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
        // C で合わせる: 0.25秒で滑らかに相手の方へ（毎フレーム相手の今の位置を見る）
        if (camAimT > 0)
        {
            camAimT -= realDt;
            if (aimTarget != null && aimTarget.alive)
            {
                Vector3 dd = aimTarget.pos - p; float wantA = Mathf.Atan2(dd.x, dd.z);
                float dA = Mathf.DeltaAngle(camYaw * Mathf.Rad2Deg, wantA * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                camYaw += dA * Mathf.Clamp01(realDt / Mathf.Max(camAimT + realDt, 0.02f));
            }
            camIdleT = Mathf.Min(camIdleT, -0.9f);
        }
        // 2026-11-28 本人「一番外側にいてカメラを止めた時、ゆっくりステージ中央にカメラが合わさる」: 外周付近（半径の68%以上）で、
        // マウス・指でカメラを0.6秒動かしていない時、向きだけ中心へ寄せる（半分寄るのに約1秒・上下は触らない）。狙っている間・必殺中・自動追従中は寄せない
        camIdleT += realDt;
        {
            float rp = new Vector2(p.x, p.z).magnitude;
            bool followNow = phase == "fight" && !mouseLook && hv.magnitude > 2.5f && touchLookT <= 0 && !touchAimOn;
            if (phase == "fight" && camIdleT > RecenterIdle && rp >= Arena.R * RecenterR && !touchAimOn && !followNow && player.specialPhase == 0 && !snap && subj == player)
            {
                float wantC = Mathf.Atan2(-p.x, -p.z);
                float dC = Mathf.DeltaAngle(camYaw * Mathf.Rad2Deg, wantC * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                camYaw += dC * (1f - Mathf.Exp(-0.693f / RecenterHalf * realDt));
            }
        }
        Vector3 fwd = new Vector3(Mathf.Sin(camYaw), 0, Mathf.Cos(camYaw));
        float portrait = cam.aspect < 1.2f ? Mathf.Lerp(1.45f, 1f, Mathf.InverseLerp(0.45f, 1.2f, cam.aspect)) : 1f;
        float baseY = Mathf.Max(p.y, -3f);
        float camD = 7.9f * Mathf.Lerp(1f, 0.62f, camClose);
        Vector3 want3 = new Vector3(p.x, baseY, p.z) + (-fwd * Mathf.Cos(camElev) + Vector3.up * Mathf.Sin(camElev)) * camD * portrait;
        // 2026-11-28 本人の写真「外周で、床が台に乗った土手に見えて、外側の分厚い壁がむき出し」: 外周で中央を向くとカメラが縁の外へ出て、壁を外から見ていた。
        // カメラは縁の上（縁の内側の高さ）より外へ出さず、縁より高くして、壁越しに中を見下ろす
        { float rc = new Vector2(want3.x, want3.z).magnitude, rl = Arena.Edge - 0.4f;
          if (rc > rl) { want3.x *= rl / rc; want3.z *= rl / rc; }
          if (rc > Arena.R - 3f) want3.y = Mathf.Max(want3.y, Arena.Bowl(Arena.R) + (Arena.Stage == 1 ? 0.9f : Arena.LipH) + 1.6f); }
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
          .Append(",\"sw\":").Append(spectating ? 1 : 0).Append(",\"spec\":").Append(spectating && specTarget != null ? specTarget.idx : -1).Append(",\"pz\":").Append(paused ? 1 : 0).Append(",\"ts\":").Append(F(Time.timeScale)).Append(",\"k\":[");
        bool first = true;
        foreach (var k in komas)
        {
            if (!k.root.activeSelf) continue;
            if (!first) sb.Append(','); first = false;
            var sp = cam.WorldToViewportPoint(k.pos + Vector3.up * 1.25f);
            sb.Append("{\"i\":").Append(k.idx).Append(",\"a\":").Append(k.alive ? 1 : 0).Append(",\"sp\":").Append(F(k.SpinRatio))
              .Append(",\"pb\":[").Append(F(k.pb0 / Mathf.Max(1f, k.pbm0))).Append(',').Append(F(k.pb1 / Mathf.Max(1f, k.pbm1))).Append(',').Append(F(k.pb2 / Mathf.Max(1f, k.pbm2))).Append(']')
              .Append(",\"nd\":").Append(F(NeedOf(k) / GaugeCap)).Append(",\"g\":").Append(F(k.gauge / GaugeCap)).Append(",\"bz\":").Append(k.bzT > 0 ? (k.evo ? 2 : 1) : k.bzStagger > 0 ? 3 : 0).Append(",\"ev\":").Append(EvoReady(k) ? 1 : 0).Append(",\"ec\":").Append(k.isPlayer ? F(evoCharge) : "0").Append(",\"ok\":").Append(SpecialReady(k) ? 1 : 0).Append(",\"fz\":").Append(k.frostT > 0 ? 1 : 0).Append(",\"br\":").Append(k.Bracing ? 1 : 0).Append(",\"rc\":").Append(k.sp == 7 && SpecialReady(k) && IsFalling(k) && NearestWithin(k, RecoverRange) != null ? 1 : 0).Append(",\"cd\":").Append(F(Mathf.Clamp01(k.dashCd / (DashCdBase * k.st.dashCd))))
              .Append(",\"x\":").Append(F(sp.x)).Append(",\"y\":").Append(F(sp.y)).Append(",\"z\":").Append(F(sp.z))
              .Append(",\"vx\":").Append(F(k.vel.x)).Append(",\"vz\":").Append(F(k.vel.z)).Append(",\"yaw\":").Append(F(camYaw)).Append(",\"air\":").Append(k.grounded ? 0 : 1).Append(",\"h\":").Append(F(k.pos.y)).Append(",\"gh\":").Append(F(Arena.H(k.pos.x, k.pos.z))).Append(",\"wx\":").Append(F(k.pos.x)).Append(",\"wz\":").Append(F(k.pos.z)).Append(",\"r\":").Append(F(new Vector2(k.pos.x, k.pos.z).magnitude))
              .Append(",\"ps\":").Append(k.pas).Append(",\"wg\":").Append(k.WingsOn ? 1 : 0).Append(",\"rg\":").Append(k.rage ? 1 : 0).Append(",\"sn\":").Append(k.stampN).Append(",\"lf\":").Append(k.left ? 1 : 0)
              .Append(",\"p\":[").Append(k.top).Append(',').Append(k.core).Append(',').Append(k.leg).Append("]")
              .Append(",\"why\":\"").Append(k.koWhy).Append("\"");
            if (debugState && k.idx == 0) sb.Append(",\"fx\":[").Append(cSpark).Append(',').Append(cDust).Append(',').Append(cFleck).Append(',').Append(cWave).Append(',').Append(cFire).Append(',').Append(cPulse).Append(',').Append(sparks.particleCount).Append(',').Append(dust.particleCount).Append(',').Append(fireP.particleCount).Append(',').Append(ActiveWaves()).Append(',').Append(chains.Count).Append(',').Append(ActiveRings()).Append("]");
            if (debugState) sb.Append(",\"dg\":[").Append(F(k.dashStop)).Append(',').Append(F(k.dashGuardT)).Append(',').Append(F(k.dashBrakeT)).Append("]");
            if (debugState) sb.Append(",\"wh\":").Append(F(Arena.WallH(k.pos.x, k.pos.z))).Append(",\"cc\":").Append(F(k.clingCd)).Append(",\"wy\":").Append(F(k.pos.y)).Append(",\"tl\":").Append(F(k.TiltDeg)).Append(",\"L\":[").Append(F(k.lossHit)).Append(',').Append(F(k.lossWall)).Append(',').Append(F(k.lossDash)).Append(',').Append(F(k.spinMax)).Append("]");
            sb.Append('}');
        }
        sb.Append("]");
        if (debugState)
        {
            // 検査用: 描画範囲（bounds）が大きい物（ステージ並み）を拾う
            bigT -= realDt;
            if (bigT <= 0)
            {
                bigT = 0.4f; bigS = "";
                foreach (var r in FindObjectsOfType<Renderer>())
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    string n = r.gameObject.name;
                    if (n == "Sky" || n == "Stands" || n == "Floor" || n == "FloorPiece" || n == "Base" || n == "Arc" || n == "Board" || n == "Tower" || n == "Lamp" || n == "WarnRing" || n == "PitPiece" || n == "PitWall") continue;
                    float m = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
                    if (m > 16f) bigS += (bigS.Length > 0 ? "," : "") + "\"" + n + ":" + Mathf.RoundToInt(m) + "\"";
                }
            }
            sb.Append(",\"big\":[").Append(bigS).Append(']');
        }
        sb.Append("}");
        Emit(sb.ToString());
    }
    float bigT; string bigS = "";
}
