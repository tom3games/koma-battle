import './mute-auto.mjs'
import { chromium } from 'playwright'
const url = process.argv[2], out = process.argv[3]
const b = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] })
const p = await b.newPage({ viewport: { width: 960, height: 600 } })
const errs = []
p.on('console', m => { if (m.type() === 'error') errs.push(m.text()) })
p.on('pageerror', e => errs.push('pageerror ' + e.message))
const t0 = Date.now()
await p.goto(url)
await p.waitForFunction(() => window.__unityReady || document.querySelector('#unity-loading-bar')?.style.display === 'none', null, { timeout: 120000 }).catch(e => errs.push('timeout'))
const t1 = Date.now()
await p.waitForTimeout(1500)
await p.screenshot({ path: out })
console.log(JSON.stringify({ loadMs: t1 - t0, errs, gl: await p.evaluate(() => { const c = document.createElement('canvas'); const g = c.getContext('webgl2'); return g ? g.getParameter(g.RENDERER) : 'none' }) }))
await b.close()
