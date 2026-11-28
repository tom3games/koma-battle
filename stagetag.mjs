import { open, wait } from './lib.mjs'
const dev = process.argv[2] || 'pc', size = dev === 'pc' ? { w: 1280, h: 720 } : { w: 390, h: 844, mobile: true }
const { b, p } = await open('http://localhost:5393/?lv=10', size)
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => { KB.send('stage|1'); KB.startMatch() })
await p.waitForFunction(() => KB.events.some(e => e.k === 'stage'), null, { timeout: 30000 }); await wait(700)
await p.screenshot({ path: `shots/${dev}_hill_tag.png` })
await p.waitForFunction(() => KB.events.some(e => e.k === 'count'), null, { timeout: 30000 }); await wait(300); await p.screenshot({ path: `shots/${dev}_hill_count.png` })
await p.waitForFunction(() => KB.events.some(e => e.k === 'go'), null, { timeout: 30000 }); await wait(1500)
await p.screenshot({ path: `shots/${dev}_hill_play.png` })
process.exit(0)
