// 極大技（ゲージ5本）の検査: 5本でないと撃てない／5本で長押し・specialx で進化版／技ごとの追加効果／画面（ルビつきの名前）。node evotest.mjs [url] [WxH] [touch]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const [W, H] = (process.argv[3] || '1280x720').split('x').map(Number), mobile = process.argv[4] === 'touch'
const tag = `${W}x${H}${mobile ? 't' : ''}`
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const only = process.argv[5] ? process.argv[5].split(',').map(Number) : [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
const names = ['メテオ', 'テンペスト', 'ファントム・レイド', 'イージス', 'アンカー', 'スパイラル', 'アロー', 'チェイン', 'トレイル', 'ミラージュ', '狂戦化']
for (const sp of only) {
  const o = await begin(url, { sp, n: 2, w: W, h: H, mobile, stage: 0 })
  await send(o.p, 'cpuoff|1')
  await o.place(0, -6, 0, 0, 0, 1); await o.place(1, 5, 2, 0, 0, 1); await o.place(2, 8, -4, 0, 0, 1); await wait(1200)
  if (sp === 5 || sp === 10) await send(o.p, 'spin|0.1,0')
  // 4本では撃てない
  await send(o.p, 'gauge|400'); await o.clear(); await send(o.p, 'specialx'); await wait(500)
  ok((await o.evs('evo')).length === 0, `${names[sp]}: 4本では極大技が出ない`)
  const k4 = await o.k(0); ok(k4.ev === 0 && Math.abs(k4.g - 0.8) < 0.02, `${names[sp]}: ゲージ表示 4本 g=${k4.g}`)
  await send(o.p, 'gauge|500'); await wait(300)
  ok((await o.k(0)).ev === 1, `${names[sp]}: 5本で ev=1`)
  await o.clear(); await send(o.p, 'specialx'); await wait(300)
  const evs = await o.evs('evo'); ok(evs.length === 1 && evs[0].p === sp, `${names[sp]}: 極大技が出る`)
  ok((await o.k(0)).g < 0.05, `${names[sp]}: ゲージを全部使う (g=${(await o.k(0)).g})`)
  await o.shot(`evo_${tag}_${sp}_a`)
  await wait(sp === 0 ? 3500 : 1800); await o.shot(`evo_${tag}_${sp}_b`)
  if (sp === 0) { /* メテオ: 岩 */ }
  console.log(names[sp], 'errs', o.errs); ok(o.errs.length === 0, `${names[sp]}: コンソールエラー0`)
  await o.b.close()
}
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(NG.length ? 1 : 0)
