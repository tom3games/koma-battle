// スマホの実寸で、タッチだけで1試合を最後まで遊ぶ（CDP の本物のタッチ）。各画面を撮る
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const sizes = (process.argv[3] || '390x844,844x390,667x375').split(',').map(s => s.split('x').map(Number))
for (const [W, H] of sizes) {
  const tag = `${W}x${H}`
  const { b, p, errs, loadMs } = await open(url, { w: W, h: H, mobile: true })
  const cdp = await p.context().newCDPSession(p)
  const shot = n => p.screenshot({ path: `shots/t_${tag}_${n}.png` })
  const over = () => p.evaluate(() => { const bad = []; for (const el of document.querySelectorAll('button,.part b,.tt-name,.slot-desc,.st,.tacho,.hud-time')) { const r = el.getBoundingClientRect(); if (r.width && (r.right > innerWidth + 1 || r.bottom > innerHeight + 1 || r.left < -1 || r.top < -1)) bad.push(el.id || el.className || el.textContent.slice(0, 10)) } return { sh: document.documentElement.scrollHeight, ih: innerHeight, bad } })
  const touches = new Map()
  const tp = () => [...touches.entries()].map(([id, [x, y]]) => ({ x, y, id }))
  const T = async (type, id, x, y) => { if (type === 'touchEnd') { touches.delete(id); await cdp.send('Input.dispatchTouchEvent', { type, touchPoints: [{ x, y, id }] }); return } touches.set(id, [x, y]); await cdp.send('Input.dispatchTouchEvent', { type, touchPoints: tp() }) }
  const tap = async sel => { const bb = await p.locator(sel).boundingBox(); const x = bb.x + bb.width / 2, y = bb.y + bb.height / 2; await T('touchStart', 9, x, y); await wait(60); await T('touchEnd', 9, x, y) }
  await wait(600); await shot('1_title'); const o1 = await over()
  await tap('#btnPlay'); await wait(900); await shot('2_select'); const o2 = await over()
  await tap('.part[data-slot="leg"][data-i="1"]'); await wait(300); await shot('2b_select_long'); const o3 = await over()
  await tap('#btnFight'); await wait(1500); await shot('3_hint')
  await wait(3000)
  // ボット: 左の親指で相手へ寄り、右の親指で相手の向きへはじく
  const zs = await p.locator('#stickZone').boundingBox(), zf = await p.locator('#flickZone').boundingBox()
  const scx = zs.x + zs.width * 0.45, scy = zs.y + zs.height * 0.6
  const fcx = zf.x + zf.width * 0.5, fcy = zf.y + zf.height * 0.6
  await p.evaluate(() => { window.__sends = {}; const o = window.unityInstance.SendMessage.bind(window.unityInstance); window.unityInstance.SendMessage = (a, b2, c) => { const k = String(c).split('|')[0]; window.__sends[k] = (window.__sends[k] || 0) + 1; return o(a, b2, c) } })
  await T('touchStart', 1, scx, scy)
  const t0 = Date.now(); let res = null, flicks = 0, specials = 0, shotN = 0
  await send(p, 'timescale|2')
  while (Date.now() - t0 < 150000) {
    const s = await state(p); res = await p.evaluate(() => window.KB.result); if (res) break
    if (!s || s.ph !== 'fight') { await wait(300); continue }
    const me = s.k.find(k => k.i === 0); if (!me || !me.a) { await wait(300); continue }
    const foes = s.k.filter(k => k.i !== 0 && k.a); if (!foes.length) { await wait(300); continue }
    foes.sort((a, c) => Math.hypot(a.wx - me.wx, a.wz - me.wz) - Math.hypot(c.wx - me.wx, c.wz - me.wz))
    const f = foes[0]; let dx = f.wx - me.wx, dz = f.wz - me.wz; const d = Math.hypot(dx, dz) || 1; dx /= d; dz /= d
    // 端に近いなら内へ
    const r = Math.hypot(me.wx, me.wz); let mx = dx, mz = dz; if (r > 10) { mx = -me.wx / r; mz = -me.wz / r }
    await T('touchMove', 1, scx + mx * 45, scy - mz * 45)
    if (me.g >= 1) { await tap('#tSpBtn'); specials++ }
    else if (me.cd === 0 && d < 7) {
      await T('touchStart', 2, fcx, fcy); await wait(40)
      await T('touchMove', 2, fcx + dx * 40, fcy - dz * 40); await wait(40)
      await T('touchMove', 2, fcx + dx * 80, fcy - dz * 80)
      if (shotN === 0) { await shot('4_flick'); shotN++ }
      await T('touchEnd', 2, fcx + dx * 80, fcy - dz * 80); flicks++
      if (shotN === 1) { await wait(120); await shot('5_dash'); shotN++ }
    }
    await wait(250)
  }
  if (touches.size) await T('touchEnd', 1, scx, scy)
  await wait(700); await shot('6_result'); const o4 = await over()
  const ev = await p.evaluate(() => { const c = {}; for (const e of window.KB.events) if (e.i === 0 || e.a === 0) c[e.k] = (c[e.k] || 0) + 1; return c })
  const fps = await p.evaluate(() => window.KB.fps); const sends = await p.evaluate(() => window.__sends)
  console.log(tag, JSON.stringify({ loadMs, ended: !!res, win: res && res.win, why: res && res.why, time: res && Math.round(res.time), flicks, specials, myEvents: ev, sends, fps, overflow: [o1, o2, o3, o4].map(o => o.sh > o.ih || o.bad.length ? o : 'ok'), errs }))
  await b.close()
}
