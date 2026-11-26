// 着地・自分/相手の必殺・左回転・結果のレベルアップを実画面で撮る。node gameshots.mjs [url]
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const W = wait
const fight = async p => { await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 30000 }); await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 }) }
for (const [w, h, tag, mob] of [[1280, 720, 'pc', false], [390, 844, 'ph', true]]) {
  const { b, p, errs } = await open(url + '?lv=7', { w, h, mobile: mob })
  await p.evaluate(() => localStorage.setItem('koma-pick2', JSON.stringify({ top: 0, core: 0, leg: 0, dir: 1, sp: 0 })))
  await p.reload(); await p.waitForFunction(() => window.__kbReady === true, null, { timeout: 120000 })
  await p.click('#btnPlay'); await W(300)
  await p.click('#nav button[data-pg="gear"]'); await W(900); await p.screenshot({ path: `shots/g/${tag}_leftspin_gear.png` })
  await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click())
  await p.click('#nav button[data-pg="home"]'); await p.click('#btnFight')
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
  await p.evaluate(() => { window.__sp = []; window.__ban = []; new MutationObserver(() => window.__ban.push(document.getElementById('banner').classList.contains('on') + ':' + document.querySelector('#banner span').textContent)).observe(document.getElementById('banner'), { attributes: true, attributeFilter: ['class'] }) })
  await send(p, 'cpuoff|1')
  // 着地の連写
  const s0 = (await state(p)).k[0]
  await send(p, 'tp|' + [s0.wx, s0.gh + 14, s0.wz, 0, 0, 0].join(','))
  for (let i = 0; i < 6; i++) { await W(i < 3 ? 250 : 120); await p.screenshot({ path: `shots/g/${tag}_land_${i}.png` }) }
  await W(2500)
  // 自分の必殺 4種
  for (let sp = 0; sp < 4; sp++) {
    await p.evaluate(sp => { window.__pickSp = sp }, sp)
    await send(p, 'start|0,0,0,1,' + sp + ',1'); await fight(p); await send(p, 'cpuoff|1')
    await W(1500)
    await send(p, 'gauge|100'); await send(p, 'specialnear')
    for (let i = 0; i < 4; i++) { await W(i === 0 ? 200 : 350); await p.screenshot({ path: `shots/g/${tag}_sp${sp}_${i}.png` }) }
    await W(3000)
  }
  console.log(tag, 'banner log (mine)', JSON.stringify(await p.evaluate(() => window.__ban.slice(0, 20))))
  // 相手の必殺: 名前の帯が出ないこと
  await send(p, 'start|0,0,0,3,0,1'); await fight(p); await send(p, 'cpuoff|0')
  await p.evaluate(() => { window.__ban.length = 0; window.__evs = []; const o = window.KB.onUnity; window.KB.onUnity = s => { o(s); if (s.includes('"k":"special"')) window.__evs.push(JSON.parse(s).i) } })
  await send(p, 'gauge|100|1'); await send(p, 'gauge|100|2'); await send(p, 'gauge|100|3')
  const t0 = Date.now(); let shot = false
  while (Date.now() - t0 < 25000) { await W(300); const ev = await p.evaluate(() => window.__evs.filter(i => i !== 0).length); if (ev && !shot) { shot = true; await W(200); await p.screenshot({ path: `shots/g/${tag}_cpu_special.png` }) } if (ev >= 2) break }
  console.log(tag, 'cpu specials', await p.evaluate(() => window.__evs.filter(i => i !== 0).length), 'banner on during cpu', JSON.stringify(await p.evaluate(() => window.__ban.filter(x => x.startsWith('true')))))
  // 結果（自動で最後まで）
  await send(p, 'auto|1'); await send(p, 'timescale|4')
  await p.waitForFunction(() => window.KB.result, null, { timeout: 240000 })
  await W(3200); await p.screenshot({ path: `shots/g/${tag}_result.png` })
  console.log(tag, 'errs', JSON.stringify(errs))
  await b.close()
}
