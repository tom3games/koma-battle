using UnityEngine;

// 2026-11-28 練習場（本人のアイデア）: 時間無し・負け無し。ダミー3種（動かない／ゆっくり回る／軽く当ててくる）。
// 当てた時に削った回転・初速を数字で出し（phit）、1.6秒後に押し出した距離（pstat）。倒れたらすぐ戻す。必殺はすぐたまる
public partial class KomaBoot
{
    bool practice; int dummyType; Koma dummy;
    readonly float[] pracDeadT = new float[4];
    float pracGoalT, pracAng; Vector3 pracGoal;
    float pracWinT = -1f, pracSpin0; Vector3 pracPos0;

    void PracticeSetup()
    {
        dummy = komas[1];
        dummy.Rebuild(3, 1, 0, mats, false); dummy.sp = 0; dummy.pas = -1;
        pracWinT = -1f; pracGoalT = 0; pracAng = 0;
        for (int i = 0; i < 4; i++) pracDeadT[i] = 0;
        player.Place(new Vector3(-6f, Arena.H(-6f, 0) + 2.5f, 0));
        dummy.Place(new Vector3(6f, Arena.H(6f, 0) + 2.5f, 0));
        brains[1] = new Brain { react = 2f };
    }

    // 毎フレーム（fight の間）
    void PracticeUpdate(float dt, float realDt)
    {
        if (phase != "fight") return;
        if (player.alive) { player.gauge = Mathf.Min(100f, player.gauge + 45f * dt); player.spin = Mathf.Min(player.spinMax, player.spin + player.spinMax * 0.05f * dt); }   // 必殺はすぐたまる・回転も少しずつ戻る
        if (dummy != null && dummy.alive) { dummy.gauge = 0f; dummy.spin = Mathf.Min(dummy.spinMax, dummy.spin + dummy.spinMax * 0.06f * dt); }
        // 動かない的: 当てられて1.2秒たったら止まる（すり鉢で転がらない）
        if (dummyType == 0 && dummy != null && dummy.alive && dummy.grounded && fightT - dummy.lastHitT > 1.2f) { dummy.vel.x = 0; dummy.vel.z = 0; }
        for (int i = 0; i < 2; i++)
        {
            var k = komas[i];
            if (k.alive) { pracDeadT[i] = 0; continue; }
            pracDeadT[i] += realDt;
            if (pracDeadT[i] > 1.3f)
            {
                float x = i == 0 ? -6f : 6f;
                k.Place(new Vector3(x, Arena.H(x, 0) + 2.5f, 0));
                pracDeadT[i] = 0;
            }
        }
        if (pracWinT >= 0f)
        {
            pracWinT += realDt;
            if (pracWinT > 1.6f && dummy != null)
            {
                Vector3 d = dummy.pos - pracPos0; d.y = 0;
                float cut = (pracSpin0 - dummy.spin) / Mathf.Max(1f, dummy.spinMax) * 100f;
                Emit("{\"t\":\"e\",\"k\":\"pstat\",\"dist\":" + F(d.magnitude) + ",\"cut\":" + F(Mathf.Max(0f, cut)) + "}");
                pracWinT = -1f;
            }
        }
    }

    // 当たった瞬間（Collide から）。lossA/lossB はそれぞれが減らした回転の量
    void PracticeHit(Koma a, Koma b, Vector3 nh, bool aAir, bool bAir, float lossA, float lossB)
    {
        if (dummy == null) return;
        if (!((a.isPlayer && b == dummy) || (b.isPlayer && a == dummy))) return;
        bool pa = a.isPlayer;
        Koma me = pa ? a : b;
        float toDummy = pa ? lossB : lossA, toMe = pa ? lossA : lossB;
        bool air = pa ? aAir : bAir, up = pa ? Upper(a, nh) && !aAir : Upper(b, -nh) && !bAir;
        Vector3 dv = dummy.vel; dv.y = 0;
        Emit("{\"t\":\"e\",\"k\":\"phit\",\"dd\":" + F(Mathf.Max(0f, toDummy) / Mathf.Max(1f, dummy.spinMax) * 100f) + ",\"dm\":" + F(Mathf.Max(0f, toMe) / Mathf.Max(1f, me.spinMax) * 100f)
             + ",\"v\":" + F(dv.magnitude) + ",\"dash\":" + (me.dashT > 0 ? 1 : 0) + ",\"air\":" + (air ? 1 : 0) + ",\"up\":" + (up ? 1 : 0) + ",\"sp\":" + (me.specialPhase >= 2 ? 1 : 0) + "}");
        if (pracWinT >= 0f) return;
        pracWinT = 0f; pracPos0 = dummy.pos; pracSpin0 = dummy.spin + toDummy;
    }

    // ダミーの動き
    void PracticeThink(Koma k, float dt, out Vector2 move, out bool dash)
    {
        move = Vector2.zero; dash = false;
        if (k != dummy || dummyType == 0) return;
        var b = brains[k.idx];
        if (dummyType == 1)
        {
            // ゆっくり中央のまわりを回る
            pracAng += 0.32f * dt;
            pracGoal = new Vector3(Mathf.Cos(pracAng) * 7f, 0, Mathf.Sin(pracAng) * 7f);
            Vector2 w = new Vector2(pracGoal.x - k.pos.x, pracGoal.z - k.pos.z);
            move = w.magnitude > 0.2f ? w.normalized * Mathf.Min(0.6f, w.magnitude * 0.3f + 0.2f) : Vector2.zero;
            return;
        }
        // 軽く当ててくる: 自分へ向かい、たまにだけダッシュ
        Vector3 to = player.pos - k.pos; to.y = 0;
        move = new Vector2(to.x, to.z).normalized * 0.7f;
        b.react -= dt;
        if (k.grounded && k.dashCd <= 0 && to.magnitude < 6f && b.react <= 0) { dash = true; b.react = 6f; }
    }
}
