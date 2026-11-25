// 画面（HTML）側: 読み込み・タイトル・パーツ選び・HUD・スマホの操作・演出・音・結果。3D と物理は Unity 側
window.__jsUp = true
const UI_V = (() => { try { return new URL(document.currentScript.src).searchParams.get('v') } catch (e) { return '' } })()
;(() => {
  const $ = id => document.getElementById(id)
  const SLOTS = [
    { key: 'top', name: '上', parts: [
      { n: 'ブラッドファング', d: '鋼の牙4本。当てると痛い' },
      { n: 'アイギスウォール', d: '分厚く重い盾。受けに強い' },
      { n: 'ゼファーウイング', d: '軽い羽。空中で曲がれる' },
      { n: 'ヴォーテックスエッジ', d: '6枚刃。攻めも守りもそこそこ' },
      { n: 'ルナティックサイズ', d: '大鎌2枚。一撃は最強、守りは紙' }] },
    { key: 'core', name: '芯', parts: [
      { n: 'エターナルコア', d: '長く回る' },
      { n: 'ソニックコア', d: '速い。ダッシュが早く戻る' },
      { n: 'グラビティコア', d: '重い。押し負けない' },
      { n: 'フェニックスコア', d: '必殺のゲージが早くたまる' },
      { n: 'アビスコア', d: '削った回転を少し吸い取る' }] },
    { key: 'leg', name: '足', parts: [
      { n: 'タイタンベース', d: '踏ん張る。飛ばされにくい' },
      { n: 'スティンガーニードル', d: 'よく滑る。回転が減りにくい' },
      { n: 'ヘヴンズスプリング', d: '高く跳ぶ。上からの一撃が重い' },
      { n: 'ファントムホイール', d: '最速。そのぶん回転が減りやすい' },
      { n: 'クレイモアスパイク', d: '踏ん張りと跳びの両方' }] },
    { key: 'sp', name: '必殺', parts: [
      { n: 'メテオ・ジャッジメント', d: '真上へ跳んで相手へ落ちる。衝撃波' },
      { n: 'テンペスト・ヴォルテックス', d: '竜巻で引き寄せて巻き上げ、吹き飛ばす' },
      { n: 'ファントム・レイド', d: '近い相手へ3連続の突進' },
      { n: 'イージス・リバース', d: '3秒の盾。弾き返し、回転を戻す' }] },
  ]
  const NAMES = ['あなた', 'クリムゾン・ガルム', 'アイアン・ゴーレム', 'ミラージュ・ヴァイパー']
  const COLORS = ['#c8432f', '#3d6aa8', '#5f8a4e', '#b8902e']
  const SPECIALS = SLOTS[3].parts.map(p => p.n)
  const isTouch = matchMedia('(pointer: coarse)').matches
  const DEBUG = /debug/.test(location.search)

  // パーツの横からの絵（自前の SVG・金属の灰と塗装の色）
  const cM = '#9aa1ab', cMD = '#555b64', cP = '#b8402e', cR = '#1c1e22', cB = '#b08a4a'
  const ART = {
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
  const pick = { top: 0, core: 0, leg: 0, sp: 0 }
  try { const s = JSON.parse(store.get('koma-pick2', 'null')); if (s) Object.assign(pick, s) } catch (e) {}
  for (const sl of SLOTS) if (!(pick[sl.key] >= 0 && pick[sl.key] < sl.parts.length)) pick[sl.key] = 0
  let nCpu = +store.get('koma-ncpu', 2) || 2

  // ---------- Unity ----------
  let unity = null, progress = 0
  const KB = window.KB = { state: null, events: [], ready: false, send, onUnity, screen: 'title' }
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
  function show(id) {
    for (const s of ['title', 'select', 'hud', 'result']) $(s).hidden = s !== id
    document.body.classList.toggle('playing', id === 'hud')
    document.body.classList.toggle('on-title', id === 'title')
    $('touch').hidden = !(id === 'hud' && isTouch)
    if (id !== 'hud') { resetTouch(); $('hint').hidden = true }
    KB.screen = id
  }

  function fightBtn() {
    const b = $('btnFight')
    b.disabled = !KB.ready
    b.textContent = KB.ready ? '試合開始' : `準備中 ${Math.round(progress * 100)}%`
  }

  function ev(m) {
    switch (m.k) {
      case 'ready':
        KB.ready = true; window.__kbReady = true
        $('ldMini').classList.add('done'); $('load').classList.add('gone')
        if (isTouch) { send('touchmode|1'); send('quality|0') }
        sendLoadout()
        send(KB.screen === 'select' ? 'select' : 'title')
        fightBtn()
        break
      case 'count': countdown(String(m.n), false); Snd.play('tick', { vol: 0.5 }); break
      case 'go':
        countdown('勝負', true); Snd.play('go', { vol: 0.8 })
        Snd.loop('bgm', 'TemplateData/snd/gyro_ragnarok_bgm.mp3', 0.3)
        break
      case 'hit': {
        const big = m.big, air = m.air
        Snd.play(big ? 'clang' : 'hit', { vol: Math.min(1, 0.45 + m.p / 25) })
        if (air) { lines(m.x, m.y, 1); onoma('ドゴォッ', m.x, m.y); flash(); Snd.duck(0.5, 0.8) }
        else if (big) { lines(m.x, m.y, 0.7); onoma('ガギィン', m.x, m.y) }
        break
      }
      case 'wall': Snd.play('hit', { vol: 0.35 }); break
      case 'dash': if (m.p === 2) { Snd.play('whoosh', { vol: 0.5, rate: 1.2 + (rush = (rush + 1) % 3) * 0.25 }); if (m.i === 0) lines(m.x, m.y, 0.4) } if (m.i === 0) Snd.play('swish', { vol: 0.7 }); else Snd.play('swish', { vol: 0.25 }); break
      case 'dive': Snd.play('whoosh', { vol: m.i === 0 ? 0.8 : 0.4, rate: 1.4 }); if (m.i === 0 && !m.p) lines(m.x, m.y, 0.5); break
      case 'boost': if (m.i === 0) { Snd.play('swish', { vol: 0.8, rate: 0.8 }); lines(m.x, m.y, 0.35) } break
      case 'jump': Snd.play('spring', { vol: m.i === 0 ? 0.7 : 0.3 }); break
      case 'special':
        banner(m.i, m.p); lines(m.x, m.y, 1); Snd.play('whoosh', { vol: 0.9 }); Snd.duck(0.35, 1.2)
        break
      case 'boom': lines(m.x, m.y, 1); flash(); onoma('ズドン', m.x, m.y); Snd.play('boom', { vol: 1 }); break
      case 'ko':
        Snd.play('down', { vol: m.i === 0 ? 1 : 0.7 })
        onoma(m.why === 'out' ? '場外' : 'ダウン', m.x, Math.max(0.25, m.y))
        break
      case 'sudden': $('sudden').hidden = false; break
      case 'end': result(m); break
    }
  }

  // ---------- タイトル（読み込み中から触れる） ----------
  $('btnPlay').onclick = () => { Snd.init(); Snd.play('ui', { vol: 0.6 }); toSelect() }

  // ---------- パーツ選び ----------
  // パーツ選び: 1段に1つ、左右で切り替える（並べすぎない）
  function slotBody(sl) {
    const i = pick[sl.key], p = sl.parts[i], n = sl.parts.length
    const art = ART[sl.key] ? `<svg viewBox="0 0 80 40">${ART[sl.key][i]}</svg>` : ''
    return `${art}<div class="slot-txt"><b>${p.n}</b><small>${p.d}</small></div><i class="slot-no">${i + 1}/${n}</i>`
  }
  function buildSlots() {
    const box = $('slots'); box.innerHTML = ''
    SLOTS.forEach(sl => {
      const n = sl.parts.length
      const d = document.createElement('div'); d.className = 'slot' + (sl.key === 'sp' ? ' sp' : '')
      d.innerHTML = `<div class="slot-name">${sl.name}</div><button class="arw" data-d="-1" aria-label="前">${ARROW_L}</button><div class="slot-body">${slotBody(sl)}</div><button class="arw" data-d="1" aria-label="次">${ARROW_R}</button>`
      d.dataset.slot = sl.key
      // 2026-11-25 本人「左右のボタンが反応が悪い」: click を待たず押した瞬間に切り替え、段は作り直さず中身だけ書き換える。
      // Unity への送信（コマの作り直し）は連打をまとめて1回
      const step = dd => { pick[sl.key] = (pick[sl.key] + dd + n) % n; d.querySelector('.slot-body').innerHTML = slotBody(sl); renderStats(); sendLoadoutSoon() }
      d.querySelectorAll('.arw').forEach(b => {
        b.onpointerdown = e => { if (e.button > 0) return; e.preventDefault(); step(+b.dataset.d); Snd.play('ui', { vol: 0.4 }) }
        b.onclick = e => { if (e.detail === 0) step(+b.dataset.d) }
      })
      box.appendChild(d)
    })
    renderStats()
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
  function sendLoadout() { send(`loadout|${pick.top},${pick.core},${pick.leg},${pick.sp}`) }
  let loadoutT = 0
  function sendLoadoutSoon() { clearTimeout(loadoutT); loadoutT = setTimeout(sendLoadout, 140) }
  function toSelect() { buildSlots(); show('select'); send('select'); sendLoadout(); fightBtn() }
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
    send(`start|${pick.top},${pick.core},${pick.leg},${nCpu},${pick.sp}`)
    lockMouse()
  }

  // ---------- HUD ----------
  const tagEls = []
  let rush = 0
  const tagParts = []
  let lastSpin = -1, frames = 0, fpsT = performance.now(), fps = 0
  function hud(m) {
    frames++
    const now = performance.now(); if (now - fpsT > 1000) { fps = Math.round(frames * 1000 / (now - fpsT)); frames = 0; fpsT = now; KB.fps = fps }
    if (KB.screen !== 'hud') return
    const t = Math.floor(m.tm)
    $('hudTime').textContent = m.ph === 'count' ? '' : `${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')}`
    const me = m.k.find(k => k.i === 0)
    if (me) {
      // 2026-11-25 本人「左上に残り回転数みたいなの出すのやめて」: 数字は出さない。回転は名札の細い線とコマの揺れで見せる
      const full = me.g >= 1 && me.a === 1
      $('spReady').classList.toggle('on', full)
      $('tSpBtn').classList.toggle('full', full)
      $('tSpBtn').style.setProperty('--g', (me.g * 100).toFixed(0) + '%')
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
        el.innerHTML = `<span>${NAMES[k.i]}</span><i><u></u></i>` + (k.i === 0 ? '<i class="g"><u></u></i>' : '')
        $('tags').appendChild(el)
        tagParts[k.i] = { u: el.querySelector('u'), g: el.querySelector('.g u') }
      }
      el.classList.toggle('out', k.a === 0)
      el.style.display = k.z > 0 && k.h > -6 ? '' : 'none'
      el.style.transform = `translate(${(k.x * W).toFixed(0)}px,${((1 - k.y) * H).toFixed(0)}px) translate(-50%,-100%)`
      const tp = tagParts[k.i]
      tp.u.style.width = (k.sp * 100).toFixed(0) + '%'
      el.classList.toggle('low', k.sp < 0.3)
      if (tp.g) tp.g.style.width = (k.g * 100).toFixed(0) + '%'
    })
  }

  // ---------- 演出 ----------
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
  function banner(i, kind) {
    const b = $('banner')
    b.querySelector('span').textContent = SPECIALS[kind | 0] || SPECIALS[0]
    b.style.filter = i === 0 ? '' : 'hue-rotate(200deg) saturate(.7)'
    b.classList.remove('on'); void b.offsetWidth; b.classList.add('on')
  }
  function countdown(text, go) {
    const c = $('count'); c.textContent = text
    c.className = 'count' + (go ? ' go' : ''); void c.offsetWidth; c.classList.add('pop')
  }

  // ---------- 結果 ----------
  function result(m) {
    if (document.pointerLockElement && document.exitPointerLock) document.exitPointerLock()
    Snd.loop('bgm', null)
    Snd.play(m.win ? 'win' : 'lose', { vol: 0.9 })
    const big = $('rsBig')
    big.textContent = m.win ? '勝ち' : '負け'
    big.className = 'rs-big ' + (m.win ? 'win' : 'lose')
    $('rsWhy').textContent = m.win ? '最後まで回り切った' : m.why === 'out' ? '場外に落ちた' : '回転が尽きた'
    $('rsLine').textContent = `${Math.floor(m.time)}秒　削った回転 ${Math.round(m.dealt)}　上からの一撃 ${m.aerial}回　必殺 ${m.specials}回`
    $('onoma').classList.remove('on'); $('banner').classList.remove('on')
    show('result')
    KB.result = m
  }
  $('btnAgain').onclick = () => { Snd.play('ui', { vol: 0.6 }); startMatch() }
  $('btnParts').onclick = () => { Snd.play('ui', { vol: 0.4 }); toSelect() }

  // ---------- 音 ----------
  const S = n => 'TemplateData/snd/' + n + '.mp3'
  Snd.def({
    hit: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_02'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_03'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_hit_04'), S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_hit_01')],
    clang: [S('100-cc0-sfx_100-CC0-SFX_0_metal_04'), S('100-cc0-sfx_100-CC0-SFX_0_metal_12')],
    boom: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_hit_01')],
    down: [S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_falling_01'), S('75-cc0-breaking-falling-hit-sfx_sfx_breaking_and_falling_bfh1_metal_falling_02')],
    swish: [S('swishes-sound-pack_swishes_swishes_swish-7'), S('swishes-sound-pack_swishes_swishes_swish-9'), S('swishes-sound-pack_swishes_swishes_swish-3')],
    spring: [S('100-cc0-sfx_100-CC0-SFX_0_spring_03'), S('100-cc0-sfx_100-CC0-SFX_0_spring_05')],
    whoosh: [S('air-whoosh_whoosh2_0')],
    tick: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_HIT_jingles_HIT03')],
    go: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_STEEL_jingles_STEEL04')],
    win: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_STEEL_jingles_STEEL00')],
    lose: [S('85-short-music-jingles_jingleSounds_Kenney_OGG_jingles_STEEL_jingles_STEEL09')],
    ui: [S('100-cc0-sfx-2_sfx_100_v2_sfx100v2_metal_hit_01')],
  })
  let muted = store.get('koma-mute', '0') === '1'
  function applyMute() { Snd.setMute(muted); $('muteX').hidden = !muted; $('muteWave').style.display = muted ? 'none' : '' }
  applyMute()
  $('btnMute').onclick = () => { muted = !muted; Snd.init(); applyMute(); store.set('koma-mute', muted ? '1' : '0') }

  // ---------- スマホの操作 ----------
  // 左: 触った所が中心になる浮くスティック。右: はじいた向きへダッシュ（空中なら急降下）。タップだけなら近くの相手へ。星のボタンで必殺
  const stick = $('stick'), knob = stick.querySelector('i')
  const arrow = $('flickArrow'), faLine = $('faLine'), faDot = $('faDot'), faOrigin = $('faOrigin')
  let sid = null, sx = 0, sy = 0
  let fid = null, fx = 0, fy = 0, fdx = 0, fdy = 0, lastFlick = null, aiming = false
  // 通知・タブ切替・ツールバーで指の離しが届かなかった時も、入力を残さない
  const dropTouch = () => resetTouch()
  addEventListener('blur', dropTouch); addEventListener('pagehide', dropTouch)
  document.addEventListener('visibilitychange', () => { if (document.visibilityState !== 'visible') dropTouch() })
  const STICK_R = 50, FLICK_MIN = 22, FLICK_MAX = 70
  function idleStick() {
    const z = $('stickZone').getBoundingClientRect()
    stick.style.transform = `translate(${z.left + z.width * 0.4}px,${z.top + z.height * 0.62}px)`
    stick.className = 'idle'; knob.style.transform = ''
  }
  function resetTouch() {
    if (sid !== null) send('move|0,0')
    if (fid !== null) send('aimoff')
    sid = null; fid = null; arrow.classList.remove('on'); if (!$('touch').hidden) idleStick()
  }
  const zs = $('stickZone'), zf = $('flickZone')
  zs.addEventListener('pointerdown', e => {
    e.preventDefault(); Snd.init(); hintSeen()   // 前の指の離しが届いていなくても、新しい指で上書きする
    sid = e.pointerId; sx = e.clientX; sy = e.clientY
    stick.style.transform = `translate(${sx}px,${sy}px)`; stick.className = 'on'; knob.style.transform = ''
    try { zs.setPointerCapture(e.pointerId) } catch (err) {}
  })
  zs.addEventListener('pointermove', e => {
    if (e.pointerId !== sid) return
    let dx = e.clientX - sx, dy = e.clientY - sy
    const len = Math.hypot(dx, dy)
    if (len > STICK_R) { dx = dx / len * STICK_R; dy = dy / len * STICK_R }
    knob.style.transform = `translate(${dx}px,${dy}px)`
    send(`move|${(dx / STICK_R).toFixed(2)},${(-dy / STICK_R).toFixed(2)}`)
  })
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
    fid = e.pointerId; fx = e.clientX; fy = e.clientY; fdx = 0; fdy = 0
    drawArrow(); arrow.classList.add('on')
    try { zf.setPointerCapture(e.pointerId) } catch (err) {}
  })
  zf.addEventListener('pointermove', e => {
    if (e.pointerId !== fid) return
    fdx = e.clientX - fx; fdy = e.clientY - fy
    drawArrow()
    const len = Math.hypot(fdx, fdy)
    if (len >= FLICK_MIN) { send(`aimdir|${(fdx / len).toFixed(3)},${(-fdy / len).toFixed(3)}`); aiming = true }
    else if (aiming) { send('aimoff'); aiming = false }
  })
  const endFlick = (e, cancel) => {
    if (e.pointerId !== fid) return
    fid = null; aiming = false; arrow.classList.remove('on')
    const len = Math.hypot(fdx, fdy)
    if (cancel) { send('aimoff'); return }
    if (len >= FLICK_MIN) { lastFlick = { x: fdx / len, y: -fdy / len, t: performance.now() }; send(`dashdir|${(fdx / len).toFixed(3)},${(-fdy / len).toFixed(3)}`) }
    else send('dashdir|0,0')
  }
  zf.addEventListener('pointerup', e => endFlick(e, false)); zf.addEventListener('pointercancel', e => endFlick(e, true))
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
  fightBtn()
})()
