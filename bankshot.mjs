import { begin, wait, send } from './sp2lib.mjs'
const dev = process.argv[2] || 'pc', size = dev === 'pc' ? { w: 1280, h: 720 } : { w: 390, h: 844, mobile: true }
const o = await begin('http://localhost:5393/', { sp: 0, ...size })
const A = Math.PI / 4
await o.place(1, -3, -3, 0, 0, 1.0)
for (let i = 0; i < 8; i++) { await o.place(0, 17.5 * Math.cos(A), 17.5 * Math.sin(A), 0, 0, 5); await wait(250) }
// 中心の向き(180度)へカメラ…ではなく接線方向: 外周を横から見る
await send(o.p, 'look0')
const k = await o.k(0); const want = Math.atan2(-Math.sin(A), Math.cos(A)); let d = want - k.yaw; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI
await send(o.p, `tlook|${d / 0.0075},-60`)
for (let i = 0; i < 6; i++) { await o.place(0, 17.5 * Math.cos(A), 17.5 * Math.sin(A), 0, 0, 5); await wait(250) }
await o.shot(`${dev}_bank`); process.exit(0)
