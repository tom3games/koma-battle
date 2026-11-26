// 名前の入力: NG は保存されない・OK は頭上の名札と育成/ホームに出る・キー入力が Unity に取られない。node nametest.mjs [url]
import { open, wait } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5392/'
const { b, p, errs } = await open(url, { w: 1280, h: 720 })
await p.click('#btnPlay'); await wait(300)
await p.click('#nav button[data-pg="set"]'); await wait(400)
const tryName = async v => { await p.fill('#nameIn', ''); await p.type('#nameIn', v); await p.click('#btnName'); await wait(150); return { msg: await p.textContent('#nameMsg'), saved: await p.evaluate(() => JSON.parse(localStorage.getItem('koma-prof') || '{}').name || '') , hdr: await p.textContent('.pl-name') } }
console.log('NG ば か   ', JSON.stringify(await tryName('ば か')))
console.log('NG 運営     ', JSON.stringify(await tryName('運営')))
console.log('OK たまき   ', JSON.stringify(await tryName('たまき')))
console.log('長い名前    ', JSON.stringify(await tryName('あいうえおかきくけこさ')))
console.log('絵文字      ', JSON.stringify(await tryName('ゆうた😀​')))
await p.screenshot({ path: 'shots/h/name_set.png' })
await p.click('#nav button[data-pg="home"]'); await wait(300); await p.screenshot({ path: 'shots/h/name_home.png' })
await p.evaluate(() => document.querySelector('#vsPick button[data-n="1"]').click())
await p.click('#btnFight'); await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'fight', null, { timeout: 60000 }); await wait(500)
console.log('名札', await p.evaluate(() => document.querySelector('.tag.me span').textContent))
await p.screenshot({ path: 'shots/h/name_tag.png' })
console.log('errs', JSON.stringify(errs))
await b.close()
