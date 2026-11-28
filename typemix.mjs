// 型のバランス（自動操作の自分の強さを消す）: 3体とも自動（CPU 同士）で、速度・攻撃・重量の3型を座席を入れ替えながら戦わせ、型ごとの勝率を数える。
// 座席は 6 通りの並びを順に使う（各型が各座席を同じ回数だけ）。node typemix.mjs <stage 0|1|2> <回数> [url]
import { open, wait } from './lib.mjs'
const stage = +process.argv[2], N = +process.argv[3], url = process.argv[4] || 'http://localhost:5393/?lv=10'
const T = { S: [2, 1, 3], A: [4, 3, 4], H: [1, 2, 0] }
const perms = [['S', 'A', 'H'], ['S', 'H', 'A'], ['A', 'S', 'H'], ['A', 'H', 'S'], ['H', 'S', 'A'], ['H', 'A', 'S']]
const { b, p } = await open(url, { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
const win = { S: 0, A: 0, H: 0 }, draw = { n: 0 }; let secs = 0, done = 0; const why = {}
for (let i = 0; i < N; i++) {
  const perm = perms[i % 6]
  await p.evaluate(({ stage, t0 }) => { Object.assign(KB.pick, { top: t0[0], core: t0[1], leg: t0[2], dir: 0, sp: 0, pas: 0 }); KB.result = null; KB.events.length = 0; KB.send('stage|' + stage); KB.startMatch() }, { stage, t0: T[perm[0]] })
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 }); await wait(100)
  await p.evaluate(({ t1, t2 }) => { KB.send(`setparts|1,${t1.join(',')}`); KB.send(`setparts|2,${t2.join(',')}`); KB.send('setsp|0,1'); KB.send('setsp|0,2'); KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|0'); KB.send('timescale|3') }, { t1: T[perm[1]], t2: T[perm[2]] })
  try { await p.waitForFunction(() => window.KB.result, null, { timeout: 400000 }) } catch (e) { continue }
  const r = await p.evaluate(() => ({ t: KB.result.time, alive: KB.state.k.filter(k => k.a === 1).map(k => k.i) }))
  done++; secs += r.t
  if (r.alive.length === 1) win[perm[r.alive[0]]]++; else draw.n++
}
const pct = k => +(win[k] / Math.max(1, done) * 100).toFixed(0)
console.log(JSON.stringify({ stage, matches: done, speed: pct('S'), attack: pct('A'), heavy: pct('H'), noWinner: draw.n, avgSec: Math.round(secs / Math.max(1, done)) }))
await b.close()
