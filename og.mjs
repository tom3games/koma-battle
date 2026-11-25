// OGP 画像（1200x630）: 上からの一撃が当たった瞬間＋題名。node og.mjs [url]
import { open, wait, state, send } from './lib.mjs'
import { chromium } from 'playwright'
import { readFileSync } from 'node:fs'
const url = process.argv[2] || 'http://localhost:5392/'
const W = 1200, H = 630
const { b, p } = await open(url, { w: W, h: H })
await p.click('#btnPlay'); await wait(400)
await p.click('#vsPick button[data-n="3"]'); await p.click('#btnFight'); await wait(5500)
await p.addStyleTag({ content: '.tacho,.hud-time,#btnMute,#tags,#onoma{display:none!important}' })
await p.evaluate(() => { window.__airHit = 0; const o = window.KB.onUnity; window.KB.onUnity = s => { o(s); if (s.includes('"k":"hit"') && s.includes('"air":1') && s.includes('"a":0,')) window.__airHit = JSON.parse(s) } })
let ok = false
for (let t = 0; t < 12 && !ok; t++) {
  const f = (await state(p)).k.find(k => k.i !== 0 && k.a)
  await send(p, `tp|${f.wx - 1.2},${f.h + 3.2},${f.wz - 0.8},0,0,0`); await wait(50)
  const g = (await state(p)).k.find(k => k.i === f.i)
  await p.mouse.move(g.x * W, (1 - g.y) * H); await p.mouse.down(); await p.mouse.up()
  try { await p.waitForFunction(() => window.__airHit, null, { timeout: 1500 }); await wait(70); ok = true } catch (e) { await wait(800) }
}
await p.screenshot({ path: 'shots/og_raw.png' })
const hit = await p.evaluate(() => window.__airHit) || { x: 0.5, y: 0.5 }
const K = 1.3, left = Math.max(-0.3 * W, Math.min(0.2 * W, 0.7 * W - hit.x * K * W)), top = Math.max(-0.3 * H, Math.min(0, 0.52 * H - (1 - hit.y) * K * H))
await b.close()
console.log('aerial', ok)
const raw = readFileSync('shots/og_raw.png').toString('base64')
const b2 = await chromium.launch(); const q = await b2.newPage({ viewport: { width: W, height: H } })
await q.setContent(`<html><head><link href="https://fonts.googleapis.com/css2?family=M+PLUS+1:wght@900&display=swap" rel="stylesheet"><style>
body{margin:0;background:#1b1f3b;width:${W}px;height:${H}px;position:relative;overflow:hidden;font-family:"M PLUS 1",sans-serif}
img{position:absolute;left:${left}px;top:${top}px;width:${K * 100}%;height:${K * 100}%}
.sh{position:absolute;inset:0;background:linear-gradient(90deg,rgba(18,20,44,.9) 0%,rgba(18,20,44,.6) 36%,rgba(18,20,44,0) 52%)}
.t{position:absolute;left:56px;top:150px;color:#f5f3ee;font-weight:900;font-size:150px;line-height:.9;letter-spacing:-.04em;transform:rotate(-4deg);transform-origin:left bottom;text-shadow:0 7px 0 #c22a14}
.l{position:absolute;left:62px;top:478px;color:#ffc83a;font-weight:900;font-size:36px}
</style></head><body><img src="data:image/png;base64,${raw}"><div class="sh"></div><div class="t">コマ<br>バトル</div><div class="l">跳んで、上から落とせ。</div></body></html>`)
await q.waitForLoadState('networkidle'); await q.evaluate(() => document.fonts.ready)
await q.screenshot({ path: 'unity/Assets/WebGLTemplates/Koma/og.png' })
// アイコン（読み込み画面のコマ）
await q.setViewportSize({ width: 192, height: 192 })
await q.setContent(`<body style="margin:0;background:#1b1f3b"><svg width="192" height="192" viewBox="0 0 120 120"><ellipse cx="60" cy="104" rx="34" ry="6" fill="#0d1028" opacity=".55"/><path d="M60 100 L52 82 H68 Z" fill="#c9ccd8"/><ellipse cx="60" cy="70" rx="44" ry="12" fill="#e8402a"/><ellipse cx="60" cy="66" rx="44" ry="12" fill="#ff6a4a"/><rect x="49" y="44" width="22" height="22" rx="3" fill="#3b82f6"/><ellipse cx="60" cy="44" rx="11" ry="4" fill="#ffd24a"/><path d="M20 66 l-10 -8 l16 2 z M100 66 l10 8 l-16 -2 z" fill="#d7dbe6"/></svg></body>`)
await q.screenshot({ path: 'unity/Assets/WebGLTemplates/Koma/TemplateData/icon.png' })
await b2.close()
