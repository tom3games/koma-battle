# koma-battle 途中経過 2026-11-26b
- 開始。読んだ: game-rules / knowledge-games-3d / feedback-tests-silent、ui.js・ui.css・Koma.cs・KomaBoot.cs
- 方針: 着地は Koma/KomaBoot の物理＋見た目の揺れ、スタート画面はハブ（ホーム/装備/育成/設定）に分割、レベルは localStorage
- 着地: Koma.Land（傾き・歳差・潰れ）＋KomaBoot.Landed（反発0.28・水平の摩擦・回転減り・土ぼこり・火花・輪・音イベント）実装。land.mjs で測定（直す前: 反発0.9-1.4m・水平速度そのまま・回転減り0・傾き0）。左回転（Koma.left・刃の鏡写し・逆回り・当たりの流れ4.2・右回りへ+12%）実装。必殺の演出（オーラ・光・焦げ跡・輪3つ・ズーム・揺れ）をC#側に実装・ビルド成功。次: UI（ハブ・レベル・HUD）
- UI ハブ（ホーム/装備/育成/設定）・レベル（Lv7まで）・HUD ゲージ撤去・他人の技名なし・効果音1.7倍+コンプ 実装。hubshots.mjs 全サイズ収まり OK。次: 試合の検査・演出の実画面
