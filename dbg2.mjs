import { open, wait, state, send } from './lib.mjs'
const { b, p } = await open('http://localhost:5392/')
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight'); await send(p, 'auto|1')
await p.evaluate(() => { window.__log = []; const o = window.KB.onUnity; window.KB.onUnity = s => { o(s); try { const m = JSON.parse(s); if (m.t === 's' && m.ph === 'fight') window.__log.push(m.tm.toFixed(2) + ' ' + m.k.map(k => `${k.i}:${k.a ? '' : 'X'}r${k.r.toFixed(1)} h${k.h.toFixed(1)}${k.air ? 'A' : ''}`).join(' ')); if (m.t === 'e' && m.k !== 'count') window.__log.push('  EV ' + s) } catch (e) {} } })
const t0 = Date.now()
while (Date.now() - t0 < 30000) { await wait(500); if (await p.evaluate(() => window.KB.result)) break }
const log = await p.evaluate(() => window.__log)
console.log(log.filter((l, i) => l.startsWith('  EV') || i % 3 === 0).slice(0, 140).join('\n'))
await b.close()
