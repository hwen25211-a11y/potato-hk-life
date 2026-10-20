#!/bin/zsh
set -eu
cd "${0:A:h:h}"
ulimit -n 8192
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY_BIN" ]]; then
  echo 'Unity executable not found. Set UNITY_BIN to the installed Unity executable.' >&2
  exit 1
fi
case "${1:-mac}" in
 mac) method=PotatoLife.Editor.PotatoBuild.BuildMac ;;
 web) method=PotatoLife.Editor.PotatoBuild.BuildWeb ;;
 prepare) method=PotatoLife.Editor.PotatoBuild.Prepare ;;
 *) echo 'Usage: Tools/build.sh [mac|web|prepare]'; exit 2 ;;
esac
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod "$method" -logFile "$PWD/docs/build-${1:-mac}.log"
