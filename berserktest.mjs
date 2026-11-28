// 狂戦化（通常）と終焉狂神（極大）の検査。node berserktest.mjs [url] [WxH] [touch]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const [W, H] = (process.argv[3] || '1280x720').split('x').map(Number), mobile = process.argv[4] === 'touch'
const tag = `${W}x${H}${mobile ? 't' : ''}`
async function p_wait(o, ms, f) { const t0 = Date.now(); while (Date.now() - t0 < ms) { if (f(await o.k(0))) return true; await wait(200) } return false }
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
// ---- 通常 ----
{
  const o = await begin(url, { sp: 10, n: 1, w: W, h: H, mobile, stage: 0, pas: 5 })   // pas 5 = 不屈（回転0でも倒れない）
  await send(o.p, 'cpuoff|1'); await o.place(0, -6, 0, 0, 0, 1); await o.place(1, 6, 0, 0, 0, 1); await wait(1200)
  await send(o.p, 'gauge|100'); await o.clear(); await send(o.p, 'specialnear'); await wait(400)
  ok((await o.evs('special')).length === 0, '狂戦化: 回転が5割以上では撃てない')
  const nd = (await o.k(0)).nd; ok(Math.abs(nd * 5 - 0.85) < 0.01, `必要ゲージ 85% (${nd})`)
  await send(o.p, 'spin|0.4,0'); await wait(300); await send(o.p, 'gauge|100'); await o.clear(); await send(o.p, 'specialnear'); await wait(500)
  ok((await o.evs('special')).some(e => e.p === 10), '狂戦化: 回転が5割を切ると撃てる')
  await send(o.p, 'inmv|1,0'); let vmax = 0; for (let i = 0; i < 12; i++) { await wait(150); const q = await o.k(0); vmax = Math.max(vmax, Math.hypot(q.vx, q.vz)); if (q.wx > 14) await send(o.p, 'tp|-12,2,0,0,0,0,0') }
  const s1 = await o.k(0); ok(s1.bz === 1, `暴走中の印 bz=${s1.bz}`)
  ok(vmax > 9.5 * 1.3, `速さが上がる (最高 ${vmax.toFixed(1)} m/s)`)
  await o.shot(`bz_${tag}_a`)
  await send(o.p, 'timescale|4'); await p_wait(o, 40000, k => k.bz !== 1); await send(o.p, 'inmv')
  const s2 = await o.k(0); ok(s2.bz === 3 || s2.bz === 0, `終わるとふらつき (bz=${s2.bz})`)
  ok(s2.sp <= 0.07 && s2.a === 1, `回転が約5%まで落ち、不屈でも倒れていない (sp=${s2.sp.toFixed(3)} a=${s2.a})`)
  await p_wait(o, 40000, k => k.bz === 0); ok((await o.k(0)).bz === 0, 'ふらつきが終わる'); await send(o.p, 'timescale|1')
  console.log('errs', o.errs); ok(o.errs.length === 0, '通常: コンソールエラー0'); await o.b.close()
}
// ---- 極大 ----
{
  const o = await begin(url, { sp: 10, n: 1, w: W, h: H, mobile, stage: 0 })
  await send(o.p, 'cpuoff|1'); await o.place(0, -6, 0, 0, 0, 1); await o.place(1, 6, 0, 0, 0, 1); await wait(1200)
  await send(o.p, 'spin|0.4,0'); await send(o.p, 'gauge|500'); await wait(300); await o.clear(); await send(o.p, 'specialx'); await wait(500)
  ok((await o.evs('evo')).some(e => e.p === 10), '終焉狂神が出る')
  await send(o.p, 'inmv|1,0'); await wait(1500); await o.shot(`bz_${tag}_evo`)
  // 倒すと延びる
  await send(o.p, 'inmv'); { const t = await o.k(1); await o.place(0, t.wx - 3, t.wz, 8, 0, 0.5) } await send(o.p, 'spin|0.001,1'); await wait(1500)
  const bk = await o.evs('bzkill'); ok(bk.length >= 1, `倒すと時間が延びる (${bk.length}回)`)
  console.log('errs', o.errs); ok(o.errs.length === 0, '極大: コンソールエラー0'); await o.b.close()
}
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(NG.length ? 1 : 0)
