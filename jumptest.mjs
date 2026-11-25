// ジャンプ台（円い山）と加速板: 16方向から進入して「加速する・跳ぶ・埋まらない・引っかからない」を確かめる
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const { b, p, errs } = await open(url, { w: 640, h: 360 })
await p.click('#btnPlay'); await wait(200)
await p.click('.slot[data-slot="leg"] .arw[data-d="-1"]').catch(() => {})
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1')
const M = { x: Math.cos(Math.PI / 4) * 13, z: Math.sin(Math.PI / 4) * 13 }
const res = []
await p.evaluate(() => { window.__ev = []; const o = window.KB.onUnity; window.KB.onUnity = s => { o(s); if (s.includes('"t":"e"')) window.__ev.push(JSON.parse(s)) } })
for (let i = 0; i < 16; i++) {
  const a = i * Math.PI * 2 / 16
  const sx = M.x + Math.cos(a) * 7.6, sz = M.z + Math.sin(a) * 7.6
  const vx = -Math.cos(a) * 9, vz = -Math.sin(a) * 9
  await send(p, 'spin|1,0'); await send(p, 'tp|' + [sx, 6, sz, 0, -1, 0].map(v => v.toFixed(2)).join(','))
  await wait(900)
  await p.evaluate(() => { window.__ev.length = 0 })
  const s0 = (await state(p)).k[0]
  await send(p, 'tp|' + [s0.wx, s0.h + 0.02, s0.wz, vx, 0, vz].map(v => v.toFixed(2)).join(','))
  let minGap = 9, maxH = -9, crossed = false
  const t0 = Date.now()
  while (Date.now() - t0 < 2600) {
    const k = (await state(p)).k[0]
    if (k.gh > -100) minGap = Math.min(minGap, k.h - k.gh)
    maxH = Math.max(maxH, k.h - k.gh)
    const d = Math.hypot(k.wx - M.x, k.wz - M.z)
    const side = (k.wx - M.x) * Math.cos(a) + (k.wz - M.z) * Math.sin(a)
    if (side < -1.5) crossed = true
    await wait(40)
  }
  const ev = await p.evaluate(() => window.__ev.filter(e => e.i === 0).map(e => e.k + (e.k === 'boost' ? Math.round(e.p) : e.k === 'jump' ? ':' + e.p : '')))
  res.push({ dir: Math.round(a * 180 / Math.PI), boost: ev.find(e => e.startsWith('boost')) || '-', launch: ev.includes('jump:1'), maxAir: +maxH.toFixed(2), minGap: +minGap.toFixed(3), crossed })
}
console.table(res)
const ok = res.filter(r => r.boost !== '-' && r.launch && r.minGap > -0.05 && r.crossed).length
console.log('OK', ok, '/', res.length, 'errs', JSON.stringify(errs))
await b.close()
