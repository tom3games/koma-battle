// スマホ: 左右のスティックの場所が固定か・外のなぞりで視点が回るか・同時に3本指で使えるか。node camswipe.mjs [url]
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
for (const [W, H] of [[390, 844], [360, 640], [844, 390]]) {
  const { b, p, errs } = await open(url, { w: W, h: H, mobile: true })
  const cdp = await p.context().newCDPSession(p), touches = new Map()
  const T = async (type, id, x, y) => { if (type === 'touchEnd') { touches.delete(id) } else touches.set(id, [x, y]); await cdp.send('Input.dispatchTouchEvent', { type, touchPoints: type === 'touchEnd' ? [...touches.entries()].map(([i, q]) => ({ x: q[0], y: q[1], id: i })) : [...touches.entries()].map(([i, q]) => ({ x: q[0], y: q[1], id: i })) }) }
  await p.click('#btnPlay'); await wait(700); await p.click('#btnFight'); await wait(5500)
  await p.evaluate(() => { window.__s = []; const o = window.unityInstance.SendMessage.bind(window.unityInstance); window.unityInstance.SendMessage = (a, f, c) => { window.__s.push(String(c)); return o(a, f, c) } })
  const pos = () => p.evaluate(() => { const r = s => { const q = document.querySelector(s).getBoundingClientRect(); return [Math.round(q.left + q.width / 2), Math.round(q.top + q.height / 2)] }; return { L: r('#stick'), R: r('#faOrigin'), z: r('#stickZone'), f: r('#flickZone') } })
  const a = await pos()
  await p.screenshot({ path: `shots/cam_${W}_a.png` })
  await T('touchStart', 1, a.z[0] + 20, a.z[1] - 40); await wait(80); const b1 = await pos()
  await T('touchStart', 2, a.f[0], a.f[1]); await T('touchMove', 2, a.f[0] + 50, a.f[1] - 20); await wait(80); const c1 = await pos()
  const cx = Math.round(W * 0.5), cy = Math.round(H * 0.4)
  await T('touchStart', 3, cx, cy); for (let i = 1; i <= 8; i++) { await T('touchMove', 3, cx + i * 14, cy); await wait(20) }
  await p.screenshot({ path: `shots/cam_${W}_b.png` })
  const s1 = await p.evaluate(() => window.__s.slice())
  await T('touchEnd', 3); await T('touchEnd', 2); await T('touchEnd', 1); await wait(100)
  const s2 = await p.evaluate(() => window.__s.slice())
  const tl = s1.filter(x => x.startsWith('tlook')).reduce((n, x) => n + parseFloat(x.slice(6)), 0)
  const fixed = [b1, c1].every(q => q.L.join() === a.L.join() && q.R.join() === a.R.join())
  const over = await p.evaluate(() => { const A = document.querySelector('#tSpBtn').getBoundingClientRect(), f = document.querySelector('#flickZone').getBoundingClientRect(), cx = f.left + f.width / 2, cy = f.top + f.height / 2; const dx = A.left + A.width / 2 - cx, dy = A.top + A.height / 2 - cy; return Math.round(Math.hypot(dx, dy) - A.width / 2 - 60) })
  console.log(W + 'x' + H, JSON.stringify({ fixed, pos: a, tlook: tl, move: s1.some(x => /^move\|0\.\d*[1-9]/.test(x)), aim: s1.some(x => x.startsWith('aimdir')), dash: s2.some(x => /^dashdir\|0\.9/.test(x)), stop: s2.lastIndexOf('move|0,0') > s2.findLastIndex(x => /^move\|/.test(x) && x !== 'move|0,0'), spGap: over, errs }))
  await b.close()
}
