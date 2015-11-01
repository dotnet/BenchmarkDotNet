using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkParametersSets
    {
        public static BenchmarkParametersSets Empty = new BenchmarkParametersSets(new int[0]);

        public int[] IntParams { get; }

        public BenchmarkParametersSets(int[] intParams)
        {
            IntParams = intParams ?? new int[0];
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(IntParams), string.Join(",", IntParams));
            }
        }

        public IEnumerable<BenchmarkParameters> ToParameters()
        {
            return IntParams.Select(intValue => new BenchmarkParameters(intValue));
        }

        public bool IsEmpty()
        {
            return IntParams.Length == 0;
        }

        public string ToCtorDefinition()
        {
            var builder = new StringBuilder();
            builder.Append($"{nameof(IntParams).ToCamelCase()}: new int[] {{ {string.Join(", ", IntParams)} }}");
            return builder.ToString();
        }

        public string Description
        {
            get
            {
                var builder = new StringBuilder();
                if (IntParams.Length > 0)
                    return $"{nameof(IntParams)}={string.Join(",", IntParams)}";
                return builder.ToString();
            }
        }
    }
}