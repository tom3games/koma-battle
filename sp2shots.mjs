// 各技の画面写真（撃つ瞬間・最中・当たった後）。node sp2shots.mjs <pc|mobile> [技] [url]
import { begin, wait, send } from './sp2lib.mjs'
const dev = process.argv[2] || 'pc', only = process.argv[3] || 'all', url = process.argv[4] || 'http://localhost:5393/'
const size = dev === 'pc' ? { w: 1280, h: 720 } : { w: 390, h: 844, mobile: true }
const want = n => only === 'all' || only === n
const cnt = (o, k) => o.evs(k).then(a => a.length)
async function align(o, x, z, dx, dz) { await o.place(0, x, z, 0, 0, 1.2); await wait(300); await send(o.p, 'look0'); const yaw = await o.p.evaluate(() => window.KB.state.k[0].yaw); const want = Math.atan2(dx, dz); let d = want - yaw; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI; await send(o.p, `tlook|${d / 0.0075},0`); await wait(500); await o.place(0, x, z, 0, 0, 1.2) }
async function setup(spn, extra = {}) {
  const o = await begin(url, { sp: spn, ...size, ...extra })
  return o
}
if (want('arrow')) {
  const o = await setup(6)
  await o.place(1, 6, 0); await align(o, -8, 0, 1, 0); await wait(600)
  await send(o.p, 'gauge|100'); await o.clear()
  await send(o.p, 'spfire|0,6,0')
  await o.until(() => window.KB.events.filter(e => e.k === 'arrowring').length >= 3, 15000); await o.shot(`${dev}_arrow_1rings`)
  await o.until(() => window.KB.events.filter(e => e.k === 'arrowring').length >= 5, 15000); await wait(100); await o.shot(`${dev}_arrow_2rings5`)
  await o.until(() => window.KB.events.some(e => e.k === 'arrowgo'), 15000); await wait(60); await o.shot(`${dev}_arrow_3dash`)
  await o.until(() => window.KB.events.some(e => e.k === 'freeze'), 15000); await wait(120); await o.shot(`${dev}_arrow_4hit`)
  await wait(700); await o.shot(`${dev}_arrow_5frozen`)
  await o.b.close()
}
if (want('anchor')) {
  const o = await setup(4)
  await o.place(1, -3, 0); await align(o, -8, 0, 1, 0); await wait(600)
  await send(o.p, 'gauge|100'); await send(o.p, 'special'); await wait(250); await o.shot(`${dev}_anchor_1fire`)
  await wait(500); await o.shot(`${dev}_anchor_2hold`)
  await o.place(1, -4.4, 0, -14, 0, 1.0); await wait(200); await o.shot(`${dev}_anchor_3hit`)
  await o.b.close()
}
if (want('chain')) {
  const o = await setup(7)
  await o.place(1, -4.5, 0); await align(o, -9, 0, 1, 0); await wait(600)
  await send(o.p, 'gauge|100'); await send(o.p, 'special'); await wait(300); await o.shot(`${dev}_chain_1fire`)
  for (let i = 0; i < 28 && !(await cnt(o, 'chainthrow')); i++) {
    const k0 = await o.k(0), k1 = await o.k(1)
    const rx = k0.wx - k1.wx, rz = k0.wz - k1.wz, rl = Math.hypot(rx, rz) || 1
    await send(o.p, `inmv|${-rz / rl - rx / rl * (rl - 5.8) * 0.6},${rx / rl - rz / rl * (rl - 5.8) * 0.6}`)
    await wait(100); if (i === 8) await o.shot(`${dev}_chain_2swing`)
  }
  await send(o.p, 'inmv'); await wait(250); await o.shot(`${dev}_chain_3throw`)
  await o.b.close()
}
if (want('spiral')) {
  const o = await setup(5)
  await o.place(1, -2, 0); await align(o, -8, 0, 1, 0); await wait(600)
  await send(o.p, 'spin|0.1,0'); await send(o.p, 'gauge|100'); await o.clear()
  await o.shot(`${dev}_spiral_0ready`)
  await send(o.p, 'spfire|0,-2,0'); await wait(200); await o.shot(`${dev}_spiral_1charge`)
  await o.until(() => window.KB.events.some(e => e.k === 'spiralgo'), 15000); await wait(100); await o.shot(`${dev}_spiral_2lunge`)
  await o.until(() => window.KB.events.some(e => e.k === 'boom'), 15000); await wait(100); await o.shot(`${dev}_spiral_3impact`)
  await o.b.close()
}
if (want('pit')) {
  const o = await setup(0, { ts: 1 })
  await o.place(1, 10, 0); await align(o, -10, 0, 1, 0); await send(o.p, 'timescale|2')
  await send(o.p, 'pitstart')
  let n = 0
  for (let i = 0; i < 900 && !(await cnt(o, 'pitfall')); i++) {
    await o.place(0, -10, 0, 0, 0, 1.2); await o.place(1, 10, 0, 0, 0, 1.2)
    const pr = await o.p.evaluate(() => window.KB.events.filter(e => e.k === 'crackstep').length)
    if (pr >= 2 && n === 0) { await o.shot(`${dev}_pit_1crack`); n = 1 }
    if (pr >= 5 && n === 1) { await o.shot(`${dev}_pit_2crack`); n = 2 }
    if (pr >= 8 && n === 2) { await o.shot(`${dev}_pit_3shake`); n = 3 }
    await wait(120)
  }
  await wait(250); await o.shot(`${dev}_pit_4fall`)
  await wait(1200); await o.shot(`${dev}_pit_5open`)
  await o.b.close()
}
process.exit(0)
