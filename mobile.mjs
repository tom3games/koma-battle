// スマホの最低限: 画面が崩れない・タッチで動かせる
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
for (const [w, h, tag] of [[390, 844, 'p'], [844, 390, 'l']]) {
  const { b, p, errs } = await open(url, { w, h, mobile: true })
  const over = async () => p.evaluate(() => ({ sh: document.documentElement.scrollHeight, ih: innerHeight, sw: document.documentElement.scrollWidth, iw: innerWidth }))
  await wait(800); await p.screenshot({ path: `shots/m_${tag}_1_title.png` })
  await p.tap('#btnPlay'); await wait(1000); await p.screenshot({ path: `shots/m_${tag}_2_select.png` })
  const o1 = await over()
  await p.tap('#btnFight'); await wait(4500)
  const s0 = (await state(p)).k[0]
  // スティック（左下）をドラッグ → 前へ
  const zone = await p.locator('#stickZone').boundingBox()
  const sx = zone.x + zone.width * 0.5, sy = zone.y + zone.height * 0.6
  await p.mouse.move(sx, sy); await p.mouse.down(); await p.mouse.move(sx, sy - 40, { steps: 4 })
  await wait(900); await p.screenshot({ path: `shots/m_${tag}_3_stick.png` })
  await p.mouse.up()
  const s1 = (await state(p)).k[0]
  await p.tap('#tDashBtn'); await wait(150); await p.screenshot({ path: `shots/m_${tag}_4_dash.png` })
  const dashes = await p.evaluate(() => window.KB.events.filter(e => e.k === 'dash' && e.i === 0).length)
  console.log(tag, JSON.stringify({ over: o1, moved: { z0: s0.wz, z1: s1.wz }, dashes, errs }))
  await b.close()
}
