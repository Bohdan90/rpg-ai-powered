#!/bin/bash
set -eu
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
export CFFIXED_USER_HOME=/private/tmp/fire-targeting-02/player-preferences
mkdir -p "$CFFIXED_USER_HOME/Library/Caches/com.unity3d.UnityEditor"
exec /Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity -projectPath "$project_dir" -logFile /private/tmp/fire-targeting-02/player-editor.log
