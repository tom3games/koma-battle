// 倒れたコマの当たり判定: 回転0で倒れたコマにぶつかると押し動かされ、ダメージは付かない
import { begin, wait, send } from './sp2lib.mjs'
const o = await begin(process.argv[2] || 'http://localhost:5393/', { sp: 0, n: 2, w: 960, h: 540, stage: 0 })
await send(o.p, 'cpuoff|1')
await o.place(0, -8, 0, 0, 0, 1); await o.place(1, 0, 0, 0, 0, 1); await o.place(2, 30, 30, 0, 0, 1); await wait(1200)
await send(o.p, 'spin|0.0001,1'); await wait(1500)
const down = await o.k(1); console.log('倒れた a=', down.a)
const before = await o.k(0), p1 = [down.wx, down.wz]
await o.place(0, down.wx - 3, down.wz, 0, 0, 0.3); await wait(300); await o.shot('downbump_a')
await o.place(0, down.wx - 2.2, down.wz, 12, 0, 0.3); await wait(250); await o.shot('downbump_b'); await wait(900)
const a = await o.k(1); const me = await o.k(0)
const moved = Math.hypot(a.wx - p1[0], a.wz - p1[1])
console.log('倒れたコマが押された距離', moved.toFixed(2), '自分の速さ', Math.hypot(me.vx, me.vz).toFixed(1), '自分の回転', me.sp.toFixed(3))
const ok = (c, m) => console.log((c ? 'OK ' : 'NG ') + m)
ok(moved > 0.4, '倒れたコマが押し動かされる'); ok(me.a === 1 && me.sp > 0.9, 'ダメージは付かない（自分は無傷）')
ok(a.a === 0, '倒れたコマはそのまま（勝敗に影響しない）'); ok(!(await o.evs('hit')).some(e => e.a === 1 || e.b === 1), '当たりのダメージイベントが出ない')
console.log('errs', o.errs); await o.b.close()
