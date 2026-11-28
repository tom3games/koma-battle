import { open, wait } from './lib.mjs'
const [top, core, leg] = process.argv.slice(2, 5).map(Number), stage = +process.argv[5]
const { b, p } = await open('http://localhost:5393/?lv=10', { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="2"]').click())
for (let i = 0; i < 5; i++) {
  await p.evaluate(({ stage, top, core, leg }) => { Object.assign(KB.pick, { top, core, leg, dir: 0, sp: 0, pas: 0 }); KB.result = null; KB.events.length = 0; KB.send('stage|' + stage); KB.send('debugstate|1'); KB.startMatch() }, { stage, top, core, leg })
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 }); await wait(100)
  await p.evaluate(() => { KB.send('debugstate|1'); KB.send('auto|1'); KB.send('cpupas|-1'); KB.send('cpusp|-1'); KB.send('timescale|3') })
  let last = null
  for (let t = 0; t < 400; t++) { const s = await p.evaluate(() => KB.state && KB.state.k && KB.state.k[0]); if (s && s.L) last = s; if (await p.evaluate(() => !!KB.result)) break; await wait(300) }
  const r = await p.evaluate(() => KB.result)
  console.log(JSON.stringify({ win: r && r.win, why: r && r.why, t: r && r.time, L: last && last.L, dealt: r && r.dealt }))
}
await b.close()
