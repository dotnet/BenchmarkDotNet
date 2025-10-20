namespace BenchmarkDotNet.Analyzers
{
    public static class DiagnosticIds
    {
        public const string BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods = "BDN1000";
        public const string BenchmarkRunner_Run_TypeArgumentClassMustBePublic = "BDN1001";
        public const string BenchmarkRunner_Run_TypeArgumentClassMustBeUnsealed = "BDN1002";
        public const string BenchmarkRunner_Run_TypeArgumentClassMustBeNonAbstract = "BDN1003";
        public const string BenchmarkRunner_Run_GenericTypeArgumentClassMustBeAnnotatedWithAGenericTypeArgumentsAttribute = "BDN1004";
        public const string General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract = "BDN1100";
        public const string General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric = "BDN1101";
        public const string General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount = "BDN1102";
        public const string General_BenchmarkClass_MethodMustBePublic = "BDN1103";
        public const string General_BenchmarkClass_MethodMustBeNonGeneric = "BDN1104";
        public const string General_BenchmarkClass_ClassMustBeNonStatic = "BDN1105";
        public const string General_BenchmarkClass_OnlyOneMethodCanBeBaseline = "BDN1106";
        public const string Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField = "BDN1200";
        public const string Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty = "BDN1201";
        public const string Attributes_GeneralParameterAttributes_FieldMustBePublic = "BDN1202";
        public const string Attributes_GeneralParameterAttributes_PropertyMustBePublic = "BDN1203";
        public const string Attributes_GeneralParameterAttributes_NotValidOnReadonlyField = "BDN1204";
        public const string Attributes_GeneralParameterAttributes_NotValidOnConstantField = "BDN1205";
        public const string Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly = "BDN1206";
        public const string Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter = "BDN1207";
        public const string Attributes_ParamsAttribute_MustHaveValues = "BDN1300";
        public const string Attributes_ParamsAttribute_MustHaveMatchingValueType = "BDN1301";
        public const string Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute = "BDN1302";
        public const string Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType = "BDN1303";
        public const string Attributes_ParamsAllValuesAttribute_PropertyOrFieldTypeMustBeEnumOrBool = "BDN1304";
        public const string Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute = "BDN1400";
        public const string Attributes_ArgumentsAttribute_MethodWithoutAttributeMustHaveNoParameters = "BDN1401";
        public const string Attributes_ArgumentsAttribute_MustHaveMatchingValueCount = "BDN1402";
        public const string Attributes_ArgumentsAttribute_MustHaveMatchingValueType = "BDN1403";
    }
}
