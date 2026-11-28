import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 0, ts: 3 })
await o.place(0, -5.5, 0); await o.place(1, 12, 6); await wait(1500)
await send(o.p, 'pitstart'); await o.until(() => window.KB.events.some(e => e.k === 'pitfall'), 60000); await wait(300)
await send(o.p, 'timescale|1')
await o.place(0, -6.5, 0); await wait(1500); await send(o.p, 'gauge|100'); await o.clear()
await send(o.p, 'spfire|0,12,0')
for (let i = 0; i < 30; i++) { await wait(200); const m = await o.k(0); console.log(i, 'x', m.wx.toFixed(2), 'z', m.wz.toFixed(2), 'y', m.h.toFixed(2), 'a', m.a, 'vx', m.vx.toFixed(1), m.why, (await o.evs('boom')).length) }
process.exit(0)
