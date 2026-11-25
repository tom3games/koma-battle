// 撮った画面を並べて1枚にする（確認用）。node sheet.mjs out.png a.png b.png ...
import './mute-auto.mjs'
import { chromium } from 'playwright'
import { readFileSync } from 'node:fs'
const [out, ...files] = process.argv.slice(2)
const cols = 2, w = 640
const imgs = files.map(f => `<figure><img src="data:image/png;base64,${readFileSync(f).toString('base64')}"><figcaption>${f.split('/').pop()}</figcaption></figure>`).join('')
const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: w * cols, height: 400 } })
await p.setContent(`<style>body{margin:0;background:#000;display:grid;grid-template-columns:repeat(${cols},${w}px)}figure{margin:0;position:relative}img{width:${w}px;display:block}figcaption{position:absolute;left:4px;top:4px;background:#000;color:#fff;font:12px monospace;padding:1px 4px}</style>${imgs}`)
await p.screenshot({ path: out, fullPage: true }); await b.close()
