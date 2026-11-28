// チェインの投げ: 相手の重さ・使い手の回転で飛ぶ速さ/距離が変わるか。node chaintest2.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const rows = []
for (const [spinLabel, spin] of [['回転多い(100%)', 1.0], ['回転少ない(30%)', 0.3]]) for (const [tLabel, parts] of [['軽い相手', '2,1,3'], ['重い相手', '1,2,0']]) {
  const o = await begin(url, { sp: 7, stage: 0 })
  await send(o.p, `setparts|1,${parts}`)
  await o.place(0, -10, 0); await o.place(1, -5.5, 0); await wait(1800)
  await send(o.p, `spin|${spin},0`); await send(o.p, 'gauge|100'); await o.clear()
  await send(o.p, 'special')
  let thrown = null
  for (let i = 0; i < 160 && !thrown; i++) {
    await wait(70)
    const k0 = await o.k(0), k1 = await o.k(1)
    const rx = k0.wx - k1.wx, rz = k0.wz - k1.wz, rl = Math.hypot(rx, rz) || 1
    await send(o.p, `inmv|${-rz / rl - rx / rl * (rl - 5.8) * 0.6},${rx / rl - rz / rl * (rl - 5.8) * 0.6}`)
    await send(o.p, `spin|${spin},0`)
    thrown = (await o.evs('chainthrow'))[0]
  }
  await send(o.p, 'inmv')
  await o.monReset(); await wait(3500)
  const m = await o.mon(); const k1 = await o.k(1)
  rows.push({ 使い手: spinLabel, 相手: tLabel, 投げた速さ: thrown && +thrown.p.toFixed(1), 飛んだ距離: +m[1].dmax.toFixed(1), 相手生存: k1.a })
  await o.b.close()
}
for (const r of rows) console.log(JSON.stringify(r)); process.exit(0)
