import { open, wait } from './lib.mjs'
const { b, p } = await open('http://localhost:5393/?lv=10', { w: 390, h: 844, mobile: true })
await p.evaluate(() => { try { localStorage.removeItem('koma-hint') } catch (e) {} })
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => { KB.send('stage|0'); KB.startMatch() })
await p.waitForFunction(() => KB.events.some(e => e.k === 'go'), null, { timeout: 40000 }); await wait(1200)
await p.screenshot({ path: 'shots/mobile_hint.png' })
const r = await p.evaluate(() => { const e = document.querySelector('.h-r b'); return e ? e.getBoundingClientRect().height + '/' + getComputedStyle(e).lineHeight : null })
console.log('hint b', r); process.exit(0)
