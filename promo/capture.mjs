import '../mute-auto.mjs'
// 宣伝用のプレイ動画の素材を撮る（音は出さない）。node promo/capture.mjs
// 実時間で動かし、画面を CDP のスクリーンキャストでコマごとに保存。鳴らした音と場面の時刻も記録する。編集は promo/edit.mjs
// ゲーム本体は変えない。自動操作の自分が戦い、見どころは検査用の命令（setpas・tp・gauge・strike・spin）で順に起こす
import { chromium } from 'playwright'
import { MUTE_LAUNCH, muteContext } from '../mute.mjs'
import { spawn } from 'node:child_process'
import { mkdirSync, writeFileSync } from 'node:fs'

const ROOT = import.meta.dirname + '/..'
const OUT = process.env.KP_OUT || '/private/tmp/claude-501/-Users-tomoki-Desktop-tmk-hub/9116bcd6-5450-4eca-92c7-248c73c1167b/scratchpad/kp-video'
const RUN = process.env.KP_RUN || 'take1'
const DIR = `${OUT}/${RUN}`, FR = `${DIR}/frames`
mkdirSync(FR, { recursive: true })
const DPR = 2, PORT = 5589
const server = spawn(process.execPath, ['server.mjs'], { cwd: ROOT, env: { ...process.env, PORT: String(PORT), ROOT: 'docs' } })
const sleep = ms => new Promise(r => setTimeout(r, ms))
await sleep(600)

const b = await chromium.launch({ channel: 'chromium', headless: true, args: [...MUTE_LAUNCH.args, '--use-angle=metal', '--enable-gpu', '--ignore-gpu-blocklist'] })
const ctx = await b.newContext({ viewport: { width: 1080, height: 1920 }, deviceScaleFactor: 1, hasTouch: true, isMobile: true, locale: 'ja-JP' })
await ctx.addInitScript(dpr => {
  const fix = () => { const m = document.querySelector('meta[name=viewport]'); if (m && !m.__f) { m.__f = 1; m.setAttribute('content', 'width=540,user-scalable=no') } }
  new MutationObserver(fix).observe(document, { childList: true, subtree: true }); fix()
  Object.defineProperty(window, 'devicePixelRatio', { get: () => dpr })
  try { localStorage.clear(); localStorage.setItem('koma-hint', '1'); localStorage.setItem('koma-prof', JSON.stringify({ xp: 70, win: 4, lose: 2, seen: 1, name: '' })) } catch (e) {}
}, DPR)
await muteContext(ctx)
await ctx.addInitScript(() => {
  addEventListener('DOMContentLoaded', () => {
    const st = document.createElement('style')
    st.textContent = `.__tap{position:fixed;z-index:2147483647;pointer-events:none;width:64px;height:64px;margin:-32px 0 0 -32px;border-radius:50%;background:rgba(255,255,255,.38);border:3px solid rgba(255,255,255,.95);box-shadow:0 0 0 1.5px rgba(0,0,0,.28);animation:__tap .5s ease-out forwards}
      @keyframes __tap{0%{transform:scale(.45);opacity:1}60%{opacity:.9}100%{transform:scale(1.25);opacity:0}}`
    document.head.appendChild(st)
  })
  addEventListener('pointerdown', e => { const d = document.createElement('div'); d.className = '__tap'; d.style.left = e.clientX + 'px'; d.style.top = e.clientY + 'px'; document.body.appendChild(d); setTimeout(() => d.remove(), 650) }, true)
})
const p = await ctx.newPage(); const errs = []
p.on('response', r => r.status() >= 400 && !/fonts\.g/.test(r.url()) && errs.push(r.status() + ' ' + r.url())); p.on('pageerror', e => errs.push(e.message)); p.on('console', m => m.type() === 'error' && errs.push(m.text().slice(0, 200)))
await p.goto(`http://localhost:${PORT}/`); await p.waitForFunction(() => window.__kbReady === true, null, { timeout: 120000 })
await p.evaluate(() => document.fonts.ready); await sleep(1500)
await p.evaluate(() => {
  window.__sndLog = []
  const op = Snd.play.bind(Snd), ol = Snd.loop.bind(Snd)
  Snd.play = (name, o = {}) => { const url = o.url || Snd.pick(name); window.__sndLog.push({ t: Date.now(), k: 'play', name, url, vol: o.vol == null ? 1 : o.vol, rate: o.rate || 1, delay: o.delay || 0 }); return op(name, o) }
  Snd.loop = (slot, url, vol = 0.3, fade = 0.6) => { window.__sndLog.push({ t: Date.now(), k: 'loop', slot, url, vol, fade }); return ol(slot, url, vol, fade) }
})
// ---- 撮り始め
const cdp = await ctx.newCDPSession(p)
const frames = []; let n = 0
cdp.on('Page.screencastFrame', f => {
  const name = String(n++).padStart(6, '0') + '.jpg'
  writeFileSync(`${FR}/${name}`, Buffer.from(f.data, 'base64'))
  frames.push({ f: name, t: f.metadata.timestamp * 1000 })
  cdp.send('Page.screencastFrameAck', { sessionId: f.sessionId }).catch(() => {})
})
await cdp.send('Page.startScreencast', { format: 'jpeg', quality: 92, everyNthFrame: 1 })
const marks = [], taps = []
const mark = name => marks.push({ name, t: Date.now() })
const send = c => p.evaluate(c => KB.send(c), c)
const tap = async (sel, what) => { const xy = await p.evaluate(s => { const r = document.querySelector(s).getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 } }, sel); taps.push({ t: Date.now(), what }); await p.touchscreen.tap(xy.x, xy.y) }
const st = () => p.evaluate(() => KB.state)

mark('title'); await sleep(2600)
mark('toHub'); await tap('#btnPlay', 'play'); await sleep(2300)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="2"]').click(); Object.assign(KB.pick, { top: 0, core: 0, leg: 2, dir: 1, sp: 0, pas: 0 }) })
mark('toFight'); await tap('#btnFight', 'fight')
await p.waitForFunction(() => KB.state && KB.state.ph === 'count', null, { timeout: 30000 })
await p.waitForFunction(() => KB.state && KB.state.ph === 'fight', null, { timeout: 30000 })
await send('auto|1'); await send('cpupas|-1'); mark('fight')
await sleep(4000); mark('fast1'); await sleep(12000); mark('h1')
// 見どころ1: 高い所から落ちる（天翔の羽）→ 着地
{ const s = (await st()).k[0]; await send('setpas|1,0'); await send('tp|' + [s.wx, s.gh + 15, s.wz, 0, 0, 0].join(',')); await sleep(5000) }
mark('h2')
// 見どころ2: 逆鱗（回転が減るとオーラ）
await send('setpas|0,0'); await send('spin|0.22,0'); await sleep(3600); await send('spin|0.9,0')
mark('h3')
// 見どころ3: 雷鳴の刻印（予告の円 → 落雷）
{ const k = (await st()).k; const t = k.find(x => x.i !== 0 && x.a === 1); await send('setpas|3,0'); if (t) await send(`strike|${t.i},0`); await sleep(4600) }
mark('h4')
// 見どころ4: 必殺（メテオ・ジャッジメント）
await send('gauge|100'); await send('specialnear'); await sleep(4800)
mark('fin')
for (const i of [1, 2]) await send(`spin|0.01,${i}`)
await p.waitForFunction(() => KB.result, null, { timeout: 60000 }).catch(() => {})
mark('result'); await sleep(7500); mark('end')
await cdp.send('Page.stopScreencast'); await sleep(300)
const sndLog = await p.evaluate(() => window.__sndLog), muteOk = await p.evaluate(() => window.__muteOk && window.__muteOk())
const res = await p.evaluate(() => KB.result)
writeFileSync(`${DIR}/log.json`, JSON.stringify({ frames, marks, taps, sndLog, errs, muteOk, res }))
console.log('frames', frames.length, 'sec', ((frames.at(-1).t - frames[0].t) / 1000).toFixed(1), JSON.stringify(res), 'errs', errs, 'muteOk', muteOk)
await b.close(); server.kill()
