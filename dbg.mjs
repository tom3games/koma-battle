import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open('http://localhost:5392/')
await p.click('#btnPlay'); await wait(300)
await p.click('#btnFight'); await send(p, 'auto|1')
await send(p, 'timescale|2')
const t0 = Date.now()
while (Date.now() - t0 < 60000) { await wait(500); if (await p.evaluate(() => window.KB.result)) break }
const ev = await p.evaluate(() => window.KB.events.filter(e => ['ko','hit','boom','special'].includes(e.k)))
for (const e of ev) console.log(JSON.stringify(e))
await b.close()
