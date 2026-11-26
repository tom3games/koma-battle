// パッシブ8種の発動を PC・スマホで撮る。雷は「逃げて外れる」と「逃げずに当たる」の両方。node passhots.mjs [url]
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const W = wait
const NAMES = ['gekirin', 'amakake', 'tamashii', 'raimei', 'fukutsu', 'guren', 'kage', 'tate']
const fight = async p => { await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 30000 }); await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 }) }
for (const [w, h, tag, mob] of [[1280, 720, 'pc', false], [390, 844, 'ph', true]]) {
  const { b, p, errs } = await open(url + '?lv=10', { w, h, mobile: mob })
  await p.click('#btnPlay'); await W(300)
  await p.evaluate(() => { window.__ev = []; const o = window.KB.onUnity; window.KB.onUnity = s => { o(s); if (s.startsWith('{"t":"e"')) { const m = JSON.parse(s); if (['pas', 'pasfx', 'warn', 'thunder'].includes(m.k)) window.__ev.push(m) } } })
  const shots = async (name, n, gap) => { for (let i = 0; i < n; i++) { await W(gap); await p.screenshot({ path: `shots/p/${tag}_${name}_${i}.png` }) } }
  const begin = async (pas, ncpu = 1) => {
    await p.evaluate(([n, pas]) => { document.querySelector(`#vsPick button[data-n="${n}"]`).click(); Object.assign(KB.pick, { top: 0, core: 0, leg: 0, dir: 0, sp: 0, pas: pas + 1 }); KB.startMatch() }, [ncpu, pas]); await fight(p); await send(p, 'cpupas|-1'); await W(300)
    await p.evaluate(() => { window.__ev.length = 0 })
  }
  for (let pas = 0; pas < 8; pas++) {
    await begin(pas); await send(p, 'cpuoff|1')
    const nm = NAMES[pas]
    if (pas === 0) { await send(p, 'spin|0.2,0'); await shots(nm, 3, 350) }
    if (pas === 1) { const s0 = (await state(p)).k[0]; await send(p, 'tp|' + [s0.wx, s0.gh + 7, s0.wz, 0, 0, 0].join(',')); await shots(nm, 4, 200) }
    if (pas === 2) { // 近くの相手へ突進して当てる
      await send(p, 'cpuoff|1'); const s = (await state(p)).k; await send(p, 'tp|' + [s[0].wx + 2.2, s[0].gh + 0.4, s[0].wz, 0, 0, 0, 1].join(',')); await send(p, 'dash'); await shots(nm, 4, 150) }
    if (pas === 3) { await send(p, 'strike|1,0'); await shots(nm, 7, 0) }
    if (pas === 4) { await send(p, 'spin|0.004,0'); await shots(nm, 4, 400) }
    if (pas === 5) { await send(p, 'move|1,0'); await W(1600); await shots(nm, 3, 300); await send(p, 'move|0,0') }
    if (pas === 6) { await W(1500); await send(p, 'dash'); await shots(nm, 3, 150) }
    if (pas === 7) { await send(p, 'guard|1,0'); await shots(nm, 2, 300) }
    console.log(tag, nm, JSON.stringify(await p.evaluate(() => window.__ev.map(e => e.k + ':' + (e.p ?? e.hit ?? '')))))
  }
  // 雷: 逃げて外れる / 逃げずに当たる（CPU が撃つ・標的は自分）
  for (const run of ['dodge', 'stay']) {
    await begin(-1); await send(p, 'cpuoff|1'); await send(p, 'setpas|3,1')
    const before = (await state(p)).k[0]
    await send(p, 'strike|0,1'); await W(120)
    if (run === 'dodge') { await send(p, 'move|0,1'); }   // 撃たれた直後から逃げる（カメラ基準の前）
    await shots(`raimei_${run}`, 7, 0)
    if (run === 'dodge') await send(p, 'move|0,0')
    const ev = await p.evaluate(() => window.__ev.filter(e => e.k === 'thunder').map(e => e.hit))
    console.log(tag, 'thunder', run, 'hit=', JSON.stringify(ev))
  }
  console.log(tag, 'errs', JSON.stringify(errs))
  await b.close()
}
