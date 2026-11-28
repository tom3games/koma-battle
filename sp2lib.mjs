// 2026-11-28 必殺4つ・中央の崩落の検査の共通部品
import { open, wait, send } from './lib.mjs'
export { wait, send }
export async function begin(url, { sp = 0, n = 1, w = 1280, h = 720, mobile = false, pas = 0, cpuoff = true, ts = 1, stage = +(process.env.STAGE ?? -1) } = {}) {
  const o = await open(url + (url.includes('?') ? '&' : '?') + 'lv=10', { w, h, mobile })
  const { p } = o
  await p.click('#btnPlay'); await wait(300)
  await p.evaluate(n => { document.querySelector(`#vsPick button[data-n="${n}"]`).click() }, n)
  await p.evaluate(([sp, pas, stage]) => { Object.assign(KB.pick, { top: 3, core: 4, leg: 4, dir: 0, sp, pas }); KB.result = null; KB.send('stage|' + stage); KB.startMatch() }, [sp, pas, stage])
  await p.waitForFunction(() => window.KB.events.some(e => e.k === 'go'), null, { timeout: 120000 })
  if (cpuoff) await send(p, 'cpuoff|1')
  await send(p, 'debugstate|1'); await send(p, 'timescale|' + ts)
  await p.evaluate(() => {
    window.__m = {}
    setInterval(() => {
      const s = window.KB.state; if (!s || !s.k) return
      for (const k of s.k) {
        const sp = Math.hypot(k.vx, k.vz)
        const m = window.__m[k.i] || (window.__m[k.i] = { vxmax: -1e9, vxmin: 1e9, spmax: 0, x0: k.wx, z0: k.wz, dmax: 0 })
        m.vxmax = Math.max(m.vxmax, k.vx); m.vxmin = Math.min(m.vxmin, k.vx); m.spmax = Math.max(m.spmax, sp); m.dmax = Math.max(m.dmax, Math.hypot(k.wx - m.x0, k.wz - m.z0))
      }
    }, 4)
  })
  o.monReset = () => p.evaluate(() => { window.__m = {} })
  o.mon = () => p.evaluate(() => window.__m)
  o.k = i => p.evaluate(i => window.KB.state.k.find(k => k.i === i), i)
  o.evs = kind => p.evaluate(kind => window.KB.events.filter(e => e.k === kind), kind)
  o.clear = () => p.evaluate(() => { window.KB.events.length = 0 })
  o.tm = () => p.evaluate(() => window.KB.state.tm)
  o.place = async (i, x, z, vx = 0, vz = 0, y = 2.5) => { await send(p, `tp|${x},${y},${z},${vx},0,${vz},${i}`) }
  o.shot = async name => { await p.screenshot({ path: `shots/${name}.png` }) }
  o.until = async (fn, ms = 20000, step = 40) => { const t0 = Date.now(); while (Date.now() - t0 < ms) { if (await p.evaluate(fn)) return true; await wait(step) } return false }
  return o
}
export const dist = (a, b) => Math.hypot(a.wx - b.wx, a.wz - b.wz)
