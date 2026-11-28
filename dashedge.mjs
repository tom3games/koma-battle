// ふつうのダッシュで自分の勢いだけで場外へ出ないか（縁の手前 2/4/7m から外向き・停止/走行）。node dashedge.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const EDGE = 24.1
const rows = []
for (const mode of (process.env.MODES || 'rest,run').split(',')) for (const d of (process.env.DS || '2,4,7').split(',').map(Number)) {
  const o = await begin(url, { sp: 0 })
  await o.place(1, -10, 8, 0, 0, 1.0); await o.place(0, mode === 'run' ? EDGE - d - 4 : EDGE - d, 0, 0, 0, 5.5); await wait(400); for (let i = 0; i < 100 && (await o.k(0)).air === 1; i++) await wait(150); await wait(300)
  await o.p.evaluate(() => { window.__maxR = 0; window.__minA = 1; setInterval(() => { const k = KB.state && KB.state.k.find(x => x.i === 0); if (k) { window.__maxR = Math.max(window.__maxR, Math.hypot(k.wx, k.wz)); window.__minA = Math.min(window.__minA, k.a) } }, 4) })
  if (mode === 'run') {
    await send(o.p, 'inmv|1,0')   // 外へ走らせ、縁の手前 d の線を越えた瞬間にダッシュ
    for (let i = 0; i < 400; i++) { const k = await o.k(0); if (Math.hypot(k.wx, k.wz) >= EDGE - d) break; await wait(30) }
    await send(o.p, 'inmv')
  }
  await send(o.p, 'dashat|0,40,0')
  await wait(4500)
  const mr = await o.p.evaluate(() => [window.__maxR, window.__minA])
  rows.push({ mode, startToEdge: d, alive: mr[1], maxR: +mr[0].toFixed(2), edgeGap: +(EDGE - mr[0]).toFixed(2), cling: (await o.evs('cling')).length })
  await o.b.close()
}
for (const r of rows) console.log(JSON.stringify(r))
console.log('場外になった回数', rows.filter(r => r.alive === 0).length); process.exit(0)
