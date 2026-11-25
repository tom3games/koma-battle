// 最終局面の床の崩れと Shift の速さを、CPU を止めて確かめる
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5444/'
const { b, p, errs } = await open(url)
await wait(800); await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click())
await p.click('#btnFight'); await wait(4300)
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1')
const pos = async () => { const k = (await state(p)).k[0]; return [k.wx, k.wz, performance.now()] }
const speed = async () => { const a = await pos(); await wait(400); const c = await pos(); return Math.hypot(c[0] - a[0], c[1] - a[1]) / 0.4 }
await send(p, 'tp|0|0,0,0').catch(() => {})
await p.keyboard.down('KeyW'); await wait(1200); const s1 = await speed(); await p.keyboard.up('KeyW')
await wait(1500)
await p.keyboard.down('ShiftLeft'); await p.keyboard.down('KeyW'); await wait(1200); const s2 = await speed(); await p.keyboard.up('KeyW'); await p.keyboard.up('ShiftLeft')
console.log('speed W', s1.toFixed(1), 'Shift+W', s2.toFixed(1))
await send(p, 'mlock|1'); for (let i = 0; i < 12; i++) await send(p, 'look|0,30')
await send(p, 'timescale|4')
let n = 0
for (let i = 0; i < 80; i++) {
  await wait(500); await send(p, 'spin|1|0'); await send(p, 'spin|1|1')
  const s = await state(p)
  if (s.sd && n < 6) { await send(p, 'timescale|1'); await p.screenshot({ path: `shots/v10b_${n}.png` }); n++; await wait(1600) }
  if (n >= 6) break
}
console.log('errs', errs); await b.close()
