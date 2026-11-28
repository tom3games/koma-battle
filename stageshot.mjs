import { begin, wait, send } from './sp2lib.mjs'
const dev = process.argv[2] || 'pc', size = dev === 'pc' ? { w: 1280, h: 720 } : { w: 390, h: 844, mobile: true }
const o = await begin('http://localhost:5393/', { sp: 0, stage: 1, n: 1, ...size })
await o.shot(`${dev}_hill_0start`)
await o.place(1, -4, -3, 0, 0, 7)
const A = Math.PI / 4
for (let i = 0; i < 8; i++) { await o.place(0, 8.5 * Math.cos(A), 8.5 * Math.sin(A), 0, 0, 5); await wait(250) }
await send(o.p, 'look0'); const k = await o.k(0)
const want = Math.atan2(-Math.cos(A), -Math.sin(A)) + 0.0; let d = (Math.atan2(Math.sin(A + 0.9), Math.cos(A + 0.9)) * 0) + (want - k.yaw); while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI
await send(o.p, `tlook|${d / 0.0075},-50`)
for (let i = 0; i < 6; i++) { await o.place(0, 8.5 * Math.cos(A), 8.5 * Math.sin(A), 0, 0, 5); await wait(250) }
await o.shot(`${dev}_hill_1slope`)
process.exit(0)
