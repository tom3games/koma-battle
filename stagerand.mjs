// ステージのランダム: 24回始めて、両方出る・同じステージが3回続かない。node stagerand.mjs [url]
import { open, wait } from './lib.mjs'
const { b, p } = await open(process.argv[2] || 'http://localhost:5393/', { w: 480, h: 270 })
await p.click('#btnPlay'); await wait(200)
const seq = []
for (let i = 0; i < 24; i++) {
  await p.evaluate(() => { KB.events.length = 0; KB.send('stage|-1'); KB.startMatch() })
  await p.waitForFunction(() => KB.events.some(e => e.k === 'stage'), null, { timeout: 30000 })
  seq.push(await p.evaluate(() => KB.events.find(e => e.k === 'stage').p)); await wait(150)
}
let run3 = false; for (let i = 2; i < seq.length; i++) if (seq[i] === seq[i - 1] && seq[i] === seq[i - 2]) run3 = true
console.log(seq.join(''), '丘', seq.filter(x => x === 1).length, '/ ボウル', seq.filter(x => x === 0).length, '3回連続', run3)
process.exit(0)
