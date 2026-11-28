// 氷の矢で自分が場外へ出ないか（縁の手前 4/8/12m から外向き・中央向き・穴向き）。node arrowedge.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const EDGE = 24.1
for (const d of [4, 8, 12]) {
  const rows = []
  for (let rep = 0; rep < 1; rep++) {
    const o = await begin(url, { sp: 6 })
    await o.place(1, -10, 8, 0, 0, 1.0)
    await o.place(0, EDGE - d, 0, 0, 0, 5.5); await wait(1300)   // 角度0度は縁の切れ目（落ちられる所）
    await send(o.p, 'gauge|100'); await o.clear()
    await send(o.p, 'spfire|0,40,0')
    await o.until(() => window.KB.events.some(e => e.k === 'arrowgo'), 20000)
    let minGap = 99, alive = 1, cling = 0
    const t0 = await o.tm()
    while ((await o.tm()) - t0 < 2.2) { await wait(50); const k = await o.k(0); alive = k.a; minGap = Math.min(minGap, EDGE - Math.hypot(k.wx, k.wz)) }
    const k = await o.k(0)
    rows.push({ startToEdge: d, alive, stoppedAtR: +Math.hypot(k.wx, k.wz).toFixed(2), edgeGap: +(EDGE - Math.hypot(k.wx, k.wz)).toFixed(2), why: k.why, clingEv: (await o.evs('cling')).length })
    await o.b.close()
  }
  console.log('外向き', JSON.stringify(rows))
  ok(rows.every(r => r.alive === 1), `縁の手前${d}m から外向き: 自分は場外に出ない`)
}
// 穴（崩落後）向き: 外周から中央へ
let o = await begin(url, { sp: 6, ts: 3 })
await o.place(1, 12, 12, 0, 0, 1.0); await o.place(0, -12, 0, 0, 0, 1.5); await wait(1000)
await send(o.p, 'pitstart')
for (let i = 0; i < 400 && !(await o.evs('pitfall')).length; i++) { await o.place(0, -12, 0, 0, 0, 1.5); await o.place(1, 12, 12, 0, 0, 1.0); await wait(150) }
await send(o.p, 'timescale|1'); await o.place(0, -12, 0, 0, 0, 1.5); await wait(900); await send(o.p, 'gauge|100'); await o.clear()
await send(o.p, 'spfire|0,20,0')
await o.until(() => window.KB.events.some(e => e.k === 'arrowgo'), 20000); await wait(1800)
const k = await o.k(0)
console.log('穴向き', JSON.stringify({ alive: k.a, x: +k.wx.toFixed(2), r: +Math.hypot(k.wx, k.wz).toFixed(2), why: k.why }))
ok(k.a === 1, `穴（半径3.9）向きでも穴に落ちない (止まった r=${Math.hypot(k.wx, k.wz).toFixed(2)})`)
await o.b.close()
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
