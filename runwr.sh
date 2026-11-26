#!/bin/bash
# 使い方: ./runwr.sh 出力ファイル パッシブ... （順に回す）
out=$1; shift
for p in "$@"; do node winrate.mjs $p 14 >> $out 2>&1; done
