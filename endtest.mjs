// 決着後に残ったコマが止まる（速さ）か。node endtest.mjs [url] [stage]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/', stage = process.argv[3] ?? 2
const { b, p, errs } = await open(url, { w: 640, h: 360 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(s => { document.querySelector('#vsPick button[data-n="1"]').click(); KB.send('stage|' + s) }, stage); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 40000 })
await send(p, 'cpuoff|1'); await send(p, 'tp|30,6,0,0,0,0,1')   // CPU を場外へ→自分が勝ち
await p.waitForFunction(() => window.KB.state.ph === 'end', null, { timeout: 20000 })
await send(p, 'tp|0,3,0,12,0,5,0')   // 勝者を走らせる
const sp = []
for (let i = 0; i < 9; i++) { await wait(300); const k = await p.evaluate(() => window.KB.state.k[0]); sp.push(+Math.hypot(k.vx, k.vz).toFixed(2)) }
console.log('決着後の勝者の速さ', JSON.stringify(sp), 'errs', errs)
console.log(sp[sp.length - 1] < 0.8 ? 'OK 止まる' : 'NG 止まらない'); await b.close()
