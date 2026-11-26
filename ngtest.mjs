import fs from 'node:fs'
globalThis.window = {}
new Function('window', fs.readFileSync(process.argv[2] || 'unity/Assets/WebGLTemplates/Koma/TemplateData/ng-words.js', 'utf8'))(globalThis.window)
const NG = window.NG
const bad = ['ば か', 'ﾊﾞｶ', 'BAKA', 'バカ', 'ばーか', 'うんこ', 'ウンコ', 'ｳﾝｺ', 'ち ん こ', 'FUCK', 'f u c k', 'fuuuck', 'Sh1t', 'ころすぞ', '殺す', '死ね', 'し ね', '氏ね', '運営', '公式', 'Admin', 'GM', 'ＧＭ', 'OFFICIAL', '管理人', '大麻', '覚醒剤', 'まんこ', 'セックス', 'sex', 'ナチス', 'きちがい', 'a.d.m.i.n', 'バカ太郎', 'ぷろでゅーさー', '強姦', 'ヘロイン', 'cocaine', 'ち*ん*こ', 'ＡＤＭＩＮ']
const ok = ['たまき', 'しのぶ', 'Hiroshi', 'ひろし', 'さくら', 'Tomo', 'ゆうた', 'まさし', '太郎', 'Mike', 'Cassie', 'Hassan', 'Shitaro', 'ころすけ', 'えっちゃん', 'シネマ', 'はなこ', 'ダイキ', 'ジャイロ', 'コマ王', '紅蓮', 'ルナ', 'Kuro', 'アスカ', 'Sasuke', 'Class', 'Assassin', 'Essex', 'Hancock', 'Dickson', 'Skill', 'ミカエル', 'まんまる', 'こまちゃん', 'ちんあなご', 'タケシ', 'Kenji', '山田', 'そら', 'りんご', 'Ayumi', 'Moderne', 'ぱんだ', 'あかね']
let ng = 0, og = 0
for (const n of bad) { const r = NG.bad(n); if (!r) { console.log('見逃し', n); ng++ } }
for (const n of ok) { const r = NG.bad(n); if (r) { console.log('誤判定', n); og++ } }
const cl = [['  たろう  ', 'たろう'], ['あ\nい', 'あい'], ['ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'ABCDEFGHIJKLMNOP'], ['あいうえおかきくけ', 'あいうえおかきく'], ['😀たろう', 'たろう'], ['ﾀﾛｳ', 'ﾀﾛｳ']]
let cg = 0
for (const [a, b] of cl) { const r = NG.clean(a); if (r !== b) { console.log('整形NG', JSON.stringify(a), JSON.stringify(r)); cg++ } }
console.log(`NG ${bad.length}件 見逃し ${ng} / OK ${ok.length}件 誤判定 ${og} / 整形 ${cl.length}件 不一致 ${cg}`)
process.exit(ng + og + cg ? 1 : 0)
