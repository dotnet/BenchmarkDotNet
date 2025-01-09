using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Exporters
{
    internal interface IIntegratedExports
    {
        IEnumerable<IntegratedExportEnum> IntegratedExportEnums { get; set; }
    }
}
