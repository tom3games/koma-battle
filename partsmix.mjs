// 決着の内訳（回転切れ・場外・軸折れ）・試合時間・最初のパーツが壊れる時刻・パーツ破壊の数。node partsmix.mjs [試合数/ステージ]
import { open, wait } from './lib.mjs'
const N = +(process.argv[2] || 4)
const { b, p } = await open('http://localhost:5393/?lv=10', { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
for (const st of [0, 1, 2]) {
  const why = {}, dur = [], first = [], breaks = [0, 0, 0]
  for (let i = 0; i < N; i++) {
    await p.evaluate(st => { Object.assign(KB.pick, { top: 3, core: 4, leg: 4, dir: 0, sp: 0, pas: 0 }); KB.result = null; KB.events.length = 0; KB.send('stage|' + st); KB.startMatch() }, st)
    await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 }); await wait(100)
    await p.evaluate(() => { KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|-1'); KB.send('timescale|3') })
    let f = null
    for (let t = 0; t < 900; t++) { if (f === null) { const e = await p.evaluate(() => { const x = KB.events.find(e => e.k === 'pbreak'); return x ? KB.state.tm : null }); if (e !== null) f = e } if (await p.evaluate(() => !!KB.result)) break; await wait(250) }
    const r = await p.evaluate(() => ({ r: KB.result, br: KB.events.filter(e => e.k === 'pbreak').map(e => e.p), ko: KB.events.filter(e => e.k === 'ko').map(e => e.why) }))
    const key = r.r && r.r.win ? (r.ko.includes('shaft') ? 'win(軸折れ含む)' : 'win') : (r.r ? (r.r.why || '?') : '?')
    why[key] = (why[key] || 0) + 1; r.ko.forEach(w => { why['ko:' + w] = (why['ko:' + w] || 0) + 1 }); if (r.r) dur.push(Math.round(r.r.time)); if (f !== null) first.push(Math.round(f)); r.br.forEach(x => breaks[x]++)
  }
  console.log(JSON.stringify({ stage: st, dur, avg: Math.round(dur.reduce((a, c) => a + c, 0) / Math.max(1, dur.length)), firstBreak: first, breaks_blade_disk_shaft: breaks, ends: why }))
}
await b.close()
