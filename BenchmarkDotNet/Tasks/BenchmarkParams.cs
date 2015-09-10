using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkParams
    {
        public string ParamFieldOrProperty { get; }
        public bool IsStatic { get; }
        public IList<int> Values { get; }

        public string Caption => $"_{ParamFieldOrProperty}";

        // this is here to stop magic strings spreading around the code
        internal static string ParamTitle { get { return "Param"; } }

        public BenchmarkParams(string paramFieldOrProperty, bool isStatic, int[] values)
        {
            if (paramFieldOrProperty == null) throw new ArgumentNullException(nameof(paramFieldOrProperty));
            if (values == null) throw new ArgumentNullException(nameof(values));

            ParamFieldOrProperty = paramFieldOrProperty;
            IsStatic = isStatic;
            Values = new List<int>(values);
        }

        public static string ParamToArgs(int param)
        {
            return $"-p={param}";
        }

        public static int Parse(string[] args)
        {
            int param = 0;
            foreach (var arg in args)
            {
                if (arg.StartsWith("-p="))
                    int.TryParse(arg.Substring(3), out param);
            }
            return param;
        }
    }
}
