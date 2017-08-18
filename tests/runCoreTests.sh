#!/usr/bin/env bash
dotnet test BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj --configuration Release --framework netcoreapp1.1 2>&1 | tee tests.log
dotnet test BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj --configuration Release --framework netcoreapp1.1 2>&1 | tee integration-tests.log
