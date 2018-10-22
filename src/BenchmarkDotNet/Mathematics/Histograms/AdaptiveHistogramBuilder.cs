using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    public class AdaptiveHistogramBuilder : IHistogramBuilder
    {
        [PublicAPI, Pure]
        public Histogram Build(Statistics s, BinSizeRule? rule = null)
        {
            double binSize = s.GetOptimalBinSize(rule);
            if (Math.Abs(binSize) < 1e-9)
                binSize = 1;
            return BuildWithFixedBinSize(s.GetSortedValues(), binSize);
        }

        private const double Resolution = 0.0001;

        // TODO: Optimize
        [PublicAPI, Pure]
        public Histogram BuildWithFixedBinSize(IEnumerable<double> values, double binSize)
        {
            const double eps = 1e-9;
            const double margin = 0.1;
            const double adaptiveFactor = 0.02;

            if (binSize < eps)
                throw new ArgumentException($"binSize ({binSize.ToStr()}) should be a positive number", nameof(binSize));
            if (binSize < Resolution)
                binSize = Resolution;
            binSize = NiceCeiling(binSize);
            
            var list = values.ToList();
            if (list.IsEmpty())
                throw new ArgumentException("Values should be non-empty", nameof(values));

            list.Sort();
            if (list.Last() - list.First() < binSize)
            {
                double center = (list.First() + list.Last()) / 2;
                double lower = center - binSize / 2;
                double upper = center + binSize / 2;
                return new Histogram(binSize, new[] { new HistogramBin(lower, upper, list.ToArray()) });
            }

            var points = new List<double> { NiceFloor(list.Min() - binSize / 2), NiceCeiling(list.Max() + binSize / 2) };
            int processedPointCount = 0;
            while (true)
            {
                if (points.Count > 10 * list.Count)
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.AppendLine("Failed to run AdaptiveHistogramBuilder.BuildWithFixedBinSize");
                    errorMessage.AppendLine("BinSize: " + binSize.ToStr("N12"));
                    errorMessage.AppendLine("Values: ");
                    foreach (double value in list)
                        errorMessage.AppendLine("  " + value.ToStr("N12"));
                    throw new InvalidOperationException(errorMessage.ToString());
                }

                int pointIndex = -1;
                for (int i = processedPointCount; i < points.Count - 1; i++)
                {
                    double adaptiveBinSize = (points[i] + points[i + 1]) / 2.0 * adaptiveFactor;
                    double maxSize = Math.Max(binSize * (1.0 + 2 * margin), adaptiveBinSize);
                    if (points[i + 1] - points[i] > maxSize)
                    {
                        pointIndex = i;
                        break;
                    }
                }

                if (pointIndex == -1)
                    break;

                double lower = points[pointIndex];
                double upper = points[pointIndex + 1];

                int bestIndex1 = -1;
                int bestIndex2 = -1;
                int bestCount = -1;
                double bestDist = double.MaxValue;

                bool Inside(double x) => x > lower - eps && x < upper - eps;

                for (int i = 0; i < list.Count; i++)
                    if (Inside(list[i]))
                    {
                        int j = i;
                        while (j < list.Count && Inside(list[j]) && list[j] - list[i] < binSize)
                            j++;
                        int count = j - i;
                        double dist = list[j - 1] - list[i];
                        if (count > bestCount || count == bestCount && dist < bestDist)
                        {
                            bestCount = count;
                            bestIndex1 = i;
                            bestIndex2 = j - 1;
                            bestDist = dist;
                        }
                    }

                if (bestIndex1 != -1)
                {
                    double center = (list[bestIndex1] + list[bestIndex2]) / 2.0;
                    double adaptiveBinSize = Math.Max(binSize, center * adaptiveFactor);
                    double left = NiceFloor(center - adaptiveBinSize / 2);
                    double right = NiceFloor(Math.Min(center + adaptiveBinSize / 2, upper));

                    if (left > lower + binSize * margin)
                        points.Insert(pointIndex + 1, left);
                    else if (right < upper - binSize * margin)
                    {
                        points.Insert(pointIndex + 1, right);
                        processedPointCount++;
                    }
                    else
                        processedPointCount++;
                }
                else
                {
                    points.Insert(pointIndex + 1, NiceFloor(lower + binSize));
                    processedPointCount++;
                }
            }

            var bins = new List<HistogramBin>(points.Count - 1);
            int counter = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var bin = new List<double>();
                double lower = points[i];
                double upper = points[i + 1];

                while (counter < list.Count && (list[counter] < upper || i == points.Count - 1))
                    bin.Add(list[counter++]);

                bins.Add(new HistogramBin(lower, upper, bin.ToArray()));
            }

            // Trim
            while (bins.Any() && bins.First().IsEmpty)
                bins.RemoveAt(0);
            while (bins.Any() && bins.Last().IsEmpty)
                bins.RemoveAt(bins.Count - 1);

            // Join small bins to neighbors
            counter = 0;
            double lastValue = 0;
            while (counter < bins.Count)
            {
                if (bins[counter].HasAny)
                    lastValue = Math.Max(lastValue, bins[counter].Values.Last());
                double adaptiveThreshold = Math.Max(binSize / 2, lastValue * adaptiveFactor);
                if (bins[counter].Gap < adaptiveThreshold)
                {
                    double leftGap = counter > 0 ? bins[counter - 1].Gap : double.MaxValue;
                    double rightGap = counter < bins.Count - 1 ? bins[counter + 1].Gap : double.MaxValue;
                    if (leftGap < rightGap && counter > 0)
                    {
                        bins[counter - 1] = HistogramBin.Union(bins[counter - 1], bins[counter]);
                        bins.RemoveAt(counter);
                    }
                    else if (counter < bins.Count - 1)
                    {
                        bins[counter] = HistogramBin.Union(bins[counter], bins[counter + 1]);
                        bins.RemoveAt(counter + 1);
                    }
                    else
                        counter++;
                }
                else
                    counter++;
            }

            return new Histogram(binSize, bins.ToArray());
        }

        private static double NiceFloor(double value) => Math.Floor(value / Resolution) * Resolution;
        private static double NiceCeiling(double value) => Math.Ceiling(value / Resolution) * Resolution;
    }
}