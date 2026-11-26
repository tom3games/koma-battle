// 狙い補助の効果を数字で見る（人の速さの bot の模型。C# の AssistDir と同じ計算）
// bot: 相手の「0.25秒前の位置」を、±10〜20度の誤差で狙ってダッシュ。ダッシュは 19m/s・0.32秒（約6.1m）。当たり = 相手の中心が線から 1.7m 以内
const A = 18, RANGE = 9, RATE = 0.5
const rnd = (a, b) => a + Math.random() * (b - a)
const deg = r => r * 180 / Math.PI
function assist(dir, opp) {
  let best = A + 1, bt = null
  for (const o of opp) {
    const d = Math.hypot(o.x, o.z); if (d > RANGE || d < 0.3) continue
    const ang = deg(Math.acos(Math.max(-1, Math.min(1, (dir.x * o.x + dir.z * o.z) / d))))
    if (ang < best) { best = ang; bt = { x: o.x / d, z: o.z / d } }
  }
  if (best > A) return { dir, bend: 0 }
  // Slerp(h, bt, RATE)
  const a0 = Math.atan2(dir.z, dir.x), a1 = Math.atan2(bt.z, bt.x)
  let da = a1 - a0; while (da > Math.PI) da -= 2 * Math.PI; while (da < -Math.PI) da += 2 * Math.PI
  const a = a0 + da * RATE
  return { dir: { x: Math.cos(a), z: Math.sin(a) }, bend: Math.abs(deg(da * RATE)) }
}
function trial(useAssist) {
  const opp = []
  const n = 1 + (Math.random() < 0.5 ? 1 : 0)
  for (let i = 0; i < n; i++) { const r = rnd(3, 9), t = rnd(0, 2 * Math.PI), s = rnd(0, 4), vt = rnd(0, 2 * Math.PI); opp.push({ x: Math.cos(t) * r, z: Math.sin(t) * r, vx: Math.cos(vt) * s, vz: Math.sin(vt) * s }) }
  // 狙う相手 = 最初の相手。反応の遅れ 0.25秒 → 古い位置を狙う
  const tgt = opp[0]
  const old = { x: tgt.x - tgt.vx * 0.25, z: tgt.z - tgt.vz * 0.25 }
  const e = (Math.random() < 0.5 ? -1 : 1) * rnd(10, 20) * Math.PI / 180    // 誤差は ±10〜20度
  const a0 = Math.atan2(old.z, old.x) + e
  let dir = { x: Math.cos(a0), z: Math.sin(a0) }
  let bend = 0
  if (useAssist) { const r = assist(dir, opp); dir = r.dir; bend = r.bend }
  // 当たり判定: 0.32秒の間、相手も等速で動く
  let hit = false
  for (let t = 0; t <= 0.32; t += 0.01) {
    for (const o of opp) { const ox = o.x + o.vx * t, oz = o.z + o.vz * t; if (Math.hypot(ox - dir.x * 19 * t, oz - dir.z * 19 * t) <= 1.7) hit = true }
  }
  return { hit, bend }
}
function run(N, assistOn) { let h = 0, mb = 0; for (let i = 0; i < N; i++) { const r = trial(assistOn); if (r.hit) h++; mb = Math.max(mb, r.bend) } return { rate: h / N * 100, maxBend: mb } }
const N = 200000
const off = run(N, false), on = run(N, true)
console.log(`N=${N}  補助なし ${off.rate.toFixed(1)}%  補助あり ${on.rate.toFixed(1)}%  差 +${(on.rate - off.rate).toFixed(1)}pt  最大の曲がり ${on.maxBend.toFixed(2)}度`)
// 100回×20セットのばらつき
const diffs = []; for (let k = 0; k < 20; k++) diffs.push(run(100, true).rate - run(100, false).rate)
console.log('100回ずつの差(pt) 平均', (diffs.reduce((a, b) => a + b) / 20).toFixed(1), '最小', Math.min(...diffs).toFixed(0), '最大', Math.max(...diffs).toFixed(0))
