// 実画面を場面ごとに撮る（本物のキーとマウスで操作する）
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const W = 1280, H = 720
const { b, p, errs, loadMs } = await open(url, { w: W, h: H })
console.log('loadMs', loadMs)
const shot = async n => { await p.screenshot({ path: `shots/${n}.png` }); console.log('shot', n) }
const scr = k => ({ x: k.x * W, y: (1 - k.y) * H })
const me = async () => (await state(p)).k.find(k => k.i === 0)
const foe = async () => (await state(p)).k.find(k => k.i !== 0 && k.a)

await wait(1200); await shot('01_title')
await p.click('#btnPlay'); await wait(1500); await shot('02_select')
await p.click('.part[data-slot="top"][data-i="2"]'); await p.click('.part[data-slot="core"][data-i="1"]'); await p.click('.part[data-slot="leg"][data-i="2"]')
await wait(1200); await shot('03_select_hane_bane')
await p.click('.part[data-slot="top"][data-i="0"]'); await p.click('.part[data-slot="core"][data-i="0"]'); await p.click('.part[data-slot="leg"][data-i="2"]')
await p.click('#vsPick button[data-n="3"]')
await p.click('#btnFight'); await wait(1300); await shot('04_countdown')
await wait(2600); await shot('05_go')
await wait(1500)
// ダッシュ: 相手にマウスを合わせて左クリック
let f = await foe(); let s = scr(f)
await p.mouse.move(s.x, s.y); await wait(250)
await shot('06_aim')
await p.mouse.down(); await p.mouse.up(); await wait(140); await shot('07_dash')
await wait(160); await shot('07b_dash')
// ジャンプ台: 跳ね台の上に置いて跳ぶ
await send(p, 'tp|6.6,1.5,0,0,0,0'); for (let i = 0; i < 4; i++) { await wait(200); await shot('08_jump_' + i) }
// 空中から急降下: 相手の頭上に置いて、相手をクリック。上からの一撃が当たった瞬間を撮る
let got = false
for (let tries = 0; tries < 10 && !got; tries++) {
  f = await foe()
  await p.evaluate(() => { window.__airHit = 0; const o = window.KB.onUnity; if (!window.__wrapped) { window.__wrapped = 1; window.KB.onUnity = s => { o(s); if (s.includes('"k":"hit"') && s.includes('"air":1') && s.includes('"a":0,')) window.__airHit = performance.now() } } })
  await send(p, `tp|${f.wx - 1.2},${f.h + 3.2},${f.wz - 0.8},0,0,0`); await wait(50)
  f = await foe(); s = scr(f); await p.mouse.move(s.x, s.y)
  await p.mouse.down(); await p.mouse.up()
  await wait(90); await shot('09_dive_' + tries)
  try { await p.waitForFunction(() => window.__airHit > 0, null, { timeout: 1500 }); await wait(60); await shot('09_aerial_hit'); got = true } catch (e) {}
  await wait(900)
}
console.log('aerial hit captured', got)
// 必殺: ゲージを満タンにして、相手に右クリック
await send(p, 'gauge|100'); await wait(300); await shot('10_gauge_full')
f = await foe(); s = scr(f); await p.mouse.move(s.x, s.y)
await p.mouse.down({ button: 'right' }); await p.mouse.up({ button: 'right' })
for (let i = 0; i < 6; i++) { await wait(150); await shot('11_special_' + i) }
await wait(1500)
// 勝ち: 相手の回転を尽きさせる
const st = await state(p)
for (const k of st.k) if (k.i !== 0 && k.a) await send(p, `spin|0.001,${k.i}`)
await wait(800); await shot('12_ko')
await p.waitForFunction(() => window.KB.result, null, { timeout: 30000 }); await wait(600); await shot('13_win')
// 負け: もう一回 → 自分の回転を尽きさせる
await p.click('#btnAgain'); await wait(5000)
await send(p, 'spin|0.001,0'); await wait(900); await shot('14_down')
await p.waitForFunction(() => window.KB.result && window.KB.result.win === 0, null, { timeout: 30000 }); await wait(600); await shot('15_lose')
console.log('errs', JSON.stringify(errs))
await b.close()
