#!/bin/sh
# Unity の Web 書き出し（バッチモード）。使い方: ./build.sh [gzip|none|brotli]
cd "$(dirname "$0")"
S=${SCRATCH:-${TMPDIR:-/tmp}}
U=/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity
T0=$(date +%s)
"$U" -batchmode -nographics -quit -projectPath "$PWD/unity" -buildTarget WebGL -executeMethod Build.WebGL -out "$PWD/docs" -compression "${1:-gzip}" -template PROJECT:Koma -logFile "$S/build.log"
RC=$?
echo "exit=$RC sec=$(( $(date +%s) - T0 ))"
grep -E "KOMA_BUILD|error CS|Error building|KOMA no" "$S/build.log" | head -30
exit $RC
