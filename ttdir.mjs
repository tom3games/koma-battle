import { open, wait, send } from './lib.mjs'
const { b, p } = await open('http://localhost:5393/', { w: 960, h: 540 })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click(); KB.send('stage|2') }); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 40000 })
await send(p, 'cpuoff|1')
for (const [n, x, z] of [['inner', 0, -6.5], ['outer', 0, -14]]) {
  await send(p, `tp|${x},4,${z},0,0,0,0`); await wait(2200)
  await p.screenshot({ path: `shots/ttdir_${n}_a.png` }); await wait(700); await p.screenshot({ path: `shots/ttdir_${n}_b.png` })
}
await b.close()
