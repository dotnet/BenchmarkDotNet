### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1305  |  Usage   | Error	   | [ParamsSource] cannot reference write-only property
BDN1600  |  Usage   | Error    | Fields or properties annotated with [BenchmarkCancellation] must be of type CancellationToken
BDN1601  |  Usage   | Error    | Fields annotated with [BenchmarkCancellation] must be public
BDN1602  |  Usage   | Error    | Properties annotated with [BenchmarkCancellation] must be public
BDN1603  |  Usage   | Error    | [BenchmarkCancellation] attribute is not valid on readonly fields
BDN1604  |  Usage   | Error    | Properties annotated with [BenchmarkCancellation] must have a public setter
BDN1605  |  Usage   | Info     | Async benchmarks should have a [BenchmarkCancellation] property for cancellation support


### Removed Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1100  |  Usage   | Error    | Rule removed as GenericTypeArguments now supports abstract classes