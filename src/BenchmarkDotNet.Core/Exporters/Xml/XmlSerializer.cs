using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Xml
{
    public class XmlSerializer
    {
        private readonly Type type;

        public XmlSerializer(Type type)
        {
            this.type = type;
        }

        public void Serialize(XmlWriter writer, SummaryDto summary)
        {
            writer.WriteStartDocument();

            WriteSummary(writer, summary);

            writer.WriteEndDocument();
        }

        private void WriteSummary(XmlWriter writer, SummaryDto summary)
        {
            writer.WriteStartElement(nameof(Summary));
            writer.WriteElementString(nameof(Summary.Title), summary.Title.ToString());

            WriteHostEnvironmentInfo(writer, summary.HostEnvironmentInfo);
            WriteBenchmarks(writer, summary.Benchmarks);

            writer.WriteEndElement();
        }
        
        private void WriteHostEnvironmentInfo(XmlWriter writer,
                                              HostEnvironmentInfoDto hei)
        {
            writer.WriteStartElement(nameof(Summary.HostEnvironmentInfo));

            foreach (PropertyInfo property in hei.GetType().GetProperties())
            {
                if(property.PropertyType == typeof(ChronometerDto))
                {
                    writer.WriteStartElement(nameof(hei.ChronometerFrequency));
                    writer.WriteElementString(nameof(ChronometerDto.Hertz),
                        hei.ChronometerFrequency.Hertz.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteElementString(property.Name, 
                        string.Format(CultureInfo.InvariantCulture, "{0}", property.GetValue(hei)));
                }
            }

            writer.WriteEndElement();
        }

        private void WriteBenchmarks(XmlWriter writer,
                                     IEnumerable<BenchmarkReportDto> benchmarks)
        {
            writer.WriteStartElement(nameof(SummaryDto.Benchmarks));

            foreach (BenchmarkReportDto benchmark in benchmarks)
            {
                WriteBenchmark(writer, benchmark);
            }

            writer.WriteEndElement();
        }

        private void WriteBenchmark(XmlWriter writer, BenchmarkReportDto benchmark)
        {
            writer.WriteStartElement(nameof(BenchmarkReport.Benchmark));

            foreach (PropertyInfo property in benchmark.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(Statistics))
                {
                    writer.WriteStartElement(nameof(benchmark.Statistics));

                    foreach (PropertyInfo statProperty in benchmark.Statistics.GetType().GetProperties())
                    {
                        writer.WriteElementString(statProperty.Name, 
                            string.Format(CultureInfo.InvariantCulture, "{0}", 
                                          statProperty.GetValue(benchmark.Statistics)));
                    }

                    writer.WriteEndElement();
                }
                else if(property.PropertyType == typeof(IEnumerable<Measurement>))
                {
                    foreach (Measurement measurement in benchmark.Measurements)
                    {
                        writer.WriteStartElement(nameof(benchmark.Measurements));

                        foreach (PropertyInfo measurementProperty in measurement.GetType().GetProperties())
                        {
                            writer.WriteElementString(measurementProperty.Name,
                                string.Format(CultureInfo.InvariantCulture, "{0}",
                                              measurementProperty.GetValue(measurement)));
                        }

                        writer.WriteEndElement();
                    }
                }
                else
                {
                    object value = property.GetValue(benchmark);

                    // Don't write empty text, empty collection or null value
                    string text = value as string;
                    IEnumerable collection = value as IEnumerable;

                    if (!string.IsNullOrWhiteSpace(text) 
                        && (collection == null || collection.Cast<object>().Any()) 
                        && value != null)
                    {
                        writer.WriteElementString(property.Name,
                            string.Format(CultureInfo.InvariantCulture, "{0}", value));
                    }
                }
            }

            writer.WriteEndElement();
        }
    }
}