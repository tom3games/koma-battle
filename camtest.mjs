// 外周でカメラを止めた時に中心へ寄る検査。node camtest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const f2 = x => +(+x).toFixed(2)
const centerYaw = k => Math.atan2(-k.wx, -k.wz)
const diffDeg = (yaw, k) => { let d = (centerYaw(k) - yaw) * 180 / Math.PI; while (d > 180) d -= 360; while (d < -180) d += 360; return +d.toFixed(1) }
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
// 1) 外周(r=17>15.3)で止めた後の yaw と中心方向の差
let o = await begin(url, { sp: 0 })
await send(o.p, 'mlock|1')
await o.place(1, -2, -2); await o.place(0, 0, 17.5, 0, 0, 3.5); await wait(1500)
await send(o.p, 'tlook|180,0')   // 中心方向から約77度ずらして、そこで止める（camIdle 0 から）
const k0 = await o.k(0); const t0 = await o.tm()
const rows = []
for (let s of [0.3, 1.6, 2.6, 3.6]) {
  while ((await o.tm()) - t0 < s) { await wait(40); await o.place(0, 0, 17.5, 0, 0, 3.5) }
  const k = await o.k(0); rows.push({ afterStop: s, yawDeg: +(k.yaw * 180 / Math.PI).toFixed(1), diffToCenterDeg: diffDeg(k.yaw, k) })
}
console.log('止めた後', JSON.stringify(rows))
ok(Math.abs(rows[0].diffToCenterDeg) > Math.abs(rows[1].diffToCenterDeg) - 1 && Math.abs(rows[1].diffToCenterDeg) > Math.abs(rows[3].diffToCenterDeg), `ゆっくり中心へ寄る (${rows.map(r => Math.abs(r.diffToCenterDeg))})`)
ok(Math.abs(rows[3].diffToCenterDeg) < Math.abs(rows[0].diffToCenterDeg) * 0.55, '約3秒で差が半分以下')
// 2) 動かしたら即止まる: 寄せの最中に look を入れる
const ya = (await o.k(0)).yaw
await send(o.p, 'look|2,0'); const tt = await o.tm()
while ((await o.tm()) - tt < 0.45) await wait(30)
const yb = (await o.k(0)).yaw
console.log('look 後 0.45秒の yaw 変化(度)', f2((yb - ya) * 180 / Math.PI), '(look 自体の分 0.37度)')
ok(Math.abs((yb - ya) * 180 / Math.PI) < 1.2, 'マウスを動かした直後は寄せが止まる')
await o.b.close()
// 3) 内側(r=8)では寄せない
o = await begin(url, { sp: 0 })
await o.place(1, -2, -2); await o.place(0, 0, 8, 0, 0, 1.5); await wait(1200)
await send(o.p, 'tlook|180,0'); const y0 = (await o.k(0)).yaw; const ti = await o.tm()
while ((await o.tm()) - ti < 3) { await wait(60); await o.place(0, 0, 8, 0, 0, 1.5) }
const y1 = (await o.k(0)).yaw
console.log('内側 r=8 の3秒後の yaw 変化(度)', f2((y1 - y0) * 180 / Math.PI))
ok(Math.abs((y1 - y0) * 180 / Math.PI) < 2, '内側では寄せない')
await o.b.close()
// 4) 外周で前へ押しっぱなし: 寄せあり/なしで場外に落ちる数
const res = {}
for (const [label, half] of [['寄せなし', 0], ['寄せあり', 1]]) {
  let outs = 0, rs = []
  for (let i = 0; i < 4; i++) {
    o = await begin(url, { sp: 0 })
    await send(o.p, `recenter|${half}`)
    await o.place(1, -2, -2); await o.place(0, 0, 18.5, 0, 0, 5.5); await wait(1000)
    await send(o.p, 'tlook|' + (i % 2 ? 250 : -250) + ',0')   // カメラは中心から約±107度ずれた向き
    await send(o.p, 'move|0,1')   // 前（カメラの向き）へ押しっぱなし
    const ts = await o.tm()
    while ((await o.tm()) - ts < 4) { await wait(60); if ((await o.k(0)).a === 0) break }
    const k = await o.k(0); if (!k.a) outs++; rs.push(f2(Math.hypot(k.wx, k.wz)))
    await o.b.close()
  }
  res[label] = { outs, finalR: rs }
}
console.log('押しっぱなし', JSON.stringify(res))
ok(res['寄せあり'].outs <= res['寄せなし'].outs, `落ちる数が増えない (${res['寄せあり'].outs} <= ${res['寄せなし'].outs})`)
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
