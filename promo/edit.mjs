// capture.mjs で撮ったコマと記録から、宣伝用の縦動画（1080x1920・30fps・H.264+AAC）を作る。node promo/edit.mjs
// 先に capture.mjs と cards.mjs を回す。ブラウザは開かない（音も出ない）
// 編集はカット・早送り・字幕1つ・最後の一枚だけ: 題名＋字幕 → ホーム → 出撃 → 等速 → 4倍速 → 見どころ4つは等速 → 結果（レベルアップ）→ 最後の一枚
// 音は後付け: 記録した時刻にゲームの録音素材（BGM と効果音）を重ねる
import { execFileSync, spawnSync } from 'node:child_process'
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs'
const ROOT = import.meta.dirname + '/../unity/Assets/WebGLTemplates/Koma/'
const OUT = process.env.KP_OUT || '/private/tmp/claude-501/-Users-tomoki-Desktop-tmk-hub/9116bcd6-5450-4eca-92c7-248c73c1167b/scratchpad/kp-video'
const RUN = process.env.KP_RUN || 'take1'
const DIR = `${OUT}/${RUN}`, CARDS = `${OUT}/cards`, WORK = `${DIR}/work`; mkdirSync(WORK, { recursive: true })
const FINAL = process.env.KP_FINAL || `${import.meta.dirname}/ジャイロ・ラグナロク_プレイ動画_20261126.mp4`
const ff = (args, o = {}) => execFileSync('ffmpeg', ['-hide_banner', '-v', 'error', '-y', ...args], { stdio: ['ignore', 'pipe', 'pipe'], maxBuffer: 1 << 26, ...o })
const L = JSON.parse(readFileSync(`${DIR}/log.json`, 'utf8'))
const t0 = L.frames[0].t, sec = t => (t - t0) / 1000
const M = n => sec(L.marks.find(m => m.name === n).t)
const rawEnd = M('end')

let list = 'ffconcat version 1.0\n'
L.frames.forEach((f, i) => { const next = i + 1 < L.frames.length ? sec(L.frames[i + 1].t) : rawEnd + 0.5; list += `file '${DIR}/frames/${f.f}'\nduration ${Math.max(0.001, next - sec(f.t)).toFixed(4)}\n` })
list += `file '${DIR}/frames/${L.frames.at(-1).f}'\n`
writeFileSync(`${WORK}/frames.txt`, list)
if (!process.env.KP_SKIP_RAW) ff(['-f', 'concat', '-safe', '0', '-i', `${WORK}/frames.txt`, '-vf', 'fps=60,scale=1080:1920:in_color_matrix=bt601:in_range=full:out_color_matrix=bt709:out_range=tv,format=yuv420p', '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '14', `${WORK}/raw.mp4`])

const FASTX = 4, END_SEC = 2.5
const segs = [
  { name: 'title', a: 0.2, b: M('toHub'), sp: 1 },
  { name: 'hub', a: M('toHub'), b: M('toFight'), sp: 1 },
  { name: 'count', a: M('toFight'), b: M('fast1'), sp: 1 },
  { name: 'fast1', a: M('fast1'), b: M('h1'), sp: FASTX },
  { name: 'h1', a: M('h1'), b: M('h2'), sp: 1 },
  { name: 'h2', a: M('h2'), b: M('h3'), sp: 1 },
  { name: 'h3', a: M('h3'), b: M('h4'), sp: 1 },
  { name: 'h4', a: M('h4'), b: M('fin'), sp: 1 },
  { name: 'fin', a: M('fin'), b: M('result'), sp: 1 },
  { name: 'result', a: M('result'), b: M('end'), sp: 1 },
]
let acc = 0; for (const s of segs) { s.o = acc; s.len = (s.b - s.a) / s.sp; acc += s.len }
const endAt = acc, TOTAL = +(acc + END_SEC).toFixed(3)
const map = t => { for (const s of segs) if (t >= s.a && t < s.b) return { o: s.o + (t - s.a) / s.sp, s }; return null }
console.log(segs.map(s => `${s.name} ${s.o.toFixed(1)}-${(s.o + s.len).toFixed(1)}`).join(' | '), `| end ${endAt.toFixed(1)}-${TOTAL}`)

const capIn = 0.3, capOut = Math.min(segs[0].len - 0.1, 2.4)
let fc = segs.map((s, i) => `[0:v]trim=${s.a.toFixed(3)}:${s.b.toFixed(3)},setpts=(PTS-STARTPTS)/${s.sp},fps=30[v${i}]`).join(';')
fc += `;[2:v]fps=30,format=yuv420p,setsar=1[ve];${segs.map((_, i) => `[v${i}]`).join('')}[ve]concat=n=${segs.length + 1}:v=1:a=0[vc]`
fc += `;[1:v]format=rgba,fade=in:st=${capIn}:d=0.2:alpha=1,fade=out:st=${(capOut - 0.2).toFixed(2)}:d=0.2:alpha=1[cap];[vc][cap]overlay=0:0:enable='between(t,${capIn},${capOut.toFixed(2)})',format=yuv420p[v]`
ff(['-i', `${WORK}/raw.mp4`, '-loop', '1', '-t', String(capOut + 0.5), '-i', `${CARDS}/caption1.png`, '-loop', '1', '-t', String(END_SEC), '-i', `${CARDS}/endcard.png`,
  '-filter_complex', fc, '-map', '[v]', '-t', String(TOTAL), '-r', '30', '-c:v', 'libx264', '-preset', 'slow', '-crf', '17', '-profile:v', 'high', '-pix_fmt', 'yuv420p',
  '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709', '-color_range', 'tv', `${WORK}/video.mp4`])

// ---- 音
const SR = 48000
const ins = [], parts = []
let ai = 0
const add = (file, pre, atSec, vol, rate = 1, extra = '') => {
  const i = ai++; ins.push(...pre, '-i', file)
  const post = rate !== 1 ? `,asetrate=${Math.round(SR * rate)},aresample=${SR}` : ''
  parts.push(`[${i}:a]aresample=${SR},aformat=sample_fmts=fltp:channel_layouts=stereo${post}${extra},volume=${vol.toFixed(3)},adelay=${Math.max(0, Math.round(atSec * 1000))}:all=1[a${i}]`)
}
// BGM: ゲームと同じ（'go' の時に鳴り出し、結果で止まる）。結果の時刻は記録の loop null
const bgmStart = L.sndLog.find(s => s.k === 'loop' && s.slot === 'bgm' && s.url)
const bgmStop = L.sndLog.find(s => s.k === 'loop' && s.slot === 'bgm' && !s.url)
if (bgmStart) {
  const m = map(sec(bgmStart.t)); const stopO = bgmStop ? (map(sec(bgmStop.t)) || { o: endAt }).o : endAt
  const len = stopO - m.o
  add(ROOT + bgmStart.url, ['-stream_loop', '-1', '-t', String(len + 0.4)], m.o, bgmStart.vol, 1, `,atrim=0:${len.toFixed(2)},afade=in:st=0:d=0.6,afade=out:st=${Math.max(0, len - 0.6).toFixed(2)}:d=0.6`)
}
// 効果音: 等速の所は記録どおり全部。早送りの所は鳴らさない
let nSfx = 0
for (const s of L.sndLog.filter(s => s.k === 'play' && s.url)) {
  const m = map(sec(s.t)); if (!m || m.s.sp !== 1) continue
  add(ROOT + s.url, [], m.o + s.delay, s.vol, s.rate); nSfx++
}
const mix = parts.join(';') + `;${parts.map((_, i) => `[a${i}]`).join('')}amix=inputs=${parts.length}:normalize=0:duration=longest,atrim=0:${TOTAL},apad=whole_dur=${TOTAL}[m]`
writeFileSync(`${WORK}/mix_filter.txt`, mix)
ff([...ins, '-filter_complex_script', `${WORK}/mix_filter.txt`, '-map', '[m]', '-c:a', 'pcm_s24le', '-ar', String(SR), `${WORK}/mix_raw.wav`])
const lnOut = spawnSync('ffmpeg', ['-hide_banner', '-i', `${WORK}/mix_raw.wav`, '-af', 'loudnorm=print_format=json', '-f', 'null', '-'], { encoding: 'utf8' }).stderr
const meas = JSON.parse(lnOut.slice(lnOut.lastIndexOf('{'), lnOut.lastIndexOf('}') + 1))
const I = +meas.input_i, TP = +meas.input_tp
const gain = Math.min(-16 - I, -1.5 - TP + 4)
console.log(`audio: ${parts.length} tracks (sfx ${nSfx}), in ${I} LUFS / TP ${TP} dB, gain ${gain.toFixed(1)} dB`)
ff(['-i', `${WORK}/mix_raw.wav`, '-af', `volume=${gain.toFixed(2)}dB,alimiter=limit=0.84:attack=3:release=60:level=disabled`, '-c:a', 'pcm_s24le', `${WORK}/mix.wav`])
ff(['-i', `${WORK}/video.mp4`, '-i', `${WORK}/mix.wav`, '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-ar', String(SR), '-t', String(TOTAL), '-movflags', '+faststart', FINAL])
writeFileSync(`${WORK}/timeline.json`, JSON.stringify({ segs, endAt, TOTAL, gain, I, TP }, null, 1))
console.log('done', FINAL, TOTAL + 's')
