// パーツ破損の検査: 刃（横）・円盤（上から）・軸（かち上げ）が削れて壊れる／軸が折れたらダウン（why=shaft）／HUD の印。node partstest.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const o = await begin(url, { sp: 0, w: 960, h: 540, stage: 0 })
const pb = async i => (await o.k(i)).pb
await o.place(0, -8, 0, 0, 0, 1); await o.place(1, 6, 0, 0, 0, 1); await wait(1500)
ok((await pb(1)).every(v => v === 1), `最初は3パーツとも満タン ${JSON.stringify(await pb(1))}`)
// 刃: 横から何度もぶつける
for (let i = 0; i < 14 && (await pb(1))[0] > 0; i++) {
  await send(o.p, 'gauge|0'); await o.place(0, -2, 0, 0, 0, 0.3); await o.place(1, 1, 0, 0, 0, 0.3); await wait(200)
  await o.place(0, -1.2, 0, 14, 0, 0.3); await wait(450)
}
const b = await pb(1); ok(b[0] < 1, `横の体当たりで刃が削れる ${b[0].toFixed(2)}`)
await send(o.p, 'timescale|1')
// 円盤: 上から
for (let i = 0; i < 12 && (await pb(1))[1] > 0; i++) {
  const t = await o.k(1); await o.place(0, t.wx - 2, t.wz, 0, 0, 1); await wait(300)
  await o.place(0, t.wx + 0.3, t.wz, 0, 0, t.gh + 4); await send(o.p, 'tp|' + [t.wx + 0.3, t.gh + 4, t.wz, 0, -16, 0, 0].join(',')); await wait(600)
}
const d = await pb(1); ok(d[1] < 1, `上からの突撃で円盤が削れる ${d[1].toFixed(2)}`)
// 軸: かち上げで削れる（1回）→ 折れるまで削る
await o.clear(); const s0 = (await pb(1))[2]
for (let i = 0; i < 20 && (await pb(1))[2] >= s0 - 0.001; i++) {
  const t = await o.k(1)
  await o.place(0, t.wx - 2.5, t.wz, 0, 0, 0.3); await wait(400); await send(o.p, 'leanw|1,0'); await send(o.p, 'inmv|1,0'); await wait(850); await send(o.p, 'inmv')
}
ok((await pb(1))[2] < s0 - 0.001, `かち上げで軸が削れる ${s0.toFixed(2)}→${(await pb(1))[2].toFixed(2)}`)
await send(o.p, 'partwear|1,2,1000'); await wait(600)
console.log('after', JSON.stringify(await pb(1)), (await o.k(1)).a, JSON.stringify(await o.evs('ko')), JSON.stringify((await o.evs('upper')).length))
const ko = (await o.evs('ko')).find(e => e.i === 1)
ok(ko && ko.why === 'shaft', `軸が折れたら軸折れでダウン (${ko && ko.why})`)
const br = await o.evs('pbreak'); ok(br.some(e => e.p === 2), `折れた通知 ${JSON.stringify(br.map(e => e.p))}`)
ok(await o.p.evaluate(() => document.querySelectorAll('.tag .pb').length >= 2), '名札に3パーツの印')
await o.shot('parts_hud')
console.log('errs', o.errs); ok(o.errs.length === 0, 'コンソールエラー0')
await o.b.close(); console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(NG.length ? 1 : 0)
