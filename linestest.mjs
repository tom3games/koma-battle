// 効果線: CPU 同士の衝突では出ない・自分が当たった時は出る。node linestest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const o = await begin(url, { sp: 0, n: 2, stage: 0 })
await o.p.evaluate(() => { window.__ln = 0; new MutationObserver(m => { for (const r of m) if (r.addedNodes.length) window.__ln++ }).observe(document.getElementById('lines'), { childList: true }) })
const lines = () => o.p.evaluate(() => window.__ln)
const clear = () => o.p.evaluate(() => { window.__ln = 0; KB.events.length = 0 })
// ① CPU1 が CPU2 の真上から落ちる（空中からの強い当たり）。自分は離れた所
await o.place(0, -14, -8, 0, 0, 1.0); await o.place(2, 6, 0, 0, 0, 1.0); await o.place(1, 14, 8, 0, 0, 1.0); await wait(2000)
await clear()
await o.place(1, 6, 0, 0, 0, 5)
await o.until(() => KB.events.some(e => e.k === 'hit'), 8000); await wait(500)
const hits1 = await o.evs('hit'); const n1 = await lines(); await o.shot('pc_lines_cpuonly')
console.log('CPU同士', JSON.stringify({ hit: hits1.length, big: hits1.map(h => [h.a, h.b, h.air, h.big]), lines: n1 }))
ok(hits1.some(h => h.a !== 0 && h.b !== 0) && n1 === 0, `CPU 同士の当たりでは集中線が出ない (hit ${hits1.length} lines ${n1})`)
// ② 自分が踏まれる
await o.place(0, -2, 8, 0, 0, 1.0); await o.place(2, 14, -8, 0, 0, 1.0); await o.place(1, 14, 8, 0, 0, 1.0); await wait(2000)
await clear()
const pk = await o.k(0); await o.place(1, pk.wx, pk.wz, 0, 0, pk.h + 4)
await o.until(() => KB.events.some(e => e.k === 'hit' && (e.a === 0 || e.b === 0)), 8000); await wait(300)
const n2 = await lines(); await o.shot('pc_lines_me')
console.log('自分が当たった', JSON.stringify({ lines: n2, hits: (await o.evs('hit')).map(h => [h.a, h.b, h.air, h.big]) }))
ok(n2 > 0, `自分が当たった時は集中線が出る (lines ${n2})`)
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
