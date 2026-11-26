// パッシブごとの勝率。自動操作の自分（パッシブあり）× CPU2体（パッシブなし）の1対2対2を N 回。node winrate.mjs <パッシブ -1〜7> <回数> [url]
import { open, wait } from './lib.mjs'
const pas = +process.argv[2], N = +process.argv[3], url = process.argv[4] || 'http://localhost:5392/?lv=10'
const { b, p, errs } = await open(url, { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
let win = 0, done = 0, secs = 0
for (let i = 0; i < N; i++) {
  await p.evaluate(pas => { Object.assign(KB.pick, { top: 0, core: 0, leg: 0, dir: 0, sp: 0, pas: pas + 1 }); KB.result = null; KB.startMatch() }, pas)
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 })
  await wait(100)
  await p.evaluate(() => { KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('timescale|3') })
  try { await p.waitForFunction(() => window.KB.result, null, { timeout: 400000 }) } catch (e) { continue }
  const r = await p.evaluate(() => KB.result)
  done++; if (r.win) win++; secs += r.time
}
console.log(JSON.stringify({ pas, N: done, win, rate: +(win / done * 100).toFixed(0), avgSec: +(secs / done).toFixed(0), errs }))
await b.close()
