#!/bin/bash
set -eu
scope="$1"
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
root=/private/tmp/strategic-map-ux
stamp=$(date +%Y%m%d-%H%M%S)
export CFFIXED_USER_HOME="$root/test-preferences"
mkdir -p "$root/logs" "$CFFIXED_USER_HOME/Library/Caches/com.unity3d.UnityEditor"
args=(-batchmode -projectPath "$project_dir" -runTests -testPlatform "$scope" -testResults "$root/logs/$scope-$stamp.xml" -logFile "$root/logs/$scope-$stamp.log")
if [ "$scope" = EditMode ]; then args+=(-nographics); fi
if [ -n "${2:-}" ]; then args+=(-testFilter "$2"); fi
exec /Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity "${args[@]}"
