#!/usr/bin/env bash

echo $(jq -r .AssemblyVersion MacroMate/bin/x64/Release/MacroMate/MacroMate.json)
