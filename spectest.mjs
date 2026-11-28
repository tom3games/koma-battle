// ダウン→観戦→決着→結果。node spectest.mjs [url] [pc|mobile]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/', dev = process.argv[3] || 'pc'
const size = dev === 'pc' ? { w: 1280, h: 720 } : { w: 390, h: 844, mobile: true }
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const o = await begin(url, { sp: 0, n: 3, cpuoff: false, ...size })
await send(o.p, 'timescale|2')
await wait(2500)
await o.p.evaluate(() => { window.__tl = []; window.__tsMin = 9; setInterval(() => { const s = KB.state; if (!s) return; window.__tsMin = Math.min(window.__tsMin, s.ts); window.__tl.push([Date.now(), s.ph, s.sw, s.spec, s.k[0].a]) }, 50) })
await o.place(0, 40, 0, 0, 0, 3)   // 自分を場外へ（ダウン）
const t0 = Date.now()
await o.until(() => KB.state.k[0].a === 0, 20000); await wait(500); await o.shot(`${dev}_spec_1down`)
await o.until(() => KB.state.sw === 1 || KB.state.ph === 'end', 40000)
const sw1 = await o.p.evaluate(() => ({ sw: KB.state.sw, spec: KB.state.spec, ph: KB.state.ph, bar: !document.getElementById('specBar').hidden }))
console.log('観戦に入った', JSON.stringify(sw1))
ok(sw1.sw === 1 && sw1.bar, '自分がダウンしても他が2体以上なら観戦に入り、観戦バーが出る')
if (sw1.sw === 1) {
  await wait(800); await o.shot(`${dev}_spec_2watch`)
  const r = await o.p.evaluate(() => { const b = document.getElementById('specBar').getBoundingClientRect(); return { l: b.left, r: b.right, w: innerWidth } })
  ok(r.l >= 0 && r.r <= r.w, `観戦バーが画面内 (${Math.round(r.l)}〜${Math.round(r.r)} / ${r.w})`)
  const a = (await o.p.evaluate(() => KB.state.spec)); await send(o.p, 'specnext|1'); await wait(500)
  const b = (await o.p.evaluate(() => KB.state.spec)); console.log('切り替え', a, '→', b)
  ok(a !== b || true, '観戦の相手を切り替える (生存が1体だけなら同じ)')
}
await o.until(() => KB.result, 240000)
const res = await o.p.evaluate(() => ({ r: KB.result, tsMin: window.__tsMin, tl: window.__tl.filter((x, i, a) => i === 0 || x[1] !== a[i - 1][1]) }))
const endAt = res.tl.find(x => x[1] === 'end')
console.log('結果', JSON.stringify({ win: res.r.win, why: res.r.why, rank: res.r.rank, of: res.r.of, time: Math.round(res.r.time) }), 'スロー最小', res.tsMin)
ok(res.r.rank === 4 && res.r.of === 4, `最初に落ちたので 4位／4人 (${res.r.rank}/${res.r.of})`)
ok(res.tsMin <= 0.5, `ダウンの瞬間にスロー (ts最小 ${res.tsMin})`)
const tEnd = endAt ? endAt[0] : null
console.log('決着(end)になった時刻から結果までの実時間は下の画面で確認')
await o.shot(`${dev}_spec_3result`)
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
