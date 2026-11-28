// 当たりの内訳: 自動操作の自分×CPU2で、自分が関わる当たりとCPU同士の当たりの数。node hitmix.mjs [試合数] [url]
import { open, wait } from './lib.mjs'
const N = +(process.argv[2] || 6)
const { b, p } = await open(process.argv[3] || 'http://localhost:5393/?lv=10', { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
let me = 0, cc = 0, t = 0, firstHit = []
for (let i = 0; i < N; i++) {
  await p.evaluate(st => { Object.assign(KB.pick, { top: 3, core: 4, leg: 4, dir: 0, sp: 0, pas: 0 }); KB.result = null; KB.events.length = 0; KB.send('stage|' + st); KB.startMatch() }, i % 3)
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 }); await wait(100)
  await p.evaluate(() => { KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|-1'); KB.send('timescale|3') })
  await p.waitForFunction(() => window.KB.result, null, { timeout: 400000 }).catch(() => {})
  const r = await p.evaluate(() => ({ h: KB.events.filter(e => e.k === 'hit').map(e => [e.a, e.b]), t: KB.result && KB.result.time }))
  const m = r.h.filter(([a, b2]) => a === 0 || b2 === 0).length; me += m; cc += r.h.length - m; t += r.t || 0
}
console.log(JSON.stringify({ matches: N, playerHits: me, cpuCpuHits: cc, playerShare: +(me / Math.max(1, me + cc)).toFixed(2), avgSec: Math.round(t / N) }))
await b.close()
