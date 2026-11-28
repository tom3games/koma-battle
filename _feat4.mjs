import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open('http://localhost:5392/', { w: 640, h: 360 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'auto|1'); await send(p, 'timescale|2'); await wait(2500)
await send(p, 'fightt|12')
for (let i = 0; i < 25; i++) { await send(p, 'spin|1,0'); await send(p, 'spin|1,1'); await wait(500) }
console.log(JSON.stringify(await p.evaluate(() => { const c = {}; for (const e of window.KB.events) c[e.k + (e.i !== undefined ? '_' + e.i : '')] = (c[e.k + (e.i !== undefined ? '_' + e.i : '')] || 0) + 1; return c })))
console.log(errs); await b.close()
