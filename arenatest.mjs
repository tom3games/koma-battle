// ステージ表示の検査（公開前の必須。smoke と一緒に回す）: 2026-11-28 本人「マップの表示がちゃんとされないことがある。二度とないように」
// 3ステージを全部の順番で連続して回し、崩落・穴が起きた試合の後（もう一回の道とタイトルを通る道の両方）も含めて、
// 毎試合のカウントダウンで「そのステージにあるべき部品が全部ある・余計な物がない」を数で確かめ、画面も撮る。NG 0 でなければ出さない。
// node arenatest.mjs [url] [試合数 既定 18]
import { open, wait, send } from './lib.mjs'
const url = process.argv[2] || 'http://localhost:5393/', N = +(process.argv[3] || 18)
const { b, p, errs } = await open(url, { w: 800, h: 450 })
const NG = []; const ok = (c, m) => { if (!c) { NG.push(m); console.log('NG ' + m) } }
await p.click('#btnPlay'); await wait(300)
await p.evaluate(() => { document.querySelector('#vsPick button[data-n="3"]').click() })
const order = [0, 1, 2, 0, 2, 1, 2, 0, 1, 0, 2, 1, 1, 2, 0, 2, 2, 0, 1, 1]
for (let i = 0; i < N; i++) {
  const st = order[i % order.length], viaTitle = i % 2 === 1
  if (viaTitle) { await send(p, 'title'); await wait(400) }
  await p.evaluate(st => { KB.events.length = 0; KB.result = null; KB.send('stage|' + st); KB.startMatch() }, st)
  await p.waitForFunction(() => window.KB.state && window.KB.state.ph === 'count', null, { timeout: 60000 })
  await wait(300)
  await send(p, 'arenacheck')
  await p.waitForFunction(() => window.KB.events.some(e => e.k === 'arena'), null, { timeout: 5000 })
  const a = await p.evaluate(() => window.KB.events.filter(e => e.k === 'arena').pop())
  const tag = `#${i + 1} stage${st}${viaTitle ? ' (タイトル経由)' : ''}`
  ok(a.stage === st && a.built === st, `${tag}: ステージ番号 ${a.stage}/${a.built}`)
  ok(a.bad === 0, `${tag}: 床の頂点のずれ ${a.bad}/${a.verts}`)
  ok(a.pieces > 0 && a.pOn === a.pieces && a.pOk === a.pieces, `${tag}: 床の破片 ${a.pOk}/${a.pOn}/${a.pieces}`)
  ok(a.base === 1 && a.pitPiece === 1 && a.mat === 1, `${tag}: 土台${a.base} 中央の床${a.pitPiece} 床の材質${a.mat}`)
  ok(a.crOK === 1 && a.pitOK === 1, `${tag}: 崩落の状態が初期に戻っている (cr${a.crOK} pit${a.pitOK})`)
  ok(a.arc === (st === 0 ? a.arcN : 0), `${tag}: 加速の弧 ${a.arc}/${a.arcN}`)
  ok(a.ice === (st === 1 ? a.iceN : 0), `${tag}: 氷の帯 ${a.ice}/${a.iceN}`)
  ok(a.turn === (st === 2 ? a.turnN : 0), `${tag}: 回転盤の部品 ${a.turn}/${a.turnN}`)
  ok(a.bgN >= 10 && a.bg === a.bgN && a.bgOK === 1, `${tag}: 背景（観覧席・壁・梁・縦穴・照明）の部品 ${a.bg}/${a.bgN} 色=${a.bgOK}`)
  await wait(1600)
  await p.screenshot({ path: `shots/arena_${String(i + 1).padStart(2, '0')}_s${st}.png` })
  console.log(`${NG.length ? '..' : 'ok'} ${tag} bad=${a.bad} pieces=${a.pOk}/${a.pieces} turn=${a.turn}/${a.turnN} arc=${a.arc} ice=${a.ice}`)
  // 次の試合の前に荒らす: 崩落・穴を起こし、ついでに自分と相手を場外へ
  await p.waitForFunction(() => window.KB.state.ph === 'fight', null, { timeout: 30000 })
  await send(p, 'timescale|6'); await send(p, 'fightt|120'); await send(p, 'pitstart'); await send(p, 'cpuoff|0')
  await wait(i % 3 === 0 ? 5000 : 2500)
  await send(p, 'timescale|1')
  // 崩れている最中・崩れた後に、崩れた所へ床の模様が残っていないか（ghost）
  await p.evaluate(() => { KB.events = KB.events.filter(e => e.k !== 'arena') }); await send(p, 'arenacheck')
  await p.waitForFunction(() => window.KB.events.some(e => e.k === 'arena'), null, { timeout: 5000 })
  const g = await p.evaluate(() => window.KB.events.filter(e => e.k === 'arena').pop())
  ok(g.ghost === 0 && g.bad === 0, `${tag}: 崩れた後の模様の残り ${g.ghost}・頂点のずれ ${g.bad}`)
}
ok(errs.length === 0, 'コンソールエラー ' + errs.join(' | ').slice(0, 200))
console.log(NG.length ? `NG ${NG.length}（${N}試合）` : `ALL OK（${N}試合・NG 0）`); await b.close(); process.exit(NG.length ? 1 : 0)
