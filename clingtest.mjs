// 場外ギリギリの踏ん張り（2026-11-27）: 縁の切れ目から外へ押し出して、①ダッシュで戻る ②何もしないと落ちる ③スペースでも戻る を確かめる
// node clingtest.mjs [url]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const { b, p, errs } = await open(url)
await p.click('#btnPlay'); await wait(500)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click() })
await p.evaluate(() => KB.send('stage|0')); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.events.some(e => e.k === 'go'), null, { timeout: 30000 })
await send(p, 'cpuoff|1')
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const evs = () => p.evaluate(() => window.KB.events.filter(e => ['cling', 'save', 'clingfall', 'ko'].includes(e.k) && e.i === 0).map(e => e.k + (e.why ? ':' + [e.why, e.r, e.vy, e.vh].join('/') : '')))
const alive = () => p.evaluate(() => { const s = window.KB.state; return s && s.k && s.k[0] ? s.k[0] : null })
async function push(label) {
  await p.evaluate(() => { window.KB.events.length = 0 })
  // CPU は遠くへ。自分は切れ目（0度）の縁の内側から外向きに滑らせる
  await send(p, 'tp|-15,8,0,0,0,0,1')
  await send(p, 'debugstate|1'); await send(p, 'tp|23.0,5.35,0,12,0,0,0')
  for (let i = 0; i < 20; i++) { if ((await evs()).includes('cling')) break; await wait(50) }
  return await evs()
}
// ① ダッシュで戻る
let e = await push('dash')
ok(e.includes('cling'), `縁に引っかかる ${e}`)
await p.screenshot({ path: 'shots/cling.png' })
await send(p, 'dash'); await wait(1200)
e = await evs(); ok(e.includes('save') && !e.some(x => x.startsWith('ko')), `ダッシュで戻る ${e}`)
await p.waitForFunction(() => window.KB.state.k[0].cc <= 0, null, { timeout: 60000 })   // 待ち（ゲームの中の6秒）明け
 console.log('before2', JSON.stringify(await alive()).slice(0, 200))
// ② 何もしない
e = await push('none')
ok(e.includes('cling'), `2回目も引っかかる ${e}`)
await p.waitForFunction(() => window.KB.events.some(e => e.k === 'ko' && e.i === 0), null, { timeout: 30000 }).catch(() => {})
e = await evs(); ok(e.includes('clingfall') && e.some(x => x.startsWith('ko')), `何もしないと落ちる ${e}`)
// ③ スペースでも戻る ④ 踏ん張り中に当てられたら落ちる（とどめ）
for (const mode of ['jump', 'hit']) {
  await p.evaluate(() => { const b = document.getElementById('btnAgain'); if (b) b.click() }).catch(() => {})
  await p.waitForFunction(() => window.KB.state && window.KB.state.k && window.KB.state.k[0].a === 1 && window.KB.events.some(e => e.k === 'go'), null, { timeout: 60000 }).catch(() => {})
  await wait(1500); await send(p, 'cpuoff|1')
  await p.waitForFunction(() => window.KB.state.k[0].cc <= 0, null, { timeout: 60000 })
  e = await push(mode); ok(e.includes('cling'), `${mode}: 引っかかる ${e}`)
  if (mode === 'jump') { await send(p, 'jump'); await wait(1200); e = await evs(); ok(e.includes('save'), `スペースで戻る ${e}`) }
  else { await send(p, 'tp|24.0,6.0,0,-6,0,0,1'); await p.waitForFunction(() => window.KB.events.some(e => e.k === 'ko' && e.i === 0), null, { timeout: 30000 }).catch(() => {}); e = await evs(); ok(e.includes('clingfall') && !e.includes('save'), `当てられて落ちる ${e}`) }
}
console.log('errs', errs); ok(errs.length === 0, 'エラー無し')
await b.close(); console.log(NG.length ? `NG ${NG.length}件` : 'NG 0件')
