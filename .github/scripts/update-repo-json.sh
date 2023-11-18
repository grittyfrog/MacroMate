#!/usr/bin/env bash
set -euo pipefail

BUILD_JSON=MacroMate/bin/x64/Release/MacroMate/MacroMate.json
BUILD_VERSION=$(jq -r .AssemblyVersion $BUILD_JSON)

ICON_URL="https://raw.githubusercontent.com/grittyfrog/MacroMate/v${BUILD_VERSION}/MacroMate/Images/icon.png"
VERSION_URL="https://github.com/grittyfrog/MacroMate/releases/download/v${BUILD_VERSION}/MacroMate.zip"

UPDATED_JSON=$(jq -n '$ARGS.named' \
    --arg AssemblyVersion "${BUILD_VERSION}" \
    --arg Description "$(jq -r .Description $BUILD_JSON)" \
    --arg Punchline "$(jq -r .Punchline $BUILD_JSON)" \
    --arg IconUrl "${ICON_URL}" \
    --arg DownloadLinkInstall "${VERSION_URL}" \
    --arg DownloadLinkTesting "${VERSION_URL}" \
    --arg DownloadLinkUpdate "${VERSION_URL}"
)

# Write to repo.json
jq "[.[0] * ${UPDATED_JSON}]" repo.json > repo.json.temp
mv repo.json.temp repo.json
