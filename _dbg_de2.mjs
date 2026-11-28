import { begin, wait, send } from './sp2lib.mjs'
for (const mon of [false, true]) {
  const o = await begin('http://localhost:5393/', { sp: 0 })
  await o.place(1, -10, 8, 0, 0, 1.0); await o.place(0, 20.1, 0, 0, 0, 5.5); await wait(1300)
  if (mon) await o.p.evaluate(() => { window.__maxR = 0; setInterval(() => { const k = KB.state && KB.state.k.find(x => x.i === 0); if (k) window.__maxR = Math.max(window.__maxR, Math.hypot(k.wx, k.wz)) }, 4) })
  await send(o.p, 'dashat|0,40,0')
  let mx = 0
  for (let i = 0; i < 12; i++) { await wait(300); const k = await o.k(0); mx = Math.max(mx, Math.hypot(k.wx, k.wz)) }
  console.log('monitor', mon, 'maxR(sample)', mx.toFixed(2), 'inpage', mon ? await o.p.evaluate(() => window.__maxR) : '-', 'alive', (await o.k(0)).a)
  await o.b.close()
}
process.exit(0)
