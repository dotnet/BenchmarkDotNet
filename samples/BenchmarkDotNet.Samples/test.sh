MYDOTNET=/Users/naricc/workspace/runtime-aot-llvm/dotnet.sh
rm -rf ../../src/BenchmarkDotNet/bin
$MYDOTNET build ../../src/BenchmarkDotNet/BenchmarkDotNet.csproj

RUNTIMEPACK=/Users/naricc/workspace/runtime-aot-llvm/artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Debug/

$MYDOTNET run BenchmarkDotNet.Samples.csproj --framework net6.0 --filter IntroBasic --runtimes monoaotllvm --aotcompilerpath /Users/naricc/workspace/runtime-aot-llvm/artifacts/obj/mono/OSX.x64.Debug/mono/mini/mono-sgen --keepfiles --cli $MYDOTNET --customruntimepack $RUNTIMEPACK
