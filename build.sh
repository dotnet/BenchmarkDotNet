#!/usr/bin/env bash
# Define varibles
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
TOOLS_DIR=$SCRIPT_DIR/tools


# Make sure the tools folder exist.
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
export DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

if [ ! -d "$SCRIPT_DIR/.dotnet" ]; then
    mkdir "$SCRIPT_DIR/.dotnet"
fi
curl -Lsfo "$SCRIPT_DIR/.dotnet/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
bash "$SCRIPT_DIR/.dotnet/dotnet-install.sh" --jsonfile ./build/global.json --install-dir .dotnet --no-path
export PATH="$SCRIPT_DIR/.dotnet":$PATH
export DOTNET_ROOT="$SCRIPT_DIR/.dotnet"

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

dotnet run --project ./build/Build.csproj -- "$@"
