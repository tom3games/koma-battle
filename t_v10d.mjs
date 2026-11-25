import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open(process.argv[2] || 'http://localhost:5444/')
await wait(800); await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
await p.click('#btnFight'); await wait(4300)
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1'); await send(p, 'timescale|6')
let s
for (let i = 0; i < 400; i++) { await wait(100); await send(p, 'spin|1|0'); await send(p, 'spin|1|1'); await send(p, 'spin|1|2'); s = await state(p); if (s.tm > 66.5) break }
await send(p, 'timescale|1')
// 1番を半径 17（落ちた輪）、2番を半径 11（まだ残る輪）へ
await send(p, 'tp|17,6,0,0,0,0,1'); await send(p, 'tp|0,6,11,0,0,0,2')
// カメラを1番の方へ
await send(p, 'mlock|1'); for (let i = 0; i < 10; i++) await send(p, 'look|0,40')
await wait(300); await p.screenshot({ path: 'shots/v10d_0.png' })
await wait(1500)
s = await state(p); console.log('tm', s.tm.toFixed(1), JSON.stringify(s.k.map(k => ({ i: k.i, a: k.a, r: k.r && k.r.toFixed(1), h: k.h && k.h.toFixed(1), gh: k.gh && k.gh.toFixed(1) }))))
const ko = await p.evaluate(() => window.KB.events.filter(e => e.k === 'ko' || e.k === 'collapse').map(e => e.k + (e.why || '') + e.i))
console.log(ko.join(' '))
await p.screenshot({ path: 'shots/v10d_1.png' })
console.log('errs', errs); await b.close()
