// 長押し: PC は右クリック長押しで進化版・短押しで通常。スマホの星ボタンも同じ。node evohold.mjs [url]
import { begin, wait, send } from './sp2lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
{
  const o = await begin(url, { sp: 0, n: 1, w: 1280, h: 720, stage: 0 }); await send(o.p, 'cpuoff|1')
  await o.place(0, -6, 0, 0, 0, 1); await o.place(1, 5, 0, 0, 0, 1); await wait(1200)
  await send(o.p, 'gauge|500'); await o.clear()
  await o.p.mouse.move(640, 360); await o.p.mouse.down({ button: 'right' }); await wait(250)
  ok((await o.evs('special')).length === 0, 'PC: 押した直後はまだ撃たない（長押しの判定中）')
  await wait(500); await o.p.mouse.up({ button: 'right' }); await wait(400)
  ok((await o.evs('evo')).length === 1, 'PC: 右クリック長押しで極大技')
  // 短押しは通常
  await wait(3500); await o.place(0, -6, 0, 0, 0, 1); await wait(800); await send(o.p, 'gauge|500'); await o.clear()
  await o.p.mouse.down({ button: 'right' }); await wait(120); await o.p.mouse.up({ button: 'right' }); await wait(500)
  ok((await o.evs('evo')).length === 0 && (await o.evs('special')).length === 1, 'PC: 短く押すと通常の必殺')
  ok((await o.k(0)).g > 0.7, `PC: 通常は1回分だけ使う (g=${(await o.k(0)).g.toFixed(2)})`)
  console.log('errs', o.errs); await o.b.close()
}
{
  const o = await begin(url, { sp: 0, n: 1, w: 390, h: 844, mobile: true, stage: 0 }); await send(o.p, 'cpuoff|1')
  await o.place(0, -6, 0, 0, 0, 1); await o.place(1, 5, 0, 0, 0, 1); await wait(1500)
  await send(o.p, 'gauge|500'); await wait(400); await o.clear()
  const press = async ms => { await o.p.evaluate(ms => new Promise(r => { const b = document.getElementById('tSpBtn'); const ev = t => b.dispatchEvent(new PointerEvent(t, { pointerId: 7, bubbles: true, button: 0 })); ev('pointerdown'); setTimeout(() => { ev('pointerup'); r() }, ms) }), ms) }
  await press(800); await wait(500)
  ok((await o.evs('evo')).length === 1, 'スマホ: 星ボタン長押しで極大技')
  await o.place(0, -6, 0, 0, 0, 1); await wait(800); await send(o.p, 'gauge|500'); await wait(400); await o.clear()
  await press(150); await wait(500)
  ok((await o.evs('evo')).length === 0 && (await o.evs('special')).length === 1, 'スマホ: 短く押すと通常の必殺')
  await o.shot('evohold_mobile')
  console.log('errs', o.errs); ok(o.errs.length === 0, 'コンソールエラー0'); await o.b.close()
}
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(NG.length ? 1 : 0)
