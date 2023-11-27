#!/usr/bin/env bash

echo $(jq -r .AssemblyVersion MacroMate/bin/x64/ReleaseAlpha/MacroMateAlpha.json)
