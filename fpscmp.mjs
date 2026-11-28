import { open, wait, send } from './lib.mjs'
const url = process.argv[2]
const { b, p } = await open(url, { w: 390, h: 844, mobile: true })
await p.click('#btnPlay'); await wait(300)
const out = []
for (const st of [0, 1, 2]) {
  await p.evaluate(s => { KB.result = null; KB.send('stage|' + s); KB.startMatch() }, st)
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
  await send(p, 'cpuoff|1'); await wait(4000)
  const fps = []; for (let i = 0; i < 4; i++) { fps.push(await p.evaluate(() => KB.fps)); await wait(1200) }
  out.push(fps)
}
console.log(url.slice(0, 50), JSON.stringify(out)); await b.close()
