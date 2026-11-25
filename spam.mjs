// 左クリックの連打で、ダッシュと急降下が待ち時間（1.3秒）より短い間隔で出ないか。node spam.mjs [url]
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5394/'
const { b, p, errs } = await open(url)
await p.click('#btnPlay'); await wait(300)
await p.click('#vsPick button[data-n="1"]'); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight' && window.KB.state.tm > 1.5, null, { timeout: 60000 })
await send(p, 'cpuoff|1')
await p.evaluate(() => { window.KB.events.length = 0 })
const g0 = (await state(p)).tm
await p.mouse.move(640, 300)
for (let i = 0; i < 160; i++) { await p.mouse.down(); await wait(20); await p.mouse.up(); await wait(30) }
const g1 = (await state(p)).tm
const ev = await p.evaluate(() => window.KB.events.filter(e => e.i === 0 && (e.k === 'dash' || e.k === 'dive')).map(e => e.k))
const sec = g1 - g0
console.log(JSON.stringify({ gameSec: +sec.toFixed(1), clicks: 160, dashes: ev.length, kinds: ev.join(','), max: Math.floor(sec / 1.3) + 1, ok: ev.length > 0 && ev.length <= Math.floor(sec / 1.3) + 1, errs }))
await b.close()
