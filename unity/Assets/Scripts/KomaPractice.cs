using UnityEngine;

// 2026-11-28 練習場（本人のアイデア）: 時間無し・負け無し。ダミー3種（動かない／歩き回る／軽く当ててくる）。
// 当てた時に押し出した距離・削った回転・自分の残り回転を数字で出し（pstat）、倒れたらすぐ戻す。必殺はすぐたまる
public partial class KomaBoot
{
    bool practice; int dummyType; Koma dummy;
    readonly float[] pracDeadT = new float[4];
    float pracGoalT; Vector3 pracGoal;
    // 当てた時の計測: 当てた瞬間の相手の位置・回転 → 1.6秒後に 押し出した距離・削った回転(%) を出す
    float pracWinT = -1f, pracSpin0; Vector3 pracPos0;

    void PracticeSetup()
    {
        // ダミーの装備は固定（標準的な型）
        dummy = komas[1];
        dummy.Rebuild(3, 1, 0, mats, false); dummy.sp = 0; dummy.pas = -1;
        pracWinT = -1f;
        for (int i = 0; i < 4; i++) pracDeadT[i] = 0;
        pracGoalT = 0;
    }

    // 毎フレーム（fight の間）
    void PracticeUpdate(float dt, float realDt)
    {
        if (phase != "fight") return;
        player.gauge = Mathf.Min(100f, player.gauge + 45f * dt);   // 必殺はすぐたまる
        if (dummy != null && dummy.alive) dummy.gauge = 0f;
        // 倒れたらすぐ戻す（負け無し）
        for (int i = 0; i < 2; i++)
        {
            var k = komas[i];
            if (k.alive) { pracDeadT[i] = 0; continue; }
            pracDeadT[i] += realDt;
            if (pracDeadT[i] > 1.3f)
            {
                float x = i == 0 ? -6f : 6f;
                k.Place(new Vector3(x, Arena.H(x, 0) + 2.5f, 0));
                if (i == 0) { pAliveLast = true; deathT = -1f; }
                pracDeadT[i] = 0;
            }
        }
        // 当てた計測の締め
        if (pracWinT >= 0f)
        {
            pracWinT += realDt;
            if (pracWinT > 1.6f && dummy != null)
            {
                Vector3 d = dummy.pos - pracPos0; d.y = 0;
                float cut = (pracSpin0 - dummy.spin) / Mathf.Max(1f, dummy.spinMax) * 100f;
                Emit("{\"t\":\"e\",\"k\":\"pstat\",\"dist\":" + F(d.magnitude) + ",\"cut\":" + F(Mathf.Max(0f, cut)) + ",\"me\":" + F(player.SpinRatio * 100f) + "}");
                pracWinT = -1f;
            }
        }
        else if (dummy != null && dummy.alive) dummy.spin = Mathf.Min(dummy.spinMax, dummy.spin + dummy.spinMax * 0.06f * dt);   // ダミーの回転はゆっくり戻る
    }

    // 当たった瞬間（Collide から）: 自分とダミーのぶつかりを計測し始める
    void PracticeHit(Koma a, Koma b)
    {
        if (!practice || dummy == null) return;
        if (!((a.isPlayer && b == dummy) || (b.isPlayer && a == dummy))) return;
        if (pracWinT >= 0f) return;   // 計測中は重ねない
        pracWinT = 0f; pracPos0 = dummy.pos; pracSpin0 = dummy.spin;
    }

    // ダミーの動き
    void PracticeThink(Koma k, float dt, out Vector2 move, out bool dash)
    {
        move = Vector2.zero; dash = false;
        var b = brains[k.idx];
        if (dummyType == 0) return;
        if (dummyType == 1)
        {
            pracGoalT -= dt;
            if (pracGoalT <= 0f || (new Vector2(k.pos.x - pracGoal.x, k.pos.z - pracGoal.z)).magnitude < 1f)
            {
                pracGoalT = 2.5f + (float)rng.NextDouble() * 2f;
                float a = (float)rng.NextDouble() * Mathf.PI * 2, r = 3f + (float)rng.NextDouble() * 6f;
                pracGoal = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            }
            Vector2 w = new Vector2(pracGoal.x - k.pos.x, pracGoal.z - k.pos.z);
            move = w.normalized * 0.55f;
            return;
        }
        // 軽く当ててくる: 自分へ向かい、たまにだけダッシュ
        Vector3 to = player.pos - k.pos; to.y = 0;
        move = new Vector2(to.x, to.z).normalized * 0.8f;
        b.react -= dt;
        if (k.grounded && k.dashCd <= 0 && to.magnitude < 7f && b.react <= 0) { dash = true; b.react = 3.5f; }
    }
}
