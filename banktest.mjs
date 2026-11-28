// 外周の傾斜（バンク）: 外周で止めた時に中へ戻る距離と時間、外周を回る様子。node banktest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const A = Math.PI / 4, c = Math.cos(A), s = Math.sin(A)
const f1 = x => +(+x).toFixed(1)
// ① 外周 r=20.5 に止めて置く → 中へ戻る
const o = await begin(url, { sp: 0 })
await o.place(1, -10, 8, 0, 0, 1.0)
const rs = []
for (const r0 of [19, 21]) {
  await o.place(0, c * r0, s * r0, 0, 0, 6.0); await wait(800)
  const t0 = await o.tm(); const row = { r0, samples: [] }
  let t15 = null
  for (let i = 0; i < 40; i++) {
    await wait(120); const k = await o.k(0); const t = (await o.tm()) - t0; const r = Math.hypot(k.wx, k.wz)
    if (t15 === null && r < r0 - 3) t15 = t
    if ([5, 12, 20, 30, 39].includes(i)) row.samples.push([f1(t), f1(r)])
  }
  row.secToMove3m = t15 === null ? null : f1(t15)
  rs.push(row)
}
console.log('止めて置く', JSON.stringify(rs))
// ② 外周を接線方向に 12m/s で回す（入力なし）→ 半径と速さの変化
await o.place(0, c * 20, s * 20, -s * 12, c * 12, 6.0)
const t0 = await o.tm(); const rows = []
for (let i = 0; i < 24; i++) { await wait(120); const k = await o.k(0); rows.push([f1((await o.tm()) - t0), f1(Math.hypot(k.wx, k.wz)), f1(Math.hypot(k.vx, k.vz))]) }
console.log('接線12m/sで回す(秒,半径,速さ)', JSON.stringify(rows.filter((_, i) => i % 3 === 0)))
const kk = await o.k(0); console.log('生存', kk.a)
process.exit(0)
