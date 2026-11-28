// ふつうのダッシュの進む距離（接線方向に出して、止まるまで）。node dashdist.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const res = {}
for (const mode of ['rest', 'run']) {
  const reps = []
  for (let rep = 0; rep < 3; rep++) {
    const o = await begin(url, { sp: 0 })
    await o.place(1, -14, 0, 0, 0, 1.0)
    await o.place(0, 10, -4, 0, 0, 1.0); await wait(1500)
    if (mode === 'run') { await send(o.p, 'inmv|0,1'); await wait(1500) }   // 走っている状態（+z へ）
    const a = await o.k(0)
    await send(o.p, 'inmv')
    await send(o.p, `dashat|0,${a.wx},${a.wz + 8}`)   // +z（接線）へ
    let peak = 0; const t0 = await o.tm()
    for (let i = 0; i < 40; i++) { await wait(100); const k = await o.k(0); peak = Math.max(peak, Math.hypot(k.vx, k.vz)); if ((await o.tm()) - t0 > 3.2) break }
    const b = await o.k(0)
    reps.push({ d: Math.hypot(b.wx - a.wx, b.wz - a.wz), peak, t: (await o.tm()) - t0 })
    await o.b.close()
  }
  res[mode] = { dist: +(reps.reduce((s, r) => s + r.d, 0) / reps.length).toFixed(1), each: reps.map(r => +r.d.toFixed(1)), peakSpeed: +(reps.reduce((s, r) => s + r.peak, 0) / reps.length).toFixed(1) }
}
console.log(JSON.stringify(res)); process.exit(0)
