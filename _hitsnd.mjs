import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 0 })
await o.place(0, -10, 0, 0, 0, 1.0); await o.place(1, -16, 0, 0, 0, 1.0); await wait(1500)
await o.p.evaluate(() => { Snd.init && Snd.init(); window.Snd.played = {} })
await o.place(1, -11.05, 0, 13, 0, 0.95); await wait(2500)
console.log(JSON.stringify(await o.p.evaluate(() => ({ played: Snd.played, hitEv: KB.events.filter(e => e.k === 'hit').length, errs: window.__kbErrs }))))
process.exit(0)
