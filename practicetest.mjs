// 練習場の検査（2026-11-28）: タイトルの「練習」→ 的3種・数字・チェックの印・リセット・タイトルへ・通常の試合に戻れる
// node practicetest.mjs [url] [WxH] [touch]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const [W, H] = (process.argv[3] || '1280x720').split('x').map(Number)
const mobile = process.argv[4] === 'touch'
const tag = `${W}x${H}${mobile ? 't' : ''}`
const { b, p, errs } = await open(url, { w: W, h: H, mobile })
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const shot = n => p.screenshot({ path: `shots/prac_${tag}_${n}.png` })
const click = async sel => { if (mobile) await p.tap(sel); else await p.click(sel) }
const st = () => p.evaluate(() => window.KB.state)
const evs = k => p.evaluate(k => window.KB.events.filter(e => e.k === k), k)
const done = () => p.evaluate(() => [...document.querySelectorAll('#prItems li.done')].map(l => l.dataset.id))
await p.evaluate(() => { try { localStorage.removeItem('koma-prac') } catch (e) {} })
await wait(800); await shot('1_title')
ok(await p.evaluate(() => !document.getElementById('btnPractice').disabled), 'タイトルに練習ボタン（有効）')
await click('#btnPractice')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 40000 })
await wait(600)
ok(await p.evaluate(() => KB.screen === 'hud' && !document.getElementById('prac').hidden), '練習の画面に入った')
ok(await p.evaluate(() => getComputedStyle(document.getElementById('hudTime')).display === 'none' || document.getElementById('hudTime').offsetParent === null), '時間表示なし')
await shot('2_start')
let s = await st(); ok(s.k.length === 2, `コマは自分とダミーの2体 (${s.k.length})`)
// 必殺がすぐたまる
await wait(3000); s = await st(); ok(s.k[0].g >= 1, `必殺ゲージが満タン g=${s.k[0].g}`)
// 的3種
const pos = async () => { const q = await st(); return [q.k[1].wx, q.k[1].wz] }
const dist = (a, c) => Math.hypot(a[0] - c[0], a[1] - c[1])
await send(p, 'cpuoff|0')
await send(p, 'tp|-12,6,0,0,0,0,0')
let a0 = await pos(); await wait(2500); let a1 = await pos()
ok(dist(a0, a1) < 0.8, `動かない的は動かない (${dist(a0, a1).toFixed(2)}m)`)
await p.evaluate(() => document.querySelector('#prDummy button[data-d="1"]').click())
a0 = await pos(); await wait(3000); a1 = await pos()
ok(dist(a0, a1) > 0.6, `回る的は動く (${dist(a0, a1).toFixed(2)}m)`)
await p.evaluate(() => document.querySelector('#prDummy button[data-d="2"]').click())
await send(p, 'tp|-6,6,0,0,0,0,0'); await wait(200)
const d0 = dist(await pos(), [(await st()).k[0].wx, (await st()).k[0].wz]); await wait(2500)
const q = await st(); const d1 = dist([q.k[1].wx, q.k[1].wz], [q.k[0].wx, q.k[0].wz])
ok(d1 < d0 || (await evs('phit')).length > 0, `体当たりの的は寄ってくる (${d0.toFixed(1)}→${d1.toFixed(1)}m)`)
await p.evaluate(() => document.querySelector('#prDummy button[data-d="0"]').click())
await send(p, 'cpuoff|1')
// 当てる（ダッシュ）
await send(p, 'pracreset'); await wait(600)
await p.evaluate(() => { KB.events.length = 0 })
{ const d = await st(); await send(p, `dashat|0,${d.k[1].wx},${d.k[1].wz}`) } await p.waitForFunction(() => KB.events.some(e => e.k === 'phit'), null, { timeout: 6000 }).catch(() => {})
let hits = await evs('phit'); ok(hits.length > 0 && hits[0].dd > 0 && hits[0].v > 0, `当てた数値が出る ${JSON.stringify(hits[0] || {})}`)
await p.waitForFunction(() => KB.events.some(e => e.k === 'pstat'), null, { timeout: 8000 }).catch(() => {}); await wait(200)
const stat = await p.evaluate(() => ['psD', 'psV', 'psR', 'psM'].map(i => document.getElementById(i).textContent))
ok(stat.every(x => x !== '-') && !stat[2].includes('…'), `画面の数字 ${stat.join(' / ')}`)
ok((await done()).includes('dash'), 'ダッシュで当てる に印')
await shot('3_hit')
// 空中から
await send(p, 'pracreset'); await wait(500)
{ const d = await st(); const dx = d.k[1].wx, dz = d.k[1].wz
  await send(p, `tp|${dx - 0.8},${(d.k[1].gh + 4).toFixed(2)},${dz},0,-14,0,0`) }
await wait(1500); ok((await done()).includes('air'), '空中から当てる に印')
// かち上げ
await send(p, 'pracreset'); await wait(500)
if (!mobile) {
  await send(p, 'cpuoff|1'); { const d = await st(); await send(p, `tp|${d.k[1].wx - 2.5},${(d.k[1].gh + 0.1).toFixed(2)},${d.k[1].wz},0,0,0,0`) }
  await wait(300); await send(p, 'leanw|1,0'); await send(p, 'inmv|1,0'); await wait(900); await send(p, 'inmv')
  ok((await done()).includes('upper'), 'かち上げ に印')
}
// 踏ん張り
await send(p, 'pracreset'); await wait(500); await send(p, 'touchmode|1'); await send(p, 'brace|1')
await p.evaluate(() => { KB.events.length = 0 })
{ const d = await st(); await send(p, `tp|${d.k[1].wx - 4},${(d.k[1].gh + 0.1).toFixed(2)},${d.k[1].wz},9,0,0,0`) }
await wait(1500); await send(p, 'brace|0'); if (!mobile) await send(p, 'touchmode|0')
ok((await done()).includes('brace'), '踏ん張りで耐える に印')
// 必殺
await send(p, 'pracreset'); await wait(500); await send(p, 'gauge|100'); await send(p, 'specialnear'); await wait(800)
ok((await done()).includes('sp'), '必殺を撃つ に印')
// 必殺をチェインへ切り替え → 撃つ
for (let i = 0; i < 9 && !(await p.evaluate(() => document.getElementById('prSp').textContent.includes('チェイン'))); i++) await click('#prSp')
ok(await p.evaluate(() => document.getElementById('prSp').textContent.includes('チェイン')), '必殺の切り替えボタン')
await wait(500); await send(p, 'pracreset'); await wait(500); await send(p, 'gauge|100'); await send(p, 'specialnear'); await wait(1000)
ok((await done()).includes('chain'), 'チェイン・バインド に印')
await shot('4_marks')
// 場外へ落とす（的を縁の外へ）
await send(p, 'pracreset'); await wait(500)
await p.evaluate(() => { KB.events.length = 0 })
await send(p, 'tp|30,6,0,0,0,0,1'); await wait(2200)
ok((await evs('ko')).some(e => e.i === 1), '的が落ちた'); ok((await done()).includes('out'), '場外へ落とす に印')
await wait(1500); s = await st(); ok(s.k[1].a === 1, '的が戻ってくる'); ok(s.ph === 'fight', 'まだ練習中（決着しない）')
// 自分が落ちても戻る・決着しない
await p.evaluate(() => { KB.events.length = 0 })
await send(p, 'tp|40,6,0,0,0,0,0'); await wait(3500); s = await st(); ok(s.k[0].a === 1 && s.ph === 'fight', `自分が落ちても戻る (a=${s.k[0].a} ph=${s.ph})`)
ok(!(await p.evaluate(() => KB.result)), '結果画面は出ない')
// 縁で踏ん張る
await send(p, 'pracreset'); await wait(500); await send(p, 'cpuoff|1')
await send(p, 'tp|23,8,0,12,0,0,0'); { const d = await st(); void d }
await wait(1200)
for (let g = 5.2; g < 9 && !(await done()).includes('cling'); g += 0.4) { await send(p, `tp|23.0,${g.toFixed(2)},0,12,0,0,0`); await wait(900) }
ok((await done()).includes('cling'), '縁で踏ん張る に印（縁で引っかかる）')
// 印の保存・消す
const saved = await p.evaluate(() => JSON.parse(localStorage.getItem('koma-prac') || '{}'))
ok(Object.keys(saved).length >= 7, `印を保存 ${Object.keys(saved).length}`)
await shot('5_after')
if (mobile) { await click('#prHead'); await wait(300); await shot('5b_list_open') }
await click('#prClear'); ok((await done()).length === 0, '印を消す')
await click('#prReset'); await wait(500)
const sr = await st(); ok(sr.k[0].a === 1 && sr.k[1].a === 1, 'リセットで戻る')
// タイトルへ → 通常の試合
await click('#prBack'); await wait(800)
ok(await p.evaluate(() => KB.screen === 'title'), 'タイトルへ戻る'); await shot('6_back_title')
await click('#btnPlay'); await wait(500); await click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 30000 })
await wait(500); s = await st(); ok(s.k.length >= 2, `通常の試合に入れる (コマ ${s.k.length})`)
ok(await p.evaluate(() => document.getElementById('prac').hidden), '通常の試合では練習の表示なし')
await send(p, 'timescale|8')
await p.waitForFunction(() => window.KB.result, null, { timeout: 120000 }).catch(() => {})
ok(await p.evaluate(() => !!window.KB.result), '通常の試合は決着する')
console.log('errs', errs); ok(errs.length === 0, 'コンソールエラー0')
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); await b.close(); process.exit(NG.length ? 1 : 0)
