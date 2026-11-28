import { begin, wait, send } from './sp2lib.mjs'
const o = await begin('http://localhost:5393/', { sp: 0, n: 3, cpuoff: false })
await wait(2000)
await o.place(0, 40, 0, 0, 0, 3)
for (let i = 0; i < 30; i++) { await wait(150); const s = await o.p.evaluate(() => ({ ts: KB.state.ts, a: KB.state.k[0].a, sw: KB.state.sw, ph: KB.state.ph, tm: KB.state.tm })); console.log(i, JSON.stringify(s)) }
process.exit(0)
