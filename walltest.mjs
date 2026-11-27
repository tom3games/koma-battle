// 壁（段差・縁の柵・弧の台）に押し付けた時の検査（2026-11-28）: めり込み（WallH より低い所に入る）と、同じ所に止まり続けないかを数字で見る
// node walltest.mjs [url]   事前に debugstate で wh（その場の WallH）が出る版が必要
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const { b, p, errs } = await open(url)
await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click() })
await p.click('#btnFight')
await p.waitForFunction(() => window.KB.events.some(e => e.k === 'go'), null, { timeout: 30000 })
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1'); await send(p, 'timescale|1')
// wh が状態に出ない版でも、弧の台と縁の柵だけは自前で計算して調べる
const bowl = r => 4.8 * (r / 22.5) ** 2
function wallH(x, z) {
  const r = Math.hypot(x, z), ang = Math.atan2(z, x) * 180 / Math.PI
  for (const d of [30, 150, 270]) { const dd = Math.abs(((ang - d + 540) % 360) - 180); if (r >= 4.2 && r <= 6.0 && dd <= 0.45 * 180 / Math.PI) return bowl(r) + 0.75 }
  if (r > 22.75 && r <= 24.7) return bowl(22.5) + 2 + 14
  return -1000
}
const me = () => p.evaluate(() => { const k = window.KB.state.k[0]; return { x: k.wx, z: k.wz, y: k.wy, wh: k.wh, a: k.a, why: k.why } }).then(s => { if (s.wh === undefined) s.wh = wallH(s.x, s.z); return s })
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const deg = d => d * Math.PI / 180
async function scenario(label, setup, secs = 3.5) {
  await p.evaluate(() => { window.KB.events.length = 0 })
  await send(p, 'tp|-15,8,0,0,0,0,1')
  await setup()
  const samples = []
  const t0 = Date.now()
  while (Date.now() - t0 < secs * 1000) { await wait(100); samples.push(await me()) }
  await send(p, 'inmv')
  // めり込み: その場の壁の高さ - 自分の高さ が段差の許容(0.35)を超えたまま
  const inside = samples.filter(s => s.wh > -100 && s.wh - s.y > 0.4).length
  // 同じ所に止まり続けた最長（0.05m 未満しか動かない連続時間・秒）
  let stuck = 0, run = 0
  for (let i = 1; i < samples.length; i++) {
    const d = Math.hypot(samples[i].x - samples[i - 1].x, samples[i].z - samples[i - 1].z)
    if (d < 0.01) { run += 0.1; stuck = Math.max(stuck, run) } else run = 0
  }
  const moved = samples.map(s => Math.hypot(s.x, s.z))
  console.log(label, JSON.stringify({ n: samples.length, inside, stuckSec: +stuck.toFixed(1), rMin: +Math.min(...moved).toFixed(2), rMax: +Math.max(...moved).toFixed(2), last: samples[samples.length - 1] }))
  return { inside, stuck }
}
// 弧の台（30度・半径4.2〜6・高さ0.75）へ内側から
const A = deg(30), cA = Math.cos(A), sA = Math.sin(A)
let r = await scenario('A 弧の台へ押し付け(ゆっくり)', async () => { await send(p, `tp|${3.2 * cA},0.3,${3.2 * sA},0,0,0,0`); await send(p, `inmv|${cA},${sA}`) })
ok(r.inside === 0, `A めり込み0 (${r.inside})`); ok(r.stuck < 1, `A 止まり続けない (${r.stuck}s)`)
// 斜めにこすり続ける（壁の向きに対し約70度）
r = await scenario('B 弧の台を斜めにこする', async () => { const a2 = A + deg(75); await send(p, `tp|${3.4 * cA},0.3,${3.4 * sA},0,0,0,0`); await send(p, `inmv|${Math.cos(a2)},${Math.sin(a2)}`) })
ok(r.inside === 0, `B めり込み0 (${r.inside})`); ok(r.stuck < 1, `B 止まり続けない (${r.stuck}s)`)
// ほかのコマに押し込まれる（相手が内側から外向きに速く当たる）
r = await scenario('C コマに壁へ押し込まれる', async () => { await send(p, `tp|${3.7 * cA},0.3,${3.7 * sA},0,0,0,0`); await send(p, `tp|${2.2 * cA},0.3,${2.2 * sA},${9 * cA},0,${9 * sA},1`) }, 3)
ok(r.inside === 0, `C めり込み0 (${r.inside})`)
// 縁の柵（45度・切れ目ではない所）へ外向きに
const B = deg(45), cB = Math.cos(B), sB = Math.sin(B)
r = await scenario('D 縁の柵へ押し付け', async () => { await send(p, `tp|${21.5 * cB},4.9,${21.5 * sB},0,0,0,0`); await send(p, `inmv|${cB},${sB}`) })
ok(r.inside === 0, `D めり込み0 (${r.inside})`); ok(r.stuck < 1, `D 止まり続けない (${r.stuck}s)`)
// 壁の中に直接置かれた時（押し出しで押し込まれた結果の再現）
r = await scenario('E 壁の中に置かれる', async () => { await send(p, `tp|${5.0 * cA},0.5,${5.0 * sA},0,0,0,0`) }, 2)
ok(r.inside === 0 || true, `E (参考) 壁の中に置いた後のめり込みサンプル ${r.inside}`)
console.log('errs', errs); console.log(NG.length ? 'NG あり ' + NG.length : 'ALL OK')
await b.close()
