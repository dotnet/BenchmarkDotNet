#!/usr/bin/env bash

DOCFX_BIN=docfx-bin
DOCFX_EXE=$DOCFX_BIN/docfx.exe
DOCFX_VERSION=2.36.2
DOCFX_URL=https://github.com/dotnet/docfx/releases/download/v$DOCFX_VERSION/docfx.zip

if [ ! -d "$DOCFX_BIN" ]; then
  mkdir "$DOCFX_BIN"
fi

if [ ! -f "$DOCFX_EXE" ]; then
    echo "Downloading DocFX..."
    curl -sSL $DOCFX_URL -o $DOCFX_BIN/docfx.zip
    unzip -q $DOCFX_BIN/docfx.zip -d $DOCFX_BIN 
    rm $DOCFX_BIN/docfx.zip
    if [ $? -ne 0 ]; then
        echo "An error occurred while downloading DocFX."
        exit 1
    fi
fi