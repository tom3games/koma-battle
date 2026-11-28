// 画面の粒・輪の量の検査: 1秒あたりの生成数と、画面上に残っている最大数。node fxtest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const names = ['spark', 'dust', 'fleck', 'wave', 'fire', 'pulse', 'liveSpark', 'liveDust', 'liveFire', 'liveWave', 'chains', 'rings']
async function watch(o, ms, label, each = null) {
  const s0 = await o.p.evaluate(() => ({ fx: window.KB.state.k[0].fx, tm: window.KB.state.tm }))
  let peak = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]; const big = {}
  const t0 = Date.now()
  while (Date.now() - t0 < ms) {
    if (each) await each()
    await wait(120)
    const s = await o.p.evaluate(() => window.KB.state.k[0].fx); if (s) peak = peak.map((v, i) => Math.max(v, s[i]))
    for (const b of (await o.p.evaluate(() => window.KB.state.big)) || []) { const [n, v] = b.split(':'); big[n] = Math.max(big[n] || 0, +v) }
  }
  const s1 = await o.p.evaluate(() => ({ fx: window.KB.state.k[0].fx, tm: window.KB.state.tm }))
  const dt = Math.max(0.1, s1.tm - s0.tm)
  if (!s1.fx || !s0.fx) { console.log(label, '(fx なし)', JSON.stringify(big)); return }
  const rate = [0, 1, 2, 3, 4, 5].map(i => Math.round((s1.fx[i] - s0.fx[i]) / dt))
  console.log(label.padEnd(26), `秒あたり 火花${rate[0]} ほこり${rate[1]} 粒${rate[2]} 輪${rate[3]} 炎${rate[4]} 光${rate[5]}`.padEnd(48), `残り最大 火花${peak[6]} ほこり${peak[7]} 炎${peak[8]} 輪${peak[9]} 鎖${peak[10]} 氷輪${peak[11]}`, `(${dt.toFixed(1)}秒)`, '大きい描画物', JSON.stringify(big))
}
let o = await begin(url, { sp: 0 })
await o.place(0, -10, 0); await o.place(1, 10, 0); await wait(1500)
await watch(o, 3000, '静止')
await o.b.close()
// 壁に押し付け続ける
o = await begin(url, { sp: 0 })
const A = Math.PI / 6
await o.place(0, 3.2 * Math.cos(A), 3.2 * Math.sin(A)); await o.place(1, -15, 8); await wait(1200)
await send(o.p, `inmv|${Math.cos(A)},${Math.sin(A)}`)
await watch(o, 5000, '弧の台に押し付け')
await send(o.p, 'inmv')
const B = Math.PI / 4
await o.place(0, 21.5 * Math.cos(B), 21.5 * Math.sin(B), 0, 0, 5.5); await wait(800)
await send(o.p, `inmv|${Math.cos(B)},${Math.sin(B)}`)
await watch(o, 5000, '縁の柵に押し付け')
await o.b.close()
// 各技（撃って終わった後の3秒まで）
for (const [sp, label] of [[4, 'アンカー'], [5, 'スパイラル(回転0.15)'], [6, 'アロー'], [7, 'チェイン'], [0, 'メテオ'], [1, '竜巻']]) {
  o = await begin(url, { sp })
  await o.place(0, -10, 0); await o.place(1, -5, 0); await wait(1500)
  if (sp === 5) await send(o.p, 'spin|0.15,0')
  await send(o.p, 'gauge|100')
  await send(o.p, 'special')
  await watch(o, sp === 4 ? 8000 : 7000, label, async () => { if (sp === 5) await send(o.p, 'spin|0.15,0'); await o.place(1, sp === 6 ? -5 : 10, 0, 0, 0, 1.0).catch(() => {}) })
  await o.b.close()
}
// 回転が減った終盤（30%未満・逆鱗つき）
o = await begin(url, { sp: 0, pas: 2 })   // 逆鱗
await o.place(0, -10, 0); await o.place(1, 10, 0); await wait(1500)
await watch(o, 4000, '回転0.2・逆鱗なし→', async () => { await send(o.p, 'spin|0.2,0') })
await o.b.close()
// 3CPU の試合を通し・2試合続けて
o = await begin(url, { sp: 7, n: 3, cpuoff: false })
await send(o.p, 'auto|1'); await send(o.p, 'timescale|2')
for (let r = 0; r < 2; r++) {
  await watch(o, 25000, `3CPU 試合${r + 1} 前半`)
  await o.p.waitForFunction(() => window.KB.result, null, { timeout: 300000 }).catch(() => {})
  await watch(o, 1500, `3CPU 試合${r + 1} 決着後`)
  if (r === 0) { await o.p.evaluate(() => { KB.result = null; KB.startMatch() }); await o.p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 30000 }); await send(o.p, 'auto|1'); await send(o.p, 'timescale|2') }
}
await watch(o, 1500, '2試合目の後')
process.exit(0)
