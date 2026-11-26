import '../mute-auto.mjs'
// 字幕（透明の PNG）と最後の一枚（題名と URL）。ゲームのタイトル画面を開き、ゲームと同じ書体（M PLUS 1）で描く。node promo/cards.mjs
import { chromium } from 'playwright'
import { MUTE_LAUNCH, muteContext } from '../mute.mjs'
import { spawn } from 'node:child_process'
import { mkdirSync } from 'node:fs'
const ROOT = import.meta.dirname + '/..'
const OUT = process.env.KP_OUT || '/private/tmp/claude-501/-Users-tomoki-Desktop-tmk-hub/9116bcd6-5450-4eca-92c7-248c73c1167b/scratchpad/kp-video'
const DIR = `${OUT}/cards`; mkdirSync(DIR, { recursive: true })
const CAPTION = process.env.KP_CAPTION || 'コマになって戦った結果'
const PORT = 5590
const server = spawn(process.execPath, ['server.mjs'], { cwd: ROOT, env: { ...process.env, PORT: String(PORT), ROOT: 'docs' } })
await new Promise(r => setTimeout(r, 600))
const b = await chromium.launch({ channel: 'chromium', headless: true, args: [...MUTE_LAUNCH.args, '--use-angle=metal', '--enable-gpu', '--ignore-gpu-blocklist'] })
const ctx = await b.newContext({ viewport: { width: 540, height: 960 }, deviceScaleFactor: 2, hasTouch: true, isMobile: true, locale: 'ja-JP' })
await muteContext(ctx)
const p = await ctx.newPage()
await p.goto(`http://localhost:${PORT}/`); await p.waitForFunction(() => window.__kbReady === true, null, { timeout: 120000 }); await p.evaluate(() => document.fonts.ready)
await p.waitForTimeout(2500)
const fonts = async () => { await p.evaluate(async () => { await document.fonts.load('900 32px "M PLUS 1"', 'コマになって戦った結果tom3games.github.io/koma-battle'); await document.fonts.ready }); await p.waitForTimeout(400) }
// 最後の一枚: タイトル画面（ボタンなし）の下に URL
await p.evaluate(() => {
  document.querySelector('#btnPlay').style.visibility = 'hidden'
  const d = document.createElement('div'); d.id = '__url'
  d.style.cssText = 'position:fixed;left:0;right:0;bottom:calc(9vh);text-align:center;z-index:99999;font:800 22px "M PLUS 1",sans-serif;letter-spacing:.06em;color:#ecebe6;text-shadow:0 2px 14px rgba(0,0,0,.9)'
  d.textContent = 'tom3games.github.io/koma-battle'; document.body.appendChild(d)
})
await fonts(); await p.screenshot({ path: `${DIR}/endcard.png` })
// 字幕: 透明の地に白い字＋濃いふち
await p.evaluate(t => {
  document.querySelector('#__url').remove()
  for (const el of document.body.children) el.style.visibility = 'hidden'
  document.documentElement.style.background = 'transparent'; document.body.style.background = 'transparent'
  const d = document.createElement('div')
  d.style.cssText = 'position:fixed;left:0;right:0;top:36%;text-align:center;z-index:99999;color:#ecebe6;font:900 34px "M PLUS 1",sans-serif;letter-spacing:.08em;-webkit-text-stroke:8px #0c0d11;paint-order:stroke fill;text-shadow:0 4px 18px rgba(0,0,0,.6)'
  d.textContent = t; document.body.appendChild(d)
}, CAPTION)
await fonts(); await p.screenshot({ path: `${DIR}/caption1.png`, omitBackground: true })
console.log('fontcheck', await p.evaluate(() => document.fonts.check('900 34px "M PLUS 1"')))
await b.close(); server.kill(); console.log('ok', DIR)
