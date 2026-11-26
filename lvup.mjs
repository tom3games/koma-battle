// 初回の1試合を自動で通し、結果画面のレベルアップ演出を撮る。node lvup.mjs
import { open, wait, send } from './lib.mjs'
for (const [w, h, tag, mob] of [[1280, 720, 'pc', false], [390, 844, 'ph', true], [844, 390, 'l', true]]) {
  const { b, p, errs } = await open('http://localhost:5392/', { w, h, mobile: mob })
  await p.click('#btnPlay'); await wait(300)
  await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click())
  await p.click('#btnFight'); await send(p, 'auto|1'); await send(p, 'timescale|6')
  await p.waitForFunction(() => window.KB.result, null, { timeout: 240000 })
  await wait(1500); await p.screenshot({ path: `shots/g/lv_${tag}_a.png` })
  await wait(2600); await p.screenshot({ path: `shots/g/lv_${tag}_b.png` })
  const r = await p.evaluate(() => ({ res: window.KB.result, up: document.getElementById('rsUp').textContent, sh: document.documentElement.scrollHeight, ih: innerHeight, prof: localStorage.getItem('koma-prof') }))
  console.log(tag, JSON.stringify(r), JSON.stringify(errs))
  await p.click('#btnParts'); await wait(500)
  console.log(tag, 'dot', await p.evaluate(() => [document.getElementById('dotGear').hidden, KB.page, document.querySelectorAll('.slot').length]))
  await p.screenshot({ path: `shots/g/lv_${tag}_gear.png` })
  await b.close()
}
