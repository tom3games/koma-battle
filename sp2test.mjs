// 2026-11-28 必殺4つ・中央の崩落・メテオの範囲・入力の逆から当てられた時の弾かれ の数字の検査
// node sp2test.mjs <arrow|anchor|chain|spiral|pit|meteor|runhit|all> [url]
import { begin, wait, send, dist } from './sp2lib.mjs'
const which = process.argv[2] || 'all'
const url = process.argv[3] || 'http://localhost:5393/'
const NG = []; const ok = (c, m) => { console.log((c ? 'OK ' : 'NG ') + m); if (!c) NG.push(m) }
const want = n => which === 'all' || which === n
const f1 = x => +(+x).toFixed(2)
const sp = (k) => Math.hypot(k.vx, k.vz)

// ---- 氷の矢 ----
if (want('arrow')) {
  for (const mode of ['hit', 'dodge']) {
    const o = await begin(url, { sp: 6 })
    const hill = process.env.STAGE === '1', PX = hill ? -4 : -8, VX = hill ? 4 : 6   // 丘は相手が坂で流れるので、8m先で確かめる
    await o.place(0, PX, 0); await o.place(1, VX, 0); await wait(1500)
    await send(o.p, 'gauge|100'); await o.clear()
    const t0 = await o.tm()
    await send(o.p, `spfire|0,${VX},0`)
    await o.until(() => window.KB.events.filter(e => e.k === 'arrowring').length >= 2, 8000)
    if (mode === 'dodge') await o.place(1, VX, 4.8, 0, 0, 2.5)   // 輪が開いている間に横へ逃げた
    await wait(2500)
    const fz = (await o.evs('freeze')).filter(e => e.i === 1).length
    const rings = (await o.evs('arrowring')).length
    const k1 = await o.k(1), k0 = await o.k(0)
    console.log(mode, JSON.stringify({ rings, freeze: fz, victimPos: [f1(k1.wx), f1(k1.wz)], mePos: [f1(k0.wx), f1(k0.wz)] }))
    ok(rings === 5, `${mode}: 輪が5個順に開いた (${rings})`)
    if (mode === 'hit') ok(fz === 1, `当たると凍える (${fz})`); else ok(fz === 0, `展開中に横へ逃げれば当たらない (${fz})`)
    await o.b.close()
  }
  // 自分が凍える側: CPU1 が撃つ
  const o = await begin(url, { sp: 0 })
  await o.place(0, -12, 0); await o.place(1, 12, 12); await wait(1500)
  await send(o.p, 'frost|2,0'); await o.clear()
  const tF = await o.tm()
  await wait(400)
  await send(o.p, 'inmv|0,1')   // 凍えたまま走る
  await o.monReset()
  let maxFrozen = 0, maxAfter = 0, tEnd = null
  for (let i = 0; i < 400; i++) {
    await wait(30)
    const k = await o.k(0)
    if (k.fz) { maxFrozen = Math.max(maxFrozen, (await o.mon())[0].spmax) } else { tEnd = await o.tm(); break }
  }
  await send(o.p, 'inmv'); await o.place(0, -12, -6); await wait(800); await send(o.p, 'inmv|0,1'); await o.monReset(); await wait(5000)
  maxAfter = (await o.mon())[0].spmax
  console.log('frost', JSON.stringify({ tFreeze: f1(tF), tMelt: tEnd && f1(tEnd), meltAfterSec: tEnd && f1(tEnd - tF), maxFrozen: f1(maxFrozen), maxAfter: f1(maxAfter) }))
  ok(tEnd && tEnd - tF > 1.6 && tEnd - tF < 2.6, `凍えは約2秒 (${tEnd && f1(tEnd - tF)})`)
  ok(maxFrozen < maxAfter * 0.75, `凍えている間は遅い (${f1(maxFrozen)} < ${f1(maxAfter)})`)
  await o.b.close()
}

// ---- アンカー ----
if (want('anchor')) {
  const res = {}
  for (const mode of ['normal', 'anchor']) {
    const o = await begin(url, { sp: 4 })
    await o.place(0, -10, 0); await o.place(1, -4, 0); await wait(1500)
    await send(o.p, 'gauge|100'); await o.clear()
    if (mode === 'anchor') { await send(o.p, 'special'); await wait(500) }
    await o.monReset()
    await o.place(1, -4.5, 0, -14, 0, 0.3)   // 相手が自分へ向かって速く当たる（-x 向き）
    await wait(6000)
    const m = await o.mon()
    res[mode] = { meMoved: f1(m[0].dmax), cpuRebound: f1(m[1].vxmax) }
    await o.b.close()
  }
  console.log('anchor', JSON.stringify(res))
  ok(res.anchor.meMoved < res.normal.meMoved * 0.4, `アンカー中は当てられても動かない (${res.anchor.meMoved} < ${res.normal.meMoved})`)
  ok(res.anchor.cpuRebound > res.normal.cpuRebound * 1.3, `当てた方がより弾かれる (${res.anchor.cpuRebound} > ${res.normal.cpuRebound})`)
}

// ---- チェイン ----
if (want('chain')) {
  const out = {}
  for (const mode of ['still', 'orbit']) {
    const o = await begin(url, { sp: 7 })
    await o.place(0, -10, 0); await o.place(1, -5.5, 0); await wait(1500)
    await send(o.p, 'gauge|100'); await o.clear()
    await send(o.p, 'special')   // 一番近い相手へ
    let thrown = null, t = 0, tug = 0
    for (let i = 0; i < 120 && !thrown; i++) {
      await wait(70)
      if (mode === 'orbit') {
        const k0 = await o.k(0), k1 = await o.k(1)
        const rx = k0.wx - k1.wx, rz = k0.wz - k1.wz, rl = Math.hypot(rx, rz) || 1
        const tx = -rz / rl, tz = rx / rl, rad = (rl - 5.8) * 0.6
        await send(o.p, `inmv|${tx - rx / rl * rad},${tz - rz / rl * rad}`)
      }
      thrown = (await o.evs('chainthrow'))[0]
    }
    await send(o.p, 'inmv')
    let peak = 0
    for (let i = 0; i < 10; i++) { await wait(50); peak = Math.max(peak, sp(await o.k(1))) }
    out[mode] = { chain: (await o.evs('chain')).length, throwSpeed: thrown && f1(thrown.p), peakVictim: f1(peak) }
    await o.b.close()
  }
  console.log('chain', JSON.stringify(out))
  ok(out.still.chain === 1 && out.still.throwSpeed != null, `鎖がつながり、最後に投げる (${JSON.stringify(out.still)})`)
  ok(out.orbit.throwSpeed > out.still.throwSpeed + 4, `回るほど遠くへ投げる (${out.orbit.throwSpeed} > ${out.still.throwSpeed})`)
  // 引っ張り合い: つながれた側（CPU1）がダッシュで逆に引く
  const o = await begin(url, { sp: 7 })
  await o.place(0, -10, 0); await o.place(1, -5.5, 0); await wait(1500)
  await send(o.p, 'gauge|100'); await o.clear(); await send(o.p, 'special'); await wait(400)
  const a = await o.k(0)
  await send(o.p, 'dashat|1,5,0')   // 相手側が鎖の向きと逆へダッシュ
  await wait(600)
  const b = await o.k(0)
  console.log('tug', JSON.stringify({ tugEv: (await o.evs('tug')).length, meMovedX: f1(b.wx - a.wx) }))
  ok((await o.evs('tug')).length >= 1, '引っ張られた側のダッシュで引っ張り合いが起きる')
  // 第三者
  await o.b.close()
}

// ---- ラスト・スパイラル ----
if (want('spiral')) {
  const o = await begin(url, { sp: 5 })
  await o.place(0, -10, 0); await o.place(1, -3, 0); await wait(1500)
  await send(o.p, 'gauge|100'); await o.clear()
  await send(o.p, 'spfire|0,-3,0'); await wait(300)
  const d1 = (await o.evs('deny')).length, g = (await o.k(0))
  ok(d1 >= 1, `回転が多い時は撃てない・拒否の表示 (deny=${d1})`)
  ok((await o.evs('special')).length === 0, '撃てない時は技が出ない')
  const rows = []
  for (const r of [0.28, 0.15, 0.04]) {
    await send(o.p, `spin|${r},0`); await send(o.p, 'spin|1,1'); await send(o.p, 'gauge|100')
    await o.place(0, -10, 0); await o.place(1, -3, 0); await wait(1200); await o.clear()
    const v0 = await o.k(1); await send(o.p, 'spin|' + r + ',0'); await send(o.p, 'spfire|0,-3,0')
    await o.until(() => window.KB.events.some(e => e.k === 'boom'), 6000)
    const pw = (await o.evs('spiralgo'))[0]
    await wait(600)
    const v1 = await o.k(1), me = await o.k(0)
    rows.push({ spin: r, power: pw && f1(pw.p), victimLoss: f1((v0.sp - v1.sp) * 100) + '%', victimMoved: f1(Math.hypot(v1.wx - v0.wx, v1.wz - v0.wz)) })
  }
  console.log('spiral', JSON.stringify(rows))
  ok(rows[0].power < rows[1].power && rows[1].power < rows[2].power, `回転が少ないほど威力が上がる (${rows.map(r => r.power)})`)
  await o.b.close()
}

// ---- 中央の崩落 ----
if (want('pit')) {
  const o = await begin(url, { sp: 0, ts: 3 })
  await o.place(0, -9, 0); await o.place(1, 9, 0); await wait(1500)
  await send(o.p, 'pitstart')
  const t0 = await o.tm()
  for (let i = 0; i < 400 && !(await o.evs('pitfall')).length; i++) { await o.place(0, -9, 0, 0, 0, 0.5); await o.place(1, 9, 0, 0, 0, 0.5); await wait(150) }
  const steps = (await o.evs('crackstep')).length, tf = await o.tm()
  console.log('pit', JSON.stringify({ crackSteps: steps, fallAfterGameSec: f1(tf - t0) }))
  ok(steps >= 4, `ひびが何度も進む音 (${steps})`); ok(tf - t0 > 7 && tf - t0 < 12, `ひびから崩れるまで約9秒 (${f1(tf - t0)})`)
  // 穴に落ちると別の理由名で負け
  await o.clear(); await send(o.p, 'cpuoff|1')
  await o.place(1, 0.5, 0, 0, 0, 1.0)
  await o.until(() => window.KB.events.some(e => e.k === 'ko'), 8000)
  const ko = (await o.evs('ko'))[0]
  console.log('ko', JSON.stringify(ko && { i: ko.i, why: ko.why }))
  ok(ko && ko.why === 'pit' && ko.i === 1, `穴に落ちた理由は pit (${ko && ko.why})`)
  await o.b.close()
}

// ---- メテオの範囲 ----
if (want('meteor')) {
  const out = {}
  for (const [label, dx] of [['near7m', 7], ['far12m', 12]]) {
    const o = await begin(url, { sp: 0 })
    await o.place(0, -9, 0); await o.place(1, -9 + dx, 0); await wait(1500)
    await send(o.p, 'gauge|100'); await o.clear()
    const s0 = await o.k(0), c0 = await o.k(1)
    await send(o.p, `spfire|0,${c0.wx},${c0.wz}`)
    await o.until(() => window.KB.events.some(e => e.k === 'boom'), 8000)
    const m = await o.k(0)
    const landFromStart = m.wx - s0.wx, toCpu = Math.hypot(m.wx - c0.wx, m.wz - c0.wz)
    out[label] = { landForwardFromStart: f1(landFromStart), distToCpu: f1(toCpu) }
    await o.b.close()
  }
  console.log('meteor', JSON.stringify(out))
  ok(out.near7m.distToCpu < 2.5, `8m 以内の相手は追う (${out.near7m.distToCpu})`)
  ok(out.far12m.landForwardFromStart > 1.5 && out.far12m.landForwardFromStart < 4.5 && out.far12m.distToCpu > 6, `12m 先は追わず少し前へ落ちる (前へ ${out.far12m.landForwardFromStart}m・相手まで ${out.far12m.distToCpu}m)`)
  // 穴の手前へずらす
  const o = await begin(url, { sp: 0, ts: 3 })
  await o.place(0, -5.5, 0); await o.place(1, 12, 6); await wait(1500)
  await send(o.p, 'pitstart')
  for (let i = 0; i < 400 && !(await o.evs('pitfall')).length; i++) { await o.place(0, -9, 0, 0, 0, 0.5); await o.place(1, 12, 6, 0, 0, 0.5); await wait(150) }
  await send(o.p, 'timescale|1'); await o.place(0, -5.6, 0, 0, 0, 0.5); await wait(900); await send(o.p, 'gauge|100'); await o.clear()
  await send(o.p, 'spfire|0,12,0')
  await o.until(() => window.KB.events.some(e => e.k === 'boom'), 12000); await wait(800)
  const m = await o.k(0)
  console.log('meteor-pit', JSON.stringify({ alive: m.a, r: f1(Math.hypot(m.wx, m.wz)), why: m.why }))
  ok(m.a === 1, `穴の方へ撃っても落ちない (alive=${m.a} r=${f1(Math.hypot(m.wx, m.wz))})`)
  await o.b.close()
}

// ---- 入力の逆から当てられた時 ----
if (want('runhit')) {
  const res = {}
  for (const [label, inp, kn] of [['入力なし', null, 1.05], ['入力あり×1.0', '1,0', 1.0], ['入力あり×1.05', '1,0', 1.05]]) {
    const dm = []
    for (let rep = 0; rep < 3; rep++) {
      const o = await begin(url, { sp: 0 })
      await send(o.p, 'runknock|' + kn)
      await o.place(0, -10, 0, 0, 0, 1.0); await o.place(1, -16, 0, 0, 0, 1.0); await wait(1500)
      await o.monReset(); await o.clear()
      // 入力を入れた直後（まだ遅い・背後の角度の判定が働かない速さ）に、後ろから当てる
      await o.place(1, -11.05, 0, 13, 0, 0.95)
      if (inp) await send(o.p, 'inmv|' + inp)
      await wait(350); if (inp) await send(o.p, 'inmv')
      await wait(2800)
      const m = await o.mon()
      dm.push({ d: m[0].dmax, ev: (await o.evs('runhit')).length })
      await o.b.close()
    }
    res[label] = { moved: f1(dm.reduce((a, x) => a + x.d, 0) / dm.length), each: dm.map(x => f1(x.d)), runhitEv: dm.map(x => x.ev) }
  }
  console.log('runhit', JSON.stringify(res))
  const a = res['入力あり×1.0'], b = res['入力あり×1.05']
  ok(b.runhitEv.some(x => x >= 1), '逃げる向きの後ろから当てられた判定が働く')
  console.log('弾かれた距離の比 (×1.05 / ×1.0):', f1(b.moved / a.moved))
}
console.log(NG.length ? 'NG ' + NG.length : 'ALL OK')
process.exit(0)
