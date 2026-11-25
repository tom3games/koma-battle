import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open(process.argv[2] || 'http://localhost:5444/')
await wait(800); await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click())
await p.click('#btnFight'); await wait(4300)
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1'); await send(p, 'mlock|1'); for (let i = 0; i < 30; i++) await send(p, 'look|0,40')
await send(p, 'timescale|5')
const shotAt = [62.2, 63.4, 67.5, 74]; let n = 0
for (let i = 0; i < 200 && n < shotAt.length; i++) {
  await wait(150); await send(p, 'spin|1|0'); await send(p, 'spin|1|1')
  const s = await state(p)
  if (s.tm > shotAt[n] - 2) await send(p, 'timescale|1')
  if (s.tm >= shotAt[n]) { await p.screenshot({ path: `shots/v10c_${n}.png` }); console.log('shot', n, s.tm.toFixed(1)); n++; await send(p, 'timescale|5') }
}
console.log('errs', errs); await b.close()
