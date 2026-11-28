import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 0 })
await o.place(1, -2, -2); await o.place(0, 0, 17, 0, 0, 3.5); await wait(1500)
await send(o.p, 'mlock|1'); await send(o.p, 'tlook|180,0')
for (let i = 0; i < 8; i++) { await wait(400); const k = await o.k(0); console.log(i, 'a', k.a, 'r', k.r.toFixed(1), 'yaw', (k.yaw * 180 / Math.PI).toFixed(1), 'v', Math.hypot(k.vx, k.vz).toFixed(1)) }
process.exit(0)
