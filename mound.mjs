// 残ったジャンプ台（45度・半径13m）で跳べるか: 山の外から速く登る。node mound.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const o = await begin(url, { sp: 0 })
await o.place(1, -10, 8, 0, 0, 1.0)
const c = 13 * Math.SQRT1_2
const res = []
for (const [label, ang] of [['外側から', 0], ['内側から', 180], ['横から', 90]]) {
  const a = ang * Math.PI / 180, ux = Math.cos(Math.PI / 4 + a), uz = Math.sin(Math.PI / 4 + a)   // 山の中心から離れる向き
  const sx = c + ux * 4.2, sz = c + uz * 4.2
  await o.p.evaluate(() => { window.__hmax = -99; window.KB.events.length = 0; clearInterval(window.__t); window.__t = setInterval(() => { const k = KB.state.k[0]; window.__hmax = Math.max(window.__hmax, k.h - k.gh) }, 5) })
  await o.place(0, sx, sz, -ux * 11, -uz * 11, 1.5)
  await wait(3500)
  const h = await o.p.evaluate(() => window.__hmax), jumps = (await o.evs('jump')).filter(e => e.p === 1 && e.i === 0).length
  res.push({ label, 地面からの最高の高さ: +h.toFixed(2), jumpEv: jumps })
}
console.log(JSON.stringify(res)); process.exit(0)
