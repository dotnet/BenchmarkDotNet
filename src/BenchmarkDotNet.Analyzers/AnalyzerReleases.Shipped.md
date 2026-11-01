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