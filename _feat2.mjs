import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open('http://localhost:5392/', { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1'); await wait(3500)
const me = async () => (await state(p)).k[0]
await send(p, 'fightt|41'); await wait(300)
const s0 = await me(); const a = 1.2, rr = 20.6
await send(p, 'tp|' + [Math.cos(a) * rr, s0.gh + 0.05, Math.sin(a) * rr, 0, 0, 0].map(v => v.toFixed(2)).join(','))
await wait(700); await p.screenshot({ path: 'shots/_board.png' })
await send(p, 'tp|' + [0, 3, 0, 0, 0, 0].join(',')); await wait(1500)
await send(p, 'spin|1,0'); await wait(2800)
await p.keyboard.down('KeyW'); await wait(900); await p.screenshot({ path: 'shots/_fire.png' }); await wait(700); await p.screenshot({ path: 'shots/_fire2.png' })
await p.keyboard.up('KeyW'); console.log(errs); await b.close()
