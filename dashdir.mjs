// スマホのダッシュの向き: カメラを回した後、右スティックを8方向へ倒して離し、進む向きが画面の向きと合うか（本物のタッチ）。node dashdir.mjs [url]
import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open(process.argv[2] || 'http://localhost:5393/', { w: 844, h: 390, mobile: true })
const cdp = await p.context().newCDPSession(p)
const T = (type, pts) => cdp.send('Input.dispatchTouchEvent', { type, touchPoints: pts })
await wait(600); await p.evaluate(() => document.getElementById('btnPlay').click()); await wait(500); await p.evaluate(() => document.getElementById('btnFight').click()); await wait(5500)
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1'); await wait(300)
const z = await p.locator('#flickZone').boundingBox(); const cx = z.x + z.width / 2, cy = z.y + z.height / 2
const me = async () => (await state(p)).k.find(k => k.i === 0)
const park = async (vx, vz) => { for (const [i, x, zz] of [[1, 13, 13], [2, -13, 13], [3, 13, -13]]) await send(p, `tp|${x},1,${zz},0,0,0,${i}`); await send(p, `tp|0,1,-3,${vx},0,${vz},0`) }
let bad = 0, n = 0
async function trial(deg, mode) {
  await park(0, 0); await wait(1900)
  // 視点を大きく回す
  await T('touchStart', [{ x: 420, y: 150, id: 5 }]); for (let i = 1; i <= 5; i++) { await T('touchMove', [{ x: 420 + i * (30 + n * 5 % 40), y: 150, id: 5 }]); await wait(20) } await T('touchEnd', [])
  await park(6, 0); await wait(150)
  const a = deg * Math.PI / 180, dx = Math.sin(a), dy = -Math.cos(a)
  const d0 = await p.evaluate(() => window.KB.events.filter(e => e.k === 'dash' && e.i === 0).length)
  if (mode === 'edge') { await T('touchStart', [{ x: cx + dx * 55, y: cy + dy * 55, id: 6 }]); await wait(90) }
  else { await T('touchStart', [{ x: cx, y: cy, id: 6 }]); for (let i = 1; i <= 4; i++) { await T('touchMove', [{ x: cx + dx * 14 * i, y: cy + dy * 14 * i, id: 6 }]); await wait(25) } if (mode === 'back') for (let i = 3; i >= 0; i--) { await T('touchMove', [{ x: cx + dx * 14 * i, y: cy + dy * 14 * i, id: 6 }]); await wait(25) } }
  await T('touchEnd', [])
  await wait(130); const k = await me(); const d1 = await p.evaluate(() => window.KB.events.filter(e => e.k === 'dash' && e.i === 0).length)
  n++
  if (mode === 'back') { const ok = d1 === d0; if (!ok) bad++; console.log(mode, deg, ok ? 'ダッシュしない OK' : 'NG'); return }
  const yaw = k.yaw, wx = Math.sin(a) * Math.cos(yaw) + Math.cos(a) * Math.sin(yaw), wz = -Math.sin(a) * Math.sin(yaw) + Math.cos(a) * Math.cos(yaw)
  const sp = Math.hypot(k.vx, k.vz), err = Math.acos(Math.max(-1, Math.min(1, (k.vx * wx + k.vz * wz) / (sp || 1)))) * 180 / Math.PI
  const ok = d1 > d0 && err < 12; if (!ok) bad++
  console.log(mode, deg, 'yaw', (yaw * 57.3).toFixed(0), 'ずれ', err.toFixed(1), 'v', sp.toFixed(1), 'dash', d1 - d0, 'pos', k.wx.toFixed(1), k.wz.toFixed(1), k.air, await p.evaluate(() => window.KB.events.slice(-3).map(e => e.k).join()), ok ? 'OK' : 'NG')
}
for (const d of [0, 45, 90, 135, 180, 225, 270, 315]) await trial(d, 'flick')
for (const d of [60, 60, 60, 200, 300, 20, 120]) await trial(d, 'edge')
for (const d of [90, 270]) await trial(d, 'back')
console.log('NG', bad, '/', n, errs); await b.close()
