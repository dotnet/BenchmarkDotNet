#!/usr/bin/env bash

# Define variables
PROJECT_ROOT=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
export DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

if [ ! -d "$PROJECT_ROOT/.dotnet" ]; then
    mkdir "$PROJECT_ROOT/.dotnet"
    curl -Lsfo "$PROJECT_ROOT/.dotnet/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
    bash "$PROJECT_ROOT/.dotnet/dotnet-install.sh" --jsonfile ./build/sdk/global.json --install-dir .dotnet --no-path
fi

export PATH="$PROJECT_ROOT/.dotnet":$PATH
export DOTNET_ROOT="$PROJECT_ROOT/.dotnet"

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

dotnet run --configuration Release --project "$PROJECT_ROOT/build/BenchmarkDotNet.Build/BenchmarkDotNet.Build.csproj" -- "$@"
