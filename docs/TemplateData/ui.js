// 画面（HTML）側: 読み込み・タイトル・パーツ選び・HUD・スマホの操作・演出・音・結果。3D と物理は Unity 側
window.__jsUp = true
const UI_V = (() => { try { return new URL(document.currentScript.src).searchParams.get('v') } catch (e) { return '' } })()
;(() => {
  const $ = id => document.getElementById(id)
  const SLOTS = [
    { key: 'top', name: '上', parts: [
      { n: 'ブラッドファング', d: '攻め重視', f: '鋼の牙4本。当てた時の削りが大きい。守りは薄め。', x: '攻撃×1.35 防御×0.85 重さ×1.0' },
      { n: 'アイギスウォール', d: '受けに強い', f: '分厚く重い盾。弾かれにくく、受けの戦いに向く。攻撃は控えめ。', x: '攻撃×0.85 防御×1.4 重さ×1.3' },
      { n: 'ゼファーウイング', d: '空中で曲がる', f: '軽い羽。空中で向きを変えやすく、上からの一撃もやや強い。', x: '攻撃×1.05 防御×0.95 重さ×0.82 空中の操作×1.7 急降下×1.15' },
      { n: 'ヴォーテックスエッジ', d: '万能型', f: '6枚刃。攻めも守りもそこそこ。迷ったらこれ。', x: '攻撃×1.15 防御×1.1 重さ×1.05' },
      { n: 'ルナティックサイズ', d: '一撃が最強', f: '大鎌2枚。当たれば最強だが、守りは紙。', x: '攻撃×1.5 防御×0.75 重さ×1.15 速さ×0.95' } ] },
    { key: 'core', name: '芯', parts: [
      { n: 'エターナルコア', d: '長く回る', f: 'よく回り続ける芯。持久戦に強いが、動きは少し鈍い。', x: '持久×1.35 速さ×0.92' },
      { n: 'ソニックコア', d: '速い', f: '素早い芯。ダッシュの待ち時間が短い。回転は減りやすい。', x: '速さ×1.22 持久×0.85 ダッシュ待ち×0.75' },
      { n: 'グラビティコア', d: '重い', f: '重い芯。押し負けにくい。', x: '重さ+0.35 防御×1.1 速さ×0.95' },
      { n: 'フェニックスコア', d: '必殺が早い', f: '必殺のゲージが早くたまる。', x: '必殺のたまり×1.55 持久×0.95' },
      { n: 'アビスコア', d: '回転を吸う', f: '当てて削った回転の一部を自分に戻す。', x: '削った分の35%を回復 重さ+0.15' } ] },
    { key: 'leg', name: '足', parts: [
      { n: 'タイタンベース', d: '踏ん張る', f: '床をつかんで飛ばされにくい。跳びは低く、動きも少し遅い。', x: '踏ん張り×1.7 跳び×0.85 速さ×0.95' },
      { n: 'スティンガーニードル', d: 'よく滑る', f: '細い先端でよく滑り、回転が減りにくい。踏ん張りは弱い。', x: '踏ん張り×0.6 速さ×1.15 回転の減り×0.7' },
      { n: 'ヘヴンズスプリング', d: '高く跳ぶ', f: 'バネの足。高く跳べて、上からの一撃が重い。', x: '跳び×1.45 急降下の威力×1.3' },
      { n: 'ファントムホイール', d: '最速', f: '最速の足。そのぶん回転が減りやすい。', x: '速さ×1.3 踏ん張り×0.8 回転の減り×1.15' },
      { n: 'クレイモアスパイク', d: '踏ん張りと跳び', f: '踏ん張りと跳びが、どちらも少し高い。', x: '踏ん張り×1.3 跳び×1.15 急降下×1.15' } ] },
    { key: 'dir', name: '回転', parts: [
      { n: '右回転', d: '標準', f: 'ふつうの回り。クセがなく扱いやすい。', x: '特別な効果なし' },
      { n: '左回転', d: '逆回り', f: '逆回り。右回りの相手を強く削り、当たると相手が横へ流れる。', x: '右回りの相手へのダメージ+12% 相手を流す力×2.8 自分の回転の総量×0.9' } ] },
    { key: 'sp', name: '必殺', parts: [
      { n: 'メテオ・ジャッジメント', d: '真上から落下', f: '真上へ跳んで相手めがけて落ちる。着地で衝撃波が広がる。', x: '範囲6m ダメージ10（攻撃÷防御）' },
      { n: 'テンペスト・ヴォルテックス', d: '竜巻で巻き上げ', f: '竜巻で周りを引き寄せて巻き上げ、最後に吹き飛ばす。', x: '3.5秒 範囲8.5m 自分の回転+15%' },
      { n: 'ファントム・レイド', d: '3連続の突進', f: '近い相手へ3回続けて突進する。', x: '突進3回 威力×1.35' },
      { n: 'イージス・リバース', d: '盾で弾き返す', f: '3秒の盾。受けるダメージを大きく減らし、触れた相手を弾き返し、回転を戻す。', x: '3.2秒 ダメージ-80% 回転+35%' },
      { n: 'グラビティ・アンカー', d: '重くなって動かない', f: '5秒だけ超重量になって地面に刺さる。当てられても動かず、当てた相手の方が吹っ飛ぶ。縁の攻防で強い。', x: '5秒 重さ×14 受けるダメージ-65% 移動が遅くなる' },
      { n: 'ラスト・スパイラル', d: 'ピンチの逆転の一撃', f: '回転が残り3割を切った時だけ撃てる。回転が少ないほど威力が上がる。撃てる時だけゲージが光る。', x: '残り30%で威力×1.0、0%に近いほど最大×2.4 範囲5.5m' },
      { n: 'グレイシャル・アロー', d: '氷の矢で突進', f: '狙った向きに輪が並び、開き終わると氷の矢になって一直線に突進する。当たった相手は弾かれて凍える。輪が開いている間に避けられる。', x: '展開0.95秒 突進約16m 凍え2秒（速さ・加速が半分）' },
      { n: 'チェイン・バインド', d: '鎖で振り回して投げる', f: '一番近い相手と鎖でつながる。自分が回るほど相手を振り回し、最後に狙いの向きへ放り投げる。つながれた側もダッシュで引っ張り返せる。', x: '4秒 鎖の長さ6.8m 回した数だけ遠くへ' },
      { n: 'ソニック・トレイル', d: '光の軌跡で削る', f: '5秒のあいだ、通った道に光の帯が残る。帯は3秒で消え、触れた相手を削って弾く。自分は当たらない。相手の周りを駆け抜けるように使う。', x: '5秒 帯は3秒残る 触れるとダメージ6＋弾き' },
      { n: 'ファントム・ミラージュ', d: '光の分身が2体', f: '6秒のあいだ、左右に光の分身が2体ついて同じ動きをする。分身にも当たり判定があり、相手に触れるとダメージ。', x: '6秒 分身2体 触れるとダメージ2.5＋速さ分' } ] },
    { key: 'pas', name: 'パッシブ', parts: [
      { n: 'なし', d: 'なし', f: 'パッシブを使わない。', x: '' },
      { n: '逆鱗', d: 'ピンチで強化', f: '回転が3割を切ると赤黒いオーラが噴き出し、芯が赤く光る。', x: '回転30%以下で発動 攻撃+25% 弾かれにくい' },
      { n: '天翔', d: '空中で強化', f: '空中にいる間、光の羽が広がる。急降下が強くなり、着地で衝撃波が出る。', x: '急降下のダメージ+30% 着地の衝撃波 範囲4.5m' },
      { n: '魂喰らい', d: '当てて回復', f: '当てた相手から光の粒を吸い出し、削った回転の一部を自分に戻す。', x: '削った分の40%を回復' },
      { n: '雷鳴の刻印', d: '3回当てて雷', f: '同じ相手に当てるたび紫の紋章がつき、3つで雷が落ちる。落ちる前に床へ円が出るので、円の外へ逃げれば外れる。円の中にいる全員に当たる（使った本人は除く）。', x: '予告0.55秒 範囲2.4m ダメージ14 当たると0.9秒止まる' },
      { n: '不屈', d: '1度だけ復活', f: '1試合に1度、回転が0になる瞬間に踏みとどまり、回転15%で復活する。場外（コースアウト）では発動しない。', x: '1試合1回 回転15%で復活 場外では発動しない' },
      { n: '紅蓮の轍', d: '炎の跡', f: '回転が7割以上の間、通った跡に炎が残り、踏んだ相手の回転を少しずつ削る。', x: '回転70%以上で発動 炎は2.5秒残る' },
      { n: '影分身', d: '残像で弾く', f: 'ダッシュの後に残像が0.5秒その場に残り、触れた相手を弾く。', x: '残像0.5秒 ダメージ5' },
      { n: '黒鉄の盾', d: '次の1発を無効', f: '3回当てられるごとに六角形の光の盾が浮かび、次の1発を無効にして砕ける。', x: '3回被弾ごとに1枚（最大1枚）' } ] },
  ]
  const NAMES = ['あなた', 'クリムゾン・ガルム', 'アイアン・ゴーレム', 'ミラージュ・ヴァイパー']
  const COLORS = ['#c8432f', '#3d6aa8', '#5f8a4e', '#b8902e']
  const PAS_RGB = ['255,40,30', '255,220,110', '100,255,150', '180,90,255', '255,255,255', '255,130,30', '100,150,255', '100,230,255']
  const PAS_HEX = ['#ff2a22', '#ffdc6e', '#64ff96', '#b45aff', '#ffffff', '#ff821e', '#6496ff', '#64e6ff']
  // 必殺ごとに要るゲージ（0〜100）。強い技ほど多い。Unity の SpNeed と同じ並び
  const SP_NEED = [90, 100, 70, 80, 85, 70, 100, 80, 75, 90]
  SLOTS.find(x => x.key === 'sp').parts.forEach((p, i) => { p.x = (p.x ? p.x + '　' : '') + `必要ゲージ${SP_NEED[i]}%` })
  const SPECIALS = SLOTS.find(x => x.key === 'sp').parts.map(p => p.n)
  const isTouch = matchMedia('(pointer: coarse)').matches
  const DEBUG = /debug/.test(location.search)

  // パーツの横からの絵（自前の SVG・金属の灰と塗装の色）
  const cM = '#9aa1ab', cMD = '#555b64', cP = '#b8402e', cR = '#1c1e22', cB = '#b08a4a'
  const ART = {
    pas: [`<circle cx="40" cy="20" r="12" fill="none" stroke="#8a8f98" stroke-width="3" stroke-dasharray="4 4"/>`].concat(['#ff2a22', '#ffdc6e', '#64ff96', '#b45aff', '#ffffff', '#ff821e', '#6496ff', '#64e6ff'].map(c => `<circle cx="40" cy="20" r="15" fill="none" stroke="${c}" stroke-width="2.5" opacity=".6"/><circle cx="40" cy="20" r="8" fill="${c}"/><path d="M40 2v7M40 31v7M22 20h7M51 20h7" stroke="${c}" stroke-width="3" stroke-linecap="round"/>`)),
    dir: [
      `<path d="M40 6a14 14 0 1 1-12 21" fill="none" stroke="#d9a441" stroke-width="4" stroke-linecap="round"/><path d="M22 24l5 8 8-5z" fill="#d9a441"/>`,
      `<path d="M40 6a14 14 0 1 0 12 21" fill="none" stroke="#2fd6c0" stroke-width="4" stroke-linecap="round"/><path d="M58 24l-5 8-8-5z" fill="#2fd6c0"/>`],
    top: [
      `<path d="M14 25h52l-3 7H17z" fill="${cP}"/><path d="M3 23l15-4v7zM77 23l-15-4v7z" fill="${cM}"/><rect x="14" y="22" width="52" height="3" fill="${cMD}"/>`,
      `<path d="M5 21h70l-7 13H12z" fill="${cP}"/><rect x="3" y="19" width="74" height="3" fill="${cM}"/><path d="M14 18h6v3h-6zM60 18h6v3h-6z" fill="${cMD}"/>`,
      `<path d="M21 25h38l-3 6H24z" fill="${cP}"/><path d="M21 25L2 17l4 10zM59 25l19-8-4 10z" fill="${cM}"/>`,
      `<path d="M13 24h54l-3 7H16z" fill="${cP}"/><path d="M6 23l9-3v5zM74 23l-9-3v5zM26 21l6-2v3zM48 21l6 2v-2z" fill="${cM}"/><rect x="13" y="22" width="54" height="2" fill="${cMD}"/>`,
      `<path d="M22 24h36l-2 7H24z" fill="${cP}"/><path d="M22 25Q6 22 0 14l10 12zM58 25q16-3 22-11l-10 12z" fill="${cM}"/>`],
    core: [
      `<rect x="28" y="9" width="24" height="15" rx="1" fill="#5c7a5f"/><rect x="26" y="5" width="28" height="4" fill="${cM}"/><rect x="35" y="2" width="10" height="3" fill="${cB}"/>`,
      `<rect x="29" y="9" width="22" height="15" rx="1" fill="#4c6a92"/><path d="M29 14h22M29 19h22" stroke="#8fa6c4" stroke-width="1.5"/><rect x="27" y="5" width="26" height="4" fill="${cM}"/>`,
      `<rect x="24" y="9" width="32" height="15" rx="1" fill="#4a4c52"/><rect x="22" y="5" width="36" height="4" fill="${cM}"/><rect x="36" y="0" width="8" height="5" fill="${cMD}"/>`,
      `<rect x="28" y="9" width="24" height="15" rx="1" fill="#a3602a"/><path d="M33 22l7-10 7 10" fill="none" stroke="#e0a060" stroke-width="2"/><rect x="26" y="5" width="28" height="4" fill="${cM}"/>`,
      `<rect x="28" y="9" width="24" height="15" rx="1" fill="#2f5658"/><circle cx="40" cy="16.5" r="4" fill="${cR}" stroke="#7fb0b2"/><rect x="26" y="5" width="28" height="4" fill="${cM}"/>`],
    leg: [
      `<rect x="25" y="4" width="30" height="5" fill="${cM}"/><path d="M26 9h28l-4 14H30z" fill="${cR}"/><rect x="22" y="23" width="36" height="5" rx="1" fill="${cMD}"/>`,
      `<rect x="26" y="4" width="28" height="6" fill="${cMD}"/><path d="M30 10h20L40 38z" fill="${cM}"/>`,
      `<rect x="24" y="3" width="32" height="4" fill="${cMD}"/><rect x="27" y="8" width="26" height="3" fill="${cB}"/><rect x="27" y="12" width="26" height="3" fill="${cR}"/><rect x="27" y="16" width="26" height="3" fill="${cB}"/><rect x="27" y="20" width="26" height="3" fill="${cR}"/><path d="M34 24h12l-2 8h-8z" fill="${cM}"/>`,
      `<rect x="26" y="4" width="28" height="16" fill="${cMD}"/><rect x="20" y="20" width="40" height="6" rx="1" fill="${cR}"/><circle cx="24" cy="28" r="3" fill="${cM}"/><circle cx="40" cy="28" r="3" fill="${cM}"/><circle cx="56" cy="28" r="3" fill="${cM}"/>`,
      `<path d="M27 4h26l-5 12H32z" fill="${cMD}"/><path d="M32 16l3 20 3-20zM38 16l2 22 2-22zM44 16l3 20 3-20z" fill="${cM}"/>`],
  }

  // Unity の Parts.Compute と同じ計算（表示用）
  function stats(t, c, l) {
    const s = { atk: 1, def: 1, mass: 1, stamina: 1, speed: 1, jump: 1, grip: 1 }
    if (t === 0) { s.atk = 1.35; s.def = 0.85; s.mass = 1.0 }
    if (t === 1) { s.atk = 0.85; s.def = 1.4; s.mass = 1.3 }
    if (t === 2) { s.atk = 1.05; s.def = 0.95; s.mass = 0.82 }
    if (t === 3) { s.atk = 1.15; s.def = 1.1; s.mass = 1.05 }
    if (t === 4) { s.atk = 1.5; s.def = 0.75; s.mass = 1.15; s.speed = 0.95 }
    if (c === 0) { s.stamina = 1.35; s.speed *= 0.92 }
    if (c === 1) { s.stamina = 0.85; s.speed *= 1.22 }
    if (c === 2) { s.stamina = 1.05; s.speed *= 0.95; s.mass += 0.35; s.def *= 1.1 }
    if (c === 3) { s.stamina = 0.95 }
    if (c === 4) { s.mass += 0.15 }
    if (l === 0) { s.grip = 1.7; s.jump = 0.85; s.speed *= 0.95 }
    if (l === 1) { s.grip = 0.6; s.speed *= 1.15; s.stamina *= 1.15 }
    if (l === 2) { s.jump = 1.45 }
    if (l === 3) { s.grip = 0.8; s.speed *= 1.3; s.stamina *= 0.9 }
    if (l === 4) { s.grip = 1.3; s.jump = 1.15 }
    return s
  }
  const STAT_ROWS = [['攻撃', 'atk', 0.7, 1.55], ['防御', 'def', 0.7, 1.6], ['重さ', 'mass', 0.8, 1.7], ['持久', 'stamina', 0.75, 1.45], ['速さ', 'speed', 0.8, 1.6], ['跳躍', 'jump', 0.8, 1.5]]

  const store = {
    get(k, d) { try { const v = localStorage.getItem(k); return v === null ? d : v } catch (e) { return d } },
    set(k, v) { try { localStorage.setItem(k, v) } catch (e) {} },
  }
  // ---------- レベル（試合の結果で経験値。保存は localStorage） ----------
  const LV_XP = [0, 80, 200, 360, 560, 800, 1100, 1400, 1700, 2000]   // そのレベルに届く累計の経験値
  const MAXLV = LV_XP.length
  // レベルで解放される物（[段の名前, 何番目]）。Lv1 では右回転・必殺は1つ・パーツは一部だけ
  const UNLOCKS = { 2: [['dir', 1]], 3: [['sp', 1], ['pas', 1]], 4: [['top', 4], ['leg', 3], ['pas', 2], ['sp', 4]], 5: [['sp', 2], ['pas', 3]], 6: [['core', 3], ['core', 4], ['pas', 4], ['sp', 5]], 7: [['sp', 3], ['pas', 5], ['sp', 8]], 8: [['pas', 6], ['sp', 6]], 9: [['pas', 7], ['sp', 7]], 10: [['pas', 8], ['sp', 9]] }
  const needLv = (key, i) => { for (const lv in UNLOCKS) if (UNLOCKS[lv].some(u => u[0] === key && u[1] === i)) return +lv; return 1 }
  const slotOf = key => SLOTS.find(x => x.key === key)
  const unlockName = ([k, i]) => k === 'dir' ? '左回転のコマ' : k === 'pas' ? `パッシブ「${slotOf(k).parts[i].n}」` : k === 'sp' ? `必殺「${slotOf(k).parts[i].n}」` : `${slotOf(k).name}パーツ「${slotOf(k).parts[i].n}」`
  const levelOf = xp => { let l = 1; for (let i = 1; i < MAXLV; i++) if (xp >= LV_XP[i]) l = i + 1; return l }
  const fracOf = xp => { const l = levelOf(xp); return l >= MAXLV ? 1 : (xp - LV_XP[l - 1]) / (LV_XP[l] - LV_XP[l - 1]) }
  const prof = { xp: 0, win: 0, lose: 0, seen: 1, name: '' }
  try { const s = JSON.parse(store.get('koma-prof', 'null')); if (s) Object.assign(prof, s) } catch (e) {}
  { const m = /[?&]lv=(\d+)/.exec(location.search); if (m) { prof.xp = Math.max(prof.xp, LV_XP[Math.min(Math.max(+m[1], 1), MAXLV) - 1]); prof.seen = levelOf(prof.xp) } }
  // プレイヤー名（全角8文字まで・NGワードは保存しない）
  const cleanName = n => (window.NG ? NG.clean(n) : String(n || '').trim().slice(0, 8))
  if (prof.name && (cleanName(prof.name) !== prof.name || (window.NG && NG.bad(prof.name)))) prof.name = ''
  const myName = () => prof.name || 'あなた'
  const saveProf = () => store.set('koma-prof', JSON.stringify(prof))
  const myLv = () => levelOf(prof.xp)

  const pick = { top: 0, core: 0, leg: 0, dir: 0, sp: 0, pas: 0 }
  try { const s = JSON.parse(store.get('koma-pick2', 'null')); if (s) Object.assign(pick, s) } catch (e) {}
  const fixPick = () => { for (const sl of SLOTS) if (!(pick[sl.key] >= 0 && pick[sl.key] < sl.parts.length) || needLv(sl.key, pick[sl.key]) > myLv()) pick[sl.key] = 0 }
  fixPick()
  let nCpu = +store.get('koma-ncpu', 2) || 2

  // ---------- Unity ----------
  let unity = null, progress = 0
  const KB = window.KB = { state: null, events: [], ready: false, send, onUnity, screen: 'title' }
  KB.pick = pick; KB.startMatch = () => startMatch()
  const pending = []
  // Unity の準備前に送った命令はためておき、準備ができたら流す
  function send(cmd) { if (unity) unity.SendMessage('KomaBoot', 'Cmd', cmd); else if (pending.length < 50) pending.push(cmd) }

  const canvas = $('unity-canvas')
  const B = window.KB_BUILD
  // 2026-11-25 本人「準備中100%から動かない」: Build/ のファイル名は毎回同じなので、前の版がブラウザに残って新しい版と混ざった。
  // ui.js と同じ版の番号（index.html の ?v=）を付けて、版ごとに必ず取り直す
  const BV = '?v=' + (UI_V || Date.now())
  const loaderScript = document.createElement('script')
  loaderScript.src = B.loader + BV
  loaderScript.onload = () => {
    createUnityInstance(canvas, {
      dataUrl: B.data + BV, frameworkUrl: B.framework + BV, codeUrl: B.code + BV,
      streamingAssetsUrl: 'StreamingAssets', companyName: B.company, productName: B.product, productVersion: B.version,
      // スマホは描く量を抑える（画素の倍率を下げる）
      devicePixelRatio: isTouch ? Math.min(window.devicePixelRatio || 1, 1.5) : Math.min(window.devicePixelRatio || 1, 1.25),
      showBanner: (msg, type) => { if (type === 'error') console.error(msg); else console.warn(msg) },
    }, p => { progress = p; $('ldFill').style.width = Math.round(p * 100) + '%'; fightBtn() })
      .then(inst => { unity = inst; window.unityInstance = inst; while (pending.length) unity.SendMessage('KomaBoot', 'Cmd', pending.shift()) })
      .catch(e => { window.__kbErrs.push(String(e)); console.error(e); $('ldText').textContent = '読み込みに失敗しました。再読み込みしてください' })
  }
  loaderScript.onerror = () => { window.__kbErrs.push('loader failed'); $('ldText').textContent = '読み込みに失敗しました。再読み込みしてください' }
  document.body.appendChild(loaderScript)

  function onUnity(s) {
    let m; try { m = JSON.parse(s) } catch (e) { return }
    if (m.t === 's') { KB.state = m; hud(m); return }
    if (m.t === 'aim') { KB.aim = m; return }
    if (m.t === 'e') { KB.events.push(m); if (KB.events.length > 400) KB.events.shift(); ev(m) }
  }

  // ---------- 画面の切り替え ----------
  let practice = false
  function show(id) {
    for (const s of ['title', 'hub', 'hud', 'result']) $(s).hidden = s !== id
    document.body.classList.toggle('playing', id === 'hud')
    document.body.classList.toggle('on-title', id === 'title')
    $('touch').hidden = !(id === 'hud' && isTouch)
    $('prac').hidden = !(id === 'hud' && practice)
    document.body.classList.toggle('practice', id === 'hud' && practice)
    if (id !== 'hud') { resetTouch(); $('hint').hidden = true }
    KB.screen = id
  }

  function fightBtn() {
    const b = $('btnFight')
    b.disabled = !KB.ready
    b.textContent = KB.ready ? '出撃' : `準備中 ${Math.round(progress * 100)}%`
    $('btnPractice').disabled = !KB.ready
  }

  function ev(m) {
    switch (m.k) {
      case 'ready':
        KB.ready = true; window.__kbReady = true
        $('ldMini').classList.add('done'); $('load').classList.add('gone')
        if (isTouch) { send('touchmode|1'); send('quality|0') }
        sendLoadout()
        send(camFor())
        fightBtn()
        break
      case 'stage': { const t = $('stageTag'); $('stageName').textContent = practice ? '練習場' : ['アビス・ボウル', 'キングス・ヒル', 'ターンテーブル'][m.p | 0] || ''; KB.stage = m.p | 0; t.classList.remove('on'); void t.offsetWidth; t.classList.add('on'); document.body.classList.add('stage-intro'); setTimeout(() => document.body.classList.remove('stage-intro'), 2600); break }
      case 'count': document.body.classList.remove('stage-intro'); Snd.rate('bgm', 1); countdown(String(m.n), false); Snd.play('tick', { vol: 0.5 }); break
      case 'go':
        countdown('勝負', true); Snd.play('go', { vol: 0.8 })
        ensureBgm()
        break
      case 'hit': {
        const big = m.big, air = m.air
        // 2026-11-28 本人「コマ同士がぶつかったときのもうちょっと金属音みたいなのが欲しい。もうちょっと甲高い感じの」:
        // 低い「ゴッ」だけだった（素材の中心 約1.5〜2kHz・半分以上が1kHz未満・0.4〜1.2秒の長い尾・さらに0.8〜0.9倍で低く再生）。
        // 高い金属の「チン」「カキィン」を主にして、低い当たりは500Hz以下を削って短く下に敷く。軽い当たりは小さく「チン」、強い当たりは大きく「カキィン」
        { const nowT = performance.now(); hitVoices = hitVoices.filter(t => nowT - t < 150)
          if (hitVoices.length < 3) {
            hitVoices.push(nowT)
            const k = Math.min(1, m.p / 10), strong = big || m.vn > 14
            if (strong) {
              Snd.play('kakiin', { vol: 0.4 + 0.2 * k, rate: 0.95, dest: 'hi', cut: 0.4 })
              Snd.play('tink', { vol: 0.3, rate: 1.05, dest: 'hi', cut: 0.25, delay: 0.012 })
              Snd.play('hit', { vol: Math.min(1, 0.5 + m.p / 20) * 0.75, rate: 1.0, dest: 'hi', cut: 0.25 })
            } else {
              Snd.play('tink', { vol: 0.3 + 0.15 * k, rate: 1.05, jitter: 0.08, dest: 'hi', cut: 0.25 })
              Snd.play('hit', { vol: (0.5 + m.p / 20) * 0.55, rate: 1.0, dest: 'hi', cut: 0.2 })
            }
          } }
        // 2026-11-26 本人「芯とか掠りとか出すのやめて」: 当たりの角度は数値だけに効かせ、文字は出さない
        // 2026-11-26 本人「他の人の効果音まで文字で出さなくていい」: 擬音の文字は自分が当てた・当てられた時だけ
        { const me = m.a === 0 || m.b === 0
          if (air) { if (me) lines(m.x, m.y, 1); if (me) { onoma('ドゴォッ', m.x, m.y); flash() } Snd.duck(0.5, 0.8) }
          else if (big) { if (me) { lines(m.x, m.y, 0.7); onoma('ガギィン', m.x, m.y) } } }
        break
      }
      case 'wall': Snd.play('hit', { vol: 0.35 }); break
      case 'brace': Snd.play('creak', { vol: m.i === 0 ? 0.35 : 0.12, rate: 1.3 }); break
      case 'bracehit': Snd.play('clang', { vol: m.i === 0 ? 0.5 : 0.22, rate: 1.5 }); Snd.play('hit', { vol: m.i === 0 ? 0.4 : 0.15, rate: 0.9 }); break
      case 'wallsoft': Snd.play('hit', { vol: Math.min(0.2, 0.06 + m.p * 0.03), rate: 1.3 }); break
      case 'dash': if (m.p === 2) { Snd.play('whoosh', { vol: 0.5, rate: 1.2 + (rush = (rush + 1) % 3) * 0.25 }); if (m.i === 0) lines(m.x, m.y, 0.4) } if (m.i === 0) Snd.play('swish', { vol: 0.7 }); else Snd.play('swish', { vol: 0.25 }); break
      case 'dive': Snd.play('whoosh', { vol: m.i === 0 ? 0.8 : 0.4, rate: 1.4 }); if (m.i === 0 && !m.p) lines(m.x, m.y, 0.5); break
      case 'boost': if (m.i === 0) { Snd.play('swish', { vol: 0.8, rate: 0.8 }); lines(m.x, m.y, 0.35) } break
      case 'jump': Snd.play('spring', { vol: m.i === 0 ? 0.7 : 0.3 }); break
      case 'super': Snd.play('whoosh', { vol: m.i === 0 ? 0.8 : 0.35, rate: 1.6 }); if (m.i === 0) lines(m.x, m.y, 0.6); break
      case 'miracle': Snd.play('boom', { vol: m.i === 0 ? 0.6 : 0.3, rate: 1.5 }); Snd.play('whoosh', { vol: 0.5, rate: 0.8 }); break
      // 2026-11-27 WASD 2回押しの傾けと、かち上げ
      case 'lean': if (m.i === 0) Snd.play('swish', { vol: 0.55, rate: 1.4 }); break
      case 'upper': Snd.play('clang', { vol: 0.8, rate: 0.75 }); Snd.play('boom', { vol: 0.45, rate: 1.2 }); if (m.i === 0 || m.p === 0) lines(m.x, m.y, 0.7); break
      // 2026-11-27 場外ギリギリの踏ん張り: 縁できしむ → 戻れたら大きく、落ちたら重く
      case 'cling': { const me = m.i === 0; Snd.play('creak', { vol: me ? 1 : 0.45, rate: 0.9 }); Snd.play('clang', { vol: me ? 0.35 : 0.15, rate: 1.4 }); if (me) { Snd.duck(0.45, 1.1); shakeScreen() } break }
      case 'save': { const me = m.i === 0; Snd.play('spring', { vol: me ? 0.9 : 0.4, rate: 0.9 }); Snd.play('clang', { vol: me ? 0.8 : 0.35, rate: 1.1 }); Snd.play('whoosh', { vol: me ? 0.8 : 0.35, rate: 1.3 }); ring(m.x, m.y); if (me) { lines(m.x, m.y, 0.8); flash() } break }
      case 'clingfall': if (m.p) { Snd.play('hit', { vol: 0.9, rate: 0.7 }); if (m.i === 0) lines(m.x, m.y, 0.6) } break
      case 'burn': Snd.play('hit', { vol: m.i === 0 ? 0.3 : 0.15, rate: 1.7 }); break
      case 'board': Snd.play('boom', { vol: 0.5, rate: 1.25 }); break
      case 'special': {
        // 発動の瞬間: 技の色の縁取り・輪・集中線・重い音。技の名前は自分が使った時だけ出す
        const me = m.i === 0, kind = m.p | 0
        if (me) banner(kind)
        tint(kind, me ? 1 : 0.5); ring(m.x, m.y); if (me) lines(m.x, m.y, 1)
        if (me) { flash(); shakeScreen() }
        Snd.play('whoosh', { vol: me ? 1 : 0.6, rate: 0.7 })
        Snd.play('boom', { vol: me ? 0.8 : 0.45, rate: 0.6 })
        Snd.play('clang', { vol: me ? 0.7 : 0.35, rate: 0.7 })
        Snd.duck(0.3, 1.5)
        break
      }
      case 'boom': {
        // 当たりの瞬間: 閃光・輪・画面の揺れ・低い爆発音（自分の技・近くの技は大きく）
        const me = m.i === 0
        if (me || m.hm === 1) lines(m.x, m.y, 1); ring(m.x, m.y); tint(m.s | 0, me ? 0.9 : 0.4)
        if (me) { flash(); onoma('ズドォン', m.x, m.y); shakeScreen() }
        Snd.play('boom', { vol: 1, rate: 0.62 }); Snd.play('clang', { vol: 0.6, rate: 0.55 }); Snd.play('hit', { vol: 0.8, rate: 0.5 })
        break
      }
      case 'pas': {
        // パッシブの発動: 名前は自分のだけ出す（見た目と音は全員）
        const me = m.i === 0, i = m.p | 0
        if (me) pasTag(i)
        tint2(PAS_RGB[i], me ? 0.6 : 0.3)
        Snd.play('whoosh', { vol: me ? 0.7 : 0.35, rate: 0.9 }); Snd.play('clang', { vol: me ? 0.55 : 0.3, rate: 0.8 })
        break
      }
      case 'pasfx': {
        const t = m.p | 0
        if (t === 0) Snd.play('clang', { vol: 0.5, rate: 1.2 })
        if (t === 1) Snd.play('boom', { vol: 0.6, rate: 0.9 })
        if (t === 2) Snd.play('clang', { vol: 0.35, rate: 1.7 })
        if (t === 3) { Snd.play('clang', { vol: 0.8, rate: 0.9 }); Snd.play('hit', { vol: 0.6, rate: 1.3 }) }
        if (t === 4) { Snd.play('boom', { vol: 1, rate: 0.5 }); Snd.play('spring', { vol: 0.7, rate: 1.4 }); ring(m.x, m.y); if (m.i === 0) { flash(); shakeScreen() } }
        break
      }
      case 'warn':
        // 雷の予告: ゴロゴロ。円の外へ逃げれば外れる
        Snd.play('boom', { vol: 0.5, rate: 0.45 }); Snd.play('whoosh', { vol: 0.45, rate: 0.5 }); tint2(PAS_RGB[3], 0.35)
        break
      case 'thunder':
        Snd.play('boom', { vol: 1, rate: 0.55 }); Snd.play('clang', { vol: 0.8, rate: 0.6 })
        flash(); ring(m.x, m.y); tint2(PAS_RGB[3], 0.8); if (m.hit || m.i === 0) shakeScreen()
        break
      case 'land': {
        // 着地: 高いほど低く重い音。先端の金属が床に当たる高い音を少し重ねる
        if (m.p < 8) break
        const v = (m.i === 0 ? 1 : 0.5) * Math.min(1, 0.16 + m.p / 34)
        Snd.play('hit', { vol: v, rate: 0.62 })
        if (m.p > 12) Snd.play('clang', { vol: v * 0.45, rate: 1.3 })
        if (m.p > 20) Snd.play('boom', { vol: v * 0.5, rate: 1.5 })
        break
      }
      case 'ko':
        Snd.play('down', { vol: m.i === 0 ? 1 : 0.7 })
        // 2026-11-28 本人「余韻がない」: 回転が尽きて倒れたら、床に当たる小さな金属音のあと、縁をこすって回る音が間隔を詰めながら小さくなる
        if (m.why !== 'out' && m.why !== 'pit') { const v = m.i === 0 ? 1 : 0.5
          Snd.play('tink', { vol: 0.34 * v, rate: 1.1, dest: 'hi', cut: 0.3, delay: 0.32 })
          ;[[0.62, 1], [0.86, 1.08], [1.04, 1.16], [1.18, 1.25], [1.28, 1.35], [1.36, 1.45], [1.42, 1.55]].forEach(([d, r], n) => Snd.play(n % 2 ? 'tink' : 'clang', { vol: (0.22 - n * 0.02) * v, rate: r + 0.3, delay: d }))
          Snd.play('creak', { vol: 0.22 * v, rate: 1.9, delay: 0.5 }) }
        if (m.i === 0) onoma(m.why === 'out' ? '場外' : m.why === 'pit' ? '落下' : m.why === 'shaft' ? '軸折れ' : 'ダウン', m.x, Math.max(0.25, m.y))
        else koLog(m)   // 2026-11-28 本人: 他のコマのダウンは画面に大きく出さない。PC だけ左上に小さく流れる1行
        break
      case 'sudden': $('sudden').hidden = false; Snd.play('down', { vol: 0.6, rate: 0.7 }); Snd.rate('bgm', 1.12); break
      // 2026-11-28 必殺4つと中央の崩落
      case 'arrowring': Snd.play('ice', { vol: m.i === 0 ? 0.55 : 0.4, rate: 0.9 + (m.p | 0) * 0.12 }); break
      case 'arrowgo': Snd.play('whoosh', { vol: m.i === 0 ? 1 : 0.7, rate: 1.7 }); Snd.play('ice', { vol: 0.8, rate: 1.3 }); Snd.play('clang', { vol: 0.35, rate: 1.8 }); if (m.i === 0) { lines(m.x, m.y, 1); flash() } break
      case 'freeze': Snd.play('shatter', { vol: m.i === 0 ? 0.9 : 0.7, rate: 0.9 }); Snd.play('ice', { vol: 0.8, rate: 0.8 }); tint2('180,238,255', m.i === 0 ? 0.7 : 0.3); if (m.i === 0) shakeScreen(); break
      case 'melt': Snd.play('ice', { vol: m.i === 0 ? 0.7 : 0.35, rate: 1.4 }); break
      case 'chain': Snd.play('chain', { vol: 0.9, rate: 0.9 }); Snd.play('chain', { vol: 0.6, rate: 1.2, delay: 0.08 }); Snd.play('clang', { vol: 0.5, rate: 1.4 }); break
      case 'tug': Snd.play('chain', { vol: 0.9, rate: 0.8 }); Snd.play('hit', { vol: 0.5, rate: 1.2 }); if (m.i === 0) shakeScreen(); break
      case 'chainhit': Snd.play('chain', { vol: 0.5, rate: 1.1 }); Snd.play('hit', { vol: 0.6, rate: 0.8 }); break
      case 'chainthrow': Snd.play('whoosh', { vol: 0.9, rate: 0.9 }); Snd.play('chain', { vol: 0.9, rate: 0.7 }); Snd.play('boom', { vol: 0.7, rate: 0.8 }); if (m.i === 0 || m.why === '0') lines(m.x, m.y, 0.9); if (m.i === 0) shakeScreen(); break
      case 'anchorend': Snd.play('crack', { vol: 0.4, rate: 1.2 }); break
      case 'spiralgo': Snd.play('whoosh', { vol: 1, rate: 1.1 }); Snd.play('clang', { vol: 0.6, rate: 0.9 }); if (m.i === 0) { lines(m.x, m.y, 1); flash() } break
      case 'deny': { const e = [$('spChip'), $('tSpBtn')]; e.forEach(x => { x.classList.remove('deny'); void x.offsetWidth; x.classList.add('deny') }); Snd.play('clang', { vol: 0.3, rate: 0.6 }); break }
      case 'pitstart': Snd.play('creak', { vol: 0.8, rate: 0.7 }); Snd.play('crack', { vol: 0.9, rate: 0.8 }); break
      case 'crackstep': Snd.play('crack', { vol: 0.55 + 0.45 * (m.p || 0), rate: 0.85 + 0.3 * (m.p || 0) }); if (m.p > 0.5) Snd.play('creak', { vol: 0.5, rate: 0.6 }); break
      case 'pitfall': Snd.play('crumble', { vol: 1, rate: 0.9 }); Snd.play('boom', { vol: 0.9, rate: 0.5 }); Snd.play('down', { vol: 0.7, rate: 0.6 }); shakeScreen(); break
      case 'collapse': Snd.play('down', { vol: 0.9, rate: 0.6 }); Snd.play('boom', { vol: 0.7, rate: 0.7 }); break
      case 'end': result(m); break
      case 'phit': pracHit(m); break
      case 'pbreak': { const me = m.i === 0, part = m.p | 0
        Snd.play('clang', { vol: me ? 1 : 0.55, rate: part === 0 ? 1.8 : part === 1 ? 0.9 : 1.3 }); Snd.play('tink', { vol: me ? 0.8 : 0.4, rate: 1.4, dest: 'hi', cut: 0.3, delay: 0.03 }); if (part === 2) Snd.play('crack', { vol: me ? 0.9 : 0.5, rate: 1.1, delay: 0.05 }); Snd.play('hit', { vol: me ? 0.5 : 0.25, rate: 1.3 })
        if (me) { onoma(['刃が欠けた', '円盤にひび', '軸が折れた'][part], m.x, Math.max(0.3, m.y)); shakeScreen() }
        const el = tagEls[m.i]; if (el) { const pb = el.querySelector('.pb'); if (pb) { const q = pb.children[part]; if (q) { q.classList.remove('hit', 'brk'); void q.offsetWidth; q.classList.add('brk') } } }
        break }
      case 'trailhit': Snd.play('clang', { vol: m.i === 0 ? 0.6 : 0.35, rate: 1.6 }); Snd.play('hit', { vol: 0.25, rate: 1.3 }); break
      case 'mirhit': Snd.play('clang', { vol: m.i === 0 ? 0.6 : 0.35, rate: 1.2 }); Snd.play('hit', { vol: 0.3, rate: 1.0 }); break
      case 'pstat': pracStat(m); break
    }
    if (practice) pracEv(m)
  }

  // ---------- タイトル → ハブ（読み込み中から触れる） ----------
  $('btnPlay').onclick = () => { Snd.init(); Snd.play('ui', { vol: 0.6 }); toHub(curPg) }

  // ---------- ハブ: ホーム / 装備 / 育成 / 設定 ----------
  let curPg = 'home'
  const LOCK_SVG = '<svg viewBox="0 0 10 12"><rect x="0.5" y="5" width="9" height="7" fill="currentColor"/><path d="M2.5 5V3.5a2.5 2.5 0 0 1 5 0V5" fill="none" stroke="currentColor" stroke-width="1.4"/></svg>'
  const camFor = () => (KB.screen === 'hub' && curPg === 'gear') ? 'select' : 'title'
  function toHub(pg) { show('hub'); go(pg || curPg) }
  function go(pg) {
    curPg = pg; KB.page = pg
    document.querySelectorAll('.pg').forEach(e => e.classList.toggle('on', e.dataset.pg === pg))
    document.querySelectorAll('#nav button').forEach(e => e.classList.toggle('on', e.dataset.pg === pg))
    if (pg === 'gear') { prof.seen = myLv(); saveProf(); buildSlots() }
    renderHome(); renderGrow(); renderSet(); renderHeader()
    send(camFor()); sendLoadout(); fightBtn()
  }
  document.querySelectorAll('#nav button').forEach(b => {
    b.onpointerdown = e => { if (e.button > 0) return; e.preventDefault(); if (curPg !== b.dataset.pg) { Snd.init(); Snd.play('ui', { vol: 0.4 }); go(b.dataset.pg) } }
    b.onclick = e => { if (e.detail === 0 && curPg !== b.dataset.pg) go(b.dataset.pg) }
  })
  function renderHeader() {
    const l = myLv()
    $('plLv').textContent = l
    document.querySelector('.pl-name').textContent = myName()
    $('plXp').style.width = Math.round(fracOf(prof.xp) * 100) + '%'
    $('plXpTxt').textContent = l >= MAXLV ? '最高レベル' : `次のレベルまで ${LV_XP[l] - prof.xp}`
    $('dotGear').hidden = prof.seen >= l
  }
  const partArt = (key, i) => ART[key] ? `<svg viewBox="0 0 80 40">${ART[key][i]}</svg>` : '<svg viewBox="0 0 80 40"><path d="M40 6l7 14 15 1-11 10 3 15-14-8-14 8 3-15L18 21l15-1z" fill="#d9a441"/></svg>'
  function renderHome() {
    $('homeLoad').innerHTML = '<h3>いまの編成</h3>' + SLOTS.map(sl => `<button class="ld-row" data-go="gear">${'<span class="ln">' + sl.name + '</span>'}${partArt(sl.key, pick[sl.key])}<span class="lv">${sl.parts[pick[sl.key]].n}</span></button>`).join('')
    $('homeLoad').querySelectorAll('.ld-row').forEach(b => b.onclick = () => { Snd.play('ui', { vol: 0.4 }); go('gear') })
    const l = myLv()
    if (l >= MAXLV) $('homeNext').innerHTML = '<h3>育成</h3>すべて解放済み。'
    else $('homeNext').innerHTML = `<h3>次の解放</h3><b>Lv${l + 1}</b>　${UNLOCKS[l + 1].map(unlockName).join('、')}<br>あと ${LV_XP[l] - prof.xp} EXP（勝つと多くもらえる）`
  }
  const shortName = ([k, i]) => k === 'dir' ? '左回転' : k === 'pas' ? `パッシブ ${slotOf(k).parts[i].n}` : k === 'sp' ? `必殺 ${slotOf(k).parts[i].n}` : slotOf(k).parts[i].n
  function renderGrow() {
    const l = myLv()
    $('grLv').textContent = l
    $('grXp').style.width = Math.round(fracOf(prof.xp) * 100) + '%'
    $('grXpTxt').textContent = l >= MAXLV ? `累計 ${prof.xp} EXP　最高レベル` : `${prof.xp - LV_XP[l - 1]} / ${LV_XP[l] - LV_XP[l - 1]} EXP`
    $('grRec').textContent = `${prof.win}勝 ${prof.lose}敗`
    let h = `<li class="${l === 1 ? 'cur' : 'done'}"><b>Lv1</b><span>右回転のコマ・基本のパーツ</span><em>${l === 1 ? 'いま' : '解放済み'}</em></li>`
    for (let lv = 2; lv <= MAXLV; lv++) {
      const st = lv < l ? 'done' : lv === l ? 'cur' : 'lock'
      h += `<li class="${st}"><b>Lv${lv}</b><span>${UNLOCKS[lv].map(shortName).join('、')}</span><em>${lv <= l ? (lv === l ? 'いま' : '解放済み') : LOCK_SVG}</em></li>`
    }
    $('ladder').innerHTML = h
  }
  function renderSet() {
    if (document.activeElement !== $('nameIn')) $('nameIn').value = prof.name
    $('btnSound').textContent = muted ? 'オフ' : 'オン'
    $('how').innerHTML = isTouch
      ? '<span><b>左</b>スティックで動く（外をなぞると視点）</span><span><b>右</b>倒して離すとダッシュ（空中なら急降下）</span><span><b>星</b>光ったら必殺</span><span><b>踏ん張り</b>押している間（左のスティックは離す）</span><p>跳んで相手の頭上から落とすと大ダメージ。回転が尽きるか場外で負け。</p>'
      : '<span><b>WASD</b>移動</span><span><b>Shift</b>速く</span><span><b>Shift2回</b>超加速</span><span><b>Space</b>ジャンプ</span><span><b>F</b>踏ん張り（回転が減る）</span><span><b>C</b>相手に照準</span><span><b>マウス</b>視点</span><span><b>左クリック</b>ダッシュ</span><span><b>右クリック</b>必殺</span><p>跳んで相手の頭上から落とすと大ダメージ。回転が尽きるか場外で負け。</p>'
  }

  // ---------- 装備 ----------
  // 1段に1つ、左右で切り替える。まだ解放されていない物は飛ばし、残りの数を鍵で出す
  const usable = sl => sl.parts.map((p, i) => i).filter(i => needLv(sl.key, i) <= myLv())
  function slotBody(sl) {
    const i = pick[sl.key], p = sl.parts[i], us = usable(sl)
    const locked = sl.parts.length - us.length
    const art = partArt(sl.key, i).replace('<svg viewBox="0 0 80 40"><path d="M40 6l7', '<svg viewBox="0 0 80 40"><path d="M40 6l7')
    return `${art}<div class="slot-txt"><b>${p.n}</b><small>${p.d}</small></div><i class="slot-no">${us.indexOf(i) + 1}/${us.length}</i>${locked ? `<i class="slot-lk">${LOCK_SVG}${locked}</i>` : ''}`
  }
  let lastSlot = 'top'
  function renderDetail() {
    const sl = slotOf(lastSlot), p = sl.parts[pick[lastSlot]]
    $('detail').innerHTML = `<b>${sl.name}　${p.n}</b>${p.f}${p.x ? `<em>${p.x}</em>` : ''}`
  }
  function buildSlots() {
    const box = $('slots'); box.innerHTML = ''
    SLOTS.forEach(sl => {
      const d = document.createElement('div'); d.className = 'slot' + (sl.key === 'sp' ? ' sp' : '')
      d.innerHTML = `<div class="slot-name">${sl.name}</div><button class="arw" data-d="-1" aria-label="前">${ARROW_L}</button><div class="slot-body">${slotBody(sl)}</div><button class="arw" data-d="1" aria-label="次">${ARROW_R}</button>`
      d.dataset.slot = sl.key
      // 2026-11-25 本人「左右のボタンが反応が悪い」: click を待たず押した瞬間に切り替え、段は作り直さず中身だけ書き換える。
      // Unity への送信（コマの作り直し）は連打をまとめて1回
      const step = dd => {
        const us = usable(sl); if (us.length < 2) return
        pick[sl.key] = us[(us.indexOf(pick[sl.key]) + dd + us.length) % us.length]
        lastSlot = sl.key; d.querySelector('.slot-body').innerHTML = slotBody(sl); renderStats(); renderDetail(); sendLoadoutSoon()
      }
      d.querySelectorAll('.arw').forEach(b => {
        b.onpointerdown = e => { if (e.button > 0) return; e.preventDefault(); step(+b.dataset.d); Snd.play('ui', { vol: 0.4 }) }
        b.onclick = e => { if (e.detail === 0) step(+b.dataset.d) }
      })
      d.querySelector('.slot-body').onpointerdown = () => { lastSlot = sl.key; renderDetail() }
      box.appendChild(d)
    })
    renderStats(); renderDetail()
  }
  function renderStats() {
    const s = stats(pick.top, pick.core, pick.leg)
    $('stats').innerHTML = STAT_ROWS.map(([n, k, lo, hi]) => {
      const w = Math.max(6, Math.min(100, (s[k] - lo) / (hi - lo) * 100))
      return `<div class="st${w > 62 ? ' hi' : ''}"><span>${n}</span><i><u style="width:${w}%"></u></i></div>`
    }).join('')
    store.set('koma-pick2', JSON.stringify(pick))
  }
  const ARROW_L = '<svg viewBox="0 0 24 24"><path d="M15 4L7 12l8 8" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square"/></svg>'
  const ARROW_R = '<svg viewBox="0 0 24 24"><path d="M9 4l8 8-8 8" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square"/></svg>'
  function sendLoadout() { send(`loadout|${pick.top},${pick.core},${pick.leg},${pick.sp},${pick.dir},${pick.pas - 1}`) }
  let loadoutT = 0
  function sendLoadoutSoon() { clearTimeout(loadoutT); loadoutT = setTimeout(sendLoadout, 140) }
  document.querySelectorAll('#vsPick button').forEach(b => {
    b.classList.toggle('on', +b.dataset.n === nCpu)
    b.onclick = () => {
      nCpu = +b.dataset.n
      document.querySelectorAll('#vsPick button').forEach(x => x.classList.toggle('on', x === b))
      store.set('koma-ncpu', nCpu)
    }
  })
  $('btnFight').onclick = () => { if (!KB.ready) return; Snd.init(); Snd.play('ui', { vol: 0.6 }); startMatch() }
  // マウスで視点: 試合中は画面にマウスを固定して、動いた量を Unity へ送る。Esc で解除、画面を押すと戻る
  function lockMouse() { if (!isTouch && canvas.requestPointerLock && document.pointerLockElement !== canvas) { try { const r = canvas.requestPointerLock(); if (r && r.catch) r.catch(() => {}) } catch (e) {} } }
  let lookX = 0, lookY = 0
  document.addEventListener('pointerlockchange', () => send('mlock|' + (document.pointerLockElement === canvas ? 1 : 0)))
  document.addEventListener('mousemove', e => { if (document.pointerLockElement === canvas) { lookX += e.movementX; lookY += e.movementY } })
  canvas.addEventListener('mousedown', () => { if (KB.screen === 'hud') lockMouse() })
  ;(function lookLoop() { if (lookX || lookY) { send(`look|${lookX},${lookY}`); lookX = lookY = 0 } requestAnimationFrame(lookLoop) })()
  function startMatch() {
    if (document.activeElement) document.activeElement.blur()
    canvas.focus()
    $('sudden').hidden = true
    tagEls.forEach(t => t && t.remove()); tagEls.length = 0; tagParts.length = 0
    show('hud')
    if (isTouch && store.get('koma-hint', '') !== '1') showHint()
    send('move|0,0')
    send(practice ? `practice|1,${pracDummy}` : 'practice|0,0')
    send(`start|${pick.top},${pick.core},${pick.leg},${nCpu},${practice ? pracSp : pick.sp},${pick.dir},${pick.pas - 1}`)
    lockMouse()
  }

  // ---------- 練習場（タイトルの「練習」から。時間なし・的3種・技のチェック） ----------
  const PRAC_ITEMS = [
    { id: 'dash', n: 'ダッシュで当てる', h: 'PC: 左クリック　スマホ: 右を倒して離す' },
    { id: 'air', n: '空中から当てる', h: 'ジャンプ台やスペースで跳び、空中でダッシュして上から' },
    { id: 'upper', n: 'かち上げ', h: 'WASD を2回押して傾け、その側で当てる', pc: true },
    { id: 'brace', n: '踏ん張りで耐える', h: 'PC: F を押しながら当てられる　スマホ: 踏ん張りボタン（左スティックは離す）' },
    { id: 'cling', n: '縁で踏ん張る', h: '縁から外へ出かけると引っかかる' },
    { id: 'sp', n: '必殺を撃つ', h: 'PC: 右クリック　スマホ: 星ボタン' },
    { id: 'chain', n: 'チェイン・バインド', h: '必殺をチェイン・バインドにして撃つ' },
    { id: 'out', n: '的を場外へ落とす', h: '縁の外へ押し出す' },
  ].filter(x => !(isTouch && x.pc))
  let pracDummy = 0, pracSp = 0
  let pracDone = {}
  try { pracDone = JSON.parse(store.get('koma-prac', '{}')) || {} } catch (e) { pracDone = {} }
  function pracRender() {
    const ul = $('prItems'); ul.innerHTML = ''
    for (const it of PRAC_ITEMS) {
      const li = document.createElement('li'); li.dataset.id = it.id; if (pracDone[it.id]) li.className = 'done'
      li.innerHTML = '<i class="ck"></i><div><b></b><small></small></div>'
      li.querySelector('b').textContent = it.n; li.querySelector('small').textContent = it.h
      ul.appendChild(li)
    }
    $('prCount').textContent = `${PRAC_ITEMS.filter(x => pracDone[x.id]).length}/${PRAC_ITEMS.length}`
  }
  function pracMark(id) {
    if (!PRAC_ITEMS.some(x => x.id === id) || pracDone[id]) return
    pracDone[id] = 1; store.set('koma-prac', JSON.stringify(pracDone)); pracRender()
    const li = document.querySelector(`#prItems li[data-id="${id}"]`); if (li) { li.classList.add('done'); Snd.play('ui', { vol: 0.5, rate: 1.5 }) }
  }
  function pracEv(m) {
    switch (m.k) {
      case 'phit': if (m.dash) pracMark('dash'); if (m.air) pracMark('air'); if (m.up) pracMark('upper'); break
      case 'bracehit': if (m.i === 0) pracMark('brace'); break
      case 'cling': if (m.i === 0) pracMark('cling'); break
      case 'special': if (m.i === 0) { pracMark('sp'); if ((m.p | 0) === 7) pracMark('chain') } break
      case 'ko': if (m.i === 1 && m.why === 'out') pracMark('out'); break
    }
  }
  const fmt = (v, d) => (+v).toFixed(d)
  function pracHit(m) {
    $('psD').textContent = fmt(m.dd, 1) + '%'; $('psV').textContent = fmt(m.v, 1) + ' m/s'; $('psR').textContent = '…'
    $('psM').textContent = fmt(m.dm, 1) + '%'
    if (m.pw) $('psP').textContent = `刃 -${fmt(m.pw[0], 1)}　円盤 -${fmt(m.pw[1], 1)}　軸 -${fmt(m.pw[2], 1)}`
  }
  function pracStat(m) { $('psR').textContent = fmt(m.dist, 1) + ' m' }
  function pracSpLabel() { $('prSp').textContent = '必殺　' + SPECIALS[pracSp] }
  function pracStart() {
    if (!KB.ready) return
    practice = true; Snd.init(); Snd.play('ui', { vol: 0.6 })
    pracSp = pick.sp; pracSpLabel()
    $('psD').textContent = $('psV').textContent = $('psR').textContent = $('psM').textContent = $('psP').textContent = '-'
    document.querySelectorAll('#prDummy button').forEach(x => x.classList.toggle('on', +x.dataset.d === pracDummy))
    if (isTouch) $('prList').classList.add('shut')
    pracRender(); startMatch()
  }
  function pracExit() {
    practice = false; resTok++
    if (document.pointerLockElement && document.exitPointerLock) document.exitPointerLock()
    send('practice|0,0'); send('title'); send('move|0,0'); send('aimoff'); send('brace|0')
    $('onoma').classList.remove('on'); $('banner').classList.remove('on')
    show('title')
  }
  $('btnPractice').onclick = pracStart
  $('prBack').onclick = () => { Snd.play('ui', { vol: 0.5 }); pracExit() }
  $('prReset').onclick = () => { Snd.play('ui', { vol: 0.5 }); send('pracreset'); $('psD').textContent = $('psV').textContent = $('psR').textContent = $('psM').textContent = $('psP').textContent = '-' }
  $('prSp').onclick = () => { pracSp = (pracSp + 1) % SPECIALS.length; pracSpLabel(); send(`setsp|${pracSp},0`); send('gauge|100'); Snd.play('ui', { vol: 0.5 }) }
  document.querySelectorAll('#prDummy button').forEach(b => b.onclick = () => {
    pracDummy = +b.dataset.d; send(`dummytype|${pracDummy}`); Snd.play('ui', { vol: 0.4 })
    document.querySelectorAll('#prDummy button').forEach(x => x.classList.toggle('on', x === b))
  })
  $('prHead').onclick = () => $('prList').classList.toggle('shut')
  $('prClear').onclick = () => { pracDone = {}; store.set('koma-prac', '{}'); pracRender() }
  // 画面の外へ出てもキー入力が残らないように、練習から戻る時は上で全部止める

  // ---------- HUD ----------
  const tagEls = []
  let rush = 0, hitVoices = []
  const tagParts = []
  let lastSpin = -1, frames = 0, fpsT = performance.now(), fps = 0
  function hud(m) {
    frames++
    const now = performance.now(); if (now - fpsT > 1000) { fps = Math.round(frames * 1000 / (now - fpsT)); frames = 0; fpsT = now; KB.fps = fps }
    if (KB.screen !== 'hud') return
    const t = Math.floor(m.tm)
    $('hudTime').textContent = m.ph === 'count' ? '' : `${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')}`
    { const sw = m.sw === 1
      if ($('specBar').hidden === sw) $('specBar').hidden = !sw
      document.body.classList.toggle('spec', sw)
      if (sw && m.spec >= 0) { const nm = m.spec === 0 ? myName() : NAMES[m.spec]; if ($('specName').textContent !== nm) $('specName').textContent = nm } }
    const me = m.k.find(k => k.i === 0)
    if (me) {
      // 2026-11-25 本人「左上に残り回転数みたいなの出すのやめて」: 数字は出さない。回転は名札の細い線とコマの揺れで見せる
      const nd = me.nd || 1, ready = me.g >= nd - 1e-6
      const full = ready && me.a === 1 && me.ok === 1
      $('spChip').classList.toggle('wait', ready && me.a === 1 && me.ok === 0); $('tSpBtn').classList.toggle('wait', ready && me.a === 1 && me.ok === 0)
      { const nn = Math.round(nd * 100); if ($('spChip').dataset.nd !== String(nn)) { $('spChip').dataset.nd = nn; $('spChip').style.setProperty('--nd', nn + '%'); $('tNd').textContent = nn } }
      { const rc = me.rc === 1 && me.a === 1; $('spChip').classList.toggle('rescue', rc); $('tSpBtn').classList.toggle('rescue', rc); const tx = $('spTxt'); const want = rc ? '復帰！　右クリック' : '必殺　右クリック'; if (tx && tx.textContent !== want) tx.textContent = want }
      $('spChip').classList.toggle('full', full); $('spFill').style.width = Math.min(100, me.g * 100).toFixed(0) + '%'
      $('tBrace').classList.toggle('on', me.br === 1)
      $('tSpBtn').classList.toggle('full', full)
      $('tSpBtn').style.setProperty('--g', (Math.min(1, me.g / nd) * 100).toFixed(0) + '%')
      $('flickArrow').classList.toggle('cd', me.cd > 0)
      KB.me = me
    }
    const W = innerWidth, H = canvas.clientHeight || innerHeight
    m.k.forEach(k => {
      let el = tagEls[k.i]
      if (!el) {
        el = tagEls[k.i] = document.createElement('div')
        el.className = 'tag' + (k.i === 0 ? ' me' : '')
        el.style.setProperty('--c', COLORS[k.i])
        { const sp = document.createElement('span'); sp.textContent = k.i === 0 ? myName() : practice ? 'ダミー' : NAMES[k.i]; el.appendChild(sp) }
        { const pb = document.createElement('div'); pb.className = 'pb'; pb.innerHTML = '<i title="刃"></i><i title="円盤"></i><i title="軸"></i>'; el.appendChild(pb) }
        $('tags').appendChild(el)
      }
      el.classList.toggle('out', k.a === 0)
      if (k.pb) { const pb = el.querySelector('.pb'); if (pb) for (let q = 0; q < 3; q++) { const c = pb.children[q], v = k.pb[q]; c.style.setProperty('--v', Math.max(0, v).toFixed(2)); c.classList.toggle('low', v > 0 && v < 0.3); c.classList.toggle('gone', v <= 0) } }
      el.style.display = k.z > 0 && k.h > -6 ? '' : 'none'
      el.style.transform = `translate(${(k.x * W).toFixed(0)}px,${((1 - k.y) * H).toFixed(0)}px) translate(-50%,-100%)`
    })
  }

  // ---------- 演出 ----------
  function koLog(m) {
    if (isTouch || practice) return
    let box = document.getElementById('koLog'); if (!box) { box = document.createElement('div'); box.id = 'koLog'; document.body.appendChild(box) }
    const nm = practice ? 'ダミー' : NAMES[m.i] || ''
    const why = m.why === 'out' ? '場外' : m.why === 'pit' ? '落下' : m.why === 'shaft' ? '軸折れ' : 'ダウン'
    const row = document.createElement('div'); row.textContent = `${nm}　${why === 'ダウン' ? 'ダウン' : 'ダウン（' + why + '）'}`
    box.appendChild(row); while (box.children.length > 4) box.removeChild(box.firstChild)
    setTimeout(() => row.remove(), 4000)
  }
  const SVGNS = 'http://www.w3.org/2000/svg'
  function lines(x, y, power) {
    const svg = $('lines')
    svg.innerHTML = ''
    const n = isTouch ? 44 : 70
    const cx = (x - 0.5) * 60, cy = (0.5 - y) * 40
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2
      const r0 = 18 + Math.random() * 14 * (1.2 - power * 0.4)
      const w = 0.25 + Math.random() * 0.9
      const p = document.createElementNS(SVGNS, 'path')
      const x0 = cx + Math.cos(a) * r0, y0 = cy + Math.sin(a) * r0
      const x1 = cx + Math.cos(a - w * 0.02) * 120, y1 = cy + Math.sin(a - w * 0.02) * 120
      const x2 = cx + Math.cos(a + w * 0.02) * 120, y2 = cy + Math.sin(a + w * 0.02) * 120
      p.setAttribute('d', `M${x0.toFixed(1)} ${y0.toFixed(1)}L${x1.toFixed(1)} ${y1.toFixed(1)}L${x2.toFixed(1)} ${y2.toFixed(1)}Z`)
      p.setAttribute('fill', i % 5 === 0 ? 'rgba(255,240,200,.9)' : 'rgba(255,255,255,.85)')
      svg.appendChild(p)
    }
    svg.classList.remove('on'); void svg.getBoundingClientRect(); svg.classList.add('on')
  }
  function flash() { const f = $('flash'); f.classList.remove('on'); void f.offsetWidth; f.classList.add('on') }
  function onoma(text, x, y) {
    const o = $('onoma'); o.textContent = text
    o.style.left = Math.min(80, Math.max(20, x * 100)) + '%'
    o.style.top = Math.min(80, Math.max(20, (1 - y) * 100 - 8)) + '%'
    o.classList.remove('on'); void o.offsetWidth; o.classList.add('on')
  }
  function angTag(text, x, y, cls) {
    const o = $('angTag'); o.textContent = text
    o.style.left = Math.min(82, Math.max(18, x * 100)) + '%'
    o.style.top = Math.min(82, Math.max(16, (1 - y) * 100 + 9)) + '%'
    o.className = cls; void o.offsetWidth; o.classList.add('on')
  }
  // 技ごとの色（メテオ=橙 / テンペスト=水色 / ファントム=紫 / イージス=金 / アンカー=赤紫 / スパイラル=赤 / アロー=氷の白 / チェイン=黄緑）
  const SP_RGB = ['255,110,40', '60,190,240', '170,90,240', '240,180,50', '230,70,190', '255,40,64', '180,238,255', '150,255,76', '110,240,255', '185,130,255']
  // 2026-11-26 本人「他の人が使ってる必殺技の名前は出さなくていい」: 名前の帯は自分の技だけ
  function banner(kind) {
    const b = $('banner')
    b.querySelector('span').textContent = SPECIALS[kind | 0] || SPECIALS[0]
    b.style.setProperty('--bc', SP_RGB[kind | 0] || SP_RGB[0])
    b.classList.remove('on'); void b.offsetWidth; b.classList.add('on')
  }
  function pasTag(i) {
    const t = $('pasTag'); t.textContent = slotOf('pas').parts[i + 1].n; t.style.setProperty('--pc', PAS_HEX[i])
    t.classList.remove('on'); void t.offsetWidth; t.classList.add('on')
  }
  function tint2(rgb, power) {
    const t = $('tint'); t.style.setProperty('--tc', `rgba(${rgb},${(0.85 * power).toFixed(2)})`)
    t.classList.remove('on'); void t.offsetWidth; t.classList.add('on')
  }
  function tint(kind, power) {
    const t = $('tint'); t.style.setProperty('--tc', `rgba(${SP_RGB[kind | 0] || SP_RGB[0]},${(0.85 * power).toFixed(2)})`)
    t.classList.remove('on'); void t.offsetWidth; t.classList.add('on')
  }
  function ring(x, y) {
    const r = $('ring'); r.style.left = Math.min(90, Math.max(10, x * 100)) + '%'; r.style.top = Math.min(85, Math.max(15, (1 - y) * 100)) + '%'
    r.classList.remove('on'); void r.offsetWidth; r.classList.add('on')
  }
  function shakeScreen() { const b = document.body; b.classList.remove('shk'); void b.offsetWidth; b.classList.add('shk'); setTimeout(() => b.classList.remove('shk'), 360) }
  function countdown(text, go) {
    const c = $('count'); c.textContent = text
    c.className = 'count' + (go ? ' go' : ''); void c.offsetWidth; c.classList.add('pop')
  }

  // ---------- 結果 ----------
  let resTok = 0
  function result(m) {
    if (document.pointerLockElement && document.exitPointerLock) document.exitPointerLock()
    Snd.hush(0.25)   // BGM は止めない（タイトル・試合・結果とつながって鳴り続ける）
   // 鳴っている効果音を消してから結果の音
    Snd.play(m.win ? 'win' : 'lose', { vol: 0.9 })
    const big = $('rsBig')
    big.textContent = m.win ? '勝ち' : '負け'
    big.className = 'rs-big ' + (m.win ? 'win' : 'lose')
    $('rsWhy').textContent = m.win ? '最後まで回り切った' : m.why === 'out' ? '場外に落ちた' : m.why === 'pit' ? '穴に落ちた' : m.why === 'shaft' ? '軸が折れた' : '回転が尽きた'
    $('rsLine').textContent = `${m.rank ? m.rank + '位／' + m.of + '人　' : ''}${Math.floor(m.time)}秒　削った回転 ${Math.round(m.dealt)}　上からの一撃 ${m.aerial}回　必殺 ${m.specials}回`
    $('onoma').classList.remove('on'); $('banner').classList.remove('on')
    // 経験値: 勝てば多い。相手が多いほど・上から当てた回数・必殺の回数で少し増える
    const gain = (m.win ? 70 + 30 * nCpu : 25 + 10 * nCpu) + Math.min(40, m.aerial * 8) + Math.min(30, m.specials * 10)
    const before = prof.xp, lv0 = levelOf(before)
    prof.xp += gain; if (m.win) prof.win++; else prof.lose++; saveProf()
    const lv1 = levelOf(prof.xp)
    m.gain = gain; m.lvBefore = lv0; m.lvAfter = lv1
    $('rsUp').hidden = true
    $('rsLv').textContent = lv0; $('rsName').textContent = myName()
    $('rsGain').textContent = `+${gain} EXP`
    const bar = $('rsXp'), box = bar.parentNode
    box.classList.remove('anim'); bar.style.width = Math.round(fracOf(before) * 100) + '%'
    show('result')
    KB.result = m
    const tok = ++resTok, W = ms => new Promise(r => setTimeout(r, ms))
    ;(async () => {
      await W(700); if (tok !== resTok) return
      box.classList.add('anim')
      let lv = lv0
      while (lv < lv1) {
        bar.style.width = '100%'; await W(750); if (tok !== resTok) return
        lv++; box.classList.remove('anim'); bar.style.width = '0%'; $('rsLv').textContent = lv; void bar.offsetWidth; box.classList.add('anim')
        Snd.play('spring', { vol: 0.8, rate: 1.2 }); Snd.play('win', { vol: 0.5, rate: 1.3 })
      }
      bar.style.width = Math.round(fracOf(prof.xp) * 100) + '%'
      if (lv1 > lv0) {
        await W(450); if (tok !== resTok) return
        const names = []
        for (let l = lv0 + 1; l <= lv1; l++) for (const u of UNLOCKS[l]) names.push(unlockName(u))
        const up = $('rsUp')
        up.innerHTML = `<b>LEVEL UP　Lv${lv0} → Lv${lv1}</b>解放: ${names.join('、')}`
        up.hidden = false
      }
      renderHeader()
    })()
  }
  $('btnAgain').onclick = () => { Snd.play('ui', { vol: 0.6 }); resTok++; fixPick(); startMatch() }
  $('btnParts').onclick = () => { Snd.play('ui', { vol: 0.4 }); resTok++; toHub('gear') }
  $('btnHome').onclick = () => { Snd.play('ui', { vol: 0.4 }); resTok++; toHub('home') }

  // 観戦: 入力は「誰を見るか」だけ（Unity へ spec 系の命令を送る）。左右キー・ボタン・画面のクリックで切り替え、結果へで抜ける
  $('specPrev').onclick = () => { Snd.play('ui', { vol: 0.3 }); send('specnext|-1') }
  $('specNext').onclick = () => { Snd.play('ui', { vol: 0.3 }); send('specnext|1') }
  $('specExit').onclick = () => { Snd.play('ui', { vol: 0.4 }); send('specexit') }
  addEventListener('keydown', e => { if (KB.state && KB.state.sw === 1) { if (e.code === 'ArrowLeft') { send('specnext|-1'); e.preventDefault() } else if (e.code === 'ArrowRight') { send('specnext|1'); e.preventDefault() } } })
  canvas.addEventListener('pointerdown', e => { if (KB.state && KB.state.sw === 1 && !isTouch && e.button === 0) send('specnext|1') })
  // ---------- BGM（2026-11-28 本人「コマのゲームは最初から BGM 流しといて」） ----------
  // ブラウザは最初の操作まで音を出せないので、最初の操作（タップ・クリック・キー・読み込み中でも）の1回で鳴らし始める。
  // 同じ曲が鳴っていれば二重に始めない。ミュートは Snd.setMute（保存済みの設定）で守る。試合・結果をまたいでも止めない
  const BGM_URL = 'TemplateData/snd/gyro_ragnarok_bgm.mp3'
  function ensureBgm() { const l = Snd.loops.bgm; if (l && l.url === BGM_URL) return; Snd.loop('bgm', BGM_URL, 0.55) }
  { const first = () => { for (const t of ['pointerdown', 'keydown', 'touchstart', 'mousedown']) document.removeEventListener(t, first, true); Snd.init(); ensureBgm() }
    for (const t of ['pointerdown', 'keydown', 'touchstart', 'mousedown']) document.addEventListener(t, first, true) }
  // ---------- 音 ----------
  const S = n => 'TemplateData/snd/' + n + '.mp3'
  Snd.sfxGain = 1   // 2026-11-26 本人「効果音じゃなくて BGM を上げて」: 効果音は元の1.0倍。BGM は 0.3→0.55。出口のコンプレッサーは BGM+効果音の合算が割れないよう残す
  Snd.def({
    hit: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_02'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_03'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_04'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_hit_01')],
    clang: [S('100-cc0-sfx_100-CC0-SFX_0_metal_04'), S('100-cc0-sfx_100-CC0-SFX_0_metal_12')],
    boom: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_hit_01')],
    down: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_falling_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_falling_02')],
    swish: [S('swishes-sound-pack_swishes_swishes_swish-7'), S('swishes-sound-pack_swishes_swishes_swish-9'), S('swishes-sound-pack_swishes_swishes_swish-3')],
    spring: [S('100-cc0-sfx_100-CC0-SFX_0_spring_03'), S('100-cc0-sfx_100-CC0-SFX_0_spring_05')],
    // 2026-11-27 本人「試合が終わっても風っぽい音がしばらく聞こえる」: 元の素材は「ヒュッ」が約1秒おきに5回入った5.2秒の音だった。最初の1回（0.85秒）だけを切り出した版を使う
    whoosh: [S('air-whoosh_whoosh2_short')],
    creak: [S('kenney-rpg_creak1'), S('kenney-rpg_creak3')],
    tick: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_HIT_jingles_HIT03')],
    go: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_STEEL_jingles_STEEL04')],
    win: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_STEEL_jingles_STEEL00')],
    lose: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_STEEL_jingles_STEEL09')],
    ui: [S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_hit_01')],
    // 2026-11-28 コマ同士の衝突: 軽い「チン」と強い「カキィン」（高い金属の録音）
    tink: [S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_hit_02'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_06'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_03')],
    kakiin: [S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_01'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_04'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_03')],
    // 2026-11-28 氷（ガラス）・鎖（金属）・ひび割れ（岩）の録音素材
    ice: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_glass_hit_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_glass_hit_02'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_glass_01')],
    shatter: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_glass_breaking_05'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_glass_breaking_02')],
    chain: [S('100-cc0-sfx_100-CC0-SFX_0_metal_05'), S('100-cc0-sfx_100-CC0-SFX_0_metal_07'), S('100-cc0-sfx_100-CC0-SFX_0_metal_09'), S('50-rpg-sound-effects_RPGsounds_Kenney_OGG_metalLatch')],
    crack: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_rock_hit_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_rock_falling_04'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_rock_breaking_03')],
    crumble: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_rock_breaking_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_rock_falling_01')],
  })
  let muted = store.get('koma-mute', '0') === '1'
  function applyMute() { Snd.setMute(muted); $('muteX').hidden = !muted; $('muteWave').style.display = muted ? 'none' : ''; $('btnSound').textContent = muted ? 'オフ' : 'オン' }
  applyMute()
  $('btnMute').onclick = () => { muted = !muted; Snd.init(); applyMute(); store.set('koma-mute', muted ? '1' : '0') }
  $('btnSound').onclick = () => { $('btnMute').onclick() }
  // 名前: 全角8文字まで。NGは保存せず「その名前は使えません」（言葉は出さない）
  function saveName() {
    const v = cleanName($('nameIn').value); const msg = $('nameMsg')
    if (v && window.NG && NG.bad(v)) { msg.textContent = 'その名前は使えません'; msg.className = 'nm bad'; return false }
    prof.name = v; saveProf(); $('nameIn').value = v
    msg.textContent = v ? '保存しました' : '「あなた」のままです'; msg.className = 'nm ok'
    renderHeader(); return true
  }
  $('btnName').onclick = () => { Snd.play('ui', { vol: 0.4 }); saveName() }
  $('nameIn').addEventListener('keydown', e => { e.stopPropagation(); if (e.key === 'Enter') { e.preventDefault(); saveName(); $('nameIn').blur() } })
  $('nameIn').addEventListener('input', () => { const c = cleanName($('nameIn').value); if (c !== $('nameIn').value.trim()) $('nameIn').value = c; $('nameMsg').textContent = '' })
  document.querySelector('.pl-name').onclick = () => { go('set'); setTimeout(() => $('nameIn').focus(), 50) }
  let wipeT = 0
  $('btnWipe').onclick = () => {
    const b = $('btnWipe')
    if (!wipeT) { b.textContent = 'もう一度押すと消えます'; wipeT = setTimeout(() => { wipeT = 0; b.textContent = 'レベルと戦績を消す' }, 3500); return }
    clearTimeout(wipeT); wipeT = 0; b.textContent = 'レベルと戦績を消す'
    prof.xp = 0; prof.win = 0; prof.lose = 0; prof.seen = 1; saveProf(); fixPick(); renderEverything()
  }
  function renderEverything() { renderHome(); renderGrow(); renderSet(); renderHeader(); if (curPg === 'gear') buildSlots(); sendLoadout() }

  // ---------- スマホの操作 ----------
  // 2026-11-26 本人「右スティックも固定して、カメラのスワイプできるように」: 左右とも場所を固定。左で動く。右は倒して離すとその向きへダッシュ（空中なら急降下）、タップだけなら近くの相手へ。
  // スティックの外をなぞると視点が回る。星のボタンで必殺
  const stick = $('stick'), knob = stick.querySelector('i')
  const arrow = $('flickArrow'), faLine = $('faLine'), faDot = $('faDot'), faOrigin = $('faOrigin')
  let sid = null, sx = 0, sy = 0
  let cid = null, cx = 0, cy = 0, tx0 = 0, ty0 = 0
  let fid = null, fx = 0, fy = 0, fdx = 0, fdy = 0, fmax = 0, lastFlick = null, aiming = false
  // 通知・タブ切替・ツールバーで指の離しが届かなかった時も、入力を残さない
  const dropTouch = () => resetTouch()
  addEventListener('blur', dropTouch); addEventListener('pagehide', dropTouch)
  document.addEventListener('visibilitychange', () => { if (document.visibilityState !== 'visible') dropTouch() })
  const STICK_R = 50, FLICK_MIN = 22, FLICK_MAX = 70
  function idleStick() {
    const z = $('stickZone').getBoundingClientRect()
    stick.style.transform = `translate(${z.left + z.width / 2}px,${z.top + z.height / 2}px)`
    { const f = $('flickZone').getBoundingClientRect(); fx = f.left + f.width / 2; fy = f.top + f.height / 2; if (fid === null) { fdx = fdy = 0; drawArrow() } }
    stick.className = 'idle'; knob.style.transform = ''
  }
  function resetTouch() {
    if (sid !== null) send('move|0,0')
    if (fid !== null) send('aimoff')
    sid = null; fid = null; cid = null; arrow.classList.remove('on'); if (!$('touch').hidden) idleStick()
  }
  const zs = $('stickZone'), zf = $('flickZone')
  zs.addEventListener('pointerdown', e => {
    e.preventDefault(); Snd.init(); hintSeen()   // 前の指の離しが届いていなくても、新しい指で上書きする
    sid = e.pointerId; { const z = zs.getBoundingClientRect(); sx = z.left + z.width / 2; sy = z.top + z.height / 2 }
    stick.className = 'on'; moveStick(e)
    try { zs.setPointerCapture(e.pointerId) } catch (err) {}
  })
  const moveStick = e => {
    if (e.pointerId !== sid) return
    let dx = e.clientX - sx, dy = e.clientY - sy
    const len = Math.hypot(dx, dy)
    if (len > STICK_R) { dx = dx / len * STICK_R; dy = dy / len * STICK_R }
    knob.style.transform = `translate(${dx}px,${dy}px)`
    send(`move|${(dx / STICK_R).toFixed(2)},${(-dy / STICK_R).toFixed(2)}`)
  }
  zs.addEventListener('pointermove', moveStick)
  const endStick = e => { if (e.pointerId !== sid) return; sid = null; send('move|0,0'); idleStick() }
  zs.addEventListener('pointerup', endStick); zs.addEventListener('pointercancel', endStick)

  function drawArrow() {
    const len = Math.hypot(fdx, fdy)
    const k = len > FLICK_MAX ? FLICK_MAX / len : 1
    const ex = fx + fdx * k, ey = fy + fdy * k
    faOrigin.setAttribute('cx', fx); faOrigin.setAttribute('cy', fy)
    faLine.setAttribute('x1', fx); faLine.setAttribute('y1', fy); faLine.setAttribute('x2', ex); faLine.setAttribute('y2', ey)
    faDot.setAttribute('cx', ex); faDot.setAttribute('cy', ey)
    arrow.classList.toggle('weak', len < FLICK_MIN)
  }
  zf.addEventListener('pointerdown', e => {
    e.preventDefault(); Snd.init(); hintSeen()
    fid = e.pointerId; { const z = zf.getBoundingClientRect(); fx = z.left + z.width / 2; fy = z.top + z.height / 2 } fdx = e.clientX - fx; fdy = e.clientY - fy; if (Math.hypot(fdx, fdy) < 34) fdx = fdy = 0
    else { const l = Math.hypot(fdx, fdy); send(`aimdir|${(fdx / l).toFixed(3)},${(-fdy / l).toFixed(3)}`); aiming = true }   // 端を押したら、その向きの矢印をすぐ出す
    fmax = 0
    tx0 = e.clientX; ty0 = e.clientY
    drawArrow(); arrow.classList.add('on')
    try { zf.setPointerCapture(e.pointerId) } catch (err) {}
  })
  zf.addEventListener('pointermove', e => {
    if (e.pointerId !== fid) return
    fdx = e.clientX - fx; fdy = e.clientY - fy
    drawArrow()
    const len = Math.hypot(fdx, fdy); fmax = Math.max(fmax, Math.hypot(e.clientX - tx0, e.clientY - ty0))
    if (len >= FLICK_MIN) { send(`aimdir|${(fdx / len).toFixed(3)},${(-fdy / len).toFixed(3)}`); aiming = true }
    else if (aiming) { send('aimoff'); aiming = false }
  })
  const endFlick = (e, cancel) => {
    if (e.pointerId !== fid) return
    fid = null; aiming = false; arrow.classList.remove('on')
    const tap = fmax < 10 && Math.hypot(fdx, fdy) < 34   // 真ん中を軽く押しただけ
    const len = Math.hypot(fdx, fdy)
    const ux = fdx, uy = fdy; fdx = fdy = 0; drawArrow(); fdx = ux; fdy = uy
    if (cancel) { send('aimoff'); return }
    if (len >= FLICK_MIN && !tap) { lastFlick = { x: fdx / len, y: -fdy / len, t: performance.now() }; send(`dashdir|${(fdx / len).toFixed(3)},${(-fdy / len).toFixed(3)}`) }
    else if (tap) send('dashdir|0,0')   // 真ん中のタップは近い相手へ
    else send('aimoff')                 // 倒してから真ん中へ戻して離したら、やめる
  }
  zf.addEventListener('pointerup', e => endFlick(e, false)); zf.addEventListener('pointercancel', e => endFlick(e, true))
  // 視点: スティックの外をなぞる
  const zc = $('camZone')
  zc.addEventListener('pointerdown', e => { e.preventDefault(); Snd.init(); hintSeen(); cid = e.pointerId; cx = e.clientX; cy = e.clientY; try { zc.setPointerCapture(e.pointerId) } catch (err) {} })
  zc.addEventListener('pointermove', e => { if (e.pointerId !== cid) return; send(`tlook|${(e.clientX - cx).toFixed(1)},${(e.clientY - cy).toFixed(1)}`); cx = e.clientX; cy = e.clientY })
  const endCam = e => { if (e.pointerId === cid) cid = null }
  zc.addEventListener('pointerup', endCam); zc.addEventListener('pointercancel', endCam)
  { const bb = $('tBrace'); let bid = null
    bb.addEventListener('pointerdown', e => { e.preventDefault(); e.stopPropagation(); Snd.init(); bid = e.pointerId; send('brace|1'); try { bb.setPointerCapture(e.pointerId) } catch (err) {} })
    const bend = e => { if (e.pointerId !== bid) return; bid = null; send('brace|0') }
    bb.addEventListener('pointerup', bend); bb.addEventListener('pointercancel', bend) }
  const spb = $('tSpBtn')
  spb.addEventListener('pointerdown', e => {
    e.preventDefault(); e.stopPropagation(); hintSeen()
    spb.classList.add('press'); setTimeout(() => spb.classList.remove('press'), 120)
    // 直前にはじいた向きがあれば、その向きの相手を狙う（なければ近い相手）
    if (lastFlick && performance.now() - lastFlick.t < 2500) send(`specialdir|${lastFlick.x.toFixed(3)},${lastFlick.y.toFixed(3)}`)
    else send('specialnear')
  })

  // 最初の1回だけの操作説明
  let hintT = 0, hintShownAt = 0
  function showHint() {
    const h = $('hint'); h.hidden = false; h.classList.remove('fade'); hintShownAt = performance.now()
    clearTimeout(hintT); hintT = setTimeout(hintSeen, 9000)
  }
  function hintSeen() {
    const h = $('hint'); if (h.hidden) return
    if (performance.now() - hintShownAt < 700) return
    store.set('koma-hint', '1'); h.classList.add('fade'); setTimeout(() => { h.hidden = true }, 500)
  }
  new MutationObserver(() => { if (!$('touch').hidden) idleStick() }).observe($('touch'), { attributes: true, attributeFilter: ['hidden'] })
  addEventListener('resize', () => { if (!$('touch').hidden && sid === null) idleStick() })

  // ---------- 確認用（?debug で手元のサーバーへ記録を送る） ----------
  if (DEBUG) {
    const post = o => { try { fetch('/log', { method: 'POST', body: JSON.stringify(o) }) } catch (e) {} }
    post({ t: 'boot', ua: navigator.userAgent, w: innerWidth, h: innerHeight, dpr: devicePixelRatio, vv: window.visualViewport && visualViewport.height })
    addEventListener('error', e => post({ t: 'error', m: e.message, f: e.filename, l: e.lineno }))
    let fr = 0, t0 = performance.now()
    const loop = () => { fr++; requestAnimationFrame(loop) }; requestAnimationFrame(loop)
    setInterval(() => {
      const now = performance.now(), f = Math.round(fr * 1000 / (now - t0)); fr = 0; t0 = now
      const s = KB.state
      post({ t: 'tick', screen: KB.screen, ready: KB.ready, progress, raf: f, fps: KB.fps, ph: s && s.ph, tm: s && s.tm, me: KB.me && { sp: KB.me.sp, wx: KB.me.wx, wz: KB.me.wz }, errs: window.__kbErrs.slice(-3), snd: Snd.ctx && Snd.ctx.state, played: Object.keys(Snd.played || {}).length, mem: performance.memory && performance.memory.usedJSHeapSize, res: KB.result && KB.result.win })
    }, 2000)
  }

  show('title')
  renderHeader(); fightBtn()
})()
