using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void Button_Clicked(object? sender, EventArgs e)
    {
        SetIsRunning(true);
        try
        {
            var logger = new AccumulationLogger();
            await Task.Run(() =>
            {
                var config = default(IConfig);
#if DEBUG
                config = new DebugInProcessConfig();
#endif
                var summary = BenchmarkRunner.Run<IntroBasic>(config);
                MarkdownExporter.Console.ExportToLog(summary, logger);
                ConclusionHelper.Print(logger,
                        summary.BenchmarksCases
                               .SelectMany(benchmark => benchmark.Config.GetCompositeAnalyser().Analyse(summary))
                               .Distinct()
                               .ToList());
            });
            Summary.Text = logger.GetLog();
        }
        catch (Exception exc)
        {
            await DisplayAlertAsync("Error", exc.Message, "Ok");
        }
        finally
        {
            SetIsRunning(false);
        }
    }

    private void SetIsRunning(bool isRunning)
    {
        Indicator.IsRunning = isRunning;
        Run.IsVisible =
            Summary.IsVisible = !isRunning;
    }
}
