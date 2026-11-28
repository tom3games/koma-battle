// 踏ん張り(F): 縁の近くで同じ当たりを受けた時の押し出される距離と回転の減り、長押しで回転が尽きるまでの秒数。node bracetest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const out = []
for (const [label, parts] of [['標準(3/4/4)', null], ['重量型(1/2/0)', '1,2,0'], ['速度型(2/1/3)', '2,1,3']]) {
  for (const brace of [false, true]) {
    const o = await begin(url, { sp: 0, stage: 0 })
    if (parts) await send(o.p, `setparts|0,${parts}`)
    await o.place(1, -10, 8, 0, 0, 1.0); await o.place(0, 20, 0, 0, 0, 5); await wait(2200)
    const k0 = await o.k(0)
    if (brace) { await send(o.p, 'brace|1'); await wait(500) }
    const k1 = await o.k(0); const x0 = k1.wx
    await o.monReset(); await o.clear()
    await o.place(1, x0 - 1.15, 0, 13, 0, k1.h + 0.1)
    await wait(3500)
    const m = await o.mon(), k2 = await o.k(0)
    out.push({ 型: label, 踏ん張り: brace ? 'あり' : 'なし', 押し出された距離: +m[0].dmax.toFixed(1), 当たる前の回転: +k1.sp.toFixed(3), '3.5秒後の回転': +k2.sp.toFixed(3), 生存: k2.a })
    await o.b.close()
  }
}
for (const r of out) console.log(JSON.stringify(r))
// 長押しで尽きるまで
for (const [label, parts] of [['標準', null], ['重量型', '1,2,0'], ['速度型', '2,1,3']]) {
  const o = await begin(url, { sp: 0, stage: 0, ts: 4 })
  if (parts) await send(o.p, `setparts|0,${parts}`)
  await o.place(1, -10, 8, 0, 0, 1.0); await o.place(0, 3, 3, 0, 0, 1.0); await wait(1500)
  await send(o.p, 'brace|1'); const t0 = await o.tm()
  let tEnd = null
  for (let i = 0; i < 400; i++) { await o.place(0, 3, 3, 0, 0, 1.0); await wait(120); const k = await o.k(0); if (!k.a) { tEnd = (await o.tm()) - t0; break } }
  console.log(JSON.stringify({ 型: label, 長押しで回転が尽きるまで秒: tEnd && +tEnd.toFixed(1) }))
  await o.b.close()
}
process.exit(0)
