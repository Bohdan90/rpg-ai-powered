#!/bin/bash
set -eu
scope="$1"
filter="${2:-}"
project="${3:-/Users/bohdanskrypka/UnityProjects/Convergence/Realm-Operations-06}"
run_stamp=$(date +%Y%m%d-%H%M%S)
results="/private/tmp/realm-operations-06/logs/${scope}-${run_stamp}"
args=(-batchmode -projectPath "$project" -runTests -testPlatform "$scope" -testResults "$results.xml" -logFile "$results.log")
if [ "$scope" = EditMode ]; then args+=(-nographics); fi
if [ -n "$filter" ]; then args+=(-testFilter "$filter"); fi
export CFFIXED_USER_HOME='/private/tmp/realm-operations-06/test-preferences'
'/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity' "${args[@]}"
