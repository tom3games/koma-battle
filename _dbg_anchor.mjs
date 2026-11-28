import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 4 })
await o.place(0, -10, 0); await o.place(1, -4, 0); await wait(1500)
await send(o.p, 'gauge|100'); await send(o.p, 'special'); await wait(500)
await o.place(1, -4.5, 0, -14, 0, 0.3)
for (let i = 0; i < 20; i++) { await wait(250); const k = await o.k(1), m = await o.k(0); console.log(i, "fps", await o.p.evaluate(()=>KB.fps), "tm", await o.tm(), k.wx.toFixed(2), k.vx.toFixed(1), k.vz.toFixed(1), '| me', m.wx.toFixed(2), m.vx.toFixed(1)) }
process.exit(0)
