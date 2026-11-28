// 型（パーツ）× ステージごとの勝率。自動操作の自分（型）× CPU2体（ランダム）。node winrate4.mjs <stage 0|1> <top> <core> <leg> <回数> [url]
import { open, wait } from './lib.mjs'
const stage = +process.argv[2], top = +process.argv[3], core = +process.argv[4], leg = +process.argv[5], N = +process.argv[6], url = process.argv[7] || 'http://localhost:5393/?lv=10'
const { b, p, errs } = await open(url, { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
let win = 0, done = 0, secs = 0, why = {}
for (let i = 0; i < N; i++) {
  await p.evaluate(({ stage, top, core, leg, sp }) => { Object.assign(KB.pick, { top, core, leg, dir: 0, sp, pas: 0 }); KB.result = null; KB.events.length = 0; KB.send('stage|' + stage); KB.startMatch() }, { stage, top, core, leg, sp: +(process.env.SP || 0) })
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 })
  await wait(100)
  await p.evaluate(() => { KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|-1'); KB.send('timescale|3') })
  try { await p.waitForFunction(() => window.KB.result, null, { timeout: 400000 }) } catch (e) { continue }
  const r = await p.evaluate(() => KB.result)
  done++; if (r.win) win++; secs += r.time; why[r.why || 'win'] = (why[r.why || 'win'] || 0) + 1
}
console.log(JSON.stringify({ stage, type: [top, core, leg].join('/'), N: done, win, rate: +(win / done * 100).toFixed(0), avgSec: +(secs / done).toFixed(0), why, errs }))
await b.close()
