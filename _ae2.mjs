import { begin, wait, send } from './sp2lib.mjs'
const EDGE = 24.1
for (const d of [4, 8, 12]) {
  const o = await begin('http://localhost:5393/', { sp: 6 })
  await o.place(1, -10, 8, 0, 0, 1.0); await o.place(0, EDGE - d, 0, 0, 0, 5.5); await wait(1300)
  await o.p.evaluate(() => { window.__maxR = 0; window.__minA = 1; setInterval(() => { const k = KB.state && KB.state.k.find(x => x.i === 0); if (k) { window.__maxR = Math.max(window.__maxR, Math.hypot(k.wx, k.wz)); window.__minA = Math.min(window.__minA, k.a) } }, 4) })
  await send(o.p, 'gauge|100'); await send(o.p, 'spfire|0,40,0')
  let t = 0; let k
  for (let i = 0; i < 40; i++) { await wait(250); k = await o.k(0); t = await o.tm(); if (i > 6 && Math.hypot(k.vx, k.vz) < 1.5 && (await o.evs('arrowgo')).length) break }
  const mr = await o.p.evaluate(() => [window.__maxR, window.__minA]); console.log('d', d, 'alive(最小)', mr[1], '最も外へ出た r', mr[0].toFixed(2), '縁まで', (EDGE - mr[0]).toFixed(2), 'y', k.h.toFixed(1), 'cling', (await o.evs('cling')).length, 'go', (await o.evs('arrowgo')).length)
  await o.b.close()
}
process.exit(0)
