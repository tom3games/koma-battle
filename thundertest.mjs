// 雷鳴の刻印: 円の中の全員に当たる（撃った本人は除く）・予告0.55秒・人（反応0.25秒）と CPU が逃げられるか。node thundertest.mjs [url]
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/?lv=10'
const { b, p, errs } = await open(url, { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="3"]').click(); Object.assign(KB.pick, { pas: 0 }); KB.startMatch() })
await p.waitForFunction(() => KB.state && KB.state.ph === 'count'); await p.waitForFunction(() => KB.state && KB.state.ph === 'fight')
await send(p, 'cpupas|-1'); await send(p, 'cpuoff|1'); await wait(2500)
await p.evaluate(() => { window.__th = []; const o = KB.onUnity; KB.onUnity = s => { o(s); if (s.includes('"k":"thunder"')) window.__th.push(JSON.parse(s)); if (s.includes('"k":"warn"')) window.__th.push({ warn: Date.now() }) } })
const K = async () => (await state(p)).k
// 1. 3体を円に集めて撃つ（撃つ本人 = 自分 も円の中）
let res = []
for (let t = 0; t < 3; t++) {
  const k = await K(); const c = k[0]
  await send(p, 'cpuoff|1')
  for (const i of [0, 1, 2, 3]) await send(p, `tp|${c.wx + (i - 1.5) * 0.8},${c.gh + 0.5},${c.wz + 0.5},0,0,0,${i}`)
  await wait(500)
  const sp0 = (await K()).map(x => x.sp)
  await send(p, 'strike|1,0'); await wait(1500)
  const th = await p.evaluate(() => window.__th.filter(e => e.k === 'thunder').pop())
  const sp1 = (await K()).map(x => x.sp)
  res.push({ hit: th && th.hit, drop: sp0.map((v, i) => +((v - sp1[i]) * 160).toFixed(1)) })
}
console.log('全員が円の中:', JSON.stringify(res), '(drop は回転の減り。0番=撃った本人は減らない)')
// 2. 予告から落雷までの時間
const gap = await p.evaluate(() => { const w = window.__th.filter(e => e.warn).pop(); return w ? w.warn : null })
// 3. 逃げられるか: 標的=自分、撃つ側=CPU1。人の反応 0.25秒のあとに外へ移動（ダッシュあり/なし）
const trial = async (mode) => {
  let hits = 0, n = 10
  for (let t = 0; t < n; t++) {
    await send(p, 'cpuoff|1'); await send(p, 'move|0,0')
    const k = await K(); const c = k[0]
    await send(p, `tp|${c.wx},${c.gh + 0.5},${c.wz},0,0,0,0`); await send(p, `tp|${c.wx + 12},${c.gh + 0.5},${c.wz},0,0,0,1`); await wait(700)
    await p.evaluate(() => { window.__th.length = 0 })
    await send(p, 'strike|0,1')
    if (mode !== 'stay') { await wait(250 * 1); await send(p, 'move|1,0'); if (mode === 'dash') await send(p, 'dash') }
    await wait(1200); await send(p, 'move|0,0')
    const th = await p.evaluate(() => window.__th.filter(e => e.k === 'thunder').pop())
    if (th && th.hit > 0) hits++
  }
  return hits + '/' + n
}
console.log('標的=自分 動かない:', await trial('stay'), ' 反応0.25秒後に移動:', await trial('move'), ' 反応0.25秒後にダッシュ+移動:', await trial('dash'))
// 4. CPU が逃げるか（CPU 1 を標的、CPU は動かす）
let cpuHit = 0
for (let t = 0; t < 10; t++) {
  await send(p, 'cpuoff|1'); const k = await K(); const c = k[0]
  await send(p, `tp|${c.wx},${c.gh + 0.5},${c.wz},0,0,0,0`); await send(p, `tp|${c.wx + 6},${c.gh + 0.5},${c.wz},0,0,0,1`); await wait(600)
  await send(p, 'cpuoff|0'); await p.evaluate(() => { window.__th.length = 0 })
  await send(p, 'strike|1,0'); await wait(1300)
  const th = await p.evaluate(() => window.__th.filter(e => e.k === 'thunder').pop()); if (th && th.hit > 0) cpuHit++
}
console.log('CPU を標的（中心）: 当たった', cpuHit + '/10')
console.log('errs', JSON.stringify(errs))
await b.close()
