## v0.16.0

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1109  |  Usage   | Error    | Required member cannot be set by BenchmarkDotNet
BDN1110  |  Usage   | Error    | Benchmark constructor must not be annotated with [SetsRequiredMembers]
BDN1305  |  Usage   | Error    | [ParamsSource] cannot reference write-only property
BDN1306  |  Usage   | Error    | [ParamsSource] must return a generic enumerable or async enumerable
BDN1307  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source method must not have required parameters
BDN1308  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source must not have more than one enumerable shape
BDN1310  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source method must not be generic
BDN1311  |  Usage   | Error    | [ParamsSource]/[ArgumentsSource] source must not yield a ref struct
BDN1312  |  Usage   | Warning  | [ParamsSource]/[ArgumentsSource] source may yield a ref struct
BDN1504  |  Usage   | Error    | [ArgumentsSource] must return a generic enumerable or async enumerable
BDN1505  |  Usage   | Error    | [ArgumentsSource] must yield an argument list for a multi-argument benchmark
BDN1506  |  Usage   | Warning  | [ArgumentsSource] must match the benchmark's parameters for every type argument
BDN1507  |  Usage   | Error    | [ArgumentsSource] must yield what the benchmark's parameters take
BDN1508  |  Usage   | Info     | [ArgumentsSource] should yield a ValueTuple rather than object[]
BDN1509  |  Usage   | Info     | [ArgumentsSource] should yield the parameter's own type rather than object
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


## v0.15.8

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1503  |  Usage   | Error	   | [Arguments] method has no parameters


## v0.15.7

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1000  |  Usage   | Error	   | BenchmarkRunner.Run() type is missing benchmark methods
BDN1001  |  Usage   | Error	   | BenchmarkRunner.Run() type is not public
BDN1002  |  Usage   | Error	   | BenchmarkRunner.Run() type is sealed
BDN1003  |  Usage   | Error	   | BenchmarkRunner.Run() type is abstract
BDN1004  |  Usage   | Error	   | BenchmarkRunner.Run() generic type is not annotated
BDN1100  |  Usage   | Error	   | Annotated generic benchmark class is abstract
BDN1101  |  Usage   | Error	   | Annotated benchmark class is not generic
BDN1102  |  Usage   | Error	   | Annotated generic benchmark class does not match type parameter count
BDN1103  |  Usage   | Error	   | Benchmark method is not public
BDN1104  |  Usage   | Error	   | Benchmark method is generic
BDN1105  |  Usage   | Error	   | Benchmark class is static
BDN1106  |  Usage   | Error	   | Single null argument passed to category
BDN1107  |  Usage   | Error	   | Multiple baseline benchmark methods
BDN1108  |  Usage   | Warning  | Multiple baseline benchmark methods per category
BDN1200  |  Usage   | Error	   | More than one [Params(Source\|AllValues)] on a field
BDN1201  |  Usage   | Error	   | More than one [Params(Source\|AllValues)] on a property
BDN1202  |  Usage   | Error	   | [Params(Source\|AllValues)] field is not public
BDN1203  |  Usage   | Error	   | [Params(Source\|AllValues)] property is not public
BDN1204  |  Usage   | Error	   | [Params(Source\|AllValues)] field is readonly
BDN1205  |  Usage   | Error	   | [Params(Source\|AllValues)] field is constant
BDN1206  |  Usage   | Error	   | [Params(Source\|AllValues)] property is init only
BDN1207  |  Usage   | Error	   | [Params(Source\|AllValues)] has no public setter
BDN1300  |  Usage   | Error	   | [Params] has no values
BDN1301  |  Usage   | Error	   | [Params] values do not match the type of the field or property
BDN1302  |  Usage   | Info	   | [Params] used with a single value
BDN1303  |  Usage   | Error	   | [ParamsAllValues] used with a [Flags] enum
BDN1304  |  Usage   | Error	   | [ParamsAllValues] used with a type that is not enum or bool
BDN1400  |  Usage   | Error	   | Benchmark method with parameters not annotated with [Arguments(Source)]
BDN1500  |  Usage   | Error	   | [Arguments(Source)] method is not a benchmark method
BDN1501  |  Usage   | Error	   | [Arguments] value(s) count does not match method parameter(s) count
BDN1502  |  Usage   | Error	   | [Arguments] value(s) do not match the type(s) of the method parameters