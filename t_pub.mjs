import { open, wait } from './lib.mjs'
const { b, p, errs, loadMs } = await open(process.argv[2] || 'https://tom3games.github.io/koma-battle/')
const logs = []; p.on('console', m => logs.push(m.type() + ' ' + m.text()))
await wait(4000)
console.log('loadMs', loadMs, 'ready', await p.evaluate(() => window.KB && window.KB.ready))
console.log(await p.evaluate(() => document.body.innerText.slice(0, 200)))
console.log('errs', errs); console.log(logs.filter(l => !l.startsWith('log')).slice(0, 10).join('\n'))
await p.screenshot({ path: 'shots/pub.png' }); await b.close()
