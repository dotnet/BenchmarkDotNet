using System;

namespace BenchmarkDotNet.Mathematics
{
    public static class NumeralSystemExtensions
    {
        public static string ToPresentation(this NumeralSystem system, int value)
        {
            switch (system)
            {
                case NumeralSystem.Arabic:
                    return value.ToString();
                case NumeralSystem.Roman:
                    return ToRoman(value);
                case NumeralSystem.Stars:
                    return new string('*', value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(system));
            }
        }

        // Based on http://stackoverflow.com/questions/7040289/converting-integers-to-roman-numerals
        private static string ToRoman(int number)
        {
            if (number < 0 || number > 3999) throw new ArgumentOutOfRangeException(nameof(number), "insert value between 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException(nameof(number), "something bad happened");
        }
    }
}