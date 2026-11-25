// v8 の確認: マウスで視点（固定の代わりに mlock と look を直接送る）
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5444/'
const { b, p, errs } = await open(url)
await wait(800); await p.click('#btnPlay'); await wait(600); await p.click('#btnFight'); await wait(4200)
await send(p, 'mlock|1')
await p.screenshot({ path: 'shots/v8_0.png' })
for (let i = 0; i < 20; i++) { await send(p, 'look|25,0'); await wait(30) }
await wait(300); await p.screenshot({ path: 'shots/v8_yaw.png' })
for (let i = 0; i < 20; i++) { await send(p, 'look|0,-20'); await wait(30) }
await wait(300); await p.screenshot({ path: 'shots/v8_low.png' })
for (let i = 0; i < 30; i++) { await send(p, 'look|0,30'); await wait(30) }
await wait(300); await p.screenshot({ path: 'shots/v8_high.png' })
// 前へダッシュ: 正面へ進むか
const s0 = await state(p); await p.mouse.click(640, 360); await wait(500); const s1 = await state(p)
console.log('me before', JSON.stringify(s0.k[0]).slice(0, 120)); console.log('me after', JSON.stringify(s1.k[0]).slice(0, 120))
await p.keyboard.down('KeyW'); await wait(1200); await p.keyboard.up('KeyW'); await p.screenshot({ path: 'shots/v8_w.png' })
console.log('errs', errs)
await b.close()
