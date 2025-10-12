namespace BenchmarkDotNet.Analyzers
{
    public static class DiagnosticIds
    {
        public const string BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods = "BDN1000";
        public const string General_BenchmarkClass_MethodMustBePublic = "BDN1001";
        public const string General_BenchmarkClass_MethodMustBeNonGeneric = "BDN1002";
        public const string General_BenchmarkClass_ClassMustBePublic = "BDN1003";
        public const string General_BenchmarkClass_ClassMustBeNonStatic = "BDN1004";
        public const string General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract = "BDN1005";
        public const string General_BenchmarkClass_GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttribute = "BDN1006";
        public const string General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric = "BDN1007";
        public const string General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount = "BDN1008";
        public const string General_BenchmarkClass_ClassMustBeUnsealed = "BDN1009";
        public const string General_BenchmarkClass_OnlyOneMethodCanBeBaseline = "BDN1010";
        public const string Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField = "BDN1011";
        public const string Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty = "BDN1012";
        public const string Attributes_GeneralParameterAttributes_FieldMustBePublic = "BDN1013";
        public const string Attributes_GeneralParameterAttributes_PropertyMustBePublic = "BDN1014";
        public const string Attributes_GeneralParameterAttributes_NotValidOnReadonlyField = "BDN1015";
        public const string Attributes_GeneralParameterAttributes_NotValidOnConstantField = "BDN1016";
        public const string Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly = "BDN1017";
        public const string Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter = "BDN1018";
        public const string Attributes_ParamsAttribute_MustHaveValues = "BDN1019";
        public const string Attributes_ParamsAttribute_UnexpectedValueType = "BDN1020";
        public const string Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute = "BDN1021";
        public const string Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType = "BDN1022";
        public const string Attributes_ParamsAllValuesAttribute_PropertyOrFieldTypeMustBeEnumOrBool = "BDN1023";
        public const string Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute = "BDN1024";
        public const string Attributes_ArgumentsAttribute_MethodWithoutAttributeMustHaveNoParameters = "BDN1025";
        public const string Attributes_ArgumentsAttribute_MustHaveMatchingValueCount = "BDN1026";
        public const string Attributes_ArgumentsAttribute_MustHaveMatchingValueType = "BDN1027";
    }
}
