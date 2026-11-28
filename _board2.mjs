import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const { b, p, errs } = await open(url, { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1'); await send(p, 'fightt|41'); await wait(400)
const me = async () => (await state(p)).k[0]
const res = {}
for (const [nm, mv] of [['none', [0, 0]], ['up', [0, 1]], ['down', [0, -1]], ['left', [-1, 0]], ['right', [1, 0]]]) {
  const s0 = await me(); const a = Math.PI / 3, rr = 20.6
  await send(p, 'move|0,0'); await send(p, 'tp|' + [Math.cos(a) * rr, s0.gh + 0.05, Math.sin(a) * rr, 0, 0, 0].map(v => v.toFixed(2)).join(','))
  await wait(700); await send(p, 'move|' + mv.join(','))
  let minR = 99, maxR = 0, a0 = null, a1 = 0
  for (let i = 0; i < 12; i++) { const k = await me(); minR = Math.min(minR, k.r); maxR = Math.max(maxR, k.r); const ang = Math.atan2(k.wz, k.wx); if (a0 === null) a0 = ang; a1 = ang; if (nm === 'none' && i >= 3 && i <= 5) { await send(p, 'camfreeze|1'); await p.screenshot({ path: `shots/_board2_${i}.png` }) }; await wait(120) }
  res[nm] = { minR: +minR.toFixed(1), maxR: +maxR.toFixed(1), sweepDeg: Math.round((a1 - a0) * 180 / Math.PI), alive: (await me()).alive }
}
await send(p, 'move|0,0')
console.log(JSON.stringify(res), 'errs', errs.slice(0, 3)); await b.close()
