import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 0 })
await o.place(0, -10, 0, 0, 0, 0.3); await o.place(1, -16, 0, 0, 0, 0.3); await wait(1500)
await o.clear()
await o.place(1, -11.5, 0, 13, 0, 0.3)
await send(o.p, 'inmv|1,0')
for (let i=0;i<8;i++){ await wait(150); const a=await o.k(0), b=await o.k(1); console.log(i, a.wx.toFixed(2), a.vx.toFixed(1), '|', b.wx.toFixed(2), b.vx.toFixed(1), b.h.toFixed(2), a.a, b.a) }
console.log(JSON.stringify(await o.p.evaluate(() => window.KB.events.map(e => e.k + ':' + e.i + (e.ka !== undefined ? '/ka' + e.ka + 'kb' + e.kb + ' a' + e.a + 'b' + e.b + ' vn' + e.vn : '')))))
process.exit(0)
