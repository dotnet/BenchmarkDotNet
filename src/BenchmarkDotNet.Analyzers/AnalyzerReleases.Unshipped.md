; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1000  |  Usage   | Error	   | BDN1000_BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods
BDN1001  |  Usage   | Error	   | BDN1001_General_BenchmarkClass_MethodMustBePublic
BDN1002  |  Usage   | Error	   | BDN1002_General_BenchmarkClass_MethodMustBeNonGeneric
BDN1003  |  Usage   | Error	   | BDN1003_General_BenchmarkClass_ClassMustBePublic
BDN1004  |  Usage   | Error	   | BDN1004_General_BenchmarkClass_ClassMustBeNonStatic
BDN1005  |  Usage   | Error	   | BDN1005_General_BenchmarkClass_ClassMustBeNonAbstract
BDN1006  |  Usage   | Error	   | BDN1006_General_BenchmarkClass_ClassMustBeNonGeneric
BDN1007  |  Usage   | Error	   | BDN1007_General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric
BDN1008  |  Usage   | Error	   | BDN1008_General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount
BDN1009  |  Usage   | Error	   | BDN1009_General_BenchmarkClass_ClassMustBeUnsealed
BDN1010  |  Usage   | Error	   | BDN1010_General_BenchmarkClass_OnlyOneMethodCanBeBaseline
BDN1011  |  Usage   | Error	   | BDN1011_Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField
BDN1012  |  Usage   | Error	   | BDN1012_Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty
BDN1013  |  Usage   | Error	   | BDN1013_Attributes_GeneralParameterAttributes_FieldMustBePublic
BDN1014  |  Usage   | Error	   | BDN1014_Attributes_GeneralParameterAttributes_PropertyMustBePublic
BDN1015  |  Usage   | Error	   | BDN1015_Attributes_GeneralParameterAttributes_NotValidOnReadonlyField
BDN1016  |  Usage   | Error	   | BDN1016_Attributes_GeneralParameterAttributes_NotValidOnConstantField
BDN1017  |  Usage   | Error	   | BDN1017_Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly
BDN1018  |  Usage   | Error	   | BDN1018_Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter
BDN1019  |  Usage   | Error	   | BDN1019_Attributes_ParamsAttribute_MustHaveValues
BDN1020  |  Usage   | Error	   | BDN1020_Attributes_ParamsAttribute_UnexpectedValueType
BDN1021  |  Usage   | Warning  | BDN1021_Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute
BDN1022  |  Usage   | Error	   | BDN1022_Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType
BDN1023  |  Usage   | Error	   | BDN1023_Attributes_ParamsAllValues_PropertyOrFieldTypeMustBeEnumOrBool
BDN1024  |  Usage   | Error	   | BDN1024_Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute
BDN1025  |  Usage   | Error	   | BDN1025_Attributes_ArgumentsAttribute_MethodWithoutAttributeMustHaveNoParameters
BDN1026  |  Usage   | Error	   | BDN1026_Attributes_ArgumentsAttribute_MustHaveMatchingValueCount
BDN1027  |  Usage   | Error	   | BDN1027_Attributes_ArgumentsAttribute_MustHaveMatchingValueType
