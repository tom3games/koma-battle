// v10 の確認: Shift の速さ・最終局面（床が崩れる）・当たりの演出
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5444/'
const { b, p, errs } = await open(url)
await wait(800); await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="3"]').click())
await p.click('#btnFight'); await wait(4300)
await send(p, 'debugstate|1')
const sp = async () => { const k = (await state(p)).k[0]; return Math.hypot(k.wx || 0, k.wz || 0) }
await p.keyboard.down('KeyW'); await wait(1500); const s1 = await sp(); await p.keyboard.down('ShiftLeft'); await wait(1500); const s2 = await sp(); await p.keyboard.up('ShiftLeft'); await p.keyboard.up('KeyW')
console.log('speed W', s1.toFixed(1), 'W+Shift', s2.toFixed(1))
await p.screenshot({ path: 'shots/v10_fight.png' })
await send(p, 'auto|1'); await send(p, 'timescale|2')
let shots = 0, t0 = Date.now(), sdAt = null
while (Date.now() - t0 < 150000) {
  await wait(700)
  const s = await state(p); const res = await p.evaluate(() => window.KB.result)
  if (s && s.sd && sdAt === null) { sdAt = s.tm; console.log('sudden at', s.tm.toFixed(1)) }
  if (sdAt !== null && shots < 5) { await p.screenshot({ path: `shots/v10_col${shots}.png` }); shots++; await wait(1200) }
  if (res) { console.log('end', JSON.stringify(res)); break }
}
const ev = await p.evaluate(() => { const c = {}; for (const e of window.KB.events) c[e.k] = (c[e.k] || 0) + 1; return c })
console.log('ev', JSON.stringify(ev)); console.log('errs', errs)
await b.close()
