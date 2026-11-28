import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const { b, p, errs } = await open(url, { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1')
await p.evaluate(() => { window.KB.events.length = 0 })
const me = async () => (await state(p)).k[0]
const evs = k => p.evaluate(k => window.KB.events.filter(e => e.k === k).length, k)
// 1 jump
for (const leg of [0, 2, 4]) {
  await send(p, 'start|0,0,' + leg + ',1'); await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 60000 }); await send(p, 'cpuoff|1')
  await wait(800); while ((await me()).air) await wait(100); await wait(300); let mh = 0; const g0 = (await me()).h
  await send(p, 'jump|1')
  const t0 = Date.now(); while (Date.now() - t0 < 900) { const k = await me(); mh = Math.max(mh, k.h - g0); await wait(30) }
  console.log('jump leg', leg, 'maxH', mh.toFixed(2))
}
// 2 super (double shift) vs single
await send(p, 'start|0,0,0,1'); await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 60000 }); await send(p, 'cpuoff|1'); await wait(500)
const peak = async (fn) => { let m = 0; await fn(); const t0 = Date.now(); while (Date.now() - t0 < 2200) { const k = await me(); m = Math.max(m, Math.hypot(k.vx, k.vz)); await wait(40) } return m }
await p.keyboard.down('KeyW')
const single = await peak(async () => { await p.keyboard.down('Shift') })
await p.keyboard.up('Shift'); await wait(1500)
const dbl = await peak(async () => { await p.keyboard.down('Shift'); await p.keyboard.up('Shift'); await wait(120); await p.keyboard.down('Shift'); await p.keyboard.up('Shift') })
console.log('peak speed sprint-only', single.toFixed(1), 'double-shift', dbl.toFixed(1), 'super events', await evs('super'))
await p.keyboard.up('KeyW')
// slow double (0.5s) must not fire
const before = await evs('super'); await wait(6500)
await p.keyboard.down('Shift'); await p.keyboard.up('Shift'); await wait(500); await p.keyboard.down('Shift'); await p.keyboard.up('Shift'); await wait(200)
console.log('slow double fired?', (await evs('super')) - before)
// 3 board
await send(p, 'start|0,0,0,1'); await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 60000 }); await send(p, 'cpuoff|1')
await send(p, 'fightt|41'); await wait(400)
await send(p, 'spin|0.6,0')
const a = Math.PI / 3, rr = 20.6
const s0 = await me()
await send(p, 'tp|' + [Math.cos(a) * rr, s0.gh + 0.05, Math.sin(a) * rr, 0, 0, 0].map(v => v.toFixed(2)).join(','))
await wait(150); const sp0 = (await me()).sp
let mx = 0, rs = [], path = []
for (let i = 0; i < 40; i++) { const k = await me(); mx = Math.max(mx, Math.hypot(k.vx, k.vz)); rs.push(k.r); path.push(Math.atan2(k.wz, k.wx)); if (i === 12) await p.screenshot({ path: 'shots/_board.png' }); await wait(150) }
const k1 = await me()
console.log('board: maxSpeed', mx.toFixed(1), 'r range', Math.min(...rs).toFixed(1), Math.max(...rs).toFixed(1), 'spin', sp0.toFixed(3), '->', k1.sp.toFixed(3), 'angle swept (deg)', ((path[path.length-1]-path[0])*180/Math.PI).toFixed(0), 'boost ev', await evs('boost'), 'board ev', await evs('board'))
// 4 fire
await send(p, 'start|0,0,0,1'); await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 60000 }); await send(p, 'cpuoff|1')
await send(p, 'fightt|12'); await send(p, 'spin|1,0'); await wait(2600)
console.log('miracle ev', await evs('miracle'))
await p.keyboard.down('KeyW'); await wait(1800); await p.screenshot({ path: 'shots/_fire.png' }); await p.keyboard.up('KeyW')
console.log('errs', errs); await b.close()
