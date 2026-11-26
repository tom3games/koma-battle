// 高い所からの着地を数字で測る。node land.mjs [url]
// 各高さから落とし（真下・横8m/s）、着地ごとの 跳ね返りの高さ・水平の速さの変化・回転数の減り・傾き(tl)・落ち着くまでの秒を出す
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const { b, p, errs } = await open(url, { w: 640, h: 360 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click())
await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1')
await p.evaluate(() => { window.__tr = []; const o = window.KB.onUnity; window.KB.onUnity = s => { o(s); if (s.startsWith('{"t":"s"')) { const m = JSON.parse(s); const k = m.k[0]; window.__tr.push({ t: m.tm, h: k.h, gh: k.gh, vx: k.vx, vz: k.vz, sp: k.sp, tl: k.tl, air: k.air }) } else if (s.includes('"k":"land"')) { const e = JSON.parse(s); if (e.i === 0) window.__tr.push({ land: e.p, n: window.__tr.filter(r => r.land === undefined).length }) } } })
const out = []
for (const [hh, vh] of [[3, 0], [6, 0], [10, 0], [16, 0], [10, 8], [16, 10]]) {
  await send(p, 'spin|1,0'); await send(p, 'tp|' + [0, 12, 0, 0, 0, 0].join(','))
  await wait(3500)
  const s0 = (await state(p)).k[0]
  await send(p, 'spin|1,0')
  await p.evaluate(() => { window.__tr.length = 0 })
  await send(p, 'tp|' + [s0.wx, s0.gh + hh, s0.wz, vh, 0, 0].join(','))
  await wait(5000)
  const tr = await p.evaluate(() => window.__tr.slice())
  const st = tr.filter(r => r.land === undefined), lands = tr.filter(r => r.land !== undefined)
  const first = lands[0]
  let impact = first ? first.land : 0, rebound = 0, hBefore = 0, hAfter = 0, spDrop = 0, tlMax = 0, settle = null
  if (first) {
    const i0 = first.n  // 着地イベントの直後に来る状態の番号
    const vb = st[Math.max(0, i0 - 1)], va = st[Math.min(st.length - 1, i0 + 2)]
    hBefore = Math.hypot(vb.vx, vb.vz); hAfter = Math.hypot(va.vx, va.vz)
    const nextLand = lands[1] ? lands[1].n : st.length
    for (let i = i0; i < nextLand; i++) rebound = Math.max(rebound, st[i].h - st[i].gh)
    spDrop = (st[Math.max(0, i0 - 1)].sp - st[Math.min(st.length - 1, i0 + 6)].sp) * 100
    tlMax = Math.max(...st.slice(i0).map(r => r.tl ?? 0))
    const t0 = st[i0].t
    for (let i = i0; i < st.length; i++) { if (st.slice(i).every(r => (r.tl ?? 0) < 1.5)) { settle = +(st[i].t - t0).toFixed(2); break } }
  }
  out.push({ fall: hh, vh, impactV: +impact.toFixed(1), contacts: lands.length, rebound_m: +rebound.toFixed(2), hspeed: hBefore.toFixed(1) + '>' + hAfter.toFixed(1), spinDrop_pt: +spDrop.toFixed(2), tiltMax: +tlMax.toFixed(1), settle_s: settle })
}
console.table(out)
console.log('errs', JSON.stringify(errs))
await b.close()
