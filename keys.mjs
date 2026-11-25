// キーボードの WASD が効くか（HTML のボタンを押した後でも）
import { open, wait, state, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/'
const { b, p, errs } = await open(url)
await p.click('#btnPlay'); await wait(300)
await p.click('#vsPick button[data-n="1"]'); await p.click('#btnFight'); await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight' && window.KB.state.tm > 1.5, null, { timeout: 60000 })
const z0 = (await state(p)).k[0].wz
await p.keyboard.down('KeyS'); await wait(1000); await p.keyboard.up('KeyS')
const z1 = (await state(p)).k[0].wz
const x0 = (await state(p)).k[0].wx
await p.keyboard.down('KeyD'); await wait(800); await p.keyboard.up('KeyD')
const x1 = (await state(p)).k[0].wx
console.log(JSON.stringify({ S_moves_back: z1 < z0, z0, z1, D_moves_right: x1 > x0, x0, x1, errs }))
await b.close()
