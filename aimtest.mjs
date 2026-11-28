// C キー（aimc）: 相手の方へ向く角度の差と、押すたびに切り替わるか。node aimtest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const o = await begin(url, { sp: 0, n: 2 })
const ang = async (i) => { const me = await o.k(0), t = await o.k(i); let d = (Math.atan2(t.wx - me.wx, t.wz - me.wz) - me.yaw) * 180 / Math.PI; while (d > 180) d -= 360; while (d < -180) d += 360; return +Math.abs(d).toFixed(1) }
await o.place(0, 0, -8); await o.place(1, 0, -14, 0, 0, 1.0); await o.place(2, 7, -8, 0, 0, 1.0)   // 1=後ろ(約6m)・2=横(7m)
await wait(1500)
const pin = async () => { await o.place(0, 0, -8, 0, 0, 1.0); await o.place(1, 0, -14, 0, 0, 1.0); await o.place(2, 7, -8, 0, 0, 1.0) }
await pin()
const before = { e1: await ang(1), e2: await ang(2) }
await send(o.p, 'aimc'); let t0 = await o.tm()
while ((await o.tm()) - t0 < 0.4) { await wait(30); await pin() }
const after1 = { e1: await ang(1), e2: await ang(2) }
console.log('C 1回目', JSON.stringify({ before, after: after1 }))
ok(after1.e1 < 5, `最も近い相手（後ろ）へ 0.4秒後に5度以内 (${before.e1} → ${after1.e1})`)
await send(o.p, 'aimc'); t0 = await o.tm()
while ((await o.tm()) - t0 < 0.4) { await wait(30); await pin() }
const after2 = { e1: await ang(1), e2: await ang(2) }
console.log('C 2回目', JSON.stringify(after2))
ok(after2.e2 < 5, `2回押しで次の相手（横）へ切り替わる (${after2.e2})`)
// 途中経過（瞬間移動しない）
await send(o.p, 'aimc'); const ys = []
for (let i = 0; i < 8; i++) { await wait(60); ys.push((await ang(1))) ; await pin() }
console.log('1回押した後の角度の推移', JSON.stringify(ys))
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
