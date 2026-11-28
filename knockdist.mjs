// 吹き飛ばしの実測: 当てられた側が止まるまでに動いた距離(m)と初速(m/s)。node knockdist.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const A = Math.PI / 4, tx = -Math.sin(A), tz = Math.cos(A)   // 接線方向（坂の影響が小さい）
const vx0 = 11 * Math.cos(A), vz0 = 11 * Math.sin(A)
const parts = { 同じ重さ: ['3,4,4', '3,4,4'], '重い→軽い': ['1,2,0', '2,1,3'], '軽い→重い': ['2,1,3', '1,2,0'] }
const rows = []
for (const [pl, [pa, pv]] of Object.entries(parts)) for (const sc of ['歩き', 'ダッシュ', '空中から', 'かち上げ']) {
  const o = await begin(url, { sp: 0, stage: 0 })
  await send(o.p, `setparts|0,${pa}`); await send(o.p, `setparts|1,${pv}`)
  await o.place(0, vx0 - tx * 1.25, vz0 - tz * 1.25, 0, 0, 1.0); await o.place(1, vx0, vz0, 0, 0, 1.0); await wait(1800)
  await o.monReset()
  const g = (await o.k(1)).gh
  if (sc === '歩き') await o.place(0, vx0 - tx * 1.25, vz0 - tz * 1.25, tx * 9, tz * 9, g + 0.1)
  else if (sc === 'ダッシュ') await o.place(0, vx0 - tx * 1.25, vz0 - tz * 1.25, tx * 19, tz * 19, g + 0.1)
  else if (sc === '空中から') await o.place(0, vx0 + tx * 0.2, vz0 + tz * 0.2, 0, 0, g + 3.2)
  else { await send(o.p, `leanw|${tx.toFixed(3)},${tz.toFixed(3)}`); await o.place(0, vx0 - tx * 1.25, vz0 - tz * 1.25, tx * 11, tz * 11, g + 0.1) }
  await wait(4200)
  const m = await o.mon()
  rows.push({ 組: pl, 当て方: sc, 初速: +m[1].spmax.toFixed(1), 動いた距離: +m[1].dmax.toFixed(1) })
  await o.b.close()
}
for (const r of rows) console.log(JSON.stringify(r)); process.exit(0)
