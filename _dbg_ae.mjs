import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 6 })
await o.place(1, -10, 8, 0, 0, 1.0); await o.place(0, 20.1, 0, 0, 0, 5.5); await wait(1300)
const a = await o.k(0); console.log('start', a.wx.toFixed(2), a.wz.toFixed(2), a.h.toFixed(2), a.a)
await send(o.p, 'gauge|100'); await send(o.p, 'spfire|0,40,0')
for (let i = 0; i < 16; i++) { await wait(300); const k = await o.k(0); console.log(i, (await o.tm()).toFixed(2), 'x', k.wx.toFixed(1), 'z', k.wz.toFixed(1), 'y', k.h.toFixed(1), 'a', k.a, 'v', Math.hypot(k.vx, k.vz).toFixed(1)) }
process.exit(0)
