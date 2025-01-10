using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BenchmarkDotNet.Exporters
{
    internal interface IIntegratedExports
    {
        IEnumerable<IntegratedExportEnum> IntegratedExportEnums { get; set; }
    }
}
