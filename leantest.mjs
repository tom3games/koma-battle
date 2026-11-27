// WASD 2回押しの傾けと、かち上げ（2026-11-27）
// node leantest.mjs [url]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const { b, p, errs } = await open(url)
await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click() })
await p.click('#btnFight')
await p.waitForFunction(() => window.KB.events.some(e => e.k === 'go'), null, { timeout: 30000 })
await wait(1200); await send(p, 'cpuoff|1'); await send(p, 'debugstate|1')
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const S = () => p.evaluate(() => window.KB.state.k)
const evs = k => p.evaluate(k => window.KB.events.filter(e => e.k === k).length, k)
// ① 本物のキーで D を2回 → 傾く
await p.mouse.click(640, 360)   // 画面に入力を向ける（左クリック＝ダッシュも出る）
await wait(1600)
let n0 = await evs('lean')
await p.keyboard.press('KeyD'); await wait(90); await p.keyboard.press('KeyD'); await wait(250)
let k = await S()
ok((await evs('lean')) === n0 + 1 && k[0].tl > 15, `D を2回で傾く（傾き ${k[0].tl}度）`)
await p.screenshot({ path: 'shots/lean.png' })
await wait(1500)
// ② 1回だけ・間をあけた2回は傾かない
n0 = await evs('lean')
await p.keyboard.press('KeyA'); await wait(600); await p.keyboard.press('KeyA'); await wait(300)
ok((await evs('lean')) === n0, '間があいた2回押しは傾かない')
// ③ かち上げ: 傾けて相手へ当てる／傾けずに当てる を比べる
async function bump(lean) {
  await send(p, 'tp|-3,4,0,0,0,0,0'); await send(p, 'tp|1,4,0,0,0,0,1'); await wait(1200)
  const k0 = await S(); const g0 = k0[0].gh, g1 = k0[1].gh
  await send(p, `tp|-3,${(g0 + 0.05).toFixed(2)},0,0,0,0,0`); await send(p, `tp|1,${(g1 + 0.05).toFixed(2)},0,0,0,0,1`); await wait(200)
  const u0 = await evs('upper')
  if (lean) await send(p, 'leanw|1,0')
  await send(p, `tp|-1.4,${(g0 + 0.05).toFixed(2)},0,11,0,0,0`)
  let maxY = 0
  for (let i = 0; i < 20; i++) { const s = await S(); maxY = Math.max(maxY, s[1].wy - g1); await wait(40) }
  return { upper: (await evs('upper')) - u0, maxY: +maxY.toFixed(2) }
}
const plain = await bump(false); await wait(1500)
const up = await bump(true)
console.log('plain', JSON.stringify(plain), 'lean', JSON.stringify(up))
ok(plain.upper === 0 && up.upper >= 1, '傾けた時だけ かち上げ が出る')
ok(up.maxY > plain.maxY + 0.8, `相手が高く浮く（${plain.maxY} → ${up.maxY}）`)
console.log('errs', errs); ok(errs.length === 0, 'エラー無し')
await b.close(); console.log(NG.length ? `NG ${NG.length}件` : 'NG 0件')
