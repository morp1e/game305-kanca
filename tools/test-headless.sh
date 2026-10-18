#!/usr/bin/env bash
# Unity testlerini, açık editöre dokunmadan ayrı bir kopyada çalıştırır.
# Kullanım: tools/test-headless.sh [PlayMode|EditMode] [kopya-dizini]
# Kopya ilk seferde git worktree olarak açılır ve Library ana projeden kopyalanır (hızlı açılış için).
set -euo pipefail
PLATFORM="${1:-PlayMode}"
REPO="$(cd "$(dirname "$0")/.." && pwd)"
COPY="${2:-$REPO/../game305-kanca-headless}"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe}"

if [ ! -d "$COPY" ]; then
  git -C "$REPO" worktree add -q --detach "$COPY" HEAD
  robocopy "$(cygpath -w "$REPO/Library")" "$(cygpath -w "$COPY/Library")" //MIR //NFL //NDL //NJH //NP //XF "*.lock" //R:0 //W:0 > /dev/null || true
fi
# Çalışma ağacındaki (commit'lenmemiş dahil) Assets ve ayarları kopyaya eşitle.
robocopy "$(cygpath -w "$REPO/Assets")" "$(cygpath -w "$COPY/Assets")" //MIR //NFL //NDL //NJH //NP //R:0 //W:0 > /dev/null || true
robocopy "$(cygpath -w "$REPO/ProjectSettings")" "$(cygpath -w "$COPY/ProjectSettings")" //MIR //NFL //NDL //NJH //NP //R:0 //W:0 > /dev/null || true
robocopy "$(cygpath -w "$REPO/Packages")" "$(cygpath -w "$COPY/Packages")" //MIR //NFL //NDL //NJH //NP //R:0 //W:0 > /dev/null || true

RESULTS="$COPY/test-results-$PLATFORM.xml"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$COPY")" -runTests -testPlatform "$PLATFORM" \
  -testResults "$(cygpath -w "$RESULTS")" -logFile "$(cygpath -w "$COPY/test-$PLATFORM.log")" || true
grep -o 'test-run [^>]*' "$RESULTS" | grep -o 'result="[^"]*"\|total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"' | tr '\n' ' '; echo
grep -o '<test-case [^>]*result="Failed"' "$RESULTS" | grep -o 'fullname="[^"]*"' || true
