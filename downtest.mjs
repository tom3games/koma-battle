// 回転が尽きた後の余韻: ko（spin）後の傾き・回り・止まるまで（コマ1を自分の目の前に置いて連続で撮る）
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const { b, p, errs } = await open(url, { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="2"]').click(); KB.send('stage|0') })
await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 30000 })
await send(p, 'cpuoff|1')
await send(p, 'tp|0,6,6,0,0,0,0'); await send(p, 'tp|0,6,10,0,0,0,1'); await send(p, 'tp|40,6,10,0,0,0,2')
await wait(1500)
await send(p, 'spin|0.0003,1')
const t0 = Date.now(); const log = []
for (let i = 0; i < 10; i++) { await p.screenshot({ path: `shots/down_${i}.png` }); const s = await p.evaluate(() => KB.state.k[1]); log.push([Date.now() - t0, s.a, s.sp]); await wait(120) }
console.log(JSON.stringify(log)); console.log('errs', errs)
await b.close()
