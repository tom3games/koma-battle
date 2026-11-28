using UnityEngine;

// 2026-11-28 パーツ破損（本人の依頼）: コマを3パーツに分けて耐久を持たせる。決着の新しい形は「軸折れ」（why = shaft）
//   刃（外周）  横の体当たりで削れる。壊れると攻撃力が下がる（弾きも弱くなる）。欠けた刃が火花と飛ぶ
//   円盤（中）  上から（空中の突撃・メテオ）で削れる。壊れると軽くなり、吹き飛びやすくなる。ひびと破片
//   軸（下）    かち上げで削れる。壊れかけはふらつき・回転減りが増え、折れたらその場でダウン
//   型ごとの硬さ: 攻撃型=刃が固い・重量型=円盤が固い・速度型=軸が固い（Koma.RecalcParts）
public partial class KomaBoot
{
    // 数字の調整はここ
    const float BladeBase = 0.36f, BladeVn = 0.12f;     // 横の当たり1回で削れる量 = Base + 当たりの強さ×Vn
    const float DiskAir = 6.5f, DiskMeteor = 6f;       // 空中からの突撃・メテオの1回の量
    const float ShaftUpper = 5f;                      // かち上げ1回の量
    const float BladeBrokenAtk = 0.72f, DiskBrokenMass = 0.7f, ShaftLow = 0.3f, ShaftLowDecay = 1.5f;

    void PartWear(Koma k, int part, float amt)
    {
        if (!k.alive || phase != "fight" || amt <= 0f) return;
        k.pbLast[part] += amt;
        // 削れた時の小さな火花と破片（壊れた時は PartBreak でもっと派手に）
        { Vector3 c0 = k.Center; int sn = Mathf.Clamp((int)(amt * 3f), 4, 16);
          Spark(part == 2 ? k.pos + Vector3.up * 0.05f : c0, Vector3.up * 0.4f, sn, 0.8f);
          if (amt > 1.5f) for (int i = 0; i < 2; i++) Fleck(c0, Random.onUnitSphere * 3f + Vector3.up * 1.5f, new Color(0.8f, 0.8f, 0.85f), 0.1f, 0.5f, true); }
        if (part == 0)
        {
            if (k.pb0 <= 0f) return;
            k.pb0 -= amt; if (k.pb0 <= 0f) { k.pb0 = 0f; k.bladeBroken = true; k.baseAtk = k.atk0 * BladeBrokenAtk; PartBreak(k, 0); }
        }
        else if (part == 1)
        {
            if (k.pb1 <= 0f) return;
            k.pb1 -= amt; if (k.pb1 <= 0f) { k.pb1 = 0f; k.diskBroken = true; k.st.mass = k.mass0 * DiskBrokenMass; PartBreak(k, 1); }
        }
        else
        {
            if (k.pb2 <= 0f) return;
            k.pb2 -= amt;
            if (k.pb2 <= 0f)
            {
                k.pb2 = 0f; PartBreak(k, 2);
                k.spin = 0; KO(k, "shaft");   // 軸が折れたらその場でダウン（不屈は回転0＝"spin" の時だけなので働かない）
            }
        }
    }

    void PartBreak(Koma k, int part)
    {
        EmitEv("pbreak", k, part);
        Vector3 c = k.Center;
        if (part == 0)
        {
            // 欠けた刃が火花と一緒に飛ぶ
            Spark(c, Vector3.up * 0.4f, 120, 2.4f); Pulse(k.pos, new Color(1f, 0.8f, 0.4f), 6f, 12f); Wave(k.pos, 4f);
            for (int i = 0; i < 14; i++) { float a = Random.value * 6.28f; Fleck(c, new Vector3(Mathf.Cos(a) * 6f, Random.Range(2f, 6f), Mathf.Sin(a) * 6f), new Color(0.85f, 0.85f, 0.9f), 0.2f, 0.9f, true); }
        }
        else if (part == 1)
        {
            // ひびと破片
            Dust(k.pos, 30); Spark(c, Vector3.up, 80, 1.8f); Pulse(k.pos, new Color(0.7f, 0.85f, 1f), 6f, 12f); Wave(k.pos, 4f);
            for (int i = 0; i < 20; i++) Fleck(c, Random.onUnitSphere * 4f + Vector3.up * 2f, new Color(0.55f, 0.55f, 0.6f), 0.22f, 1.1f, false);
        }
        else
        {
            // 軸が折れた: 床に火花の線
            Spark(k.pos + Vector3.up * 0.05f, Vector3.up * 0.3f, 140, 2.4f); Pulse(k.pos, new Color(1f, 0.7f, 0.3f), 8f, 14f); Wave(k.pos, 5f);
            // 折れた軸が飛ぶ
            for (int i = 0; i < 3; i++) Fleck(c, new Vector3(Random.Range(-5f, 5f), Random.Range(6f, 10f), Random.Range(-5f, 5f)), new Color(0.95f, 0.85f, 0.6f), 0.38f, 1.2f, true);
            SlowFx(0.3f, 0.55f);
            for (int i = 0; i < 14; i++) { float a = Random.value * 6.28f; Fleck(k.pos + Vector3.up * 0.05f, new Vector3(Mathf.Cos(a) * 5f, 0.6f, Mathf.Sin(a) * 5f), new Color(1f, 0.7f, 0.25f), 0.12f, 0.6f, true); }
        }
        if (k.isPlayer) shake = Mathf.Max(shake, 0.5f);
    }

    // 当たった瞬間（Collide から）。ふつうの横の当たりは刃、上からは円盤（受けた側）、かち上げは軸（受けた側）
    void PartsHit(Koma a, Koma b, Vector3 nh, bool aAir, bool bAir, float vn)
    {
        for (int i = 0; i < 3; i++) { a.pbLast[i] = 0; b.pbLast[i] = 0; }
        bool upA = Upper(a, nh) && !aAir, upB = Upper(b, -nh) && !bAir;
        if (aAir) PartWear(b, 1, DiskAir * a.st.aerial);
        else if (bAir) PartWear(a, 1, DiskAir * b.st.aerial);
        if (upA) PartWear(b, 2, ShaftUpper);
        if (upB) PartWear(a, 2, ShaftUpper);
        if (!aAir && !bAir && !upA && !upB)
        {
            float w = BladeBase + Mathf.Min(vn, 24f) * BladeVn;
            PartWear(a, 0, w); PartWear(b, 0, w);
        }
    }

    // 毎フレーム（回転の減りに足す倍率）: 軸が壊れかけだと回転が速く減る
    float ShaftDecayMul(Koma k) => k.pbm2 > 0f && k.pb2 < k.pbm2 * ShaftLow ? ShaftLowDecay : 1f;
}
