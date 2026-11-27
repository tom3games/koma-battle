// 必殺ごとの勝率。自動操作の自分（必殺 sp）× CPU2体（必殺はランダム・パッシブなし）の1対2を N 回。node winrate2.mjs <sp 0〜7> <回数> [url]
import { open, wait } from './lib.mjs'
const sp = +process.argv[2], N = +process.argv[3], url = process.argv[4] || 'http://localhost:5393/?lv=10'
const { b, p, errs } = await open(url, { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
let win = 0, done = 0, secs = 0, used = 0, why = {}
for (let i = 0; i < N; i++) {
  await p.evaluate(sp => { Object.assign(KB.pick, { top: 3, core: 0, leg: 4, dir: 0, sp, pas: 0 }); KB.result = null; KB.events.length = 0; KB.startMatch() }, sp)
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 })
  await wait(100)
  await p.evaluate(() => { KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|-1'); KB.send('timescale|3') })
  try { await p.waitForFunction(() => window.KB.result, null, { timeout: 400000 }) } catch (e) { continue }
  const r = await p.evaluate(() => KB.result)
  const u = await p.evaluate(() => KB.events.filter(e => e.k === 'special' && e.i === 0).length)
  done++; if (r.win) win++; secs += r.time; used += u; why[r.why || 'win'] = (why[r.why || 'win'] || 0) + 1
}
console.log(JSON.stringify({ sp, N: done, win, rate: +(win / done * 100).toFixed(0), avgSec: +(secs / done).toFixed(0), usedPerMatch: +(used / done).toFixed(1), why, errs }))
await b.close()
