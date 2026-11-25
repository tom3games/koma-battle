import './mute-auto.mjs'
import { chromium } from 'playwright'
export const GL_ARGS = ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist']
export async function open(url, { w = 1280, h = 720, mobile = false } = {}) {
  const b = await chromium.launch({ args: GL_ARGS })
  const ctx = await b.newContext({ viewport: { width: w, height: h }, hasTouch: mobile, isMobile: mobile, deviceScaleFactor: 1 })
  const p = await ctx.newPage()
  const errs = []
  p.on('console', m => { if (m.type() === 'error') errs.push(m.text()) })
  p.on('pageerror', e => errs.push('pageerror ' + e.message))
  const t0 = Date.now()
  await p.goto(url)
  await p.waitForFunction(() => window.__kbReady === true, null, { timeout: 180000 })
  const loadMs = Date.now() - t0
  return { b, ctx, p, errs, loadMs }
}
export const wait = ms => new Promise(r => setTimeout(r, ms))
export const state = p => p.evaluate(() => window.KB.state)
export const send = (p, c) => p.evaluate(c => window.KB.send(c), c)
