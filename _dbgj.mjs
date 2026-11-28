import { open, wait, state, send } from './lib.mjs'
const { b, p, errs } = await open('http://localhost:5392/', { w: 640, h: 360 })
await p.click('#btnPlay'); await wait(200); await p.click('#nav button[data-pg="gear"]'); await wait(300); await p.click('.slot[data-slot="leg"] .arw[data-d="-1"]'); await wait(400); console.log(await p.evaluate(() => JSON.stringify(JSON.parse(localStorage.getItem('koma-pick2'))))); await p.click('#nav button[data-pg="home"]'); await wait(300)
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click()); await p.click('#btnFight')
await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 })
await send(p, 'cpuoff|1')
const M = { x: Math.cos(Math.PI / 4) * 13, z: Math.sin(Math.PI / 4) * 13 }
const a = 0
const sx = M.x + Math.cos(a) * 7.6, sz = M.z + Math.sin(a) * 7.6
await send(p, 'spin|1,0'); await send(p, 'tp|' + [sx, 6, sz, 0, -1, 0].join(','))
await wait(900)
const s0 = (await state(p)).k[0]; console.log('s0', s0.h, s0.gh, s0.air, s0.wx, s0.wz)
await send(p, 'tp|' + [s0.wx, s0.h + 0.02, s0.wz, -9, 0, 0].join(','))
for (let i = 0; i < 20; i++) { await wait(150); const k = (await state(p)).k[0]; console.log(k.wx.toFixed(2), k.wz.toFixed(2), 'h', (k.h - k.gh).toFixed(2), 'v', k.vx, k.vz, 'air', k.air) }
console.log(await p.evaluate(() => KB.events.filter(e => e.i === 0).slice(-8).map(e => e.k + ':' + e.p)))
await b.close()
