#!/bin/bash
set -eu
project="$(cd "$(dirname "$0")/../.." && pwd)"
if [ -f "$project/Temp/UnityLockfile" ]; then
  echo 'This project is already open. Use Gate C > Visual Asset Lab > HW_TI Meshy Trial 01 in that editor.'
  exit 1
fi
export CFFIXED_USER_HOME="${HW_TI_DATA_ROOT:-/private/tmp/hw-ti-engine/preferences}"
mkdir -p "$CFFIXED_USER_HOME"
exec /Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity \
  -projectPath "$project" -executeMethod RPG.VisualTrial.Editor.HwTrialBuild.Launch \
  -logFile "$CFFIXED_USER_HOME/visual-lab.log"
