// 画面（HTML）側: 読み込み・タイトル・パーツ選び・HUD・スマホの操作・演出・音・結果。3D と物理は Unity 側
window.__jsUp = true
;(() => {
  const $ = id => document.getElementById(id)
  const SLOTS = [
    { key: 'top', name: '上', parts: [
      { n: 'ブラッドファング', d: 'とげ4本。当てると痛い' },
      { n: 'アイギスウォール', d: '分厚く重い。受けに強い' },
      { n: 'ゼファーウイング', d: '軽い羽。空中で曲がれる' }] },
    { key: 'core', name: '芯', parts: [
      { n: 'エターナルコア', d: '長く回る' },
      { n: 'ソニックコア', d: '速い。ダッシュが早く戻る' },
      { n: 'グラビティコア', d: '重い。押し負けない' }] },
    { key: 'leg', name: '足', parts: [
      { n: 'タイタンベース', d: '踏ん張る。飛ばされにくい' },
      { n: 'スティンガーニードル', d: 'よく滑る。回転が減りにくい' },
      { n: 'ヘヴンズスプリング', d: '高く跳ぶ。上からの一撃が重い' }] },
  ]
  const NAMES = ['あなた', 'クリムゾン・ガルム', 'アイアン・ゴーレム', 'ミラージュ・ヴァイパー']
  const COLORS = ['#f2402a', '#2673f2', '#33bf4d', '#fabf1a']
  const SPECIAL = 'メテオ・ジャッジメント'
  const isTouch = matchMedia('(pointer: coarse)').matches
  const DEBUG = /debug/.test(location.search)

  // パーツの横からの絵（自前の SVG）
  const ART = {
    top: [
      '<path d="M14 26h52l-4 8H18z" fill="#ff6a4a"/><path d="M4 24l14-4v8zM76 24l-14-4v8z" fill="#d7dbe6"/><rect x="32" y="12" width="16" height="12" rx="2" fill="#8a90a8"/>',
      '<path d="M6 22h68l-6 14H12z" fill="#ff6a4a"/><rect x="4" y="20" width="72" height="4" fill="#d7dbe6"/><rect x="32" y="10" width="16" height="12" rx="2" fill="#8a90a8"/>',
      '<path d="M20 26h40l-3 6H23z" fill="#ff6a4a"/><path d="M20 26L2 18l4 10zM60 26l18-8-4 10z" fill="#ffb09c"/><rect x="32" y="13" width="16" height="13" rx="2" fill="#8a90a8"/>'],
    core: [
      '<rect x="28" y="8" width="24" height="26" rx="3" fill="#59d95a"/><ellipse cx="40" cy="8" rx="9" ry="3" fill="#ffd24a"/>',
      '<rect x="30" y="10" width="20" height="24" rx="3" fill="#4d99ff"/><path d="M30 18h20M30 24h20" stroke="#bcd6ff" stroke-width="2"/><ellipse cx="40" cy="10" rx="8" ry="3" fill="#ffd24a"/>',
      '<rect x="24" y="10" width="32" height="24" rx="3" fill="#5a5861"/><rect x="36" y="2" width="8" height="8" fill="#c9ccd8"/><ellipse cx="40" cy="10" rx="11" ry="3" fill="#ffd24a"/>'],
    leg: [
      '<path d="M24 10h32l-4 18H28z" fill="#5a5861"/><rect x="22" y="28" width="36" height="6" rx="2" fill="#c9ccd8"/>',
      '<path d="M26 8h28L40 38z" fill="#c9ccd8"/>',
      '<rect x="26" y="8" width="28" height="5" rx="2" fill="#ffd24a"/><rect x="26" y="15" width="28" height="5" rx="2" fill="#5a5861"/><rect x="26" y="22" width="28" height="5" rx="2" fill="#ffd24a"/><path d="M34 27h12l-2 9h-8z" fill="#c9ccd8"/>'],
  }

  // Unity の Parts.Compute と同じ計算（表示用）
  function stats(t, c, l) {
    const s = { atk: 1, def: 1, mass: 1, stamina: 1, speed: 1, jump: 1, grip: 1 }
    if (t === 0) { s.atk = 1.35; s.def = 0.85; s.mass = 1.0 }
    if (t === 1) { s.atk = 0.85; s.def = 1.4; s.mass = 1.3 }
    if (t === 2) { s.atk = 1.05; s.def = 0.95; s.mass = 0.82 }
    if (c === 0) { s.stamina = 1.35; s.speed *= 0.92 }
    if (c === 1) { s.stamina = 0.85; s.speed *= 1.22 }
    if (c === 2) { s.stamina = 1.05; s.speed *= 0.95; s.mass += 0.35; s.def *= 1.1 }
    if (l === 0) { s.grip = 1.7; s.jump = 0.85; s.speed *= 0.95 }
    if (l === 1) { s.grip = 0.6; s.speed *= 1.15 }
    if (l === 2) { s.jump = 1.45 }
    return s
  }
  const STAT_ROWS = [['攻撃', 'atk', 0.8, 1.4], ['防御', 'def', 0.8, 1.6], ['重さ', 'mass', 0.8, 1.7], ['持久', 'stamina', 0.8, 1.4], ['速さ', 'speed', 0.8, 1.45], ['跳躍', 'jump', 0.8, 1.5]]

  const store = {
    get(k, d) { try { const v = localStorage.getItem(k); return v === null ? d : v } catch (e) { return d } },
    set(k, v) { try { localStorage.setItem(k, v) } catch (e) {} },
  }
  const pick = { top: 0, core: 0, leg: 0 }
  try { const s = JSON.parse(store.get('koma-pick', 'null')); if (s) Object.assign(pick, s) } catch (e) {}
  let nCpu = +store.get('koma-ncpu', 2) || 2

  // ---------- Unity ----------
  let unity = null, progress = 0
  const KB = window.KB = { state: null, events: [], ready: false, send, onUnity, screen: 'title' }
  function send(cmd) { if (unity) unity.SendMessage('KomaBoot', 'Cmd', cmd) }

  const canvas = $('unity-canvas')
  const B = window.KB_BUILD
  const loaderScript = document.createElement('script')
  loaderScript.src = B.loader
  loaderScript.onload = () => {
    createUnityInstance(canvas, {
      dataUrl: B.data, frameworkUrl: B.framework, codeUrl: B.code,
      streamingAssetsUrl: 'StreamingAssets', companyName: B.company, productName: B.product, productVersion: B.version,
      // スマホは描く量を抑える（画素の倍率を下げる）
      devicePixelRatio: isTouch ? Math.min(window.devicePixelRatio || 1, 1.5) : Math.min(window.devicePixelRatio || 1, 1.25),
      showBanner: (msg, type) => { if (type === 'error') console.error(msg); else console.warn(msg) },
    }, p => { progress = p; $('ldFill').style.width = Math.round(p * 100) + '%'; fightBtn() })
      .then(inst => { unity = inst; window.unityInstance = inst })
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
        Snd.loop('bgm', 'TemplateData/snd/music_catch-me-if-you-can_dominoespizzaakacatchme_0.mp3', 0.22)
        break
      case 'hit': {
        const big = m.big, air = m.air
        Snd.play(big ? 'clang' : 'hit', { vol: Math.min(1, 0.45 + m.p / 25) })
        if (air) { lines(m.x, m.y, 1); onoma('ドゴォッ', m.x, m.y); flash(); Snd.play('crowd', { vol: 0.5 }); Snd.duck(0.5, 0.8) }
        else if (big) { lines(m.x, m.y, 0.7); onoma('ガギィン', m.x, m.y) }
        break
      }
      case 'wall': Snd.play('hit', { vol: 0.35 }); break
      case 'dash': if (m.i === 0) Snd.play('swish', { vol: 0.7 }); else Snd.play('swish', { vol: 0.25 }); break
      case 'dive': Snd.play('whoosh', { vol: m.i === 0 ? 0.8 : 0.4, rate: 1.4 }); if (m.i === 0 && !m.p) lines(m.x, m.y, 0.5); break
      case 'jump': Snd.play('spring', { vol: m.i === 0 ? 0.7 : 0.3 }); break
      case 'special':
        banner(m.i); lines(m.x, m.y, 1); Snd.play('whoosh', { vol: 0.9 }); Snd.duck(0.35, 1.2)
        break
      case 'boom': lines(m.x, m.y, 1); flash(); onoma('ズドン', m.x, m.y); Snd.play('boom', { vol: 1 }); Snd.play('crowd', { vol: 0.6 }); break
      case 'ko':
        Snd.play('down', { vol: m.i === 0 ? 1 : 0.7 })
        if (m.i !== 0) Snd.play('crowd', { vol: 0.45 })
        onoma(m.why === 'out' ? '場外' : 'ダウン', m.x, Math.max(0.25, m.y))
        break
      case 'sudden': $('sudden').hidden = false; break
      case 'end': result(m); break
    }
  }

  // ---------- タイトル（読み込み中から触れる） ----------
  $('btnPlay').onclick = () => { Snd.init(); startAmb(); Snd.play('ui', { vol: 0.6 }); toSelect() }
  function startAmb() { Snd.loop('amb', 'TemplateData/snd/crowd-shoutingspeaking-ambience_crowd_shouting_0.mp3', 0.16) }

  // ---------- パーツ選び ----------
  function buildSlots() {
    const box = $('slots'); box.innerHTML = ''
    SLOTS.forEach(sl => {
      const d = document.createElement('div'); d.className = 'slot'
      d.innerHTML = `<div class="slot-name">${sl.name}</div><div class="slot-row"></div><div class="slot-desc">${sl.parts[pick[sl.key]].d}</div>`
      const row = d.querySelector('.slot-row')
      sl.parts.forEach((p, i) => {
        const b = document.createElement('button'); b.className = 'part' + (pick[sl.key] === i ? ' on' : '')
        b.dataset.slot = sl.key; b.dataset.i = i
        b.innerHTML = `<svg viewBox="0 0 80 40">${ART[sl.key][i]}</svg><b>${p.n}</b>`
        b.onclick = () => { pick[sl.key] = i; Snd.play('ui', { vol: 0.4 }); buildSlots(); sendLoadout() }
        row.appendChild(b)
      })
      box.appendChild(d)
    })
    const s = stats(pick.top, pick.core, pick.leg)
    $('stats').innerHTML = STAT_ROWS.map(([n, k, lo, hi]) => {
      const w = Math.max(8, Math.min(100, (s[k] - lo) / (hi - lo) * 100))
      return `<div class="st${w > 62 ? ' hi' : ''}"><span>${n}</span><i><u style="width:${w}%"></u></i></div>`
    }).join('')
    store.set('koma-pick', JSON.stringify(pick))
  }
  function sendLoadout() { send(`loadout|${pick.top},${pick.core},${pick.leg}`) }
  function toSelect() { buildSlots(); show('select'); send('select'); sendLoadout(); fightBtn() }
  document.querySelectorAll('#vsPick button').forEach(b => {
    b.classList.toggle('on', +b.dataset.n === nCpu)
    b.onclick = () => {
      nCpu = +b.dataset.n
      document.querySelectorAll('#vsPick button').forEach(x => x.classList.toggle('on', x === b))
      store.set('koma-ncpu', nCpu)
    }
  })
  $('btnFight').onclick = () => { if (!KB.ready) return; Snd.init(); startAmb(); Snd.play('ui', { vol: 0.6 }); startMatch() }
  function startMatch() {
    if (document.activeElement) document.activeElement.blur()
    canvas.focus()
    $('sudden').hidden = true
    tagEls.forEach(t => t && t.remove()); tagEls.length = 0
    show('hud')
    if (isTouch && store.get('koma-hint', '') !== '1') showHint()
    send('move|0,0')
    send(`start|${pick.top},${pick.core},${pick.leg},${nCpu}`)
  }

  // ---------- HUD ----------
  const tagEls = []
  let lastSpin = -1, frames = 0, fpsT = performance.now(), fps = 0
  function hud(m) {
    frames++
    const now = performance.now(); if (now - fpsT > 1000) { fps = Math.round(frames * 1000 / (now - fpsT)); frames = 0; fpsT = now; KB.fps = fps }
    if (KB.screen !== 'hud') return
    const t = Math.floor(m.tm)
    $('hudTime').textContent = m.ph === 'count' ? '' : `${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')}`
    const me = m.k.find(k => k.i === 0)
    if (me) {
      const sp = Math.round(me.sp * 100)
      if (sp !== lastSpin) { $('tNum').textContent = sp; lastSpin = sp }
      $('tSpin').style.strokeDasharray = `${(292.2 * me.sp).toFixed(1)} 389.6`
      $('tGauge').style.strokeDasharray = `${(235.6 * me.g).toFixed(1)} 314.2`
      const tc = document.querySelector('.tacho')
      tc.classList.toggle('full', me.g >= 1 && me.a === 1)
      tc.classList.toggle('low', me.sp < 0.3)
      $('tDash').classList.toggle('wait', me.cd > 0)
      $('tSpBtn').classList.toggle('full', me.g >= 1 && me.a === 1)
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
        el.innerHTML = `<span>${NAMES[k.i]}</span><i><u></u></i>`
        $('tags').appendChild(el)
      }
      el.classList.toggle('out', k.a === 0)
      el.style.display = k.z > 0 && k.h > -6 ? '' : 'none'
      el.style.transform = `translate(${(k.x * W).toFixed(0)}px,${((1 - k.y) * H).toFixed(0)}px) translate(-50%,-100%)`
      el.querySelector('u').style.width = (k.sp * 100).toFixed(0) + '%'
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
  function banner(i) {
    const b = $('banner')
    b.querySelector('span').textContent = SPECIAL
    b.style.filter = i === 0 ? '' : 'hue-rotate(200deg) saturate(.7)'
    b.classList.remove('on'); void b.offsetWidth; b.classList.add('on')
  }
  function countdown(text, go) {
    const c = $('count'); c.textContent = text
    c.className = 'count' + (go ? ' go' : ''); void c.offsetWidth; c.classList.add('pop')
  }

  // ---------- 結果 ----------
  function result(m) {
    Snd.loop('bgm', null)
    Snd.play(m.win ? 'win' : 'lose', { vol: 0.9 })
    const big = $('rsBig')
    big.textContent = m.win ? '勝ち' : '負け'
    big.className = 'rs-big ' + (m.win ? 'win' : 'lose')
    $('rsWhy').textContent = m.win ? '最後まで回り切った' : m.why === 'out' ? '場外に飛ばされた' : '回転が尽きた'
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
    crowd: [S('crowd-shoutingspeaking-ambience_crowd_shouting_0')],
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
  let fid = null, fx = 0, fy = 0, fdx = 0, fdy = 0
  const STICK_R = 50, FLICK_MIN = 22, FLICK_MAX = 120
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
    if (sid !== null) return
    e.preventDefault(); Snd.init(); hintSeen()
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
    if (fid !== null) return
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
    if (len >= FLICK_MIN) send(`aimdir|${(fdx / len).toFixed(3)},${(-fdy / len).toFixed(3)}`)
  })
  const endFlick = (e, cancel) => {
    if (e.pointerId !== fid) return
    fid = null; arrow.classList.remove('on')
    const len = Math.hypot(fdx, fdy)
    if (cancel) { send('aimoff'); return }
    if (len >= FLICK_MIN) send(`dashdir|${(fdx / len).toFixed(3)},${(-fdy / len).toFixed(3)}`)
    else send('dashdir|0,0')
  }
  zf.addEventListener('pointerup', e => endFlick(e, false)); zf.addEventListener('pointercancel', e => endFlick(e, true))
  const spb = $('tSpBtn')
  spb.addEventListener('pointerdown', e => {
    e.preventDefault(); e.stopPropagation(); hintSeen()
    spb.classList.add('press'); setTimeout(() => spb.classList.remove('press'), 120)
    send('specialnear')
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
