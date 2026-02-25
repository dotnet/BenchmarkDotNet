#if NETSTANDARD2_0
namespace System;

internal static partial class EnumExtensions
{
    extension(Enum)
    {
        public static TEnum[] GetValues<TEnum>()
            where TEnum : struct, Enum
        {
            var values = Enum.GetValues(typeof(TEnum));
            var result = new TEnum[values.Length];
            Array.Copy(values, result, values.Length);
            return result;
        }

        public static TEnum Parse<TEnum>(string value)
            where TEnum : struct, Enum
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }

        public static string[] GetNames<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetNames(typeof(TEnum));
        }
    }
}
#endif
