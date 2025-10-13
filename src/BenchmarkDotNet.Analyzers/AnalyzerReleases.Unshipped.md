; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
BDN1000  |  Usage   | Error	   | BDN1000_BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods
BDN1001  |  Usage   | Error	   | BDN1001_BenchmarkRunner_Run_TypeArgumentClassMustBeNonAbstract
BDN1100  |  Usage   | Error	   | BDN1100_General_BenchmarkClass_MethodMustBePublic
BDN1101  |  Usage   | Error	   | BDN1101_General_BenchmarkClass_MethodMustBeNonGeneric
BDN1102  |  Usage   | Error	   | BDN1102_General_BenchmarkClass_ClassMustBePublic
BDN1103  |  Usage   | Error	   | BDN1103_General_BenchmarkClass_ClassMustBeNonStatic
BDN1104  |  Usage   | Error	   | BDN1104_General_BenchmarkClass_ClassMustBeNonAbstract
BDN1105  |  Usage   | Error	   | BDN1105_General_BenchmarkClass_ClassMustBeNonGeneric
BDN1106  |  Usage   | Error	   | BDN1106_General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric
BDN1107  |  Usage   | Error	   | BDN1107_General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount
BDN1108  |  Usage   | Error	   | BDN1108_General_BenchmarkClass_ClassMustBeUnsealed
BDN1109  |  Usage   | Error	   | BDN1109_General_BenchmarkClass_OnlyOneMethodCanBeBaseline
BDN1200  |  Usage   | Error	   | BDN1200_Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField
BDN1201  |  Usage   | Error	   | BDN1201_Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty
BDN1202  |  Usage   | Error	   | BDN1202_Attributes_GeneralParameterAttributes_FieldMustBePublic
BDN1203  |  Usage   | Error	   | BDN1203_Attributes_GeneralParameterAttributes_PropertyMustBePublic
BDN1204  |  Usage   | Error	   | BDN1204_Attributes_GeneralParameterAttributes_NotValidOnReadonlyField
BDN1205  |  Usage   | Error	   | BDN1205_Attributes_GeneralParameterAttributes_NotValidOnConstantField
BDN1206  |  Usage   | Error	   | BDN1206_Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly
BDN1207  |  Usage   | Error	   | BDN1207_Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter
BDN1300  |  Usage   | Error	   | BDN1300_Attributes_ParamsAttribute_MustHaveValues
BDN1301  |  Usage   | Error	   | BDN1301_Attributes_ParamsAttribute_UnexpectedValueType
BDN1302  |  Usage   | Warning  | BDN1302_Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute
BDN1303  |  Usage   | Error	   | BDN1303_Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType
BDN1304  |  Usage   | Error	   | BDN1304_Attributes_ParamsAllValues_PropertyOrFieldTypeMustBeEnumOrBool
BDN1400  |  Usage   | Error	   | BDN1400_Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute
BDN1401  |  Usage   | Error	   | BDN1401_Attributes_ArgumentsAttribute_MethodWithoutAttributeMustHaveNoParameters
BDN1402  |  Usage   | Error	   | BDN1402_Attributes_ArgumentsAttribute_MustHaveMatchingValueCount
BDN1403  |  Usage   | Error	   | BDN1403_Attributes_ArgumentsAttribute_MustHaveMatchingValueType
