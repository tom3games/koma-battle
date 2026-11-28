// BGM: 最初の操作で鳴り始める・ミュート設定を守る・試合→結果→タイトルでも二重にも途切れもしない。node bgmtest.mjs [url]
import './mute-auto.mjs'
import { chromium } from 'playwright'
import { GL_ARGS, wait } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
for (const mode of ['key', 'tap-loading', 'muted']) {
  const b = await chromium.launch({ args: GL_ARGS })
  const ctx = await b.newContext({ viewport: { width: 1280, height: 720 } })
  if (mode === 'muted') await ctx.addInitScript(() => { try { localStorage.setItem('koma-mute', '1') } catch (e) {} })
  const p = await ctx.newPage(); const errs = []
  p.on('pageerror', e => errs.push(e.message))
  await p.goto(url)
  await p.waitForFunction(() => window.__jsUp === true, null, { timeout: 60000 })
  const before = await p.evaluate(() => ({ loop: Snd.played['loop:bgm'] || 0, ctx: !!Snd.ctx }))
  ok(before.loop === 0, `${mode}: 操作する前は鳴らない`)
  if (mode === 'key') await p.keyboard.press('KeyA'); else await p.mouse.click(300, 300)   // 読み込み中でも画面のどこかを触る / キー
  await wait(1500)
  const s1 = await p.evaluate(() => ({ loop: Snd.played['loop:bgm'] || 0, playing: Snd.playing(), gain: Snd.master && Snd.master.gain.value, muted: Snd.muted }))
  ok(s1.loop === 1 && s1.playing.includes('bgm'), `${mode}: 最初の操作1回でBGMが始まる (${JSON.stringify(s1)})`)
  if (mode === 'muted') ok(s1.gain === 0, 'ミュート設定なら音量0を守る')
  await p.waitForFunction(() => window.__kbReady === true, null, { timeout: 180000 })
  if (mode === 'key') {
    await p.click('#btnPlay'); await wait(300)
    await p.evaluate(() => { document.querySelector('#vsPick button[data-n="1"]').click() })
    await p.click('#btnFight'); await p.waitForFunction(() => KB.events.some(e => e.k === 'go'), null, { timeout: 60000 })
    await wait(500)
    const s2 = await p.evaluate(() => ({ loop: Snd.played['loop:bgm'], playing: Snd.playing() }))
    ok(s2.loop === 1 && s2.playing.length === 1, `試合が始まっても二重にならない (${JSON.stringify(s2)})`)
    await p.evaluate(() => { KB.send('auto|1'); KB.send('timescale|4') })
    await p.waitForFunction(() => KB.result, null, { timeout: 300000 })
    await wait(800)
    const s3 = await p.evaluate(() => ({ loop: Snd.played['loop:bgm'], playing: Snd.playing() }))
    ok(s3.loop === 1 && s3.playing.includes('bgm'), `結果画面でも止まらない・二重にならない (${JSON.stringify(s3)})`)
    await p.click('#btnHome'); await wait(800)
    const s4 = await p.evaluate(() => ({ loop: Snd.played['loop:bgm'], playing: Snd.playing() }))
    ok(s4.loop === 1 && s4.playing.length === 1, `ホームに戻っても途切れず二重にならない (${JSON.stringify(s4)})`)
  }
  ok(errs.length === 0, `${mode}: エラー0 ${errs}`)
  await b.close()
}
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK'); process.exit(0)
