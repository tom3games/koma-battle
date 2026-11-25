// 画面が使う音のファイルが公開先に全部あるか（2026-11-25 BGM が gitignore で抜けて iOS で警告が出た）
import { readFileSync } from 'node:fs'
const base = process.argv[2] || 'https://tom3games.github.io/koma-battle/'
const js = readFileSync('unity/Assets/WebGLTemplates/Koma/TemplateData/ui.js', 'utf8')
const names = [...new Set([...js.matchAll(/S\('([^']+)'\)/g)].map(m => 'TemplateData/snd/' + m[1] + '.mp3').concat([...js.matchAll(/'(TemplateData\/snd\/[^']+\.mp3)'/g)].map(m => m[1])))]
let bad = 0
for (const n of names) { const r = await fetch(base + n + '?t=' + Date.now(), { method: 'HEAD' }); if (r.status !== 200) { bad++; console.log('NG', r.status, n) } }
console.log('snd files', names.length, 'NG', bad)
