#!/bin/zsh
set -eu
cd "${0:A:h:h}"
UNITY_ROOT="${UNITY_ROOT:-/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents}"
mkdir -p Build/Checks
"$UNITY_ROOT/MonoBleedingEdge/bin/mono" "$UNITY_ROOT/MonoBleedingEdge/lib/mono/4.5/mcs.exe" -define:POTATO_STANDALONE -out:Build/Checks/PotatoChecks.exe Assets/Scripts/PotatoModel.cs Assets/Scripts/PotatoRules.cs Assets/Editor/PotatoChecks.cs
"$UNITY_ROOT/MonoBleedingEdge/bin/mono" Build/Checks/PotatoChecks.exe
