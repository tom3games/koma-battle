import { open, wait, state, send } from './lib.mjs'
const { b, p } = await open('http://localhost:5392/?lv=10', { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click(); Object.assign(KB.pick, { pas: 2 }); KB.startMatch() })
await p.waitForFunction(() => KB.state && KB.state.ph === 'count'); await p.waitForFunction(() => KB.state && KB.state.ph === 'fight')
await send(p, 'cpuoff|1'); await wait(1500)
const s = (await state(p)).k[0]
await wait(1500)
await send(p, 'timescale|0.1'); await send(p, 'tp|' + [s.wx, s.gh + 9, s.wz, 0, 0, 0].join(','))
await wait(1500)
for (let i = 0; i < 2; i++) { await wait(60); await p.screenshot({ path: `shots/p/_wing_${i}.png` }) }
await b.close()
