import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open('http://localhost:5392/', { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1'); await wait(3500)
const me = async () => (await state(p)).k
await send(p, 'fightt|12'); await send(p, 'spin|1,0')
await send(p, 'tp|-6,3,-10,0,0,0'); await send(p, 'spin|1,0')
await wait(2600)
await p.keyboard.down('KeyD'); 
for (let i = 0; i < 4; i++) { await wait(500); await p.screenshot({ path: `shots/_fire${i}.png` }) }
// victim: place cpu in fire path
const ev = await p.evaluate(() => window.KB.events.filter(e => e.k === 'miracle').length)
console.log('miracle', ev)
await p.keyboard.up('KeyD'); console.log(errs); await b.close()
