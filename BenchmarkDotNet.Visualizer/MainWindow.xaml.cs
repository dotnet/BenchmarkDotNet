using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using BenchmarkDotNet.Export;
using BenchmarkDotNet.Reports;
using Microsoft.Win32;

namespace BenchmarkDotNet.Visualizer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var logger = new FctbLogger(OutputTextBox);
            runner = new BenchmarkRunner(new[] { logger });
        }

        private readonly BenchmarkRunner runner;

        private void OnRunButtonClick(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "";
            var url = "https://raw.githubusercontent.com/PerfDotNet/BenchmarkDotNet/master/BenchmarkDotNet.Samples/Intro_00_Basic.cs";
            Task.Factory.StartNew(Run);
        }

        private void Run()
        {
            var reports = runner.RunSource(SourceTextBox.Text).ToList();
            MarkdownReportTextBox.Text = "";
            MarkdownReportExporter.Default.Export(reports, new FctbLogger(MarkdownReportTextBox));
        }

        private void OnLoadFileButtonClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                SourceTextBox.Text = File.ReadAllText(openFileDialog.FileName);
        }

        private void OnLoadUrlButtonClick(object sender, RoutedEventArgs e)
        {
            var openUrlDialog = new OpenUrlDialog();
            if (openUrlDialog.ShowDialog() == true)
            {
                var url = openUrlDialog.Url;
                string benchmarkContent = string.Empty;
                try
                {
                    var webRequest = WebRequest.Create(url);
                    using (var response = webRequest.GetResponse())
                    using (var content = response.GetResponseStream())
                    using (var reader = new StreamReader(content))
                        benchmarkContent = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(benchmarkContent))
                        MessageBox.Show($"content of '{url}' is empty.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception: " + ex.Message);
                }
                SourceTextBox.Text = benchmarkContent;
            }
        }

        private void OnLoadSampleButtonClick(object sender, RoutedEventArgs e)
        {
            var openSampleDialog = new OpenSampleDialog();
            if (openSampleDialog.ShowDialog() == true)
            {
                var sampleName = openSampleDialog.SampleName;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BenchmarkDotNet.Visualizer.Samples." + sampleName))
                using (StreamReader reader = new StreamReader(stream))
                    SourceTextBox.Text = reader.ReadToEnd();
            }
        }
    }
}
