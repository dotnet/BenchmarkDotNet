﻿using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class LogicalGroupColumnAttribute : ColumnConfigBaseAttribute
    {
        public LogicalGroupColumnAttribute() : base(LogicalGroupColumn.Default) { }
    }
}