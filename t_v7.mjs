// v7 の確認: 左右ボタン・追う視点・へこみ・バネ/加速板なし
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5444/'
const mobile = process.argv[3] === 'm'
const { b, p, errs, loadMs } = await open(url, mobile ? { w: 390, h: 844, mobile: true } : {})
console.log('loadMs', loadMs)
await wait(800)
await p.click('#btnPlay'); await wait(800)
// 左右を速く5回押す → 5つ進むか
const before = await p.$eval('.slot .slot-no', e => e.textContent)
const arw = await p.$$('.slot .arw[data-d="1"]')
for (let i = 0; i < 3; i++) { const bb = await (await p.$$(".slot .arw[data-d=\"1\"]"))[0].boundingBox(); if (mobile) await p.touchscreen.tap(bb.x + bb.width / 2, bb.y + bb.height / 2); else await p.mouse.click(bb.x + bb.width / 2, bb.y + bb.height / 2); await wait(60) }
const after = await p.$eval('.slot .slot-no', e => e.textContent)
console.log('slot', before, '->', after)
await wait(500)
await p.screenshot({ path: `shots/v7_${mobile?'m':'pc'}_select.png` })
await p.click('#btnFight'); await wait(1500)
await p.screenshot({ path: `shots/v7_${mobile?'m':'pc'}_count.png` })
await wait(2500)
if (!mobile) { await p.keyboard.down('KeyW'); await wait(1500); await p.screenshot({ path: 'shots/v7_pc_w.png' }); await p.keyboard.down('KeyD'); await wait(1500); await p.keyboard.up('KeyD'); await p.keyboard.up('KeyW'); await p.screenshot({ path: 'shots/v7_pc_wd.png' }) }
await send(p, 'auto|1'); await send(p, 'debugstate|1')
const pos = []
for (let i = 0; i < 12; i++) { await wait(1500); const s = await state(p); if (s) pos.push(s.k.map(k => k.x !== undefined ? [Math.round(k.x), Math.round(k.z)].join(',') : '?').join(' ')); if (i === 3 || i === 8) await p.screenshot({ path: `shots/v7_${mobile?'m':'pc'}_auto${i}.png` }) }
console.log(pos.join('\n'))
const ev = await p.evaluate(() => { const c = {}; for (const e of window.KB.events || []) c[e.k] = (c[e.k] || 0) + 1; return c })
console.log('ev', JSON.stringify(ev))
console.log('errs', errs)
await b.close()
