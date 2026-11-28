// 背景（地下闘技場）の実画面を各ステージ・PC とスマホで撮る。node bgshots.mjs [url]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
for (const [tag, w, h, mobile] of [['pc', 1280, 720, false], ['mob', 390, 844, true]]) {
  const { b, p, errs } = await open(url, { w, h, mobile })
  await p.click('#btnPlay'); await wait(300)
  await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
  for (const st of [0, 1, 2]) {
    await p.evaluate(s => { KB.result = null; KB.events.length = 0; KB.send('stage|' + s); KB.startMatch() }, st)
    await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
    await send(p, 'cpuoff|1'); await send(p, 'tp|0,6,6,0,0,0,0'); await wait(1800)
    await p.screenshot({ path: `shots/bg_${tag}_s${st}_a.png` })
    await send(p, 'tp|14,6,-12,0,0,0,0'); await wait(2200)
    await p.screenshot({ path: `shots/bg_${tag}_s${st}_b.png` })
  }
  console.log(tag, 'errs', errs); await b.close()
}
