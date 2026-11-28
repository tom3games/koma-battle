// チェインの復帰: 縁の外へ投げ出された状態から、近くに相手あり/なし・重い/軽いで戻れるか。node recovertest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const rows = []
for (const [label, parts, near] of [['相手なし(遠い)', '2,1,3', false], ['軽い相手が近く', '2,1,3', true], ['重い相手が近く', '1,2,0', true]]) {
  for (let rep = 0; rep < 2; rep++) {
    const o = await begin(url, { sp: 7, stage: 0 })
    await send(o.p, `setparts|1,${parts}`)
    await o.place(1, near ? 20.5 : -10, near ? 0 : 8, 0, 0, 6); await wait(2000)
    await o.p.evaluate(() => { window.__maxR = 0; window.__minY = 99; setInterval(() => { const k = KB.state && KB.state.k.find(x => x.i === 0); if (k) { window.__maxR = Math.max(window.__maxR, Math.hypot(k.wx, k.wz)) } }, 4) })
    await send(o.p, 'gauge|100'); await o.clear()
    await o.place(1, near ? 20.5 : -10, near ? 0 : 8, 0, 0, 5.5)   // 相手を置き直す（転がって離れないように）
    await o.place(0, 26.5, 0, 5, 0, 7)   // 縁の外の空中（外向きに飛んでいる）
    await send(o.p, 'special')
    await wait(4500)
    const k = await o.k(0), k1 = await o.k(1), ev = (await o.evs('chain')).length
    rows.push({ label, rep, alive: k.a, 最終r: +Math.hypot(k.wx, k.wz).toFixed(1), 相手の最終r: +Math.hypot(k1.wx, k1.wz).toFixed(1), 相手生存: k1.a, chain: ev, gauge: +k.g.toFixed(2) })
    await o.b.close()
  }
}
for (const r of rows) console.log(JSON.stringify(r))
const f = (l) => rows.filter(r => r.label === l)
ok(f('相手なし(遠い)').every(r => r.alive === 0 && r.gauge >= 0.99), '相手が遠い時は空振り（ゲージを消費しない・戻れない）')
ok(f('重い相手が近く').every(r => r.alive === 1), '重い相手にかければ確実に戻れる')
console.log('軽い相手: 戻れた', f('軽い相手が近く').filter(r => r.alive).length, '/ 2・相手も落ちた', f('軽い相手が近く').filter(r => !r.相手生存).length)
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
