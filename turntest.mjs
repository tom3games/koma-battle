// ターンテーブル（stage 2）: 床の流れ（外ほど強い・時計回り）・同じ向き/逆向きの回転の減り・画面（PC/スマホ）
// node turntest.mjs [url] [WxH] [touch]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const [W, H] = (process.argv[3] || '1280x720').split('x').map(Number), mobile = process.argv[4] === 'touch'
const tag = `${W}x${H}${mobile ? 't' : ''}`
const { b, p, errs } = await open(url, { w: W, h: H, mobile })
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
await p.click('#btnPlay'); await wait(400)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click(); KB.send('stage|2') })
await p.click('#btnFight')
await p.waitForFunction(() => window.KB.events.some(e => e.k === 'stage'), null, { timeout: 30000 })
await wait(300)
ok(await p.evaluate(() => document.getElementById('stageName').textContent === 'ターンテーブル'), 'ステージ名の表示')
await p.screenshot({ path: `shots/tt_${tag}_1_name.png` })
await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 30000 }); await wait(500)
await send(p, 'cpuoff|1'); await send(p, 'debugstate|1')
const k = i => p.evaluate(i => window.KB.state.k[i], i)
async function drift(x, z, sec) {
  await send(p, `tp|${x},6,${z},0,0,0,0`); await wait(1200)
  const a = await k(0); await wait(sec * 1000); const c = await k(0)
  // 時計回り（上から見て）= 角度 atan2(z, x) が減る向き
  const a0 = Math.atan2(a.wz, a.wx), a1 = Math.atan2(c.wz, c.wx); let d = a1 - a0; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI
  return { d, r0: Math.hypot(a.wx, a.wz), r1: Math.hypot(c.wx, c.wz), arc: -d * Math.hypot(c.wx, c.wz) }
}
const near = await drift(3, 0, 3), far = await drift(15, 0, 3)
console.log('中心付近', JSON.stringify(near), '外', JSON.stringify(far))
ok(far.d < -0.05, `外では時計回りに流される (角度 ${far.d.toFixed(2)}rad)`)
ok(near.d > 0.05, `内側は反時計回り (角度 ${near.d.toFixed(2)}rad)`)
// 回転の減り: 右回り vs 左回り
await send(p, 'tp|0,6,0,0,0,0,0'); await wait(800)
const sp0 = (await k(0)).sp; await wait(4000); const sp1 = (await k(0)).sp
console.log('右回り 4秒の回転の減り', (sp0 - sp1).toFixed(3))
await p.screenshot({ path: `shots/tt_${tag}_2_fight.png` })
await send(p, 'tp|17,6,0,0,0,0,0'); await wait(1500); await p.screenshot({ path: `shots/tt_${tag}_3_rim.png` })
await send(p, 'tp|-12,6,3,0,0,0,0'); await wait(2500); await p.screenshot({ path: `shots/tt_${tag}_4_later.png` })
console.log('errs', errs); ok(errs.length === 0, 'コンソールエラー0')
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); await b.close(); process.exit(NG.length ? 1 : 0)
