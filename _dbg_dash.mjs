import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 0 })
await o.place(1, -14, 0, 0, 0, 1.0); await o.place(0, 10, -4, 0, 0, 1.0); await wait(1500)
const a = await o.k(0); const t0 = await o.tm()
await send(o.p, `dashat|0,${a.wx},${a.wz + 8}`)
for (let i = 0; i < 25; i++) { await wait(80); const k = await o.k(0); console.log(((await o.tm()) - t0).toFixed(2), 'd', Math.hypot(k.wx - a.wx, k.wz - a.wz).toFixed(2), 'v', Math.hypot(k.vx, k.vz).toFixed(1)) }
process.exit(0)
