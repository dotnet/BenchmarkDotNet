using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Attributes
{
    public class ExceptionDiagnoserConfig
    {
        /// <param name="displayExceptionsIfZeroValue">Determines whether the Exceptions column is displayed when its value is not calculated. True by default.</param>

        [PublicAPI]
        public ExceptionDiagnoserConfig(bool displayExceptionsIfZeroValue = true)
        {
            DisplayExceptionsIfZeroValue = displayExceptionsIfZeroValue;
        }

        public bool DisplayExceptionsIfZeroValue { get; }
    }
}
