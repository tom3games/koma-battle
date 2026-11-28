// 背景（地下闘技場）の実画面を各ステージ・PC とスマホで撮る。node bgshots.mjs [url]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const res = []
// 黒い面の割合: 画面の中の明るさが 18/255 未満のピクセルの割合（スクリーンショットを別のページの canvas で数える）
async function blackRatio(b, buf) { const pg = await b.newPage(); const r = await pg.evaluate(async d => { const im = new Image(); im.src = 'data:image/png;base64,' + d; await im.decode(); const c = document.createElement('canvas'); c.width = im.width; c.height = im.height; const x = c.getContext('2d'); x.drawImage(im, 0, 0); const px = x.getImageData(0, 0, c.width, c.height).data; let n = 0; for (let i = 0; i < px.length; i += 4) if (0.2126 * px[i] + 0.7152 * px[i + 1] + 0.0722 * px[i + 2] < 18) n++; return n / (px.length / 4) }, buf.toString('base64')); await pg.close(); return +r.toFixed(3) }
for (const [tag, w, h, mobile] of [['pc', 1280, 720, false], ['mob', 390, 844, true]]) {
  const { b, p, errs } = await open(url, { w, h, mobile })
  await p.click('#btnPlay'); await wait(300)
  await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
  for (const st of [0, 1, 2]) {
    await p.evaluate(s => { KB.result = null; KB.events.length = 0; KB.send('stage|' + s); KB.startMatch() }, st)
    await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
    await send(p, 'cpuoff|1'); await send(p, 'tp|0,6,6,0,0,0,0'); await wait(1800)
    { const buf = await p.screenshot({ path: `shots/bg_${tag}_s${st}_a.png` }); res.push(`${tag} s${st} a ${await blackRatio(b, buf)}`) }
    await send(p, 'tp|14,6,-12,0,0,0,0'); await wait(2200)
    { const buf = await p.screenshot({ path: `shots/bg_${tag}_s${st}_b.png` }); res.push(`${tag} s${st} b ${await blackRatio(b, buf)}`) }
  }
  console.log(tag, 'errs', errs); await b.close()
  console.log('黒い面の割合', res.join(' | '))
}
