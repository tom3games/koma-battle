// ソニック・トレイル(8)・ファントム・ミラージュ(9)・ゲージ量の検査。node sp3test.mjs [url] [WxH] [touch]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const [W, H] = (process.argv[3] || '1280x720').split('x').map(Number), mobile = process.argv[4] === 'touch'
const tag = `${W}x${H}${mobile ? 't' : ''}`
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
// ---- トレイル ----
{
  const o = await begin(url, { sp: 8, w: W, h: H, mobile, stage: 0 })
  await o.place(0, -8, 0, 0, 0, 1); await o.place(1, 0, 4, 0, 0, 1); await wait(1500)
  const nd = (await o.k(0)).nd; ok(Math.abs(nd * 5 - 0.75) < 0.01, `トレイルの必要ゲージ 0.75 (${nd})`)
  await send(o.p, 'gauge|70'); await send(o.p, 'specialnear'); await wait(300)
  ok((await o.evs('special')).length === 0, '70%では撃てない（必要75%）')
  await send(o.p, 'gauge|80'); await o.clear(); await send(o.p, 'specialnear'); await wait(400)
  ok((await o.evs('special')).some(e => e.i === 0 && e.p === 8), 'ソニック・トレイルが出る')
  const g1 = (await o.k(0)).g; ok(g1 < 0.2, `ゲージを必要量だけ使う (残 ${g1.toFixed(2)})`)
  // 的の周りを走り抜けて帯を作る
  const samp = []; await send(o.p, 'inmv|0,1'); await wait(700); { const q = await o.k(0); samp.push([q.wx, q.wz]) } await wait(500); await send(o.p, 'inmv|1,0'); await wait(1000); await send(o.p, 'inmv|0,-1'); await wait(1000); await send(o.p, 'inmv')
  await o.shot(`sp3_${tag}_trail`)
  const lines = await o.p.evaluate(() => 1)
  const before = (await o.k(1)).sp
  // 帯の上へ的を置く（自分は離れる）
  await o.place(0, 14, -8, 0, 0, 1); await o.clear()
  await o.place(1, samp[0][0], samp[0][1], 0, 0, 1); await wait(800)
  const th = (await o.evs('trailhit')).length
  ok(th >= 1, `帯に触れた相手が削られる (${th}回)`); ok((await o.k(1)).sp < before - 0.001, `回転が減った (${before.toFixed(3)}→${(await o.k(1)).sp.toFixed(3)})`)
  console.log('errs', o.errs)
  await o.b.close()
}
// ---- ミラージュ ----
{
  const o = await begin(url, { sp: 9, w: W, h: H, mobile, stage: 0 })
  await o.place(0, -8, 0, 0, 0, 1); await o.place(1, 10, 6, 0, 0, 1); await wait(1500)
  const nd = (await o.k(0)).nd; ok(Math.abs(nd * 5 - 0.9) < 0.01, `ミラージュの必要ゲージ 0.9 (${nd})`)
  await send(o.p, 'gauge|100'); await o.clear(); await send(o.p, 'specialnear'); await wait(500)
  ok((await o.evs('special')).some(e => e.i === 0 && e.p === 9), '分身が出る')
  await send(o.p, 'inmv|1,0.3'); await wait(800); await o.shot(`sp3_${tag}_mirage`)
  // 分身が相手に当たる: 的を自分の脇（2.4m 横）の進路に置く
  await send(o.p, 'inmv'); await o.clear()
  for (let r = 0; r < 4 && !(await o.evs('mirhit')).length; r++) {
    await send(o.p, 'gauge|100'); if (r > 0) { await send(o.p, 'specialnear'); await wait(300) }
    await o.place(0, -8, 0, 0, 0, 1); await wait(500)
    const me = await o.k(0)
    await o.place(1, me.wx + 5, me.wz + 2.4, 0, 0, 1)
    await send(o.p, 'inmv|1,0'); await wait(1400); await send(o.p, 'inmv')
  }
  const mh = (await o.evs('mirhit')).length
  ok(mh >= 1, `分身が相手に当たった (${mh}回)`)
  console.log('errs', o.errs); ok(o.errs.length === 0, 'コンソールエラー0')
  await o.b.close()
}
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(NG.length ? 1 : 0)
