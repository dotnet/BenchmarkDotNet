### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1109  |  Usage   | Error    | Required member cannot be set by BenchmarkDotNet
BDN1110  |  Usage   | Error    | Benchmark constructor must not be annotated with [SetsRequiredMembers]
BDN1208  |  Usage   | Error    | [Params(Source\|AllValues)] member name is reserved by code generation
BDN1305  |  Usage   | Error    | [ParamsSource] cannot reference write-only property
BDN1306  |  Usage   | Error    | [ParamsSource] must return a generic enumerable or async enumerable
BDN1307  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source method must not have required parameters
BDN1308  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source must not have more than one enumerable shape
BDN1310  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source method must not be generic
BDN1311  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source must not yield a ref struct
BDN1312  |  Usage   | Warning  | [ParamsSource]/[ArgumentsSource] source may yield a ref struct
BDN1504  |  Usage   | Error    | [ArgumentsSource] must return a generic enumerable or async enumerable
BDN1600  |  Usage   | Error    | Fields or properties annotated with [BenchmarkCancellation] must be of type CancellationToken
BDN1601  |  Usage   | Error    | Fields annotated with [BenchmarkCancellation] must be public
BDN1602  |  Usage   | Error    | Properties annotated with [BenchmarkCancellation] must be public
BDN1603  |  Usage   | Error    | [BenchmarkCancellation] attribute is not valid on readonly fields
BDN1604  |  Usage   | Error    | Properties annotated with [BenchmarkCancellation] must have a public setter
BDN1605  |  Usage   | Info     | Async benchmarks should have a [BenchmarkCancellation] property for cancellation support
BDN1700  |  Usage   | Error    | [GlobalSetup]/[GlobalCleanup]/[IterationSetup]/[IterationCleanup] method must not return an async enumerable
BDN1701  |  Usage   | Warning  | Benchmark/setup/cleanup return type is both awaitable and an async enumerable; the iterator is never enumerated
BDN1800  |  Usage   | Warning  | Setting both Runtime and Toolchain on a job is order-dependent; they are coupled and the last assignment wins while the other is discarded


### Removed Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1100  |  Usage   | Error    | Rule removed as GenericTypeArguments now supports abstract classes
BDN1206  |  Usage   | Error    | Rule removed as parameters are now assigned through an object initializer, which can set init-only properties