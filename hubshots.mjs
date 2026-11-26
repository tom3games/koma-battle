// ハブの各ページを PC・スマホの幅で撮り、一画面に収まるか数える。node hubshots.mjs [url] [lv]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const lv = process.argv[3] || '3'
const sizes = [[1280, 720, 'pc', false], [390, 844, 'p390', true], [360, 640, 'p360', true], [430, 932, 'p430', true], [844, 390, 'l844', true]]
let bad = 0
for (const [w, h, tag, mob] of sizes) {
  const { b, p, errs } = await open(url + '?lv=' + lv, { w, h, mobile: mob })
  await wait(600)
  await p.screenshot({ path: `shots/h/${tag}_0_title.png` })
  await p.click('#btnPlay'); await wait(900)
  for (const pg of ['home', 'gear', 'grow', 'set']) {
    await p.click(`#nav button[data-pg="${pg}"]`); await wait(700)
    await p.screenshot({ path: `shots/h/${tag}_${pg}.png` })
    const m = await p.evaluate(() => {
      const pgEl = document.querySelector('.pg.on'); const r = pgEl.getBoundingClientRect()
      let over = []
      pgEl.querySelectorAll('*').forEach(e => { const q = e.getBoundingClientRect(); if (q.width && (q.bottom > innerHeight + 1 || q.right > innerWidth + 1 || q.left < -1)) over.push(e.className || e.tagName) })
      const nav = document.getElementById('nav').getBoundingClientRect()
      const sr = [...pgEl.children].map(c => c.getBoundingClientRect()).map(q => Math.round(q.bottom))
      // ページ内の物が下のメニューと重なるか
      const hit = []
      pgEl.querySelectorAll('.card,.sel-panel,.sel-side,.home-go,.grow-card,.set-card').forEach(e => { const q = e.getBoundingClientRect(); if (q.width && !(q.right <= nav.left || q.left >= nav.right || q.bottom <= nav.top || q.top >= nav.bottom)) hit.push(e.className) })
      const hdr = document.getElementById('hdr').getBoundingClientRect()
      pgEl.querySelectorAll('.card,.sel-panel,.sel-side,.home-go,.grow-card,.set-card').forEach(e => { const q = e.getBoundingClientRect(); if (q.width && !(q.right <= hdr.left || q.left >= hdr.right || q.bottom <= hdr.top || q.top >= hdr.bottom)) hit.push('hdr:' + e.className) })
      return { sh: document.documentElement.scrollHeight, ih: innerHeight, over: over.slice(0, 4), hit, bottoms: sr }
    })
    const ng = m.sh > m.ih || m.over.length || m.hit.length
    if (ng) bad++
    console.log(tag, pg, ng ? 'NG' : 'ok', JSON.stringify(m))
  }
  console.log(tag, 'errs', JSON.stringify(errs))
  await b.close()
}
console.log('bad', bad)
