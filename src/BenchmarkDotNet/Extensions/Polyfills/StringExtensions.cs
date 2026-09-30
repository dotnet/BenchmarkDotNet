#if NETSTANDARD2_0
namespace System;

internal static partial class StringExtensions
{
    extension(string text)
    {
        public bool Contains(char value) => text.IndexOf(value) >= 0;
    }
}
#endif
