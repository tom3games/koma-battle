// 自動で入力を流して CPU 戦が最後まで決着するかを見る。node match.mjs [url] [回数] [相手の数]
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const runs = +(process.argv[3] || 1), ncpu = +(process.argv[4] || 3)
const { b, p, errs, loadMs } = await open(url)
console.log('loadMs', loadMs)
await p.click('#btnPlay'); await wait(500)
for (let r = 0; r < runs; r++) {
  await p.evaluate(n => { document.querySelector(`#vsPick button[data-n="${n}"]`).click() }, ncpu)
  if (r === 0) await p.click('#btnFight'); else await p.click('#btnAgain')
  await send(p, 'auto|1'); await send(p, 'timescale|3'); await send(p, 'debugstate|1')
  await p.evaluate(() => { window.KB.events.length = 0; window.KB.result = null })
  const t0 = Date.now()
  let res = null, maxT = 0
  while (Date.now() - t0 < 240000) {
    await wait(1000)
    res = await p.evaluate(() => window.KB.result)
    const s = await state(p); if (s) maxT = s.tm
    if (res) break
  }
  const ev = await p.evaluate(() => { const c = {}; for (const e of window.KB.events) { c[e.k] = (c[e.k] || 0) + 1; if (e.k === 'ko') c['ko_' + e.i + '_' + e.why] = Math.round(window.KB.state.tm); if (e.k === 'special') c['sp' + e.p + (e.i === 0 ? 'me' : 'cpu')] = (c['sp' + e.p + (e.i === 0 ? 'me' : 'cpu')] || 0) + 1 } return c })
  const fps = await p.evaluate(() => new Promise(r => { let n = 0; const t = performance.now(); const f = () => { n++; if (performance.now() - t < 1000) requestAnimationFrame(f); else r(n) }; requestAnimationFrame(f) }))
  const L = (await state(p)).k.map(k => (k.L || []).map(Math.round).join('/'))
  console.log(JSON.stringify({ L, run: r, ended: !!res, res, gameSec: maxT, wallSec: Math.round((Date.now() - t0) / 1000), ev, fps }))
  console.log('  KO', JSON.stringify(await p.evaluate(() => window.KB.events.filter(e => e.k === 'ko').map(e => [e.i, e.why, e.r, e.ang, e.vy, e.vh, e.lastHit]))))
  if (r === 0) await p.screenshot({ path: 'shots/09_result_auto.png' })
}
console.log('errs', errs)
await b.close()
