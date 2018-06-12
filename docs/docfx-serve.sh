#!/usr/bin/env bash

cd ./changelog-generator/
./generate-all.sh
cd ..
mono ./docfx-bin/docfx.exe docfx.json --serve