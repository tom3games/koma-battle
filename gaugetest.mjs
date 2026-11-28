// 必殺ゲージの貯まり方: 自動操作×CPU2で、1本目（その技の必要量）が貯まるまでの秒数と、1試合の必殺回数。node gaugetest.mjs [試合数/ステージ]
import { open, wait } from './lib.mjs'
const N = +(process.argv[2] || 3)
const { b, p } = await open('http://localhost:5393/?lv=10', { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
for (const st of [0, 1, 2]) {
  const first = [], used = [], dur = []
  for (let i = 0; i < N; i++) {
    await p.evaluate(st => { Object.assign(KB.pick, { top: 3, core: 4, leg: 4, dir: 0, sp: 0, pas: 0 }); KB.result = null; KB.events.length = 0; KB.send('stage|' + st); KB.startMatch() }, st)
    await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 }); await wait(100)
    await p.evaluate(() => { KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|-1'); KB.send('timescale|3') })
    let f = null
    for (let t = 0; t < 600; t++) {
      const s = await p.evaluate(() => KB.state && KB.state.k && KB.state.k[0] ? { tm: KB.state.tm, g: KB.state.k[0].g, nd: KB.state.k[0].nd } : null)
      if (s && f === null && s.g >= s.nd) f = s.tm
      if (await p.evaluate(() => !!KB.result)) break; await wait(250)
    }
    const r = await p.evaluate(() => KB.result); const sp = await p.evaluate(() => KB.events.filter(e => e.k === 'special' && e.i === 0).length)
    first.push(f === null ? null : +f.toFixed(0)); used.push(r ? r.specials : sp); dur.push(r ? +r.time.toFixed(0) : null)
  }
  console.log(JSON.stringify({ stage: st, first, used, dur }))
}
await b.close()
