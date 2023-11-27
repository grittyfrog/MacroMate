#!/usr/bin/env bash
set -euo pipefail

BUILD_LOCATION=MacroMate/bin/x64/Release
ALPHA_LOCATION=MacroMate/bin/x64/ReleaseAlpha

rm -rf ${ALPHA_LOCATION} || true
cp -r ${BUILD_LOCATION} ${ALPHA_LOCATION}
mv ${ALPHA_LOCATION}/MacroMate.deps.json ${ALPHA_LOCATION}/MacroMateAlpha.deps.json
mv ${ALPHA_LOCATION}/MacroMate.dll ${ALPHA_LOCATION}/MacroMateAlpha.dll
mv ${ALPHA_LOCATION}/MacroMate.json ${ALPHA_LOCATION}/MacroMateAlpha.json
mv ${ALPHA_LOCATION}/MacroMate.pdb ${ALPHA_LOCATION}/MacroMateAlpha.pdb

# Reconstruct the release zip
rm -rf "${ALPHA_LOCATION}/MacroMate"

cd "${ALPHA_LOCATION}"
zip -r MacroMateAlpha.zip . *
cd -
