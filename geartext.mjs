// 装備の文字が切れていないか: 全部品・必殺・回転・パッシブを1つずつ選び、5サイズで数える。node geartext.mjs [url]
import { open, wait } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
let bad = 0, checks = 0
for (const [w, h, tag, mob] of [[360, 640, 'p360', true], [390, 844, 'p390', true], [430, 932, 'p430', true], [844, 390, 'l844', true], [1280, 720, 'pc', false]]) {
  const { b, p, errs } = await open(url + '?lv=10', { w, h, mobile: mob })
  await p.click('#btnPlay'); await wait(300); await p.click('#nav button[data-pg="gear"]'); await wait(600)
  for (const key of ['top', 'core', 'leg', 'dir', 'sp', 'pas']) {
    const n = await p.evaluate(k => document.querySelector(`.slot[data-slot="${k}"]`).querySelector('.slot-no').textContent.split('/')[1], key)
    for (let i = 0; i < +n; i++) {
      await p.evaluate(k => document.querySelector(`.slot[data-slot="${k}"] .slot-body`).dispatchEvent(new PointerEvent('pointerdown')), key)
      const r = await p.evaluate(() => {
        const out = []
        const chk = (el, why) => { if (!el) return; if (el.scrollWidth > el.clientWidth + 1 || (why === 'detail' && el.scrollHeight > el.clientHeight + 1)) out.push(why + ' ' + el.textContent.slice(0, 12)) }
        document.querySelectorAll('.slot-body').forEach(sb => { const lim = sb.getBoundingClientRect().right - 26; sb.querySelectorAll('.slot-txt b,.slot-txt small').forEach(e => { const r = document.createRange(); r.selectNodeContents(e); const q = r.getBoundingClientRect(); if (q.right > lim + 1) out.push('text-over ' + e.textContent.slice(0, 10)) }) })
        document.querySelectorAll('.slot-txt b,.slot-txt small').forEach(e => chk(e, 'row'))
        chk(document.getElementById('detail'), 'detail')
        const vw = innerWidth, vh = innerHeight
        document.querySelectorAll('.slot-body,.detail,.sel-stats,.sel-panel,.sel-side').forEach(e => { const q = e.getBoundingClientRect(); if (q.right > vw + 1 || q.bottom > vh + 1 || q.left < -1 || q.top < -1) out.push('out ' + e.className) })
        const a = document.querySelector('.sel-panel').getBoundingClientRect(), d = document.getElementById('detail').getBoundingClientRect(), s = document.getElementById('stats').getBoundingClientRect()
        const ov = (x, y) => !(x.right <= y.left || x.left >= y.right || x.bottom <= y.top || x.top >= y.bottom)
        if (ov(a, d)) out.push('panel/detail'); if (ov(d, s)) out.push('detail/stats'); if (ov(a, s)) out.push('panel/stats')
        if (document.documentElement.scrollHeight > innerHeight) out.push('page scroll')
        return out
      })
      checks++
      if (r.length) { bad++; console.log(tag, key, i, JSON.stringify(r)) }
      await p.click(`.slot[data-slot="${key}"] .arw[data-d="1"]`); await wait(60)
    }
  }
  await p.screenshot({ path: `shots/h/gear_${tag}.png` })
  console.log(tag, 'errs', JSON.stringify(errs))
  await b.close()
}
console.log('checks', checks, 'bad', bad)
