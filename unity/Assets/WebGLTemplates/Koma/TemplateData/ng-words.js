// プレイヤー名の検査。名前は外へ送らない一人用だが、配信で映るので強めにする。
// 照らし方: 全角半角（NFKC）・大文字小文字・カタカナ→ひらがな・記号と空白と長音を除く・数字や記号の読み替え（0→o 1→i 3→e 4→a 5→s @→a $→s）・同じ英字の連続を1つに、のうえで一覧と比べる
// 一覧の書き方: 'ことば' = 3文字以上は部分一致 / '=ことば' = 名前全体がその語のときだけ（短い語・名前の一部になりうる語）/ 2文字以下の日本語は 全体・先頭・末尾 のとき
// 当たった言葉は画面に出さない（「その名前は使えません」だけ）
;(() => {
  const LIST = [
    // 下品
    'うんこ', 'うんち', 'ちんこ', 'ちんぽ', 'ちんちん', 'まんこ', 'おまんこ', 'おっぱい', 'ちくび', 'おなにー', 'おなに', 'せっくす', 'ふぇら', 'ぱんつ', 'きんたま', 'ちんかす', 'しこしこ', 'ぶっかけ', 'いんもう', 'ばかやろう', 'くそやろう', 'くそったれ', 'くそが', 'くたばれ', 'うざい', 'きもい', 'きしょい', 'ぶす', 'ばか', 'あほ', 'くそ', '=かす', '=ぼけ', '=はげ', '=でぶ', '=えっち', '=えろ', 'えろい', 'えろす', 'えろげ',
    'fuck', 'fuk', 'fck', 'bitch', 'asshole', 'bastard', 'pussy', 'penis', 'vagina', 'boob', 'porn', 'dildo', 'cumshot', 'whore', 'slut', 'nigger', 'nigga', 'faggot', 'retard', '=sex', '=ass', '=cock', '=dick', '=cum', '=anal', '=tits', '=shit', '=kill', '=die', '=rape', '=nazi', '=suck', '=cunt', '=baka', '=aho', '=kuso', '=unko', 'chinko', 'chinpo', 'manko', 'oppai', 'kutabare',
    // 性的
    'やりまん', 'やりちん', 'いんらん', 'ちかん', 'れいぷ', 'ろりこん', 'しょたこん', 'らぶほ', 'ふうぞく', 'ばいしゅん', 'えんこう', 'ごうかん', 'じゅんじょ', 'ひがい', 'せいき', 'ほうけい', 'どうてい', 'しょじょ', 'ちつ', 'ざーめん', 'ふぁっく', 'ぽるの', 'えいぶい', '売春', '援交', '強姦', '痴漢', '童貞', '処女', '性器', '包茎', '中出し', '精子', '乱交', '変態',
    // 差別
    'きちがい', 'きちげえ', 'めくら', 'つんぼ', 'かたわ', 'ちょんこ', 'ちゃんころ', 'しなじん', 'ちょうせんじん', 'こくじん', 'がいじ', 'ふぐしゃ', 'ぶらくみん', 'ひにん', 'せんじん', 'じゃっぷ', '=えた', '=ちょん', '=しな', '=ぱい', '=ごき', '=がいじ', '=ばいた', '=びっこ', '=ちび', 'ほんもの', '外人', '黒人', '支那', '在日', '朝鮮人', '障害者', '気違い', '白痴', '乞食', '部落', '穢多', '非人', '池沼', '知恵遅れ', 'jap', 'chink', 'gook', 'spic', 'kike', 'negro', 'homo', 'tranny', 'hitler', 'ヒトラー', 'ナチス', 'ネトウヨ', 'ぱよく', 'さよく',
    // 暴力・犯罪・自傷
    'ころすぞ', 'ころしてやる', 'ころされ', 'ぶっころ', 'ころしあい', 'ころしや', 'さつじん', 'さつがい', 'ぎゃくさつ', 'ごうとう', 'ゆうかい', 'ばくだんま', 'てろりすと', 'じさつ', 'しにたい', 'しんでしまえ', 'しんじまえ', 'しんでくれ', 'しねよ', 'しねや', 'しんじゃえ', 'しんでね', 'じさ', 'ほうか', 'ふんしん', 'りんち', 'ちかんでん', 'どろぼう', 'さぎし', 'さぎ', '=しね', '=ころす', '=ころせ', '=てろ', '=さつ', '=ばくだん', '死ね', '氏ね', '市ね', '殺す', '殺せ', '殺し', '殺人', '殺害', '虐殺', '自殺', '自害', '爆破', '誘拐', '強盗', '放火', '拷問', 'ちね', 'タヒ', 'murder', 'suicide', 'terror', 'killyou', 'killer', 'bomber', 'massacre', 'torture', 'kidnap', 'ころ', 'こr',
    // 薬物
    'たいま', 'まりふぁな', 'まやく', 'かくせいざい', 'かくせいき', 'こかいん', 'へろいん', 'しゃぶちゅう', 'しゃぶ', 'あへん', 'けしのみ', 'ごうほう', 'だつほう', 'しんなー', 'ぐりーんりーふ', 'とばす', '=らりる', '=ぱけ', '=やく', '=ぶつ', '=はっぱ', '=あたま', '=がんじゃ', '=ぼん', '大麻', '麻薬', '覚醒剤', '覚せい剤', '覚せいざい', '危険ドラッグ', '脱法', '合法ハーブ', 'cocaine', 'heroin', 'marijuana', 'cannabis', 'opium', 'ecstasy', 'amphetamine', 'methamphetamine', '=meth', '=lsd', '=mdma', '=weed', '=crack', '=drug', '=drugs',
    // 運営・公式を名乗る
    'うんえい', 'こうしき', 'かんりにん', 'かんりしゃ', 'かいはつしゃ', 'さぽーと', 'じむきょく', 'ぷろでゅーさー', 'ともきしょうてん', 'tom3', 'tomoki', 'どうがいし', 'へるぷ', 'administrator', 'moderator', 'official', 'developer', 'support', 'operator', 'sysadmin', 'webmaster', 'owner', 'admin', '=gm', '=gamemaster', '=mod', '=staff', '=system', '=bot', '=null', '=none', '=root', '=sys', '=dev', '=cpu', '=npc', '=guest', '=player', '=you', '=me', '運営', '公式', '管理人', '管理者', '開発者', '開発元', '事務局', '運営者', '公認', '本人', '中の人', 'しすてむ', '=げーむますたー', '=じーえむ', '=えーじーえむ', 'げーむますたー'
  ]
  // 名前として自然に使う語をうっかり弾く物を除く（上の一覧の「ころ」「こr」「ほんもの」「しな」「ぱい」などは、近い語が普通の名前に入るため外す）
  const DROP = new Set(['ころ', 'こr', 'ほんもの', '=しな', '=ぱい', '=かす', '=ぼけ', '=はげ', '=でぶ', '=ぶつ', '=あたま', '=ぼん', '=さつ', '=やく', '=ちび', 'じさ', 'さぎ', 'ひがい', 'せいき', 'ほうけい', 'じゅんじょ', 'ほうか', 'しゃぶ', 'とばす', 'ごうほう', 'だつほう', 'へるぷ', 'さぽーと', 'ともきしょうてん', 'tomoki', 'owner', 'support', '=you', '=me', '=player', '=guest', '=none', '=null', '=cpu', '=npc', '=bot', '=dev', '=sys', '=root', '=mod', '=crack', '=drug', '=drugs', '=suck', '=die', '=cum', '=anal', '=tits', '=ass', '=cock', '=dick', '=sex', '=kill', '=rape', '=nazi', '本人', '中の人', '外人', '黒人', 'こくじん', 'せんじん', 'ひにん', 'がいじ', 'ちかんでん', 'ばくだんま', 'ごうとう', 'ゆうかい', 'どろぼう', 'ぱよく', 'さよく', 'ぱんつ', 'ちんちん', 'うざい', 'きもい', 'きしょい', 'ぶす', 'ふうぞく', 'ちつ', 'ばいしゅん', 'えんこう', 'ふぁっく', 'killer', 'bomber', 'terror', 'jap', 'homo', 'あへん', 'けしのみ', 'ぐりーんりーふ', 'しんなー', 'ぶっかけ', 'らぶほ', 'しょたこん', 'ろりこん', 'どうてい', 'しょじょ'])
  // ↑ 外した物のうち、必ず弾きたい物は下で別に足す
  const ADD = ['=ばいた', 'ひにん', 'こくじん', 'ぱんつ', 'ちんちん', 'ばいしゅん', 'えんこう', 'ふぁっく', 'らぶほ', 'しょたこん', 'ろりこん', 'どうてい', 'しょじょ', 'ふうぞく', '=sex', '=ass', '=cock', '=dick', '=cum', '=anal', '=tits', '=kill', '=die', '=rape', '=nazi', '=suck', '=crack', 'killer', 'terror', '=jap']
  const norm = s => {
    let t = String(s).normalize('NFKC').toLowerCase()
    t = t.replace(/[ァ-ヶ]/g, c => String.fromCharCode(c.charCodeAt(0) - 0x60))
    t = t.replace(/[0@]/g, 'o').replace(/[1!|]/g, 'i').replace(/3/g, 'e').replace(/4/g, 'a').replace(/[5$]/g, 's')
    t = t.replace(/[^\p{L}\p{N}]/gu, '').replace(/[ーｰ]/g, '')
    return t
  }
  const collapse = t => t.replace(/([a-z])\1+/g, '$1')
  const isLatin = w => /^[a-z0-9]+$/.test(w)
  const rules = []
  for (const raw of LIST.filter(w => !DROP.has(w)).concat(ADD)) {
    const exact = raw[0] === '='; const w = norm(exact ? raw.slice(1) : raw); if (!w) continue
    rules.push({ w, exact, latin: isLatin(w) })
  }
  const hit = (t, r) => {
    if (r.exact) return t === r.w
    if (r.w.length >= 3) return t.includes(r.w)
    if (r.latin) return t === r.w
    return t === r.w || t.startsWith(r.w) || t.endsWith(r.w)
  }
  const NG = {
    // 名前に使えない言葉が含まれるか
    bad(name) {
      const t = norm(name); if (!t) return false
      const c = collapse(t)
      for (const r of rules) if (hit(t, r) || hit(c, r)) return true
      return false
    },
    // 絵文字・制御文字・改行を外し、前後の空白を削り、全角8（半角16）までに切る
    clean(name) {
      let s = String(name == null ? '' : name).normalize('NFC')
      s = s.replace(/[\u0000-\u001f\u007f-\u009f\u200b-\u200f\u2028-\u202f\u2060\ufeff\ufe0e\ufe0f\u20e3]/g, '')
      s = s.replace(/\p{Extended_Pictographic}|\p{Emoji_Presentation}|\p{Regional_Indicator}|[\u{1F3FB}-\u{1F3FF}]/gu, '')
      s = s.replace(/[ 　]+/g, ' ').trim()
      let w = 0, out = ''
      for (const ch of s) { const cw = /[ -~｡-ﾟ]/.test(ch) ? 1 : 2; if (w + cw > 16) break; w += cw; out += ch }
      return out.trim()
    },
  }
  window.NG = NG
})()
