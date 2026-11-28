import { begin, wait, send } from './sp2lib.mjs'
const dev = process.argv[2] || 'pc', tag = process.argv[3] || 'now', size = dev === 'pc' ? { w: 1280, h: 720 } : { w: 390, h: 844, mobile: true }
const o = await begin('http://localhost:5393/', { sp: 0, stage: 0, ...size })
await o.place(1, -3, -3, 0, 0, 1.0)
const A = Math.PI / 4
for (let i = 0; i < 8; i++) { await o.place(0, 20 * Math.cos(A), 20 * Math.sin(A), 0, 0, 6); await wait(250) }
await send(o.p, 'look0'); const k = await o.k(0)
// 外側から中央を見る: カメラの向き(yaw) = 中央へ向かう向き
const want = Math.atan2(-Math.cos(A), -Math.sin(A)); let d = want - k.yaw; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI
await send(o.p, `tlook|${d / 0.0075},-20`)
for (let i = 0; i < 6; i++) { await o.place(0, 20 * Math.cos(A), 20 * Math.sin(A), 0, 0, 6); await wait(250) }
await o.shot(`${dev}_bankview_${tag}`)
// もう1枚: 外周に沿って横を向く
await send(o.p, `tlook|${(Math.PI / 2) / 0.0075},-30`)
for (let i = 0; i < 6; i++) { await o.place(0, 20 * Math.cos(A), 20 * Math.sin(A), 0, 0, 6); await wait(250) }
await o.shot(`${dev}_bankview_${tag}_side`)
process.exit(0)
