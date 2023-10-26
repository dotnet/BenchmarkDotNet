using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public abstract class StoppingCriteriaBase : IStoppingCriteria
    {
        private readonly Lazy<string> lazyTitle;
        private readonly Lazy<int> lazyMaxIterationCount;
        private readonly Lazy<string[]> lazyWarnings;

        public string Title => lazyTitle.Value;
        public int MaxIterationCount => lazyMaxIterationCount.Value;
        public IReadOnlyList<string> Warnings => lazyWarnings.Value;

        protected StoppingCriteriaBase()
        {
            lazyTitle = new Lazy<string>(GetTitle);
            lazyMaxIterationCount = new Lazy<int>(GetMaxIterationCount);
            lazyWarnings = new Lazy<string[]>(() => GetWarnings().ToArray());
        }

        public abstract StoppingResult Evaluate(IReadOnlyList<Measurement> measurements);

        protected abstract string GetTitle();

        protected abstract int GetMaxIterationCount();

        protected abstract IEnumerable<string> GetWarnings();
    }
}