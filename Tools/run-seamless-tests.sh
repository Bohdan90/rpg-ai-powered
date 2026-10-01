#!/bin/bash
set -eu
scope="$1"
filter="${2:-}"
project="$(cd "$(dirname "$0")/.." && pwd)"
run_stamp=$(date +%Y%m%d-%H%M%S)
root=/private/tmp/seamless-worlds-07
mkdir -p "$root/logs" "$root/test-preferences/Library/Caches/com.unity3d.UnityEditor"
export CFFIXED_USER_HOME="$root/test-preferences"
args=(-batchmode -projectPath "$project" -runTests -testPlatform "$scope" -testResults "$root/logs/$scope-$run_stamp.xml" -logFile "$root/logs/$scope-$run_stamp.log")
if [ "$scope" = EditMode ]; then args+=(-nographics); fi
if [ -n "$filter" ]; then args+=(-testFilter "$filter"); fi
exec /Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity "${args[@]}"
